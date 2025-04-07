using System.Linq;
using System.Numerics;
using Content.Shared.ContentVariables;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Profiling;

namespace Content.Client.SpriteStacking;


public record struct StackData(Vector2 Position, Angle Rotation, Texture Texture, UIBox2 TextureRect, int Z);
public sealed class SpriteStackingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private readonly TransformSystem _transformSystem;
    
    private int _stackByOneLayer = 1;
    
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private SortedDictionary<float, HashSet<StackData>> _layers = new();
    public static readonly ITransformContext TransformContext = new ShittyTransformContext();
    
    private DrawingSpriteStackingContext _drawingContext = new();

    public SpriteStackingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _configurationManager.OnValueChanged(CCVars.StackByOneLayer,OnStackLayerChanged,true);
        _transformSystem = _entityManager.System<TransformSystem>();
    }

    private void OnStackLayerChanged(int stackByOneLayer)
    {
        _stackByOneLayer = stackByOneLayer;
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
        var handle = new DrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds, TransformContext, _drawingContext);
        
        DrawThink(eye, handle, bounds);
    }

    private void DrawEntities( 
        IEye eye, 
        DrawingHandleSpriteStacking handle, 
        Box2 bounds)
    {
        var query = 
            _entityManager.EntityQuery<SpriteStackingComponent, TransformComponent>()
                .Where((tuple => tuple.Item2.MapID == eye.Position.MapId))
                .OrderBy((tuple => -RotateVector(tuple.Item2.WorldPosition, eye).Y));
        
        foreach (var (spriteStackingComponent, transformComponent) in query)
        {
            var drawPos = transformComponent.WorldPosition - new Vector2(0.5f);;
            if(!bounds.Contains(drawPos))
                continue;
            
            var datum = spriteStackingComponent.Data;
            var texture = datum.States[spriteStackingComponent.State];
            var size = datum.Metadata.Size;
            
            for (var i = 0; i < datum.Metadata.Height; i++)
            {
                var xIndex = i / texture.Width;
                var yIndex = i % texture.Height;

                var sr = UIBox2.FromDimensions(new Vector2(xIndex * size.X, yIndex * size.Y), size);

                for (var y = 0; y < _stackByOneLayer; y++)
                {
                    var realZ = i + y / (float)_stackByOneLayer;

                    handle.DrawLayer(drawPos, realZ, transformComponent.WorldRotation, texture, sr);
                }
            }
        }
    }
    
    private void DrawThink(
        IEye eye, 
        DrawingHandleSpriteStacking handle, 
        Box2 bounds
    )
    {
        _layers.Clear();
        var query = _entityManager.EntityQueryEnumerator<SpriteStackingComponent, TransformComponent>();
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
                    list = new HashSet<StackData>();
                    _layers.Add(i, list);
                }
                
                list.Add(new StackData(drawPos, _transformSystem.GetWorldRotation(uid), texture, sr, i));
            }
        }

        foreach (var (_, stackData) in _layers)
        {
            foreach (var stack in stackData)
            {
                for (int i = 0; i < _stackByOneLayer; i++)
                {
                    var realZ = stack.Z + i / (float)_stackByOneLayer;
                    handle.DrawLayer(stack.Position, realZ, stack.Rotation, stack.Texture, stack.TextureRect);
                }
            }
        }
    }

    private Vector2 RotateVector(Vector2 vector, IEye eye)
    {
        vector -= eye.Position.Position;
        vector = eye.Rotation.RotateVec(vector);
        return vector + eye.Position.Position;
    }
    
}


public interface ITransformContext
{
    public Vector2 Transform(Vector2 p1, float Zlevel, IEye currentEye);
}

