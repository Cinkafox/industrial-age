using Content.Shared.DependencyRegistration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.StateManipulation;

[InjectDependencies]
public abstract class SharedContentStateManager : IContentStateManager, IDependencyInitialize
{
   [Dependency] protected readonly INetManager NetManager = default!;

   public virtual void Initialize()
   {
   }

   public abstract void SetState(ICommonSession session, string type);
}