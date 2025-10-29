using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;

namespace Content.Client.SpriteStacking.Overlays;

public sealed class EntTransformOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public EntTransformOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _configurationManager.GetCVar<bool>("stacksprite.enabled");
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!args.MapUid.IsValid()) return;
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);
        var eye = args.Viewport.Eye!;

        var spriteQuery = _entityManager.EntityQueryEnumerator<SpriteComponent, TransformComponent>();
        while (spriteQuery.MoveNext(out var uid, out var spriteComponent, out var transformComponent))
        {
            if (transformComponent.MapUid != args.MapUid) continue;
            spriteComponent.Visible = false;

            if (_entityManager.HasComponent<SpriteStackingComponent>(uid))
                continue;

            var ordinalPosition = transformComponent.WorldPosition;
            var ordinalPositionScale = transformComponent.WorldPosition + new Vector2(1, 1);
            var ordinalRotation = transformComponent.WorldRotation;

            var rotTransEye = Matrix3x2.CreateRotation((float)eye.Rotation);
            var rotTransEyeNeg = Matrix3x2.CreateRotation(-(float)eye.Rotation);

            Transform(ref ordinalPosition, eye, rotTransEye, rotTransEyeNeg);

            if (!bounds.Contains(ordinalPosition)) continue;

            Transform(ref ordinalPositionScale, eye, rotTransEye, rotTransEyeNeg);
            var realScale = ordinalPositionScale - ordinalPosition;

            //spriteComponent.Scale = realScale;
            spriteComponent.Render(handle, eye.Rotation, ordinalRotation, position: ordinalPosition);
        }
    }

    private void Transform(ref Vector2 ordinalPosition, IEye eye, Matrix3x2 rotTransEye, Matrix3x2 rotTransEyeNeg)
    {
        ordinalPosition -= eye.Position.Position;
        ordinalPosition = Vector2.Transform(ordinalPosition, rotTransEye);
        ordinalPosition += eye.Position.Position;

        ordinalPosition = SpriteStackingOverlay.TransformContext.Transform(
            ordinalPosition,
            0,
            eye
        );

        ordinalPosition -= eye.Position.Position;
        ordinalPosition = Vector2.Transform(ordinalPosition, rotTransEyeNeg);
        ordinalPosition += eye.Position.Position;
    }
}