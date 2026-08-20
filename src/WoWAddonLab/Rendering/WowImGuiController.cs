using System.Numerics;
using WoWAddonLab.Emulator.UI;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace WoWAddonLab.Rendering;

public sealed class WowImGuiController : IDisposable
{
    private const uint TaggedTextureMarker = 0x80000000;
    private const uint TaggedTextureModeMask = 0x70000000;
    private const uint TaggedTextureDesaturationMask = 0x0FFFFFFF;
    private const uint FrameAlphaGradientTag = 0xF0000000;
    private const uint FrameAlphaGradientIdMask = 0x0FFFFFFF;
    private const uint TextureMaskTag = 0xE0000000;
    private const uint TextureMaskIdMask = 0x0FFFFFFF;
    private static readonly nint BeginFrameBufferCallback = unchecked((nint)(-2));
    private static readonly nint EndFrameBufferCallback = unchecked((nint)(-3));
    private static readonly object FrameAlphaGradientLock = new();
    private static readonly Dictionary<uint, FrameAlphaGradientDrawState>
        FrameAlphaGradientStates = [];
    private static readonly Dictionary<FrameAlphaGradientDrawState, uint>
        FrameAlphaGradientIds = [];
    private static uint _nextFrameAlphaGradientId = 1;
    private static readonly object TextureMaskLock = new();
    private static readonly Dictionary<uint, TextureMaskDrawState>
        TextureMaskStates = [];
    private static readonly Dictionary<TextureMaskDrawState, uint>
        TextureMaskIds = [];
    private static uint _nextTextureMaskId = 1;

    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private readonly List<char> _pressedCharacters = [];
    private readonly IKeyboard _keyboard;
    private bool _frameBegun;
    private int _windowWidth;
    private int _windowHeight;
    private uint _vertexBuffer;
    private uint _indexBuffer;
    private uint _vertexArray;
    private uint _fontTexture;
    private uint _program;
    private uint _frameBufferProgram;
    private int _textureUniform;
    private int _projectionUniform;
    private int _fragmentClipUniform;
    private int _desaturationUniform;
    private int _frameAlphaGradientEnabledUniform;
    private int _frameAlphaGradientEdgesUniform;
    private int _frameAlphaGradientRectangleUniform;
    private int _textureMaskCountUniform;
    private readonly int[] _textureMaskSamplerUniforms = new int[3];
    private readonly int[] _textureMaskOriginUniforms = new int[3];
    private readonly int[] _textureMaskPositionXUniforms = new int[3];
    private readonly int[] _textureMaskPositionYUniforms = new int[3];
    private int _frameBufferTextureUniform;
    private int _frameBufferAlphaUniform;
    private readonly Dictionary<int, CachedFrameBuffer> _cachedFrameBuffers = [];

    public WowImGuiController(
        GL gl,
        IWindow window,
        IInputContext input,
        Action? configureIo = null)
    {
        _gl = gl;
        _window = window;
        _input = input;
        _windowWidth = window.Size.X;
        _windowHeight = window.Size.Y;
        Context = ImGui.CreateContext();
        ImGui.SetCurrentContext(Context);
        ImGui.StyleColorsDark();
        configureIo?.Invoke();
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        CreateDeviceResources();

        _keyboard = input.Keyboards[0];
        _window.Resize += OnWindowResized;
        _keyboard.KeyDown += OnKeyDown;
        _keyboard.KeyUp += OnKeyUp;
        _keyboard.KeyChar += OnKeyChar;
        SetPerFrameData(1f / 60);
        ImGui.NewFrame();
        _frameBegun = true;
    }

    private ImGuiMouseCursor _appliedCursor = ImGuiMouseCursor.Arrow;

    public nint Context { get; }

    public static nint TextureId(
        uint handle,
        string? blendMode,
        float desaturation = 0)
    {
        var mode = blendMode?.ToUpperInvariant() switch
        {
            "ADD" => UiTextureBlendMode.Add,
            "MOD" => UiTextureBlendMode.Mod,
            "DISABLE" => UiTextureBlendMode.Disable,
            "ALPHAKEY" => UiTextureBlendMode.AlphaKey,
            _ => UiTextureBlendMode.Blend
        };
        desaturation = Math.Clamp(desaturation, 0, 1);
        if ((mode == UiTextureBlendMode.Blend && desaturation <= 0) ||
            nint.Size < sizeof(long))
        {
            return (nint)handle;
        }

        var quantizedDesaturation = Math.Min(
            TaggedTextureDesaturationMask,
            (uint)Math.Round(
                (double)desaturation * TaggedTextureDesaturationMask,
                MidpointRounding.AwayFromZero));
        var tag =
            TaggedTextureMarker |
            ((uint)mode << 28) |
            quantizedDesaturation;
        var tagged = ((ulong)tag << 32) | handle;
        return unchecked((nint)(long)tagged);
    }

    public static nint FrameAlphaGradientTextureId(
        nint textureId,
        UiFrameAlphaGradientParameters gradient)
    {
        lock (FrameAlphaGradientLock)
        {
            var state = new FrameAlphaGradientDrawState(textureId, gradient);
            if (!FrameAlphaGradientIds.TryGetValue(state, out var id))
            {
                id = _nextFrameAlphaGradientId++;
                if (id == 0 || id > FrameAlphaGradientIdMask)
                {
                    FrameAlphaGradientStates.Clear();
                    FrameAlphaGradientIds.Clear();
                    id = 1;
                    _nextFrameAlphaGradientId = 2;
                }
                FrameAlphaGradientStates[id] = state;
                FrameAlphaGradientIds[state] = id;
            }

            var tag = FrameAlphaGradientTag | id;
            var tagged = ((ulong)tag << 32) | (uint)(ulong)(long)textureId;
            return unchecked((nint)(long)tagged);
        }
    }

