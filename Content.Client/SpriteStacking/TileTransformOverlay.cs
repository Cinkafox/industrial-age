using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Vector3 = System.Numerics.Vector3;

namespace Content.Client.SpriteStacking;

public sealed class TileTransformOverlay : GridOverlay
{
    public override bool RequestScreenTexture => true;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private readonly TransformSystem _transformSystem;
    private readonly SpriteSystem _spriteSystem;
    public TileTransformOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transformSystem = _entityManager.System<TransformSystem>();
        _spriteSystem = _entityManager.System<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || !_entityManager.TryGetComponent<TransformComponent>(Grid, out var xform))
            return;
        
        var eye = _eyeManager.CurrentEye;
        var bounds = args.WorldAABB.Enlarged(5f);
        
        using var draw = _profManager.Group("SpriteStackDraw");
        using var handle = new DrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds, _profManager, SpriteStackingOverlay.Accumulator);
        
        handle.DrawLayer(args.WorldAABB.BottomLeft, 0, xform.LocalRotation, ScreenTexture, scale: args.WorldAABB.Size);
    }
}