namespace Apollo.Contracts.Analysis;

/// <summary>
/// Request for semantic tokens for a document.
/// </summary>
public record SemanticTokensRequest
{
    /// <summary>
    /// The document URI to get semantic tokens for.
    /// </summary>
    public string DocumentUri { get; init; } = "";

    /// <summary>
    /// Optional Razor file content (for .razor files that may not be in the Roslyn workspace).
    /// </summary>
    public string? RazorContent { get; init; }
}

/// <summary>
/// Result containing encoded semantic tokens for Monaco.
/// Monaco expects delta-encoded tokens: each token is represented by 5 integers:
/// [deltaLine, deltaStartChar, length, tokenType, tokenModifiers]
/// </summary>
public record SemanticTokensResult
{
    /// <summary>
    /// The encoded token data as a flat array of integers.
    /// Every 5 integers represent one token:
    /// [deltaLine, deltaStartChar, length, tokenType, tokenModifiers]
    /// </summary>
    public int[] Data { get; init; } = [];

    /// <summary>
    /// Optional result ID for incremental updates.
    /// </summary>
    public string? ResultId { get; init; }

    public static SemanticTokensResult Empty => new() { Data = [] };
}

/// <summary>
/// The semantic token legend defining token types and modifiers.
/// This must match what's registered with Monaco.
/// </summary>
public record SemanticTokensLegend
{
    /// <summary>
    /// List of token type names (e.g., "class", "method", "parameter").
    /// </summary>
    public string[] TokenTypes { get; init; } = [];

    /// <summary>
    /// List of token modifier names (e.g., "static", "readonly", "async").
    /// </summary>
    public string[] TokenModifiers { get; init; } = [];
}

/// <summary>
/// Roslyn classification types mapped to Monaco semantic token types.
/// </summary>
public static class SemanticTokenTypes
{
    // Types
    public const int Namespace = 0;
    public const int Type = 1;
    public const int Class = 2;
    public const int Enum = 3;
    public const int Interface = 4;
    public const int Struct = 5;
    public const int TypeParameter = 6;
    public const int Parameter = 7;
    public const int Variable = 8;
    public const int Property = 9;
    public const int EnumMember = 10;
    public const int Event = 11;
    public const int Function = 12;
    public const int Method = 13;
    public const int Macro = 14;
    public const int Keyword = 15;
    public const int Modifier = 16;
    public const int Comment = 17;
    public const int String = 18;
    public const int Number = 19;
    public const int Regexp = 20;
    public const int Operator = 21;
    public const int Decorator = 22;
    public const int Label = 23;
    public const int Component = 24;  // Razor component type (PascalCase tags like <DropZone>)
    public const int RazorTransition = 25;  // Razor @ transition character

    /// <summary>
    /// The token type names in order (index = token type ID).
    /// Must match Monaco's semantic token legend.
    /// </summary>
    public static readonly string[] TokenTypeNames =
    [
        "namespace",
        "type",
        "class",
        "enum",
        "interface",
        "struct",
        "typeParameter",
        "parameter",
        "variable",
        "property",
        "enumMember",
        "event",
        "function",
        "method",
        "macro",
        "keyword",
        "modifier",
        "comment",
        "string",
        "number",
        "regexp",
        "operator",
        "decorator",
        "label",
        "component",
        "razorTransition"
    ];
}

/// <summary>
/// Semantic token modifiers (bit flags).
/// </summary>
public static class SemanticTokenModifiers
{
    public const int None = 0;
    public const int Declaration = 1 << 0;
    public const int Definition = 1 << 1;
    public const int Readonly = 1 << 2;
    public const int Static = 1 << 3;
    public const int Deprecated = 1 << 4;
    public const int Abstract = 1 << 5;
    public const int Async = 1 << 6;
    public const int Modification = 1 << 7;
    public const int Documentation = 1 << 8;
    public const int DefaultLibrary = 1 << 9;

    /// <summary>
    /// The modifier names in order (index = bit position).
    /// Must match Monaco's semantic token legend.
    /// </summary>
    public static readonly string[] ModifierNames =
    [
        "declaration",
        "definition",
        "readonly",
        "static",
        "deprecated",
        "abstract",
        "async",
        "modification",
        "documentation",
        "defaultLibrary"
    ];
}
