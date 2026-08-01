namespace Structa.Camera;

/// <summary>
/// Parâmetros configuráveis de navegação da câmera (sensibilidade, suavização e limites).
/// </summary>
public sealed class CameraNavigationSettings
{
    /// <summary>Radianos de rotação por pixel arrastado durante a órbita.</summary>
    public float OrbitSensitivity { get; set; } = 0.008f;

    /// <summary>Fração da distância à câmera movida por pixel arrastado durante o pan.</summary>
    public float PanSensitivity { get; set; } = 0.0025f;

    /// <summary>Fração da distância aplicada por "notch" da roda do mouse ao dar zoom.</summary>
    public float ZoomSensitivity { get; set; } = 0.12f;

    /// <summary>
    /// Velocidade da suavização (maior = resposta mais rápida/seca, menor = movimento mais fluido).
    /// </summary>
    public float SmoothingSpeed { get; set; } = 12f;

    public float MinDistance { get; set; } = 0.5f;

    public float MaxDistance { get; set; } = 5000f;

    public float MinPitchDegrees { get; set; } = -89f;

    public float MaxPitchDegrees { get; set; } = 89f;
}
