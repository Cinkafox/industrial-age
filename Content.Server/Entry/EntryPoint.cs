using Content.Server.Acz;
using Robust.Server.ServerStatus;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

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
}