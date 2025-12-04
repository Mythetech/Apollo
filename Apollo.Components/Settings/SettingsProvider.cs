using System.Text.Json;
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

    public async Task ApplyStoredSettingsAsync(Dictionary<string, Dictionary<string, object>>? stored)
    {
        if (stored == null) return;

        foreach (var settingsModel in _settings)
        {
            var typeName = settingsModel.GetType().Name;
            if (!stored.TryGetValue(typeName, out var properties)) continue;

            var anyApplied = false;
            foreach (var property in settingsModel.GetType().GetProperties())
            {
                if (!properties.TryGetValue(property.Name, out var value)) continue;
                if (!property.CanWrite) continue;

                try
                {
                    var converted = ConvertValue(value, property.PropertyType);
                    if (converted != null)
                    {
                        property.SetValue(settingsModel, converted);
                        anyApplied = true;
                    }
                }
                catch
                {
                }
            }

            if (anyApplied)
                await _bus.PublishAsync(new SettingsModelChanged(settingsModel.Type, settingsModel));
        }
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (value is JsonElement jsonElement)
            return ConvertJsonElement(jsonElement, targetType);

        if (targetType.IsEnum && value is string stringValue)
            return Enum.Parse(targetType, stringValue);

        if (targetType.IsInstanceOfType(value))
            return value;

        return Convert.ChangeType(value, targetType);
    }

    private static object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        if (targetType == typeof(int))
            return element.GetInt32();
        if (targetType == typeof(bool))
            return element.GetBoolean();
        if (targetType == typeof(string))
            return element.GetString();
        if (targetType == typeof(double))
            return element.GetDouble();
        if (targetType.IsEnum)
            return Enum.Parse(targetType, element.GetString()!);

        return null;
    }

    public async Task SettingsModelChanged<T>(T model) where T : SettingsBase
    {
        await _bus.PublishAsync(new SettingsModelChanged<T>(model));
    }
}