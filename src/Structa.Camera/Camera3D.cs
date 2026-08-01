using System.Numerics;

namespace Structa.Camera;

/// <summary>
/// Câmera 3D look-at (convenção Z para cima, como no SketchUp). Expõe apenas posição/orientação
/// e as matrizes derivadas — quem manipula esse estado (órbita/pan/zoom) é o
/// <see cref="OrbitCameraController"/>.
/// </summary>
public sealed class Camera3D
{
    public Vector3 Position { get; set; } = new(9f, -9f, 7f);

    public Vector3 Target { get; set; } = Vector3.Zero;

    public Vector3 Up { get; set; } = new(0f, 0f, 1f);

    public float FieldOfViewRadians { get; set; } = MathF.PI / 4f;

    public float NearPlane { get; set; } = 0.05f;

    public float FarPlane { get; set; } = 10000f;

    public float AspectRatio { get; set; } = 16f / 9f;

    public Matrix4x4 GetViewMatrix() => Matrix4x4.CreateLookAt(Position, Target, Up);

    public Matrix4x4 GetProjectionMatrix() =>
        Matrix4x4.CreatePerspectiveFieldOfView(FieldOfViewRadians, AspectRatio, NearPlane, FarPlane);
}
