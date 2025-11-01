using Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;
using Snippets.DesignPatterns.Behavioral.Command;
using Snippets.DesignPatterns.Behavioral.Interpreter;
using Snippets.DesignPatterns.Behavioral.Iterator;
using Snippets.DesignPatterns.Behavioral.Mediator;
using Snippets.DesignPatterns.Behavioral.Memento;
using Snippets.DesignPatterns.Behavioral.State;
using Snippets.DesignPatterns.Behavioral.Strategy;
using Snippets.DesignPatterns.Behavioral.TemplateMethod;
using Snippets.DesignPatterns.Behavioral.Visitor;

Console.WriteLine("🎯 Behavioral Patterns Demonstration\n");

// Chain of Responsibility Pattern Demo
await DemonstrateChainOfResponsibility();

// Command Pattern Demo
DemonstrateCommandPattern();

// Iterator Pattern Demo
DemonstrateIteratorPattern();

// Interpreter Pattern Demo
DemonstrateInterpreterPattern();

// Mediator Pattern Demo
await DemonstrateMediatorPattern();

// Memento Pattern Demo
await DemonstrateMementoPattern();

// State Pattern Demo
await DemonstrateStatePattern();

// Strategy Pattern Demo
await DemonstrateStrategyPattern();

// Template Method Pattern Demo
await DemonstrateTemplateMethodPattern();

// Visitor Pattern Demo
await DemonstrateVisitorPattern();

Console.WriteLine("\n" + new string('=', 80));
Console.WriteLine("🎯 All 11 Behavioral Design Patterns demonstrated!");
Console.WriteLine("Each pattern showcases different aspects of object interaction and responsibility.");

static async Task DemonstrateChainOfResponsibility()
{
    Console.WriteLine("=== Chain of Responsibility Pattern ===");

    // Build authentication chain
    var authChain = new AuthenticationChainBuilder()
        .AddHandler(new LoggingHandler())
        .AddHandler(new IpWhitelistHandler())
        .AddHandler(new RateLimitHandler())
        .AddHandler(new BasicAuthHandler())
        .AddHandler(new TokenAuthHandler())
        .AddHandler(new AuthorizationHandler())
        .Build();

    Console.WriteLine("🔗 Authentication Chain Built\n");

    // Test scenarios
    var scenarios = new[]
    {
        new AuthRequest
        {
            Username = "admin",
            Password = "admin123",
            IpAddress = "127.0.0.1",
            UserAgent = "TestClient/1.0",
            Resource = "/admin/users",
            RequiredRoles = ["admin"]
        },
        new AuthRequest
        {
            Token = "token_user_456",
            IpAddress = "127.0.0.1",
            UserAgent = "MobileApp/2.0",
            Resource = "/api/data",
            RequiredRoles = ["user"]
        },
        new AuthRequest
        {
            Username = "hacker",
            Password = "wrongpass",
            IpAddress = "192.168.999.1",
            UserAgent = "BadBot/1.0",
            Resource = "/admin/secrets"
        },
        new AuthRequest
        {
            Username = "guest",
            Password = "guest123",
            IpAddress = "127.0.0.1",
            UserAgent = "WebBrowser/1.0",
            Resource = "/public/info"
        }
    };

    for (int i = 0; i < scenarios.Length; i++)
    {
        Console.WriteLine($"--- Scenario {i + 1}: {scenarios[i].Username ?? "Token User"} ---");

        var response = await authChain.HandleAsync(scenarios[i]);

        if (response != null)
        {
            Console.WriteLine($"🏁 Result: {(response.IsSuccessful ? "✅ SUCCESS" : "❌ FAILED")}");
            Console.WriteLine($"   Handler: {response.HandlerName}");
            Console.WriteLine($"   Message: {response.Message}");
            if (response.IsSuccessful)
            {
                Console.WriteLine($"   User ID: {response.UserId}");
                Console.WriteLine($"   Roles: [{string.Join(", ", response.UserRoles)}]");
            }
        }
        else
        {
            Console.WriteLine("🏁 Result: ✅ SUCCESS (Chain completed)");
        }

        Console.WriteLine();
        await Task.Delay(100); // Small delay for readability
    }

    // Support Ticket System Demo
    Console.WriteLine("\n=== Support Ticket Chain ===");

    var supportChain = new CriticalIssuesHandler();
    var techSupport = new TechnicalSupportHandler();
    var billingSupport = new BillingSupportHandler();
    var generalSupport = new GeneralSupportHandler();

    supportChain.NextHandler = techSupport;
    techSupport.NextHandler = billingSupport;
    billingSupport.NextHandler = generalSupport;

    var tickets = new[]
    {
        new SupportTicket
        {
            Id = 1001, Title = "Server Down", Priority = TicketPriority.Critical, Type = TicketType.Technical,
            CustomerEmail = "customer1@example.com"
        },
        new SupportTicket
        {
            Id = 1002, Title = "Login Issues", Priority = TicketPriority.High, Type = TicketType.Technical,
            CustomerEmail = "customer2@example.com"
        },
        new SupportTicket
        {
            Id = 1003, Title = "Invoice Question", Priority = TicketPriority.Medium, Type = TicketType.Billing,
            CustomerEmail = "customer3@example.com"
        },
        new SupportTicket
        {
            Id = 1004, Title = "General Inquiry", Priority = TicketPriority.Low, Type = TicketType.General,
            CustomerEmail = "customer4@example.com"
        }
    };

    foreach (var ticket in tickets)
    {
        Console.WriteLine($"--- Processing Ticket #{ticket.Id}: {ticket.Title} ---");

        var response = await supportChain.HandleAsync(ticket);

        if (response != null && response.IsHandled)
        {
            Console.WriteLine($"🎫 Assigned to: {response.AssignedTo}");
            Console.WriteLine($"   Team: {response.HandlerName}");
            Console.WriteLine($"   Message: {response.Message}");
            Console.WriteLine($"   ETA: {response.EstimatedResolution}");
        }

        Console.WriteLine();
    }

    Console.WriteLine("🎯 Chain of Responsibility Key Benefits:");
    Console.WriteLine("✅ Decouples sender from receiver");
    Console.WriteLine("✅ Multiple handlers can process request");
    Console.WriteLine("✅ Easy to add/remove/reorder handlers");
    Console.WriteLine("✅ Single responsibility per handler");
    Console.WriteLine("✅ Dynamic chain configuration\n");
}

static void DemonstrateCommandPattern()
{
    Console.WriteLine("=== Command Pattern ===");

    // Basic Text Editor with Command Pattern
    Console.WriteLine("--- Text Editor with Undo/Redo ---");

    var editor = new TextEditor();
    var commandManager = new CommandManager();

    // Execute some commands
    var insertHello = new InsertTextCommand(editor, "Hello", 0);
    var insertWorld = new InsertTextCommand(editor, " World", 5);
    var insertExclamation = new InsertTextCommand(editor, "!", 11);

    commandManager.ExecuteCommand(insertHello);
    Console.WriteLine(editor);

    commandManager.ExecuteCommand(insertWorld);
    Console.WriteLine(editor);

    commandManager.ExecuteCommand(insertExclamation);
    Console.WriteLine(editor);

    // Test undo operations
    Console.WriteLine("\n--- Undo Operations ---");
    commandManager.Undo();
    Console.WriteLine(editor);

    commandManager.Undo();
    Console.WriteLine(editor);

    // Test redo operations
    Console.WriteLine("\n--- Redo Operations ---");
    commandManager.Redo();
    Console.WriteLine(editor);

    // Test delete command
    Console.WriteLine("\n--- Delete Operation ---");
    var deleteCommand = new DeleteTextCommand(editor, 0, 5);
    commandManager.ExecuteCommand(deleteCommand);
    Console.WriteLine(editor);

    commandManager.Undo();
    Console.WriteLine(editor);

    // Test macro command
    Console.WriteLine("\n--- Macro Command ---");
    editor.Clear();

    var macro = new MacroCommand("Format Header");
    macro.AddCommand(new InsertTextCommand(editor, "*** ", 0));
    macro.AddCommand(new InsertTextCommand(editor, "TITLE", 4));
    macro.AddCommand(new InsertTextCommand(editor, " ***", 9));

    commandManager.ClearHistory();
    commandManager.ExecuteCommand(macro);
    Console.WriteLine(editor);

    commandManager.Undo();
    Console.WriteLine(editor);

    // Universal Remote Control Example
    Console.WriteLine("\n--- Universal Remote Control ---");

    var tv = new Television();
    var stereo = new Stereo();
    var remote = new UniversalRemote();

    // Set up remote control
    remote.SetCommand("TV_ON", new TurnOnCommand(tv));
    remote.SetCommand("TV_OFF", new TurnOffCommand(tv));
    remote.SetCommand("TV_VOL_UP", new VolumeUpCommand(tv));
    remote.SetCommand("TV_VOL_DOWN", new VolumeDownCommand(tv));

    remote.SetCommand("STEREO_ON", new TurnOnCommand(stereo));
    remote.SetCommand("STEREO_OFF", new TurnOffCommand(stereo));
    remote.SetCommand("STEREO_VOL_UP", new VolumeUpCommand(stereo));
    remote.SetCommand("STEREO_VOL_DOWN", new VolumeDownCommand(stereo));

    Console.WriteLine("\n--- Using Remote Control ---");
    remote.PressButton("TV_ON");
    remote.PressButton("TV_VOL_UP");
    remote.PressButton("TV_VOL_UP");

    remote.PressButton("STEREO_ON");
    remote.PressButton("STEREO_VOL_UP");

    Console.WriteLine("\n--- Device Status ---");
    Console.WriteLine(tv.GetStatus());
    Console.WriteLine(stereo.GetStatus());

    Console.WriteLine("\n--- Undo Last Commands ---");
    remote.PressUndo(); // Undo stereo volume up
    remote.PressUndo(); // Undo stereo on
    remote.PressUndo(); // Undo TV volume up

    Console.WriteLine("\n--- Final Device Status ---");
    Console.WriteLine(tv.GetStatus());
    Console.WriteLine(stereo.GetStatus());

    Console.WriteLine("\n🎯 Command Pattern Key Benefits:");
    Console.WriteLine("✅ Decouples sender from receiver");
    Console.WriteLine("✅ Commands can be queued, logged, and undone");
    Console.WriteLine("✅ Easy to add new commands without changing existing code");
    Console.WriteLine("✅ Supports macro commands and remote operations");
    Console.WriteLine("✅ Enables undo/redo functionality");
}

