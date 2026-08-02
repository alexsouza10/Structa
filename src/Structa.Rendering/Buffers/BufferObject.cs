using Silk.NET.OpenGL;

namespace Structa.Rendering.Buffers;

public sealed class BufferObject<T> : IDisposable where T : unmanaged
{
    private readonly GL _gl;
    private readonly BufferTargetARB _target;

    public uint Handle { get; }

    public BufferObject(GL gl, ReadOnlySpan<T> data, BufferTargetARB target, BufferUsageARB usage = BufferUsageARB.StaticDraw)
    {
        _gl = gl;
        _target = target;

        Handle = _gl.GenBuffer();
        Bind();
        _gl.BufferData(target, data, usage);
    }

    public void Bind() => _gl.BindBuffer(_target, Handle);

    /// <summary>Substitui o conteúdo do buffer. Para buffers criados com <see cref="BufferUsageARB.DynamicDraw"/>
    /// (ex.: preview de ferramentas, atualizado a cada frame).</summary>
    public void SetData(ReadOnlySpan<T> data)
    {
        Bind();
        _gl.BufferData(_target, data, BufferUsageARB.DynamicDraw);
    }

    public void Dispose() => _gl.DeleteBuffer(Handle);
}
