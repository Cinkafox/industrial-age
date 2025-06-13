using System.IO;
using Content.Shared.ContentVariables;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.SpriteStacking;

public sealed class SpriteStackingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IGameController _gameController = default!;

    private SpriteStackingTextureContainer _spriteStackingTextureContainer = new();
    
    public override void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.StackRenderEnabled, OnRenderEnabledChanged, true);
        
        _overlayManager.AddOverlay(new SpriteStackingOverlay(_spriteStackingTextureContainer));
        _overlayManager.AddOverlay(new TileTransformOverlay());
        _overlayManager.AddOverlay(new EntTransformOverlay());
        
        try
        {
            _spriteStackingTextureContainer.RebuildAtlasTexture();
        }
        catch(Exception e)
        {
            Log.Error(e.Message);
            Log.Error(e.StackTrace ?? "?");
            _gameController.Shutdown();
        }
        
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

public sealed class SpriteStackingTextureContainer
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    
    public Texture AtlasTexture = default!;
    public readonly Dictionary<ResPath, SpriteStackingContainerEntry> StackManifest = new();

    public SpriteStackingTextureContainer()
    {
        IoCManager.InjectDependencies(this);
    }
    
    public void RebuildAtlasTexture()
    {
        StackManifest.Clear();
        var rawImage = new Image<Rgba32>(1024, 1024);
        
        var widthShift = 0;
        var heightShift = 0;
        var heightMax = 0;
        
        foreach (var entProto in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if(!entProto.TryGetComponent<SpriteStackingComponent>(out var spriteStackingComponent, _componentFactory))
                continue;

            var currPath = spriteStackingComponent.Path;

            if (currPath == ResPath.Empty)
            {
                if(!entProto.TryGetComponent<SpriteComponent>(out var spriteComponent, _componentFactory) || 
                   spriteComponent.BaseRSI is null)
                    continue;
                
                currPath = spriteComponent.BaseRSI.Path;
            }
            
            if(!_resourceCache.TryGetResource<SpriteStackingResource>(currPath, out var spriteStackingResource))
                continue;
            
            if (!StackManifest.TryGetValue(currPath, out var state))
            {
                state = new SpriteStackingContainerEntry(
                    new Dictionary<string, Vector2i>(), 
                    spriteStackingResource.Data.Metadata.Height, 
                    spriteStackingResource.Data.Metadata.Size
                    );
                StackManifest.Add(currPath, state);
            }

            foreach (var (stateName, stateTexture) in spriteStackingResource.Data.States)
            {
                var stateBox = new UIBox2i(0,0, stateTexture.Width, stateTexture.Height);
                var boxTranslated = new Vector2i(widthShift, heightShift);
                
                stateTexture.Blit(stateBox, rawImage, boxTranslated);
                
                state.States.Add(stateName, boxTranslated);
                widthShift += stateTexture.Width;
                heightMax = Math.Max(heightMax, stateTexture.Height);
            }
        }
        
        AtlasTexture = _clyde.LoadTextureFromImage(rawImage, "SpriteStackingAtlas");
    }
}

public record struct SpriteStackingContainerEntry(Dictionary<string, Vector2i> States, int ZLevels, Vector2i Size);