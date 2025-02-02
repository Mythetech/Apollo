using MudBlazor;

namespace Apollo.Components.Shared.ApolloNotificationBar;

public static class SnackbarExtensions
{
    public static void AddApolloNotification(this ISnackbar snackbar, string message, Severity severity = Severity.Info)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Message", message },
            {"Severity", severity}
        };

        snackbar.Add<ApolloNotificationBar>(parameters, severity);
    }
}