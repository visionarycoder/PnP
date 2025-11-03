---
description: JavaScript/TypeScript coding standards and best practices
applyTo: '**/*.{js,ts,jsx,tsx,mjs,cjs}'
---

# JavaScript / TypeScript Instructions

## Scope
Applies to `.js`, `.ts`, `.jsx`, `.tsx`, `.mjs`, `.cjs` files and JavaScript/TypeScript development.

## Language Conventions
- Use `camelCase` for variables, functions, and methods.
- Use `PascalCase` for classes, interfaces, and types.
- Use `SCREAMING_SNAKE_CASE` for constants.
- Prefer `const` and `let` over `var`.
- Use meaningful and descriptive names.
- Use TypeScript for type safety when possible.

## Modern JavaScript Features
- Use arrow functions for short, simple functions.
- Prefer template literals over string concatenation.
- Use destructuring assignment for objects and arrays.
- Use spread operator for array/object operations.
- Prefer `async`/`await` over `.then()` for promises.
- Use optional chaining (`?.`) and nullish coalescing (`??`).

## Code Organization
- Use ES6+ module syntax (`import`/`export`).
- Group imports: external libraries first, then internal modules.
- Export functions and classes at the bottom of files.
- Use default exports for single-purpose modules.
- Organize code into logical functions and classes.

## Error Handling
- Use `try`/`catch` blocks for error-prone operations.
- Create custom error classes for specific error types.
- Validate input parameters and provide meaningful error messages.
- Use proper error logging and monitoring.
- Handle async errors with proper `catch` blocks.

## TypeScript Specific
- Enable strict mode in `tsconfig.json`.
- Use interfaces for object shapes and contracts.
- Use type unions and intersections appropriately.
- Prefer `unknown` over `any` for type safety.
- Use generics for reusable type-safe functions.
- Document complex types with JSDoc comments.

## Performance Best Practices
- Avoid deep nesting and callback hell.
- Use efficient array methods (`map`, `filter`, `reduce`).
- Implement proper memoization for expensive calculations.
- Use `Object.freeze()` for immutable objects.
- Avoid memory leaks with proper event listener cleanup.

## Testing
- Write unit tests for all functions and methods.
- Use descriptive test names that explain the scenario.
- Follow Arrange-Act-Assert pattern in tests.
- Mock external dependencies in unit tests.
- Achieve high test coverage for critical business logic.

## Documentation
- Use JSDoc for function and class documentation.
- Include parameter types, return types, and examples.
- Document complex algorithms and business logic.
- Provide usage examples for public APIs.
- Keep README files updated with setup and usage instructions.

## Security Considerations
- Validate and sanitize all user input.
- Use proper authentication and authorization.
- Avoid storing sensitive data in client-side code.
- Use HTTPS for all network communications.
- Implement proper CORS policies.

## Node.js Specific
- Use `process.env` for environment configuration.
- Handle process signals for graceful shutdowns.
- Use proper logging libraries instead of `console.log`.
- Implement proper error handling middleware.
- Use `package-lock.json` for dependency management.