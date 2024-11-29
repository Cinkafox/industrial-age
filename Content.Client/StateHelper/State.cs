using Robust.Client.State;

namespace Content.Client.StateHelper;

public abstract class State<T> : State
{
    protected override Type LinkedScreenType => typeof(T);
}