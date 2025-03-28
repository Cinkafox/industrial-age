using System.Globalization;
using Content.Shared.DependencyRegistration;
using Content.Shared.Tile;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Entry;

public sealed class EntryPoint : GameShared
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    
    private const string Culture = "ru-RU";
    
    public override void PreInit()
    {
        AutoDependencyRegister.Register();
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
        
        _localizationManager.LoadCulture(new CultureInfo(Culture));
    }

    public override void Init()
    {
        _componentFactory.DoAutoRegistrations();
        _componentFactory.IgnoreMissingComponents();
        _componentFactory.GenerateNetIds();
        
        AutoDependencyRegister.Initialize();
    }

    public override void PostInit()
    {
        InitTileDefinitions();
    }
    
    private void InitTileDefinitions()
    {
        var prototypeList = new List<ContentTileDefinition>();
        
        _tileDefinitionManager.Register(new VoidTile());
        
        foreach (var tileDef in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
        {
            tileDef.Sprite = tileDef.SpriteSpecifier switch
            {
                SpriteSpecifier.Texture texture => texture.TexturePath,
                SpriteSpecifier.Rsi rsi => rsi.RsiPath / $"{rsi.RsiState}.png",
                _ => tileDef.Sprite
            };

            if (string.IsNullOrEmpty(tileDef.Name))
                tileDef.Name = Loc.GetString(tileDef.ID);
            
            prototypeList.Add(tileDef);
        }
        prototypeList.Sort((a, b) => string.Compare(a.ID, b.ID, StringComparison.Ordinal));

        foreach (var tileDef in prototypeList)
        {
            _tileDefinitionManager.Register(tileDef);
        }

        _tileDefinitionManager.Initialize();
    }
}

[Prototype("VoidTile")]
public sealed class VoidTile : ITileDefinition, IPrototype
{
    public ushort TileId { get; set; }
    public string Name { get; } = "Void";
    [IdDataField] public string ID { get; } = "Void";
    public ResPath? Sprite { get; }
    public Dictionary<Direction, ResPath> EdgeSprites { get; } = new();
    public int EdgeSpritePriority { get; }
    public float Friction { get; }
    public byte Variants { get; }
    public void AssignTileId(ushort id)
    {
        TileId = id;
    }
}