    public static nint TextureMaskTextureId(
        nint textureId,
        IReadOnlyList<TextureMaskDrawBinding> masks)
    {
        if (masks.Count == 0)
            return textureId;
        if (masks.Count > 3)
            throw new ArgumentOutOfRangeException(nameof(masks));

        var state = new TextureMaskDrawState(
            textureId,
            masks.Count,
            masks.ElementAtOrDefault(0),
            masks.ElementAtOrDefault(1),
            masks.ElementAtOrDefault(2));
        lock (TextureMaskLock)
        {
            if (!TextureMaskIds.TryGetValue(state, out var id))
            {
                id = _nextTextureMaskId++;
                if (id == 0 || id > TextureMaskIdMask)
                {
                    TextureMaskStates.Clear();
                    TextureMaskIds.Clear();
                    id = 1;
                    _nextTextureMaskId = 2;
                }
                TextureMaskStates[id] = state;
                TextureMaskIds[state] = id;
            }

            var tag = TextureMaskTag | id;
            var tagged = ((ulong)tag << 32) | (uint)(ulong)(long)textureId;
            return unchecked((nint)(long)tagged);
        }
    }

    public static void BeginFrameBuffer(
        ImDrawListPtr drawList,
        int frameId,
        float alpha) =>
        AddFrameBufferCallback(
            drawList,
            BeginFrameBufferCallback,
            frameId,
            alpha);

    public static void EndFrameBuffer(
        ImDrawListPtr drawList,
        int frameId,
        float alpha) =>
        AddFrameBufferCallback(
            drawList,
            EndFrameBufferCallback,
            frameId,
            alpha);

    private static void AddFrameBufferCallback(
        ImDrawListPtr drawList,
        nint callback,
        int frameId,
        float alpha)
    {
        drawList.AddDrawCmd();
        var command = drawList.CmdBuffer[drawList.CmdBuffer.Size - 1];
        command.UserCallback = callback;
        var alphaByte = (byte)Math.Clamp(
            MathF.Floor(Math.Clamp(alpha, 0, 1) * 255 + .5f),
            0,
            byte.MaxValue);
        command.UserCallbackData = unchecked((nint)(long)(
            (ulong)(uint)frameId |
            ((ulong)alphaByte << 32)));
        drawList.AddDrawCmd();
    }

    public void Update(float deltaSeconds)
    {
        ImGui.SetCurrentContext(Context);
        if (_frameBegun)
            ImGui.Render();
        SetPerFrameData(deltaSeconds);
        UpdateInput();
        UpdateMouseCursor();
        ImGui.NewFrame();
        _frameBegun = true;
    }

    private void UpdateMouseCursor()
    {
        var requested = ImGui.GetMouseCursor();
        if (requested == _appliedCursor)
            return;
        _appliedCursor = requested;
        if (_input.Mice.Count == 0)
            return;

        var cursor = _input.Mice[0].Cursor;
        if (requested == ImGuiMouseCursor.None)
        {
            cursor.CursorMode = CursorMode.Hidden;
            return;
        }

        cursor.CursorMode = CursorMode.Normal;
        cursor.StandardCursor = requested switch
        {
            ImGuiMouseCursor.TextInput => StandardCursor.IBeam,
            ImGuiMouseCursor.ResizeNS => StandardCursor.VResize,
            ImGuiMouseCursor.ResizeEW => StandardCursor.HResize,
            ImGuiMouseCursor.ResizeNESW => StandardCursor.NeswResize,
            ImGuiMouseCursor.ResizeNWSE => StandardCursor.NwseResize,
            ImGuiMouseCursor.ResizeAll => StandardCursor.ResizeAll,
            ImGuiMouseCursor.NotAllowed => StandardCursor.NotAllowed,
            ImGuiMouseCursor.Hand => StandardCursor.Hand,
            _ => StandardCursor.Arrow
        };
    }

    public void Render()
    {
        if (!_frameBegun)
            return;
        ImGui.SetCurrentContext(Context);
        _frameBegun = false;
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
        ClearFrameAlphaGradientStates();
        ClearTextureMaskStates();
    }

    public void Dispose()
    {
        _window.Resize -= OnWindowResized;
        _keyboard.KeyDown -= OnKeyDown;
        _keyboard.KeyUp -= OnKeyUp;
        _keyboard.KeyChar -= OnKeyChar;
        if (_vertexBuffer != 0)
            _gl.DeleteBuffer(_vertexBuffer);
        if (_indexBuffer != 0)
            _gl.DeleteBuffer(_indexBuffer);
        if (_vertexArray != 0)
            _gl.DeleteVertexArray(_vertexArray);
        if (_fontTexture != 0)
            _gl.DeleteTexture(_fontTexture);
        if (_program != 0)
            _gl.DeleteProgram(_program);
        if (_frameBufferProgram != 0)
            _gl.DeleteProgram(_frameBufferProgram);
        foreach (var frameBuffer in _cachedFrameBuffers.Values)
            DeleteFrameBuffer(frameBuffer);
        _cachedFrameBuffers.Clear();
        ImGui.DestroyContext(Context);
    }

    private void OnWindowResized(Vector2D<int> size)
    {
        _windowWidth = size.X;
        _windowHeight = size.Y;
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int scanCode) =>
        OnKeyEvent(keyboard, key, scanCode, true);

