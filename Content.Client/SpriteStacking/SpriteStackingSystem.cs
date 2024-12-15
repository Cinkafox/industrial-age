using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.SpriteStacking;

public sealed class SpriteStackingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    
    public override void Initialize()
    {
        _overlayManager.AddOverlay(new SpriteStackingOverlay());
        SubscribeLocalEvent<SpriteStackingComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<SpriteStackingComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Path == default)
        {
            if (!TryComp<SpriteComponent>(ent, out var spriteComponent) || spriteComponent.BaseRSI == null)
            {
                Log.Error("Path is not defined!");
                RemComp<SpriteStackingComponent>(ent);
                return;
            }
            
            ent.Comp.Path = spriteComponent.BaseRSI.Path;
            ent.Comp.State = spriteComponent.LayerGetState(0).Name!;
            ent.Comp.UpdateStateOnSpriteChange = true;
        }

        if (!_resourceCache.TryGetResource<SpriteStackingResource>(ent.Comp.Path, out var spriteStackingResource))
        {
            Log.Error("Path not found!");
            RemComp<SpriteStackingComponent>(ent);
            return;
        }

        ent.Comp.Data = spriteStackingResource.Data;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SpriteStackingComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var spriteStackingComponent, out var spriteComponent))
        {
            var state = spriteComponent.LayerGetState(0).Name;
            if (spriteStackingComponent.UpdateStateOnSpriteChange &&
                state != spriteStackingComponent.State && state != null)
            {
                spriteStackingComponent.State = state;
            }
        }
    }
}