static void DemonstrateIteratorPattern()
{
    Console.WriteLine("\n=== Iterator Pattern ===");

    // Custom Collection with Different Iterators
    Console.WriteLine("--- Custom Collection with Multiple Iterators ---");

    var collection = new CustomCollection<string>();
    collection.AddRange(["Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape"]);

    Console.WriteLine($"Collection: {collection} - [{string.Join(", ", collection)}]");

    // Forward Iterator
    Console.WriteLine("\n🔄 Forward Iterator:");
    var forwardIterator = collection.CreateIterator();
    var forwardResults = new List<string>();
    while (forwardIterator.HasNext())
    {
        forwardResults.Add(forwardIterator.Next());
    }

    Console.WriteLine($"   {string.Join(" → ", forwardResults)}");

    // Reverse Iterator
    Console.WriteLine("\n🔄 Reverse Iterator:");
    var reverseIterator = collection.CreateReverseIterator();
    var reverseResults = new List<string>();
    while (reverseIterator.HasNext())
    {
        reverseResults.Add(reverseIterator.Next());
    }

    Console.WriteLine($"   {string.Join(" → ", reverseResults)}");

    // Skip Iterator
    Console.WriteLine("\n🔄 Skip Iterator (every 2nd):");
    var skipIterator = collection.CreateSkipIterator(2);
    var skipResults = new List<string>();
    while (skipIterator.HasNext())
    {
        skipResults.Add(skipIterator.Next());
    }

    Console.WriteLine($"   {string.Join(" → ", skipResults)}");

    // Filter Iterator
    Console.WriteLine("\n🔄 Filter Iterator (contains 'e'):");
    var filterIterator = collection.CreateFilterIterator(item => item.Contains('e'));
    var filterResults = new List<string>();
    while (filterIterator.HasNext())
    {
        filterResults.Add(filterIterator.Next());
    }

    Console.WriteLine($"   {string.Join(" → ", filterResults)}");

    // Transform Iterator
    Console.WriteLine("\n🔄 Transform Iterator (to uppercase):");
    var transformIterator = collection.CreateTransformIterator<string>(item => item.ToUpper());
    var transformResults = new List<string>();
    while (transformIterator.HasNext())
    {
        transformResults.Add(transformIterator.Next());
    }

    Console.WriteLine($"   {string.Join(" → ", transformResults)}");

    // Yield-based methods
    Console.WriteLine("\n--- Yield-based Iteration ---");
    Console.WriteLine($"🍎 Forward: {string.Join(" → ", collection.Forward())}");
    Console.WriteLine($"🍎 Reverse: {string.Join(" → ", collection.Reverse())}");
    Console.WriteLine($"🍎 Skip(3): {string.Join(" → ", collection.Skip(3))}");
    Console.WriteLine($"🍎 Where(len>5): {string.Join(" → ", collection.Where(x => x.Length > 5))}");
    Console.WriteLine($"🍎 Select(len): {string.Join(" → ", collection.Select(x => x.Length))}");

    // Tree Iterator
    Console.WriteLine("\n--- Tree Iterator (Depth-First) ---");
    var tree = new Tree<string>("Root");
    tree.Root!.AddChild(new TreeNode<string>("Child1"));
    tree.Root.AddChild(new TreeNode<string>("Child2"));
    tree.Root.AddChild(new TreeNode<string>("Child3"));

    tree.Root.Children[0].AddChild("Grandchild1.1");
    tree.Root.Children[0].AddChild("Grandchild1.2");
    tree.Root.Children[1].AddChild("Grandchild2.1");

    Console.WriteLine("🌲 Tree Structure:");
    Console.WriteLine("   Root");
    Console.WriteLine("   ├── Child1");
    Console.WriteLine("   │   ├── Grandchild1.1");
    Console.WriteLine("   │   └── Grandchild1.2");
    Console.WriteLine("   ├── Child2");
    Console.WriteLine("   │   └── Grandchild2.1");
    Console.WriteLine("   └── Child3");

    var treeIterator = tree.CreateIterator();
    var treeResults = new List<string>();
    while (treeIterator.HasNext())
    {
        treeResults.Add(treeIterator.Next());
    }

    Console.WriteLine($"🔄 Depth-First: {string.Join(" → ", treeResults)}");
    Console.WriteLine($"🔄 Breadth-First: {string.Join(" → ", tree.BreadthFirst())}");

    // Matrix Iterator
    Console.WriteLine("\n--- Matrix Iterator ---");
    var matrix = new Matrix<int>(3, 4);
    int value = 1;
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 4; j++)
        {
            matrix[i, j] = value++;
        }
    }

    Console.WriteLine("🔢 Matrix (3x4):");
    for (int i = 0; i < 3; i++)
    {
        var row = new List<int>();
        for (int j = 0; j < 4; j++)
        {
            row.Add(matrix[i, j]);
        }

        Console.WriteLine($"   [{string.Join(", ", row.Select(x => x.ToString().PadLeft(2)))}]");
    }

    Console.WriteLine($"🔄 By Rows: {string.Join(" → ", matrix.IterateByRows())}");
    Console.WriteLine($"🔄 By Columns: {string.Join(" → ", matrix.IterateByColumns())}");
    Console.WriteLine($"🔄 Diagonal: {string.Join(" → ", matrix.IterateDiagonal())}");
    Console.WriteLine($"🔄 Spiral: {string.Join(" → ", matrix.IterateSpiral())}");

    // Pagination Iterator
    Console.WriteLine("\n--- Pagination Iterator ---");
    var numbers = Enumerable.Range(1, 23).ToList();
    var paginationIterator = new PaginationIterator<int>(numbers, 5);

    Console.WriteLine($"📄 Total items: {numbers.Count}, Page size: 5, Total pages: {paginationIterator.TotalPages}");

    int pageNumber = 1;
    while (paginationIterator.HasNext())
    {
        var page = paginationIterator.Next();
        Console.WriteLine($"   Page {pageNumber++}: [{string.Join(", ", page)}]");
    }

    Console.WriteLine("\n🎯 Iterator Pattern Key Benefits:");
    Console.WriteLine("✅ Provides uniform interface for traversal");
    Console.WriteLine("✅ Hides internal collection structure");
    Console.WriteLine("✅ Multiple iterators can traverse same collection");
    Console.WriteLine("✅ Supports different traversal algorithms");
    Console.WriteLine("✅ Lazy evaluation with yield return");
    Console.WriteLine("✅ Memory efficient for large collections");
}

