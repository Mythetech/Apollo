using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Settings.Events;
using Blazored.LocalStorage;

namespace Apollo.Components.Settings.Consumers;

public class LocalStorageUserSettingsSaver : IConsumer<SettingsChanged>
{
    private readonly ILocalStorageService _localStorageService;

    public LocalStorageUserSettingsSaver(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }
    public async Task Consume(SettingsChanged message)
    {
        var prefs = new StoredUserPreferences(message.Settings.CurrentTheme.Name, message.Settings.ThemeMode);
        
        await _localStorageService.SetItemAsync(SettingsStorage.UserPreferencesKey, prefs);
    }
}