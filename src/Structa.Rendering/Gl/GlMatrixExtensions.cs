using System.Numerics;
using Silk.NET.OpenGL;

namespace Structa.Rendering.Gl;

internal static class GlMatrixExtensions
{
    /// <summary>
    /// Envia um <see cref="Matrix4x4"/> (armazenamento row-major, convenção row-vector do System.Numerics:
    /// v' = v * M) para um uniform mat4 GLSL (convenção column-vector: v' = M * v).
    ///
    /// Os 16 floats em ordem de campo (M11,M12,M13,M14, M21,...) já são exatamente os dados que o GLSL
    /// precisa quando lidos como column-major: cada "coluna" do array vira uma LINHA da matriz M do
    /// System.Numerics, o que produz em column-vector a mesma transformação que M produz em row-vector.
    /// OpenGL ES não permite transpose=true em glUniformMatrix4fv, então usamos sempre transpose=false —
    /// os dados já estão na ordem certa, nenhuma transposição manual é necessária.
    /// </summary>
    public static void SetMatrix4(this GL gl, int location, in Matrix4x4 m)
    {
        Span<float> data = [m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44];

        gl.UniformMatrix4(location, false, data);
    }
}
