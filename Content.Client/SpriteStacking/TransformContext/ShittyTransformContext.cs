using System.Numerics;
using Robust.Shared.Graphics;

namespace Content.Client.SpriteStacking.TransformContext;

public sealed class ShittyTransformContext : ITransformContext
{
    public Vector2 Transform(Vector2 p1, float zlevel, IEye currentEye)
    {
        return ApplyPerspectiveTransform(p1, zlevel,
            currentEye.Position.Position + currentEye.Scale / 2,
            0f,
            0.025f);
    }

    private static Vector2 ApplyPerspectiveTransform(Vector2 vector, float zLevel, Vector2 center, float perspectiveX,
        float perspectiveY)
    {
        var translated = vector - center;

        var w = 1 + perspectiveX * translated.X + perspectiveY * translated.Y;
        var x = translated.X / w;
        var y = translated.Y / w;
        var z = zLevel / w;

        return new Vector2(x, y * 0.55f + z * 0.032f) + center;
    }
}