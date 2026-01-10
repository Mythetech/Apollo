using System.Reflection;
using Apollo.Components.Analysis;
using Microsoft.AspNetCore.Components;

namespace Apollo.Components.Preview;

/// <summary>
/// Resolves component types from the user's compiled assembly.
/// </summary>
public class ComponentTypeResolver
{
    private readonly UserAssemblyStore _assemblyStore;
    private Assembly? _loadedAssembly;
    private byte[]? _lastAssemblyBytes;

    public ComponentTypeResolver(UserAssemblyStore assemblyStore)
    {
        _assemblyStore = assemblyStore;
    }

    /// <summary>
    /// Gets a component type by name from the compiled assembly.
    /// </summary>
    public Type? GetComponentType(string componentName)
    {
        ReloadAssemblyIfNeeded();

        if (_loadedAssembly == null)
            return null;

        return _loadedAssembly.GetTypes()
            .FirstOrDefault(t =>
                t.Name.Equals(componentName, StringComparison.OrdinalIgnoreCase) &&
                typeof(IComponent).IsAssignableFrom(t) &&
                !t.IsAbstract);
    }

    /// <summary>
    /// Gets all available component types from the compiled assembly.
    /// </summary>
    public IEnumerable<ComponentInfo> GetAllComponentTypes()
    {
        ReloadAssemblyIfNeeded();

        if (_loadedAssembly == null)
            return Enumerable.Empty<ComponentInfo>();

        return _loadedAssembly.GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)
            .Select(t => new ComponentInfo
            {
                Name = t.Name,
                FullName = t.FullName ?? t.Name,
                Type = t,
                Parameters = GetComponentParameters(t)
            })
            .OrderBy(c => c.Name);
    }

    /// <summary>
    /// Gets the parameters for a component type.
    /// </summary>
    public IEnumerable<ComponentParameterInfo> GetComponentParameters(Type componentType)
    {
        return componentType.GetProperties()
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
            .Select(p => new ComponentParameterInfo
            {
                Name = p.Name,
                PropertyType = p.PropertyType,
                IsRequired = p.GetCustomAttribute<EditorRequiredAttribute>() != null,
                DefaultValue = GetDefaultValue(p.PropertyType)
            });
    }

    /// <summary>
    /// Gets default parameter values for a component type.
    /// </summary>
    public Dictionary<string, object?> GetDefaultParameters(Type? componentType)
    {
        if (componentType == null)
            return new Dictionary<string, object?>();

        var parameters = new Dictionary<string, object?>();

        foreach (var prop in componentType.GetProperties())
        {
            var paramAttr = prop.GetCustomAttribute<ParameterAttribute>();
            if (paramAttr != null)
            {
                parameters[prop.Name] = GetDefaultValue(prop.PropertyType);
            }
        }

        return parameters;
    }

    private object? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(bool))
            return false;
        if (type == typeof(int))
            return 0;
        if (type == typeof(double))
            return 0.0;
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }

    private void ReloadAssemblyIfNeeded()
    {
        var currentBytes = _assemblyStore.CurrentAssembly;

        if (currentBytes == null)
        {
            _loadedAssembly = null;
            _lastAssemblyBytes = null;
            return;
        }

        // Only reload if the assembly has changed
        if (_lastAssemblyBytes != null && currentBytes.SequenceEqual(_lastAssemblyBytes))
            return;

        try
        {
            _loadedAssembly = Assembly.Load(currentBytes);
            _lastAssemblyBytes = currentBytes;
        }
        catch (Exception)
        {
            _loadedAssembly = null;
            _lastAssemblyBytes = null;
        }
    }
}

/// <summary>
/// Information about a discovered component.
/// </summary>
public class ComponentInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Type? Type { get; set; }
    public IEnumerable<ComponentParameterInfo> Parameters { get; set; } = Enumerable.Empty<ComponentParameterInfo>();
}

/// <summary>
/// Information about a component parameter.
/// </summary>
public class ComponentParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public Type PropertyType { get; set; } = typeof(object);
    public bool IsRequired { get; set; }
    public object? DefaultValue { get; set; }
}
