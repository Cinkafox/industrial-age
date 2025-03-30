using System.Numerics;
using Content.Shared.ContentVariables;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Profiling;

namespace Content.Client.SpriteStacking;


public record struct StackData(Vector2 Position, Angle Rotation, Texture Texture, UIBox2 TextureRect);
public sealed class SpriteStackingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private readonly TransformSystem _transformSystem;
    
    private int _stackByOneLayer = 1;
    
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private SortedDictionary<int, HashSet<StackData>> _layers = new();
    public static readonly ITransformContext TransformContext = new ShittyTransformContext();

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
        _layers.Clear();
        
        var eye = args.Viewport.Eye!;
        var bounds = args.WorldAABB.Enlarged(5f);
        var query = _entityManager.EntityQueryEnumerator<SpriteStackingComponent, TransformComponent>();

        using var draw = _profManager.Group("SpriteStackDraw");
        var handle = new DrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds, TransformContext);
        
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
                
                list.Add(new StackData(drawPos, _transformSystem.GetWorldRotation(uid), texture, sr));
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

public sealed class DrawingHandleSpriteStacking
{
    private DrawingHandleBase _baseHandle;
    private IEye _currentEye;
    private Box2 _bounds;
    private ITransformContext _transformContext;

    public DrawingHandleSpriteStacking(DrawingHandleBase baseHandle, IEye currentEye, Box2 bounds, ITransformContext transformContext)
    {
        _baseHandle = baseHandle;
        _currentEye = currentEye;
        _bounds = bounds;
        _transformContext = transformContext;
    }

    private Vector2 p1;
    private Vector2 p2;
    private Vector2 p3;
    private Vector2 p4;
    
    private DrawVertexUV2D[] UvVertexes = new DrawVertexUV2D[6]; 

    public void DrawLayer(Vector2 position, float Zlevel, Angle rotation, Texture texture, UIBox2? textureRegion = null, Vector2? scale = null)
    {
        position += _currentEye.Offset;

        if (texture is AtlasTexture atlasTexture)
        {
            textureRegion = atlasTexture.SubRegion;
            texture = atlasTexture.SourceTexture;
        }
        
        var sr = textureRegion ?? UIBox2.FromDimensions(Vector2.Zero, texture.Size);
        
        var currScale = scale ?? sr.Size / EyeManager.PixelsPerMeter;
        
        p1 = position; //LeftTop
        p3 = position + currScale; //RightBottom

        p2 = new Vector2(p1.X, p3.Y);//LeftBottom
        p4 = new Vector2(p3.X, p1.Y);//RightTop

        var center = p1 + currScale / 2f;
        
        var rotTransEye = Matrix3x2.CreateRotation((float)_currentEye.Rotation);
        var rotTransEyeNeg = Matrix3x2.CreateRotation(-(float)_currentEye.Rotation);
        var rotTrans = Matrix3x2.CreateRotation((float)rotation);

        ShiftPoints(-center);
        TransformPoints(rotTrans);
        ShiftPoints(center);
        
        ShiftPoints(-_currentEye.Position.Position);
        TransformPoints(rotTransEye);
        ShiftPoints(_currentEye.Position.Position);

        p1 = _transformContext.Transform(p1, Zlevel, _currentEye);
        p2 = _transformContext.Transform(p2, Zlevel, _currentEye);
        p3 = _transformContext.Transform(p3, Zlevel, _currentEye);
        p4 = _transformContext.Transform(p4, Zlevel, _currentEye);
        
        ShiftPoints(-_currentEye.Position.Position);
        TransformPoints(rotTransEyeNeg);
        ShiftPoints(_currentEye.Position.Position);
        
        if(!_bounds.Contains(p1) && 
           !_bounds.Contains(p2) && 
           !_bounds.Contains(p3) && 
           !_bounds.Contains(p4)) 
            return;
            
        var t1 = sr.TopLeft / texture.Size;
        var t2 = sr.BottomLeft / texture.Size;
        var t3 = sr.BottomRight / texture.Size;
        var t4 = sr.TopRight / texture.Size;
            
        UvVertexes[0] = new DrawVertexUV2D(p1, t1);
        UvVertexes[1] = new DrawVertexUV2D(p2, t2);
        UvVertexes[2] = new DrawVertexUV2D(p3, t3);
            
        UvVertexes[3] = new DrawVertexUV2D(p1, t1);
        UvVertexes[4] = new DrawVertexUV2D(p3, t3);
        UvVertexes[5] = new DrawVertexUV2D(p4, t4);
        
        _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,texture,UvVertexes); 
    }
    

    private void TransformPoints(Matrix3x2 matrix)
    {
        p1 = Vector2.Transform(p1, matrix);
        p2 = Vector2.Transform(p2, matrix);
        p3 = Vector2.Transform(p3, matrix);
        p4 = Vector2.Transform(p4, matrix);
    }

    private void ShiftPoints(Vector2 to)
    {
        p1 += to;
        p2 += to;
        p3 += to;
        p4 += to;
    }
}