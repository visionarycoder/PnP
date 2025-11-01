namespace Snippets.DesignPatterns.Creational.Builder;

/// <summary>
/// Concrete builder implementation
/// </summary>
public class ComputerBuilder : IComputerBuilder
{
    private Computer computer = new();

    public void Reset()
    {
        computer = new Computer();
    }

    public IComputerBuilder SetCpu(string cpu)
    {
        computer.Cpu = cpu;
        return this;
    }

    public IComputerBuilder SetGpu(string gpu)
    {
        computer.Gpu = gpu;
        return this;
    }

    public IComputerBuilder SetRam(int ramGb)
    {
        computer.Ram = ramGb;
        return this;
    }

    public IComputerBuilder SetStorage(int storageGb, string storageType = "SSD")
    {
        computer.Storage = storageGb;
        computer.StorageType = storageType;
        return this;
    }

    public IComputerBuilder SetMotherBoard(string motherBoard)
    {
        computer.MotherBoard = motherBoard;
        return this;
    }

    public IComputerBuilder SetPowerSupply(string powerSupply)
    {
        computer.PowerSupply = powerSupply;
        return this;
    }

    public IComputerBuilder SetCase(string computerCase)
    {
        computer.Case = computerCase;
        return this;
    }

    public IComputerBuilder AddAccessory(string accessory)
    {
        computer.Accessories.Add(accessory);
        return this;
    }

    public IComputerBuilder SetConnectivity(bool wifi = true, bool bluetooth = true)
    {
        computer.HasWiFi = wifi;
        computer.HasBluetooth = bluetooth;
        return this;
    }

    public IComputerBuilder SetOperatingSystem(string os)
    {
        computer.OperatingSystem = os;
        return this;
    }

    public IComputerBuilder SetPrice(decimal price)
    {
        computer.Price = price;
        return this;
    }

    public Computer Build()
    {
        var result = computer;
        Reset(); // Prepare for next build
        return result;
    }
}