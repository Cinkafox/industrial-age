using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YamlDotNet.RepresentationModel;

namespace Content.Client.SpriteStacking;

public sealed partial class SpriteStackingMetadata
{
    public readonly Vector2i Size;
    public readonly int Height;
    public readonly string[] States;

    public SpriteStackingMetadata(Vector2i size, string[] states, int height)
    {
        Size = size;
        States = states;
        Height = height;
    }

    public static SpriteStackingMetadata ReadStream(YamlDocument document, ISerializationManager serializationManager)
    {
        var manifestRaw =
            serializationManager.Read<SpriteStackingRawMetadata>(document.RootNode.ToDataNode(), notNullableOverride:true);

        if (manifestRaw is null) throw new Exception("Invalid manifest file!");
        if (manifestRaw.Height < 0) throw new Exception("Invalid height!");
        if (manifestRaw.Size.X <= 1 || manifestRaw.Size.Y <= 1) throw new Exception("Invalid size of sprite!");

        return new SpriteStackingMetadata((Vector2i) manifestRaw.Size, manifestRaw.States, manifestRaw.Height);
    }

    [Serializable, DataDefinition]
    sealed partial class SpriteStackingRawMetadata
    {
        [DataField] public Vector2i Size;
        [DataField] public int Height;
        [DataField] public string[] States = [];
    }
}

public sealed class SpriteStackingData
{
    public SpriteStackingMetadata Metadata { get; }
    public Dictionary<string, Image<Rgba32>> States = new();
    
    public SpriteStackingData(SpriteStackingMetadata metadata)
    {
        Metadata = metadata;
    }
}

public sealed class SpriteStackingResource : BaseResource
{
    public SpriteStackingData Data = default!;
    private LoadingContext _currentContext = new();
    public override void Load(IDependencyCollection dependencies, ResPath path)
    {
        _currentContext.ResolveContext(dependencies);
        _currentContext.Path = path;
        
        StackPreLoad();
    }

    private void StackPreLoad()
    {
        var manifestPath = _currentContext.Path / "meta.yml";
        var manifestFile = _currentContext._resMan.ContentFileReadYaml(manifestPath);
        var metadata = SpriteStackingMetadata.ReadStream(manifestFile.Documents[0], _currentContext._serMan);
        
        Data = new SpriteStackingData(metadata);

        foreach (var state in metadata.States)
        {
            var path = _currentContext.Path / (state + ".stack.png");
            if (!_currentContext._resCache.TryGetResource<TextureResource>(path, out var textureResource))
            {
                _currentContext.Logger.Error($"Texture in ${path} not found!");
                continue;
            }

            Data.States[state] = Image.Load<Rgba32>(_currentContext._resMan.ContentFileRead(path));
        }
        //TODO: Make some atlas think for optimisation purpose
    }
    
    private sealed class LoadingContext
    {
        public ResPath Path = default!;
        public IResourceManager _resMan = default!;
        public IResourceCache _resCache = default!;
        public ISerializationManager _serMan = default!;
        
        public ISawmill Logger = default!;

        public void ResolveContext(IDependencyCollection dependencies)
        {
            _resMan = dependencies.Resolve<IResourceManager>();
            _resCache = dependencies.Resolve<IResourceCache>();
            _serMan = dependencies.Resolve<ISerializationManager>();
            Logger = Robust.Shared.Log.Logger.GetSawmill("Stack.LoadingContext");
        }
    }
}