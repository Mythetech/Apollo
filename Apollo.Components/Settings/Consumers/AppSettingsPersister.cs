using System.Text.Json;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Settings.Events;
using Blazored.LocalStorage;

namespace Apollo.Components.Settings.Consumers;

public class AppSettingsPersister(ILocalStorageService localStorage) : IConsumer<SettingsModelChanged>
{
    public async Task Consume(SettingsModelChanged message)
    {
        var stored = await localStorage.GetItemAsync<Dictionary<string, Dictionary<string, object>>>(SettingsStorage.AppSettingsKey)
                     ?? new Dictionary<string, Dictionary<string, object>>();

        var typeName = message.Type.Name;
        var properties = new Dictionary<string, object>();

        foreach (var property in message.Type.GetProperties())
        {
            if (property.Name is "Type" or "Model" or "Section") continue;
            
            var value = property.GetValue(message.Model);
            if (value == null) continue;
            
            if (property.PropertyType.IsEnum)
                properties[property.Name] = value.ToString()!;
            else
                properties[property.Name] = value;
        }

        if (stored.TryGetValue(typeName, out var existing) && AreEquivalent(existing, properties))
            return;

        stored[typeName] = properties;
        await localStorage.SetItemAsync(SettingsStorage.AppSettingsKey, stored);
    }

    private static bool AreEquivalent(Dictionary<string, object> existing, Dictionary<string, object> incoming)
    {
        if (existing.Count != incoming.Count) return false;

        foreach (var (key, incomingValue) in incoming)
        {
            if (!existing.TryGetValue(key, out var existingValue)) return false;

            var existingNormalized = existingValue is JsonElement je ? je.ToString() : existingValue?.ToString();
            var incomingNormalized = incomingValue?.ToString();

            if (existingNormalized != incomingNormalized) return false;
        }

        return true;
    }
}

