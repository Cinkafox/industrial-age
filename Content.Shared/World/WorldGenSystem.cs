using System.Linq;
using System.Numerics;
using Content.Shared.GameTicking;
using Content.Shared.StateManipulation;
using Content.Shared.Tile;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.World;

public sealed class WorldGenSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GameInitializedEvent>(OnGameInitialized);
        SubscribeLocalEvent<WorldGenComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MapGridComponent, ChunkCreatedEvent>(OnChunkCreated);
        SubscribeLocalEvent<WorldLoaderComponent, MoveEvent>(OnLoaderMove);
    }

    private void OnLoaderMove(Entity<WorldLoaderComponent> ent, ref MoveEvent args)
    {
        var parent = args.Entity.Comp1.ParentUid;
        if(!TryComp<WorldGenComponent>(parent, out var worldGenComponent)) return;

        var center = (Vector2i)args.NewPosition.Position / 8;
        
        RegenerateChunks(new Entity<WorldGenComponent>(parent, worldGenComponent),center, 3);
    }

    private void OnChunkCreated(Entity<MapGridComponent> ent, ref ChunkCreatedEvent args)
    {
        foreach (var (localPos, entry) in args.Chunk.GetEntries())
        {
            var realPos = localPos + args.ChunkPos * WorldChunk.ChunkSize;
            _mapSystem.SetTile(ent, realPos, entry.Tile);
            foreach (var entity in entry.Entities)
            {
                var uid = Spawn(entity.Entity);
                _transformSystem.SetParent(uid, ent);
                _transformSystem.SetLocalPosition(uid, realPos + entity.Offset + Vector2.One / 2);
                _transformSystem.SetLocalRotation(uid, entity.Rotation);
            }
        }
    }

    private void OnComponentInit(Entity<WorldGenComponent> ent, ref ComponentInit args)
    {
        if(ent.Comp.WorldGenData == default)
            ent.Comp.WorldGenData = _prototypeManager.Index(ent.Comp.WorldGenPrototype).Data;
    }

    private void OnGameInitialized(GameInitializedEvent ev)
    {
        var worldComp = AddComp<WorldGenComponent>(ev.GridUid);
        
        RegenerateChunks(new Entity<WorldGenComponent>(ev.GridUid, worldComp), new Vector2i(0,0),4);
    }

    public void RegenerateChunks(Entity<WorldGenComponent> entity,Vector2i chunkPos, int radius)
    {
        for (int x = -radius; x < radius; x++)
        {
            for (int y = -radius; y < radius; y++)
            {
                GetChunk(entity, chunkPos + new Vector2i(x, y));
            }   
        }
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
                chunk.Entries[x * WorldChunk.ChunkSize + y] = GetEntry(ent,pos * WorldChunk.ChunkSize + new Vector2i(x, y));
            }
        }
        
        ent.Comp.LoadedChunks.Add(pos, chunk);
        
        RaiseLocalEvent(ent, new ChunkCreatedEvent(pos, chunk));
        return chunk;
    }
    
    private WorldTileEntry GetEntry(Entity<WorldGenComponent> ent, Vector2i pos)
    {
        var height = GetNoise(ent, pos);
        var tileProto = GetTile(ent, height);
       
        if(!_tileDefinitionManager.TryGetDefinition(tileProto, out var definition)) 
            return new WorldTileEntry(height, new Robust.Shared.Map.Tile(0), tileProto);
       
        var tile = _tileDefinitionManager.GetVariantTile(definition, _robustRandom);

        var entry = new WorldTileEntry(height, tile, tileProto);

        foreach (var addition in ent.Comp.WorldGenData.Additions)
        {
            addition.Invoke(ent.Comp.WorldGenData, entry, pos, _robustRandom);
        }
        
        RaiseLocalEvent(ent, new TileEntryLoading(entry, pos));
        
        return entry;
    }


    private float GetNoise(Entity<WorldGenComponent> ent, Vector2i pos)
    {
        var comp = ent.Comp;
        var sum = 0f;
        var amplitudeSum = 0f;
        
        foreach (var amplitude in comp.WorldGenData!.NoiseWorld)
        {
            sum += amplitude.Noise.GetNoise(pos.X, pos.Y) * amplitude.Amplitude;
            amplitudeSum += amplitude.Amplitude;
        }
        
        sum = sum / amplitudeSum;

        return float.Pow(sum, comp.WorldGenData.Redistribution);
    }

    private ProtoId<ContentTileDefinition> GetTile(Entity<WorldGenComponent> ent, float height)
    {
        var tileElevation = ent.Comp.WorldGenData!.TileElevation;
        var heights = tileElevation.Keys.ToList();

        var first = 0f;
        var second = 0f;

        for (int i = 0; i < heights.Count-1; i++)
        {
            first = heights[i];
            second = heights[i + 1];
            if (height > first && height <= second) return tileElevation[first];
        }
        
        if(height > second) 
            return tileElevation[second];

        return ent.Comp.WorldGenData!.DefaultTile;
    }
}

public record struct ChunkCreatedEvent(Vector2i ChunkPos, WorldChunk Chunk);

public record struct TileEntryLoading(WorldTileEntry Entry, Vector2i Pos);