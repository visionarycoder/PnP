namespace Patterns.Tests;

public class ConstantsDictionaryBuilderTests
{
    // Test class with const fields
    public class ConstantsClass
    {
        public const string StringConstant = "Hello";
        public const int IntConstant = 42;
        public const double DoubleConstant = 3.14;
        public static readonly string ReadonlyStringField = "World";
        public static readonly int ReadonlyIntField = 100;
    }

    // Test class with static properties
    public class PropertiesClass
    {
        public static string StringProperty => "PropertyValue";
        public static int IntProperty => 123;
        public static double DoubleProperty { get; set; } = 2.71;
    }

    // Test class with mixed members
    public class MixedClass
    {
        public const string Constant = "ConstValue";
        public static readonly string ReadonlyField = "ReadonlyValue";
        public static string Property => "PropertyValue";
    }

    [Fact]
    public void BuildFromConstants_ShouldIncludeConstFields()
    {
        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromConstants<ConstantsClass>();

        // Assert
        Assert.Equal(5, dictionary.Count);
        Assert.Equal("Hello", dictionary["StringConstant"]);
        Assert.Equal(42, dictionary["IntConstant"]);
        Assert.Equal(3.14, dictionary["DoubleConstant"]);
    }

    [Fact]
    public void BuildFromConstants_ShouldIncludeReadonlyFields()
    {
        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromConstants<ConstantsClass>();

        // Assert
        Assert.Equal("World", dictionary["ReadonlyStringField"]);
        Assert.Equal(100, dictionary["ReadonlyIntField"]);
    }

    [Fact]
    public void BuildFromProperties_ShouldIncludeStaticProperties()
    {
        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromProperties<PropertiesClass>();

        // Assert
        Assert.Equal(3, dictionary.Count);
        Assert.Equal("PropertyValue", dictionary["StringProperty"]);
        Assert.Equal(123, dictionary["IntProperty"]);
        Assert.Equal(2.71, dictionary["DoubleProperty"]);
    }

    [Fact]
    public void BuildFromAll_ShouldIncludeFieldsAndProperties()
    {
        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromAll<MixedClass>();

        // Assert
        Assert.Equal(3, dictionary.Count);
        Assert.Equal("ConstValue", dictionary["Constant"]);
        Assert.Equal("ReadonlyValue", dictionary["ReadonlyField"]);
        Assert.Equal("PropertyValue", dictionary["Property"]);
    }

    [Fact]
    public void BuildFromConstants_WithType_ShouldWork()
    {
        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromConstants(typeof(ConstantsClass));

        // Assert
        Assert.Equal(5, dictionary.Count);
        Assert.Equal("Hello", dictionary["StringConstant"]);
    }

    [Fact]
    public void BuildFromProperties_WithType_ShouldWork()
    {
        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromProperties(typeof(PropertiesClass));

        // Assert
        Assert.Equal(3, dictionary.Count);
        Assert.Equal("PropertyValue", dictionary["StringProperty"]);
    }

    [Fact]
    public void BuildFromAll_WithType_ShouldWork()
    {
        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromAll(typeof(MixedClass));

        // Assert
        Assert.Equal(3, dictionary.Count);
        Assert.Equal("ConstValue", dictionary["Constant"]);
    }

    [Fact]
    public void BuildFromConstants_EmptyClass_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var emptyClass = new { }.GetType();

        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromConstants(emptyClass);

        // Assert
        Assert.Empty(dictionary);
    }

    [Fact]
    public void BuildFromProperties_EmptyClass_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var emptyClass = new { }.GetType();

        // Act
        var dictionary = ConstantsDictionaryBuilder.BuildFromProperties(emptyClass);

        // Assert
        Assert.Empty(dictionary);
    }
}
