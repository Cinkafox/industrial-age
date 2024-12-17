using Content.Shared.Movement;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Movement;

/// <summary>
/// Handles setting sprite states based on whether an entity has movement input.
/// </summary>
public sealed class SpriteMovementSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpriteMovementComponent, MoveInputEvent>(OnSpriteMoveInput);
        _spriteQuery = GetEntityQuery<SpriteComponent>();
    }

    private void OnSpriteMoveInput(EntityUid uid, SpriteMovementComponent component, ref MoveInputEvent args)
    {
        var oldMoving = args.OldMagnitude != 0;
        var moving = args.CurrentMagnitude != 0;

        if (oldMoving == moving || !_spriteQuery.TryGetComponent(uid, out var sprite))
            return;

        if (moving)
        {
            foreach (var (layer, state) in component.MovementLayers)
            {
                sprite.LayerSetData(layer, state);
            }
        }
        else
        {
            foreach (var (layer, state) in component.NoMovementLayers)
            {
                sprite.LayerSetData(layer, state);
            }
        }
    }
}