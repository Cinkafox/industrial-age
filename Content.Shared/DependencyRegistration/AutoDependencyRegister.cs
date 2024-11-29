using System.Linq;
using Robust.Shared.Reflection;

namespace Content.Shared.DependencyRegistration;

public static class AutoDependencyRegister
{
    private static HashSet<Type> _injectDependenciesImplementations = new();
    
    private static HashSet<Type> _initializeImplementations = new();
    
    public static void Register()
    {
        foreach (var implementation in GetEnumerator<DependencyRegisterAttribute>())
        {
            var attribute = (DependencyRegisterAttribute) 
                Attribute.GetCustomAttribute(implementation, typeof(DependencyRegisterAttribute))!;

            var interfaceType = implementation;

            if (attribute.InterfaceType is not null) 
                interfaceType = attribute.InterfaceType;
            
            IoCManager.Instance!.Register(interfaceType, implementation);

            if (HasAttribute<InjectDependenciesAttribute>(implementation))
            {
                _injectDependenciesImplementations.Add(interfaceType);
            }

            if (implementation.GetInterfaces().Any(@interface => @interface == typeof(IDependencyInitialize)))
            {
                _initializeImplementations.Add(interfaceType);
            }
        }    
    }
    
    public static void Initialize()
    {
        foreach (var interfaceType in _injectDependenciesImplementations)
        {
            var instance = IoCManager.ResolveType(interfaceType);
            IoCManager.InjectDependencies(instance);
        }

        foreach (var interfaceType in _initializeImplementations)
        {
            var instance = (IDependencyInitialize)
                IoCManager.ResolveType(interfaceType);
            
            instance.Initialize();
        }
    }

    private static IEnumerable<Type> GetEnumerator<T>() where T : Attribute
    {
        return IoCManager.Resolve<IReflectionManager>().FindTypesWithAttribute<T>();
    }

    private static bool HasAttribute<T>(Type implementation)
    {
        return Attribute.GetCustomAttribute(implementation, typeof(T)) != null;
    }
}

public interface IDependencyInitialize
{
    public void Initialize();
}