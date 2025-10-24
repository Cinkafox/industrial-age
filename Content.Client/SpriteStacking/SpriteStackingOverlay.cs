using System.Collections;
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
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    
    private readonly TransformSystem _transformSystem;
    private readonly SpriteStackingTextureContainer _container;
    
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public static readonly ITransformContext TransformContext = new ShittyTransformContext();
    
    private readonly DrawingSpriteStackingContext _drawingContext = new(1024*32, 48);

    private Font _font;

    public SpriteStackingOverlay(SpriteStackingTextureContainer container)
    {
        IoCManager.InjectDependencies(this);
        _configurationManager.OnValueChanged(CCVars.StackByOneLayer,OnStackLayerChanged,true);
        _transformSystem = _entityManager.System<TransformSystem>();

        _font = _resourceCache.GetResource<FontResource>("/Fonts/Ebbe/Ebbe Black.ttf").MakeDefault();
        
        _container = container;
    }

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
        _drawingContext.Texture = _container.AtlasTexture;
        
        var eye = args.Viewport.Eye!;
        var bounds = args.WorldAABB.Enlarged(5f);

        using var draw = _profManager.Group("SpriteStackDraw");
        using var handle = new DrawingHandleSpriteStacking(args.DrawingHandle, eye, bounds, TransformContext, _drawingContext);
        
        DrawEntities(eye, handle, bounds);
    }
    
    private void DrawEntities(
        IEye eye, 
        DrawingHandleSpriteStacking handle, 
        Box2 bounds
    )
    {
        var query = _entityManager.EntityQueryEnumerator<SpriteStackingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stackSpriteComponent, out var transformComponent))
        {
            if(transformComponent.MapID != eye.Position.MapId) continue;
            
            var drawPos = _transformSystem.GetWorldPosition(uid) - new Vector2(0.5f);;
            if(!bounds.Contains(drawPos))
                continue;

            var (vector2Is, zLevels, size) = _container.StackManifest[stackSpriteComponent.Path];

            var translatedPos = vector2Is[stackSpriteComponent.State];
            
            for (var i = 0; i < zLevels; i++)
            {
                var sr = UIBox2i.FromDimensions(translatedPos + new Vector2i(0, size.Y * i), size);
                
                handle.DrawLayer(drawPos, i, transformComponent.WorldRotation, sr);
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
    public Vector2 Transform(Vector2 p1, float zlevel, IEye currentEye)
    {
        return ApplyPerspectiveTransform(p1,zlevel,
            currentEye.Position.Position + currentEye.Scale / 2,
            0f,
            0.025f);
    }
    
    static Vector2 ApplyPerspectiveTransform(Vector2 vector,float zLevel, Vector2 center, float perspectiveX, float perspectiveY)
    {
        Vector2 translated = vector - center;
        
        var w = 1 + perspectiveX * translated.X + perspectiveY * translated.Y;
        var x = translated.X / w;
        var y = translated.Y / w;
        var z = zLevel / w;
        
        return new Vector2(x, y * 0.55f + z*0.032f) + center;
    }
}

public sealed class DrawingSpriteStackingContext
{
    public DrawVertexUV2DColor[] ProcessingVertices = new DrawVertexUV2DColor[4];

    private readonly int _layerBufferLength;
    private readonly int _layerMaxZIndex ;

    private readonly DrawVertexUV2DColor[] _layers;
    private readonly int[] _layerLengths;
    
    private int _totalLength = 0;

    private LayerEnumerator _currentEnumerator;
    
    public int TotalLength = 0;

    public Texture Texture = default!;

    public Matrix3x2 rotTransEye;
    public Matrix3x2 rotTransEyeNeg;
    public Matrix3x2 rotTrans;
    
    public Vector2 currScale;
    public Vector2 center;

    public int LayerPerZ = 1;

    public DrawingSpriteStackingContext(int layerBufferLength, int layerMaxZIndex)
    {
        _layerBufferLength = layerBufferLength;
        _layerMaxZIndex = layerMaxZIndex;
        
        _layers = new DrawVertexUV2DColor[_layerMaxZIndex*_layerBufferLength];
        _layerLengths = new int[_layerMaxZIndex];
            
        _currentEnumerator = new(this);
        for (var i = 0; i < ProcessingVertices.Length; i++)
        {
            ProcessingVertices[i].Color = Color.White;
        }
    }

    public void PushVertex(int zLevel, DrawVertexUV2DColor uv)
    {
        _layers[_layerLengths[zLevel] + _layerBufferLength * zLevel] = uv;
        _layerLengths[zLevel] += 1;
        _totalLength += 1;
    }

    public void PushCurrentFace(int zLevel)
    {
        PushVertex(zLevel, ProcessingVertices[0]);
        PushVertex(zLevel, ProcessingVertices[1]);
        PushVertex(zLevel, ProcessingVertices[2]);
        
        PushVertex(zLevel, ProcessingVertices[0]);
        PushVertex(zLevel, ProcessingVertices[3]);
        PushVertex(zLevel, ProcessingVertices[2]);
    }

    public void Clear()
    {
        for (var i = 0; i < _layerLengths.Length; i++)
        {
            _layerLengths[i] = 0;
        }

        TotalLength = _totalLength;
        _totalLength = 0;
    }

    public LayerEnumerator GetEnumerator()
    {
        _currentEnumerator.Reset();
        return _currentEnumerator;
    }
    
    public sealed class LayerEnumerator : IEnumerator<DrawVertexUV2DColor[]>
    {
        private readonly DrawingSpriteStackingContext _context;
    
        public LayerEnumerator(DrawingSpriteStackingContext context)
        {
            _context = context;
        }
    
        private int currentLayer = 0;

        private DrawVertexUV2DColor[] _current = default!;

        public bool MoveNext()
        {
            if (_context._layerLengths.Length <= currentLayer) return false;
        
            _current = new DrawVertexUV2DColor[_context._layerLengths[currentLayer]];
        
            for (var vertexIndex = 0; vertexIndex < _context._layerLengths[currentLayer]; vertexIndex++)
            {
                _current[vertexIndex] = _context._layers[vertexIndex + currentLayer * _context._layerBufferLength];
            }

            currentLayer++;

            return true;
        }

        public bool MoveNext(out DrawVertexUV2DColor[]? vertexUv2DColors)
        {
            vertexUv2DColors = null;
            if (!MoveNext()) return false;
        
            vertexUv2DColors = _current;
            return true;
        }

        public void Reset()
        {
            currentLayer = 0;
        }

        public DrawVertexUV2DColor[] Current => _current;

        object? IEnumerator.Current => _current;

        public void Dispose()
        {
        
        }
    }
}

public sealed class DrawingHandleSpriteStacking: IDisposable
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

    public void DrawLayer(Vector2 position, int zlevel, Angle rotation, UIBox2 textureRegion , Vector2? scale = null)
    {
        if (_drawingContext.LayerPerZ == 1)
        {
            _drawLayer(position, zlevel,0 , rotation, textureRegion, scale);
            return;
        }

        var drawTranslate = 1f / _drawingContext.LayerPerZ;

        for (var i = 0; i < _drawingContext.LayerPerZ; i++)
        {
            _drawLayer(position, zlevel, i * drawTranslate, rotation, textureRegion, scale);
        }
    }

    private void _drawLayer(Vector2 position, int zlevel, float translateZ, Angle rotation, UIBox2 textureRegion , Vector2? scale = null)
    {
        position += _currentEye.Offset;
        
        _drawingContext.currScale = scale ?? textureRegion.Size / EyeManager.PixelsPerMeter;
        
        _drawingContext.ProcessingVertices[0].Position = position; //LeftTop
        _drawingContext.ProcessingVertices[2].Position = position + _drawingContext.currScale; //RightBottom

        _drawingContext.ProcessingVertices[1].Position = new Vector2(_drawingContext.ProcessingVertices[0].Position.X, _drawingContext.ProcessingVertices[2].Position.Y);//LeftBottom
        _drawingContext.ProcessingVertices[3].Position = new Vector2(_drawingContext.ProcessingVertices[2].Position.X, _drawingContext.ProcessingVertices[0].Position.Y);//RightTop

        _drawingContext.center = _drawingContext.ProcessingVertices[0].Position + _drawingContext.currScale / 2f;
        _drawingContext.rotTrans = Matrix3x2.CreateTranslation(-_drawingContext.center) * 
                                   Matrix3x2.CreateRotation((float)rotation) *
                                   Matrix3x2.CreateTranslation(_drawingContext.center);
        
        TransformPoints(_drawingContext.rotTrans * _drawingContext.rotTransEye);
      
        _drawingContext.ProcessingVertices[0].Position = _transformContext.Transform(_drawingContext.ProcessingVertices[0].Position, zlevel + translateZ, _currentEye);
        _drawingContext.ProcessingVertices[1].Position = _transformContext.Transform(_drawingContext.ProcessingVertices[1].Position, zlevel + translateZ, _currentEye);
        _drawingContext.ProcessingVertices[2].Position = _transformContext.Transform(_drawingContext.ProcessingVertices[2].Position, zlevel + translateZ, _currentEye);
        _drawingContext.ProcessingVertices[3].Position = _transformContext.Transform(_drawingContext.ProcessingVertices[3].Position, zlevel + translateZ, _currentEye);
        
        TransformPoints(_drawingContext.rotTransEyeNeg);
      
        if(!_bounds.Contains(_drawingContext.ProcessingVertices[0].Position) && 
           !_bounds.Contains(_drawingContext.ProcessingVertices[1].Position) && 
           !_bounds.Contains(_drawingContext.ProcessingVertices[2].Position) && 
           !_bounds.Contains(_drawingContext.ProcessingVertices[3].Position)) 
            return;
        
        _drawingContext.ProcessingVertices[0].UV = textureRegion.TopLeft / _drawingContext.Texture.Size + new Vector2(0.0001f,0);
        _drawingContext.ProcessingVertices[1].UV = textureRegion.BottomLeft / _drawingContext.Texture.Size + new Vector2(0.0001f,0);;;
        _drawingContext.ProcessingVertices[2].UV = textureRegion.BottomRight / _drawingContext.Texture.Size;
        _drawingContext.ProcessingVertices[3].UV = textureRegion.TopRight / _drawingContext.Texture.Size;
        
        _drawingContext.PushCurrentFace(zlevel);
    }
    
    private void TransformPoints(Matrix3x2 matrix)
    {
        _drawingContext.ProcessingVertices[0].Position = Vector2.Transform(_drawingContext.ProcessingVertices[0].Position, matrix);
        _drawingContext.ProcessingVertices[1].Position = Vector2.Transform(_drawingContext.ProcessingVertices[1].Position, matrix);
        _drawingContext.ProcessingVertices[2].Position = Vector2.Transform(_drawingContext.ProcessingVertices[2].Position, matrix);
        _drawingContext.ProcessingVertices[3].Position = Vector2.Transform(_drawingContext.ProcessingVertices[3].Position, matrix);
    }

    public void Flush()
    {
        try
        {
            using var enumerator = _drawingContext.GetEnumerator();
            while (enumerator.MoveNext(out var vertexUv2DColors))
            {
                _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _drawingContext.Texture,
                    vertexUv2DColors);
            }
        }
        finally
        {
            _drawingContext.Clear();
        }
    }

    public void Dispose()
    {
        Flush();
    }
}