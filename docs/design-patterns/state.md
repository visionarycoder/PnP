# State Pattern

**Description**: Allows an object to alter its behavior when its internal state changes. The object appears to change its class by delegating state-specific behavior to separate state classes, eliminating complex conditional statements and making state transitions explicit and manageable.

**Language/Technology**: C#

**Code**:

## 1. Basic State Structure

```csharp
// State interface
public interface IState<T>
{
    string StateName { get; }
    void Enter(T context);
    void Exit(T context);
    void Handle(T context);
}

// Context interface
public interface IStateMachine<TState> where TState : IState<IStateMachine<TState>>
{
    TState CurrentState { get; }
    void TransitionTo(TState newState);
    void Handle();
    List<string> GetStateHistory();
}

// Base state machine implementation
public abstract class StateMachine<TState> : IStateMachine<TState> 
    where TState : IState<IStateMachine<TState>>
{
    private TState currentState = default!;
    private readonly List<string> stateHistory = new();
    
    public TState CurrentState => currentState;
    public List<string> GetStateHistory() => new(stateHistory);
    
    public virtual void TransitionTo(TState newState)
    {
        var previousState = currentState?.StateName ?? "None";
        
        currentState?.Exit(this);
        currentState = newState;
        currentState?.Enter(this);
        
        stateHistory.Add($"{DateTime.Now:HH:mm:ss} - {previousState} -> {newState.StateName}");
        Console.WriteLine($"State transition: {previousState} -> {newState.StateName}");
    }
    
    public virtual void Handle()
    {
        currentState?.Handle(this);
    }
    
    protected void SetInitialState(TState initialState)
    {
        currentState = initialState;
        currentState?.Enter(this);
        stateHistory.Add($"{DateTime.Now:HH:mm:ss} - Initial state: {initialState.StateName}");
    }
}
```

## 2. Document Workflow Example

