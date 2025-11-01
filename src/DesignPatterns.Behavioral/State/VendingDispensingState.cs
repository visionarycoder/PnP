namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Vending machine dispensing state - dispensing product and change
/// </summary>
public class VendingDispensingState : VendingMachineState
{
    public override string StateName => "Dispensing";

    public override void Enter(VendingMachine machine)
    {
        Console.WriteLine("Dispensing product and change...");

        // Simulate dispensing process
        Task.Run(async () =>
        {
            await Task.Delay(2000); // Dispensing takes time
            if (machine.CurrentState == this)
            {
                var change = machine.InsertedAmount - machine.ProductPrice;
                Console.WriteLine($"Product dispensed: {machine.SelectedProduct}");
                if (change > 0)
                {
                    Console.WriteLine($"Change dispensed: ${change:F2}");
                }

                // Reset machine state
                machine.InsertedAmount = 0;
                machine.SelectedProduct = string.Empty;
                machine.ProductPrice = 0;

                machine.TransitionTo(VendingMachine.Idle);
            }
        });
    }

    public override void Exit(VendingMachine machine)
    {
        Console.WriteLine("Transaction complete");
    }

    public override void Handle(VendingMachine machine)
    {
        // Dispensing in progress
    }

    public override void InsertMoney(VendingMachine machine, decimal amount)
    {
        Console.WriteLine("Cannot insert money while dispensing");
    }

    public override void SelectProduct(VendingMachine machine, string productName)
    {
        Console.WriteLine("Cannot select product while dispensing");
    }

    public override void ReturnMoney(VendingMachine machine)
    {
        Console.WriteLine("Cannot cancel while dispensing");
    }

    public override void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("Already dispensing");
    }

    public override void Restock(VendingMachine machine)
    {
        Console.WriteLine("Cannot restock while dispensing");
    }
}