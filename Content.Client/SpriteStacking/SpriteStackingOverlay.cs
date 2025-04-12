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
        
        DrawEntities(eye, handle, bounds);
    }
    
    private void DrawEntities(
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
        
        _drawingContext.rotTransEye =
            Matrix3x2.CreateTranslation(-_currentEye.Position.Position) * 
            Matrix3x2.CreateRotation((float)_currentEye.Rotation) *
            Matrix3x2.CreateTranslation(_currentEye.Position.Position);
        
        Matrix3x2.Invert(_drawingContext.rotTransEye, out _drawingContext.rotTransEyeNeg);
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
        
        _drawingContext.UvVertexes[0].Position = position; //LeftTop
        _drawingContext.UvVertexes[2].Position = position + _drawingContext.currScale; //RightBottom

        _drawingContext.UvVertexes[1].Position = new Vector2(_drawingContext.UvVertexes[0].Position.X, _drawingContext.UvVertexes[2].Position.Y);//LeftBottom
        _drawingContext.UvVertexes[3].Position = new Vector2(_drawingContext.UvVertexes[2].Position.X, _drawingContext.UvVertexes[0].Position.Y);//RightTop

        _drawingContext.center = _drawingContext.UvVertexes[0].Position + _drawingContext.currScale / 2f;
        _drawingContext.rotTrans = Matrix3x2.CreateTranslation(-_drawingContext.center) * 
                                   Matrix3x2.CreateRotation((float)rotation) *
                                   Matrix3x2.CreateTranslation(_drawingContext.center);
        
        TransformPoints(_drawingContext.rotTrans * _drawingContext.rotTransEye);
      
        _drawingContext.UvVertexes[0].Position = _transformContext.Transform(_drawingContext.UvVertexes[0].Position, zlevel, _currentEye);
        _drawingContext.UvVertexes[1].Position = _transformContext.Transform(_drawingContext.UvVertexes[1].Position, zlevel, _currentEye);
        _drawingContext.UvVertexes[2].Position = _transformContext.Transform(_drawingContext.UvVertexes[2].Position, zlevel, _currentEye);
        _drawingContext.UvVertexes[3].Position = _transformContext.Transform(_drawingContext.UvVertexes[3].Position, zlevel, _currentEye);
        
        TransformPoints(_drawingContext.rotTransEyeNeg);
      
        if(!_bounds.Contains(_drawingContext.UvVertexes[0].Position) && 
           !_bounds.Contains(_drawingContext.UvVertexes[1].Position) && 
           !_bounds.Contains(_drawingContext.UvVertexes[2].Position) && 
           !_bounds.Contains(_drawingContext.UvVertexes[3].Position)) 
            return;
        
        _drawingContext.UvVertexes[0].UV = textureRegion.Value.TopLeft / texture.Size;
        _drawingContext.UvVertexes[1].UV = textureRegion.Value.BottomLeft / texture.Size;
        _drawingContext.UvVertexes[2].UV = textureRegion.Value.BottomRight / texture.Size;
        _drawingContext.UvVertexes[3].UV = textureRegion.Value.TopRight / texture.Size;
        
        _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, texture,_drawingContext.UvVertexes); 
    }
    private void TransformPoints(Matrix3x2 matrix)
    {
        _drawingContext.UvVertexes[0].Position = Vector2.Transform(_drawingContext.UvVertexes[0].Position, matrix);
        _drawingContext.UvVertexes[1].Position = Vector2.Transform(_drawingContext.UvVertexes[1].Position, matrix);
        _drawingContext.UvVertexes[2].Position = Vector2.Transform(_drawingContext.UvVertexes[2].Position, matrix);
        _drawingContext.UvVertexes[3].Position = Vector2.Transform(_drawingContext.UvVertexes[3].Position, matrix);
    }
}