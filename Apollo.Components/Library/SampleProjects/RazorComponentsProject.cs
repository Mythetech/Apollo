using Apollo.Components.Solutions;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Library.SampleProjects;

public static class RazorComponentsProject
{
    public static SolutionModel Create()
    {
        var solution = new SolutionModel
        {
            Name = "RazorComponentsDemo",
            Description = "Razor class library with reusable UI and interactive components",
            ProjectType = ProjectType.RazorClassLibrary,
            Items = new List<ISolutionItem>()
        };

        var rootFolder = new Folder
        {
            Name = "RazorComponentsDemo",
            Uri = "virtual/RazorComponentsDemo",
            Items = new List<ISolutionItem>()
        };
        solution.Items.Add(rootFolder);

        solution.Items.Add(new SolutionFile
        {
            Name = "_Imports.razor",
            Uri = "virtual/RazorComponentsDemo/_Imports.razor",
            Data = ImportsCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "Button.razor",
            Uri = "virtual/RazorComponentsDemo/Button.razor",
            Data = ButtonCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "Card.razor",
            Uri = "virtual/RazorComponentsDemo/Card.razor",
            Data = CardCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "Counter.razor",
            Uri = "virtual/RazorComponentsDemo/Counter.razor",
            Data = CounterCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "Toggle.razor",
            Uri = "virtual/RazorComponentsDemo/Toggle.razor",
            Data = ToggleCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "Alert.razor",
            Uri = "virtual/RazorComponentsDemo/Alert.razor",
            Data = AlertCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        return solution;
    }

    private const string ImportsCode = """
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
""";

    private const string ButtonCode = """
<button class="@ButtonClass"
        disabled="@Disabled"
        @onclick="HandleClick"
        style="@ButtonStyle">
    @Text
</button>

@code {
    [Parameter] public string Text { get; set; } = "Click me";
    [Parameter] public string Color { get; set; } = "primary";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private string ButtonClass => $"btn btn-{Color}";

    private string ButtonStyle => Color switch
    {
        "primary" => "background: #6366f1; color: white; border: none; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; font-size: 1rem;",
        "success" => "background: #22c55e; color: white; border: none; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; font-size: 1rem;",
        "danger" => "background: #ef4444; color: white; border: none; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; font-size: 1rem;",
        "warning" => "background: #f59e0b; color: white; border: none; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; font-size: 1rem;",
        _ => "background: #6b7280; color: white; border: none; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; font-size: 1rem;"
    };

    private async Task HandleClick()
    {
        if (!Disabled)
        {
            await OnClick.InvokeAsync();
        }
    }
}
""";

    private const string CardCode = """
<div class="card" style="@CardStyle">
    @if (!string.IsNullOrEmpty(Title))
    {
        <div class="card-header" style="@HeaderStyle">
            <h3 style="margin: 0; font-size: 1.125rem;">@Title</h3>
        </div>
    }
    <div class="card-body" style="padding: 1rem;">
        @ChildContent
    </div>
    @if (Footer != null)
    {
        <div class="card-footer" style="@FooterStyle">
            @Footer
        </div>
    }
</div>

@code {
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public bool Elevated { get; set; } = true;

    private string CardStyle => Elevated
        ? "background: #1e1e2e; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.3); overflow: hidden; color: #e0e0e0;"
        : "background: #1e1e2e; border-radius: 12px; border: 1px solid #333; overflow: hidden; color: #e0e0e0;";

    private string HeaderStyle => "padding: 1rem; border-bottom: 1px solid #333; background: rgba(255,255,255,0.02);";
    private string FooterStyle => "padding: 0.75rem 1rem; border-top: 1px solid #333; background: rgba(255,255,255,0.02);";
}
""";

    private const string CounterCode = """
<div class="counter" style="@ContainerStyle">
    <div style="font-size: 3rem; font-weight: bold; color: #6366f1;">
        @currentCount
    </div>
    <div style="display: flex; gap: 0.5rem; margin-top: 1rem;">
        <button style="@ButtonStyle" @onclick="Decrement">-</button>
        <button style="@ButtonStyle" @onclick="Reset">Reset</button>
        <button style="@ButtonStyle" @onclick="Increment">+</button>
    </div>
    @if (currentCount != StartValue)
    {
        <div style="margin-top: 0.5rem; font-size: 0.875rem; color: #888;">
            Changed by @(currentCount - StartValue)
        </div>
    }
</div>

@code {
    [Parameter] public int StartValue { get; set; } = 0;
    [Parameter] public int Step { get; set; } = 1;
    [Parameter] public EventCallback<int> OnCountChanged { get; set; }

    private int currentCount;

    protected override void OnInitialized()
    {
        currentCount = StartValue;
    }

    private async Task Increment()
    {
        currentCount += Step;
        await OnCountChanged.InvokeAsync(currentCount);
    }

    private async Task Decrement()
    {
        currentCount -= Step;
        await OnCountChanged.InvokeAsync(currentCount);
    }

    private async Task Reset()
    {
        currentCount = StartValue;
        await OnCountChanged.InvokeAsync(currentCount);
    }

    private string ContainerStyle => "text-align: center; padding: 2rem; background: #1e1e2e; border-radius: 12px;";
    private string ButtonStyle => "width: 48px; height: 48px; font-size: 1.5rem; background: #6366f1; color: white; border: none; border-radius: 8px; cursor: pointer;";
}
""";

    private const string ToggleCode = """
<label class="toggle" style="@ContainerStyle">
    <div style="display: flex; align-items: center; gap: 0.75rem; cursor: pointer;" @onclick="Toggle">
        <div style="@TrackStyle">
            <div style="@ThumbStyle"></div>
        </div>
        @if (!string.IsNullOrEmpty(Label))
        {
            <span style="color: #e0e0e0;">@Label</span>
        }
    </div>
</label>

@code {
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public bool Disabled { get; set; }

    private async Task Toggle()
    {
        if (Disabled) return;

        Value = !Value;
        await ValueChanged.InvokeAsync(Value);
    }

    private string ContainerStyle => Disabled
        ? "display: inline-block; opacity: 0.5; cursor: not-allowed;"
        : "display: inline-block;";

    private string TrackStyle => Value
        ? "width: 48px; height: 26px; background: #6366f1; border-radius: 13px; position: relative; transition: background 0.2s;"
        : "width: 48px; height: 26px; background: #4b5563; border-radius: 13px; position: relative; transition: background 0.2s;";

    private string ThumbStyle => Value
        ? "width: 22px; height: 22px; background: white; border-radius: 50%; position: absolute; top: 2px; left: 24px; transition: left 0.2s; box-shadow: 0 2px 4px rgba(0,0,0,0.2);"
        : "width: 22px; height: 22px; background: white; border-radius: 50%; position: absolute; top: 2px; left: 2px; transition: left 0.2s; box-shadow: 0 2px 4px rgba(0,0,0,0.2);";
}
""";

    private const string AlertCode = """
@if (!_dismissed)
{
    <div class="alert" style="@AlertStyle" role="alert">
        <div style="display: flex; align-items: flex-start; gap: 0.75rem;">
            <span style="font-size: 1.25rem;">@Icon</span>
            <div style="flex: 1;">
                @if (!string.IsNullOrEmpty(Title))
                {
                    <div style="font-weight: 600; margin-bottom: 0.25rem;">@Title</div>
                }
                <div>@ChildContent</div>
            </div>
            @if (Dismissible)
            {
                <button style="@CloseButtonStyle" @onclick="Dismiss">&times;</button>
            }
        </div>
    </div>
}

@code {
    [Parameter] public string Severity { get; set; } = "info";
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool Dismissible { get; set; } = true;
    [Parameter] public EventCallback OnDismissed { get; set; }

    private bool _dismissed;

    private async Task Dismiss()
    {
        _dismissed = true;
        await OnDismissed.InvokeAsync();
    }

    private string Icon => Severity switch
    {
        "success" => "\u2714",
        "warning" => "\u26A0",
        "error" => "\u2716",
        _ => "\u2139"
    };

    private string AlertStyle => Severity switch
    {
        "success" => "padding: 1rem; border-radius: 8px; background: rgba(34, 197, 94, 0.1); border: 1px solid #22c55e; color: #22c55e;",
        "warning" => "padding: 1rem; border-radius: 8px; background: rgba(245, 158, 11, 0.1); border: 1px solid #f59e0b; color: #f59e0b;",
        "error" => "padding: 1rem; border-radius: 8px; background: rgba(239, 68, 68, 0.1); border: 1px solid #ef4444; color: #ef4444;",
        _ => "padding: 1rem; border-radius: 8px; background: rgba(99, 102, 241, 0.1); border: 1px solid #6366f1; color: #6366f1;"
    };

    private string CloseButtonStyle => "background: none; border: none; font-size: 1.5rem; cursor: pointer; color: inherit; padding: 0; line-height: 1;";
}
""";
}
