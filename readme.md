# Tomlyn [![Build Status](https://github.com/xoofx/Tomlyn/workflows/ci/badge.svg?branch=main)](https://github.com/xoofx/Tomlyn/actions)  [![Coverage Status](https://coveralls.io/repos/github/xoofx/Tomlyn/badge.svg?branch=main)](https://coveralls.io/github/xoofx/Tomlyn?branch=main)  [![NuGet](https://img.shields.io/nuget/v/Tomlyn.svg)](https://www.nuget.org/packages/Tomlyn/)

<img align="right" width="160px" height="160px" src="img/logo.png">

Tomlyn is a TOML parser, validator and authoring library for .NET Framework and .NET Core

## Features

- Very fast parser, GC friendly.
- Compatible with the latest [TOML 1.0.0 specs](https://github.com/toml-lang/toml).
- Allow to map a TOML string to a default runtime model via `Toml.ToModel(string)`
- Allow to map a TOML string to a custom runtime model via `Toml.ToModel<T>(string)`
  - Very convenient for loading custom configurations for example.
- Allow to generate a TOML string from a runtime model via `string Toml.FromModel(object)`
  - Preserve comments, by default with the default runtime model, or by implementing the `ITomlMetadataProvider`.
- Allow to parse to a `DocumentSyntax` via `Toml.Parse(string)`.
  - Preserve all spaces, new line, comments but also invalid characters/tokens.
  - Can roundtrip to text with exact representation.
- Provides a validator with the `Toml.Validate` method.
- Supports for .NET Standard 2.0+ and provides an API with nullable annotations.

## Documentation

See the [documentation](https://github.com/xoofx/Tomlyn/blob/main/doc/readme.md) for more details.

## Usage

```c#
var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

// Parse the TOML string to the default runtime model `TomlTable`
var model = Toml.ToModel(toml);
// Generates a TOML string from the model
var tomlOut = Toml.FromModel(model);
// Output the generated TOML
Console.WriteLine(tomlOut);
```

This will print the original TOML by preserving most the comments:

```toml
global = "this is a string"
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
```

## License

This software is released under the [BSD-Clause 2 license](https://opensource.org/licenses/BSD-2-Clause). 

## Credits

Modified version of the logo `Thor` by [Mike Rowe](https://thenounproject.com/itsmikerowe/) from the Noun Project (Creative Commons)

## Author

Alexandre Mutel aka [xoofx](http://xoofx.com).
