namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Vending machine product selected state - ready to dispense
/// </summary>
public class VendingProductSelectedState : VendingMachineState
{
    public override string StateName => "ProductSelected";

    public override void Enter(VendingMachine machine)
    {
        Console.WriteLine($"Product selected: {machine.SelectedProduct} (${machine.ProductPrice:F2})");
        Console.WriteLine($"Change: ${machine.InsertedAmount - machine.ProductPrice:F2}");
        Console.WriteLine("Dispensing product...");

        // Automatically dispense after selection
        Task.Run(async () =>
        {
            await Task.Delay(500);
            if (machine.CurrentState == this)
            {
                machine.TransitionTo(VendingMachine.Dispensing);
            }
        });
    }

    public override void Exit(VendingMachine machine)
    {
        Console.WriteLine("Leaving product selected state");
    }

    public override void Handle(VendingMachine machine)
    {
        // Preparing to dispense
    }

    public override void InsertMoney(VendingMachine machine, decimal amount)
    {
        Console.WriteLine("Product already selected, dispensing in progress");
    }

    public override void SelectProduct(VendingMachine machine, string productName)
    {
        Console.WriteLine("Product already selected, dispensing in progress");
    }

    public override void ReturnMoney(VendingMachine machine)
    {
        Console.WriteLine("Cannot cancel, dispensing already started");
    }

    public override void DispenseProduct(VendingMachine machine)
    {
        machine.TransitionTo(VendingMachine.Dispensing);
    }

    public override void Restock(VendingMachine machine)
    {
        Console.WriteLine("Cannot restock during transaction");
    }
}