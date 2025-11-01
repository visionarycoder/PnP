namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Approved state - document approved and ready for publishing
/// </summary>
public class ApprovedState : DocumentState
{
    public override string StateName => "Approved";
    public override bool CanEdit => false;
    public override bool CanSubmit => false;
    public override bool CanApprove => false;
    public override bool CanReject => false;

    public override void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been approved and is ready for publishing");
    }

    public override void Exit(DocumentWorkflow workflow)
    {
    }

    public override void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is approved - ready for publishing");
    }

    public override void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit approved document");
    }

    public override void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already approved");
    }

    public override void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already approved");
    }

    public override void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject approved document - contact administrator");
    }

    public void Publish(DocumentWorkflow workflow)
    {
        workflow.AddComment("Document published");
        workflow.TransitionTo(DocumentWorkflow.Published);
    }
}