using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Settings.Events;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Apollo.Components.Settings;
using Apollo.Components.Theme;

public class SettingsState
{
    private readonly IMessageBus _bus;
    private readonly ILocalStorageService _localStorageService;
    private readonly IJSRuntime _jsRuntime;
    private readonly CustomThemeService _customThemeService;

    private ThemeMode _themeMode = ThemeMode.Dark;
    private EditorThemeDefinition _currentTheme = ApolloTheme.Instance.Theme;
    private ThemeMode _systemTheme = Settings.ThemeMode.Dark;
    private readonly SettingsProvider _settingsProvider;
    private string? _customThemeId;
    
    public event Action? SettingsChanged;

    public CustomThemeService CustomThemes => _customThemeService;

    public SettingsState(IMessageBus bus, ILocalStorageService localStorageService, IJSRuntime jsRuntime, CustomThemeService customThemeService)
    {
        _bus = bus;
        _localStorageService = localStorageService;
        _jsRuntime = jsRuntime;
        _customThemeService = customThemeService;
        _currentTheme.Initialize(this);
        _settingsProvider = new SettingsProvider(bus);
        
        _customThemeService.OnThemesChanged += OnCustomThemesChanged;
    }

    public async Task TryLoadSettingsFromStorageAsync()
    {
        await _customThemeService.LoadFromStorageAsync();
        
        var saved = await _localStorageService.GetItemAsync<StoredUserPreferences>(SettingsStorage.UserPreferencesKey);

        if (saved != null)
        {
            if (!string.IsNullOrWhiteSpace(saved.Theme))
                SetTheme(saved.Theme, saved.CustomThemeId);

            if (saved.Mode != null)
                ThemeMode = saved.Mode.Value;
        }

        var appSettings = await _localStorageService.GetItemAsync<Dictionary<string, Dictionary<string, object>>>(SettingsStorage.AppSettingsKey);
        await _settingsProvider.ApplyStoredSettingsAsync(appSettings);
    }

    public async Task TrySetSystemThemeAsync()
    {
        var (_, value) = await _jsRuntime.InvokeAsyncWithErrorHandling(false, "darkModeChange");
        
        _systemTheme = value ? ThemeMode.Dark : ThemeMode.Light;
    }

    public ThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (_themeMode == value) return;
            _themeMode = value;
            NotifyStateChanged();
        }
    }

    public EditorThemeDefinition CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme == value) return;
            _currentTheme = value;
            _currentTheme.Initialize(this);
            NotifyStateChanged();
        }
    }

    public bool IsUsingCustomTheme => _currentTheme is CustomTheme;
    
    public string? CurrentCustomThemeId => _customThemeId;

    public void SetTheme(string name, string? customThemeId = null)
    {
        _customThemeId = null;
        
        if (!string.IsNullOrEmpty(customThemeId))
        {
            var customTheme = _customThemeService.GetTheme(customThemeId);
            if (customTheme != null)
            {
                _customThemeId = customThemeId;
                CurrentTheme = customTheme;
                return;
            }
        }
        
        if (name.StartsWith("custom:"))
        {
            var id = name[7..];
            var customTheme = _customThemeService.GetTheme(id);
            if (customTheme != null)
            {
                _customThemeId = id;
                CurrentTheme = customTheme;
                return;
            }
        }
        
        var customByName = _customThemeService.GetThemeByName(name);
        if (customByName != null)
        {
            _customThemeId = customByName.Data.Id;
            CurrentTheme = customByName;
            return;
        }

        EditorThemeDefinition theme = name switch
        {
            "Apollo" => ApolloTheme.Instance.Theme,
            "Stealth" => StealthTheme.Instance.Theme,
            "Siren" => SirenTheme.Instance.Theme,
            _ => ApolloTheme.Instance.Theme
        };

        CurrentTheme = theme;
    }
    
    public void SetCustomTheme(CustomTheme theme)
    {
        _customThemeId = theme.Data.Id;
        CurrentTheme = theme;
    }

    public bool IsDarkMode
    {
        get
        {
            return ThemeMode switch
            {
                ThemeMode.Light => false,
                ThemeMode.Dark => true,
                ThemeMode.System => _systemTheme == ThemeMode.Dark,
                _ => true
            };
        }
    }

    public List<SettingsBase> GetSettingsModels()
    {
        return _settingsProvider.Settings;
    }

    private void OnCustomThemesChanged()
    {
        if (_customThemeId != null)
        {
            var updatedTheme = _customThemeService.GetTheme(_customThemeId);
            if (updatedTheme != null)
            {
                _currentTheme = updatedTheme;
                _currentTheme.Initialize(this);
            }
            else
            {
                _customThemeId = null;
                _currentTheme = ApolloTheme.Instance.Theme;
                _currentTheme.Initialize(this);
            }
        }
        
        SettingsChanged?.Invoke();
    }

    private async void NotifyStateChanged()
    {
        SettingsChanged?.Invoke();
        await _bus.PublishAsync(new SettingsChanged(this));
    }
}
