using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Apollo.Contracts.Debugging;

namespace Apollo.Debugging;

public class DebugSymbolRewritter : CSharpSyntaxRewriter
{
    private readonly ICollection<Breakpoint> _breakpoints;
    
    public DebugSymbolRewritter(ICollection<Breakpoint> breakpoints)
        : base(visitIntoStructuredTrivia: false)
    {
        _breakpoints = breakpoints;
    }

    private StatementSyntax InjectDebugCheckpoint(StatementSyntax node)
    {
        // Skip if this is already a debug checkpoint
        if (IsDebugCheckpoint(node))
            return node;

        var location = node.GetLocation();
        var lineSpan = location.GetLineSpan();
        
        // Create the debug checkpoint call
        var debugCall = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseName("DebugRuntime.CheckBreakpoint"),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(location.SourceTree?.FilePath ?? "")
                            )
                        ),
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(lineSpan.StartLinePosition.Line + 1)
                            )
                        )
                    })
                )
            )
        );

        return SyntaxFactory.Block(debugCall, node)
            .WithLeadingTrivia(node.GetLeadingTrivia())
            .WithTrailingTrivia(node.GetTrailingTrivia());
    }

    private bool IsDebugCheckpoint(StatementSyntax node)
    {
        if (node is ExpressionStatementSyntax expr)
        {
            if (expr.Expression is InvocationExpressionSyntax invoke)
            {
                var name = invoke.Expression.ToString();
                return name == "DebugRuntime.CheckBreakpoint";
            }
        }
        return false;
    }

    // Basic statements
    public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
    {
        return InjectDebugCheckpoint(node);
    }

    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        return InjectDebugCheckpoint(node);
    }

    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
    {
        return InjectDebugCheckpoint(node);
    }

    // Control flow statements
    public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
    {
        var newStatement = (IfStatementSyntax)base.VisitIfStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        var newStatement = (ForStatementSyntax)base.VisitForStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        var newStatement = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        var newStatement = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    public override SyntaxNode? VisitSwitchStatement(SwitchStatementSyntax node)
    {
        var newStatement = (SwitchStatementSyntax)base.VisitSwitchStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    public override SyntaxNode? VisitBreakStatement(BreakStatementSyntax node)
    {
        return InjectDebugCheckpoint(node);
    }

    public override SyntaxNode? VisitContinueStatement(ContinueStatementSyntax node)
    {
        return InjectDebugCheckpoint(node);
    }

    // Exception handling
    public override SyntaxNode? VisitThrowStatement(ThrowStatementSyntax node)
    {
        return InjectDebugCheckpoint(node);
    }

    public override SyntaxNode? VisitTryStatement(TryStatementSyntax node)
    {
        var newStatement = (TryStatementSyntax)base.VisitTryStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    public override SyntaxNode? VisitCatchClause(CatchClauseSyntax node)
    {
        var newClause = (CatchClauseSyntax)base.VisitCatchClause(node)!;
        return InjectDebugCheckpoint(newClause.Block);
    }

    public override SyntaxNode? VisitFinallyClause(FinallyClauseSyntax node)
    {
        var newClause = (FinallyClauseSyntax)base.VisitFinallyClause(node)!;
        return InjectDebugCheckpoint(newClause.Block);
    }

    // Resource management
    public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
    {
        var newStatement = (UsingStatementSyntax)base.VisitUsingStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    public override SyntaxNode? VisitLockStatement(LockStatementSyntax node)
    {
        var newStatement = (LockStatementSyntax)base.VisitLockStatement(node)!;
        return InjectDebugCheckpoint(newStatement);
    }

    // Yield statements
    public override SyntaxNode? VisitYieldStatement(YieldStatementSyntax node)
    {
        return InjectDebugCheckpoint(node);
    }
}