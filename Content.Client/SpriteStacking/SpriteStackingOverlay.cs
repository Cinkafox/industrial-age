using System.Numerics;
using Content.Shared.ContentVariables;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Profiling;
using Vector3 = System.Numerics.Vector3;

namespace Content.Client.SpriteStacking;


public record struct StackData(Vector2 Position, Angle Rotation, Texture Texture, UIBox2 TextureRect);
public sealed class SpriteStackingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private readonly TransformSystem _transformSystem;
    private readonly SpriteSystem _spriteSystem;
    
    private int _stackByOneLayer = 1;
    public static readonly SpriteStackingAccumulator Accumulator = new();
    
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private SortedDictionary<int, HashSet<StackData>> _layers = new();

    public SpriteStackingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _configurationManager.OnValueChanged(CCVars.StackByOneLayer,OnStackLayerChanged,true);
        _transformSystem = _entityManager.System<TransformSystem>();
        _spriteSystem = _entityManager.System<SpriteSystem>();
    }

    private void OnStackLayerChanged(int stackByOneLayer)
    {
        _stackByOneLayer = stackByOneLayer;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _layers.Clear();
        
        var eye = _eyeManager.CurrentEye;
        var bounds = args.WorldAABB.Enlarged(5f);
        var query = _entityManager.EntityQueryEnumerator<SpriteStackingComponent, TransformComponent>();

        using var draw = _profManager.Group("SpriteStackDraw");
        
        using var handle = new DrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds, _profManager, Accumulator);
        
        while (query.MoveNext(out var uid, out var stackSpriteComponent, out var transformComponent))
        {
            if(transformComponent.MapID != eye.Position.MapId) continue;
            
            var drawPos = _transformSystem.GetWorldPosition(uid) - new Vector2(0.5f);;
            if(!bounds.Contains(drawPos))
                continue;

            var datum = stackSpriteComponent.Data;
            var texture = datum.States[stackSpriteComponent.State];
            var size = datum.Metadata.Size;
            
            for (var i = 0; i < datum.Metadata.Height; i++)
            {
                var xIndex = i / texture.Width;
                var yIndex = i % texture.Height;

                var sr = UIBox2.FromDimensions(new Vector2(xIndex * size.X, yIndex * size.Y), size);
                
                if(!_layers.TryGetValue(i, out var list))
                {
                    list = new System.Collections.Generic.HashSet<StackData>();
                    _layers.Add(i, list);
                }
                
                list.Add(new StackData(drawPos, transformComponent.WorldRotation, texture, sr));
            }
        }

        foreach ((var Z, var stackData) in _layers)
        {
            foreach (var stack in stackData)
            {
                for (int i = 0; i < _stackByOneLayer; i++)
                {
                    var realZ = Z + i / (float)_stackByOneLayer;
                    handle.DrawLayer(stack.Position, realZ, stack.Rotation, stack.Texture, stack.TextureRect);
                }
            }
        }
    }
}

public sealed class SpriteStackingAccumulator
{
    public DrawVertexUV2D[] UvVertexes = new DrawVertexUV2D[6]; 
}

public sealed class DrawingHandleSpriteStacking : IDisposable
{
    private DrawingHandleBase _baseHandle;
    private IEye _currentEye;
    private Box2 _bounds;
    private readonly ProfManager _profManager;

    private SpriteStackingAccumulator _accumulator;

    public DrawingHandleSpriteStacking(DrawingHandleBase baseHandle, IEye currentEye, Box2 bounds, ProfManager profManager, SpriteStackingAccumulator accumulator)
    {
        _baseHandle = baseHandle;
        _currentEye = currentEye;
        _bounds = bounds;
        _profManager = profManager;
        _accumulator = accumulator;
    }

    public void DrawLayer(Vector2 position, float Zlevel, Angle rotation, Texture texture, UIBox2? textureRegion = null, Vector2? scale = null)
    {
        var sr = textureRegion ?? UIBox2.FromDimensions(Vector2.Zero, texture.Size);
        
        var currScale = scale ?? sr.Size / EyeManager.PixelsPerMeter;
        
        var p1 = position; //LeftTop
        var p3 = position + currScale; //RightBottom

        var p2 = new Vector2(p1.X, p3.Y);//LeftBottom
        var p4 = new Vector2(p3.X, p1.Y);//RightTop

        var center = p1 + currScale / 2f;
        
        var rotTrans = Matrix3x2.CreateRotation((float)rotation);

        p1 -= center;
        p2 -= center;
        p3 -= center;
        p4 -= center;

        p1 = Vector2.Transform(p1, rotTrans);
        p2 = Vector2.Transform(p2, rotTrans);
        p3 = Vector2.Transform(p3, rotTrans);
        p4 = Vector2.Transform(p4, rotTrans);
        
        p1 += center;
        p2 += center;
        p3 += center;
        p4 += center;

        p1 = Transform(p1,center,Zlevel);
        p2 = Transform(p2,center,Zlevel);
        p3 = Transform(p3,center,Zlevel);
        p4 = Transform(p4,center,Zlevel);
        
        if(!_bounds.Contains(p1) && 
           !_bounds.Contains(p2) && 
           !_bounds.Contains(p3) && 
           !_bounds.Contains(p4)) 
            return;
                
        var hw = texture.Size;
            
        var t1 = sr.TopLeft / hw;
        var t2 = sr.BottomLeft / hw;
        var t3 = sr.BottomRight / hw;
        var t4 = sr.TopRight / hw;
            
        _accumulator.UvVertexes[0] = new DrawVertexUV2D(p1, t1);
        _accumulator.UvVertexes[1] = new DrawVertexUV2D(p2, t2);
        _accumulator.UvVertexes[2] = new DrawVertexUV2D(p3, t3);
            
        _accumulator.UvVertexes[3] = new DrawVertexUV2D(p1, t1);
        _accumulator.UvVertexes[4] = new DrawVertexUV2D(p3, t3);
        _accumulator.UvVertexes[5] = new DrawVertexUV2D(p4, t4);
        
        _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,texture,_accumulator.UvVertexes); 
    }

    public Vector2 Transform(Vector2 p1, Vector2 center, float Zlevel)
    {
        return ApplyPerspectiveTransform(p1,Zlevel,
            _currentEye.Position.Position + _currentEye.Scale / 2,
            0f,
            0.025f);
    }
    
    static Vector2 ApplyPerspectiveTransform(Vector2 vector,float ZLevel, Vector2 center, float perspectiveX, float perspectiveY)
    {
        Vector2 translated = vector - center;
        
        float w = 1 + perspectiveX * translated.X + perspectiveY * translated.Y;
        float x = translated.X / w;
        float y = translated.Y / w;
        float z = ZLevel / w;
        
        return new Vector2(x, y * 0.55f + z*0.032f) + center;
    }

    public void Dispose()
    {
      
    }
}