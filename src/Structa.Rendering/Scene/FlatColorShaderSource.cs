namespace Structa.Rendering.Scene;

/// <summary>Shader sem iluminação (cor sólida via uniform), usado para wireframe, vértices e highlight.</summary>
internal static class FlatColorShaderSource
{
    public const string Vertex = """
        #version 300 es
        layout(location = 0) in vec3 aPosition;

        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform float uPointSize;

        void main()
        {
            gl_Position = uProjection * uView * vec4(aPosition, 1.0);
            gl_PointSize = uPointSize;
        }
        """;

    public const string Fragment = """
        #version 300 es
        precision mediump float;

        out vec4 FragColor;
        uniform vec3 uColor;

        void main()
        {
            FragColor = vec4(uColor, 1.0);
        }
        """;
}