static void DemonstrateInterpreterPattern()
{
    Console.WriteLine("\n=== Interpreter Pattern ===");

    // Mathematical Expression Evaluator
    Console.WriteLine("--- Mathematical Expression Evaluator ---");

    var evaluator = new MathExpressionEvaluator();
    var context = evaluator.Context;

    // Set some variables
    context.SetVariable("x", 10);
    context.SetVariable("y", 5);
    context.SetVariable("pi", Math.PI);
    context.SetVariable("e", Math.E);

    Console.WriteLine("🔢 Variables defined:");
    foreach (var (name, value) in context.Variables)
    {
        Console.WriteLine($"   {name} = {value:G}");
    }

    // Test basic arithmetic expressions
    Console.WriteLine("\n--- Basic Arithmetic ---");
    var basicExpressions = new[]
    {
        "2 + 3 * 4",
        "x + y * 2",
        "(x + y) * 2",
        "x^2 + y^2",
        "x % 3",
        "-x + y"
    };

    foreach (var expr in basicExpressions)
    {
        try
        {
            var result = evaluator.Evaluate(expr);
            Console.WriteLine($"   {expr} = {result:G}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {expr} = ERROR: {ex.Message}");
        }
    }

    // Test function calls
    Console.WriteLine("\n--- Mathematical Functions ---");
    var functionExpressions = new[]
    {
        "sqrt(x + y)",
        "sin(pi / 2)",
        "cos(0)",
        "abs(-15)",
        "pow(2, 8)",
        "max(x, y, 20)",
        "min(x, y, 3)",
        "sum(1, 2, 3, 4, 5)",
        "avg(10, 20, 30)"
    };

    foreach (var expr in functionExpressions)
    {
        try
        {
            var result = evaluator.Evaluate(expr);
            Console.WriteLine($"   {expr} = {result:G}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {expr} = ERROR: {ex.Message}");
        }
    }

    // Test conditional expressions
    Console.WriteLine("\n--- Conditional Expressions ---");
    var conditionalExpressions = new[]
    {
        "x > y ? x : y",
        "x < 5 ? 0 : x * 2",
        "x + y > 10 ? sqrt(x + y) : x + y"
    };

    foreach (var expr in conditionalExpressions)
    {
        try
        {
            var result = evaluator.Evaluate(expr);
            Console.WriteLine($"   {expr} = {result:G}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {expr} = ERROR: {ex.Message}");
        }
    }

    // Complex expression with nested functions and conditionals
    Console.WriteLine("\n--- Complex Expressions ---");
    var complexExpressions = new[]
    {
        "sin(pi * x / 180) + cos(pi * y / 180)",
        "pow(e, x / 10) * log(x + 1)",
        "x > 0 ? sqrt(x) : -sqrt(-x)",
        "max(sin(pi/4), cos(pi/4), tan(pi/4))"
    };

    foreach (var expr in complexExpressions)
    {
        try
        {
            var result = evaluator.Evaluate(expr);
            Console.WriteLine($"   {expr} = {result:G}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {expr} = ERROR: {ex.Message}");
        }
    }

    // Expression Analysis
    Console.WriteLine("\n--- Expression Analysis ---");
    var analyzer = new ExpressionAnalyzer();

    var analysisExpressions = new[]
    {
        "x + y * z",
        "sin(pi * angle / 180)",
        "a > 0 ? sqrt(a) : 0",
        "pow(base, exp) + log(value)"
    };

    // Set additional variables for analysis
    context.SetVariable("z", 2);
    context.SetVariable("angle", 45);
    context.SetVariable("a", 16);
    context.SetVariable("base", 2);
    context.SetVariable("exp", 3);
    context.SetVariable("value", 10);

    foreach (var exprStr in analysisExpressions)
    {
        try
        {
            var expression = evaluator.Parse(exprStr);
            var info = expression.Accept(analyzer);
            var result = expression.Interpret(context);

            Console.WriteLine($"\n📊 Expression: {exprStr}");
            Console.WriteLine($"   Result: {result:G}");
            Console.WriteLine($"   Type: {info.Type}");
            Console.WriteLine($"   Depth: {info.Depth}");
            Console.WriteLine($"   Variables: [{string.Join(", ", info.Variables)}]");
            Console.WriteLine($"   Functions: [{string.Join(", ", info.Functions)}]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n📊 Expression: {exprStr}");
            Console.WriteLine($"   ERROR: {ex.Message}");
        }
    }

    // Custom function definition
    Console.WriteLine("\n--- Custom Functions ---");
    context.SetFunction("factorial", args =>
    {
        var n = (int)args[0];
        double result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }

        return result;
    });

    context.SetFunction("fibonacci", args =>
    {
        var n = (int)args[0];
        if (n <= 1) return n;

        double a = 0, b = 1;
        for (int i = 2; i <= n; i++)
        {
            var temp = a + b;
            a = b;
            b = temp;
        }

        return b;
    });

    var customFunctionExpressions = new[]
    {
        "factorial(5)",
        "fibonacci(10)",
        "factorial(3) + fibonacci(7)"
    };

    foreach (var expr in customFunctionExpressions)
    {
        try
        {
            var result = evaluator.Evaluate(expr);
            Console.WriteLine($"   {expr} = {result:G}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {expr} = ERROR: {ex.Message}");
        }
    }

    // Error handling demonstration
    Console.WriteLine("\n--- Error Handling ---");
    var errorExpressions = new[]
    {
        "10 / 0", // Division by zero
        "undefinedVar + 5", // Undefined variable
        "unknownFunc(10)", // Unknown function
        "2 + + 3", // Syntax error
        "sqrt(-1" // Missing parenthesis
    };

    foreach (var expr in errorExpressions)
    {
        try
        {
            var result = evaluator.Evaluate(expr);
            Console.WriteLine($"   {expr} = {result:G}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {expr} = ❌ {ex.GetType().Name}: {ex.Message}");
        }
    }

    Console.WriteLine("\n🎯 Interpreter Pattern Key Benefits:");
    Console.WriteLine("✅ Defines grammar for domain-specific language");
    Console.WriteLine("✅ Easy to extend with new expressions");
    Console.WriteLine("✅ Separates parsing from interpretation");
    Console.WriteLine("✅ Supports complex nested expressions");
    Console.WriteLine("✅ Enables runtime expression evaluation");
    Console.WriteLine("✅ Visitor pattern for expression analysis");
}

