using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Client.SpriteStacking;

public sealed class EntTransformOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    
    public EntTransformOverlay()
    {
        IoCManager.InjectDependencies(this);
    }
    
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _configurationManager.GetCVar<bool>("stacksprite.enabled");
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if(!args.MapUid.IsValid()) return;
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);
        var eye = args.Viewport.Eye!;
        
        var spriteQuery = _entityManager.EntityQueryEnumerator<SpriteComponent, TransformComponent>();
        while (spriteQuery.MoveNext(out var uid, out var spriteComponent, out var transformComponent))
        {
            if(transformComponent.MapUid != args.MapUid) continue;
            spriteComponent.Visible = false;
            
            if(_entityManager.HasComponent<SpriteStackingComponent>(uid)) 
                continue;
            
            var ordinalPosition = transformComponent.WorldPosition;
            var ordinalRotation = transformComponent.WorldRotation;
            
            var rotTransEye = Matrix3x2.CreateRotation((float)eye.Rotation);
            var rotTransEyeNeg = Matrix3x2.CreateRotation(-(float)eye.Rotation);
            
            
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
            
            if(!bounds.Contains(ordinalPosition)) continue;
            
            spriteComponent.Render(handle, eye.Rotation, ordinalRotation, position: ordinalPosition);
        }
    }
    
}