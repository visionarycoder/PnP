namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Rejected state - document rejected and returned to author
/// </summary>
public class RejectedState : DocumentState
{
    public override string StateName => "Rejected";
    public override bool CanEdit => true;
    public override bool CanSubmit => true;
    public override bool CanApprove => false;
    public override bool CanReject => false;

    public override void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been rejected and returned to author");
    }

    public override void Exit(DocumentWorkflow workflow)
    {
    }

    public override void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is rejected - can be edited and resubmitted");
    }

    public override void Edit(DocumentWorkflow workflow, string content)
    {
        workflow.Content = content;
        workflow.LastModified = DateTime.UtcNow;
        workflow.AddComment($"Revised by {workflow.Author}");
        workflow.TransitionTo(DocumentWorkflow.Draft);
    }

    public override void Submit(DocumentWorkflow workflow)
    {
        workflow.AddComment($"Resubmitted by {workflow.Author}");
        workflow.TransitionTo(DocumentWorkflow.Submitted);
    }

    public override void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Cannot approve rejected document");
    }

    public override void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Document is already rejected");
    }
}