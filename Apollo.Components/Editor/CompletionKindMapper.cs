using MonacoKind = BlazorMonaco.Languages.CompletionItemKind;
using OmniSharpKind = OmniSharp.Models.v1.Completion.CompletionItemKind;

namespace Apollo.Components.Editor;

public static class CompletionKindMapper
{
    public static MonacoKind ToMonacoKind(this OmniSharpKind kind) => kind switch
    {
        OmniSharpKind.Class => MonacoKind.Class,
        OmniSharpKind.Struct => MonacoKind.Struct,
        OmniSharpKind.Method => MonacoKind.Method,
        OmniSharpKind.Field => MonacoKind.Field,
        OmniSharpKind.Constant => MonacoKind.Constant,
        OmniSharpKind.Property => MonacoKind.Property,
        OmniSharpKind.Event => MonacoKind.Event,
        OmniSharpKind.Variable => MonacoKind.Variable,
        OmniSharpKind.Interface => MonacoKind.Interface,
        OmniSharpKind.TypeParameter => MonacoKind.TypeParameter,
        OmniSharpKind.Enum => MonacoKind.Enum,
        OmniSharpKind.EnumMember => MonacoKind.EnumMember,
        OmniSharpKind.Module => MonacoKind.Module,
        OmniSharpKind.Keyword => MonacoKind.Keyword,
        OmniSharpKind.Snippet => MonacoKind.Snippet,
        OmniSharpKind.Text => MonacoKind.Text,
        _ => MonacoKind.Text
    };
}
