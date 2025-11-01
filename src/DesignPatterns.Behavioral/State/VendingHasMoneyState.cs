namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Vending machine has money state - can select product
/// </summary>
public class VendingHasMoneyState : VendingMachineState
{
    public override string StateName => "HasMoney";

    public override void Enter(VendingMachine machine)
    {
        Console.WriteLine($"Vending machine has money: ${machine.InsertedAmount:F2}");
        Console.WriteLine("Please select a product...");
    }

    public override void Exit(VendingMachine machine)
    {
        Console.WriteLine("Exiting money inserted state");
    }

    public override void Handle(VendingMachine machine)
    {
        // Display product options
        Console.WriteLine("Available products:");
        foreach (var product in machine.Products)
        {
            Console.WriteLine($"- {product.Key}: ${product.Value:F2}");
        }
    }

    public override void InsertMoney(VendingMachine machine, decimal amount)
    {
        machine.InsertedAmount += amount;
        Console.WriteLine($"Additional ${amount:F2} inserted. Total: ${machine.InsertedAmount:F2}");
    }

    public override void SelectProduct(VendingMachine machine, string productName)
    {
        if (machine.Products.TryGetValue(productName, out decimal price))
        {
            if (machine.InsertedAmount >= price)
            {
                machine.SelectedProduct = productName;
                machine.ProductPrice = price;
                machine.TransitionTo(VendingMachine.ProductSelected);
            }
            else
            {
                Console.WriteLine(
                    $"Insufficient funds. ${productName} costs ${price:F2}, but only ${machine.InsertedAmount:F2} inserted");
            }
        }
        else
        {
            Console.WriteLine($"Product '{productName}' not available");
        }
    }

    public override void ReturnMoney(VendingMachine machine)
    {
        Console.WriteLine($"Returning ${machine.InsertedAmount:F2}");
        machine.InsertedAmount = 0;
        machine.TransitionTo(VendingMachine.Idle);
    }

    public override void DispenseProduct(VendingMachine machine)
    {
        Console.WriteLine("Please select a product first");
    }

    public override void Restock(VendingMachine machine)
    {
        Console.WriteLine("Cannot restock while customer is using the machine");
    }
}