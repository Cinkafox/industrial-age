using Content.Shared.ContentVariables;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client.SpriteStacking;

public sealed class SpriteStackingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    
    public override void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.StackRenderEnabled, OnRenderEnabledChanged, true);
        _overlayManager.AddOverlay(new SpriteStackingOverlay());
        _overlayManager.AddOverlay(new TileTransformOverlay());
        _overlayManager.AddOverlay(new EntTransformOverlay());
        SubscribeLocalEvent<SpriteStackingComponent, ComponentInit>(OnInit);
    }

    private void OnRenderEnabledChanged(bool obj)
    {
        if (!obj)
        {
            var query = EntityQueryEnumerator<SpriteComponent>();
            while (query.MoveNext(out var spriteComponent))
            {
                spriteComponent.Visible = true;
            }
        }
    }

    private void OnInit(Entity<SpriteStackingComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Path == ResPath.Empty)
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

    public override void FrameUpdate(float frameTime)
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