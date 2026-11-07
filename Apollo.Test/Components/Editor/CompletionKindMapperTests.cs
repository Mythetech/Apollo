using Apollo.Components.Editor;
using Shouldly;
using Xunit;
using MonacoKind = BlazorMonaco.Languages.CompletionItemKind;
using OmniSharpKind = OmniSharp.Models.v1.Completion.CompletionItemKind;

namespace Apollo.Test.Components.Editor;

public class CompletionKindMapperTests
{
    [Theory(DisplayName = "Completion kinds map to the matching Monaco kind")]
    [InlineData(OmniSharpKind.Class, MonacoKind.Class)]
    [InlineData(OmniSharpKind.Struct, MonacoKind.Struct)]
    [InlineData(OmniSharpKind.Event, MonacoKind.Event)]
    [InlineData(OmniSharpKind.Module, MonacoKind.Module)]
    [InlineData(OmniSharpKind.Constant, MonacoKind.Constant)]
    [InlineData(OmniSharpKind.TypeParameter, MonacoKind.TypeParameter)]
    public void ToMonacoKind_MapsKind(OmniSharpKind kind, MonacoKind expected)
    {
        kind.ToMonacoKind().ShouldBe(expected);
    }
}
