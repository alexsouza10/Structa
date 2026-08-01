namespace Structa.Rendering.Scene;

internal static class GridShaderSource
{
    // Avalonia usa ANGLE no Windows (contexto OpenGL ES, não desktop GL) — os shaders
    // precisam de sintaxe GLSL ES ("#version 300 es" + qualificadores de precisão).
    public const string Vertex = """
        #version 300 es
        layout(location = 0) in vec3 aPosition;

        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec3 vWorldPos;

        void main()
        {
            vWorldPos = aPosition;
            gl_Position = uProjection * uView * vec4(aPosition, 1.0);
        }
        """;

    // Técnica clássica de grid procedural (fract + fwidth para anti-aliasing por derivadas de tela),
    // com fade radial a partir da câmera para simular um plano infinito sem borda visível.
    public const string Fragment = """
        #version 300 es
        precision highp float;

        in vec3 vWorldPos;
        out vec4 FragColor;

        uniform vec3 uCameraPosition;
        uniform vec3 uMinorLineColor;
        uniform vec3 uMajorLineColor;
        uniform float uFadeDistance;

        float gridFactor(vec2 coord)
        {
            vec2 derivative = fwidth(coord);
            vec2 grid = abs(fract(coord - 0.5) - 0.5) / max(derivative, vec2(0.0001));
            float line = min(grid.x, grid.y);
            return 1.0 - min(line, 1.0);
        }

        void main()
        {
            float minor = gridFactor(vWorldPos.xy);
            float major = gridFactor(vWorldPos.xy / 10.0);

            float dist = distance(uCameraPosition.xy, vWorldPos.xy);
            float fade = 1.0 - smoothstep(0.0, uFadeDistance, dist);

            vec3 color = mix(uMinorLineColor, uMajorLineColor, major);
            float alpha = max(minor, major) * fade;

            if (alpha < 0.02)
                discard;

            FragColor = vec4(color, alpha);
        }
        """;
}
