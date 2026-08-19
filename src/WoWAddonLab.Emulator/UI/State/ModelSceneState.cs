using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class ModelSceneState
{
    private const float NativeDirectionNormalizationThresholdSquared =
        0.00000023841858f;

    private Vector3? _explicitForward;
    private Vector3? _explicitRight;
    private Vector3? _explicitUp;
    private Vector3 _lightDirection = Vector3.UnitY;

    public Vector3 CameraPosition { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Roll { get; set; }
    public float FieldOfView { get; set; } = MathF.PI * 0.3f;
    public float NearClip { get; set; } = 0.2f;
    public float FarClip { get; set; } = 100;
    public Vector3 AmbientLight { get; set; } = new(0.7f);
    public Vector3 AmbientLightSecondary { get; set; } = new(0.7f);
    public Vector3 AmbientLightTertiary { get; set; } = new(0.7f);
    public Vector3 DiffuseLight { get; set; } = new(0.8f, 0.8f, 0.64f);
    public Vector3 LightDirection
    {
        get => _lightDirection;
        set
        {
            var lengthSquared = value.LengthSquared();
            _lightDirection = lengthSquared > NativeDirectionNormalizationThresholdSquared
                ? value / MathF.Sqrt(lengthSquared)
                : value;
        }
    }
    public Vector3 LightPosition { get; set; }
    public int LightType { get; set; }
    public bool LightVisible { get; set; } = true;
    public Vector4 FogColor { get; set; } = Vector4.One;
    public float FogNear { get; set; } = 10_000_000;
    public float FogFar { get; set; } = 100_000_000;
    public bool FogEnabled { get; set; }
    public bool AllowOverlappedModels { get; set; }
    public UiInsets ViewInsets { get; set; }
    public Vector2 ViewTranslation { get; set; }

    public Vector3 Forward
    {
        get
        {
            if (_explicitForward is { } explicitForward)
                return explicitForward;
            var cosPitch = MathF.Cos(Pitch);
            return Vector3.Normalize(new Vector3(
                -MathF.Sin(Yaw) * cosPitch,
                MathF.Cos(Yaw) * cosPitch,
                -MathF.Sin(Pitch)));
        }
    }

    public Vector3 Right
    {
        get
        {
            if (_explicitRight is { } explicitRight)
                return explicitRight;
            var right = Vector3.Normalize(new Vector3(MathF.Cos(Yaw), MathF.Sin(Yaw), 0));
            if (MathF.Abs(Roll) < 0.000001f)
                return right;
            var up = Vector3.Normalize(Vector3.Cross(right, Forward));
            return Vector3.Normalize(
                right * MathF.Cos(Roll) -
                up * MathF.Sin(Roll));
        }
    }

    public Vector3 Up
    {
        get
        {
            if (_explicitUp is { } explicitUp)
                return explicitUp;
            var up = Vector3.Cross(Right, Forward);
            return up.LengthSquared() < 0.000001f ? Vector3.UnitZ : Vector3.Normalize(up);
        }
    }

    public void SetOrientationByYawPitchRoll(float yaw, float pitch, float roll)
    {
        Yaw = yaw;
        Pitch = pitch;
        Roll = roll;
        _explicitForward = null;
        _explicitRight = null;
        _explicitUp = null;
    }

    public bool TrySetOrientationByAxisVectors(
        Vector3 forward,
        Vector3 right,
        Vector3 up)
    {
        const float unitLengthTolerance = 0.001f;
        const float orthogonalTolerance = 0.00000023841858f;
        if (MathF.Abs(forward.LengthSquared() - 1) >= unitLengthTolerance ||
            MathF.Abs(right.LengthSquared() - 1) >= unitLengthTolerance ||
            MathF.Abs(up.LengthSquared() - 1) >= unitLengthTolerance ||
            MathF.Abs(Vector3.Dot(forward, right)) >= orthogonalTolerance ||
            MathF.Abs(Vector3.Dot(forward, up)) >= orthogonalTolerance ||
            MathF.Abs(Vector3.Dot(right, up)) >= orthogonalTolerance)
        {
            return false;
        }

        _explicitForward = forward;
        _explicitRight = right;
        _explicitUp = up;
        return true;
    }

    public (float X, float Y, float Depth) Project(
        Vector3 point,
        float width,
        float height,
        float modelCoordinateScale = 1)
    {
        var delta = point - CameraPosition;
        var forwardDepth = Vector3.Dot(delta, Forward);
        var right = Vector3.Dot(delta, Right);
        var up = Vector3.Dot(delta, Up);
        var aspect = width / height;
        var verticalFieldOfView = FieldOfView / MathF.Sqrt(aspect * aspect + 1);
        var tanHalf = MathF.Tan(verticalFieldOfView / 2);

        var x = width / 2 + (right / (forwardDepth * tanHalf * aspect)) * width / 2;
        var y = height / 2 + (up / (forwardDepth * tanHalf)) * height / 2;

        var clipSpaceW = forwardDepth * modelCoordinateScale;
        var reverseDepth = NearClip == FarClip
            ? 0
            : (FarClip - clipSpaceW) / (FarClip - NearClip);

        return (x, y, reverseDepth);
    }
}
