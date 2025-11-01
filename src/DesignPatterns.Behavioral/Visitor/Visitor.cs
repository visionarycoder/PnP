namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor Pattern Implementation
/// Separates algorithms from objects they operate on using double dispatch.
/// Enables adding new operations without modifying existing object structures.
/// </summary>

#region Basic Visitor Infrastructure

/// <summary>
/// Abstract visitor base class with default implementation
/// </summary>
/// <typeparam name="TResult">Return type of visitor operations</typeparam>
public abstract class Visitor<TResult> : IVisitor<TResult>
{
    public abstract TResult Visit(ElementA element);
    public abstract TResult Visit(ElementB element);
    public abstract TResult Visit(ElementC element);

    protected virtual TResult DefaultVisit(IVisitable element)
    {
        return default(TResult)!;
    }
}

#endregion

#region Basic Elements

#endregion

#region Document Processing System

#endregion

#region Document Export Visitors

#endregion

#region Document Validation

#endregion

#region AST (Abstract Syntax Tree) System

#endregion

#region AST Visitors

#endregion

#region Serialization Visitors

#endregion