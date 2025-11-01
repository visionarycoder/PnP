namespace Snippets.DesignPatterns.Behavioral.State;

/// <summary>
/// Vending machine state interface
/// </summary>
public abstract class VendingMachineState : ISimpleState
{
    public abstract string StateName { get; }

    public void Enter(object context) => Enter((VendingMachine)context);
    public void Exit(object context) => Exit((VendingMachine)context);
    public void Handle(object context) => Handle((VendingMachine)context);

    public abstract void Enter(VendingMachine machine);
    public abstract void Exit(VendingMachine machine);
    public abstract void Handle(VendingMachine machine);
    public abstract void InsertMoney(VendingMachine machine, decimal amount);
    public abstract void SelectProduct(VendingMachine machine, string productCode);
    public abstract void DispenseProduct(VendingMachine machine);
    public abstract void ReturnMoney(VendingMachine machine);
    public abstract void Restock(VendingMachine machine);
}