```csharp
// Document workflow states
public interface IDocumentState : IState<DocumentWorkflow>
{
    bool CanEdit { get; }
    bool CanSubmit { get; }
    bool CanApprove { get; }
    bool CanReject { get; }
    void Edit(DocumentWorkflow workflow, string content);
    void Submit(DocumentWorkflow workflow);
    void Approve(DocumentWorkflow workflow);
    void Reject(DocumentWorkflow workflow, string reason);
}

// Document workflow context
public class DocumentWorkflow : StateMachine<IDocumentState>
{
    public string DocumentId { get; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; }
    public string? Reviewer { get; set; }
    public List<string> Comments { get; } = new();
    public DateTime CreatedAt { get; }
    public DateTime LastModified { get; set; }
    
    // Available states
    public static readonly DraftState Draft = new();
    public static readonly SubmittedState Submitted = new();
    public static readonly ReviewingState Reviewing = new();
    public static readonly ApprovedState Approved = new();
    public static readonly RejectedState Rejected = new();
    public static readonly PublishedState Published = new();
    
    public DocumentWorkflow(string documentId, string author, string title)
    {
        DocumentId = documentId;
        Author = author;
        Title = title;
        CreatedAt = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;
        
        SetInitialState(Draft);
    }
    
    public void AddComment(string comment)
    {
        Comments.Add($"{DateTime.Now:HH:mm:ss} - {comment}");
        Console.WriteLine($"Comment added: {comment}");
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"\n=== Document Status ===");
        Console.WriteLine($"ID: {DocumentId}");
        Console.WriteLine($"Title: {Title}");
        Console.WriteLine($"Author: {Author}");
        Console.WriteLine($"Reviewer: {Reviewer ?? "None"}");
        Console.WriteLine($"State: {CurrentState.StateName}");
        Console.WriteLine($"Created: {CreatedAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Modified: {LastModified:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Can Edit: {CurrentState.CanEdit}");
        Console.WriteLine($"Can Submit: {CurrentState.CanSubmit}");
        Console.WriteLine($"Can Approve: {CurrentState.CanApprove}");
        Console.WriteLine($"Can Reject: {CurrentState.CanReject}");
        
        if (Comments.Any())
        {
            Console.WriteLine("Comments:");
            foreach (var comment in Comments.TakeLast(3))
            {
                Console.WriteLine($"  - {comment}");
            }
        }
    }
}

// Draft state
public class DraftState : IDocumentState
{
    public string StateName => "Draft";
    public bool CanEdit => true;
    public bool CanSubmit => true;
    public bool CanApprove => false;
    public bool CanReject => false;
    
    public void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' is now in draft mode");
    }
    
    public void Exit(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Leaving draft mode for '{workflow.Title}'");
    }
    
    public void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("In draft state - document can be edited and submitted");
    }
    
    public void Edit(DocumentWorkflow workflow, string content)
    {
        workflow.Content = content;
        workflow.LastModified = DateTime.UtcNow;
        Console.WriteLine("Document content updated");
    }
    
    public void Submit(DocumentWorkflow workflow)
    {
        if (string.IsNullOrWhiteSpace(workflow.Content))
        {
            Console.WriteLine("Cannot submit empty document");
            return;
        }
        
        workflow.AddComment($"Submitted by {workflow.Author}");
        workflow.TransitionTo(DocumentWorkflow.Submitted);
    }
    
    public void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Cannot approve document in draft state");
    }
    
    public void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject document in draft state");
    }
}

// Submitted state
public class SubmittedState : IDocumentState
{
    public string StateName => "Submitted";
    public bool CanEdit => false;
    public bool CanSubmit => false;
    public bool CanApprove => false;
    public bool CanReject => false;
    
    public void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been submitted for review");
        // Auto-transition to reviewing after a brief delay
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            workflow.TransitionTo(DocumentWorkflow.Reviewing);
        });
    }
    
    public void Exit(DocumentWorkflow workflow) { }
    
    public void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is submitted - waiting for review assignment");
    }
    
    public void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit submitted document");
    }
    
    public void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already submitted");
    }
    
    public void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Cannot approve in submitted state - wait for review");
    }
    
    public void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject in submitted state - wait for review");
    }
}

// Reviewing state
public class ReviewingState : IDocumentState
{
    public string StateName => "Under Review";
    public bool CanEdit => false;
    public bool CanSubmit => false;
    public bool CanApprove => true;
    public bool CanReject => true;
    
    public void Enter(DocumentWorkflow workflow)
    {
        workflow.Reviewer = "System Reviewer"; // In real system, would be assigned
        Console.WriteLine($"Document '{workflow.Title}' is now under review by {workflow.Reviewer}");
    }
    
    public void Exit(DocumentWorkflow workflow) { }
    
    public void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is under review - awaiting approval or rejection");
    }
    
    public void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit document under review");
    }
    
    public void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already under review");
    }
    
    public void Approve(DocumentWorkflow workflow)
    {
        workflow.AddComment($"Approved by {workflow.Reviewer}");
        workflow.TransitionTo(DocumentWorkflow.Approved);
    }
    
    public void Reject(DocumentWorkflow workflow, string reason)
    {
        workflow.AddComment($"Rejected by {workflow.Reviewer}: {reason}");
        workflow.TransitionTo(DocumentWorkflow.Rejected);
    }
}

// Approved state
public class ApprovedState : IDocumentState
{
    public string StateName => "Approved";
    public bool CanEdit => false;
    public bool CanSubmit => false;
    public bool CanApprove => false;
    public bool CanReject => false;
    
    public void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been approved and is ready for publishing");
    }
    
    public void Exit(DocumentWorkflow workflow) { }
    
    public void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is approved - ready for publishing");
    }
    
    public void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit approved document");
    }
    
    public void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already approved");
    }
    
    public void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already approved");
    }
    
    public void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject approved document - contact administrator");
    }
    
    public void Publish(DocumentWorkflow workflow)
    {
        workflow.AddComment("Document published");
        workflow.TransitionTo(DocumentWorkflow.Published);
    }
}

// Rejected state
public class RejectedState : IDocumentState
{
    public string StateName => "Rejected";
    public bool CanEdit => true;
    public bool CanSubmit => true;
    public bool CanApprove => false;
    public bool CanReject => false;
    
    public void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been rejected and returned to author");
    }
    
    public void Exit(DocumentWorkflow workflow) { }
    
    public void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is rejected - can be edited and resubmitted");
    }
    
    public void Edit(DocumentWorkflow workflow, string content)
    {
        workflow.Content = content;
        workflow.LastModified = DateTime.UtcNow;
        workflow.AddComment($"Revised by {workflow.Author}");
        workflow.TransitionTo(DocumentWorkflow.Draft);
    }
    
    public void Submit(DocumentWorkflow workflow)
    {
        workflow.AddComment($"Resubmitted by {workflow.Author}");
        workflow.TransitionTo(DocumentWorkflow.Submitted);
    }
    
    public void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Cannot approve rejected document");
    }
    
    public void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Document is already rejected");
    }
}

// Published state
public class PublishedState : IDocumentState
{
    public string StateName => "Published";
    public bool CanEdit => false;
    public bool CanSubmit => false;
    public bool CanApprove => false;
    public bool CanReject => false;
    
    public void Enter(DocumentWorkflow workflow)
    {
        Console.WriteLine($"Document '{workflow.Title}' has been published");
    }
    
    public void Exit(DocumentWorkflow workflow) { }
    
    public void Handle(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is published - no further changes allowed");
    }
    
    public void Edit(DocumentWorkflow workflow, string content)
    {
        Console.WriteLine("Cannot edit published document");
    }
    
    public void Submit(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already published");
    }
    
    public void Approve(DocumentWorkflow workflow)
    {
        Console.WriteLine("Document is already published");
    }
    
    public void Reject(DocumentWorkflow workflow, string reason)
    {
        Console.WriteLine("Cannot reject published document");
    }
}
```

