namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Published state - final state, no changes allowed
/// </summary>
public class PublishedState : DocumentState
{
    public override string StateName => "Published";
    public override bool CanEdit => false;
    public override bool CanSubmit => false;
    public override bool CanApprove => false;
    public override bool CanReject => false;

    public override void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been published");
    }

    public override void Exit(DocumentWorkflow workflow)
    {
    }

    public override void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is published - no further changes allowed");
    }

    public override void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit published document");
    }

    public override void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already published");
    }

    public override void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already published");
    }

    public override void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject published document");
    }
}