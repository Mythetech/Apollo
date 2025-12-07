using System.ComponentModel;
using System.Reflection;

namespace Apollo.Components.Infrastructure.Keyboard;

public static class KeyBindingActionExtensions
{
    public static string GetDescription(this KeyBindingAction action)
    {
        var field = typeof(KeyBindingAction).GetField(action.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? action.ToString();
    }

    public static IEnumerable<(KeyBindingAction Action, string Description)> GetAllActions()
    {
        return Enum.GetValues<KeyBindingAction>()
            .Select(action => (action, action.GetDescription()));
    }
}

