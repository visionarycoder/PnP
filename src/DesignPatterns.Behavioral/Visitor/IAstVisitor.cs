namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// AST visitor interface for syntax tree operations
/// </summary>
/// <typeparam name="TResult">Result type of AST operations</typeparam>
public interface IAstVisitor<TResult> : IVisitor<TResult>
{
    TResult Visit(LiteralNode node);
    TResult Visit(VariableNode node);
    TResult Visit(BinaryOperationNode node);
    TResult Visit(UnaryOperationNode node);
    TResult Visit(FunctionCallNode node);
    TResult Visit(AssignmentNode node);
    TResult Visit(BlockNode node);
}