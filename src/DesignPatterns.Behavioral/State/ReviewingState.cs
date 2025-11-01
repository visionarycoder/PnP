namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Reviewing state - document under review, can be approved or rejected
/// </summary>
public class ReviewingState : DocumentState
{
    public override string StateName => "Under Review";
    public override bool CanEdit => false;
    public override bool CanSubmit => false;
    public override bool CanApprove => true;
    public override bool CanReject => true;

    public override void Enter(DocumentWorkflow workflow)
    {
        workflow.Reviewer = "System Reviewer"; // In real system, would be assigned
        Console.WriteLine($"Document '{workflow.Title}' is now under review by {workflow.Reviewer}");
    }

    public override void Exit(DocumentWorkflow workflow)
    {
    }

    public override void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is under review - awaiting approval or rejection");
    }

    public override void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit document under review");
    }

    public override void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already under review");
    }

    public override void Approve(DocumentWorkflow workflow)
    {
        workflow.AddComment($"Approved by {workflow.Reviewer}");
        workflow.TransitionTo(DocumentWorkflow.Approved);
    }

    public override void Reject(DocumentWorkflow workflow, string reason)
    {
        workflow.AddComment($"Rejected by {workflow.Reviewer}: {reason}");
        workflow.TransitionTo(DocumentWorkflow.Rejected);
    }
}