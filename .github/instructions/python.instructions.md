---
description: Python coding standards and best practices following PEP 8
applyTo: '**/*.{py,pyw,pyi}'
---

# Python Instructions

## Scope
Applies to `.py`, `.pyw`, `.pyi` files and Python development.

## Language Conventions
- Follow PEP 8 style guide strictly.
- Use `snake_case` for variables, functions, and module names.
- Use `PascalCase` for class names.
- Use `SCREAMING_SNAKE_CASE` for constants.
- Use descriptive names that explain purpose.
- Limit line length to 88 characters (Black formatter standard).

## Code Organization
- Use meaningful docstrings for modules, classes, and functions.
- Follow Google or NumPy docstring conventions.
- Group imports: standard library, third-party, local imports.
- Use `if __name__ == "__main__":` for script execution.
- Organize code into logical functions and classes.

## Type Hints
- Use type hints for function parameters and return values.
- Import types from `typing` module when needed.
- Use `Optional[T]` for nullable parameters.
- Use `Union[T, U]` for multiple possible types.
- Use `List[T]`, `Dict[K, V]` for container types.
- Use `Protocol` for structural typing when appropriate.

## Error Handling
- Use specific exception types instead of bare `except:`.
- Create custom exception classes for domain-specific errors.
- Use `try`/`except`/`finally` blocks appropriately.
- Include meaningful error messages and context.
- Log errors with proper severity levels.

## Data Structures
- Use list comprehensions and generator expressions.
- Prefer `collections` module for specialized data structures.
- Use `dataclasses` or `NamedTuple` for structured data.
- Use `Enum` for constants with meaningful names.
- Leverage `itertools` for efficient iteration patterns.

## Function Design
- Keep functions small and focused on single responsibility.
- Use default parameters wisely.
- Use `*args` and `**kwargs` for flexible APIs.
- Return meaningful values or raise exceptions.
- Use decorators for cross-cutting concerns.

## Object-Oriented Programming
- Use properties (`@property`) for computed attributes.
- Implement `__str__` and `__repr__` for custom classes.
- Use `super()` for method resolution in inheritance.
- Follow composition over inheritance principle.
- Use abstract base classes for contracts.

## Performance Best Practices
- Use built-in functions and libraries when possible.
- Profile code before optimizing.
- Use `collections.defaultdict` and `collections.Counter`.
- Leverage NumPy for numerical computations.
- Use `functools.lru_cache` for memoization.

## Testing
- Write unit tests using `pytest` or `unittest`.
- Use descriptive test function names.
- Follow Arrange-Act-Assert pattern.
- Use fixtures for test setup and teardown.
- Aim for high test coverage of critical functionality.

## Dependencies
- Use `requirements.txt` or `pyproject.toml` for dependencies.
- Pin dependency versions for reproducible builds.
- Use virtual environments for project isolation.
- Keep dependencies minimal and well-justified.
- Regular security updates for dependencies.

## Documentation
- Write clear and comprehensive docstrings.
- Use Sphinx for generating documentation.
- Include code examples in docstrings.
- Maintain up-to-date README files.
- Document configuration and environment requirements.

## Security Considerations
- Validate and sanitize all input data.
- Use parameterized queries for database operations.
- Store sensitive configuration in environment variables.
- Use secure random number generation.
- Handle file operations safely with proper permissions.

## Async Programming
- Use `asyncio` for asynchronous programming.
- Prefer `async`/`await` over callbacks.
- Use `aiohttp` for async HTTP operations.
- Handle async context managers properly.
- Avoid blocking operations in async functions.