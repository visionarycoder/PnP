namespace Snippets.DesignPatterns.Creational.AbstractFactory;

public class Application(IUiFactory uiFactory)
{
    private readonly List<IButton> buttons = [];
    private readonly List<ICheckbox> checkboxes = [];
    private readonly List<ITextField> textFields = [];

    public void CreateUi()
    {
        // Create UI elements using the factory
        var button = uiFactory.CreateButton();
        var checkbox = uiFactory.CreateCheckbox();
        var textField = uiFactory.CreateTextField();

        buttons.Add(button);
        checkboxes.Add(checkbox);
        textFields.Add(textField);

        Console.WriteLine("UI Created with factory: " + uiFactory.GetType().Name);
    }

    public void RenderUi()
    {
        Console.WriteLine("Rendering UI...");
        foreach (var button in buttons)
            button.Render();
        foreach (var checkbox in checkboxes)
            checkbox.Render();
        foreach (var textField in textFields)
            textField.Render();
    }

    public void InteractWithUi()
    {
        if (buttons.Count > 0)
            buttons[0].Click();
        if (checkboxes.Count > 0)
            checkboxes[0].Toggle();
        if (textFields.Count > 0)
            textFields[0].SetText("Hello World");
    }
}