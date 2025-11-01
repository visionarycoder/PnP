namespace Snippets.DesignPatterns.Structural.Adapter;

// Target interface that clients expect to work with

// Adaptee classes with incompatible interfaces
public class Mp3Player
{
    public void PlayMp3(string fileName)
    {
        Console.WriteLine($"Playing MP3 file: {fileName}");
    }
}

// Advanced media format interfaces

// Adapters for specific players

// Adapter that implements the target interface

// Main audio player that can play different formats

// Object Adapter Pattern Example (Composition)

// Class Adapter Pattern Example (Inheritance)

// Two-way adapter (implements both interfaces)