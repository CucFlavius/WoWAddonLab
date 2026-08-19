using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowDurationTextBindingApi
{
    private const string MetatableName = "DurationTextBinding";
    private const int StorageMagic = 0x44544244;

    private static readonly lua_CFunction GarbageCollectCallback = GarbageCollect;
    private static readonly lua_CFunction IndexCallback = Index;
    private static readonly lua_CFunction NewIndexCallback = NewIndex;
    private static readonly lua_CFunction EqualCallback = Equal;
    private static readonly lua_CFunction ToStringCallback = ToStringValue;
    private static readonly lua_CFunction DumpCallback = Dump;

    private static readonly IReadOnlyDictionary<string, lua_CFunction> Methods =
        new Dictionary<string, lua_CFunction>(StringComparer.Ordinal)
        {
            ["CanFormatText"] = state => Dispatch(state, "CanFormatText"),
            ["CanUpdateFontString"] = state => Dispatch(state, "CanUpdateFontString"),
            ["Disable"] = state => Dispatch(state, "Disable"),
            ["Enable"] = state => Dispatch(state, "Enable"),
            ["GetDuration"] = state => Dispatch(state, "GetDuration"),
            ["GetExpiredText"] = state => Dispatch(state, "GetExpiredText"),
            ["GetFontString"] = state => Dispatch(state, "GetFontString"),
            ["GetFormattedText"] = state => Dispatch(state, "GetFormattedText"),
            ["GetTimeModifier"] = state => Dispatch(state, "GetTimeModifier"),
            ["GetUpdateInterval"] = state => Dispatch(state, "GetUpdateInterval"),
            ["GetZeroDurationText"] = state => Dispatch(state, "GetZeroDurationText"),
            ["HasSecretValues"] = state => Dispatch(state, "HasSecretValues"),
            ["IsEnabled"] = state => Dispatch(state, "IsEnabled"),
            ["SetDuration"] = state => Dispatch(state, "SetDuration"),
            ["SetEnabled"] = state => Dispatch(state, "SetEnabled"),
            ["SetExpiredText"] = state => Dispatch(state, "SetExpiredText"),
            ["SetFontString"] = state => Dispatch(state, "SetFontString"),
            ["SetFormatter"] = state => Dispatch(state, "SetFormatter"),
            ["SetTextFormat"] = state => Dispatch(state, "SetTextFormat"),
            ["SetTimeModifier"] = state => Dispatch(state, "SetTimeModifier"),
            ["SetToDefaults"] = state => Dispatch(state, "SetToDefaults"),
            ["SetUpdateInterval"] = state => Dispatch(state, "SetUpdateInterval"),
            ["SetZeroDurationText"] = state => Dispatch(state, "SetZeroDurationText"),
            ["UpdateFontString"] = state => Dispatch(state, "UpdateFontString")
        };

    public static void Register(lua_State state)
    {
        RegisterMetatable(state);
        RegisterEnums(state);
    }

    public static void Push(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var binding = new WowDurationTextBindingState();
        runtime.DurationTextBindings.Add(binding);

        lua_newtable(state);
        var propertyTableReference = LuaRuntime.CaptureValue(state, -1);
        lua_pop(state, 1);

        unsafe
        {
            var storage = (BindingStorage*)lua_newuserdata(
                state,
                (UIntPtr)sizeof(BindingStorage));
            storage->Magic = StorageMagic;
            storage->PropertyTableReference = propertyTableReference;
            storage->StateHandle = GCHandle.ToIntPtr(GCHandle.Alloc(binding));
        }
        luaL_getmetatable(state, MetatableName);
        lua_setmetatable(state, -2);
    }

    public static void Tick(LuaRuntime runtime, double deltaSeconds)
    {
        foreach (var binding in runtime.DurationTextBindings.ToArray())
        {
            if (!binding.Enabled)
                continue;
            if (binding.UpdateInterval > 0)
            {
                binding.UpdateElapsed += (float)deltaSeconds;
                if (binding.UpdateElapsed < binding.UpdateInterval)
                    continue;
                binding.UpdateElapsed = 0;
            }
            UpdateFontString(runtime, binding);
        }
    }

    private static void RegisterMetatable(lua_State state)
    {
        if (luaL_newmetatable(state, MetatableName) == 0)
        {
            lua_pop(state, 1);
            return;
        }
        foreach (var (name, callback) in Methods)
        {
            lua_pushcfunction(state, callback);
            lua_setfield(state, -2, name);
        }
        lua_pushcfunction(state, GarbageCollectCallback);
        lua_setfield(state, -2, "__gc");
        lua_pushcfunction(state, IndexCallback);
        lua_setfield(state, -2, "__index");
        lua_pushcfunction(state, NewIndexCallback);
        lua_setfield(state, -2, "__newindex");
        lua_pushcfunction(state, EqualCallback);
        lua_setfield(state, -2, "__eq");
        lua_pushcfunction(state, ToStringCallback);
        lua_setfield(state, -2, "__tostring");
        lua_pushcfunction(state, DumpCallback);
        lua_setfield(state, -2, "__dump");
        lua_pushboolean(state, 0);
        lua_setfield(state, -2, "__metatable");
        lua_pop(state, 1);
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }
        SetEnum(
            state,
            "DurationTextBindingProperty",
            ("RemainingDuration", 0), ("RemainingPercent", 1),
            ("ElapsedDuration", 2), ("ElapsedPercent", 3),
            ("TotalDuration", 4), ("StartTime", 5), ("EndTime", 6));
        SetEnumMeta(state, "DurationTextBindingPropertyMeta", 7, 0, 6);
        SetEnum(
            state,
            "DurationTimeModifier",
            ("RealTime", 0), ("BaseTime", 1));
        SetEnumMeta(state, "DurationTimeModifierMeta", 2, 0, 1);
        lua_setglobal(state, "Enum");
    }

    private static int Dispatch(lua_State state, string operation)
    {
        var usage = Usage(operation);
        if (!TryRead(state, 1, out var binding))
            return luaL_error(state, usage);
        var runtime = LuaBindings.GetRuntime(state);

        switch (operation)
        {
            case "CanFormatText":
                return PushBooleanGetter(
                    state,
                    usage,
                    binding!.DurationReference > 0 && binding.TextFormat.Length > 0);
            case "CanUpdateFontString":
                return PushBooleanGetter(
                    state,
                    usage,
                    binding!.FontStringId is not null &&
                    binding.DurationReference > 0 &&
                    binding.TextFormat.Length > 0);
            case "HasSecretValues":
                return PushBooleanGetter(state, usage, false);
            case "IsEnabled":
                return PushBooleanGetter(state, usage, binding!.Enabled);
            case "Disable":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                binding!.Enabled = false;
                binding.UpdateElapsed = 0;
                return 0;
            case "Enable":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                SetEnabled(runtime, binding!, true);
                return 0;
            case "GetDuration":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                PushReference(state, binding!.DurationReference);
                return 1;
            case "GetExpiredText":
                return PushOptionalStringGetter(state, usage, binding!.ExpiredText);
            case "GetZeroDurationText":
                return PushOptionalStringGetter(state, usage, binding!.ZeroDurationText);
            case "GetFontString":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                runtime.PushObject(
                    binding!.FontStringId is { } fontStringId
                        ? runtime.Ui.Find(fontStringId)
                        : null);
                return 1;
            case "GetFormattedText":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                lua_pushstring(state, Format(runtime, binding!));
                return 1;
            case "GetTimeModifier":
                return PushIntegerGetter(state, usage, (byte)binding!.TimeModifier);
            case "GetUpdateInterval":
                return PushNumberGetter(state, usage, binding!.UpdateInterval);
            case "SetDuration":
                if (lua_gettop(state) != 2 ||
                    !WowDurationApi.TryRead(state, 2, out _))
                {
                    return luaL_error(state, usage);
                }
                ReplaceReference(runtime, state, ref binding!.DurationReference, 2);
                UpdateFontString(runtime, binding);
                return 0;
            case "SetEnabled":
                if (!TryReadRequiredBoolean(state, 2, out var enabled))
                    return luaL_error(state, usage);
                SetEnabled(runtime, binding!, enabled);
                return 0;
            case "SetExpiredText":
                if (!TryReadOptionalString(state, 2, out var expiredText))
                    return luaL_error(state, usage);
                binding!.ExpiredText = expiredText;
                UpdateFontString(runtime, binding);
                return 0;
            case "SetZeroDurationText":
                if (!TryReadOptionalString(state, 2, out var zeroText))
                    return luaL_error(state, usage);
                binding!.ZeroDurationText = zeroText;
                UpdateFontString(runtime, binding);
                return 0;
            case "SetFontString":
            {
                var fontString = lua_gettop(state) == 2
                    ? LuaBindings.GetObject(runtime, 2)
                    : null;
                if (fontString is null ||
                    !fontString.ObjectType.Equals(
                        "FontString",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return luaL_error(state, usage);
                }
                binding!.FontStringId = fontString.Id;
                UpdateFontString(runtime, binding);
                return 0;
            }
            case "SetFormatter":
                if (lua_gettop(state) != 2 ||
                    !WowStringUtilApi.TryReadFormatter(state, 2, out var formatter))
                {
                    return luaL_error(state, usage);
                }
                binding!.TextFormat = "{}";
                binding.ReplaceComponents(
                    runtime,
                    [new WowDurationTextFormatComponent(
                        DurationTextBindingProperty.RemainingDuration,
                        new WowDurationFormatterReference(
                            formatter!,
                            LuaRuntime.CaptureValue(state, 2))) ]);
                UpdateFontString(runtime, binding);
                return 0;
            case "SetTextFormat":
                if (!TryReadTextFormat(
                        runtime,
                        state,
                        usage,
                        out var textFormat,
                        out var components))
                {
                    return luaL_error(state, usage);
                }
                var placeholderCount = CountPlaceholders(textFormat);
                if (placeholderCount != components.Count)
                {
                    foreach (var component in components)
                        component.Formatter.Release(runtime);
                    return luaL_error(
                        state,
                        $"expected {placeholderCount} format components for {components.Count} placeholders");
                }
                binding!.TextFormat = textFormat;
                binding.ReplaceComponents(runtime, components);
                UpdateFontString(runtime, binding);
                return 0;
            case "SetTimeModifier":
                if (!TryReadRequiredEnum(state, 2, 1, out var modifier))
                    return luaL_error(state, usage);
                binding!.TimeModifier = (DurationTimeModifier)modifier;
                UpdateFontString(runtime, binding);
                return 0;
            case "SetToDefaults":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                binding!.Reset(runtime);
                return 0;
            case "SetUpdateInterval":
                if (!TryReadRequiredFloat(state, 2, out var interval))
                    return luaL_error(state, usage);
                binding!.UpdateInterval = Math.Max(interval, 0);
                binding.UpdateElapsed = 0;
                return 0;
            case "UpdateFontString":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                UpdateFontString(runtime, binding!);
                return 0;
            default:
                return 0;
        }
    }

    private static void SetEnabled(
        LuaRuntime runtime,
        WowDurationTextBindingState binding,
        bool enabled)
    {
        if (binding.Enabled == enabled)
            return;
        binding.Enabled = enabled;
        binding.UpdateElapsed = 0;
        if (enabled)
            UpdateFontString(runtime, binding);
    }

    private static void UpdateFontString(
        LuaRuntime runtime,
        WowDurationTextBindingState binding)
    {
        if (binding.FontStringId is not { } id ||
            binding.DurationReference <= 0 ||
            binding.TextFormat.Length == 0 ||
            runtime.Ui.Find(id) is not { } fontString)
        {
            return;
        }
        LuaBindings.SetObjectText(runtime, fontString, Format(runtime, binding));
        runtime.Ui.InvalidateLayout();
    }

    private static string Format(
        LuaRuntime runtime,
        WowDurationTextBindingState binding)
    {
        if (!TryReadDurationMetrics(runtime, binding, out var metrics) || metrics.IsZero)
            return binding.ZeroDurationText ?? string.Empty;
        if (metrics.HasExpired)
            return binding.ExpiredText ?? string.Empty;
        if (binding.TextFormat.Length == 0)
            return string.Empty;

        var output = binding.TextFormat;
        var searchStart = 0;
        foreach (var component in binding.Components)
        {
            var placeholder = output.IndexOf("{}", searchStart, StringComparison.Ordinal);
            if (placeholder < 0)
                break;
            var number = component.Property switch
            {
                DurationTextBindingProperty.RemainingDuration => metrics.RemainingDuration,
                DurationTextBindingProperty.RemainingPercent => metrics.RemainingPercent * 100,
                DurationTextBindingProperty.ElapsedDuration => metrics.ElapsedDuration,
                DurationTextBindingProperty.ElapsedPercent => metrics.ElapsedPercent * 100,
                DurationTextBindingProperty.TotalDuration => metrics.TotalDuration,
                DurationTextBindingProperty.StartTime => metrics.StartTime,
                DurationTextBindingProperty.EndTime => metrics.EndTime,
                _ => 0
            };
            var text = component.Formatter.State is { } formatter
                ? WowStringUtilApi.Format(runtime.State, formatter, number)
                : number.ToString("G14", CultureInfo.InvariantCulture);
            output = output.Remove(placeholder, 2).Insert(placeholder, text);
            searchStart = placeholder + text.Length;
        }
        return output;
    }

    private static bool TryReadDurationMetrics(
        LuaRuntime runtime,
        WowDurationTextBindingState binding,
        out WowDurationMetrics metrics)
    {
        metrics = default;
        if (binding.DurationReference <= 0)
            return false;
        lua_rawgeti(runtime.State, LUA_REGISTRYINDEX, binding.DurationReference);
        var valid = WowDurationApi.TryReadMetrics(
            runtime.State,
            -1,
            binding.TimeModifier == DurationTimeModifier.BaseTime,
            out metrics);
        lua_pop(runtime.State, 1);
        return valid;
    }

    private static bool TryReadTextFormat(
        LuaRuntime runtime,
        lua_State state,
        string usage,
        out string format,
        out List<WowDurationTextFormatComponent> components)
    {
        format = string.Empty;
        components = [];
        if (lua_gettop(state) != 3 ||
            lua_isstring(state, 2) == 0 ||
            lua_istable(state, 3) == 0)
        {
            return false;
        }
        format = lua_tostring(state, 2) ?? string.Empty;
        var componentTable = AbsoluteIndex(state, 3);
        var count = checked((int)lua_objlen(state, componentTable));
        for (var index = 1; index <= count; index++)
        {
            lua_rawgeti(state, componentTable, index);
            if (lua_istable(state, -1) == 0)
            {
                lua_pop(state, 1);
                ReleaseComponents(runtime, components);
                return false;
            }
            var item = AbsoluteIndex(state, -1);
            lua_getfield(state, item, "property");
            var validProperty = TryReadRequiredEnumValue(state, -1, 6, out var property);
            lua_pop(state, 1);
            if (!validProperty)
            {
                lua_pop(state, 1);
                ReleaseComponents(runtime, components);
                return false;
            }

            lua_getfield(state, item, "formatter");
            WowDurationFormatterReference formatter = new();
            if (lua_isnil(state, -1) == 0)
            {
                if (!WowStringUtilApi.TryReadFormatter(state, -1, out var formatterState))
                {
                    lua_pop(state, 2);
                    ReleaseComponents(runtime, components);
                    return false;
                }
                formatter.Set(runtime, formatterState!, LuaRuntime.CaptureValue(state, -1));
            }
            lua_pop(state, 1);
            components.Add(new WowDurationTextFormatComponent(
                (DurationTextBindingProperty)property,
                formatter));
            lua_pop(state, 1);
        }
        return true;
    }

    private static void ReleaseComponents(
        LuaRuntime runtime,
        IEnumerable<WowDurationTextFormatComponent> components)
    {
        foreach (var component in components)
            component.Formatter.Release(runtime);
    }

    private static int CountPlaceholders(string value)
    {
        var count = 0;
        for (var index = 0;
             (index = value.IndexOf("{}", index, StringComparison.Ordinal)) >= 0;
             index += 2)
        {
            count++;
        }
        return count;
    }

    private static bool TryRead(
        lua_State state,
        int index,
        out WowDurationTextBindingState? binding)
    {
        binding = null;
        unsafe
        {
            if (!TryGetStorage(state, index, out var storage) ||
                storage->StateHandle == IntPtr.Zero)
            {
                return false;
            }
            binding = GCHandle.FromIntPtr(storage->StateHandle).Target
                as WowDurationTextBindingState;
            return binding is not null;
        }
    }

    private static int GarbageCollect(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            if (LuaBindings.TryGetRuntime(state, out var runtime))
            {
                runtime!.ReleaseReference(storage->PropertyTableReference);
                if (storage->StateHandle != IntPtr.Zero &&
                    GCHandle.FromIntPtr(storage->StateHandle).Target is
                        WowDurationTextBindingState binding)
                {
                    binding.ReleaseReferences(runtime);
                    runtime.DurationTextBindings.Remove(binding);
                }
            }
            storage->PropertyTableReference = 0;
            if (storage->StateHandle != IntPtr.Zero)
            {
                var handle = GCHandle.FromIntPtr(storage->StateHandle);
                if (handle.IsAllocated)
                    handle.Free();
                storage->StateHandle = IntPtr.Zero;
            }
            storage->Magic = 0;
            return 0;
        }
    }

    private static int Index(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
            {
                lua_pushnil(state);
                return 1;
            }
            luaL_getmetatable(state, MetatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            if (lua_isnil(state, -1) == 0)
            {
                lua_remove(state, -2);
                return 1;
            }
            lua_pop(state, 2);
            lua_rawgeti(state, LUA_REGISTRYINDEX, storage->PropertyTableReference);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            lua_remove(state, -2);
            return 1;
        }
    }

    private static int NewIndex(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            luaL_getmetatable(state, MetatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            var readOnly = lua_isnil(state, -1) == 0;
            lua_pop(state, 2);
            if (readOnly)
            {
                return luaL_error(
                    state,
                    $"Attempted to assign to read-only key {LuaKeyText(state, 2)}");
            }
            lua_rawgeti(state, LUA_REGISTRYINDEX, storage->PropertyTableReference);
            lua_pushvalue(state, 2);
            lua_pushvalue(state, 3);
            lua_rawset(state, -3);
            lua_pop(state, 1);
            return 0;
        }
    }

    private static int Equal(lua_State state)
    {
        unsafe
        {
            var equal = TryGetStorage(state, 1, out var left) &&
                        TryGetStorage(state, 2, out var right) &&
                        left == right;
            lua_pushboolean(state, equal ? 1 : 0);
            return 1;
        }
    }

    private static int ToStringValue(lua_State state)
    {
        lua_pushstring(
            state,
            $"DurationTextBinding: 0x{lua_topointer(state, 1).ToUInt64():X}");
        return 1;
    }

    private static int Dump(lua_State state)
    {
        lua_pushnil(state);
        return 1;
    }

    private static unsafe bool TryGetStorage(
        lua_State state,
        int index,
        out BindingStorage* storage)
    {
        storage = null;
        if (lua_type(state, index) != LUA_TUSERDATA ||
            lua_getmetatable(state, index) == 0)
        {
            return false;
        }
        luaL_getmetatable(state, MetatableName);
        var matches = lua_rawequal(state, -1, -2) != 0;
        lua_pop(state, 2);
        if (!matches)
            return false;
        storage = (BindingStorage*)lua_touserdata(state, index);
        return storage is not null && storage->Magic == StorageMagic;
    }

    private static int PushBooleanGetter(lua_State state, string usage, bool value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushIntegerGetter(lua_State state, string usage, int value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        lua_pushinteger(state, value);
        return 1;
    }

    private static int PushNumberGetter(lua_State state, string usage, double value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        lua_pushnumber(state, value);
        return 1;
    }

    private static int PushOptionalStringGetter(
        lua_State state,
        string usage,
        string? value)
    {
        if (lua_gettop(state) != 1)
            return luaL_error(state, usage);
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        return 1;
    }

    private static void PushReference(lua_State state, int reference)
    {
        if (reference > 0)
            lua_rawgeti(state, LUA_REGISTRYINDEX, reference);
        else
            lua_pushnil(state);
    }

    private static void ReplaceReference(
        LuaRuntime runtime,
        lua_State state,
        ref int destination,
        int stackIndex)
    {
        runtime.ReleaseReference(destination);
        destination = LuaRuntime.CaptureValue(state, stackIndex);
    }

    private static bool TryReadRequiredBoolean(
        lua_State state,
        int index,
        out bool value)
    {
        value = false;
        if (lua_gettop(state) != index || lua_isnil(state, index) != 0)
            return false;
        value = lua_toboolean(state, index) != 0;
        return true;
    }

    private static bool TryReadOptionalString(
        lua_State state,
        int index,
        out string? value)
    {
        value = null;
        var top = lua_gettop(state);
        if (top < index || top == index && lua_isnil(state, index) != 0)
            return true;
        if (top != index || lua_isstring(state, index) == 0)
            return false;
        value = lua_tostring(state, index) ?? string.Empty;
        return true;
    }

    private static bool TryReadRequiredFloat(
        lua_State state,
        int index,
        out float value)
    {
        value = 0;
        if (lua_gettop(state) != index || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (double.IsNaN(number) || number is < -float.MaxValue or > float.MaxValue)
            return false;
        value = (float)number;
        return true;
    }

    private static bool TryReadRequiredEnum(
        lua_State state,
        int index,
        byte maximum,
        out byte value)
    {
        value = 0;
        return lua_gettop(state) == index &&
               TryReadRequiredEnumValue(state, index, maximum, out value);
    }

    private static bool TryReadRequiredEnumValue(
        lua_State state,
        int index,
        byte maximum,
        out byte value)
    {
        value = 0;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = unchecked((byte)(int)number);
        return value <= maximum;
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_createtable(state, 0, values.Length);
        foreach (var value in values)
        {
            lua_pushinteger(state, value.Value);
            lua_setfield(state, -2, value.Name);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int count,
        int minimum,
        int maximum)
    {
        lua_createtable(state, 0, 3);
        lua_pushinteger(state, count);
        lua_setfield(state, -2, "NumValues");
        lua_pushinteger(state, minimum);
        lua_setfield(state, -2, "MinValue");
        lua_pushinteger(state, maximum);
        lua_setfield(state, -2, "MaxValue");
        lua_setfield(state, -2, name);
    }

    private static string LuaKeyText(lua_State state, int index) =>
        lua_type(state, index) == LUA_TSTRING
            ? lua_tostring(state, index) ?? string.Empty
            : lua_typename(state, lua_type(state, index)) ?? "unknown";

    private static string Usage(string operation) => operation switch
    {
        "CanFormatText" => "Usage: local canFormatText = self:CanFormatText()",
        "CanUpdateFontString" => "Usage: local canUpdateText = self:CanUpdateFontString()",
        "Disable" => "Usage: self:Disable()",
        "Enable" => "Usage: self:Enable()",
        "GetDuration" => "Usage: local duration = self:GetDuration()",
        "GetExpiredText" => "Usage: local text = self:GetExpiredText()",
        "GetFontString" => "Usage: local fontString = self:GetFontString()",
        "GetFormattedText" => "Usage: local text = self:GetFormattedText()",
        "GetTimeModifier" => "Usage: local modifier = self:GetTimeModifier()",
        "GetUpdateInterval" => "Usage: local updateInterval = self:GetUpdateInterval()",
        "GetZeroDurationText" => "Usage: local text = self:GetZeroDurationText()",
        "HasSecretValues" => "Usage: local hasSecretValues = self:HasSecretValues()",
        "IsEnabled" => "Usage: local enabled = self:IsEnabled()",
        "SetDuration" => "Usage: self:SetDuration(duration)",
        "SetEnabled" => "Usage: self:SetEnabled(enabled)",
        "SetExpiredText" => "Usage: self:SetExpiredText([text])",
        "SetFontString" => "Usage: self:SetFontString(fontString)",
        "SetFormatter" => "Usage: self:SetFormatter(formatter)",
        "SetTextFormat" => "Usage: self:SetTextFormat(format, components)",
        "SetTimeModifier" => "Usage: self:SetTimeModifier(modifier)",
        "SetToDefaults" => "Usage: self:SetToDefaults()",
        "SetUpdateInterval" => "Usage: self:SetUpdateInterval(updateInterval)",
        "SetZeroDurationText" => "Usage: self:SetZeroDurationText([text])",
        _ => "Usage: self:UpdateFontString()"
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct BindingStorage
    {
        public int Magic;
        public int PropertyTableReference;
        public IntPtr StateHandle;
    }
}
