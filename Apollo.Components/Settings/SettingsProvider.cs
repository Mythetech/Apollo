using Apollo.Components.Editor;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Settings.Events;

namespace Apollo.Components.Settings;

public class SettingsProvider
{
    private readonly IMessageBus _bus;
    private List<SettingsBase> _settings = [];
    
    public List<SettingsBase> Settings => _settings;
    
    public SettingsProvider(IMessageBus bus)
    {
        _bus = bus;
        var editorSettings = new EditorSettings();
        _settings.Add(editorSettings);
    }

    public async Task SettingsModelChanged<T>(T model) where T : SettingsBase
    {
        await _bus.PublishAsync(new SettingsModelChanged<T>(model));
    }
}