using System.Numerics;
using Content.Shared.PlayerIndicator;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly PlayerIndicatorSystem _indicatorSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<VehicleComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NitroIndicator = _indicatorSystem.AddIndicator(ent.Owner, "EntIndicatorNitro");
    }

    private void OnCollide(Entity<VehicleComponent> ent, ref StartCollideEvent args)
    {
        var collideDot = - Vector2.Dot(Vector2.Normalize(args.OurBody.LinearVelocity), args.WorldNormal);
        ent.Comp.Drag = double.Max(0, ent.Comp.Drag - collideDot);
    }

    public override void Update(float frameTime)
    {
        UpdateControl(frameTime);

        var query = EntityQueryEnumerator<VehicleComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var vehicleComponent, out var transformComponent, out var physicsComponent))
        {
            var angle =  transformComponent.LocalRotation - Angle.FromDegrees(270);
            var velocity = angle.ToVec() * (float)(vehicleComponent.Power - vehicleComponent.Reverse);
            
            _physicsSystem.SetLinearVelocity(uid, velocity);
            _physicsSystem.SetAngularVelocity(uid, (float)vehicleComponent.AngularVelocity);
            _physicsSystem.SetAngularVelocity(uid, (float)(physicsComponent.AngularVelocity * vehicleComponent.Drag));

            vehicleComponent.AngularVelocity *= vehicleComponent.AngularDrag;
        }
    }
    
}