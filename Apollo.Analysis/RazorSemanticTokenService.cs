using System.Text.RegularExpressions;
using Apollo.Contracts.Analysis;
using Apollo.Infrastructure.Workers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Apollo.Analysis;

/// <summary>
/// Provides semantic token classification for Razor files.
/// Uses the Razor compiler to extract C#, classifies with Roslyn, and maps back to Razor positions.
/// </summary>
public partial class RazorSemanticTokenService
{
    private readonly RazorCodeExtractor _razorExtractor;
    private readonly RoslynProjectService _projectService;
    private readonly ILoggerProxy _logger;

    public RazorSemanticTokenService(
        RazorCodeExtractor razorExtractor,
        RoslynProjectService projectService,
        ILoggerProxy logger)
    {
        _razorExtractor = razorExtractor;
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Get semantic tokens for a Razor document.
    /// </summary>
    public async Task<SemanticTokensResult> GetSemanticTokensAsync(
        string razorContent,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace($"Getting semantic tokens for Razor file: {filePath}");

        var extraction = _razorExtractor.Extract(razorContent, filePath);
        if (extraction.IsEmpty)
        {
            _logger.LogTrace($"No generated code from Razor extraction for {filePath}");

            return await GetRazorOnlyTokensAsync(razorContent);
        }

        _logger.LogTrace($"Generated {extraction.GeneratedCode.Length} chars of C# with {extraction.SourceMappings.Count} source mappings");

        var classifiedSpans = await ClassifyGeneratedCodeAsync(
            extraction.GeneratedCode,
            cancellationToken);

        _logger.LogTrace($"Got {classifiedSpans.Count} classified spans from Roslyn");

        var razorTokens = MapToRazorPositions(
            classifiedSpans,
            extraction.SourceMappings,
            extraction.GeneratedCode,
            razorContent);

        _logger.LogTrace($"Mapped {razorTokens.Count} tokens to Razor positions");

        var componentTokens = DetectRazorComponents(razorContent);
        razorTokens.AddRange(componentTokens);

        _logger.LogTrace($"Detected {componentTokens.Count} Razor components");

        var directiveTokens = DetectRazorDirectives(razorContent);
        razorTokens.AddRange(directiveTokens);

        _logger.LogTrace($"Detected {directiveTokens.Count} Razor directives");

        var attrExprTokens = DetectAttributeExpressions(razorContent);
        razorTokens.AddRange(attrExprTokens);

        _logger.LogTrace($"Detected {attrExprTokens.Count} attribute expression tokens");

        var sortedTokens = razorTokens
            .OrderBy(t => t.Line)
            .ThenBy(t => t.Character)
            .ToList();

        var encodedTokens = EncodeSemanticTokens(sortedTokens);

        _logger.LogTrace($"Encoded {encodedTokens.Length / 5} semantic tokens for Razor file");

        return new SemanticTokensResult
        {
            Data = encodedTokens,
            ResultId = Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// Get tokens for Razor-specific syntax only (components, directives).
    /// </summary>
    private Task<SemanticTokensResult> GetRazorOnlyTokensAsync(string razorContent)
    {
        var razorTokens = new List<RazorSemanticToken>();

        razorTokens.AddRange(DetectRazorComponents(razorContent));
        razorTokens.AddRange(DetectRazorDirectives(razorContent));
        razorTokens.AddRange(DetectAttributeExpressions(razorContent));

        var sortedTokens = razorTokens
            .OrderBy(t => t.Line)
            .ThenBy(t => t.Character)
            .ToList();

        var encodedTokens = EncodeSemanticTokens(sortedTokens);

        return Task.FromResult(new SemanticTokensResult
        {
            Data = encodedTokens,
            ResultId = Guid.NewGuid().ToString()
        });
    }

    /// <summary>
    /// Classify the generated C# code using Roslyn's Classifier API.
    /// </summary>
    private async Task<List<ClassifiedSpan>> ClassifyGeneratedCodeAsync(
        string generatedCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var workspace = _projectService.Workspace;
            if (workspace == null)
            {
                _logger.LogTrace("Workspace not available for Razor classification");
                return [];
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(
                generatedCode,
                new CSharpParseOptions(LanguageVersion.Latest),
                cancellationToken: cancellationToken);

            var references = _projectService.GetAllReferences();

            var compilation = CSharpCompilation.Create(
                "RazorGenerated",
                [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var sourceText = await syntaxTree.GetTextAsync(cancellationToken);
            var textSpan = TextSpan.FromBounds(0, sourceText.Length);

            var project = workspace.CurrentSolution.Projects.FirstOrDefault();
            if (project == null)
            {
                _logger.LogTrace("No project available for Razor classification");
                return [];
            }

            var documentId = DocumentId.CreateNewId(project.Id);
            var solution = workspace.CurrentSolution.AddDocument(
                documentId,
                "RazorGenerated.cs",
                sourceText);
            var document = solution.GetDocument(documentId);

            if (document == null)
            {
                _logger.LogTrace("Failed to create document for Razor classification");
                return [];
            }

            var classifiedSpans = await Classifier.GetClassifiedSpansAsync(
                document,
                textSpan,
                cancellationToken);

            return classifiedSpans
                .Where(s => IsSemanticClassification(s.ClassificationType))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Error classifying generated C# code: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Map classified spans from generated C# positions to original Razor positions.
    /// </summary>
    private List<RazorSemanticToken> MapToRazorPositions(
        List<ClassifiedSpan> classifiedSpans,
        List<SourceMapping> sourceMappings,
        string generatedCode,
        string razorContent)
    {
        var result = new List<RazorSemanticToken>();
        var razorText = SourceText.From(razorContent);

        foreach (var span in classifiedSpans)
        {
            var mapping = FindContainingMapping(span.TextSpan, sourceMappings);
            if (mapping == null)
                continue;

            var generatedOffset = span.TextSpan.Start - mapping.GeneratedSpan.AbsoluteIndex;

            if (generatedOffset < 0 || generatedOffset >= mapping.OriginalSpan.Length)
                continue;

            var mappedLength = Math.Min(
                span.TextSpan.Length,
                mapping.OriginalSpan.Length - generatedOffset);

            if (mappedLength <= 0)
                continue;

            var razorStart = mapping.OriginalSpan.AbsoluteIndex + generatedOffset;

            if (razorStart < 0 || razorStart + mappedLength > razorContent.Length)
                continue;

            var razorSpan = new TextSpan(razorStart, mappedLength);
            var linePosition = razorText.Lines.GetLinePositionSpan(razorSpan);

            var tokenType = MapClassificationToTokenType(span.ClassificationType);
            if (tokenType < 0)
                continue;

            result.Add(new RazorSemanticToken
            {
                Line = linePosition.Start.Line,
                Character = linePosition.Start.Character,
                Length = mappedLength,
                TokenType = tokenType,
                TokenModifiers = 0
            });
        }

        return result;
    }

    /// <summary>
    /// Find a source mapping that contains the given span.
    /// </summary>
    private SourceMapping? FindContainingMapping(TextSpan span, List<SourceMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            var generatedStart = mapping.GeneratedSpan.AbsoluteIndex;
            var generatedEnd = generatedStart + mapping.GeneratedSpan.Length;

            if (span.Start >= generatedStart && span.Start < generatedEnd)
            {
                return mapping;
            }
        }
        return null;
    }

    /// <summary>
    /// Detect Razor components (PascalCase tags) and their attributes in the Razor content.
    /// </summary>
    private List<RazorSemanticToken> DetectRazorComponents(string razorContent)
    {
        var components = new List<RazorSemanticToken>();
        var razorText = SourceText.From(razorContent);

        // Use regex to find PascalCase tags: <ComponentName or </ComponentName
        // Pattern matches: < followed by optional /, then PascalCase identifier
        var tagPattern = MyRegex();

        foreach (Match match in tagPattern.Matches(razorContent))
        {
            var tagName = match.Groups[1].Value;

            if (!IsPascalCase(tagName))
                continue;

            var nameStart = match.Groups[1].Index;
            var nameLength = tagName.Length;

            var linePosition = razorText.Lines.GetLinePositionSpan(new TextSpan(nameStart, nameLength));

            components.Add(new RazorSemanticToken
            {
                Line = linePosition.Start.Line,
                Character = linePosition.Start.Character,
                Length = nameLength,
                TokenType = SemanticTokenTypes.Component,
                TokenModifiers = 0
            });
        }

        var attrPattern = new Regex(
            @"\s([A-Z][a-zA-Z0-9]*)=",
            RegexOptions.Compiled);

        foreach (Match match in attrPattern.Matches(razorContent))
        {
            var attrName = match.Groups[1].Value;

            // Verify it's PascalCase (has lowercase letters too)
            if (!IsPascalCase(attrName))
                continue;

            var attrStart = match.Groups[1].Index;
            var attrLength = attrName.Length;

            var linePosition = razorText.Lines.GetLinePositionSpan(new TextSpan(attrStart, attrLength));

            components.Add(new RazorSemanticToken
            {
                Line = linePosition.Start.Line,
                Character = linePosition.Start.Character,
                Length = attrLength,
                TokenType = SemanticTokenTypes.Component,
                TokenModifiers = 0
            });
        }

        return components;
    }

    /// <summary>
    /// Detect Razor directives and special syntax (@using, @code, @inject, [attributes], etc.)
    /// </summary>
    private List<RazorSemanticToken> DetectRazorDirectives(string razorContent)
    {
        var tokens = new List<RazorSemanticToken>();
        var razorText = SourceText.From(razorContent);

        // Detect @directive keywords: @using, @code, @inject, @inherits, @implements, @page, @namespace, etc.
        var directivePattern = new Regex(
            @"(@)(using|code|inject|inherits|implements|page|namespace|typeparam|attribute|layout|rendermode)\b",
            RegexOptions.Compiled);

        foreach (Match match in directivePattern.Matches(razorContent))
        {
            // Highlight the @ symbol (purple)
            var atStart = match.Groups[1].Index;
            var atPos = razorText.Lines.GetLinePositionSpan(new TextSpan(atStart, 1));

            tokens.Add(new RazorSemanticToken
            {
                Line = atPos.Start.Line,
                Character = atPos.Start.Character,
                Length = 1,
                TokenType = SemanticTokenTypes.RazorTransition,
                TokenModifiers = 0
            });

            // Highlight the directive keyword
            var keywordStart = match.Groups[2].Index;
            var keywordLength = match.Groups[2].Length;

            var linePosition = razorText.Lines.GetLinePositionSpan(new TextSpan(keywordStart, keywordLength));

            tokens.Add(new RazorSemanticToken
            {
                Line = linePosition.Start.Line,
                Character = linePosition.Start.Character,
                Length = keywordLength,
                TokenType = SemanticTokenTypes.Keyword,
                TokenModifiers = 0
            });
        }

        // Detect C# attributes: [Parameter], [Inject], [CascadingParameter], etc.
        var attributePattern = new Regex(
            @"\[([A-Z][a-zA-Z0-9]*)\]",
            RegexOptions.Compiled);

        foreach (Match match in attributePattern.Matches(razorContent))
        {
            var attrName = match.Groups[1].Value;
            var attrStart = match.Groups[1].Index;
            var attrLength = attrName.Length;

            var linePosition = razorText.Lines.GetLinePositionSpan(new TextSpan(attrStart, attrLength));

            // Attributes get Class coloring (teal)
            tokens.Add(new RazorSemanticToken
            {
                Line = linePosition.Start.Line,
                Character = linePosition.Start.Character,
                Length = attrLength,
                TokenType = SemanticTokenTypes.Class,
                TokenModifiers = 0
            });
        }

        return tokens;
    }

    /// <summary>
    /// Detect @expressions inside attribute values (e.g., ="@typeof(App).Assembly").
    /// Uses Roslyn to properly parse and classify C# expressions.
    /// </summary>
    private List<RazorSemanticToken> DetectAttributeExpressions(string razorContent)
    {
        var tokens = new List<RazorSemanticToken>();
        var razorText = SourceText.From(razorContent);

        // Match attribute expressions: ="@..." or ="@(...)"
        var attrExprPattern = new Regex(
            @"=""(@)([^""]+)""",
            RegexOptions.Compiled);

        foreach (Match match in attrExprPattern.Matches(razorContent))
        {
            var atSymbol = match.Groups[1];
            var expression = match.Groups[2];

            // Emit token for @ symbol (purple)
            var atPos = razorText.Lines.GetLinePositionSpan(new TextSpan(atSymbol.Index, 1));
            tokens.Add(new RazorSemanticToken
            {
                Line = atPos.Start.Line,
                Character = atPos.Start.Character,
                Length = 1,
                TokenType = SemanticTokenTypes.RazorTransition,
                TokenModifiers = 0
            });

            // Parse the C# expression with Roslyn and emit tokens
            var exprContent = expression.Value;

            // Handle explicit expressions: @(...) - strip the outer parentheses for parsing
            if (exprContent.StartsWith("(") && exprContent.EndsWith(")"))
            {
                exprContent = exprContent.Substring(1, exprContent.Length - 2);
                var exprTokens = ParseCSharpExpression(exprContent, expression.Index + 1, razorText);
                tokens.AddRange(exprTokens);
            }
            else
            {
                var exprTokens = ParseCSharpExpression(exprContent, expression.Index, razorText);
                tokens.AddRange(exprTokens);
            }
        }

        return tokens;
    }

    /// <summary>
    /// Parse a C# expression using Roslyn and return semantic tokens.
    /// </summary>
    private List<RazorSemanticToken> ParseCSharpExpression(
        string expression,
        int startIndex,
        SourceText razorText)
    {
        var tokens = new List<RazorSemanticToken>();

        try
        {
            // Parse as a complete expression by wrapping it
            var wrappedCode = $"var __expr = {expression};";
            var syntaxTree = CSharpSyntaxTree.ParseText(
                wrappedCode,
                new CSharpParseOptions(LanguageVersion.Latest));

            var root = syntaxTree.GetRoot();

            // Offset to account for "var __expr = " prefix
            var prefixLength = "var __expr = ".Length;

            // Walk syntax tokens and classify them
            foreach (var token in root.DescendantTokens())
            {
                // Skip tokens that are part of our wrapper
                if (token.SpanStart < prefixLength)
                    continue;

                // Calculate the position in the original expression
                var exprOffset = token.SpanStart - prefixLength;

                // Skip if this token extends past our expression (e.g., the trailing semicolon)
                if (exprOffset >= expression.Length)
                    continue;

                // Map the syntax kind to a token type
                var tokenType = MapSyntaxKindToTokenType(token.Kind(), token.Text);
                if (tokenType < 0)
                    continue;

                // Calculate position in razor source
                var razorStart = startIndex + exprOffset;

                // Ensure we don't exceed bounds
                var tokenLength = Math.Min(token.Span.Length, expression.Length - exprOffset);
                if (razorStart < 0 || razorStart + tokenLength > razorText.Length)
                    continue;

                var linePos = razorText.Lines.GetLinePositionSpan(new TextSpan(razorStart, tokenLength));

                tokens.Add(new RazorSemanticToken
                {
                    Line = linePos.Start.Line,
                    Character = linePos.Start.Character,
                    Length = tokenLength,
                    TokenType = tokenType,
                    TokenModifiers = 0
                });
            }
        }
        catch (Exception)
        {
            // If parsing fails, fall back to simple identifier detection
            tokens.AddRange(ParseSimpleExpression(expression, startIndex, razorText));
        }

        return tokens;
    }

    /// <summary>
    /// Fallback: parse a simple expression using basic pattern matching.
    /// </summary>
    private List<RazorSemanticToken> ParseSimpleExpression(
        string expression,
        int startIndex,
        SourceText razorText)
    {
        var tokens = new List<RazorSemanticToken>();

        // Split by dots and highlight each identifier
        var identifierPattern = new Regex(
            @"([a-zA-Z_][a-zA-Z0-9_]*)(\([^)]*\))?",
            RegexOptions.Compiled);

        foreach (Match match in identifierPattern.Matches(expression))
        {
            var identifier = match.Groups[1].Value;
            var hasParens = match.Groups[2].Success;
            var identStart = startIndex + match.Groups[1].Index;

            var linePos = razorText.Lines.GetLinePositionSpan(new TextSpan(identStart, identifier.Length));

            // Determine token type based on context
            int tokenType;
            if (IsKnownKeyword(identifier))
                tokenType = SemanticTokenTypes.Keyword;
            else if (hasParens)
                tokenType = SemanticTokenTypes.Method;
            else if (char.IsUpper(identifier[0]))
                tokenType = SemanticTokenTypes.Class; // Assume PascalCase = type
            else
                tokenType = SemanticTokenTypes.Variable;

            tokens.Add(new RazorSemanticToken
            {
                Line = linePos.Start.Line,
                Character = linePos.Start.Character,
                Length = identifier.Length,
                TokenType = tokenType,
                TokenModifiers = 0
            });
        }

        return tokens;
    }

    /// <summary>
    /// Map C# SyntaxKind to semantic token type.
    /// </summary>
    private static int MapSyntaxKindToTokenType(SyntaxKind kind, string text)
    {
        // Check for keywords first
        if (SyntaxFacts.IsKeywordKind(kind))
            return SemanticTokenTypes.Keyword;

        return kind switch
        {
            // Identifiers - need context to determine type
            SyntaxKind.IdentifierToken => ClassifyIdentifier(text),

            // Literals
            SyntaxKind.NumericLiteralToken => SemanticTokenTypes.Number,
            SyntaxKind.StringLiteralToken => SemanticTokenTypes.String,
            SyntaxKind.CharacterLiteralToken => SemanticTokenTypes.String,

            // Operators and punctuation (don't highlight these by default)
            SyntaxKind.DotToken => -1,
            SyntaxKind.OpenParenToken => -1,
            SyntaxKind.CloseParenToken => -1,
            SyntaxKind.CommaToken => -1,

            _ => -1
        };
    }

    /// <summary>
    /// Classify an identifier based on its text.
    /// </summary>
    private static int ClassifyIdentifier(string text)
    {
        // Check for known type keywords
        if (IsKnownKeyword(text))
            return SemanticTokenTypes.Keyword;

        // PascalCase typically indicates a type
        if (char.IsUpper(text[0]) && text.Length > 1 && text.Any(char.IsLower))
            return SemanticTokenTypes.Class;

        // Otherwise treat as a variable/property
        return SemanticTokenTypes.Property;
    }

    /// <summary>
    /// Check if the identifier is a known C# keyword that looks like an identifier.
    /// </summary>
    private static bool IsKnownKeyword(string text)
    {
        return text switch
        {
            "typeof" => true,
            "nameof" => true,
            "sizeof" => true,
            "default" => true,
            "true" => true,
            "false" => true,
            "null" => true,
            "this" => true,
            "base" => true,
            "new" => true,
            "await" => true,
            "async" => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if a string is PascalCase (starts with uppercase, has at least one lowercase).
    /// </summary>
    private static bool IsPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length < 2)
            return false;

        // Must start with uppercase
        if (!char.IsUpper(name[0]))
            return false;

        // Must have at least one lowercase letter (to distinguish from ALL_CAPS)
        return name.Any(char.IsLower);
    }

    /// <summary>
    /// Encode semantic tokens in Monaco's delta format.
    /// Each token is 5 integers: [deltaLine, deltaStartChar, length, tokenType, tokenModifiers]
    /// </summary>
    private int[] EncodeSemanticTokens(List<RazorSemanticToken> tokens)
    {
        var result = new List<int>();
        var previousLine = 0;
        var previousChar = 0;

        foreach (var token in tokens)
        {
            var deltaLine = token.Line - previousLine;
            var deltaChar = deltaLine == 0
                ? token.Character - previousChar
                : token.Character;

            result.Add(deltaLine);
            result.Add(deltaChar);
            result.Add(token.Length);
            result.Add(token.TokenType);
            result.Add(token.TokenModifiers);

            previousLine = token.Line;
            previousChar = token.Character;
        }

        return result.ToArray();
    }

    /// <summary>
    /// Check if a classification type represents a semantic token we want to highlight.
    /// </summary>
    private static bool IsSemanticClassification(string classificationType)
    {
        return classificationType switch
        {
            // Types
            ClassificationTypeNames.ClassName => true,
            ClassificationTypeNames.RecordClassName => true,
            ClassificationTypeNames.RecordStructName => true,
            ClassificationTypeNames.DelegateName => true,
            ClassificationTypeNames.EnumName => true,
            ClassificationTypeNames.InterfaceName => true,
            ClassificationTypeNames.ModuleName => true,
            ClassificationTypeNames.StructName => true,
            ClassificationTypeNames.TypeParameterName => true,

            // Members
            ClassificationTypeNames.ParameterName => true,
            ClassificationTypeNames.LocalName => true,
            ClassificationTypeNames.FieldName => true,
            ClassificationTypeNames.ConstantName => true,
            ClassificationTypeNames.PropertyName => true,
            ClassificationTypeNames.EventName => true,
            ClassificationTypeNames.MethodName => true,
            ClassificationTypeNames.ExtensionMethodName => true,
            ClassificationTypeNames.NamespaceName => true,
            ClassificationTypeNames.LabelName => true,
            ClassificationTypeNames.EnumMemberName => true,
            ClassificationTypeNames.StaticSymbol => true,

            // Keywords (all types)
            ClassificationTypeNames.Keyword => true,
            ClassificationTypeNames.ControlKeyword => true,
            ClassificationTypeNames.PreprocessorKeyword => true,
            ClassificationTypeNames.PreprocessorText => true,

            // Other
            ClassificationTypeNames.StringEscapeCharacter => true,
            ClassificationTypeNames.Operator => true,
            ClassificationTypeNames.NumericLiteral => true,
            ClassificationTypeNames.StringLiteral => true,
            ClassificationTypeNames.VerbatimStringLiteral => true,

            _ => false
        };
    }

    /// <summary>
    /// Map Roslyn classification type to Monaco semantic token type index.
    /// </summary>
    private static int MapClassificationToTokenType(string classificationType)
    {
        return classificationType switch
        {
            // Types
            ClassificationTypeNames.ClassName => SemanticTokenTypes.Class,
            ClassificationTypeNames.RecordClassName => SemanticTokenTypes.Class,
            ClassificationTypeNames.RecordStructName => SemanticTokenTypes.Struct,
            ClassificationTypeNames.DelegateName => SemanticTokenTypes.Type,
            ClassificationTypeNames.EnumName => SemanticTokenTypes.Enum,
            ClassificationTypeNames.InterfaceName => SemanticTokenTypes.Interface,
            ClassificationTypeNames.ModuleName => SemanticTokenTypes.Namespace,
            ClassificationTypeNames.StructName => SemanticTokenTypes.Struct,
            ClassificationTypeNames.TypeParameterName => SemanticTokenTypes.TypeParameter,

            // Members
            ClassificationTypeNames.ParameterName => SemanticTokenTypes.Parameter,
            ClassificationTypeNames.LocalName => SemanticTokenTypes.Variable,
            ClassificationTypeNames.FieldName => SemanticTokenTypes.Property,
            ClassificationTypeNames.ConstantName => SemanticTokenTypes.Variable,
            ClassificationTypeNames.PropertyName => SemanticTokenTypes.Property,
            ClassificationTypeNames.EventName => SemanticTokenTypes.Event,
            ClassificationTypeNames.MethodName => SemanticTokenTypes.Method,
            ClassificationTypeNames.ExtensionMethodName => SemanticTokenTypes.Method,
            ClassificationTypeNames.EnumMemberName => SemanticTokenTypes.EnumMember,

            // Other
            ClassificationTypeNames.NamespaceName => SemanticTokenTypes.Namespace,
            ClassificationTypeNames.LabelName => SemanticTokenTypes.Label,
            ClassificationTypeNames.StaticSymbol => SemanticTokenTypes.Variable,

            // Keywords
            ClassificationTypeNames.Keyword => SemanticTokenTypes.Keyword,
            ClassificationTypeNames.ControlKeyword => SemanticTokenTypes.Keyword,
            ClassificationTypeNames.PreprocessorKeyword => SemanticTokenTypes.Macro,
            ClassificationTypeNames.PreprocessorText => SemanticTokenTypes.String,

            // Literals and operators
            ClassificationTypeNames.StringEscapeCharacter => SemanticTokenTypes.Regexp,
            ClassificationTypeNames.Operator => SemanticTokenTypes.Operator,
            ClassificationTypeNames.NumericLiteral => SemanticTokenTypes.Number,
            ClassificationTypeNames.StringLiteral => SemanticTokenTypes.String,
            ClassificationTypeNames.VerbatimStringLiteral => SemanticTokenTypes.String,

            _ => -1 // Not mapped
        };
    }

    [GeneratedRegex(@"</?([A-Z][a-zA-Z0-9]*)(?=[\s/>])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}

/// <summary>
/// Represents a semantic token in Razor source coordinates.
/// </summary>
public record RazorSemanticToken
{
    public int Line { get; init; }
    public int Character { get; init; }
    public int Length { get; init; }
    public int TokenType { get; init; }
    public int TokenModifiers { get; init; }
}
