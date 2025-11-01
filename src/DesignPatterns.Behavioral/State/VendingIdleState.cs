namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Vending machine idle state - waiting for money
/// </summary>
public class VendingIdleState : VendingMachineState
{
    public override string StateName => "Idle";

    public override void Enter(VendingMachine machine)
    {
        Console.WriteLine("Vending machine is ready. Please insert money.");
    }

    public override void Exit(VendingMachine machine)
    {
    }

    public override void Handle(VendingMachine machine)
    {
        Console.WriteLine("Waiting for money insertion...");
    }

    public override void InsertMoney(VendingMachine machine, decimal amount)
    {
        machine.InsertedAmount += amount;
        Console.WriteLine($"${amount:F2} inserted. Total: ${machine.InsertedAmount:F2}");
        machine.TransitionTo(VendingMachine.HasMoney);
    }

    public override void SelectProduct(VendingMachine machine, string productCode)
    {
        Console.WriteLine("Please insert money first");
    }

    public override void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("No product selected");
    }

    public override void ReturnMoney(VendingMachine machine)
    {
        Console.WriteLine("No money to return");
    }

    public override void Restock(VendingMachine machine)
    {
        Console.WriteLine("Machine restocked and ready");
    }
}