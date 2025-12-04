using Apollo.Components.Infrastructure.MessageBus;
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

    private ThemeMode _themeMode = ThemeMode.Dark;
    private EditorThemeDefinition _currentTheme = ApolloTheme.Instance.Theme;
    private ThemeMode _systemTheme = Settings.ThemeMode.Dark;
    private readonly SettingsProvider _settingsProvider;
    public event Action SettingsChanged;

    public SettingsState(IMessageBus bus, ILocalStorageService localStorageService, IJSRuntime jsRuntime)
    {
        _bus = bus;
        _localStorageService = localStorageService;
        _jsRuntime = jsRuntime;
        _currentTheme.Initialize(this);
        _settingsProvider = new SettingsProvider(bus);
    }

    public async Task TryLoadSettingsFromStorageAsync()
    {
        var saved = await _localStorageService.GetItemAsync<StoredUserPreferences>(SettingsStorage.UserPreferencesKey);

        if (saved != null)
        {
            if (!string.IsNullOrWhiteSpace(saved.Theme))
                SetTheme(saved.Theme);

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
        set
        {
            if (_currentTheme == value) return;
            _currentTheme = value;
            _currentTheme.Initialize(this);
            NotifyStateChanged();
        }
    }

    public void SetTheme(string name)
    {
        EditorThemeDefinition theme = name switch
        {
            "Apollo" => ApolloTheme.Instance.Theme,
            "Stealth" => StealthTheme.Instance.Theme,
            "Siren" => SirenTheme.Instance.Theme,
            _ => ApolloTheme.Instance.Theme
        };

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

    private async void NotifyStateChanged()
    {
        SettingsChanged?.Invoke();
        await _bus.PublishAsync(new SettingsChanged(this));
    }
} 