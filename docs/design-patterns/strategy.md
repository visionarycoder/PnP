# Strategy Pattern

**Description**: Defines a family of algorithms, encapsulates each one, and makes them interchangeable. Allows the algorithm to vary independently from clients that use it.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

// Strategy interface
public interface IPaymentStrategy
{
    decimal CalculateFee(decimal amount);
    bool ProcessPayment(decimal amount, string merchantId);
    string GetPaymentMethod();
}

// Concrete Strategies
public class CreditCardPayment : IPaymentStrategy
{
    private readonly string _cardNumber;
    private readonly string _cvv;
    private readonly string _expiryDate;
    
    public CreditCardPayment(string cardNumber, string cvv, string expiryDate)
    {
        _cardNumber = cardNumber;
        _cvv = cvv;
        _expiryDate = expiryDate;
    }
    
    public decimal CalculateFee(decimal amount)
    {
        return amount * 0.029m; // 2.9% fee for credit cards
    }
    
    public bool ProcessPayment(decimal amount, string merchantId)
    {
        var fee = CalculateFee(amount);
        Console.WriteLine($"Processing Credit Card payment of ${amount:F2} (Fee: ${fee:F2})");
        Console.WriteLine($"Card: ****{_cardNumber[^4..]} | Expiry: {_expiryDate}");
        
        // Simulate payment processing
        var success = ValidateCard() && amount > 0;
        Console.WriteLine($"Credit Card payment {(success ? "✅ APPROVED" : "❌ DECLINED")}");
        
        return success;
    }
    
    public string GetPaymentMethod() => "Credit Card";
    
    private bool ValidateCard()
    {
        // Simulate card validation (always true for demo)
        return !string.IsNullOrEmpty(_cardNumber) && !string.IsNullOrEmpty(_cvv);
    }
}

public class PayPalPayment : IPaymentStrategy
{
    private readonly string _email;
    private readonly string _password;
    
    public PayPalPayment(string email, string password)
    {
        _email = email;
        _password = password;
    }
    
    public decimal CalculateFee(decimal amount)
    {
        return amount * 0.034m; // 3.4% fee for PayPal
    }
    
    public bool ProcessPayment(decimal amount, string merchantId)
    {
        var fee = CalculateFee(amount);
        Console.WriteLine($"Processing PayPal payment of ${amount:F2} (Fee: ${fee:F2})");
        Console.WriteLine($"PayPal Account: {_email}");
        
        // Simulate PayPal processing
        var success = ValidateAccount() && amount > 0;
        Console.WriteLine($"PayPal payment {(success ? "✅ APPROVED" : "❌ DECLINED")}");
        
        return success;
    }
    
    public string GetPaymentMethod() => "PayPal";
    
    private bool ValidateAccount()
    {
        return !string.IsNullOrEmpty(_email) && _email.Contains("@");
    }
}

public class BankTransferPayment : IPaymentStrategy
{
    private readonly string _accountNumber;
    private readonly string _routingNumber;
    
    public BankTransferPayment(string accountNumber, string routingNumber)
    {
        _accountNumber = accountNumber;
        _routingNumber = routingNumber;
    }
    
    public decimal CalculateFee(decimal amount)
    {
        return amount > 1000 ? 0 : 1.50m; // Free for amounts over $1000, otherwise $1.50 flat fee
    }
    
    public bool ProcessPayment(decimal amount, string merchantId)
    {
        var fee = CalculateFee(amount);
        Console.WriteLine($"Processing Bank Transfer payment of ${amount:F2} (Fee: ${fee:F2})");
        Console.WriteLine($"Account: ****{_accountNumber[^4..]} | Routing: {_routingNumber}");
        
        // Simulate bank transfer processing (slower)
        var success = ValidateAccount() && amount > 0;
        Console.WriteLine($"Bank Transfer payment {(success ? "✅ APPROVED (Processing 2-3 days)" : "❌ DECLINED")}");
        
        return success;
    }
    
    public string GetPaymentMethod() => "Bank Transfer";
    
    private bool ValidateAccount()
    {
        return !string.IsNullOrEmpty(_accountNumber) && !string.IsNullOrEmpty(_routingNumber);
    }
}

public class CryptocurrencyPayment : IPaymentStrategy
{
    private readonly string _walletAddress;
    private readonly string _cryptoType;
    
    public CryptocurrencyPayment(string walletAddress, string cryptoType = "Bitcoin")
    {
        _walletAddress = walletAddress;
        _cryptoType = cryptoType;
    }
    
    public decimal CalculateFee(decimal amount)
    {
        return 0.001m * amount; // 0.1% network fee
    }
    
