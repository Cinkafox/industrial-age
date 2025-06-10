namespace Content.Shared.DependencyRegistration;

[AttributeUsage(AttributeTargets.Class)]
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

[AttributeUsage(AttributeTargets.Class)]
public sealed class InjectDependenciesAttribute : Attribute
{
    public InjectDependenciesAttribute()
    {
    }
}