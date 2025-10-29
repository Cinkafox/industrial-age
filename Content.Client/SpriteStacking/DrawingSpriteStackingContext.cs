using System.Numerics;
using Content.Client.SpriteStacking.TransformContext;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;

namespace Content.Client.SpriteStacking;

public sealed class DrawingSpriteStackingContext
{
    private readonly int _layerBufferLength;
    private readonly int[] _layerLengths;
    private readonly int _layerMaxZIndex;
    private readonly ITextureContainer _textureContainer;

    private readonly DrawVertexUV2DColor[] _layers;
    private readonly DrawVertexUV2DColor[] _processingVertices = new DrawVertexUV2DColor[4];
    private readonly LayerEnumerator _currentEnumerator;
    private Texture _texture => _textureContainer.AtlasTexture;
    
    private Vector2 _center;
    private Vector2 _currScale;
    private Matrix3x2 _rotTrans;
    private Matrix3x2 _rotTransEye;
    private Matrix3x2 _rotTransEyeNeg;
    private int _totalLength;

    public DrawingSpriteStackingContext(int layerBufferLength, int layerMaxZIndex, ITransformContext transformContext, ITextureContainer textureContainer)
    {
        TransformContext = transformContext;
        _layerBufferLength = layerBufferLength;
        _layerMaxZIndex = layerMaxZIndex;
        _textureContainer = textureContainer;

        _layers = new DrawVertexUV2DColor[_layerMaxZIndex * _layerBufferLength];
        _layerLengths = new int[_layerMaxZIndex];

        _currentEnumerator = new LayerEnumerator(this);
        for (var i = 0; i < _processingVertices.Length; i++) _processingVertices[i].Color = Color.White;
    }

    public ITransformContext TransformContext { get; }
    public int TotalLength { get; private set; }
    public int LayerPerZ { get; set; } = 1;

    private void PushVertex(int zLevel, DrawVertexUV2DColor uv)
    {
        _layers[_layerLengths[zLevel] + _layerBufferLength * zLevel] = uv;
        _layerLengths[zLevel] += 1;
        _totalLength += 1;
    }

    private void PushCurrentFace(int zLevel)
    {
        PushVertex(zLevel, _processingVertices[0]);
        PushVertex(zLevel, _processingVertices[1]);
        PushVertex(zLevel, _processingVertices[2]);

        PushVertex(zLevel, _processingVertices[0]);
        PushVertex(zLevel, _processingVertices[3]);
        PushVertex(zLevel, _processingVertices[2]);
    }

    private void Clear()
    {
        for (var i = 0; i < _layerLengths.Length; i++) _layerLengths[i] = 0;

        TotalLength = _totalLength;
        _totalLength = 0;
    }

    private LayerEnumerator GetEnumerator()
    {
        _currentEnumerator.Reset();
        return _currentEnumerator;
    }

    public DrawingHandleSpriteStacking GetDrawingHandleSpriteStacking(DrawingHandleBase drawingHandleBase,
        IEye currentEye, Box2 bounds)
    {
        return new DrawingHandleSpriteStacking(drawingHandleBase, currentEye, bounds, TransformContext, this);
    }

    private sealed class LayerEnumerator
    {
        private readonly DrawingSpriteStackingContext _context;

        private int _currentLayer;

        public LayerEnumerator(DrawingSpriteStackingContext context)
        {
            _context = context;
        }

        public bool MoveNext(out ReadOnlySpan<DrawVertexUV2DColor> vertexUv2DColors)
        {
            if (_context._layerLengths.Length <= _currentLayer)
            {
                vertexUv2DColors = null;
                return false;
            }

            vertexUv2DColors = _context._layers.AsSpan(_currentLayer * _context._layerBufferLength,
                _context._layerLengths[_currentLayer]);
            _currentLayer++;
            return true;
        }

        public void Reset()
        {
            _currentLayer = 0;
        }
    }

    public sealed class DrawingHandleSpriteStacking : IDisposable
    {
        private readonly DrawingSpriteStackingContext _drawingContext;
        private readonly DrawingHandleBase _baseHandle;
        private readonly Box2 _bounds;
        private readonly IEye _currentEye;
        private readonly ITransformContext _transformContext;

        public DrawingHandleSpriteStacking(DrawingHandleBase baseHandle, IEye currentEye, Box2 bounds,
            ITransformContext transformContext, DrawingSpriteStackingContext drawingContext)
        {
            _baseHandle = baseHandle;
            _currentEye = currentEye;
            _bounds = bounds;
            _transformContext = transformContext;
            _drawingContext = drawingContext;

            _drawingContext._rotTransEye =
                Matrix3x2.CreateTranslation(-_currentEye.Position.Position) *
                Matrix3x2.CreateRotation((float)_currentEye.Rotation) *
                Matrix3x2.CreateTranslation(_currentEye.Position.Position);

            Matrix3x2.Invert(_drawingContext._rotTransEye, out _drawingContext._rotTransEyeNeg);
        }

