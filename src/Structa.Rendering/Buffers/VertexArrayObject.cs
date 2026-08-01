using Silk.NET.OpenGL;

namespace Structa.Rendering.Buffers;

public sealed class VertexArrayObject : IDisposable
{
    private readonly GL _gl;

    public uint Handle { get; }

    public VertexArrayObject(GL gl)
    {
        _gl = gl;
        Handle = _gl.GenVertexArray();
    }

    public void Bind() => _gl.BindVertexArray(Handle);

    public void SetVertexAttribute(uint index, int componentCount, uint strideBytes, int offsetBytes)
    {
        _gl.EnableVertexAttribArray(index);
        _gl.VertexAttribPointer(index, componentCount, VertexAttribPointerType.Float, false, strideBytes, (IntPtr)offsetBytes);
    }

    public void Dispose() => _gl.DeleteVertexArray(Handle);
}
