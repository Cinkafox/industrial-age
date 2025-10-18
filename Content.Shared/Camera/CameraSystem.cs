namespace Content.Shared.Camera;

public sealed class CameraSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _sharedEyeSystem = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CameraFollowComponent, EyeComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var camera, out var eye, out var transform))
        {
            var targetAngle = - transform.LocalRotation ;
            var delta = Angle.ShortestDistance(camera.CameraAngle, targetAngle);
            if (double.Abs(delta) > Angle.FromDegrees(5))
                delta /= 3;
            camera.CameraAngle += delta;
            
            _sharedEyeSystem.SetRotation(uid, camera.CameraAngle, eye);
        }
    }
}