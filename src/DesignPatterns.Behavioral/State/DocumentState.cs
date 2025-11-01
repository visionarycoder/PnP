namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Document workflow state interface with specific capabilities
/// </summary>
public abstract class DocumentState : ISimpleState
{
    public abstract string StateName { get; }
    public abstract bool CanEdit { get; }
    public abstract bool CanSubmit { get; }
    public abstract bool CanApprove { get; }
    public abstract bool CanReject { get; }

    public void Enter(object context) => Enter((DocumentWorkflow)context);
    public void Exit(object context) => Exit((DocumentWorkflow)context);
    public void Handle(object context) => Handle((DocumentWorkflow)context);

    public abstract void Enter(DocumentWorkflow workflow);
    public abstract void Exit(DocumentWorkflow workflow);
    public abstract void Handle(DocumentWorkflow workflow);
    public abstract void Edit(DocumentWorkflow workflow, string content);
    public abstract void Submit(DocumentWorkflow workflow);
    public abstract void Approve(DocumentWorkflow workflow);
    public abstract void Reject(DocumentWorkflow workflow, string reason);
}