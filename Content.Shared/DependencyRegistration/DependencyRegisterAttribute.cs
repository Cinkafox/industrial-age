namespace Content.Shared.DependencyRegistration;

public sealed class DependencyRegisterAttribute : Attribute
{
    public Type? InterfaceType;

    public DependencyRegisterAttribute()
    {
    }

    public DependencyRegisterAttribute(Type interfaceType)
    {
        InterfaceType = interfaceType;
    }
}

public sealed class InjectDependenciesAttribute : Attribute
{
    public InjectDependenciesAttribute()
    {
    }
}