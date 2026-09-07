using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Components.Snackbar;
using Apollo.Components.Solutions.Services;
using MudBlazor;

namespace Apollo.Components.Solutions.Commands;

public record PromptOpenBase64Solution(string? Base64 = null);