static async Task DemonstrateMediatorPattern()
{
    Console.WriteLine("\n=== Mediator Pattern ===");

    // Chat Room Mediator Example
    Console.WriteLine("--- Chat Room Mediator ---");

    var chatRoom = new ChatRoomMediator();

    // Create chat participants
    var alice = new ChatUser("alice", "Alice");
    var bob = new ChatUser("bob", "Bob");
    var charlie = new ChatUser("charlie", "Charlie");
    var bot = new ChatBot();

    // Register participants with mediator
    chatRoom.RegisterColleague(alice);
    await Task.Delay(200);

    chatRoom.RegisterColleague(bob);
    await Task.Delay(200);

    chatRoom.RegisterColleague(charlie);
    await Task.Delay(200);

    chatRoom.RegisterColleague(bot);
    await Task.Delay(500);

    Console.WriteLine("\n--- Public Chat Messages ---");

    // Public chat messages
    await alice.SendMessage("Hello everyone! 👋");
    await Task.Delay(300);

    await bob.SendMessage("Hi Alice! Great to see you here!");
    await Task.Delay(300);

    await charlie.SendMessage("Hey folks! What's everyone working on?");
    await Task.Delay(300);

    Console.WriteLine("\n--- Bot Commands ---");

    // Bot interaction
    await alice.SendMessage("/help");
    await Task.Delay(500);

    await bob.SendMessage("/time");
    await Task.Delay(500);

    await charlie.SendMessage("/joke");
    await Task.Delay(500);

    await alice.SendMessage("/weather");
    await Task.Delay(500);

    await bob.SendMessage("/unknown");
    await Task.Delay(500);

    Console.WriteLine("\n--- Private Messages ---");

    // Private messages
    await alice.SendMessage("Want to collaborate on that project?", "bob");
    await Task.Delay(300);

    await bob.SendMessage("Sure! Let's discuss it offline.", "alice");
    await Task.Delay(300);

    // Try to send to non-existent user
    await charlie.SendMessage("Are you there?", "diana");
    await Task.Delay(300);

    Console.WriteLine("\n--- User Leaving Chat ---");

    // User leaves
    chatRoom.RemoveColleague(charlie);
    await Task.Delay(500);

    await alice.SendMessage("Charlie left? That's too bad.");
    await Task.Delay(300);

    Console.WriteLine($"\n📊 Chat Statistics:");
    Console.WriteLine($"   Total messages: {chatRoom.MessageHistory.Count}");
    Console.WriteLine($"   Active participants: {chatRoom.Participants.Count}");

    // Dialog Mediator Example
    Console.WriteLine("\n--- Dialog Form Mediator ---");

    var dialog = new DialogMediator();

    // Create form components
    var emailField = new InputField("Email");
    var passwordField = new InputField("Password");
    var ageField = new InputField("Age");
    var submitButton = new SubmitButton();
    var resetButton = new ResetButton();

    // Register components with mediator
    dialog.RegisterColleague(emailField);
    dialog.RegisterColleague(passwordField);
    dialog.RegisterColleague(ageField);
    dialog.RegisterColleague(submitButton);
    dialog.RegisterColleague(resetButton);

    Console.WriteLine("\n--- Form Field Updates ---");

    // Simulate user input
    await emailField.SetValue("alice");
    await Task.Delay(200);

    await passwordField.SetValue("123");
    await Task.Delay(200);

    await ageField.SetValue("25");
    await Task.Delay(200);

    Console.WriteLine("\n--- Fix Validation Errors ---");

    // Fix email validation
    await emailField.SetValue("alice@example.com");
    await Task.Delay(200);

    // Fix password validation
    await passwordField.SetValue("securepassword123");
    await Task.Delay(200);

    Console.WriteLine("\n--- Form Submission ---");

    // Try to submit form
    await submitButton.Click();
    await Task.Delay(300);

    Console.WriteLine("\n--- Form Reset ---");

    // Reset form
    await resetButton.Click();
    await Task.Delay(300);

    Console.WriteLine("\n--- Invalid Age Test ---");

    // Test invalid age
    await emailField.SetValue("bob@example.com");
    await passwordField.SetValue("password123");
    await ageField.SetValue("15"); // Too young
    await Task.Delay(300);

    await submitButton.Click(); // Should fail
    await Task.Delay(300);

    // Fix age
    await ageField.SetValue("30");
    await Task.Delay(300);

    await submitButton.Click(); // Should succeed
    await Task.Delay(300);

    Console.WriteLine($"\n📊 Form State:");
    Console.WriteLine($"   Valid: {dialog.IsFormValid}");
    Console.WriteLine($"   Fields: {string.Join(", ", dialog.FormData.Keys)}");
    Console.WriteLine(
        $"   Errors: {(dialog.ValidationErrors.Any() ? string.Join(", ", dialog.ValidationErrors.Keys) : "None")}");

    Console.WriteLine("\n🎯 Mediator Pattern Key Benefits:");
    Console.WriteLine("✅ Promotes loose coupling between objects");
    Console.WriteLine("✅ Centralizes complex communication logic");
    Console.WriteLine("✅ Makes interaction easier to understand and maintain");
    Console.WriteLine("✅ Easy to extend with new participants");
    Console.WriteLine("✅ Reusable interaction patterns");
    Console.WriteLine("✅ Simplifies object protocols");
}

static async Task DemonstrateMementoPattern()
{
    Console.WriteLine("=== Memento Pattern ===");
    Console.WriteLine("Captures and restores object state without violating encapsulation\n");

    // 1. Text Editor Example with Undo/Redo
    Console.WriteLine("📝 Text Editor Example:");
    var editor = new MementoTextEditor();
    var caretaker = new MementoCaretaker<MementoTextEditorMemento>();

    Console.WriteLine("\n1. Creating initial document:");
    editor.InsertText("Hello World");
    caretaker.SaveMemento(editor.CreateMemento(), "Initial text");

    editor.SetFont("Times New Roman", 14);
    editor.SetBold(true);
    caretaker.SaveMemento(editor.CreateMemento(), "Formatted text");

    editor.InsertText("\nThis is a new line.");
    editor.SetItalic(true);
    caretaker.SaveMemento(editor.CreateMemento(), "Added line with italic");

    Console.WriteLine("\n2. Current editor state:");
    editor.PrintStatus();

    Console.WriteLine("\n3. Memento history:");
    caretaker.PrintHistory();

    Console.WriteLine("\n4. Restoring to previous state:");
    var previousState = caretaker.RestoreMemento(1);
    if (previousState != null)
    {
        editor.RestoreFromMemento(previousState);
        editor.PrintStatus();
    }

    Console.WriteLine("\n" + new string('-', 60));

    // 2. Game State Example with Save/Load
    Console.WriteLine("\n🎮 Game State Example:");
    var gameState = new GameState();
    var gameCaretaker = new MementoCaretaker<GameStateMemento>();

    Console.WriteLine("\n1. Starting game progression:");
    gameState.AddScore(100);
    gameState.AddItem(new Item("Health Potion", "Consumable", 3, new()));
    gameCaretaker.SaveMemento(gameState.CreateMemento(), "Game Start");

    gameState.LevelUp();
    gameState.MovePlayer(10, 15, "Forest");
    gameState.AddScore(250);
    gameState.AddItem(new Item("Magic Sword", "Weapon", 1, new()));
    gameCaretaker.SaveMemento(gameState.CreateMemento(), "Level 2 Start");

    gameState.UseItem("Health Potion");
    gameState.LoseLife();
    gameState.AddScore(150);
    gameCaretaker.SaveMemento(gameState.CreateMemento(), "After Battle");

    Console.WriteLine("\n2. Current game state:");
    gameState.PrintStatus();

    Console.WriteLine("\n3. Loading checkpoint:");
    var checkpoint = gameCaretaker.RestoreMemento(1);
    if (checkpoint != null)
    {
        gameState.RestoreFromMemento(checkpoint);
        gameState.PrintStatus();
    }

    Console.WriteLine("\n" + new string('-', 60));

    // 3. Snapshot Manager with Branching
    Console.WriteLine("\n🌳 Snapshot Manager with Branching:");
    var snapshotManager = new SnapshotManager<GameStateMemento>();

    Console.WriteLine("\n1. Saving to main branch:");
    snapshotManager.SaveSnapshot(gameState.CreateMemento(), "Main Progress");

    Console.WriteLine("\n2. Creating experimental branch:");
    snapshotManager.CreateBranch("experimental");
    snapshotManager.SwitchBranch("experimental");

    // Make experimental changes
    gameState.AddScore(1000);
    gameState.GainLife();
    gameState.SetGameData("cheat_mode", true);
    snapshotManager.SaveSnapshot(gameState.CreateMemento(), "Experimental Changes");

    Console.WriteLine("\n3. Branch overview:");
    snapshotManager.PrintBranches();

    Console.WriteLine("\n4. Switching back to main branch:");
    snapshotManager.SwitchBranch("main");
    var mainSnapshot = snapshotManager.GetSnapshot(0);
    if (mainSnapshot != null)
    {
        gameState.RestoreFromMemento(mainSnapshot);
        gameState.PrintStatus();
    }

    Console.WriteLine("\n" + new string('-', 60));

    // 4. Auto-save Example
    Console.WriteLine("\n💾 Auto-save Example:");
    using var autoSaveCaretaker = new AutoSaveCaretaker<MementoTextEditorMemento>(TimeSpan.FromSeconds(2));
    autoSaveCaretaker.SetMementoFactory(() => editor.CreateMemento());
    autoSaveCaretaker.StartAutoSave();

    Console.WriteLine("\n1. Making changes with auto-save:");
    editor.InsertText("\nAuto-saved content");
    await Task.Delay(2500); // Wait for auto-save

    editor.InsertText("\nMore content");
    await Task.Delay(2500); // Wait for another auto-save

    autoSaveCaretaker.StopAutoSave();

    Console.WriteLine($"\n2. Auto-save history count: {autoSaveCaretaker.GetHistory().Count}");

    Console.WriteLine("\n" + new string('-', 60));

    // 5. Compression Example
    Console.WriteLine("\n🗜️ Compression Example:");
    var largeGameState = new GameState();

    // Create a larger state
    for (int i = 0; i < 20; i++)
    {
        largeGameState.AddItem(new Item($"Item_{i}", "Equipment", i + 1, new()));
        largeGameState.SetGameData($"data_{i}", $"value_{i}");
    }

    Console.WriteLine("\n1. Creating compressed memento:");
    var compressedMemento = new CompressedMemento(largeGameState, "Large game state");

    Console.WriteLine($"   Original size: {compressedMemento.OriginalSize} bytes");
    Console.WriteLine($"   Compressed size: {compressedMemento.CompressedSize} bytes");
    Console.WriteLine($"   Compression ratio: {compressedMemento.CompressionRatio:P1}");

    Console.WriteLine("\n🎯 Memento Pattern Key Benefits:");
    Console.WriteLine("✅ Preserves object encapsulation while enabling state capture");
    Console.WriteLine("✅ Provides undo/redo functionality for complex operations");
    Console.WriteLine("✅ Supports save/load checkpoints in applications");
    Console.WriteLine("✅ Enables branching and merging of state histories");
    Console.WriteLine("✅ Allows state compression for memory optimization");
    Console.WriteLine("✅ Facilitates debugging and testing with state snapshots");
    Console.WriteLine("✅ Supports auto-save and periodic state backup");
}

