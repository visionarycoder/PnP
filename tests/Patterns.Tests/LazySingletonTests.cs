namespace Patterns.Tests;

public class LazySingletonTests
{
    // Test class that uses the LazySingleton pattern
    public class TestSingleton : LazySingleton<TestSingleton>
    {
        public int CallCount { get; private set; }

        public TestSingleton()
        {
            CallCount = 0;
        }

        public void IncrementCount()
        {
            CallCount++;
        }
    }

    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        // Arrange & Act
        var instance1 = TestSingleton.Instance;
        var instance2 = TestSingleton.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Instance_ShouldMaintainState()
    {
        // Arrange
        var instance = TestSingleton.Instance;
        
        // Act
        instance.IncrementCount();
        instance.IncrementCount();
        instance.IncrementCount();
        
        var sameInstance = TestSingleton.Instance;

        // Assert
        Assert.Equal(3, sameInstance.CallCount);
    }

    [Fact]
    public async Task Instance_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new TestSingleton[10];
        var tasks = new Task[10];

        // Act
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                instances[index] = TestSingleton.Instance;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        var firstInstance = instances[0];
        for (int i = 1; i < instances.Length; i++)
        {
            Assert.Same(firstInstance, instances[i]);
        }
    }
}
