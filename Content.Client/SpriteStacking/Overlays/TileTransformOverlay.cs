using System.Linq;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Profiling;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.SpriteStacking.Overlays;

public sealed class TileTransformOverlay : GridOverlay
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private readonly DrawingSpriteStackingContext _drawingContext;
    private readonly TileTextureContainer _tileTextureContainer;

    private readonly MapSystem _mapSystem;
    private readonly TransformSystem _transformSystem;

    public TileTransformOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transformSystem = _entityManager.System<TransformSystem>();
        _mapSystem = _entityManager.System<MapSystem>();
        _tileTextureContainer = new TileTextureContainer();
        _tileTextureContainer._genTextureAtlas();
        _drawingContext = new(1024*16, 2, SpriteStackingOverlay.TransformContext, _tileTextureContainer);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _configurationManager.GetCVar<bool>("stacksprite.enabled");
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye!;
        var bounds = args.WorldAABB.Enlarged(5f);
        args.WorldHandle.DrawRect(bounds, Color.AliceBlue);

        using var draw = _profManager.Group("TileDrawStack");
        using var handle = _drawingContext.GetDrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds);

        DrawGrid(handle, args);
    }
    
    private void DrawGrid(DrawingSpriteStackingContext.DrawingHandleSpriteStacking handle, in OverlayDrawArgs args)
    {
        //var tiles = _mapSystem.GetAllTilesEnumerator(Grid, Grid.Comp);
        var AABB = args.WorldAABB.Enlarged(25f);
        var tiles = _mapSystem.GetLocalTilesEnumerator(Grid, Grid.Comp, AABB);

        while (tiles.MoveNext(out var tileDef))
        {
            var tile = tileDef.Tile;
            var regionMaybe = _tileTextureContainer.TileAtlasRegion(tile)![tile.Variant];

            handle.DrawLayer(new Vector2(tileDef.X, tileDef.Y),
                0,
                _transformSystem.GetWorldRotation(Grid),
                regionMaybe);
        }
    }
}

public sealed class TileTextureContainer : ITextureContainer
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IResourceManager _manager = default!;
    
    private readonly Dictionary<int, UIBox2[]> _tileRegions = new();
    public Texture AtlasTexture { get; private set; } = default!;

    public TileTextureContainer()
    {
        IoCManager.InjectDependencies(this);
    }
    
    public UIBox2[]? TileAtlasRegion(Tile tile)
    {
        return TileAtlasRegion(tile.TypeId);
    }

    public UIBox2[]? TileAtlasRegion(int tileType)
    {
        if (_tileRegions.TryGetValue(tileType, out var region)) return region;

        return null;
    }
    
    internal void _genTextureAtlas()
    {
        _tileRegions.Clear();

        var defList = _tileDefinitionManager.Where(t => t.Sprite != null).ToList();

        // If there are no tile definitions, we do nothing.
        if (defList.Count <= 0)
            return;

        const int tileSize = EyeManager.PixelsPerMeter;

        var tileCount = defList.Select(x => (int)x.Variants).Sum() + 1;

        var dimensionX = (int)Math.Ceiling(Math.Sqrt(tileCount));
        var dimensionY = (int)Math.Ceiling((float)tileCount / dimensionX);

        var imgWidth = dimensionX * tileSize;
        var imgHeight = dimensionY * tileSize;
        var sheet = new Image<Rgba32>(imgWidth, imgHeight);

        if (imgWidth >= 2048 || imgHeight >= 2048)
            // Sanity warning, some machines don't have textures larger than this and need multiple atlases.
            Logger.Warning(
                $"Tile texture atlas is ({imgWidth} x {imgHeight}), larger than 2048 x 2048. If you really need {tileCount} tiles, file an issue on RobustToolbox.");

        var column = 1;
        var row = 0;

        foreach (var def in defList)
        {
            Image<Rgba32> image;
            // Already know it's not null above
            var path = def.Sprite!.Value;

            using (var stream = _manager.ContentFileRead(path))
            {
                image = Image.Load<Rgba32>(stream);
            }

            if (image.Width != tileSize * def.Variants || image.Height != tileSize)
                throw new NotSupportedException(
                    $"Unable to load {path}, due to being unable to use tile texture with a dimension other than {tileSize}x({tileSize} * Variants).");

            var regionList = new UIBox2[def.Variants];

            for (var j = 0; j < def.Variants; j++)
            {
                var point = new Vector2i(column * tileSize, row * tileSize);

                var box = new UIBox2i(0, 0, tileSize, tileSize).Translated(new Vector2i(j * tileSize, 0));
                image.Blit(box, sheet, point);


                regionList[j] = UIBox2.FromDimensions(
                    point.X, sheet.Width - point.Y - EyeManager.PixelsPerMeter,
                    tileSize, tileSize);
                column++;

                if (column >= dimensionX)
                {
                    column = 0;
                    row++;
                }
            }

            _tileRegions.Add(def.TileId, regionList);
        }

        AtlasTexture = Texture.LoadFromImage(sheet, "Tile Atlas");
    }
}