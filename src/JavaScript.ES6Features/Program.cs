namespace JavaScript.ES6Features;

/// <summary>
/// Demonstrates ES6+ features and modern JavaScript patterns
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("JavaScript ES6+ Features Examples");
        Console.WriteLine("==================================");
        
        ArrowFunctionExamples();
        DestructuringExamples();
        ModulePatternExamples();
        ClassExamples();
        AsyncAwaitExamples();
        TemplateLiteralExamples();
    }

    /// <summary>
    /// Arrow function patterns and use cases
    /// </summary>
    private static void ArrowFunctionExamples()
    {
        Console.WriteLine("\n1. Arrow Functions:");
        
        // Traditional function vs arrow function
        Console.WriteLine("// Traditional function");
        Console.WriteLine("function add(a, b) { return a + b; }");
        
        Console.WriteLine("\n// Arrow function");
        Console.WriteLine("const add = (a, b) => a + b;");
        
        Console.WriteLine("\n// Array methods with arrow functions");
        Console.WriteLine("const numbers = [1, 2, 3, 4, 5];");
        Console.WriteLine("const doubled = numbers.map(n => n * 2);");
        Console.WriteLine("const evens = numbers.filter(n => n % 2 === 0);");
        Console.WriteLine("const sum = numbers.reduce((acc, n) => acc + n, 0);");
    }

    /// <summary>
    /// Destructuring assignment patterns
    /// </summary>
    private static void DestructuringExamples()
    {
        Console.WriteLine("\n2. Destructuring Assignment:");
        
        Console.WriteLine("// Array destructuring");
        Console.WriteLine("const [first, second, ...rest] = [1, 2, 3, 4, 5];");
        
        Console.WriteLine("\n// Object destructuring");
        Console.WriteLine("const { name, age, city = 'Unknown' } = user;");
        
        Console.WriteLine("\n// Nested destructuring");
        Console.WriteLine("const { address: { street, zipCode } } = user;");
        
        Console.WriteLine("\n// Function parameter destructuring");
        Console.WriteLine("function greet({ name, age }) {");
        Console.WriteLine("  return `Hello ${name}, you are ${age} years old`;");
        Console.WriteLine("}");
    }

    /// <summary>
    /// ES6 module patterns
    /// </summary>
    private static void ModulePatternExamples()
    {
        Console.WriteLine("\n3. Module Patterns:");
        
        Console.WriteLine("// Named exports");
        Console.WriteLine("export const PI = 3.14159;");
        Console.WriteLine("export function calculateArea(radius) {");
        Console.WriteLine("  return PI * radius * radius;");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Default export");
        Console.WriteLine("export default class Calculator {");
        Console.WriteLine("  add(a, b) { return a + b; }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Import patterns");
        Console.WriteLine("import Calculator, { PI, calculateArea } from './math.js';");
        Console.WriteLine("import * as MathUtils from './math.js';");
    }

    /// <summary>
    /// ES6 class patterns
    /// </summary>
    private static void ClassExamples()
    {
        Console.WriteLine("\n4. ES6 Classes:");
        
        Console.WriteLine("class Animal {");
        Console.WriteLine("  constructor(name) {");
        Console.WriteLine("    this.name = name;");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  speak() {");
        Console.WriteLine("    console.log(`${this.name} makes a sound`);");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Class inheritance");
        Console.WriteLine("class Dog extends Animal {");
        Console.WriteLine("  constructor(name, breed) {");
        Console.WriteLine("    super(name);");
        Console.WriteLine("    this.breed = breed;");
        Console.WriteLine("  }");
        Console.WriteLine("  ");
        Console.WriteLine("  speak() {");
        Console.WriteLine("    console.log(`${this.name} barks`);");
        Console.WriteLine("  }");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Async/await patterns
    /// </summary>
    private static void AsyncAwaitExamples()
    {
        Console.WriteLine("\n5. Async/Await Patterns:");
        
        Console.WriteLine("// Promise-based function");
        Console.WriteLine("async function fetchData(url) {");
        Console.WriteLine("  try {");
        Console.WriteLine("    const response = await fetch(url);");
        Console.WriteLine("    const data = await response.json();");
        Console.WriteLine("    return data;");
        Console.WriteLine("  } catch (error) {");
        Console.WriteLine("    console.error('Error fetching data:', error);");
        Console.WriteLine("    throw error;");
        Console.WriteLine("  }");
        Console.WriteLine("}");
        
        Console.WriteLine("\n// Concurrent async operations");
        Console.WriteLine("async function fetchMultipleUrls(urls) {");
        Console.WriteLine("  const promises = urls.map(url => fetchData(url));");
        Console.WriteLine("  return await Promise.all(promises);");
        Console.WriteLine("}");
    }

    /// <summary>
    /// Template literal patterns
    /// </summary>
    private static void TemplateLiteralExamples()
    {
        Console.WriteLine("\n6. Template Literals:");
        
        Console.WriteLine("// Basic template literal");
        Console.WriteLine("const greeting = `Hello, ${name}!`;");
        
        Console.WriteLine("\n// Multi-line strings");
        Console.WriteLine("const html = `");
        Console.WriteLine("  <div class=\"card\">");
        Console.WriteLine("    <h2>${title}</h2>");
        Console.WriteLine("    <p>${description}</p>");
        Console.WriteLine("  </div>");
        Console.WriteLine("`;");
        
        Console.WriteLine("\n// Tagged template literals");
        Console.WriteLine("function highlight(strings, ...values) {");
        Console.WriteLine("  return strings.reduce((result, string, i) => {");
        Console.WriteLine("    const value = values[i] ? `<mark>${values[i]}</mark>` : '';");
        Console.WriteLine("    return result + string + value;");
        Console.WriteLine("  }, '');");
        Console.WriteLine("}");
        Console.WriteLine("const message = highlight`Search for ${term} found ${count} results`;");
    }
}