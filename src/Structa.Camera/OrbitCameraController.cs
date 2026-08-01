using System.Numerics;

namespace Structa.Camera;

/// <summary>
/// Navegação estilo SketchUp: órbita, pan e zoom em torno de um ponto-alvo, com movimento suave
/// (a câmera nunca salta direto para o novo estado — interpola até ele a cada frame).
///
/// A câmera é representada internamente em coordenadas esféricas (alvo, distância, yaw, pitch)
/// e traduzida para <see cref="Camera3D.Position"/>/<see cref="Camera3D.Target"/> a cada
/// <see cref="Update"/>. Os métodos Orbit/Pan/Zoom só ajustam o estado "desejado"; quem aplica
/// o movimento suave é o Update, chamado uma vez por frame pelo render loop.
/// </summary>
public sealed class OrbitCameraController
{
    private readonly Camera3D _camera;

    private readonly Vector3 _defaultTarget;
    private readonly float _defaultYaw;
    private readonly float _defaultPitch;
    private readonly float _defaultDistance;

    private Vector3 _target;
    private float _yaw;
    private float _pitch;
    private float _distance;

    private Vector3 _targetTarget;
    private float _targetYaw;
    private float _targetPitch;
    private float _targetDistance;

    public CameraNavigationSettings Settings { get; }

    public OrbitCameraController(Camera3D camera, CameraNavigationSettings? settings = null)
    {
        _camera = camera;
        Settings = settings ?? new CameraNavigationSettings();

        var offset = camera.Position - camera.Target;
        var distance = offset.Length();

        _defaultTarget = camera.Target;
        _defaultDistance = distance;
        _defaultYaw = MathF.Atan2(offset.Y, offset.X);
        _defaultPitch = MathF.Asin(Math.Clamp(offset.Z / MathF.Max(distance, 0.0001f), -1f, 1f));

        _target = _targetTarget = _defaultTarget;
        _yaw = _targetYaw = _defaultYaw;
        _pitch = _targetPitch = _defaultPitch;
        _distance = _targetDistance = _defaultDistance;

        ApplyToCamera();
    }

    /// <summary>Rotaciona a câmera em torno do alvo. Deltas em pixels de tela.</summary>
    public void Orbit(float deltaXPixels, float deltaYPixels)
    {
        var minPitch = Settings.MinPitchDegrees * MathF.PI / 180f;
        var maxPitch = Settings.MaxPitchDegrees * MathF.PI / 180f;

        _targetYaw -= deltaXPixels * Settings.OrbitSensitivity;
        _targetPitch = Math.Clamp(_targetPitch + deltaYPixels * Settings.OrbitSensitivity, minPitch, maxPitch);
    }

    /// <summary>Desloca o alvo no plano da tela. Deltas em pixels de tela.</summary>
    public void Pan(float deltaXPixels, float deltaYPixels)
    {
        var (right, up) = GetScreenBasis();
        var scale = _targetDistance * Settings.PanSensitivity;

        _targetTarget += (-right * deltaXPixels + up * deltaYPixels) * scale;
    }

    /// <summary>Aproxima/afasta a câmera do alvo. <paramref name="notches"/> positivo aproxima.</summary>
    public void Zoom(float notches)
    {
        var factor = MathF.Pow(1f - Settings.ZoomSensitivity, notches);
        _targetDistance = Math.Clamp(_targetDistance * factor, Settings.MinDistance, Settings.MaxDistance);
    }

    /// <summary>Restaura o enquadramento inicial da câmera.</summary>
    public void ResetView()
    {
        _targetTarget = _defaultTarget;
        _targetYaw = _defaultYaw;
        _targetPitch = _defaultPitch;
        _targetDistance = _defaultDistance;
    }

    /// <summary>Avança a suavização e atualiza a <see cref="Camera3D"/>. Chamar uma vez por frame.</summary>
    public void Update(double deltaSeconds)
    {
        var t = 1f - MathF.Exp(-Settings.SmoothingSpeed * (float)deltaSeconds);

        _target = Vector3.Lerp(_target, _targetTarget, t);
        _yaw = float.Lerp(_yaw, _targetYaw, t);
        _pitch = float.Lerp(_pitch, _targetPitch, t);
        _distance = float.Lerp(_distance, _targetDistance, t);

        ApplyToCamera();
    }

    private void ApplyToCamera()
    {
        var offset = new Vector3(
            _distance * MathF.Cos(_pitch) * MathF.Cos(_yaw),
            _distance * MathF.Cos(_pitch) * MathF.Sin(_yaw),
            _distance * MathF.Sin(_pitch));

        _camera.Target = _target;
        _camera.Position = _target + offset;
    }

    private (Vector3 Right, Vector3 Up) GetScreenBasis()
    {
        var forward = Vector3.Normalize(_camera.Target - _camera.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitZ));
        var up = Vector3.Cross(right, forward);

        return (right, up);
    }
}