## 3. Game Character States

```csharp
// Game character states
public interface ICharacterState : IState<GameCharacter>
{
    int MovementSpeed { get; }
    int AttackDamage { get; }
    bool CanMove { get; }
    bool CanAttack { get; }
    bool CanUseItems { get; }
    void Move(GameCharacter character, int x, int y);
    void Attack(GameCharacter character, string target);
    void UseItem(GameCharacter character, string item);
    void TakeDamage(GameCharacter character, int damage);
}

// Game character context
public class GameCharacter : StateMachine<ICharacterState>
{
    public string Name { get; }
    public int Health { get; set; } = 100;
    public int MaxHealth { get; } = 100;
    public int Mana { get; set; } = 50;
    public int MaxMana { get; } = 50;
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public List<string> Inventory { get; } = new();
    public List<string> StatusEffects { get; } = new();
    
    // Available states
    public static readonly IdleState Idle = new();
    public static readonly MovingState Moving = new();
    public static readonly AttackingState Attacking = new();
    public static readonly CastingState Casting = new();
    public static readonly StunnedState Stunned = new();
    public static readonly DeadState Dead = new();
    public static readonly StealthState Stealth = new();
    
    public GameCharacter(string name)
    {
        Name = name;
        SetInitialState(Idle);
    }
    
    public void AddStatusEffect(string effect, int duration)
    {
        StatusEffects.Add($"{effect} ({duration}s)");
        Console.WriteLine($"{Name} gained status effect: {effect}");
    }
    
    public void RemoveStatusEffect(string effect)
    {
        var effectToRemove = StatusEffects.FirstOrDefault(e => e.StartsWith(effect));
        if (effectToRemove != null)
        {
            StatusEffects.Remove(effectToRemove);
            Console.WriteLine($"{Name} lost status effect: {effect}");
        }
    }
    
    public void RestoreMana(int amount)
    {
        Mana = Math.Min(Mana + amount, MaxMana);
        Console.WriteLine($"{Name} restored {amount} mana. Current: {Mana}/{MaxMana}");
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"\n=== {Name} Status ===");
        Console.WriteLine($"Health: {Health}/{MaxHealth}");
        Console.WriteLine($"Mana: {Mana}/{MaxMana}");
        Console.WriteLine($"Position: ({X}, {Y})");
        Console.WriteLine($"State: {CurrentState.StateName}");
        Console.WriteLine($"Movement Speed: {CurrentState.MovementSpeed}");
        Console.WriteLine($"Attack Damage: {CurrentState.AttackDamage}");
        Console.WriteLine($"Can Move: {CurrentState.CanMove}");
        Console.WriteLine($"Can Attack: {CurrentState.CanAttack}");
        Console.WriteLine($"Can Use Items: {CurrentState.CanUseItems}");
        
        if (StatusEffects.Any())
        {
            Console.WriteLine($"Status Effects: {string.Join(", ", StatusEffects)}");
        }
    }
}

// Idle state
public class IdleState : ICharacterState
{
    public string StateName => "Idle";
    public int MovementSpeed => 5;
    public int AttackDamage => 10;
    public bool CanMove => true;
    public bool CanAttack => true;
    public bool CanUseItems => true;
    
    public void Enter(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} is now idle");
    }
    
    public void Exit(GameCharacter character) { }
    
    public void Handle(GameCharacter character)
    {
        // Auto-regen mana while idle
        if (character.Mana < character.MaxMana)
        {
            character.RestoreMana(1);
        }
    }
    
    public void Move(GameCharacter character, int x, int y)
    {
        character.TransitionTo(GameCharacter.Moving);
        character.CurrentState.Move(character, x, y);
    }
    
    public void Attack(GameCharacter character, string target)
    {
        character.TransitionTo(GameCharacter.Attacking);
        character.CurrentState.Attack(character, target);
    }
    
    public void UseItem(GameCharacter character, string item)
    {
        Console.WriteLine($"{character.Name} used {item}");
        
        if (item == "Health Potion")
        {
            character.Health = Math.Min(character.Health + 20, character.MaxHealth);
            Console.WriteLine($"{character.Name} restored 20 health");
        }
        else if (item == "Mana Potion")
        {
            character.RestoreMana(15);
        }
    }
    
    public void TakeDamage(GameCharacter character, int damage)
    {
        character.Health -= damage;
        Console.WriteLine($"{character.Name} took {damage} damage. Health: {character.Health}");
        
        if (character.Health <= 0)
        {
            character.TransitionTo(GameCharacter.Dead);
        }
        else if (damage >= 15) // Heavy damage causes stun
        {
            character.TransitionTo(GameCharacter.Stunned);
        }
    }
}

// Moving state
public class MovingState : ICharacterState
{
    public string StateName => "Moving";
    public int MovementSpeed => 5;
    public int AttackDamage => 5; // Reduced damage while moving
    public bool CanMove => true;
    public bool CanAttack => true;
    public bool CanUseItems => false; // Cannot use items while moving
    
    public void Enter(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} started moving");
    }
    
    public void Exit(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} stopped moving");
    }
    
    public void Handle(GameCharacter character)
    {
        // Auto-return to idle after movement
        Task.Run(async () =>
        {
            await Task.Delay(500);
            if (character.CurrentState == this)
            {
                character.TransitionTo(GameCharacter.Idle);
            }
        });
    }
    
    public void Move(GameCharacter character, int x, int y)
    {
        character.X = x;
        character.Y = y;
        Console.WriteLine($"{character.Name} moved to ({x}, {y})");
    }
    
    public void Attack(GameCharacter character, string target)
    {
        Console.WriteLine($"{character.Name} performs moving attack on {target} for {AttackDamage} damage");
        character.TransitionTo(GameCharacter.Attacking);
    }
    
    public void UseItem(GameCharacter character, string item)
    {
        Console.WriteLine($"{character.Name} cannot use items while moving");
    }
    
    public void TakeDamage(GameCharacter character, int damage)
    {
        character.Health -= damage;
        Console.WriteLine($"{character.Name} took {damage} damage while moving. Health: {character.Health}");
        
        if (character.Health <= 0)
        {
            character.TransitionTo(GameCharacter.Dead);
        }
    }
}

// Attacking state
public class AttackingState : ICharacterState
{
    public string StateName => "Attacking";
    public int MovementSpeed => 2; // Slower while attacking
    public int AttackDamage => 15; // Increased damage
    public bool CanMove => false;
    public bool CanAttack => false; // Cannot chain attacks
    public bool CanUseItems => false;
    
    public void Enter(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} is attacking");
    }
    
    public void Exit(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} finished attacking");
    }
    
    public void Handle(GameCharacter character)
    {
        // Return to idle after attack
        Task.Run(async () =>
        {
            await Task.Delay(800);
            if (character.CurrentState == this)
            {
                character.TransitionTo(GameCharacter.Idle);
            }
        });
    }
    
    public void Move(GameCharacter character, int x, int y)
    {
        Console.WriteLine($"{character.Name} cannot move while attacking");
    }
    
    public void Attack(GameCharacter character, string target)
    {
        Console.WriteLine($"{character.Name} attacks {target} for {AttackDamage} damage");
    }
    
    public void UseItem(GameCharacter character, string item)
    {
        Console.WriteLine($"{character.Name} cannot use items while attacking");
    }
    
    public void TakeDamage(GameCharacter character, int damage)
    {
        var reducedDamage = damage / 2; // Reduced damage during attack
        character.Health -= reducedDamage;
        Console.WriteLine($"{character.Name} took {reducedDamage} damage (reduced) while attacking. Health: {character.Health}");
        
        if (character.Health <= 0)
        {
            character.TransitionTo(GameCharacter.Dead);
        }
    }
}

// Casting state
public class CastingState : ICharacterState
{
    public string StateName => "Casting";
    public int MovementSpeed => 0; // Cannot move while casting
    public int AttackDamage => 0; // Cannot attack while casting
    public bool CanMove => false;
    public bool CanAttack => false;
    public bool CanUseItems => false;
    
    public void Enter(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} is casting a spell");
    }
    
    public void Exit(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} finished casting");
    }
    
    public void Handle(GameCharacter character)
    {
        // Complete spell casting
        Task.Run(async () =>
        {
            await Task.Delay(1200);
            if (character.CurrentState == this)
            {
                Console.WriteLine($"{character.Name} completed spell casting");
                character.TransitionTo(GameCharacter.Idle);
            }
        });
    }
    
    public void Move(GameCharacter character, int x, int y)
    {
        Console.WriteLine($"{character.Name} cannot move while casting - spell interrupted!");
        character.TransitionTo(GameCharacter.Idle);
    }
    
    public void Attack(GameCharacter character, string target)
    {
        Console.WriteLine($"{character.Name} cannot attack while casting - spell interrupted!");
        character.TransitionTo(GameCharacter.Idle);
    }
    
    public void UseItem(GameCharacter character, string item)
    {
        Console.WriteLine($"{character.Name} cannot use items while casting");
    }
    
    public void TakeDamage(GameCharacter character, int damage)
    {
        character.Health -= damage;
        Console.WriteLine($"{character.Name} took {damage} damage while casting - spell interrupted! Health: {character.Health}");
        
        if (character.Health <= 0)
        {
            character.TransitionTo(GameCharacter.Dead);
        }
        else
        {
            character.TransitionTo(GameCharacter.Stunned);
        }
    }
}

// Stunned state
public class StunnedState : ICharacterState
{
    public string StateName => "Stunned";
    public int MovementSpeed => 0;
    public int AttackDamage => 0;
    public bool CanMove => false;
    public bool CanAttack => false;
    public bool CanUseItems => false;
    
    public void Enter(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} is stunned");
        character.AddStatusEffect("Stunned", 3);
        
        // Auto-recover after stun duration
        Task.Run(async () =>
        {
            await Task.Delay(3000);
            if (character.CurrentState == this)
            {
                character.RemoveStatusEffect("Stunned");
                character.TransitionTo(GameCharacter.Idle);
            }
        });
    }
    
    public void Exit(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} recovered from stun");
    }
    
    public void Handle(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} is stunned and cannot act");
    }
    
    public void Move(GameCharacter character, int x, int y)
    {
        Console.WriteLine($"{character.Name} cannot move while stunned");
    }
    
    public void Attack(GameCharacter character, string target)
    {
        Console.WriteLine($"{character.Name} cannot attack while stunned");
    }
    
    public void UseItem(GameCharacter character, string item)
    {
        Console.WriteLine($"{character.Name} cannot use items while stunned");
    }
    
    public void TakeDamage(GameCharacter character, int damage)
    {
        var increasedDamage = damage * 2; // Double damage while stunned
        character.Health -= increasedDamage;
        Console.WriteLine($"{character.Name} took {increasedDamage} damage (double) while stunned. Health: {character.Health}");
        
        if (character.Health <= 0)
        {
            character.TransitionTo(GameCharacter.Dead);
        }
    }
}

// Dead state
public class DeadState : ICharacterState
{
    public string StateName => "Dead";
    public int MovementSpeed => 0;
    public int AttackDamage => 0;
    public bool CanMove => false;
    public bool CanAttack => false;
    public bool CanUseItems => false;
    
    public void Enter(GameCharacter character)
    {
        character.Health = 0;
        Console.WriteLine($"{character.Name} has died");
    }
    
    public void Exit(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} has been revived");
    }
    
    public void Handle(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} is dead and cannot act");
    }
    
    public void Move(GameCharacter character, int x, int y)
    {
        Console.WriteLine($"{character.Name} cannot move while dead");
    }
    
    public void Attack(GameCharacter character, string target)
    {
        Console.WriteLine($"{character.Name} cannot attack while dead");
    }
    
    public void UseItem(GameCharacter character, string item)
    {
        if (item == "Resurrection Scroll")
        {
            character.Health = character.MaxHealth / 2;
            Console.WriteLine($"{character.Name} has been revived with {character.Health} health");
            character.TransitionTo(GameCharacter.Idle);
        }
        else
        {
            Console.WriteLine($"{character.Name} cannot use {item} while dead");
        }
    }
    
    public void TakeDamage(GameCharacter character, int damage)
    {
        Console.WriteLine($"{character.Name} is already dead");
    }
    
    public void Revive(GameCharacter character)
    {
        character.Health = character.MaxHealth / 2;
        character.TransitionTo(GameCharacter.Idle);
    }
}

// Stealth state
public class StealthState : ICharacterState
{
    public string StateName => "Stealth";
    public int MovementSpeed => 3; // Slower movement in stealth
    public int AttackDamage => 20; // Bonus damage from stealth
    public bool CanMove => true;
    public bool CanAttack => true;
    public bool CanUseItems => true;
    
    public void Enter(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} entered stealth mode");
        character.AddStatusEffect("Stealth", 10);
    }
    
    public void Exit(GameCharacter character)
    {
        Console.WriteLine($"{character.Name} left stealth mode");
        character.RemoveStatusEffect("Stealth");
    }
    
    public void Handle(GameCharacter character)
    {
        // Stealth breaks after time or action
        Task.Run(async () =>
        {
            await Task.Delay(10000);
            if (character.CurrentState == this)
            {
                Console.WriteLine("Stealth duration expired");
                character.TransitionTo(GameCharacter.Idle);
            }
        });
    }
    
    public void Move(GameCharacter character, int x, int y)
    {
        character.X = x;
        character.Y = y;
        Console.WriteLine($"{character.Name} moved stealthily to ({x}, {y})");
    }
    
    public void Attack(GameCharacter character, string target)
    {
        Console.WriteLine($"{character.Name} performs sneak attack on {target} for {AttackDamage} damage");
        character.TransitionTo(GameCharacter.Attacking); // Stealth breaks after attack
    }
    
    public void UseItem(GameCharacter character, string item)
    {
        if (item == "Smoke Bomb")
        {
            Console.WriteLine($"{character.Name} used {item} to extend stealth");
        }
        else
        {
            Console.WriteLine($"{character.Name} used {item} - stealth broken");
            character.TransitionTo(GameCharacter.Idle);
        }
    }
    
    public void TakeDamage(GameCharacter character, int damage)
    {
        character.Health -= damage;
        Console.WriteLine($"{character.Name} took {damage} damage - stealth broken! Health: {character.Health}");
        
        if (character.Health <= 0)
        {
            character.TransitionTo(GameCharacter.Dead);
        }
        else
        {
            character.TransitionTo(GameCharacter.Idle);
        }
    }
}
```

