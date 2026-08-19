using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiModelRenderCameraState(
    Vector3 Forward,
    Vector3 Right,
    Vector3 Up,
    Vector3 Position,
    float DiagonalFieldOfView,
    float NearClip,
    float FarClip,
    float Scale)
{
    public Matrix4x4 CreateViewOrientationMatrix() =>
        new(
            -Right.X, Up.X, Forward.X, 0,
            -Right.Y, Up.Y, Forward.Y, 0,
            -Right.Z, Up.Z, Forward.Z, 0,
            0, 0, 0, 1);

    public Matrix4x4 CreateViewMatrix()
    {
        var cameraPosition = Position * Scale;
        return Matrix4x4.CreateTranslation(-cameraPosition) *
            CreateViewOrientationMatrix();
    }

    public Matrix4x4 CreateProjectionMatrix(float aspectRatio)
    {
        var fieldOfView =
            DiagonalFieldOfView /
            MathF.Sqrt(aspectRatio * aspectRatio + 1);
        var halfHeightAtNear = MathF.Tan(fieldOfView * .5f) * NearClip;
        var depthRange = NearClip - FarClip;
        return new Matrix4x4(
            NearClip / (halfHeightAtNear * aspectRatio), 0, 0, 0,
            0, NearClip / halfHeightAtNear, 0, 0,
            0, 0, NearClip / depthRange, 1,
            0, 0, -NearClip * FarClip / depthRange, 0);
    }

    public Matrix4x4 CreateModelViewProjectionMatrix(
        Matrix4x4 model,
        float aspectRatio) =>
        model * CreateViewMatrix() * CreateProjectionMatrix(aspectRatio);
}
