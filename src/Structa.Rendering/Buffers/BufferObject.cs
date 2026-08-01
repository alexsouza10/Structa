using Silk.NET.OpenGL;

namespace Structa.Rendering.Buffers;

public sealed class BufferObject<T> : IDisposable where T : unmanaged
{
    private readonly GL _gl;
    private readonly BufferTargetARB _target;

    public uint Handle { get; }

    public BufferObject(GL gl, ReadOnlySpan<T> data, BufferTargetARB target)
    {
        _gl = gl;
        _target = target;

        Handle = _gl.GenBuffer();
        Bind();
        _gl.BufferData(target, data, BufferUsageARB.StaticDraw);
    }

    public void Bind() => _gl.BindBuffer(_target, Handle);

    public void Dispose() => _gl.DeleteBuffer(Handle);
}
