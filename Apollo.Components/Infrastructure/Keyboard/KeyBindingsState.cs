using Blazored.LocalStorage;

namespace Apollo.Components.Infrastructure.Keyboard;

public class KeyBindingsState
{
    private const string StorageKey = "__apollo_keybindings";
    
    private readonly ILocalStorageService _localStorageService;
    private List<KeyBinding> _keyBindings = [];
    private bool _loaded;
    
    public event Action? OnKeyBindingsChanged;

    public KeyBindingsState(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    public IReadOnlyList<KeyBinding> KeyBindings => _keyBindings;

    public async Task LoadFromStorageAsync()
    {
        if (_loaded) return;
        
        var stored = await _localStorageService.GetItemAsync<List<StoredKeyBinding>>(StorageKey);
        
        if (stored is { Count: > 0 })
        {
            _keyBindings = stored.Select(s => s.ToKeyBinding()).ToList();
        }
        else
        {
            _keyBindings = DefaultKeyBindings.GetDefaults();
        }
        
        _loaded = true;
    }

    public async Task UpdateKeyBindingAsync(KeyBinding binding)
    {
        var existing = _keyBindings.FirstOrDefault(k => k.Id == binding.Id);
        if (existing != null)
        {
            var index = _keyBindings.IndexOf(existing);
            _keyBindings[index] = binding;
        }
        else
        {
            _keyBindings.Add(binding);
        }
        
        await SaveToStorageAsync();
        OnKeyBindingsChanged?.Invoke();
    }

    public async Task DeleteKeyBindingAsync(KeyBinding binding)
    {
        var existing = _keyBindings.FirstOrDefault(k => k.Id == binding.Id);
        if (existing != null)
        {
            _keyBindings.Remove(existing);
            await SaveToStorageAsync();
            OnKeyBindingsChanged?.Invoke();
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        _keyBindings = DefaultKeyBindings.GetDefaults();
        await SaveToStorageAsync();
        OnKeyBindingsChanged?.Invoke();
    }

    public bool IsDefaultBinding(KeyBinding binding)
    {
        return DefaultKeyBindings.GetDefaults().Any(d => d.Id == binding.Id);
    }

    public KeyBinding? GetBindingForAction(KeyBindingAction action)
    {
        return _keyBindings.FirstOrDefault(k => k.Action == action);
    }

    private async Task SaveToStorageAsync()
    {
        var stored = _keyBindings.Select(k => new StoredKeyBinding
        {
            Id = k.Id,
            Ctrl = k.Ctrl,
            Alt = k.Alt,
            Shift = k.Shift,
            Meta = k.Meta,
            Key = k.Key.ToString(),
            Action = k.Action.ToString()
        }).ToList();
        
        await _localStorageService.SetItemAsync(StorageKey, stored);
    }
}

internal class StoredKeyBinding
{
    public string Id { get; set; } = "";
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Meta { get; set; }
    public string Key { get; set; } = "";
    public string Action { get; set; } = "";

    public KeyBinding ToKeyBinding() => new()
    {
        Id = Id,
        Ctrl = Ctrl,
        Alt = Alt,
        Shift = Shift,
        Meta = Meta,
        Key = Enum.TryParse<Microsoft.FluentUI.AspNetCore.Components.KeyCode>(Key, out var key) ? key : default,
        Action = Enum.TryParse<KeyBindingAction>(Action, out var action) ? action : KeyBindingAction.None
    };
}

