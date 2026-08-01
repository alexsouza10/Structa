namespace Structa.Rendering.Scene;

/// <summary>Shader com sombreamento difuso simples (uma luz fixa), usado para as faces da malha.</summary>
internal static class MeshShaderSource
{
    public const string Vertex = """
        #version 300 es
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aNormal;

        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec3 vNormal;

        void main()
        {
            vNormal = aNormal;
            gl_Position = uProjection * uView * vec4(aPosition, 1.0);
        }
        """;

    public const string Fragment = """
        #version 300 es
        precision mediump float;

        in vec3 vNormal;
        out vec4 FragColor;

        uniform vec3 uColor;

        void main()
        {
            vec3 lightDir = normalize(vec3(0.4, -0.6, 0.7));
            float diffuse = max(dot(normalize(vNormal), lightDir), 0.0);
            float shade = 0.35 + 0.65 * diffuse;
            FragColor = vec4(uColor * shade, 1.0);
        }
        """;
}
