using Content.Shared.Movement;

namespace Content.Shared.Vehicle;

public partial class VehicleSystem
{
    public void UpdateControl(float frameTime)
    {
        var query = EntityQueryEnumerator<InputMoverComponent,VehicleComponent>();
        while (query.MoveNext(out var inputMoverComponent,out var vehicleComponent))
        {
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Nitro))
            {
                if (_indicatorSystem.TrySubtract(vehicleComponent.NitroIndicator, (float)vehicleComponent.Power/10f))
                    vehicleComponent.AccelerationMul = vehicleComponent.AccelerationFactor;
                else
                    vehicleComponent.AccelerationMul = 1;
            }
            else
            {
                _indicatorSystem.TryAdd(vehicleComponent.NitroIndicator, 5);
                vehicleComponent.AccelerationMul = 1;
            }
            
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Up) && !inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Break))
            {
                if(vehicleComponent.Power + vehicleComponent.PowerFactor * vehicleComponent.AccelerationMul < vehicleComponent.MaxPower * vehicleComponent.AccelerationMul)
                    vehicleComponent.Power += vehicleComponent.PowerFactor * vehicleComponent.AccelerationMul;
            }
            else
            {
                vehicleComponent.Power -= vehicleComponent.PassiveBreakFactor;
            }
            
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Down) && !inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Break))
            {
                vehicleComponent.Reverse += vehicleComponent.ReverseFactor;
            }
            else
            {
                vehicleComponent.Reverse -= vehicleComponent.PassiveBreakFactor;
            }

            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Break))
            {
                vehicleComponent.Reverse -= vehicleComponent.BreakFactor;
                vehicleComponent.Power -= vehicleComponent.BreakFactor;
                vehicleComponent.Turn = vehicleComponent.TurnOnBreakFactor;
            }
            else
            {
                vehicleComponent.Turn = vehicleComponent.TurnSpeedFactor * (vehicleComponent.Power + vehicleComponent.Reverse);
            }

            vehicleComponent.Turn = double.Clamp(vehicleComponent.Turn, 0, vehicleComponent.TurnMax);
            vehicleComponent.Power = double.Max(vehicleComponent.Power, 0);
            vehicleComponent.Reverse = double.Clamp(vehicleComponent.Reverse, 0, vehicleComponent.MaxReverse * vehicleComponent.AccelerationMul);

            var direction = vehicleComponent.Power >= vehicleComponent.Reverse ? 1 : -1;
            vehicleComponent.Turn = direction * vehicleComponent.Turn;
            
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Left))
            {
                vehicleComponent.AngularVelocity += vehicleComponent.Turn;
            }
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Right))
            {
                vehicleComponent.AngularVelocity -= vehicleComponent.Turn;
            }
        }
    }
}