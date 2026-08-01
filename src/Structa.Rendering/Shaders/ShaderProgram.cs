using System.Numerics;
using Silk.NET.OpenGL;
using Structa.Rendering.Gl;

namespace Structa.Rendering.Shaders;

public sealed class ShaderProgram : IDisposable
{
    private readonly GL _gl;

    public uint Handle { get; }

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;

        var vertexShader = Compile(ShaderType.VertexShader, vertexSource);
        var fragmentShader = Compile(ShaderType.FragmentShader, fragmentSource);

        Handle = _gl.CreateProgram();
        _gl.AttachShader(Handle, vertexShader);
        _gl.AttachShader(Handle, fragmentShader);
        _gl.LinkProgram(Handle);

        if (_gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus) == 0)
        {
            var info = _gl.GetProgramInfoLog(Handle);
            throw new InvalidOperationException($"Falha ao linkar shader program: {info}");
        }

        _gl.DetachShader(Handle, vertexShader);
        _gl.DetachShader(Handle, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
    }

    public void Use() => _gl.UseProgram(Handle);

    public void SetMatrix4(string name, in Matrix4x4 matrix) => _gl.SetMatrix4(GetUniformLocation(name), matrix);

    public void SetVector3(string name, Vector3 value) => _gl.Uniform3(GetUniformLocation(name), value);

    public void SetFloat(string name, float value) => _gl.Uniform1(GetUniformLocation(name), value);

    public void Dispose() => _gl.DeleteProgram(Handle);

    private int GetUniformLocation(string name) => _gl.GetUniformLocation(Handle, name);

    private uint Compile(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        if (_gl.GetShader(shader, ShaderParameterName.CompileStatus) == 0)
        {
            var info = _gl.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"Falha ao compilar shader ({type}): {info}");
        }

        return shader;
    }
}