## 4. Vending Machine Example

```csharp
// Vending machine states
public interface IVendingState : IState<VendingMachine>
{
    void InsertCoin(VendingMachine machine, decimal amount);
    void SelectProduct(VendingMachine machine, string productCode);
    void PressCancel(VendingMachine machine);
    void DispenseProduct(VendingMachine machine);
}

public class VendingMachine : StateMachine<IVendingState>
{
    public decimal InsertedAmount { get; set; } = 0m;
    public string? SelectedProduct { get; set; }
    public Dictionary<string, (string Name, decimal Price, int Stock)> Products { get; } = new();
    public decimal TotalSales { get; private set; } = 0m;
    
    // States
    public static readonly IdleVendingState IdleVending = new();
    public static readonly HasMoneyState HasMoney = new();
    public static readonly ProductSelectedState ProductSelected = new();
    public static readonly DispensingState Dispensing = new();
    public static readonly OutOfOrderState OutOfOrder = new();
    
    public VendingMachine()
    {
        // Initialize products
        Products["A1"] = ("Coke", 1.25m, 10);
        Products["A2"] = ("Pepsi", 1.25m, 8);
        Products["B1"] = ("Chips", 1.50m, 15);
        Products["B2"] = ("Candy", 1.00m, 20);
        Products["C1"] = ("Water", 1.00m, 12);
        
        SetInitialState(IdleVending);
    }
    
    public void AddStock(string productCode, int quantity)
    {
        if (Products.ContainsKey(productCode))
        {
            var (name, price, stock) = Products[productCode];
            Products[productCode] = (name, price, stock + quantity);
            Console.WriteLine($"Added {quantity} {name} to stock. Total: {Products[productCode].Stock}");
        }
    }
    
    public void SetOutOfOrder(bool outOfOrder)
    {
        if (outOfOrder)
        {
            TransitionTo(OutOfOrder);
        }
        else if (CurrentState == OutOfOrder)
        {
            TransitionTo(IdleVending);
        }
    }
    
    public decimal CalculateChange()
    {
        if (SelectedProduct != null && Products.ContainsKey(SelectedProduct))
        {
            return InsertedAmount - Products[SelectedProduct].Price;
        }
        return InsertedAmount;
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"\n=== Vending Machine Status ===");
        Console.WriteLine($"State: {CurrentState.StateName}");
        Console.WriteLine($"Inserted Amount: ${InsertedAmount:F2}");
        Console.WriteLine($"Selected Product: {SelectedProduct ?? "None"}");
        Console.WriteLine($"Total Sales: ${TotalSales:F2}");
        Console.WriteLine("Available Products:");
        
        foreach (var (code, (name, price, stock)) in Products)
        {
            var status = stock > 0 ? "Available" : "Sold Out";
            Console.WriteLine($"  {code}: {name} - ${price:F2} ({stock} in stock) - {status}");
        }
    }
}

public class IdleVendingState : IVendingState
{
    public string StateName => "Idle";
    
    public void Enter(VendingMachine machine)
    {
        machine.InsertedAmount = 0m;
        machine.SelectedProduct = null;
        Console.WriteLine("Vending machine is ready. Please insert coins.");
    }
    
    public void Exit(VendingMachine machine) { }
    
    public void Handle(VendingMachine machine)
    {
        Console.WriteLine("Waiting for coin insertion...");
    }
    
    public void InsertCoin(VendingMachine machine, decimal amount)
    {
        machine.InsertedAmount += amount;
        Console.WriteLine($"Inserted ${amount:F2}. Total: ${machine.InsertedAmount:F2}");
        machine.TransitionTo(VendingMachine.HasMoney);
    }
    
    public void SelectProduct(VendingMachine machine, string productCode)
    {
        Console.WriteLine("Please insert coins first");
    }
    
    public void PressCancel(VendingMachine machine)
    {
        Console.WriteLine("Nothing to cancel");
    }
    
    public void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("No product selected");
    }
}

public class HasMoneyState : IVendingState
{
    public string StateName => "Has Money";
    
    public void Enter(VendingMachine machine)
    {
        Console.WriteLine($"Money inserted: ${machine.InsertedAmount:F2}. Please select a product.");
    }
    
    public void Exit(VendingMachine machine) { }
    
    public void Handle(VendingMachine machine)
    {
        Console.WriteLine("Waiting for product selection...");
    }
    
    public void InsertCoin(VendingMachine machine, decimal amount)
    {
        machine.InsertedAmount += amount;
        Console.WriteLine($"Inserted ${amount:F2}. Total: ${machine.InsertedAmount:F2}");
    }
    
    public void SelectProduct(VendingMachine machine, string productCode)
    {
        if (!machine.Products.ContainsKey(productCode))
        {
            Console.WriteLine($"Invalid product code: {productCode}");
            return;
        }
        
        var (name, price, stock) = machine.Products[productCode];
        
        if (stock <= 0)
        {
            Console.WriteLine($"{name} is sold out");
            return;
        }
        
        if (machine.InsertedAmount < price)
        {
            Console.WriteLine($"Insufficient funds. {name} costs ${price:F2}, inserted ${machine.InsertedAmount:F2}");
            return;
        }
        
        machine.SelectedProduct = productCode;
        Console.WriteLine($"Selected {name} for ${price:F2}");
        machine.TransitionTo(VendingMachine.ProductSelected);
    }
    
    public void PressCancel(VendingMachine machine)
    {
        Console.WriteLine($"Transaction cancelled. Returning ${machine.InsertedAmount:F2}");
        machine.TransitionTo(VendingMachine.IdleVending);
    }
    
    public void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("Please select a product first");
    }
}

public class ProductSelectedState : IVendingState
{
    public string StateName => "Product Selected";
    
    public void Enter(VendingMachine machine)
    {
        if (machine.SelectedProduct != null)
        {
            var (name, price, _) = machine.Products[machine.SelectedProduct];
            var change = machine.CalculateChange();
            Console.WriteLine($"{name} selected. Change due: ${change:F2}");
        }
        
        // Auto-dispense after selection
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            if (machine.CurrentState == this)
            {
                machine.CurrentState.DispenseProduct(machine);
            }
        });
    }
    
    public void Exit(VendingMachine machine) { }
    
    public void Handle(VendingMachine machine)
    {
        Console.WriteLine("Preparing to dispense product...");
    }
    
    public void InsertCoin(VendingMachine machine, decimal amount)
    {
        machine.InsertedAmount += amount;
        Console.WriteLine($"Additional ${amount:F2} inserted. Total: ${machine.InsertedAmount:F2}");
    }
    
    public void SelectProduct(VendingMachine machine, string productCode)
    {
        Console.WriteLine("Product already selected. Cancel to select different product.");
    }
    
    public void PressCancel(VendingMachine machine)
    {
        Console.WriteLine($"Selection cancelled. Returning ${machine.InsertedAmount:F2}");
        machine.TransitionTo(VendingMachine.IdleVending);
    }
    
    public void DispenseProduct(VendingMachine machine)
    {
        machine.TransitionTo(VendingMachine.Dispensing);
    }
}

public class DispensingState : IVendingState
{
    public string StateName => "Dispensing";
    
    public void Enter(VendingMachine machine)
    {
        Console.WriteLine("Dispensing product...");
        
        if (machine.SelectedProduct != null && machine.Products.ContainsKey(machine.SelectedProduct))
        {
            var (name, price, stock) = machine.Products[machine.SelectedProduct];
            
            // Update stock and sales
            machine.Products[machine.SelectedProduct] = (name, price, stock - 1);
            machine.TotalSales += price;
            
            var change = machine.CalculateChange();
            
            Console.WriteLine($"Dispensed: {name}");
            if (change > 0)
            {
                Console.WriteLine($"Change returned: ${change:F2}");
            }
            
            // Complete transaction
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                if (machine.CurrentState == this)
                {
                    machine.TransitionTo(VendingMachine.IdleVending);
                }
            });
        }
    }
    
    public void Exit(VendingMachine machine)
    {
        Console.WriteLine("Transaction complete. Thank you!");
    }
    
    public void Handle(VendingMachine machine)
    {
        Console.WriteLine("Dispensing in progress...");
    }
    
    public void InsertCoin(VendingMachine machine, decimal amount)
    {
        Console.WriteLine("Transaction in progress. Please wait.");
    }
    
    public void SelectProduct(VendingMachine machine, string productCode)
    {
        Console.WriteLine("Transaction in progress. Please wait.");
    }
    
    public void PressCancel(VendingMachine machine)
    {
        Console.WriteLine("Cannot cancel during dispensing");
    }
    
    public void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("Product is being dispensed");
    }
}

public class OutOfOrderState : IVendingState
{
    public string StateName => "Out of Order";
    
    public void Enter(VendingMachine machine)
    {
        Console.WriteLine("Vending machine is out of order. Service required.");
        
        // Return any inserted money
        if (machine.InsertedAmount > 0)
        {
            Console.WriteLine($"Returning inserted amount: ${machine.InsertedAmount:F2}");
        }
    }
    
    public void Exit(VendingMachine machine)
    {
        Console.WriteLine("Vending machine is back in service");
    }
    
    public void Handle(VendingMachine machine)
    {
        Console.WriteLine("Machine is out of order");
    }
    
    public void InsertCoin(VendingMachine machine, decimal amount)
    {
        Console.WriteLine("Machine is out of order. Coins rejected.");
    }
    
    public void SelectProduct(VendingMachine machine, string productCode)
    {
        Console.WriteLine("Machine is out of order. Service required.");
    }
    
    public void PressCancel(VendingMachine machine)
    {
        Console.WriteLine("Machine is out of order");
    }
    
    public void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("Machine is out of order. Cannot dispense.");
    }
}
```

