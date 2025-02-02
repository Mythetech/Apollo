using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Services;
using MudBlazor;

namespace Apollo.Components.Solutions.Commands;

public record PromptOpenBase64Solution(string? Base64 = null);