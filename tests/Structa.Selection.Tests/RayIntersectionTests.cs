using System.Numerics;
using Structa.Selection;

namespace Structa.Selection.Tests;

public class RayIntersectionTests
{
    [Fact]
    public void TryIntersectPlane_ReturnsPointWherePerpendicularRayCrossesPlane()
    {
        var ray = new Ray(new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, -1f));

        var hit = RayIntersection.TryIntersectPlane(ray, Vector3.Zero, Vector3.UnitZ, out var point);

        Assert.True(hit);
        Assert.Equal(Vector3.Zero, point);
    }

    [Fact]
    public void TryIntersectPlane_ReturnsFalseWhenRayIsParallelToPlane()
    {
        var ray = new Ray(new Vector3(0f, 0f, 5f), Vector3.UnitX);

        var hit = RayIntersection.TryIntersectPlane(ray, Vector3.Zero, Vector3.UnitZ, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryIntersectPlane_ReturnsFalseWhenPlaneIsBehindTheRayOrigin()
    {
        var ray = new Ray(new Vector3(0f, 0f, -5f), new Vector3(0f, 0f, -1f));

        var hit = RayIntersection.TryIntersectPlane(ray, Vector3.Zero, Vector3.UnitZ, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryClosestPointOnLine_FindsPointWhereRayCrossesTheLine()
    {
        var ray = new Ray(new Vector3(5f, 0f, 3f), new Vector3(-1f, 0f, 0f));

        var found = RayIntersection.TryClosestPointOnLine(ray, Vector3.Zero, Vector3.UnitZ, out var point);

        Assert.True(found);
        Assert.Equal(new Vector3(0f, 0f, 3f), point);
    }

    [Fact]
    public void TryClosestPointOnLine_ReturnsFalseWhenRayIsParallelToTheLine()
    {
        var ray = new Ray(new Vector3(5f, 0f, 0f), Vector3.UnitZ);

        var found = RayIntersection.TryClosestPointOnLine(ray, Vector3.Zero, Vector3.UnitZ, out _);

        Assert.False(found);
    }
}