/// <summary>
/// Demonstrates the State pattern with document workflow and vending machine examples
/// </summary>
static async Task DemonstrateStatePattern()
{
    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("🔄 STATE PATTERN DEMONSTRATION");
    Console.WriteLine(new string('=', 50));

    Console.WriteLine("\n=== Document Workflow State Machine ===");
    var document = new DocumentWorkflow("DOC-001", "John Doe", "Technical Specification v2.1");
    document.PrintStatus();

    // Progress through document workflow
    Console.WriteLine("\n--- Submitting document ---");
    document.Submit();
    document.PrintStatus();

    // Wait for automatic transition to reviewing state
    await Task.Delay(2100); // Wait for automatic transition
    Console.WriteLine("\n--- Document now under review ---");
    document.PrintStatus();

    Console.WriteLine("\n--- Approving document ---");
    document.Approve();
    document.PrintStatus();

    Console.WriteLine("\n=== Vending Machine State Machine ===");
    var machine = new VendingMachine();
    machine.PrintStatus();

    // Successful transaction
    Console.WriteLine("\n--- Inserting money ---");
    machine.InsertMoney(2.50m);
    machine.PrintStatus();

    Console.WriteLine("\n--- Selecting product ---");
    machine.SelectProduct("Chips");

    await Task.Delay(3000); // Wait for dispensing to complete
    machine.PrintStatus();

    // Transaction with insufficient funds
    Console.WriteLine("\n--- Insufficient funds scenario ---");
    machine.InsertMoney(1.00m);
    machine.SelectProduct("Chips"); // Chips cost $2.00
    machine.ReturnMoney();
    machine.PrintStatus();

    // Out of order scenario
    Console.WriteLine("\n--- Out of order scenario ---");
    machine.InsertMoney(1.50m);
    machine.TransitionTo(VendingMachine.OutOfOrder);
    machine.PrintStatus();

    Console.WriteLine("\n--- Restocking machine ---");
    machine.Restock();
    machine.PrintStatus();

    Console.WriteLine("\n🎯 State Pattern Key Benefits:");
    Console.WriteLine("✅ Encapsulates state-dependent behavior in separate objects");
    Console.WriteLine("✅ Makes state transitions explicit and type-safe");
    Console.WriteLine("✅ Eliminates complex conditional logic based on state");
    Console.WriteLine("✅ Enables runtime behavior changes based on state");
    Console.WriteLine("✅ Facilitates adding new states without modifying existing code");
}

static async Task DemonstrateStrategyPattern()
{
    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("🔄 STRATEGY PATTERN");
    Console.WriteLine(new string('=', 50));
    Console.WriteLine("Defines a family of algorithms, encapsulates each one,");
    Console.WriteLine("and makes them interchangeable at runtime.\n");

    // 1. Payment Processing Strategies
    Console.WriteLine("💳 Payment Processing Strategies");
    Console.WriteLine(new string('-', 40));

    var paymentProcessor = new PaymentProcessor();
    var amount = 1500.00m;

    // Different payment strategies
    var creditCard = new CreditCardPaymentStrategy(
        "4111111111111111",
        "John Doe",
        "12/25",
        "123");

    var paypal = new PayPalPaymentStrategy("john.doe@example.com", isVerified: true);

    var crypto = new CryptocurrencyPaymentStrategy(
        "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
        CryptocurrencyPaymentStrategy.CryptoNetwork.Bitcoin);

    var bankTransfer = new BankTransferPaymentStrategy(
        "123456789",
        "021000021",
        "Chase Bank");

    // Compare all payment strategies
    paymentProcessor.CompareStrategies(amount, creditCard, paypal, crypto, bankTransfer);

    // Process payments with different strategies
    Console.WriteLine("\n🔄 Processing Payments:");

    // Credit card payment with fraud detection
    var metadata = new Dictionary<string, object>
    {
        ["recentTransactionCount"] = 2,
        ["isInternational"] = false
    };
    await paymentProcessor.ProcessPaymentAsync(creditCard, amount, metadata: metadata);

    // PayPal payment
    await paymentProcessor.ProcessPaymentAsync(paypal, 750.00m);

    // Cryptocurrency payment (no refunds)
    await paymentProcessor.ProcessPaymentAsync(crypto, 2500.00m);

    // Bank transfer (low fees for high amounts)
    await paymentProcessor.ProcessPaymentAsync(bankTransfer, 5000.00m);

    // Show transaction history
    paymentProcessor.PrintTransactionHistory();

    // 2. Sorting Algorithm Strategies
    Console.WriteLine("\n\n🔢 Sorting Algorithm Strategies");
    Console.WriteLine(new string('-', 40));

    var sortingContext = new SortingContext<int>();

    // Create test data
    var random = new Random(42); // Fixed seed for reproducible results
    var smallArray = Enumerable.Range(1, 20).OrderBy(_ => random.Next()).ToArray();
    var largeArray = Enumerable.Range(1, 1000).OrderBy(_ => random.Next()).ToArray();

    Console.WriteLine($"Original small array: [{string.Join(", ", smallArray.Take(10))}...]");

    // Different sorting strategies
    var bubbleSort = new BubbleSortStrategy<int>();
    var quickSort = new QuickSortStrategy<int>();
    var mergeSort = new MergeSortStrategy<int>();
    var heapSort = new HeapSortStrategy<int>();

    // Compare sorting algorithms on small array
    Console.WriteLine("\n📊 Small Array Performance (20 elements):");
    sortingContext.CompareSortingStrategies(smallArray, bubbleSort, quickSort, mergeSort, heapSort);

    // Compare on larger array (exclude bubble sort for performance)
    Console.WriteLine($"\n📊 Large Array Performance (1000 elements):");
    sortingContext.CompareSortingStrategies(largeArray, quickSort, mergeSort, heapSort);

    // Demonstrate individual sorting
    var testArray = new[] { 64, 34, 25, 12, 22, 11, 90 };
    Console.WriteLine($"\n🔄 Sorting [{string.Join(", ", testArray)}]:");

    foreach (var strategy in new ISortingStrategy<int>[] { bubbleSort, quickSort, mergeSort, heapSort })
    {
        var result = sortingContext.Sort(testArray, strategy);
        Console.WriteLine(
            $"   {strategy.Name}: [{string.Join(", ", result.SortedArray)}] ({result.ElapsedMilliseconds}ms)");
    }

    // 3. Data Compression Strategies
    Console.WriteLine("\n\n📦 Data Compression Strategies");
    Console.WriteLine(new string('-', 40));

    var compressionContext = new CompressionContext();

    // Create test data - JSON with repetitive content (compresses well)
    var testData = System.Text.Encoding.UTF8.GetBytes(
        System.Text.Json.JsonSerializer.Serialize(new
        {
            users = Enumerable.Range(1, 100).Select(i => new
            {
                id = i,
                name = $"User {i}",
                email = $"user{i}@example.com",
                department = i % 5 == 0 ? "Engineering" :
                    i % 4 == 0 ? "Marketing" :
                    i % 3 == 0 ? "Sales" : "Support",
                active = true,
                metadata = new { created = DateTime.Now, lastLogin = DateTime.Now }
            }).ToArray()
        }));

    // Different compression strategies
    var gzipCompression = new GzipCompressionStrategy(System.IO.Compression.CompressionLevel.Optimal);
    var deflateCompression = new DeflateCompressionStrategy(System.IO.Compression.CompressionLevel.Optimal);
    var brotliCompression = new BrotliCompressionStrategy(System.IO.Compression.CompressionLevel.Optimal);
    var noCompression = new NoCompressionStrategy();

    // Compare compression algorithms
    compressionContext.CompareCompressionStrategies(testData,
        noCompression, gzipCompression, deflateCompression, brotliCompression);

    // 4. Data Validation Strategies
    Console.WriteLine("\n\n✅ Data Validation Strategies");
    Console.WriteLine(new string('-', 40));

    var emailValidationContext = new ValidationContext<string>();
    var passwordValidationContext = new ValidationContext<string>();

    // Test email validation with different strategies
    var testEmails = new[]
    {
        "john.doe@example.com", // Valid
        "invalid-email", // Invalid - no @
        "test@", // Invalid - no domain
        "user@domain", // Warning - no TLD
        "very.long.email.address.that.exceeds.the.maximum.length@verylongdomainname.com", // Too long
        "test..test@example.com" // Invalid - consecutive dots
    };

    var basicEmailValidation = new EmailValidationStrategy(EmailValidationStrategy.EmailValidationLevel.Basic);
    var standardEmailValidation = new EmailValidationStrategy(EmailValidationStrategy.EmailValidationLevel.Standard);
    var strictEmailValidation = new EmailValidationStrategy(EmailValidationStrategy.EmailValidationLevel.Strict);

    Console.WriteLine("📧 Email Validation Comparison:");
    foreach (var email in testEmails.Take(3))
    {
        Console.WriteLine($"\n🔍 Testing: '{email}'");
        emailValidationContext.ValidateWithMultipleStrategies(email,
            basicEmailValidation, standardEmailValidation, strictEmailValidation);
    }

    // Test password validation
    var testPasswords = new[]
    {
        "MySecure123!", // Strong
        "password", // Weak - common
        "12345678", // Weak - only numbers
        "VeryStrongP@ssw0rd2024!", // Very strong
        "abc", // Too short
    };

    var passwordValidation = new PasswordValidationStrategy(new PasswordPolicy
    {
        MinLength = 8,
        RequireUppercase = true,
        RequireLowercase = true,
        RequireNumbers = true,
        RequireSpecialChars = true
    });

    Console.WriteLine("\n\n🔐 Password Validation:");
    foreach (var password in testPasswords.Take(3))
    {
        Console.WriteLine($"\n🔍 Testing password: '{password}'");
        var result = passwordValidation.Validate(password);
        Console.WriteLine($"   Result: {(result.IsValid ? "✅ VALID" : "❌ INVALID")}");
        Console.WriteLine($"   Message: {result.Message}");
        if (result.HasWarnings && result.Warnings != null)
        {
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"   ⚠️  Warning: {warning}");
            }
        }
    }

    // 5. Runtime Strategy Selection
    Console.WriteLine("\n\n🔄 Runtime Strategy Selection");
    Console.WriteLine(new string('-', 40));

    // Simulate choosing payment method based on amount and user preference
    var paymentStrategies = new Dictionary<string, IPaymentStrategy>
    {
        ["credit"] = creditCard,
        ["paypal"] = paypal,
        ["crypto"] = crypto,
        ["bank"] = bankTransfer
    };

    var testScenarios = new[]
    {
        (amount: 50.00m, preference: "credit", reason: "Small purchase"),
        (amount: 2000.00m, preference: "bank", reason: "Large purchase - minimize fees"),
        (amount: 500.00m, preference: "crypto", reason: "Privacy focused"),
        (amount: 100.00m, preference: "paypal", reason: "Quick checkout")
    };

    Console.WriteLine("💡 Choosing optimal payment strategy:");
    foreach (var scenario in testScenarios)
    {
        Console.WriteLine(
            $"\n💰 Amount: ${scenario.amount:F2} | Preference: {scenario.preference} | {scenario.reason}");

        var strategy = paymentStrategies[scenario.preference];
        var fee = strategy.CalculateFee(scenario.amount);
        var feePercentage = scenario.amount > 0 ? (fee / scenario.amount * 100) : 0;

        Console.WriteLine($"   Selected: {strategy.Name}");
        Console.WriteLine($"   Fee: ${fee:F2} ({feePercentage:F2}%)");
        Console.WriteLine($"   Total: ${scenario.amount + fee:F2}");
        Console.WriteLine($"   Refundable: {(strategy.SupportsRefunds ? "Yes" : "No")}");
    }

    // Strategy Pattern Benefits
    Console.WriteLine(new string('=', 50));
    Console.WriteLine("✨ Strategy Pattern Benefits:");
    Console.WriteLine("✅ Eliminates complex conditional logic for algorithm selection");
    Console.WriteLine("✅ Makes algorithms interchangeable at runtime");
    Console.WriteLine("✅ Follows Open/Closed Principle - easy to add new strategies");
    Console.WriteLine("✅ Encapsulates algorithm families and makes them testable");
    Console.WriteLine("✅ Enables performance comparison and optimization");
    Console.WriteLine("✅ Supports different algorithm variants for different contexts");
}

