using Microsoft.AspNetCore.Components;

namespace Apollo.Components.Infrastructure;

public class ApolloBaseComponent : ComponentBase
{
    [Parameter]
    public string Class { get; set; } = "";

    [Parameter]
    public string Style { get; set; } = "";

    /// <summary>
    /// Gets or sets the unique identifier.
    /// The value will be used as the HTML <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/id">global id attribute</see>.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    protected string? GetId()
    {
        return string.IsNullOrEmpty(Id) ? null : Id;
    }

    private ElementReference _ref;

    /// <summary>
    /// Gets or sets the associated web component. 
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </summary>
    public ElementReference Element
    {
        get => _ref;
        protected set
        {
            _ref = value;
        }
    }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public virtual IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}