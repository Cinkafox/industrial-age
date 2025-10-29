using System.Numerics;
using Robust.Shared.Graphics;

namespace Content.Client.SpriteStacking.TransformContext;

public interface ITransformContext
{
    public Vector2 Transform(Vector2 p1, float zlevel, IEye currentEye);
}