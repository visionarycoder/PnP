namespace Snippets.DesignPatterns.Creational.Builder;

/// <summary>
/// Abstract builder interface
/// </summary>
public interface IComputerBuilder
{
    IComputerBuilder SetCpu(string cpu);
    IComputerBuilder SetGpu(string gpu);
    IComputerBuilder SetRam(int ramGb);
    IComputerBuilder SetStorage(int storageGb, string storageType = "SSD");
    IComputerBuilder SetMotherBoard(string motherBoard);
    IComputerBuilder SetPowerSupply(string powerSupply);
    IComputerBuilder SetCase(string computerCase);
    IComputerBuilder AddAccessory(string accessory);
    IComputerBuilder SetConnectivity(bool wifi = true, bool bluetooth = true);
    IComputerBuilder SetOperatingSystem(string os);
    IComputerBuilder SetPrice(decimal price);
    Computer Build();
    void Reset();
}