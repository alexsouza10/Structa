namespace Structa.Rendering.Scene;

internal static class AxesShaderSource
{
    public const string Vertex = """
        #version 300 es
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aColor;

        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec3 vColor;

        void main()
        {
            vColor = aColor;
            gl_Position = uProjection * uView * vec4(aPosition, 1.0);
        }
        """;

    public const string Fragment = """
        #version 300 es
        precision mediump float;

        in vec3 vColor;
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(vColor, 1.0);
        }
        """;
}
