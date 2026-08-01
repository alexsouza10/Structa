using System.Numerics;

namespace Structa.Selection;

/// <summary>Raio 3D usado para picking: um ponto de origem e uma direção normalizada.</summary>
public readonly record struct Ray(Vector3 Origin, Vector3 Direction);