static async Task DemonstrateTemplateMethodPattern()
{
    Console.WriteLine("\n=== Template Method Pattern ===");
    Console.WriteLine("🏗️ Defines skeleton of algorithm, subclasses implement specific steps");

    // Data Processing Pipeline Demo
    Console.WriteLine("\n📊 Data Processing Pipeline Demo:");
    Console.WriteLine(new string('-', 40));

    var numberProcessor = new NumberProcessor();
    var textProcessor = new TextProcessor();
    var jsonProcessor = new JsonProcessor();

    // Number processing
    Console.WriteLine("\n🔢 Processing Numbers:");
    var numberOptions = new ProcessingOptions
    {
        EnablePreProcessing = true,
        EnablePostProcessing = true,
        ParallelProcessing = false,
        CustomOptions = new Dictionary<string, object>
        {
            ["multiplier"] = 3,
            ["threshold"] = 10,
            ["limit"] = 5
        }
    };

    var numbers = new[] { 5, 2, 8, 2, 15, 3, 8, 12, 6, 20, 1, 9 };
    var numberResult = numberProcessor.ProcessData(numbers, numberOptions);

    Console.WriteLine($"\n📈 Processing Result:");
    Console.WriteLine($"   Input: [{string.Join(", ", numbers)}]");
    Console.WriteLine($"   Output: [{string.Join(", ", numberResult.Data)}]");
    Console.WriteLine($"   Status: {numberResult.Status}");
    Console.WriteLine($"   Success Rate: {numberResult.Statistics.SuccessRate:P1}");
    Console.WriteLine($"   Processing Time: {numberResult.Statistics.ProcessingTime.TotalMilliseconds:F0}ms");

    // Text processing
    Console.WriteLine("\n📝 Processing Text:");
    var textOptions = new ProcessingOptions
    {
        EnablePreProcessing = true,
        EnablePostProcessing = true,
        CustomOptions = new Dictionary<string, object>
        {
            ["prefix"] = "✨ ",
            ["suffix"] = " ⭐",
            ["caseTransform"] = "title",
            ["normalize"] = true,
            ["minLength"] = 5,
            ["maxLength"] = 20
        }
    };

    var texts = new[]
    {
        "hello world", "   design patterns   ", "template method", "", "c# programming", "software architecture", "a",
        "behavioral patterns rock!"
    };
    var textResult = textProcessor.ProcessData(texts, textOptions);

    Console.WriteLine($"\n📈 Processing Result:");
    Console.WriteLine($"   Input Count: {texts.Length} texts");
    Console.WriteLine($"   Valid Output: {textResult.Data.Count} texts");
    foreach (var text in textResult.Data)
    {
        Console.WriteLine($"   • {text}");
    }

    Console.WriteLine($"   Success Rate: {textResult.Statistics.SuccessRate:P1}");

    // JSON processing
    Console.WriteLine("\n🔄 Processing JSON:");
    var jsonOptions = new ProcessingOptions
    {
        EnablePreProcessing = false,
        EnablePostProcessing = true,
        CustomOptions = new Dictionary<string, object>
        {
            ["indented"] = true,
            ["minify"] = false
        }
    };

    var jsonTexts = new[]
    {
        "{\"name\":\"John\",\"age\":30}",
        "[1,2,3,4,5]",
        "invalid json",
        "{\"pattern\":\"template method\",\"type\":\"behavioral\",\"benefits\":[\"code reuse\",\"consistency\"]}",
        "{\"complex\":{\"nested\":{\"data\":true}}}"
    };

    var jsonResult = jsonProcessor.ProcessData(jsonTexts, jsonOptions);

    Console.WriteLine($"\n📈 Processing Result:");
    Console.WriteLine($"   Input: {jsonTexts.Length} JSON strings");
    Console.WriteLine($"   Valid: {jsonResult.Data.Count} formatted JSONs");
    Console.WriteLine($"   Success Rate: {jsonResult.Statistics.SuccessRate:P1}");
    foreach (var json in jsonResult.Data.Take(2))
    {
        Console.WriteLine($"   📄 Formatted JSON:\n{json}\n");
    }

    // Document Generation Demo
    Console.WriteLine("\n📄 Document Generation Demo:");
    Console.WriteLine(new string('-', 40));

    var reportGenerator = new TechnicalReportGenerator();
    var proposalGenerator = new BusinessProposalGenerator();

    // Technical report
    Console.WriteLine("\n📊 Generating Technical Report:");
    var reportRequest = new DocumentRequest
    {
        Title = "System Performance Analysis Report",
        Author = "Engineering Team",
        Subject = "Q4 2024 Performance Review",
        Keywords = ["performance", "analysis", "metrics", "optimization"],
        Options = new DocumentOptions
        {
            IncludeHeader = true,
            IncludeFooter = true,
            IncludeTableOfContents = true
        },
        Data = new Dictionary<string, object>
        {
            ["specifications"] =
                "• CPU: Intel i9-12900K\n• RAM: 32GB DDR4\n• Storage: 1TB NVMe SSD\n• Network: Gigabit Ethernet",
            ["performance"] =
                "• Response Time: 125ms average\n• Throughput: 1,500 req/sec\n• Error Rate: 0.02%\n• Uptime: 99.97%",
            ["architecture"] = "Microservices architecture with load balancing, auto-scaling, and distributed caching",
            ["codeExamples"] =
                "// Performance optimization example\nvar cache = new MemoryCache();\nvar result = cache.GetOrCreate(key, factory);"
        }
    };

    var report = reportGenerator.GenerateDocument(reportRequest);
    Console.WriteLine($"📄 Generated Report: {report.Title}");
    Console.WriteLine($"   Pages: {report.PageCount}");
    Console.WriteLine($"   Success: {report.Success}");
    Console.WriteLine($"   Author: {report.Metadata.Author}");
    Console.WriteLine($"   Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm}");

    // Business proposal
    Console.WriteLine("\n💼 Generating Business Proposal:");
    var proposalRequest = new DocumentRequest
    {
        Title = "Digital Transformation Initiative Proposal",
        Author = "Solutions Architecture Team",
        Subject = "Enterprise Modernization Project",
        Keywords = ["digital transformation", "modernization", "ROI", "strategy"],
        Options = new DocumentOptions
        {
            IncludeHeader = true,
            IncludeFooter = true,
            IncludeTableOfContents = false
        },
        Data = new Dictionary<string, object>
        {
            ["problem"] =
                "Legacy systems causing 40% productivity loss, manual processes creating bottlenecks, and outdated technology limiting scalability.",
            ["solution"] =
                "Implement cloud-native architecture, automate key processes, modernize user interfaces, and establish CI/CD pipelines.",
            ["timeline"] =
                "Phase 1: Analysis (2 months)\nPhase 2: Core Migration (4 months)\nPhase 3: Process Automation (3 months)\nPhase 4: Optimization (2 months)",
            ["budget"] = "$1,250,000"
        }
    };

    var proposal = proposalGenerator.GenerateDocument(proposalRequest);
    Console.WriteLine($"💼 Generated Proposal: {proposal.Title}");
    Console.WriteLine($"   Pages: {proposal.PageCount}");
    Console.WriteLine($"   Success: {proposal.Success}");
    Console.WriteLine($"   Processing Time: {proposal.Logs.Count} log entries");

    // Game AI Demo
    Console.WriteLine("\n🎮 Game AI Decision Making Demo:");
    Console.WriteLine(new string('-', 40));

    var aggressiveAi = new AggressiveAi();
    var defensiveAi = new DefensiveAi();

    // Scenario 1: Balanced fight
    Console.WriteLine("\n⚔️ Scenario 1: Balanced Combat");
    var gameState1 = new GameContext
    {
        PlayerHealth = 80,
        EnemyHealth = 75,
        EnemyDistance = 3,
        Inventory = ["Sword", "Shield", "Health Potion"]
    };

    var aggressiveDecision1 = aggressiveAi.MakeDecision(gameState1);
    var defensiveDecision1 = defensiveAi.MakeDecision(gameState1);

    Console.WriteLine(
        $"🤖 Aggressive AI Decision: {aggressiveDecision1.Action.Name} (Confidence: {aggressiveDecision1.Confidence:P1})");
    Console.WriteLine($"   Reasoning: {aggressiveDecision1.Reasoning}");
    Console.WriteLine(
        $"🤖 Defensive AI Decision: {defensiveDecision1.Action.Name} (Confidence: {defensiveDecision1.Confidence:P1})");
    Console.WriteLine($"   Reasoning: {defensiveDecision1.Reasoning}");

    // Scenario 2: Low health
    Console.WriteLine("\n🩹 Scenario 2: Critical Health");
    var gameState2 = new GameContext
    {
        PlayerHealth = 15,
        EnemyHealth = 60,
        EnemyDistance = 2,
        Inventory = ["Health Potion", "Smoke Bomb"]
    };

    var aggressiveDecision2 = aggressiveAi.MakeDecision(gameState2);
    var defensiveDecision2 = defensiveAi.MakeDecision(gameState2);

    Console.WriteLine(
        $"🤖 Aggressive AI Decision: {aggressiveDecision2.Action.Name} (Confidence: {aggressiveDecision2.Confidence:P1})");
    Console.WriteLine(
        $"🤖 Defensive AI Decision: {defensiveDecision2.Action.Name} (Confidence: {defensiveDecision2.Confidence:P1})");

    // Scenario 3: Overwhelming advantage
    Console.WriteLine("\n💪 Scenario 3: Overwhelming Advantage");
    var gameState3 = new GameContext
    {
        PlayerHealth = 95,
        EnemyHealth = 20,
        EnemyDistance = 1,
        Inventory = ["Legendary Sword", "Magic Shield", "Healing Scroll", "Power Boost"]
    };

    var aggressiveDecision3 = aggressiveAi.MakeDecision(gameState3);
    var defensiveDecision3 = defensiveAi.MakeDecision(gameState3);

    Console.WriteLine(
        $"🤖 Aggressive AI Decision: {aggressiveDecision3.Action.Name} (Confidence: {aggressiveDecision3.Confidence:P1})");
    Console.WriteLine(
        $"🤖 Defensive AI Decision: {defensiveDecision3.Action.Name} (Confidence: {defensiveDecision3.Confidence:P1})");

    // Performance comparison
    Console.WriteLine("\n📊 AI Performance Statistics:");
    Console.WriteLine(
        $"Aggressive AI - Total Decisions: {aggressiveDecision3.Statistics.TotalDecisions}, Avg Time: {aggressiveDecision3.Statistics.AverageDecisionTime.TotalMilliseconds:F1}ms");
    Console.WriteLine(
        $"Defensive AI - Total Decisions: {defensiveDecision3.Statistics.TotalDecisions}, Avg Time: {defensiveDecision3.Statistics.AverageDecisionTime.TotalMilliseconds:F1}ms");

    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("✨ Template Method Pattern Benefits:");
    Console.WriteLine("✅ Defines algorithm skeleton while allowing step customization");
    Console.WriteLine("✅ Promotes code reuse through inheritance and shared structure");
    Console.WriteLine("✅ Provides hooks for subclass customization without changing flow");
    Console.WriteLine("✅ Ensures consistent algorithm structure across implementations");
    Console.WriteLine("✅ Implements Hollywood Principle: 'Don't call us, we'll call you'");
    Console.WriteLine("✅ Separates invariant parts from variant parts of algorithms");
    Console.WriteLine("✅ Enables controlled extension points for framework development");
    Console.WriteLine("✅ Provides template for complex multi-step processes");
}

