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
- Write clear and concise comments to explain the purpose and functionality of your code.
- All public APIs must have XML documentation comments to avoid CS1591 warnings.
- Ensure that your code is well-structured and modular to facilitate maintenance and future enhancements.
- Adhere to best practices for error handling and input validation.
- Write unit tests for any new functionality you add to ensure code quality and reliability.
  - When fixing a bug, add a unit test that reproduces the bug before implementing the fix.
- Use meaningful variable and method names that accurately reflect their purpose.
- Avoid code duplication by reusing existing methods and classes whenever possible.

## Performance Considerations

- Ensure that the code is optimized for performance without sacrificing readability.
- Ensure that the code minimizes GC allocations where possible.
  - Use `Span<T>`/`ReadOnlySpan<T>` where appropriate to reduce memory allocations.

## Source Generator Guidelines

- The `TomlModelAttribute` only needs to be applied to the root type; nested model types are discovered transitively.
- Ensure generated code is AOT-compatible and trimmer-friendly.

## Git Commit Instructions

- Write a concise and descriptive commit message that summarizes the changes made.
- Create a commit for each logical change or feature added to facilitate easier code review and tracking of changes.
