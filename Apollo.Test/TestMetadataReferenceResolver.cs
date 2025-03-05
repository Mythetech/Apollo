using System.Reflection;
using Apollo.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Apollo.Test;

public class TestMetadataReferenceResolver : IMetadataReferenceResolver
{
    private readonly Dictionary<string, PortableExecutableReference> _referenceCache = new();

    public TestMetadataReferenceResolver()
    {
        // Pre-populate with common references needed for tests
        //AddReference(typeof(object));  // mscorlib/System.Runtime
        //AddReference(typeof(Console)); // System.Console
        //AddReference(typeof(System.Linq.Enumerable)); // System.Linq
        //AddReference(typeof(System.ComponentModel.INotifyPropertyChanged)); // System.ComponentModel
        //AddReference(typeof(Task)); // System.Threading.Tasks
        //AddReference(typeof(System.Collections.Generic.List<>)); // System.Collections
    }

    private void AddReference(Type type)
    {
        var assembly = type.Assembly;
        var name = assembly.GetName().Name + ".wasm";
        if (!_referenceCache.ContainsKey(name))
        {
            _referenceCache[name] = MetadataReference.CreateFromFile(assembly.Location);
        }
    }

    public Task<PortableExecutableReference> GetMetadataReferenceAsync(string wasmModule)
    {
        // Strip .wasm extension if present to match assembly names
        var moduleName = wasmModule.EndsWith(".wasm") 
            ? wasmModule 
            : wasmModule + ".wasm";

        if (_referenceCache.TryGetValue(moduleName, out var reference))
        {
            return Task.FromResult(reference);
        }

        try
        {
            var assembly = Assembly.Load(moduleName.Replace(".wasm", ""));
            var newRef = MetadataReference.CreateFromFile(assembly.Location);
            _referenceCache[moduleName] = newRef;
            return Task.FromResult(newRef);
        }
        catch
        {
            return Task.FromResult(_referenceCache["System.Runtime.wasm"]);
        }
    }

    public void AddCustomReference(string moduleName, PortableExecutableReference reference)
    {
        var name = moduleName.EndsWith(".wasm") ? moduleName : moduleName + ".wasm";
        _referenceCache[name] = reference;
    }
}

