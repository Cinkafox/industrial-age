using System.Numerics;
using Content.Client.SpriteStacking.TransformContext;
using Content.Shared.ContentVariables;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Profiling;

namespace Content.Client.SpriteStacking.Overlays;

public sealed class SpriteStackingOverlay : Overlay
{
    public static readonly ITransformContext TransformContext = new ShittyTransformContext();
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    private readonly SpriteStackingTextureContainer _container;

    private readonly DrawingSpriteStackingContext _drawingContext;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;

    private readonly TransformSystem _transformSystem;

    public SpriteStackingOverlay(SpriteStackingTextureContainer container)
    {
        IoCManager.InjectDependencies(this);
        _drawingContext = new(1024 * 32, 48, TransformContext, container);
        _configurationManager.OnValueChanged(CCVars.StackByOneLayer, OnStackLayerChanged, true);
        _transformSystem = _entityManager.System<TransformSystem>();
        _container = container;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private void OnStackLayerChanged(int stackByOneLayer)
    {
        _drawingContext.LayerPerZ = stackByOneLayer;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _configurationManager.GetCVar<bool>("stacksprite.enabled");
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye!;
        var bounds = args.WorldAABB.Enlarged(5f);

        using var draw = _profManager.Group("SpriteStackDraw");
        using var handle = _drawingContext.GetDrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds);

        DrawEntities(eye, handle, bounds);
    }

    private void DrawEntities(
        IEye eye,
        DrawingSpriteStackingContext.DrawingHandleSpriteStacking handle,
        Box2 bounds
    )
    {
        var query = _entityManager.EntityQueryEnumerator<SpriteStackingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stackSpriteComponent, out var transformComponent))
        {
            if (transformComponent.MapID != eye.Position.MapId) continue;

            var drawPos = _transformSystem.GetWorldPosition(uid) - new Vector2(0.5f);
            ;
            if (!bounds.Contains(drawPos))
                continue;

            var (vector2Is, zLevels, size) = _container.StackManifest[stackSpriteComponent.Path];

            var translatedPos = vector2Is[stackSpriteComponent.State];

            for (var i = 0; i < zLevels; i++)
            {
                var sr = UIBox2i.FromDimensions(translatedPos + new Vector2i(0, size.Y * i), size);
                handle.DrawLayer(drawPos, i, _transformSystem.GetWorldRotation(uid), sr);
            }
        }
    }
}