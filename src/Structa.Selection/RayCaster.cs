using System.Numerics;

namespace Structa.Selection;

/// <summary>
/// Conversões entre coordenadas de tela (pixels, origem no canto superior esquerdo) e o espaço 3D
/// da cena, usando as mesmas matrizes view/projection do <c>Camera3D</c>. Não depende do módulo
/// Camera — recebe as matrizes prontas, do mesmo jeito que o <c>RenderEngine</c>.
/// </summary>
public static class RayCaster
{
    /// <summary>Constrói o raio que parte da câmera e passa pelo pixel de tela informado.</summary>
    public static Ray ScreenPointToRay(
        Vector2 screenPoint,
        Vector2 viewportSize,
        Vector3 cameraPosition,
        Matrix4x4 view,
        Matrix4x4 projection)
    {
        var ndcX = (2f * screenPoint.X / viewportSize.X) - 1f;
        var ndcY = 1f - (2f * screenPoint.Y / viewportSize.Y);

        var viewProjection = view * projection;
        Matrix4x4.Invert(viewProjection, out var inverse);

        var farClip = new Vector4(ndcX, ndcY, 1f, 1f);
        var farWorld4 = Vector4.Transform(farClip, inverse);
        var farWorld = new Vector3(farWorld4.X, farWorld4.Y, farWorld4.Z) / farWorld4.W;

        return new Ray(cameraPosition, Vector3.Normalize(farWorld - cameraPosition));
    }

    /// <summary>Projeta um ponto do mundo para coordenadas de tela (pixels). Retorna false se estiver atrás da câmera.</summary>
    public static bool TryWorldToScreen(
        Vector3 worldPoint,
        Vector2 viewportSize,
        Matrix4x4 view,
        Matrix4x4 projection,
        out Vector2 screenPoint)
    {
        var clip = Vector4.Transform(new Vector4(worldPoint, 1f), view * projection);

        if (clip.W <= 0f)
        {
            screenPoint = default;
            return false;
        }

        var ndc = new Vector2(clip.X / clip.W, clip.Y / clip.W);

        screenPoint = new Vector2(
            (ndc.X + 1f) / 2f * viewportSize.X,
            (1f - ndc.Y) / 2f * viewportSize.Y);

        return true;
    }
}