#region Visitor Pattern

static async Task DemonstrateVisitorPattern()
{
    Console.WriteLine("=== Visitor Pattern ===");
    Console.WriteLine("🌐 Separates algorithms from objects using double dispatch");
    Console.WriteLine();

    await Task.Delay(10); // Simulate async operation

    // Document Processing Demo
    Console.WriteLine("📄 Document Processing Demo:");
    Console.WriteLine(new string('-', 40));

    var document = CreateSampleDocument();

    // Export to HTML
    Console.WriteLine("\n🔄 Exporting to HTML:");
    var htmlVisitor = new HtmlExportVisitor(new HtmlExportOptions
    {
        IncludeInlineStyles = true,
        PrettyPrint = true,
        IncludeDoctype = false
    });
    var html = document.Accept(htmlVisitor);
    Console.WriteLine("📄 HTML Output (first 300 chars):");
    Console.WriteLine(html.Length > 300 ? html[..300] + "..." : html);

    // Export to Markdown
    Console.WriteLine("\n🔄 Exporting to Markdown:");
    var markdownVisitor = new MarkdownExportVisitor();
    var markdown = document.Accept(markdownVisitor);
    Console.WriteLine("📝 Markdown Output:");
    Console.WriteLine(markdown);

    // Count words
    Console.WriteLine("🔄 Counting words:");
    var wordCountVisitor = new WordCountVisitor();
    var wordCount = document.Accept(wordCountVisitor);
    Console.WriteLine($"📊 Total word count: {wordCount} words");

    // Validate document
    Console.WriteLine("\n🔄 Validating document:");
    var validationVisitor = new ValidationVisitor(new ValidationOptions
    {
        MaxParagraphLength = 1000,
        RequireAltText = true
    });
    var validationResult = document.Accept(validationVisitor);
    Console.WriteLine($"📋 Validation Result: {validationResult}");

    if (!validationResult.IsValid || validationResult.HasWarnings)
    {
        Console.WriteLine("⚠️  Issues found:");
        foreach (var error in validationResult.Errors.Take(3))
        {
            Console.WriteLine($"   ❌ ERROR: {error}");
        }

        foreach (var warning in validationResult.Warnings.Take(3))
        {
            Console.WriteLine($"   ⚠️  WARNING: {warning}");
        }
    }

    // JSON Serialization
    Console.WriteLine("\n🔄 Serializing to JSON:");
    var jsonVisitor = new JsonSerializationVisitor(new SerializationOptions
    {
        PrettyPrint = false,
        IncludeTypeInfo = true
    });
    var firstElement = document.Elements.First();
    var json = firstElement.Accept(jsonVisitor);
    Console.WriteLine($"📄 First element as JSON: {json}");

    Console.WriteLine(new string('-', 60));

    // AST Processing Demo
    Console.WriteLine("\n🌳 Abstract Syntax Tree Demo:");
    Console.WriteLine(new string('-', 40));

    // Build complex AST: 
    // {
    //   x = 5 + 3 * sqrt(16);
    //   y = sin(pi / 4) + cos(pi / 4);
    //   result = pow(x, 2) + pow(y, 2);
    // }
    var ast = new BlockNode(
        new AssignmentNode("x",
            new BinaryOperationNode("+",
                new LiteralNode(5),
                new BinaryOperationNode("*",
                    new LiteralNode(3),
                    new FunctionCallNode("sqrt", new LiteralNode(16))
                )
            )
        ),
        new AssignmentNode("y",
            new BinaryOperationNode("+",
                new FunctionCallNode("sin",
                    new BinaryOperationNode("/", new VariableNode("pi"), new LiteralNode(4))
                ),
                new FunctionCallNode("cos",
                    new BinaryOperationNode("/", new VariableNode("pi"), new LiteralNode(4))
                )
            )
        ),
        new AssignmentNode("result",
            new BinaryOperationNode("+",
                new FunctionCallNode("pow", new VariableNode("x"), new LiteralNode(2)),
                new FunctionCallNode("pow", new VariableNode("y"), new LiteralNode(2))
            )
        )
    );

    // Evaluate AST
    Console.WriteLine("\n🔄 Evaluating expressions:");
    var evaluator = new EvaluationVisitor();
    var result = ast.Accept(evaluator);
    Console.WriteLine($"🧮 Final result: {result}");
    Console.WriteLine($"   x = {evaluator.Visit(new VariableNode("x"))}");
    Console.WriteLine($"   y = {evaluator.Visit(new VariableNode("y"))}");
    Console.WriteLine($"   result = {evaluator.Visit(new VariableNode("result"))}");

    // Generate code
    Console.WriteLine("\n🔄 Generating code:");
    var codeGen = new CodeGenerationVisitor();
    var code = ast.Accept(codeGen);
    Console.WriteLine("💻 Generated C# code:");
    Console.WriteLine(code);

    // Analyze AST
    Console.WriteLine("\n🔄 Analyzing AST structure:");
    var analyzer = new AstAnalysisVisitor();
    ast.Accept(analyzer);
    var analysis = analyzer.GetResult();
    Console.WriteLine("📊 AST Analysis:");
    Console.WriteLine(analysis);

    // Advanced AST examples
    Console.WriteLine("\n🔄 Complex expression evaluation:");

    var expressions = new Dictionary<string, IAstNode>
    {
        ["Quadratic formula"] = new BinaryOperationNode("/",
            new BinaryOperationNode("+",
                new UnaryOperationNode("-", new VariableNode("b")),
                new FunctionCallNode("sqrt",
                    new BinaryOperationNode("-",
                        new FunctionCallNode("pow", new VariableNode("b"), new LiteralNode(2)),
                        new BinaryOperationNode("*",
                            new BinaryOperationNode("*", new LiteralNode(4), new VariableNode("a")),
                            new VariableNode("c")
                        )
                    )
                )
            ),
            new BinaryOperationNode("*", new LiteralNode(2), new VariableNode("a"))
        ),

        ["Compound interest"] = new BinaryOperationNode("*",
            new VariableNode("principal"),
            new FunctionCallNode("pow",
                new BinaryOperationNode("+", new LiteralNode(1), new VariableNode("rate")),
                new VariableNode("time")
            )
        ),

        ["Distance formula"] = new FunctionCallNode("sqrt",
            new BinaryOperationNode("+",
                new FunctionCallNode("pow",
                    new BinaryOperationNode("-", new VariableNode("x2"), new VariableNode("x1")),
                    new LiteralNode(2)
                ),
                new FunctionCallNode("pow",
                    new BinaryOperationNode("-", new VariableNode("y2"), new VariableNode("y1")),
                    new LiteralNode(2)
                )
            )
        )
    };

    foreach (var (name, expr) in expressions)
    {
        Console.WriteLine($"\n📐 {name}:");

        // Set variables for evaluation
        var exprEvaluator = new EvaluationVisitor();

        if (name.Contains("Quadratic"))
        {
            exprEvaluator.SetVariable("a", 1);
            exprEvaluator.SetVariable("b", -5);
            exprEvaluator.SetVariable("c", 6);
        }
        else if (name.Contains("Compound"))
        {
            exprEvaluator.SetVariable("principal", 1000);
            exprEvaluator.SetVariable("rate", 0.05);
            exprEvaluator.SetVariable("time", 10);
        }
        else if (name.Contains("Distance"))
        {
            exprEvaluator.SetVariable("x1", 0);
            exprEvaluator.SetVariable("y1", 0);
            exprEvaluator.SetVariable("x2", 3);
            exprEvaluator.SetVariable("y2", 4);
        }

        try
        {
            var exprResult = expr.Accept(exprEvaluator);
            var exprCode = expr.Accept(codeGen);
            Console.WriteLine($"   Expression: {exprCode}");
            Console.WriteLine($"   Result: {exprResult:F6}");

            // Quick analysis
            var quickAnalyzer = new AstAnalysisVisitor();
            expr.Accept(quickAnalyzer);
            var quickAnalysis = quickAnalyzer.GetResult();
            Console.WriteLine($"   Complexity: {quickAnalysis.TotalNodes} nodes, {quickAnalysis.MaxDepth} depth");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error: {ex.Message}");
        }
    }

    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("✨ Visitor Pattern Benefits:");
    Console.WriteLine("✅ Separates algorithms from object structures using double dispatch");
    Console.WriteLine("✅ Easy to add new operations without modifying existing classes");
    Console.WriteLine("✅ Centralizes related operations in visitor classes");
    Console.WriteLine("✅ Type-safe operations with compile-time checking");
    Console.WriteLine("✅ Supports different result types from same traversal");
    Console.WriteLine("✅ Enables complex transformations and analysis operations");
    Console.WriteLine("✅ Follows Open/Closed Principle for extensibility");
    Console.WriteLine("✅ Provides clean separation between data and operations");
}

