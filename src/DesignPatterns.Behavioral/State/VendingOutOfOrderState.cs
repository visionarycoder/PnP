namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Vending machine out of order state - maintenance mode
/// </summary>
public class VendingOutOfOrderState : VendingMachineState
{
    public override string StateName => "OutOfOrder";

    public override void Enter(VendingMachine machine)
    {
        Console.WriteLine("Vending machine is out of order - maintenance required");

        // Return any inserted money
        if (machine.InsertedAmount > 0)
        {
            Console.WriteLine($"Returning inserted money: ${machine.InsertedAmount:F2}");
            machine.InsertedAmount = 0;
        }
    }

    public override void Exit(VendingMachine machine)
    {
        Console.WriteLine("Vending machine back online");
    }

    public override void Handle(VendingMachine machine)
    {
        Console.WriteLine("Machine is out of order");
    }

    public override void InsertMoney(VendingMachine machine, decimal amount)
    {
        Console.WriteLine("Machine is out of order - cannot accept money");
    }

    public override void SelectProduct(VendingMachine machine, string productName)
    {
        Console.WriteLine("Machine is out of order - no products available");
    }

    public override void ReturnMoney(VendingMachine machine)
    {
        Console.WriteLine("No money to return - machine is out of order");
    }

    public override void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("Cannot dispense - machine is out of order");
    }

    public override void Restock(VendingMachine machine)
    {
        Console.WriteLine("Restocking vending machine...");
        machine.Products["Soda"] = 1.50m;
        machine.Products["Chips"] = 2.00m;
        machine.Products["Candy"] = 1.25m;
        machine.Products["Water"] = 1.00m;
        Console.WriteLine("Machine restocked and repaired");
        machine.TransitionTo(VendingMachine.Idle);
    }
}