        public void Dispose()
        {
            Flush();
        }

        public void DrawLayer(Vector2 position, int zlevel, Angle rotation, UIBox2 textureRegion, Vector2? scale = null)
        {
            if (_drawingContext.LayerPerZ == 1)
            {
                _drawLayer(position, zlevel, 0, rotation, textureRegion, scale);
                return;
            }

            var drawTranslate = 1f / _drawingContext.LayerPerZ;

            for (var i = 0; i < _drawingContext.LayerPerZ; i++)
                _drawLayer(position, zlevel, i * drawTranslate, rotation, textureRegion, scale);
        }

        private void _drawLayer(Vector2 position, int zlevel, float translateZ, Angle rotation, UIBox2 textureRegion,
            Vector2? scale = null)
        {
            position += _currentEye.Offset;

            _drawingContext._currScale = scale ?? textureRegion.Size / EyeManager.PixelsPerMeter;

            _drawingContext._processingVertices[0].Position = position; //LeftTop
            _drawingContext._processingVertices[2].Position = position + _drawingContext._currScale; //RightBottom

            _drawingContext._processingVertices[1].Position =
                new Vector2(_drawingContext._processingVertices[0].Position.X,
                    _drawingContext._processingVertices[2].Position.Y); //LeftBottom
            _drawingContext._processingVertices[3].Position =
                new Vector2(_drawingContext._processingVertices[2].Position.X,
                    _drawingContext._processingVertices[0].Position.Y); //RightTop

            _drawingContext._center = _drawingContext._processingVertices[0].Position + _drawingContext._currScale / 2f;
            _drawingContext._rotTrans = Matrix3x2.CreateTranslation(-_drawingContext._center) *
                                        Matrix3x2.CreateRotation((float)rotation) *
                                        Matrix3x2.CreateTranslation(_drawingContext._center);

            TransformPoints(_drawingContext._rotTrans * _drawingContext._rotTransEye);

            _drawingContext._processingVertices[0].Position =
                _transformContext.Transform(_drawingContext._processingVertices[0].Position, zlevel + translateZ,
                    _currentEye);
            _drawingContext._processingVertices[1].Position =
                _transformContext.Transform(_drawingContext._processingVertices[1].Position, zlevel + translateZ,
                    _currentEye);
            _drawingContext._processingVertices[2].Position =
                _transformContext.Transform(_drawingContext._processingVertices[2].Position, zlevel + translateZ,
                    _currentEye);
            _drawingContext._processingVertices[3].Position =
                _transformContext.Transform(_drawingContext._processingVertices[3].Position, zlevel + translateZ,
                    _currentEye);

            TransformPoints(_drawingContext._rotTransEyeNeg);

            if (!_bounds.Contains(_drawingContext._processingVertices[0].Position) &&
                !_bounds.Contains(_drawingContext._processingVertices[1].Position) &&
                !_bounds.Contains(_drawingContext._processingVertices[2].Position) &&
                !_bounds.Contains(_drawingContext._processingVertices[3].Position))
                return;

            _drawingContext._processingVertices[0].UV =
                textureRegion.TopLeft / _drawingContext._texture.Size + new Vector2(0.0001f, 0);
            _drawingContext._processingVertices[1].UV =
                textureRegion.BottomLeft / _drawingContext._texture.Size + new Vector2(0.0001f, 0);
 
            _drawingContext._processingVertices[2].UV = textureRegion.BottomRight / _drawingContext._texture.Size;
            _drawingContext._processingVertices[3].UV = textureRegion.TopRight / _drawingContext._texture.Size;

            _drawingContext.PushCurrentFace(zlevel);
        }

        private void TransformPoints(Matrix3x2 matrix)
        {
            _drawingContext._processingVertices[0].Position =
                Vector2.Transform(_drawingContext._processingVertices[0].Position, matrix);
            _drawingContext._processingVertices[1].Position =
                Vector2.Transform(_drawingContext._processingVertices[1].Position, matrix);
            _drawingContext._processingVertices[2].Position =
                Vector2.Transform(_drawingContext._processingVertices[2].Position, matrix);
            _drawingContext._processingVertices[3].Position =
                Vector2.Transform(_drawingContext._processingVertices[3].Position, matrix);
        }

        public void Flush()
        {
            try
            {
                var enumerator = _drawingContext.GetEnumerator();
                while (enumerator.MoveNext(out var vertexUv2DColors))
                    _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _drawingContext._texture,
                        vertexUv2DColors);
            }
            finally
            {
                _drawingContext.Clear();
            }
        }
    }
}