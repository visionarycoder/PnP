# Internal.Snippet

A repository of code snippets and ideas that I can use as a quick way to look up solutions I have developed to various projects.

## 📚 Table of Contents

- [Overview](#overview)
- [Structure](#structure)
- [Categories](#categories)
- [Usage](#usage)
- [Contributing](#contributing)

## Overview

This repository serves as a personal knowledge base of code snippets, solutions, and best practices collected from various projects. It's organized by language, technology, and problem domain for quick reference and reuse.

## Structure

```
snippets/
├── csharp/          # C# code snippets
├── python/          # Python code snippets
├── javascript/      # JavaScript/TypeScript snippets
├── sql/             # SQL queries and database solutions
├── bash/            # Bash scripts and commands
├── powershell/      # PowerShell scripts
├── algorithms/      # Algorithm implementations
├── design-patterns/ # Design pattern examples
├── web/             # Web development snippets (HTML, CSS, etc.)
├── docker/          # Docker configurations and snippets
├── git/             # Git commands and workflows
└── utilities/       # General utility scripts and tools
```

## Categories

### 🔷 C# (`snippets/csharp/`)
C# language snippets including LINQ queries, async/await patterns, extension methods, and .NET-specific solutions.

### 🐍 Python (`snippets/python/`)
Python scripts and code snippets for data processing, automation, APIs, and common patterns.

### 🟨 JavaScript (`snippets/javascript/`)
JavaScript and TypeScript snippets for web development, Node.js, and modern ES6+ patterns.

### 🗃️ SQL (`snippets/sql/`)
SQL queries, stored procedures, optimization techniques, and database-specific solutions.

### 🐚 Bash (`snippets/bash/`)
Bash scripts, one-liners, and shell commands for automation and system administration.

### 💻 PowerShell (`snippets/powershell/`)
PowerShell scripts and cmdlets for Windows automation and administration.

### 🧮 Algorithms (`snippets/algorithms/`)
Common algorithm implementations including sorting, searching, and data structure operations.

### 🏗️ Design Patterns (`snippets/design-patterns/`)
Examples of software design patterns with practical implementations.

### 🌐 Web (`snippets/web/`)
HTML, CSS, and web development snippets including responsive design and accessibility patterns.

### 🐳 Docker (`snippets/docker/`)
Dockerfile examples, docker-compose configurations, and container management snippets.

### 🔀 Git (`snippets/git/`)
Git commands, workflows, and useful aliases for version control.

### 🛠️ Utilities (`snippets/utilities/`)
Cross-platform utility scripts and general-purpose tools.

## Usage

### Finding Snippets

1. Browse the category folders in the `snippets/` directory
2. Check the README.md in each category for an index of available snippets
3. Use GitHub's search functionality to find specific keywords

### Using a Snippet

Each snippet includes:
- **Description**: What the snippet does
- **Code**: The actual implementation
- **Usage Example**: How to use the snippet
- **Notes**: Any important considerations or dependencies

### Example

```markdown
# String Helper - Truncate with Ellipsis

**Description**: Truncates a string to a specified length and adds ellipsis if needed.

**Code**:
```csharp
public static string Truncate(string value, int maxLength)
{
    if (string.IsNullOrEmpty(value)) return value;
    return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
}
```

**Usage**:
```csharp
string result = StringHelper.Truncate("This is a long text", 10);
// Output: "This is a..."
```

**Notes**: Works with .NET Framework 4.5+ and .NET Core
```

## Contributing

This is a personal repository, but feel free to fork and adapt the structure for your own use.

### Adding New Snippets

1. Choose the appropriate category folder
2. Create a new `.md` file with a descriptive name
3. Follow the snippet template format
4. Update the category's README.md index
5. Commit with a clear message

### Snippet Template

Use this template when adding new snippets:

```markdown
# [Snippet Title]

**Description**: [Brief description of what this snippet does]

**Language/Technology**: [e.g., C#, Python, SQL]

**Code**:
```[language]
[Your code here]
```

**Usage**:
```[language]
[Usage example]
```

**Notes**: 
- [Any dependencies]
- [Platform requirements]
- [Performance considerations]
- [Related snippets or alternatives]
```

## License

This is a personal repository for internal use.

---

*Last Updated: 2025-10-31*
