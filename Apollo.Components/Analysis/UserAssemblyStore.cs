namespace Apollo.Components.Analysis;

public class UserAssemblyStore
{
    private byte[]? _currentAssembly;
    private readonly object _lock = new();
    
    public event Func<byte[], Task>? OnAssemblyUpdated;

    public byte[]? CurrentAssembly
    {
        get
        {
            lock (_lock)
            {
                return _currentAssembly;
            }
        }
    }

    public bool HasAssembly
    {
        get
        {
            lock (_lock)
            {
                return _currentAssembly != null && _currentAssembly.Length > 0;
            }
        }
    }

    public async Task UpdateAssemblyAsync(byte[]? assemblyBytes)
    {
        lock (_lock)
        {
            _currentAssembly = assemblyBytes;
        }

        if (OnAssemblyUpdated != null && assemblyBytes != null)
        {
            await OnAssemblyUpdated.Invoke(assemblyBytes);
        }
    }

    public void ClearAssembly()
    {
        lock (_lock)
        {
            _currentAssembly = null;
        }
    }
}

