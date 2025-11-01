namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Submitted state - waiting for review assignment
/// </summary>
public class SubmittedState : DocumentState
{
    public override string StateName => "Submitted";
    public override bool CanEdit => false;
    public override bool CanSubmit => false;
    public override bool CanApprove => false;
    public override bool CanReject => false;

    public override void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been submitted for review");
        // Auto-transition to reviewing after a brief delay
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            workflow.TransitionTo(DocumentWorkflow.Reviewing);
        });
    }

    public override void Exit(DocumentWorkflow workflow)
    {
    }

    public override void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is submitted - waiting for review assignment");
    }

    public override void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit submitted document");
    }

    public override void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already submitted");
    }

    public override void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Cannot approve in submitted state - wait for review");
    }

    public override void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject in submitted state - wait for review");
    }
}