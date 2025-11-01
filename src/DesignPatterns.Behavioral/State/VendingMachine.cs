namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Vending machine with state-dependent behavior
/// </summary>
public class VendingMachine : SimpleStateMachine
{
    public decimal InsertedAmount { get; set; } = 0;
    public string SelectedProduct { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; } = 0;

    public Dictionary<string, decimal> Products { get; } = new()
    {
        ["Soda"] = 1.50m,
        ["Chips"] = 2.00m,
        ["Candy"] = 1.25m,
        ["Water"] = 1.00m
    };

    public Dictionary<string, int> Inventory { get; } = new()
    {
        ["Soda"] = 10,
        ["Chips"] = 8,
        ["Candy"] = 15,
        ["Water"] = 12
    };

    // Available states
    public static readonly VendingIdleState Idle = new();
    public static readonly VendingHasMoneyState HasMoney = new();
    public static readonly VendingProductSelectedState ProductSelected = new();
    public static readonly VendingDispensingState Dispensing = new();
    public static readonly VendingOutOfOrderState OutOfOrder = new();

    public VendingMachine()
    {
        SetInitialState(Idle);
    }

    public void PrintStatus()
    {
        Console.WriteLine($"\n=== Vending Machine Status ===");
        Console.WriteLine($"State: {CurrentState?.StateName}");
        Console.WriteLine($"Inserted Money: ${InsertedAmount:F2}");
        Console.WriteLine($"Selected Product: {(string.IsNullOrEmpty(SelectedProduct) ? "None" : SelectedProduct)}");
        Console.WriteLine("Available Products:");
        foreach (var item in Products)
        {
            var stock = Inventory.ContainsKey(item.Key) ? Inventory[item.Key] : 0;
            Console.WriteLine($"  {item.Key}: {stock} units @ ${item.Value:F2}");
        }
    }

    public bool HasStock(string productCode)
    {
        return Inventory.ContainsKey(productCode) && Inventory[productCode] > 0;
    }

    public void DecrementStock(string productCode)
    {
        if (Inventory.ContainsKey(productCode) && Inventory[productCode] > 0)
        {
            Inventory[productCode]--;
        }
    }

    // Convenience methods that delegate to current state
    public void InsertMoney(decimal amount)
    {
        ((VendingMachineState)CurrentState!).InsertMoney(this, amount);
    }

    public void SelectProduct(string productName)
    {
        ((VendingMachineState)CurrentState!).SelectProduct(this, productName);
    }

    public void ReturnMoney()
    {
        ((VendingMachineState)CurrentState!).ReturnMoney(this);
    }

    public void DispenseProduct()
    {
        ((VendingMachineState)CurrentState!).DispenseProduct(this);
    }

    public void Restock()
    {
        ((VendingMachineState)CurrentState!).Restock(this);
    }
}