    public bool ProcessPayment(decimal amount, string merchantId)
    {
        var fee = CalculateFee(amount);
        Console.WriteLine($"Processing {_cryptoType} payment of ${amount:F2} (Network Fee: ${fee:F2})");
        Console.WriteLine($"Wallet: {_walletAddress[..10]}...{_walletAddress[^6..]}");
        
        // Simulate crypto processing
        var success = ValidateWallet() && amount > 0;
        Console.WriteLine($"{_cryptoType} payment {(success ? "✅ CONFIRMED" : "❌ FAILED")}");
        
        return success;
    }
    
    public string GetPaymentMethod() => $"Cryptocurrency ({_cryptoType})";
    
    private bool ValidateWallet()
    {
        return !string.IsNullOrEmpty(_walletAddress) && _walletAddress.Length > 20;
    }
}

// Context class that uses strategies
public class PaymentProcessor
{
    private IPaymentStrategy _paymentStrategy;
    
    public void SetPaymentStrategy(IPaymentStrategy strategy)
    {
        _paymentStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }
    
    public bool ProcessOrder(decimal orderAmount, string merchantId = "MERCH_001")
    {
        if (_paymentStrategy == null)
        {
            throw new InvalidOperationException("Payment strategy must be set before processing");
        }
        
        Console.WriteLine($"\n💰 Processing order: ${orderAmount:F2}");
        Console.WriteLine($"Payment Method: {_paymentStrategy.GetPaymentMethod()}");
        
        var fee = _paymentStrategy.CalculateFee(orderAmount);
        var totalAmount = orderAmount + fee;
        
        Console.WriteLine($"Total Amount (including fees): ${totalAmount:F2}");
        
        return _paymentStrategy.ProcessPayment(orderAmount, merchantId);
    }
    
    public void ComparePaymentMethods(decimal amount, params IPaymentStrategy[] strategies)
    {
        Console.WriteLine($"\n📊 Comparing payment methods for ${amount:F2}:");
        Console.WriteLine("Method".PadRight(25) + "Fee".PadRight(10) + "Total".PadRight(10));
        Console.WriteLine(new string('-', 45));
        
        foreach (var strategy in strategies)
        {
            var fee = strategy.CalculateFee(amount);
            var total = amount + fee;
            Console.WriteLine($"{strategy.GetPaymentMethod()}".PadRight(25) + 
                            $"${fee:F2}".PadRight(10) + 
                            $"${total:F2}".PadRight(10));
        }
    }
}

// Advanced Strategy Pattern with Sorting Algorithms
public interface ISortStrategy<T> where T : IComparable<T>
{
    void Sort(T[] array);
    string GetAlgorithmName();
}