    private static void OnKeyUp(IKeyboard keyboard, Key key, int scanCode) =>
        OnKeyEvent(keyboard, key, scanCode, false);

    private static void OnKeyEvent(IKeyboard keyboard, Key key, int scanCode, bool down)
    {
        var translated = TranslateKey(key);
        if (translated == ImGuiKey.None)
            return;
        var io = ImGui.GetIO();
        io.AddKeyEvent(translated, down);
        io.SetKeyEventNativeData(translated, (int)key, scanCode);
    }

    private void OnKeyChar(IKeyboard keyboard, char character) =>
        _pressedCharacters.Add(character);

    private void SetPerFrameData(float deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);
        if (_windowWidth > 0 && _windowHeight > 0)
        {
            io.DisplayFramebufferScale = new Vector2(
                (float)_window.FramebufferSize.X / _windowWidth,
                (float)_window.FramebufferSize.Y / _windowHeight);
        }
        io.DeltaTime = Math.Max(deltaSeconds, 1e-6f);
    }

    private void UpdateInput()
    {
        var io = ImGui.GetIO();
        using var mouse = _input.Mice[0].CaptureState();
        io.MouseDown[0] = mouse.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonPressed(MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonPressed(MouseButton.Middle);
        io.MousePos = mouse.Position;
        var wheel = mouse.GetScrollWheels()[0];
        io.MouseWheel = wheel.Y;
        io.MouseWheelH = wheel.X;
        foreach (var character in _pressedCharacters)
            io.AddInputCharacter(character);
        _pressedCharacters.Clear();
        io.KeyCtrl = _keyboard.IsKeyPressed(Key.ControlLeft) ||
                     _keyboard.IsKeyPressed(Key.ControlRight);
        io.KeyShift = _keyboard.IsKeyPressed(Key.ShiftLeft) ||
                      _keyboard.IsKeyPressed(Key.ShiftRight);
        io.KeyAlt = _keyboard.IsKeyPressed(Key.AltLeft) ||
                    _keyboard.IsKeyPressed(Key.AltRight);
        io.KeySuper = _keyboard.IsKeyPressed(Key.SuperLeft) ||
                      _keyboard.IsKeyPressed(Key.SuperRight);
    }

    private static ImGuiKey TranslateKey(Key key)
    {
        var name = key.ToString();
        if (Enum.TryParse<ImGuiKey>(name, out var direct))
            return direct;
        if (name.StartsWith("Number", StringComparison.Ordinal) &&
            name.Length == 7 &&
            Enum.TryParse<ImGuiKey>($"_{name[^1]}", out var number))
            return number;
        return key switch
        {
            Key.Left => ImGuiKey.LeftArrow,
            Key.Right => ImGuiKey.RightArrow,
            Key.Up => ImGuiKey.UpArrow,
            Key.Down => ImGuiKey.DownArrow,
            Key.ShiftLeft => ImGuiKey.LeftShift,
            Key.ShiftRight => ImGuiKey.RightShift,
            Key.ControlLeft => ImGuiKey.LeftCtrl,
            Key.ControlRight => ImGuiKey.RightCtrl,
            Key.AltLeft => ImGuiKey.LeftAlt,
            Key.AltRight => ImGuiKey.RightAlt,
            Key.SuperLeft => ImGuiKey.LeftSuper,
            Key.SuperRight => ImGuiKey.RightSuper,
            Key.BackSlash => ImGuiKey.Backslash,
            _ => ImGuiKey.None
        };
    }

    private unsafe void CreateDeviceResources()
    {
        const string vertexSource = """
            #version 330
            layout (location = 0) in vec2 Position;
            layout (location = 1) in vec2 UV;
            layout (location = 2) in vec4 Color;
            uniform mat4 ProjMtx;
            out vec2 Frag_UV;
            out vec4 Frag_Color;
            out vec2 Frag_Position;
            void main()
            {
                Frag_UV = UV;
                Frag_Color = Color;
                Frag_Position = Position;
                gl_Position = ProjMtx * vec4(Position.xy, 0, 1);
            }
            """;
        const string fragmentSource = """
            #version 330
            in vec2 Frag_UV;
            in vec4 Frag_Color;
            in vec2 Frag_Position;
            uniform sampler2D Texture;
            uniform vec4 WowFragmentClip;
            uniform float WowDesaturation;
            uniform int WowFrameAlphaGradientEnabled;
            uniform vec4 WowFrameAlphaGradientEdges;
            uniform vec4 WowFrameAlphaGradientRectangle;
            uniform int WowTextureMaskCount;
            uniform sampler2D WowTextureMask0;
            uniform sampler2D WowTextureMask1;
            uniform sampler2D WowTextureMask2;
            uniform vec2 WowTextureMaskOrigin[3];
            uniform vec2 WowTextureMaskPositionX[3];
            uniform vec2 WowTextureMaskPositionY[3];
            layout (location = 0) out vec4 Out_Color;
            float WowSaturate(float value)
            {
                return isnan(value) ? 0.0 : clamp(value, 0.0, 1.0);
            }
            void main()
            {
                if (
                    gl_FragCoord.x < WowFragmentClip.x ||
                    gl_FragCoord.y < WowFragmentClip.y ||
                    gl_FragCoord.x >= WowFragmentClip.z ||
                    gl_FragCoord.y >= WowFragmentClip.w)
                {
                    discard;
                }
                vec4 color = Frag_Color * texture(Texture, Frag_UV.st);
                float luminance = dot(color.rgb, vec3(0.299, 0.587, 0.114));
                color.rgb = mix(color.rgb, vec3(luminance), WowDesaturation);
                Out_Color = color;
                if (WowTextureMaskCount > 0)
                {
                    vec2 maskUv =
                        WowTextureMaskOrigin[0] +
                        WowTextureMaskPositionX[0] * Frag_Position.x +
                        WowTextureMaskPositionY[0] * Frag_Position.y;
                    Out_Color.a *= texture(WowTextureMask0, maskUv).a;
                }
                if (WowTextureMaskCount > 1)
                {
                    vec2 maskUv =
                        WowTextureMaskOrigin[1] +
                        WowTextureMaskPositionX[1] * Frag_Position.x +
                        WowTextureMaskPositionY[1] * Frag_Position.y;
                    Out_Color.a *= texture(WowTextureMask1, maskUv).a;
                }
                if (WowTextureMaskCount > 2)
                {
                    vec2 maskUv =
                        WowTextureMaskOrigin[2] +
                        WowTextureMaskPositionX[2] * Frag_Position.x +
                        WowTextureMaskPositionY[2] * Frag_Position.y;
                    Out_Color.a *= texture(WowTextureMask2, maskUv).a;
                }
                if (WowFrameAlphaGradientEnabled != 0)
                {
                    vec4 distance = abs(
                        Frag_Position.xyxy - WowFrameAlphaGradientRectangle);
                    vec4 fade = vec4(
                        WowSaturate(distance.x / WowFrameAlphaGradientEdges.x),
                        WowSaturate(distance.y / WowFrameAlphaGradientEdges.y),
                        WowSaturate(distance.z / WowFrameAlphaGradientEdges.z),
                        WowSaturate(distance.w / WowFrameAlphaGradientEdges.w));
                    Out_Color.a *= fade.x * fade.y * fade.z * fade.w;
                }
            }
            """;

        var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);
        _gl.LinkProgram(_program);
        _gl.GetProgram(_program, GLEnum.LinkStatus, out var linked);
        if (linked == 0)
            throw new InvalidOperationException($"ImGui shader link failed: {_gl.GetProgramInfoLog(_program)}");
        _gl.DetachShader(_program, vertexShader);
        _gl.DetachShader(_program, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
        _textureUniform = _gl.GetUniformLocation(_program, "Texture");
        _projectionUniform = _gl.GetUniformLocation(_program, "ProjMtx");
        _fragmentClipUniform =
            _gl.GetUniformLocation(_program, "WowFragmentClip");
        _desaturationUniform = _gl.GetUniformLocation(_program, "WowDesaturation");
        _frameAlphaGradientEnabledUniform =
            _gl.GetUniformLocation(_program, "WowFrameAlphaGradientEnabled");
        _frameAlphaGradientEdgesUniform =
            _gl.GetUniformLocation(_program, "WowFrameAlphaGradientEdges");
        _frameAlphaGradientRectangleUniform =
            _gl.GetUniformLocation(_program, "WowFrameAlphaGradientRectangle");
        _textureMaskCountUniform =
            _gl.GetUniformLocation(_program, "WowTextureMaskCount");
        for (var index = 0; index < 3; index++)
        {
            _textureMaskSamplerUniforms[index] =
                _gl.GetUniformLocation(_program, $"WowTextureMask{index}");
            _textureMaskOriginUniforms[index] =
                _gl.GetUniformLocation(_program, $"WowTextureMaskOrigin[{index}]");
            _textureMaskPositionXUniforms[index] =
                _gl.GetUniformLocation(_program, $"WowTextureMaskPositionX[{index}]");
            _textureMaskPositionYUniforms[index] =
                _gl.GetUniformLocation(_program, $"WowTextureMaskPositionY[{index}]");
        }

        CreateFrameBufferCompositeProgram();

        _vertexBuffer = _gl.GenBuffer();
        _indexBuffer = _gl.GenBuffer();
        _vertexArray = _gl.GenVertexArray();
        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(GLEnum.ArrayBuffer, _vertexBuffer);
        _gl.BindBuffer(GLEnum.ElementArrayBuffer, _indexBuffer);
        _gl.EnableVertexAttribArray(0);
        _gl.EnableVertexAttribArray(1);
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(0, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)0);
        _gl.VertexAttribPointer(1, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)8);
        _gl.VertexAttribPointer(2, 4, GLEnum.UnsignedByte, true, (uint)sizeof(ImDrawVert), (void*)16);

        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out _);
        _fontTexture = _gl.GenTexture();
        _gl.BindTexture(GLEnum.Texture2D, _fontTexture);
        _gl.TexImage2D(
            GLEnum.Texture2D,
            0,
            InternalFormat.Rgba8,
            (uint)width,
            (uint)height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            pixels);
        _gl.TexParameter(
            GLEnum.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        _gl.TexParameter(
            GLEnum.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        io.Fonts.SetTexID((nint)_fontTexture);
        _gl.BindVertexArray(0);
    }

    private void CreateFrameBufferCompositeProgram()
    {
        const string vertexSource = """
            #version 330
            out vec2 FrameBufferUv;
            void main()
            {
                int x = (gl_VertexID << 1) & 2;
                int y = gl_VertexID & 2;
                FrameBufferUv = vec2(x, y);
                gl_Position = vec4(
                    FrameBufferUv.x * 2.0 - 1.0,
                    1.0 - FrameBufferUv.y * 2.0,
                    0.0,
                    1.0);
            }
            """;
        const string fragmentSource = """
            #version 330
            in vec2 FrameBufferUv;
            uniform sampler2D FrameBufferTexture;
            uniform float FrameBufferAlpha;
            layout (location = 0) out vec4 OutColor;
            void main()
            {
                vec4 sampled = texture(
                    FrameBufferTexture,
                    vec2(FrameBufferUv.x, 1.0 - FrameBufferUv.y));
                OutColor = vec4(sampled.rgb, sampled.a * FrameBufferAlpha);
            }
            """;

        var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
        _frameBufferProgram = _gl.CreateProgram();
        _gl.AttachShader(_frameBufferProgram, vertexShader);
        _gl.AttachShader(_frameBufferProgram, fragmentShader);
        _gl.LinkProgram(_frameBufferProgram);
        _gl.GetProgram(_frameBufferProgram, GLEnum.LinkStatus, out var linked);
        if (linked == 0)
        {
            throw new InvalidOperationException(
                $"Framebuffer composite shader link failed: " +
                _gl.GetProgramInfoLog(_frameBufferProgram));
        }

        _gl.DetachShader(_frameBufferProgram, vertexShader);
        _gl.DetachShader(_frameBufferProgram, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
        _frameBufferTextureUniform =
            _gl.GetUniformLocation(_frameBufferProgram, "FrameBufferTexture");
        _frameBufferAlphaUniform =
            _gl.GetUniformLocation(_frameBufferProgram, "FrameBufferAlpha");
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);
        if (compiled == 0)
            throw new InvalidOperationException(
                $"ImGui {type} compilation failed: {_gl.GetShaderInfoLog(shader)}");
        return shader;
    }

    private unsafe void RenderDrawData(ImDrawDataPtr drawData)
    {
        var framebufferWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        var framebufferHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0)
            return;

        _gl.GetInteger(GLEnum.ActiveTexture, out var previousActiveTexture);
        _gl.ActiveTexture(GLEnum.Texture0);
        _gl.GetInteger(GLEnum.CurrentProgram, out var previousProgram);
        _gl.GetInteger(GLEnum.TextureBinding2D, out var previousTexture);
        _gl.GetInteger(GLEnum.SamplerBinding, out var previousSampler);
        Span<int> previousMaskTextures = stackalloc int[3];
        Span<int> previousMaskSamplers = stackalloc int[3];
        for (var index = 0; index < 3; index++)
        {
            _gl.ActiveTexture((GLEnum)((int)GLEnum.Texture1 + index));
            _gl.GetInteger(
                GLEnum.TextureBinding2D,
                out previousMaskTextures[index]);
            _gl.GetInteger(
                GLEnum.SamplerBinding,
                out previousMaskSamplers[index]);
        }
        _gl.ActiveTexture(GLEnum.Texture0);
        _gl.GetInteger(GLEnum.ArrayBufferBinding, out var previousArrayBuffer);
        _gl.GetInteger(GLEnum.VertexArrayBinding, out var previousVertexArray);
        _gl.GetInteger(GLEnum.FramebufferBinding, out var previousFrameBuffer);
        _gl.GetInteger(GLEnum.BlendSrcRgb, out var previousBlendSourceRgb);
        _gl.GetInteger(GLEnum.BlendDstRgb, out var previousBlendDestinationRgb);
        _gl.GetInteger(GLEnum.BlendSrcAlpha, out var previousBlendSourceAlpha);
        _gl.GetInteger(GLEnum.BlendDstAlpha, out var previousBlendDestinationAlpha);
        _gl.GetInteger(GLEnum.BlendEquationRgb, out var previousBlendEquationRgb);
        _gl.GetInteger(GLEnum.BlendEquationAlpha, out var previousBlendEquationAlpha);
        Span<int> previousScissor = stackalloc int[4];
        _gl.GetInteger(GLEnum.ScissorBox, previousScissor);
        Span<int> previousViewport = stackalloc int[4];
        _gl.GetInteger(GLEnum.Viewport, previousViewport);
        Span<float> previousClearColor = stackalloc float[4];
        _gl.GetFloat(GLEnum.ColorClearValue, previousClearColor);
        var blendEnabled = _gl.IsEnabled(GLEnum.Blend);
        var cullEnabled = _gl.IsEnabled(GLEnum.CullFace);
        var depthEnabled = _gl.IsEnabled(GLEnum.DepthTest);
        var stencilEnabled = _gl.IsEnabled(GLEnum.StencilTest);
        var scissorEnabled = _gl.IsEnabled(GLEnum.ScissorTest);

        SetupRenderState(drawData);
        _gl.Viewport(0, 0, (uint)framebufferWidth, (uint)framebufferHeight);
        var clipOffset = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;
        var frameBufferStack = new Stack<ActiveFrameBuffer>();
        for (var listIndex = 0; listIndex < drawData.CmdListsCount; listIndex++)
        {
            var list = drawData.CmdLists[listIndex];
            _gl.BufferData(
                GLEnum.ArrayBuffer,
                (nuint)(list.VtxBuffer.Size * sizeof(ImDrawVert)),
                (void*)list.VtxBuffer.Data,
                GLEnum.StreamDraw);
            _gl.BufferData(
                GLEnum.ElementArrayBuffer,
                (nuint)(list.IdxBuffer.Size * sizeof(ushort)),
                (void*)list.IdxBuffer.Data,
                GLEnum.StreamDraw);

            for (var commandIndex = 0; commandIndex < list.CmdBuffer.Size; commandIndex++)
            {
                var command = list.CmdBuffer[commandIndex];
                if (command.UserCallback != nint.Zero)
                {
                    if (command.UserCallback == (nint)(-1))
                        SetupRenderState(drawData);
                    else if (command.UserCallback == BeginFrameBufferCallback)
                    {
                        BeginFrameBuffer(
                            command.UserCallbackData,
                            framebufferWidth,
                            framebufferHeight,
                            drawData,
                            frameBufferStack);
                    }
                    else if (command.UserCallback == EndFrameBufferCallback)
                    {
                        EndFrameBuffer(
                            command.UserCallbackData,
                            framebufferWidth,
                            framebufferHeight,
                            drawData,
                            frameBufferStack);
                    }
                    continue;
                }

                var clip = new Vector4(
                    (command.ClipRect.X - clipOffset.X) * clipScale.X,
                    (command.ClipRect.Y - clipOffset.Y) * clipScale.Y,
                    (command.ClipRect.Z - clipOffset.X) * clipScale.X,
                    (command.ClipRect.W - clipOffset.Y) * clipScale.Y);
                var fragmentClip = UiFragmentClip.FromTopLeft(
                    clip.X,
                    clip.Y,
                    clip.Z,
                    clip.W,
                    framebufferWidth,
                    framebufferHeight);
                if (fragmentClip.IsEmpty)
                    continue;
                var scissor = fragmentClip.ConservativeScissor();
                _gl.Scissor(
                    scissor.X,
                    scissor.Y,
                    (uint)scissor.Width,
                    (uint)scissor.Height);
                _gl.Uniform4(
                    _fragmentClipUniform,
                    fragmentClip.Left,
                    fragmentClip.Bottom,
                    fragmentClip.Right,
                    fragmentClip.Top);

                DecodeTextureId(
                    command.TextureId,
                    out var texture,
                    out var mode,
                    out var desaturation,
                    out var frameAlphaGradient,
                    out var textureMasks);
                ApplyBlendMode(mode);
                _gl.Uniform1(_desaturationUniform, desaturation);
                ApplyFrameAlphaGradient(frameAlphaGradient);
                ApplyTextureMasks(textureMasks);
                _gl.BindTexture(GLEnum.Texture2D, texture);
                _gl.DrawElementsBaseVertex(
                    GLEnum.Triangles,
                    command.ElemCount,
                    GLEnum.UnsignedShort,
                    (void*)(command.IdxOffset * sizeof(ushort)),
                    (int)command.VtxOffset);
            }
        }

        if (frameBufferStack.Count != 0)
            throw new InvalidOperationException("Unbalanced framebuffer render commands.");

        _gl.UseProgram((uint)previousProgram);
        for (var index = 0; index < 3; index++)
        {
            _gl.ActiveTexture((GLEnum)((int)GLEnum.Texture1 + index));
            _gl.BindTexture(
                GLEnum.Texture2D,
                (uint)previousMaskTextures[index]);
            _gl.BindSampler(
                (uint)(index + 1),
                (uint)previousMaskSamplers[index]);
        }
        _gl.ActiveTexture(GLEnum.Texture0);
        _gl.BindTexture(GLEnum.Texture2D, (uint)previousTexture);
        _gl.BindSampler(0, (uint)previousSampler);
        _gl.ActiveTexture((GLEnum)previousActiveTexture);
        _gl.BindVertexArray((uint)previousVertexArray);
        _gl.BindBuffer(GLEnum.ArrayBuffer, (uint)previousArrayBuffer);
        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            (uint)previousFrameBuffer);
        _gl.Viewport(
            previousViewport[0],
            previousViewport[1],
            (uint)previousViewport[2],
            (uint)previousViewport[3]);
        _gl.ClearColor(
            previousClearColor[0],
            previousClearColor[1],
            previousClearColor[2],
            previousClearColor[3]);
        _gl.BlendEquationSeparate(
            (GLEnum)previousBlendEquationRgb,
            (GLEnum)previousBlendEquationAlpha);
        _gl.BlendFuncSeparate(
            (GLEnum)previousBlendSourceRgb,
            (GLEnum)previousBlendDestinationRgb,
            (GLEnum)previousBlendSourceAlpha,
            (GLEnum)previousBlendDestinationAlpha);
        RestoreEnabled(GLEnum.Blend, blendEnabled);
        RestoreEnabled(GLEnum.CullFace, cullEnabled);
        RestoreEnabled(GLEnum.DepthTest, depthEnabled);
        RestoreEnabled(GLEnum.StencilTest, stencilEnabled);
        RestoreEnabled(GLEnum.ScissorTest, scissorEnabled);
        _gl.Scissor(
            previousScissor[0],
            previousScissor[1],
            (uint)previousScissor[2],
            (uint)previousScissor[3]);
    }

    private void BeginFrameBuffer(
        nint commandData,
        int width,
        int height,
        ImDrawDataPtr drawData,
        Stack<ActiveFrameBuffer> stack)
    {
        DecodeFrameBufferCommand(
            commandData,
            out var frameId,
            out var alpha);
        _gl.GetInteger(GLEnum.FramebufferBinding, out var previousFrameBuffer);
        var target = EnsureFrameBuffer(frameId, width, height);
        stack.Push(new ActiveFrameBuffer(
            frameId,
            alpha,
            target,
            (uint)previousFrameBuffer));

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.FrameBuffer);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.Disable(GLEnum.ScissorTest);
        _gl.ClearColor(0, 0, 0, 0);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        SetupRenderState(drawData);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    private void EndFrameBuffer(
        nint commandData,
        int width,
        int height,
        ImDrawDataPtr drawData,
        Stack<ActiveFrameBuffer> stack)
    {
        DecodeFrameBufferCommand(
            commandData,
            out var frameId,
            out _);
        if (!stack.TryPop(out var active) || active.FrameId != frameId)
        {
            throw new InvalidOperationException(
                $"Mismatched framebuffer render command for Frame {frameId}.");
        }

        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            active.PreviousFrameBuffer);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.Disable(GLEnum.ScissorTest);
        _gl.Disable(GLEnum.CullFace);
        _gl.Disable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.StencilTest);
        _gl.UseProgram(_frameBufferProgram);
        _gl.ActiveTexture(GLEnum.Texture0);
        _gl.Uniform1(_frameBufferTextureUniform, 0);
        _gl.Uniform1(_frameBufferAlphaUniform, active.Alpha);
        _gl.BindTexture(GLEnum.Texture2D, active.Target.Texture);
        ApplyBlendMode(UiTextureBlendMode.Blend);
        _gl.BindVertexArray(_vertexArray);
        _gl.DrawArrays(GLEnum.Triangles, 0, 3);

        SetupRenderState(drawData);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    private unsafe CachedFrameBuffer EnsureFrameBuffer(
        int frameId,
        int width,
        int height)
    {
        if (_cachedFrameBuffers.TryGetValue(frameId, out var existing) &&
            existing.Width == width &&
            existing.Height == height)
        {
            return existing;
        }

        if (existing is not null)
            DeleteFrameBuffer(existing);

        _gl.GetInteger(GLEnum.FramebufferBinding, out var previousFrameBuffer);
        _gl.GetInteger(GLEnum.TextureBinding2D, out var previousTexture);
        var texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, texture);
        _gl.TexImage2D(
            TextureTarget.Texture2D,
            0,
            InternalFormat.Rgba8,
            (uint)width,
            (uint)height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            null);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);

        var frameBuffer = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
        _gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            texture,
            0);
        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            (uint)previousFrameBuffer);
        _gl.BindTexture(GLEnum.Texture2D, (uint)previousTexture);
        if (status != GLEnum.FramebufferComplete)
        {
            _gl.DeleteFramebuffer(frameBuffer);
            _gl.DeleteTexture(texture);
            throw new InvalidOperationException(
                $"Framebuffer for Frame {frameId} is incomplete: {status}.");
        }

        var created = new CachedFrameBuffer(
            frameBuffer,
            texture,
            width,
            height);
        _cachedFrameBuffers[frameId] = created;
        return created;
    }

    private void DeleteFrameBuffer(CachedFrameBuffer frameBuffer)
    {
        if (frameBuffer.FrameBuffer != 0)
            _gl.DeleteFramebuffer(frameBuffer.FrameBuffer);
        if (frameBuffer.Texture != 0)
            _gl.DeleteTexture(frameBuffer.Texture);
    }

    private static void DecodeFrameBufferCommand(
        nint commandData,
        out int frameId,
        out float alpha)
    {
        var packed = unchecked((ulong)(long)commandData);
        frameId = unchecked((int)(uint)packed);
        alpha = (byte)(packed >> 32) / 255f;
    }

    private unsafe void SetupRenderState(ImDrawDataPtr drawData)
    {
        _gl.Enable(GLEnum.Blend);
        _gl.BlendEquation(GLEnum.FuncAdd);
        _gl.Disable(GLEnum.CullFace);
        _gl.Disable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.StencilTest);
        _gl.Enable(GLEnum.ScissorTest);
        _gl.UseProgram(_program);
        _gl.Uniform1(_textureUniform, 0);
        _gl.Uniform4(
            _fragmentClipUniform,
            0,
            0,
            float.MaxValue,
            float.MaxValue);
        _gl.Uniform1(_desaturationUniform, 0);
        ApplyFrameAlphaGradient(null);
        ApplyTextureMasks(null);
        for (var index = 0; index < 3; index++)
            _gl.Uniform1(_textureMaskSamplerUniforms[index], index + 1);
        _gl.BindSampler(0, 0);

        var left = drawData.DisplayPos.X;
        var right = drawData.DisplayPos.X + drawData.DisplaySize.X;
        var top = drawData.DisplayPos.Y;
        var bottom = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
        Span<float> projection = stackalloc float[]
        {
            2 / (right - left), 0, 0, 0,
            0, 2 / (top - bottom), 0, 0,
            0, 0, -1, 0,
            (right + left) / (left - right),
            (top + bottom) / (bottom - top),
            0, 1
        };
        _gl.UniformMatrix4(_projectionUniform, 1, false, projection);
        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(GLEnum.ArrayBuffer, _vertexBuffer);
        _gl.BindBuffer(GLEnum.ElementArrayBuffer, _indexBuffer);
        ApplyBlendMode(UiTextureBlendMode.Blend);
    }

    private void ApplyBlendMode(UiTextureBlendMode mode)
    {
        _gl.BlendEquation(GLEnum.FuncAdd);
        var state = UiTextureBlendState.Resolve(mode);
        if (!state.Enabled)
        {
            _gl.Disable(GLEnum.Blend);
            return;
        }

        _gl.Enable(GLEnum.Blend);
        _gl.BlendFuncSeparate(
            ToGlBlendFactor(state.SourceRgb),
            ToGlBlendFactor(state.DestinationRgb),
            ToGlBlendFactor(state.SourceAlpha),
            ToGlBlendFactor(state.DestinationAlpha));
    }

    private static GLEnum ToGlBlendFactor(UiBlendFactor factor) => factor switch
    {
        UiBlendFactor.Zero => GLEnum.Zero,
        UiBlendFactor.One => GLEnum.One,
        UiBlendFactor.SourceAlpha => GLEnum.SrcAlpha,
        UiBlendFactor.OneMinusSourceAlpha => GLEnum.OneMinusSrcAlpha,
        UiBlendFactor.DestinationAlpha => GLEnum.DstAlpha,
        UiBlendFactor.DestinationColor => GLEnum.DstColor,
        _ => GLEnum.One
    };

    private static void DecodeTextureId(
        nint id,
        out uint texture,
        out UiTextureBlendMode mode,
        out float desaturation,
        out UiFrameAlphaGradientParameters? frameAlphaGradient,
        out TextureMaskDrawState? textureMasks)
    {
        var raw = unchecked((ulong)(long)id);
        texture = (uint)raw;
        var tag = (uint)(raw >> 32);
        if ((tag & 0xF0000000) == FrameAlphaGradientTag)
        {
            lock (FrameAlphaGradientLock)
            {
                if (FrameAlphaGradientStates.TryGetValue(
                        tag & FrameAlphaGradientIdMask,
                        out var state))
                {
                    DecodeTextureId(
                        state.TextureId,
                        out texture,
                        out mode,
                        out desaturation,
                        out _,
                        out textureMasks);
                    frameAlphaGradient = state.Gradient;
                    return;
                }
            }
        }
        if ((tag & 0xF0000000) == TextureMaskTag)
        {
            lock (TextureMaskLock)
            {
                if (TextureMaskStates.TryGetValue(
                        tag & TextureMaskIdMask,
                        out var state))
                {
                    DecodeTextureId(
                        state.TextureId,
                        out texture,
                        out mode,
                        out desaturation,
                        out frameAlphaGradient,
                        out _);
                    textureMasks = state;
                    return;
                }
            }
        }
        if ((tag & TaggedTextureMarker) == 0)
        {
            mode = UiTextureBlendMode.Blend;
            desaturation = 0;
            frameAlphaGradient = null;
            textureMasks = null;
            return;
        }

        mode = (UiTextureBlendMode)((tag & TaggedTextureModeMask) >> 28);
        desaturation =
            (tag & TaggedTextureDesaturationMask) /
            (float)TaggedTextureDesaturationMask;
        frameAlphaGradient = null;
        textureMasks = null;
    }

    private void ApplyFrameAlphaGradient(
        UiFrameAlphaGradientParameters? gradient)
    {
        _gl.Uniform1(
            _frameAlphaGradientEnabledUniform,
            gradient.HasValue ? 1 : 0);
        if (gradient is not { } value)
            return;
        _gl.Uniform4(
            _frameAlphaGradientEdgesUniform,
            value.EdgeWidths.X,
            value.EdgeWidths.Y,
            value.EdgeWidths.Z,
            value.EdgeWidths.W);
        _gl.Uniform4(
            _frameAlphaGradientRectangleUniform,
            value.Rectangle.X,
            value.Rectangle.Y,
            value.Rectangle.Z,
            value.Rectangle.W);
    }

    private void ApplyTextureMasks(TextureMaskDrawState? state)
    {
        _gl.Uniform1(_textureMaskCountUniform, state?.Count ?? 0);
        for (var index = 0; index < 3; index++)
        {
            var binding = state?.Binding(index);
            _gl.ActiveTexture((GLEnum)((int)GLEnum.Texture1 + index));
            _gl.BindSampler((uint)(index + 1), 0);
            _gl.BindTexture(
                GLEnum.Texture2D,
                binding?.Texture ?? 0);
            if (binding is not { } value)
                continue;
            var transform = value.Transform;
            _gl.Uniform2(
                _textureMaskOriginUniforms[index],
                transform.Origin.X,
                transform.Origin.Y);
            _gl.Uniform2(
                _textureMaskPositionXUniforms[index],
                transform.PositionX.X,
                transform.PositionX.Y);
            _gl.Uniform2(
                _textureMaskPositionYUniforms[index],
                transform.PositionY.X,
                transform.PositionY.Y);
        }
        _gl.ActiveTexture(GLEnum.Texture0);
    }

    private static void ClearFrameAlphaGradientStates()
    {
        lock (FrameAlphaGradientLock)
        {
            FrameAlphaGradientStates.Clear();
            FrameAlphaGradientIds.Clear();
            _nextFrameAlphaGradientId = 1;
        }
    }

    private static void ClearTextureMaskStates()
    {
        lock (TextureMaskLock)
        {
            TextureMaskStates.Clear();
            TextureMaskIds.Clear();
            _nextTextureMaskId = 1;
        }
    }

    private void RestoreEnabled(GLEnum capability, bool enabled)
    {
        if (enabled)
            _gl.Enable(capability);
        else
            _gl.Disable(capability);
    }

    private readonly record struct FrameAlphaGradientDrawState(
        nint TextureId,
        UiFrameAlphaGradientParameters Gradient);

    public readonly record struct TextureMaskDrawBinding(
        uint Texture,
        UiTextureMaskShaderTransform Transform);

    private readonly record struct TextureMaskDrawState(
        nint TextureId,
        int Count,
        TextureMaskDrawBinding First,
        TextureMaskDrawBinding Second,
        TextureMaskDrawBinding Third)
    {
        public TextureMaskDrawBinding Binding(int index) =>
            index switch
            {
                0 => First,
                1 => Second,
                2 => Third,
                _ => default
            };
    }

    private sealed record CachedFrameBuffer(
        uint FrameBuffer,
        uint Texture,
        int Width,
        int Height);

    private readonly record struct ActiveFrameBuffer(
        int FrameId,
        float Alpha,
        CachedFrameBuffer Target,
        uint PreviousFrameBuffer);
}