**Usage**:

```csharp
// 1. Document Workflow Example
var document = new DocumentWorkflow("DOC001", "John Doe", "Project Proposal");

// Edit and submit document
document.CurrentState.Edit(document, "This is my project proposal content...");
document.CurrentState.Submit(document);

await Task.Delay(1500); // Wait for auto-transition to reviewing

// Review process
document.CurrentState.Approve(document);
document.PrintStatus();

// Check state history
foreach (var transition in document.GetStateHistory())
{
    Console.WriteLine(transition);
}

// 2. Game Character Example
var hero = new GameCharacter("Hero");

// Normal gameplay
hero.CurrentState.Move(hero, 5, 10);
await Task.Delay(600);

hero.CurrentState.Attack(hero, "Goblin");
await Task.Delay(900);

// Take damage and get stunned
hero.CurrentState.TakeDamage(hero, 20);
await Task.Delay(3100); // Wait for stun to wear off

// Use items
hero.Inventory.Add("Health Potion");
hero.CurrentState.UseItem(hero, "Health Potion");

// Enter stealth
hero.TransitionTo(GameCharacter.Stealth);
hero.CurrentState.Move(hero, 15, 20);
hero.CurrentState.Attack(hero, "Enemy");

hero.PrintStatus();

// 3. Vending Machine Example
var vendingMachine = new VendingMachine();
vendingMachine.PrintStatus();

// Normal purchase
vendingMachine.CurrentState.InsertCoin(vendingMachine, 1.00m);
vendingMachine.CurrentState.InsertCoin(vendingMachine, 0.50m);
vendingMachine.CurrentState.SelectProduct(vendingMachine, "B1"); // Chips for $1.50

await Task.Delay(3000); // Wait for dispensing

// Another transaction with cancellation
vendingMachine.CurrentState.InsertCoin(vendingMachine, 0.75m);
vendingMachine.CurrentState.SelectProduct(vendingMachine, "A1"); // Insufficient funds
vendingMachine.CurrentState.PressCancel(vendingMachine);

// Out of order scenario
vendingMachine.SetOutOfOrder(true);
vendingMachine.CurrentState.InsertCoin(vendingMachine, 1.00m); // Rejected
vendingMachine.SetOutOfOrder(false);

vendingMachine.PrintStatus();

// Expected output demonstrates:
// - State transitions with enter/exit behavior
// - Context-dependent behavior changes
// - State-specific validations and restrictions
// - Automatic state transitions based on conditions
// - History tracking and state visualization
// - Complex workflows with branching paths
```

**Notes**:

- **State Encapsulation**: Each state encapsulates specific behavior and transitions
- **Context Independence**: States can be reused across different contexts
- **Transition Management**: Explicit state transitions with enter/exit hooks
- **Behavior Delegation**: Context delegates behavior to current state
- **State History**: Track state changes for debugging and analysis
- **Async Operations**: Handle time-based transitions and delays
- **Validation**: States can enforce rules about valid operations
- **Error Recovery**: Graceful handling of invalid actions in each state

**Prerequisites**:

- .NET 6.0 or later for async/await and record types
- Understanding of state machines and finite automata
- Knowledge of delegation and polymorphism
- Familiarity with the Strategy pattern (similar structure)

**Related Patterns**:

- **Strategy**: Similar structure but Strategy is about algorithms, State is about behavior
- **Command**: States often trigger commands during transitions
- **Observer**: State changes can notify observers
- **Memento**: Can be used to save/restore state machine state

