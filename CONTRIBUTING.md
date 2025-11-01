# Contributing to Internal.Snippet

Thank you for contributing to this code snippet repository! This guide will help you maintain consistency and quality.

## Adding New Snippets

### 1. Choose the Right Category

Place your snippet in the most appropriate category folder:
- `csharp/` - C# and .NET code
- `python/` - Python scripts and code
- `javascript/` - JavaScript/TypeScript code
- `sql/` - SQL queries and database code
- `bash/` - Bash scripts and shell commands
- `powershell/` - PowerShell scripts
- `algorithms/` - Algorithm implementations
- `design-patterns/` - Design pattern examples
- `web/` - HTML, CSS, web development
- `docker/` - Docker and containerization
- `git/` - Git commands and workflows
- `utilities/` - Cross-platform utilities

If your snippet doesn't fit any existing category, consider creating a new one.

### 2. Use the Template

Copy the `SNIPPET_TEMPLATE.md` file and fill it out completely:

```bash
cp SNIPPET_TEMPLATE.md snippets/[category]/[snippet-name].md
```

### 3. Follow Naming Conventions

**File Names:**
- Use lowercase letters
- Use hyphens to separate words
- Be descriptive but concise
- Examples: `string-truncate.md`, `retry-pattern.md`, `docker-compose-example.md`

**Snippet Titles:**
- Use title case
- Be clear and descriptive
- Examples: "String Truncate", "Retry Pattern with Exponential Backoff"

### 4. Write Quality Code

**Code Quality:**
- Write clean, readable code
- Follow language-specific conventions and style guides
- Include helpful comments for complex logic
- Handle edge cases and errors appropriately
- Use meaningful variable and function names

**Examples:**
- Provide practical, real-world usage examples
- Show different use cases when applicable
- Include expected output or results
- Demonstrate error handling

### 5. Update the Category README

After adding a snippet, update the category's `README.md`:

```markdown
## Index

- [Your New Snippet](your-new-snippet.md) - Brief description
- [Existing Snippet](existing-snippet.md) - Description
```

### 6. Add Appropriate Metadata

Include all relevant information:
- **Description**: Clear explanation of purpose
- **Prerequisites**: Dependencies, versions, platform requirements
- **Notes**: Important considerations, limitations, alternatives
- **Tags**: Relevant keywords for searchability
- **Related Snippets**: Links to similar or complementary snippets

## Snippet Quality Checklist

Before submitting a snippet, ensure:

- [ ] Code is tested and working
- [ ] Template is completely filled out
- [ ] Code follows language best practices
- [ ] Usage examples are clear and practical
- [ ] Edge cases are handled or documented
- [ ] Prerequisites are listed (if any)
- [ ] Notes section includes important information
- [ ] Related snippets are linked (if applicable)
- [ ] Category README is updated
- [ ] File name follows naming conventions
- [ ] Code syntax highlighting is correct
- [ ] No sensitive information (passwords, keys, etc.)

## Code Standards

### General Guidelines

1. **Clarity over Cleverness**: Write code that's easy to understand
2. **Completeness**: Include all necessary imports, dependencies
3. **Error Handling**: Show proper error handling patterns
4. **Comments**: Explain "why" not "what" (code should be self-explanatory)
5. **Security**: Never include credentials, API keys, or sensitive data

### Language-Specific Standards

**C#:**
- Follow Microsoft C# coding conventions
- Use meaningful names for variables, methods, classes
- Include XML documentation comments for public APIs
- Use async/await for asynchronous operations

**Python:**
- Follow PEP 8 style guide
- Use type hints where appropriate (Python 3.5+)
- Include docstrings for functions and classes
- Use list comprehensions appropriately

**JavaScript:**
- Use modern ES6+ syntax
- Prefer `const` and `let` over `var`
- Use arrow functions appropriately
- Include JSDoc comments for complex functions

**SQL:**
- Use UPPERCASE for SQL keywords
- Use clear table and column aliases
- Format queries for readability
- Include comments for complex queries

## Organizing Your Snippets

### Categories

Keep snippets organized by:
1. **Primary language/technology** (e.g., all Python in `python/`)
2. **Specific subdomain** if needed (e.g., `python/data-science/`)
3. **Related functionality** (e.g., all string operations together)

### Cross-References

When snippets are related:
- Link to related snippets in the "Related Snippets" section
- Mention alternative approaches
- Create "See also" references where helpful

## Documentation Standards

### Description
- Start with what the snippet does
- Explain when to use it
- Mention key benefits

### Code Section
- Include all necessary imports/using statements
- Add inline comments for complex logic
- Keep examples focused and concise

### Usage Section
- Show realistic examples
- Include expected output
- Demonstrate common use cases
- Show error handling when relevant

### Notes Section
- Performance implications
- Security considerations
- Platform/version requirements
- Known limitations
- Alternative approaches
- Common pitfalls

## Maintenance

### Updating Existing Snippets

When updating a snippet:
1. Update the "Last Updated" date
2. Document what changed in git commit message
3. Ensure all sections are still accurate
4. Test the updated code

### Deprecating Snippets

If a snippet becomes outdated:
1. Add a "DEPRECATED" warning at the top
2. Explain why it's deprecated
3. Link to the replacement snippet
4. Consider moving to an `archived/` folder

## Examples of Good Snippets

Good examples in this repository:
- `csharp/string-truncate.md` - Clear, well-documented, includes edge cases
- `csharp/retry-pattern.md` - Shows advanced pattern with good explanations
- `python/file-operations.md` - Comprehensive coverage with multiple methods
- `javascript/array-methods.md` - Multiple related examples organized well
- `git/common-commands.md` - Well-organized reference material

## Questions?

If you're unsure about:
- Where to place a snippet
- How to format something
- Whether a snippet belongs here

Look at existing snippets in similar categories for guidance.

## License

By contributing to this repository, you agree that your contributions will be subject to the same license as the repository.

---

*Happy coding and snippet sharing!*
