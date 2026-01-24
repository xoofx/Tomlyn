# Tomlyn Code Contribution Instructions

## Overview

- In the `readme.md` file, you will find general information about the Tomlyn project.
- In the `doc/readme.md` file you will find the user guide documentation for the Tomlyn library.

## Project Structure

- In the `src/Tomlyn` folder you will find the code for the main Tomlyn library.
- In the `src/Tomlyn.Tests` folder you will find the unit tests for the library.
- In the `src/Tomlyn.SourceGeneration` folder you will find the AOT-friendly source generator for TOML model mapping.
- In the `src/Tomlyn.Signed` folder you will find the strong-named signed version of the library.
- In the `src/Tomlyn.AotTests` folder you will find AOT compatibility tests.
- In the `src/Tomlyn.Benchmarks` folder you will find performance benchmarks.
- In the `ext/toml-test` folder you will find the external TOML test suite used for standard compliance testing.

## Building and Testing

- To build the project, navigate to the `src` directory and run `dotnet build -c Release`.
- To run the unit tests, navigate to the `src` directory and run `dotnet test -c Release`.
- Ensure that all tests pass successfully before submitting any changes.
- Ensure that user guide documentation (`doc/readme.md`) and top-level readme are updated to reflect any changes made to the library.

## General Coding Instructions

- Follow the coding style and conventions used in the existing code base.
- Write clear and concise inline comments to explain the purpose and functionality of your code. This is about the "why" more than the "what".
- All public APIs must have XML documentation comments to avoid CS1591 warnings.
- Ensure that your code is well-structured and modular to facilitate maintenance and future enhancements.
- Adhere to best practices for error handling and input validation.
- Write unit tests for any new functionality you add to ensure code quality and reliability.
  - When fixing a bug, add a unit test that reproduces the bug before implementing the fix.
- Use meaningful variable and method names that accurately reflect their purpose.
- Avoid code duplication by reusing existing methods and classes whenever possible.

## C# Coding Conventions

### Naming Conventions

- Use `PascalCase` for public members, types, and namespaces.
- Use `camelCase` for local variables and parameters.
- Use `_camelCase` (with underscore prefix) for private fields.
- Prefix interfaces with `I` (e.g., `IMyInterface`).
- Use descriptive names; avoid abbreviations unless widely understood (e.g., `Id`, `Url`).

### Code Style

- Use file-scoped namespaces (`namespace Tomlyn;`) unless the file requires multiple namespaces.
- Use `var` when the type is obvious from the right-hand side; otherwise, use explicit types.
- Prefer expression-bodied members for single-line implementations.
- Use pattern matching and switch expressions where they improve readability.
- Place `using` directives outside the namespace, sorted alphabetically with `System` namespaces first.

### Nullable Reference Types

- This project uses nullable reference types. Respect nullability annotations.
- Never suppress nullable warnings (`#pragma warning disable`) without a comment explaining why.
- Use `ArgumentNullException.ThrowIfNull()` for null checks on parameters.
- Prefer `is null` and `is not null` over `== null` and `!= null`.

### Error Handling

- Throw `ArgumentException` or `ArgumentNullException` for invalid arguments.
- Use specific exception types rather than generic `Exception`.
- Include meaningful error messages that help diagnose the issue.
- Document exceptions in XML comments using `<exception cref="...">`.

### Async/Await

- Suffix async methods with `Async` (e.g., `LoadDataAsync`).
- Use `ConfigureAwait(false)` in library code unless context capture is required.
- Prefer `ValueTask<T>` over `Task<T>` for hot paths that often complete synchronously.
- Never use `async void` except for event handlers.

## Performance Considerations

- Ensure that the code is optimized for performance without sacrificing readability.
- Ensure that the code minimizes GC allocations where possible.
  - Use `Span<T>`/`ReadOnlySpan<T>` where appropriate to reduce memory allocations.
  - Use `stackalloc` for small, fixed-size buffers in performance-critical paths.
  - Prefer `StringBuilder` for string concatenation in loops.
  - Use `ArrayPool<T>` for temporary arrays that would otherwise cause allocations.
- Ensure generated code is AOT-compatible and trimmer-friendly.
  - Avoid reflection where possible; prefer source generators.
  - Use `[DynamicallyAccessedMembers]` attributes when reflection is necessary.
- Use `sealed` on classes that are not designed for inheritance to enable devirtualization.
- Prefer `ReadOnlySpan<char>` over `string` for parsing and substring operations.

## Source Generator Guidelines

- The `TomlModelAttribute` only needs to be applied to the root type; nested model types are discovered transitively.
- Ensure generated code is AOT-compatible and trimmer-friendly.

## Testing Guidelines

### Test Organization

- Name test classes as `{ClassName}Tests` (e.g., `ParserTests`).
- Name test methods descriptively: `{MethodName}_{Scenario}_{ExpectedResult}` or use plain English.
- Group related tests using `#region` or nested classes if the test file is large.

### Test Quality

- Each test should verify one specific behavior (single assertion concept).
- Use the Arrange-Act-Assert (AAA) pattern.
- Include edge cases: null inputs, empty collections, boundary values, and error conditions.
- Avoid test interdependencies; each test must be able to run in isolation.

### Test Coverage

- Aim for high coverage of public APIs and critical code paths.
- Prioritize testing complex logic, error handling, and edge cases over trivial code.
- When fixing a bug, first write a test that reproduces the bug, then fix it.

## API Design Guidelines

- Follow .NET API design guidelines for consistency with the ecosystem.
- Don't over engineer APIs; keep them simple and focused.
- Don't introduce unnecessary interface abstractions; prefer concrete types unless specific extensibility is required that is preferred through an interface.
- Use immutable types where possible to enhance thread safety and predictability.
- Allow mutable types when necessary for performance or usability.
- Make APIs hard to misuse: validate inputs early, use strong types.
- Prefer method overloads over optional parameters for binary compatibility.
- Use `params ReadOnlySpan<T>` for variadic methods (C# 13+) when targeting modern runtimes.
- Consider adding `Try*` pattern methods (returning `bool`) alongside throwing versions.
- Mark obsolete APIs with `[Obsolete("message", error: false)]` before removal.

## Git Commit Instructions

- Write a concise and descriptive commit message that summarizes the changes made.
- Start the commit message with a verb in imperative mood (e.g., "Add", "Fix", "Update", "Remove").
- Keep the first line under 72 characters; add details in the body if needed.
- Create a commit for each logical change or feature added to facilitate easier code review and tracking of changes.
- Reference related issues in commit messages when applicable (e.g., "Fix #123").

## Pre-Submission Checklist

Before submitting changes, verify:

- [ ] Code builds without errors or warnings (`dotnet build -c Release`).
- [ ] All tests pass (`dotnet test -c Release`).
- [ ] New public APIs have XML documentation comments.
- [ ] Changes are covered by unit tests.
- [ ] No unintended files are included in the commit. But don't remove code that was changed locally but is not part of your intended changes.
- [ ] Documentation is updated if behavior changes.
