using System.Text;
using System.Text.Json;

namespace Python.FileOperations;

/// <summary>
/// Common file handling patterns for reading, writing, and processing files safely.
/// Demonstrates C# equivalents of Python file operation patterns following best practices.
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Python File Operations Examples ===\n");

        var tempDir = Path.Combine(Path.GetTempPath(), "FileOperationsDemo");
        Directory.CreateDirectory(tempDir);

        try
        {
            ReadFileSafelyExample(tempDir);
            WriteFileSafelyExample(tempDir);
            ReadLinesExample(tempDir);
            ReadLinesGeneratorExample(tempDir);
            AppendToFileExample(tempDir);
            PathOperationsExample(tempDir);
            FileMetadataExample(tempDir);
            BatchProcessingExample(tempDir);
            JsonFileOperationsExample(tempDir);
            CsvFileOperationsExample(tempDir);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Safely read entire file contents with proper error handling
    /// Python equivalent: with open(filepath, 'r', encoding='utf-8') as f: return f.read()
    /// </summary>
    private static void ReadFileSafelyExample(string tempDir)
    {
        Console.WriteLine("1. Read File Safely:");

        var testFile = Path.Combine(tempDir, "test.txt");
        var content = "Hello, World!\nThis is a test file.\nLine 3 with UTF-8: 🌟";
        File.WriteAllText(testFile, content, Encoding.UTF8);

        static string ReadFileSafely(string filepath, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            try
            {
                return File.ReadAllText(filepath, encoding);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"   File not found: {filepath}");
                return "";
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"   Permission denied: {filepath}");
                return "";
            }
            catch (Exception e)
            {
                Console.WriteLine($"   Unexpected error reading file: {e.Message}");
                return "";
            }
        }

        var fileContent = ReadFileSafely(testFile);
        var missingContent = ReadFileSafely("nonexistent.txt");

        Console.WriteLine($"   File content length: {fileContent.Length} chars");
        Console.WriteLine($"   First line: '{fileContent.Split('\n')[0]}'");
        Console.WriteLine($"   Missing file content: '{missingContent}'\n");
    }

    /// <summary>
    /// Safely write content to a file with directory creation
    /// Python equivalent: with open(filepath, 'w', encoding='utf-8') as f: f.write(content)
    /// </summary>
    private static void WriteFileSafelyExample(string tempDir)
    {
        Console.WriteLine("2. Write File Safely:");

        static bool WriteFileSafely(string filepath, string content, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            try
            {
                // Create directory if it doesn't exist (like Python's parents=True)
                var directory = Path.GetDirectoryName(filepath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filepath, content, encoding);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"   Error writing file: {e.Message}");
                return false;
            }
        }

        var nestedFile = Path.Combine(tempDir, "nested", "folder", "output.txt");
        var success = WriteFileSafely(nestedFile, "Content written to nested file!");

        Console.WriteLine($"   Write success: {success}");
        Console.WriteLine($"   File exists: {File.Exists(nestedFile)}");
        if (File.Exists(nestedFile))
        {
            Console.WriteLine($"   File content: '{File.ReadAllText(nestedFile)}'");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Read file lines into a list
    /// Python equivalent: with open(filepath, 'r') as f: return [line.rstrip('\n') for line in f]
    /// </summary>
    private static void ReadLinesExample(string tempDir)
    {
        Console.WriteLine("3. Read Lines:");

        var linesFile = Path.Combine(tempDir, "lines.txt");
        var lines = new[] { "Line 1", "Line 2", "Line 3 with spaces   ", "Line 4" };
        File.WriteAllLines(linesFile, lines, Encoding.UTF8);

        static List<string> ReadLines(string filepath, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            try
            {
                return File.ReadAllLines(filepath, encoding)
                    .Select(line => line.TrimEnd('\r', '\n'))
                    .ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine($"   Error reading file: {e.Message}");
                return new List<string>();
            }
        }

        var readLines = ReadLines(linesFile);
        
        Console.WriteLine($"   Lines count: {readLines.Count}");
        for (var i = 0; i < readLines.Count; i++)
        {
            Console.WriteLine($"   Line {i + 1}: '{readLines[i]}'");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Read file lines lazily using yield return (memory efficient for large files)
    /// Python equivalent: generator function with yield
    /// </summary>
    private static void ReadLinesGeneratorExample(string tempDir)
    {
        Console.WriteLine("4. Read Lines Generator (Memory Efficient):");

        var largeFile = Path.Combine(tempDir, "large.txt");
        var largeContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i:D4}"));
        File.WriteAllText(largeFile, largeContent);

        static IEnumerable<string> ReadLinesGenerator(string filepath, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            using var reader = new StreamReader(filepath, encoding);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        var lineCount = 0;
        var firstFiveLines = new List<string>();

        foreach (var line in ReadLinesGenerator(largeFile))
        {
            lineCount++;
            if (firstFiveLines.Count < 5)
            {
                firstFiveLines.Add(line);
            }
        }

        Console.WriteLine($"   Total lines processed: {lineCount:N0}");
        Console.WriteLine($"   First 5 lines: [{string.Join(", ", firstFiveLines)}]");
        Console.WriteLine($"   Memory efficient: Only one line in memory at a time\n");
    }

    /// <summary>
    /// Append content to an existing file
    /// Python equivalent: with open(filepath, 'a', encoding='utf-8') as f: f.write(content)
    /// </summary>
    private static void AppendToFileExample(string tempDir)
    {
        Console.WriteLine("5. Append to File:");

        var appendFile = Path.Combine(tempDir, "append.txt");
        
        static bool AppendToFile(string filepath, string content, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            try
            {
                File.AppendAllText(filepath, content, encoding);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"   Error appending to file: {e.Message}");
                return false;
            }
        }

        AppendToFile(appendFile, "Initial content\n");
        AppendToFile(appendFile, "Appended line 1\n");
        AppendToFile(appendFile, "Appended line 2\n");

        var finalContent = File.ReadAllText(appendFile);
        var lineCount = finalContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

        Console.WriteLine($"   Final line count: {lineCount}");
        Console.WriteLine($"   File content:\n{finalContent.Trim().Replace("\n", "\n     ")}\n");
    }

    /// <summary>
    /// Path operations using modern C# Path and Directory methods
    /// Python equivalent: os.path operations and pathlib.Path
    /// </summary>
    private static void PathOperationsExample(string tempDir)
    {
        Console.WriteLine("6. Path Operations:");

        var testPath = Path.Combine(tempDir, "subdir", "file.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(testPath)!);
        File.WriteAllText(testPath, "test");

        Console.WriteLine($"   Original path: {testPath}");
        Console.WriteLine($"   Directory name: {Path.GetDirectoryName(testPath)}");
        Console.WriteLine($"   File name: {Path.GetFileName(testPath)}");
        Console.WriteLine($"   File name without extension: {Path.GetFileNameWithoutExtension(testPath)}");
        Console.WriteLine($"   Extension: {Path.GetExtension(testPath)}");
        Console.WriteLine($"   Absolute path: {Path.GetFullPath(testPath)}");
        Console.WriteLine($"   Exists: {File.Exists(testPath)}");
        Console.WriteLine($"   Parent directory exists: {Directory.Exists(Path.GetDirectoryName(testPath))}\n");
    }

    /// <summary>
    /// File metadata operations
    /// Python equivalent: os.stat() and pathlib.Path.stat()
    /// </summary>
    private static void FileMetadataExample(string tempDir)
    {
        Console.WriteLine("7. File Metadata:");

        var metadataFile = Path.Combine(tempDir, "metadata.txt");
        File.WriteAllText(metadataFile, "Content for metadata testing");

        var fileInfo = new FileInfo(metadataFile);
        
        Console.WriteLine($"   File size: {fileInfo.Length} bytes");
        Console.WriteLine($"   Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   Accessed: {fileInfo.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   Read-only: {fileInfo.IsReadOnly}");
        Console.WriteLine($"   Directory: {fileInfo.Directory?.FullName}\n");
    }

    /// <summary>
    /// Batch file processing
    /// Python equivalent: processing multiple files in a directory
    /// </summary>
    private static void BatchProcessingExample(string tempDir)
    {
        Console.WriteLine("8. Batch File Processing:");

        var batchDir = Path.Combine(tempDir, "batch");
        Directory.CreateDirectory(batchDir);

        // Create test files
        for (var i = 1; i <= 5; i++)
        {
            File.WriteAllText(Path.Combine(batchDir, $"file{i}.txt"), $"Content of file {i}");
        }

        var txtFiles = Directory.GetFiles(batchDir, "*.txt");
        var totalSize = 0L;
        var processedCount = 0;

        foreach (var file in txtFiles)
        {
            var fileInfo = new FileInfo(file);
            totalSize += fileInfo.Length;
            processedCount++;
            Console.WriteLine($"   Processed: {Path.GetFileName(file)} ({fileInfo.Length} bytes)");
        }

        Console.WriteLine($"   Total files: {processedCount}");
        Console.WriteLine($"   Total size: {totalSize} bytes\n");
    }

    /// <summary>
    /// JSON file operations
    /// Python equivalent: json.load() and json.dump()
    /// </summary>
    private static void JsonFileOperationsExample(string tempDir)
    {
        Console.WriteLine("9. JSON File Operations:");

        var jsonFile = Path.Combine(tempDir, "data.json");
        
        var data = new
        {
            Name = "John Doe",
            Age = 30,
            Skills = new[] { "C#", "Python", "JavaScript" },
            Metadata = new { Created = DateTime.Now, Version = "1.0" }
        };

        // Write JSON
        var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonFile, jsonString);

        // Read JSON
        var readJsonString = File.ReadAllText(jsonFile);
        using var document = JsonDocument.Parse(readJsonString);
        var root = document.RootElement;

        Console.WriteLine($"   JSON file size: {new FileInfo(jsonFile).Length} bytes");
        Console.WriteLine($"   Name: {root.GetProperty("Name").GetString()}");
        Console.WriteLine($"   Age: {root.GetProperty("Age").GetInt32()}");
        Console.WriteLine($"   Skills count: {root.GetProperty("Skills").GetArrayLength()}");
        Console.WriteLine($"   First skill: {root.GetProperty("Skills")[0].GetString()}\n");
    }

    /// <summary>
    /// CSV file operations
    /// Python equivalent: csv module operations
    /// </summary>
    private static void CsvFileOperationsExample(string tempDir)
    {
        Console.WriteLine("10. CSV File Operations:");

        var csvFile = Path.Combine(tempDir, "data.csv");
        
        // Write CSV
        var csvLines = new[]
        {
            "Name,Age,City",
            "Alice,25,New York",
            "Bob,30,London",
            "Charlie,35,Tokyo"
        };
        File.WriteAllLines(csvFile, csvLines);

        // Read CSV
        var lines = File.ReadAllLines(csvFile);
        var headers = lines[0].Split(',');
        var dataRows = lines.Skip(1).Select(line => line.Split(',')).ToArray();

        Console.WriteLine($"   CSV headers: [{string.Join(", ", headers)}]");
        Console.WriteLine($"   Data rows: {dataRows.Length}");
        
        foreach (var row in dataRows)
        {
            var record = string.Join(" | ", headers.Zip(row, (h, v) => $"{h}: {v}"));
            Console.WriteLine($"   {record}");
        }
    }
}