public class BubbleSortStrategy<T> : ISortStrategy<T> where T : IComparable<T>
{
    public void Sort(T[] array)
    {
        for (int i = 0; i < array.Length - 1; i++)
        {
            for (int j = 0; j < array.Length - i - 1; j++)
            {
                if (array[j].CompareTo(array[j + 1]) > 0)
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }
    
    public string GetAlgorithmName() => "Bubble Sort";
}

public class QuickSortStrategy<T> : ISortStrategy<T> where T : IComparable<T>
{
    public void Sort(T[] array)
    {
        QuickSort(array, 0, array.Length - 1);
    }
    
    private void QuickSort(T[] array, int low, int high)
    {
        if (low < high)
        {
            int pi = Partition(array, low, high);
            QuickSort(array, low, pi - 1);
            QuickSort(array, pi + 1, high);
        }
    }
    
    private int Partition(T[] array, int low, int high)
    {
        T pivot = array[high];
        int i = low - 1;
        
        for (int j = low; j < high; j++)
        {
            if (array[j].CompareTo(pivot) <= 0)
            {
                i++;
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
        
        (array[i + 1], array[high]) = (array[high], array[i + 1]);
        return i + 1;
    }
    
    public string GetAlgorithmName() => "Quick Sort";
}

public class Sorter<T> where T : IComparable<T>
{
    private ISortStrategy<T> _sortStrategy;
    
    public void SetSortStrategy(ISortStrategy<T> strategy)
    {
        _sortStrategy = strategy;
    }
    
    public void Sort(T[] array)
    {
        if (_sortStrategy == null)
        {
            throw new InvalidOperationException("Sort strategy must be set");
        }
        
        Console.WriteLine($"Sorting using {_sortStrategy.GetAlgorithmName()}");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        
        _sortStrategy.Sort(array);
        
        watch.Stop();
        Console.WriteLine($"Sorted {array.Length} elements in {watch.ElapsedMilliseconds}ms");
    }
}

// Strategy Factory Pattern
public static class PaymentStrategyFactory
{
    public static IPaymentStrategy CreateStrategy(string paymentType, Dictionary<string, string> parameters)
    {
        return paymentType.ToLower() switch
        {
            "creditcard" => new CreditCardPayment(
                parameters["cardNumber"], 
                parameters["cvv"], 
                parameters["expiryDate"]),
            "paypal" => new PayPalPayment(
                parameters["email"], 
                parameters["password"]),
            "banktransfer" => new BankTransferPayment(
                parameters["accountNumber"], 
                parameters["routingNumber"]),
            "crypto" => new CryptocurrencyPayment(
                parameters["walletAddress"], 
                parameters.GetValueOrDefault("cryptoType", "Bitcoin")),
            _ => throw new ArgumentException($"Unknown payment type: {paymentType}")
        };
    }
}
```

**Usage**:

```csharp
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Payment Strategy Pattern ===");
        
        var processor = new PaymentProcessor();
        
        // Create different payment strategies
        var creditCard = new CreditCardPayment("1234567890123456", "123", "12/25");
        var paypal = new PayPalPayment("user@example.com", "password123");
        var bankTransfer = new BankTransferPayment("9876543210", "123456789");
        var crypto = new CryptocurrencyPayment("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", "Bitcoin");
        
        decimal orderAmount = 250.00m;
        
        // Process payments with different strategies
        processor.SetPaymentStrategy(creditCard);
        processor.ProcessOrder(orderAmount);
        
        processor.SetPaymentStrategy(paypal);
        processor.ProcessOrder(orderAmount);
        
        processor.SetPaymentStrategy(bankTransfer);
        processor.ProcessOrder(orderAmount);
        
        processor.SetPaymentStrategy(crypto);
        processor.ProcessOrder(orderAmount);
        
        // Compare payment methods
        processor.ComparePaymentMethods(1500.00m, creditCard, paypal, bankTransfer, crypto);
        
        Console.WriteLine("\n=== Strategy Factory Pattern ===");
        
        // Using factory to create strategies
        var factoryParams = new Dictionary<string, string>
        {
            ["cardNumber"] = "4532123456789012",
            ["cvv"] = "456",
            ["expiryDate"] = "08/26"
        };
        
        var factoryStrategy = PaymentStrategyFactory.CreateStrategy("creditcard", factoryParams);
        processor.SetPaymentStrategy(factoryStrategy);
        processor.ProcessOrder(199.99m);
        
        Console.WriteLine("\n=== Sorting Strategy Pattern ===");
        
        // Demonstrate strategy pattern with sorting algorithms
        var numbers = new int[] { 64, 34, 25, 12, 22, 11, 90, 88, 76, 50 };
        var sorter = new Sorter<int>();
        
        // Try different sorting strategies
        Console.WriteLine($"Original array: [{string.Join(", ", numbers)}]");
        
        var bubbleSort = new BubbleSortStrategy<int>();
        var numbersForBubble = (int[])numbers.Clone();
        sorter.SetSortStrategy(bubbleSort);
        sorter.Sort(numbersForBubble);
        Console.WriteLine($"Bubble sorted: [{string.Join(", ", numbersForBubble)}]");
        
        var quickSort = new QuickSortStrategy<int>();
        var numbersForQuick = (int[])numbers.Clone();
        sorter.SetSortStrategy(quickSort);
        sorter.Sort(numbersForQuick);
        Console.WriteLine($"Quick sorted: [{string.Join(", ", numbersForQuick)}]");
        
        Console.WriteLine("\n=== Dynamic Strategy Selection ===");
        
        // Demonstrate runtime strategy selection
        var strategies = new IPaymentStrategy[]
        {
            creditCard, paypal, bankTransfer, crypto
        };
        
        var random = new Random();
        for (int i = 0; i < 3; i++)
        {
            var randomStrategy = strategies[random.Next(strategies.Length)];
            processor.SetPaymentStrategy(randomStrategy);
            
            var randomAmount = (decimal)(random.NextDouble() * 1000 + 50);
            Console.WriteLine($"\n--- Random Payment #{i + 1} ---");
            processor.ProcessOrder(Math.Round(randomAmount, 2));
        }
        
        Console.WriteLine("\n=== Strategy Benefits ===");
        Console.WriteLine("✅ Easy to add new payment methods without changing existing code");
        Console.WriteLine("✅ Runtime strategy switching");
        Console.WriteLine("✅ Cleaner code without large if-else or switch statements");
        Console.WriteLine("✅ Each strategy is independently testable");
        Console.WriteLine("✅ Follows Open/Closed Principle");
    }
}
```

**Notes**:

- Eliminates conditional statements for algorithm selection
- Makes it easy to add new algorithms without modifying existing code
- Each strategy is independently testable and maintainable
- Promotes code reuse and follows Single Responsibility Principle
- Runtime algorithm switching provides flexibility
- Strategy factory pattern simplifies strategy creation
- Can be combined with other patterns (Factory, Template Method)
- Consider using for tax calculations, discount rules, data validation, etc.
- Related patterns: [State Pattern](state.md), [Template Method](template-method.md), [Command Pattern](command.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of interfaces and polymorphism
- Knowledge of generic types (for generic strategies)

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Strategy Pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/strategy)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #strategy #behavioral #algorithms #polymorphism*
