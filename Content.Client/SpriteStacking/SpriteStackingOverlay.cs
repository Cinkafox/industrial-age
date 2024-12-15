using System.Numerics;
using Content.Shared.ContentVariables;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Profiling;

namespace Content.Client.SpriteStacking;

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
    private SpriteStackingAccumulator _accumulator = new();
    
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

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
        var eye = _eyeManager.CurrentEye;
        var bounds = args.WorldAABB.Enlarged(5f);
        var query = _entityManager.EntityQueryEnumerator<SpriteStackingComponent, TransformComponent>();

        using var draw = _profManager.Group("SpriteStackDraw");
        
        using var handle = new DrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds, _profManager, _accumulator);
        
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
                
                for (var z = 0; z < _stackByOneLayer; z++)
                {
                    var zLevelLayer = i + z / (float)_stackByOneLayer;
                    
                    handle.DrawLayer(drawPos, zLevelLayer, transformComponent.WorldRotation, texture, sr);
                }
            }
            
            handle.Flush();
        }
    }
}

public sealed class SpriteStackingAccumulator
{
    public SortedDictionary<float, HashSet<VertexStackin>> VertexQueue = new();
    public List<Vector2> VertexPool = new();
    public DrawVertexUV2D[] UvVertexes = new DrawVertexUV2D[6]; 
}

public sealed record VertexStackin(Texture Texture, int vertId, UIBox2 sr);

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

    public void DrawLayer(Vector2 position, float Zlevel, Angle rotation, Texture texture, UIBox2 sr)
    {
        var currScale = sr.Size / (float)EyeManager.PixelsPerMeter;
        
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

        p1 = Transform(p1,Zlevel);
        p2 = Transform(p2,Zlevel);
        p3 = Transform(p3,Zlevel);
        p4 = Transform(p4,Zlevel);
        
        if(!_bounds.Contains(p1) && 
           !_bounds.Contains(p2) && 
           !_bounds.Contains(p3) && 
           !_bounds.Contains(p4)) 
            return;
        
        using var preparing = _profManager.Group("SpriteStackingPrepareLayer");
        
        var vertexId = _accumulator.VertexPool.Count;
        
        _accumulator.VertexPool.Add(p1);
        _accumulator.VertexPool.Add(p2);
        _accumulator.VertexPool.Add(p3);
        _accumulator.VertexPool.Add(p4);

        if (!_accumulator.VertexQueue.TryGetValue(Zlevel, out var vertexArra))
        {
            vertexArra = new HashSet<VertexStackin>();
            _accumulator.VertexQueue.Add(Zlevel, vertexArra);
        }

        vertexArra.Add(new VertexStackin(texture, vertexId, sr));
    }

    public void Flush()
    {
        using var flush = _profManager.Group("SpriteStackingFlush");

        foreach (var (_, vertexHeight) in _accumulator.VertexQueue)
        {
            foreach (var vertexStackin in vertexHeight)
            {
                var texture = vertexStackin.Texture;
                var sr = vertexStackin.sr;
                
                var hw = texture.Size;
            
                var t1 = sr.TopLeft / hw;
                var t2 = sr.BottomLeft / hw;
                var t3 = sr.BottomRight / hw;
                var t4 = sr.TopRight / hw;
            
                var p1 = _accumulator.VertexPool[vertexStackin.vertId];
                var p2 = _accumulator.VertexPool[vertexStackin.vertId + 1];
                var p3 = _accumulator.VertexPool[vertexStackin.vertId + 2];
                var p4 = _accumulator.VertexPool[vertexStackin.vertId + 3];
            
                _accumulator.UvVertexes[0] = new DrawVertexUV2D(p1, t1);
                _accumulator.UvVertexes[1] = new DrawVertexUV2D(p2, t2);
                _accumulator.UvVertexes[2] = new DrawVertexUV2D(p3, t3);
            
                _accumulator.UvVertexes[3] = new DrawVertexUV2D(p1, t1);
                _accumulator.UvVertexes[4] = new DrawVertexUV2D(p3, t3);
                _accumulator.UvVertexes[5] = new DrawVertexUV2D(p4, t4);
            
                _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,texture,_accumulator.UvVertexes); 
            }
        }
    }

    private Vector2 Transform(Vector2 p1, float Zlevel)
    {
        var delta = p1 - _currentEye.Position.Position;
        return new Vector2(p1.X, p1.Y) + delta*0.01f* Zlevel+Zlevel*new Vector2(0,0.0f);
    }

    public void Dispose()
    {
        _accumulator.VertexPool.Clear();
        _accumulator.VertexQueue.Clear();
    }
}