using Robust.Shared.Player;

namespace Content.Shared.StateManipulation;

public interface IContentStateManager
{
    public void Initialize();
    public void SetState(ICommonSession session, string type);
}