static Document CreateSampleDocument()
{
    var document = new Document
    {
        Title = "Visitor Pattern Demonstration",
        Author = "Design Patterns Expert",
        CreatedDate = DateTime.UtcNow
    };

    // Add various document elements
    document.AddElement(new Header
    {
        Level = 1,
        Content = "Introduction to Visitor Pattern",
        Anchor = "introduction"
    });

    document.AddElement(new Paragraph
    {
        Content =
            "The Visitor pattern is a behavioral design pattern that separates algorithms from the objects on which they operate. It uses double dispatch to determine both the visitor type and element type at runtime.",
        FontFamily = "Arial",
        FontSize = 12,
        IsJustified = true
    });

    document.AddElement(new Header
    {
        Level = 2,
        Content = "Key Benefits"
    });

    document.AddElement(new DocumentList
    {
        Type = ListType.Unordered,
        Items =
        [
            "Extensibility without modification",
            "Type-safe operations",
            "Centralized algorithms",
            "Multiple result types"
        ]
    });

    document.AddElement(new Image
    {
        Source = "visitor-pattern-diagram.png",
        AltText = "Visitor pattern class diagram showing double dispatch",
        Width = 600,
        Height = 400
    });

    var table = new Table
    {
        Rows = 3,
        Columns = 3,
        Headers = ["Pattern", "Complexity", "Use Case"]
    };
    table.Data.Add(["Visitor", "Medium", "Multiple operations on object structures"]);
    table.Data.Add(["Strategy", "Low", "Runtime algorithm selection"]);
    table.Data.Add(["Command", "Medium", "Encapsulated requests"]);
    document.AddElement(table);

    return document;
}

#endregion