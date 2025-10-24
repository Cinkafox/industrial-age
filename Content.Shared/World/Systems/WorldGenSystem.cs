using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Entry;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.World.Systems;

public sealed class WorldGenSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;

    private Robust.Shared.Map.Tile _voidTile = new Robust.Shared.Map.Tile(0);
    
    public override void Initialize()
    {
        SubscribeLocalEvent<WorldGenComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MapGridComponent, ChunkCreatedEvent>(OnChunkCreated);
        SubscribeLocalEvent<WorldLoaderComponent, MoveEvent>(OnLoaderMove);
        SubscribeLocalEvent<WorldLoaderComponent, ComponentInit>(OnLoaderInit);
    }

    private void OnLoaderInit(Entity<WorldLoaderComponent> ent, ref ComponentInit args)
    {
        if(!TryGetWorldByEntity(ent, out var world))
            return;
        
        RegenerateChunks(world.Value, GetChunkPosition(ent), ent.Comp.Radius);
    }

    private void OnLoaderMove(Entity<WorldLoaderComponent> ent, ref MoveEvent args)
    {
        var parent = args.Entity.Comp1.ParentUid;
        if(!TryComp<WorldGenComponent>(parent, out var worldGenComponent)) return;
        var parentEnt = new Entity<WorldGenComponent>(parent, worldGenComponent);

        var center = (Vector2i)args.NewPosition.Position / 8;
        
        var loaded = RegenerateChunks(parentEnt, center, ent.Comp.Radius);
        foreach (var loadedChunk in worldGenComponent.LoadedChunks.Keys.ToList())
        {
            if(loaded.Contains(loadedChunk)) continue;
            UnloadChunk(parentEnt, loadedChunk);
        }
    }

    private void OnChunkCreated(Entity<MapGridComponent> ent, ref ChunkCreatedEvent args)
    {
        var tileList = new List<(Vector2i Position, Robust.Shared.Map.Tile Tile)>();
        var entList = new List<(Vector2i Position, EntitySpawnContext)>();
        
        foreach (var (localPos, entry) in args.Chunk.GetEntries())
        {
            var realPos = localPos + args.ChunkPos * WorldChunk.ChunkSize;
            tileList.Add((realPos, entry.Tile));
            entList.AddRange(entry.Entities.Select(entity => (realPos, entity)));
        }
        
        _mapSystem.SetTiles(ent, tileList);

        foreach (var (pos,entity) in entList)
        {
            var uid = Spawn(entity.Entity);
            _transformSystem.SetParent(uid, ent);
            _transformSystem.SetLocalPosition(uid, pos + entity.Offset + Vector2.One / 2);
            _transformSystem.SetLocalRotation(uid, entity.Rotation);
            _transformSystem.AnchorEntity(uid);
            _physicsSystem.SetCanCollide(uid, true);
            args.Chunk.LoadedEntities.Add(GetNetEntity(uid));
        }
    }

    private void OnComponentInit(Entity<WorldGenComponent> ent, ref ComponentInit args)
    {
        if(ent.Comp.WorldGenData == default)
            ent.Comp.WorldGenData = _prototypeManager.Index(ent.Comp.WorldGenPrototype).Data;
        if(ent.Comp.Seed != 0)
            ent.Comp.WorldGenData.Seed = ent.Comp.Seed;
        else
            ent.Comp.WorldGenData.Seed = new Random(DateTime.Now.Second).Next();
    }

    public void UnloadChunk(Entity<WorldGenComponent> ent, Vector2i chunkPos)
    {
        if(!ent.Comp.LoadedChunks.TryGetValue(chunkPos, out var loadedChunk))
            return;

        if (!TryComp<MapGridComponent>(ent, out var gridComponent))
            throw new Exception();

        foreach (var entity in loadedChunk.LoadedEntities)
        {
            Del(GetEntity(entity));
        }
        
        var tileList = new List<(Vector2i Position, Robust.Shared.Map.Tile Tile)>();
        
        foreach (var (localPos, entry) in loadedChunk.GetEntries())
        {
            var realPos = localPos + chunkPos * WorldChunk.ChunkSize;
            tileList.Add((realPos, _voidTile));
        }
        
        _mapSystem.SetTiles(ent, gridComponent, tileList);
        
        ent.Comp.LoadedChunks.Remove(chunkPos);
    }

    public List<Vector2i> RegenerateChunks(Entity<WorldGenComponent> entity,Vector2i chunkPos, int radius)
    {
        var loadedChunks = new List<Vector2i>();
        for (int x = -radius; x < radius; x++)
        {
            for (int y = -radius; y < radius; y++)
            {
                var pos = chunkPos + new Vector2i(x, y);
                GetChunk(entity, pos);
                loadedChunks.Add(pos);
            }   
        }
        return loadedChunks;
    }

    public WorldChunk GetChunk(Entity<WorldGenComponent> ent, Vector2i pos)
    {
        if (ent.Comp.LoadedChunks.TryGetValue(pos, out var chunk))
        {
            return chunk;
        }

        chunk = new WorldChunk();
        
        for (int x = 0; x < WorldChunk.ChunkSize; x++)
        {
            for (int y = 0; y < WorldChunk.ChunkSize; y++)
            {
                var localPos = pos * WorldChunk.ChunkSize + new Vector2i(x, y);
                
                var entry = GetEntry(ent, localPos);
                chunk.Entries[x * WorldChunk.ChunkSize + y] = entry;
            }
        }
        
        RaiseLocalEvent(ent, new ChunkLoadingEvent(pos, chunk));
        
        ent.Comp.LoadedChunks.Add(pos, chunk);
        
        RaiseLocalEvent(ent, new ChunkCreatedEvent(pos, chunk));
        return chunk;
    }

    public Vector2i GetChunkPosition(Vector2 pos)
    {
        return (Vector2i)(pos / WorldChunk.ChunkSize);
    }

    public Vector2i GetChunkPosition(EntityUid uid)
    {
        return GetChunkPosition(_transformSystem.GetWorldPosition(uid));
    }
    
    internal WorldTileEntry GetEntry(Entity<WorldGenComponent> ent, Vector2i pos)
    {
        var height = ent.Comp.WorldGenData.GetNoise(pos);
        var tileProto = ent.Comp.WorldGenData.GetTile(height);
       
        if(!_tileDefinitionManager.TryGetDefinition(tileProto, out var definition)) 
            return new WorldTileEntry(height, new Robust.Shared.Map.Tile(0), tileProto);
        
        var tile = _tileDefinitionManager.GetVariantTile(definition, _robustRandom);

        var entry = new WorldTileEntry(height, tile, tileProto);

        foreach (var addition in ent.Comp.WorldGenData.Additions)
        {
            addition.Invoke(ent.Comp.WorldGenData, entry, pos);
        }
        
        return entry;
    }

    private bool TryGetWorldByEntity(EntityUid child,[NotNullWhen(true)] out Entity<WorldGenComponent>? worldGen)
    {
        worldGen = null;
        var transform = Transform(child);
        if (!TryComp<WorldGenComponent>(transform.ParentUid, out var worldGenComp)) 
            return false;
        worldGen = new Entity<WorldGenComponent>(transform.ParentUid, worldGenComp);
        return true;
    }
    
}


public record struct ChunkLoadingEvent(Vector2i ChunkPos, WorldChunk Chunk);

public record struct ChunkCreatedEvent(Vector2i ChunkPos, WorldChunk Chunk);