// Make this transform by matrix later
public sealed class ShittyTransformContext : ITransformContext
{
    public Vector2 Transform(Vector2 p1, float Zlevel, IEye currentEye)
    {
        return ApplyPerspectiveTransform(p1,Zlevel,
            currentEye.Position.Position + currentEye.Scale / 2,
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
}

public sealed class DrawingSpriteStackingContext
{
    public DrawVertexUV2D[] UvVertexes = new DrawVertexUV2D[4]; 
    
    public Vector2 p1;
    public Vector2 p2;
    public Vector2 p3;
    public Vector2 p4;

    public Matrix3x2 rotTransEye;
    public Matrix3x2 rotTransEyeNeg;
    public Matrix3x2 rotTrans;
    
    public Vector2 currScale;
    public Vector2 center;

    public DrawingSpriteStackingContext()
    {
        for (int i = 0; i < 4; i++)
        {
            UvVertexes[i] = new DrawVertexUV2D();
        }
    }
}

public sealed class DrawingHandleSpriteStacking
{
    private DrawingHandleBase _baseHandle;
    private IEye _currentEye;
    private Box2 _bounds;
    private ITransformContext _transformContext;
    private readonly DrawingSpriteStackingContext _drawingContext;

    public DrawingHandleSpriteStacking(DrawingHandleBase baseHandle, IEye currentEye, Box2 bounds, ITransformContext transformContext, DrawingSpriteStackingContext drawingContext)
    {
        _baseHandle = baseHandle;
        _currentEye = currentEye;
        _bounds = bounds;
        _transformContext = transformContext;
        _drawingContext = drawingContext;
    }

    public void DrawLayer(Vector2 position, float zlevel, Angle rotation, Texture texture, UIBox2? textureRegion = null, Vector2? scale = null)
    {
        position += _currentEye.Offset;

        if (texture is AtlasTexture atlasTexture)
        {
            textureRegion = atlasTexture.SubRegion;
            texture = atlasTexture.SourceTexture;
        }

        textureRegion ??= UIBox2.FromDimensions(Vector2.Zero, texture.Size);
        
        _drawingContext.currScale = scale ?? textureRegion.Value.Size / EyeManager.PixelsPerMeter;
        
        _drawingContext.p1 = position; //LeftTop
        _drawingContext.p3 = position + _drawingContext.currScale; //RightBottom

        _drawingContext.p2 = new Vector2(_drawingContext.p1.X, _drawingContext.p3.Y);//LeftBottom
        _drawingContext.p4 = new Vector2(_drawingContext.p3.X, _drawingContext.p1.Y);//RightTop

        _drawingContext.center = _drawingContext.p1 + _drawingContext.currScale / 2f;
        
        _drawingContext.rotTransEye = Matrix3x2.CreateRotation((float)_currentEye.Rotation);
        Matrix3x2.Invert(_drawingContext.rotTransEye, out _drawingContext.rotTransEyeNeg);
        _drawingContext.rotTrans = Matrix3x2.CreateRotation((float)rotation);
        
        ShiftPoints(-_drawingContext.center);
        TransformPoints(_drawingContext.rotTrans);
        ShiftPoints(_drawingContext.center);
        
        ShiftPoints(-_currentEye.Position.Position);
        TransformPoints(_drawingContext.rotTransEye);
        ShiftPoints(_currentEye.Position.Position);
      
        _drawingContext.p1 = _transformContext.Transform(_drawingContext.p1, zlevel, _currentEye);
        _drawingContext.p2 = _transformContext.Transform(_drawingContext.p2, zlevel, _currentEye);
        _drawingContext.p3 = _transformContext.Transform(_drawingContext.p3, zlevel, _currentEye);
        _drawingContext.p4 = _transformContext.Transform(_drawingContext.p4, zlevel, _currentEye);
        
        ShiftPoints(-_currentEye.Position.Position);
        TransformPoints(_drawingContext.rotTransEyeNeg);
        ShiftPoints(_currentEye.Position.Position);
      
        if(!_bounds.Contains(_drawingContext.p1) && 
           !_bounds.Contains(_drawingContext.p2) && 
           !_bounds.Contains(_drawingContext.p3) && 
           !_bounds.Contains(_drawingContext.p4)) 
            return;
        
        SetVertex(0, _drawingContext.p1, textureRegion.Value.TopLeft     / texture.Size);
        SetVertex(1, _drawingContext.p2, textureRegion.Value.BottomLeft  / texture.Size);
        SetVertex(2, _drawingContext.p3, textureRegion.Value.BottomRight / texture.Size);
        SetVertex(3, _drawingContext.p4, textureRegion.Value.TopRight    / texture.Size);
        
        _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, texture,_drawingContext.UvVertexes); 
    }

    private void SetVertex(int i, Vector2 position, Vector2 uv)
    {
        _drawingContext.UvVertexes[i].Position = position;
        _drawingContext.UvVertexes[i].UV = uv;
    }

    private void TransformPoints(Matrix3x2 matrix)
    {
        _drawingContext.p1 = Vector2.Transform(_drawingContext.p1, matrix);
        _drawingContext.p2 = Vector2.Transform(_drawingContext.p2, matrix);
        _drawingContext.p3 = Vector2.Transform(_drawingContext.p3, matrix);
        _drawingContext.p4 = Vector2.Transform(_drawingContext.p4, matrix);
    }

    private void ShiftPoints(Vector2 to)
    {
        _drawingContext.p1 += to;
        _drawingContext.p2 += to;
        _drawingContext.p3 += to;
        _drawingContext.p4 += to;
    }
}