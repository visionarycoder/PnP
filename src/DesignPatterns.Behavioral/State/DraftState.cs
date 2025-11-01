namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Draft state - document can be edited and submitted
/// </summary>
public class DraftState : DocumentState
{
    public override string StateName => "Draft";
    public override bool CanEdit => true;
    public override bool CanSubmit => true;
    public override bool CanApprove => false;
    public override bool CanReject => false;

    public override void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' is now in draft mode");
    }

    public override void Exit(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Leaving draft mode for '{workflow.Title}'");
    }

    public override void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("In draft state - document can be edited and submitted");
    }

    public override void Edit(DocumentWorkflow workflow, string content)
    {
        workflow.Content = content;
        workflow.LastModified = DateTime.UtcNow;
        Console.WriteLine("Document content updated");
    }

    public override void Submit(DocumentWorkflow workflow)
    {
        if (string.IsNullOrWhiteSpace(workflow.Content))
        {
            Console.WriteLine("Cannot submit empty document");
            return;
        }

        workflow.AddComment($"Submitted by {workflow.Author}");
        workflow.TransitionTo(DocumentWorkflow.Submitted);
    }

    public override void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Cannot approve document in draft state");
    }

    public override void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject document in draft state");
    }
}