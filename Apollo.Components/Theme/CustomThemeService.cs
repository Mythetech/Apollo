using Blazored.LocalStorage;
using MudBlazor;

namespace Apollo.Components.Theme;

public class CustomThemeService
{
    private const string StorageKey = "__apollo_custom_themes";
    
    private readonly ILocalStorageService _localStorage;
    private readonly ISnackbar _snackbar;
    private readonly Dictionary<string, CustomTheme> _themes = new();
    
    public event Action? OnThemesChanged;
    
    public IReadOnlyDictionary<string, CustomTheme> Themes => _themes;
    
    public CustomThemeService(ILocalStorageService localStorage, ISnackbar snackbar)
    {
        _localStorage = localStorage;
        _snackbar = snackbar;
    }
    
    public async Task LoadFromStorageAsync()
    {
        try
        {
            var json = await _localStorage.GetItemAsStringAsync(StorageKey);
            if (string.IsNullOrEmpty(json)) return;
            
            var themes = CustomThemeSerializer.DeserializeMany(json);
            if (themes == null) return;
            
            _themes.Clear();
            foreach (var data in themes)
            {
                var theme = new CustomTheme(data);
                _themes[data.Id] = theme;
            }
            
            OnThemesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load custom themes: {ex.Message}");
        }
    }
    
    public async Task SaveToStorageAsync()
    {
        try
        {
            var dataList = _themes.Values.Select(t => t.Data).ToList();
            var json = CustomThemeSerializer.SerializeMany(dataList);
            await _localStorage.SetItemAsStringAsync(StorageKey, json);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to save custom themes: {ex.Message}");
        }
    }
    
    public async Task<CustomTheme> AddThemeAsync(CustomThemeData data)
    {
        var theme = new CustomTheme(data);
        _themes[data.Id] = theme;
        await SaveToStorageAsync();
        OnThemesChanged?.Invoke();
        return theme;
    }
    
    public async Task<CustomTheme?> UpdateThemeAsync(string id, CustomThemeData newData)
    {
        if (!_themes.TryGetValue(id, out var existing)) return null;
        
        var updatedData = newData with 
        { 
            Id = id, 
            CreatedAt = existing.Data.CreatedAt,
            ModifiedAt = DateTimeOffset.UtcNow 
        };
        
        var updatedTheme = new CustomTheme(updatedData);
        _themes[id] = updatedTheme;
        await SaveToStorageAsync();
        OnThemesChanged?.Invoke();
        return updatedTheme;
    }
    
    public async Task<bool> DeleteThemeAsync(string id)
    {
        if (!_themes.Remove(id)) return false;
        await SaveToStorageAsync();
        OnThemesChanged?.Invoke();
        return true;
    }
    
    public CustomTheme? GetTheme(string id) 
        => _themes.GetValueOrDefault(id);
    
    public CustomTheme? GetThemeByName(string name) 
        => _themes.Values.FirstOrDefault(t => t.Name == name);
    
    public string ExportTheme(string id)
    {
        if (!_themes.TryGetValue(id, out var theme)) return "";
        return CustomThemeSerializer.Serialize(theme.Data);
    }
    
    public string ExportAllThemes()
    {
        var dataList = _themes.Values.Select(t => t.Data).ToList();
        return CustomThemeSerializer.SerializeMany(dataList);
    }
    
    public async Task<(bool Success, string? Error, CustomTheme? Theme)> ImportThemeAsync(string json)
    {
        var (isValid, error) = CustomThemeSerializer.Validate(json);
        if (!isValid) return (false, error, null);
        
        var data = CustomThemeSerializer.Deserialize(json);
        if (data == null) return (false, "Failed to parse theme", null);
        
        var existingByName = GetThemeByName(data.Name);
        if (existingByName != null)
        {
            data = data with { Name = $"{data.Name} (Imported)" };
        }
        
        data = data with { Id = Guid.NewGuid().ToString("N")[..8] };
        
        var theme = await AddThemeAsync(data);
        return (true, null, theme);
    }
    
    public async Task<(int Imported, int Skipped, List<string> Errors)> ImportMultipleThemesAsync(string json)
    {
        var themes = CustomThemeSerializer.DeserializeMany(json);
        if (themes == null || themes.Count == 0)
        {
            var (success, error, theme) = await ImportThemeAsync(json);
            return success ? (1, 0, new List<string>()) : (0, 1, new List<string> { error ?? "Unknown error" });
        }
        
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();
        
        foreach (var data in themes)
        {
            try
            {
                var newData = data with { Id = Guid.NewGuid().ToString("N")[..8] };
                
                var existingByName = GetThemeByName(newData.Name);
                if (existingByName != null)
                {
                    newData = newData with { Name = $"{newData.Name} (Imported)" };
                }
                
                await AddThemeAsync(newData);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"{data.Name}: {ex.Message}");
                skipped++;
            }
        }
        
        return (imported, skipped, errors);
    }
    
    public async Task<CustomTheme> DuplicateThemeAsync(string id)
    {
        if (!_themes.TryGetValue(id, out var source))
            throw new InvalidOperationException($"Theme {id} not found");
            
        var newData = source.Data with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = $"{source.Data.Name} (Copy)",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
        
        return await AddThemeAsync(newData);
    }
    
    public async Task<CustomTheme> CreateFromBuiltInAsync(EditorThemeDefinition builtIn)
    {
        var data = CustomThemeData.FromBuiltIn(builtIn);
        return await AddThemeAsync(data);
    }
}


