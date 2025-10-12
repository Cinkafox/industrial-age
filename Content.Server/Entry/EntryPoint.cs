using Content.Server.Acz;
using Content.Server.GameTicking;
using Robust.Server.ServerStatus;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Entry;

public sealed class EntryPoint : GameServer
{
    public override void PreInit()
    {
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }

    public override void Init()
    {
        var aczProvider = new ContentMagicAczProvider(IoCManager.Resolve<IDependencyCollection>());
        IoCManager.Resolve<IStatusHost>().SetMagicAczProvider(aczProvider);
    }

    public override void PostInit()
    {
        IoCManager.Resolve<EntityManager>().System<GameTicker>().InitializeGame();
    }
}