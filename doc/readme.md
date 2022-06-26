# Tomlyn Documentation

Tomlyn provides several ways to interact with TOML content:

- [1. Convert a TOML string to a runtime model](#1-convert-a-toml-string-to-a-runtime-model)
  - [1.1 To a generic model `TomlTable`](#11-to-a-generic-model-tomltable)
  - [1.2 To a custom model](#12-to-a-custom-model)
- [2. Convert a runtime model to TOML string](#2-convert-a-runtime-model-to-toml-string)
  - [2.1 `TomlTable` to a TOML string](#21-tomltable-to-a-toml-string)
  - [2.2 A custom model to a TOML string](#22-a-custom-model-to-a-toml-string)
  - [2.3 Preserving comments on custom model](#23-preserving-comments-on-custom-model)
- [3. Parse and tokenize a TOML string](#3-parse-and-tokenize-a-toml-string)
  - [3.1 Parsing a TOML string to a `DocumentSyntax` tree](#31-parsing-a-toml-string-to-a-documentsyntax-tree)
  - [3.2 Fetching tokens from a TOML string](#32-fetching-tokens-from-a-toml-string)

## 1. Convert a TOML string to a runtime model

Use this approach if you are looking to access the data with convenient accessors.

See [Parse and tokenize a TOML string](#3-parse-and-tokenize-a-toml-string) if you are requiring to have precise processing of the original TOML document.

### 1.1 To a generic model `TomlTable`

The default `Toml.ToModel(string)` method provides a convenient way to map to a dynamic model.

This mapping comes with the following default handling:

- A TOML table maps to a `TomlTable` object and is in practice a `IDictionary<string, object?>`.
- A TOML table array  maps to a `TomlTableArray` object
- A TOML array maps to a `TomlArray` object and is in practice a `IList<object?>`.
- Floats/Double are all mapped to C# `double`.
- Integers are all mapped to C# `long`.
- Comments are preserved by default as `TomlTable` is implementing `ITomlMetadataProvider` (See [Convert a runtime model to TOML string](#2-convert-a-runtime-model-to-toml-string) for more details).

```c#
var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

// Converts the TOML string to a `TomlTable`
var model = Toml.ToModel(toml);
// Prints "this is a string"
Console.WriteLine(model["global"]);
// Prints "1"
Console.WriteLine(((TomlTable)model["my_table"]!)["key"]);
// Prints 4, 5, 6
Console.WriteLine(string.Join(", ", (TomlArray)((TomlTable)model["my_table"]!)["list"]));
```

### 1.2 To a custom model

Tomlyn allows to map to a TOML string to a custom runtime model by using `System.Reflection` via the method `Toml.ToModel<T>(string)`.

The mapping comes with a few requirements and is highly customizable:

- Maps properties only.
  - Readable only properties for string/value types are ignored.
  - Can ignore properties via attributes (`JsonIgnore`, `DataMemberIgnore`). These attributes can be configured in `TomlModelOptions.AttributeListForIgnore`
  - By default property names are lowered and split by `_` by PascalCase letters
    - For example: `ThisIsAnExample` becomes `this_is_an_example`
    - This behavior can be changed by passing a `TomlModelOptions` and specifying the `TomlModelOptions.ConvertPropertyName` delegate.
    - If the property contains a special attribute (e.g `JsonPropertyName`, `DataMember`), it will use this name instead. You can control the list of attributes in `TomlModelOptions.AttributeListForGetName`.
  - It is possible to override the lowering of a property at a lower level via `TomlModelOptions.GetPropertyName` delegate. Note that this delegate is handling the renaming by attributes and calling the configured `TomlModelOptions.ConvertPropertyName`.
- Requires class types to have a parameter-less constructor.
- Supports all C# primitive types, `string`, `DateTime`, `DateTimeOffset` and `TomlDateTime`.
- Prefer concrete types instead of interfaces or abstract types.
  - Unless you can guarantee that your model provides an instantiation of the interface (e.g see example below with the `MyTable.ListOfIntegers` property returning an `IList<int>`)
  - You can customize instances created by setting a delegate on `TomlModelOptions.CreateInstance(Type, ObjectKind)`.
- Conversion between types supports `IConvertible` by default
  - You can customize the conversion by setting a delegate on `TomlModelOptions.ConvertTo(object value, Type toType)`
- Collections used in the model must inherit from `ICollection<T>`
- Dictionary used in the model must inherit from `IDictionary<TKey, TValue>`

> NOTE: if the model is not able to map properties, it will throw a `TomlException` detailing the missing properties.
> You can use `Toml.TryToModel<T>` which returns a `DiagnosticBag` in case of an error without throwing an exception.

```c#
var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

var model = Toml.ToModel<MyModel>(toml);
// Prints "this is a string"
Console.WriteLine($"found global = \"{model.Global}\"");
// Prints 1
var key = model.MyTable!.Key;
Console.WriteLine($"found key = {key}");
// Check list
var list = model.MyTable!.List;
Console.WriteLine($"found list = {string.Join(", ", list)}");

// Simple model that maps the TOML string above
class MyModel
{
    public string? Global { get; set; }

    public MyTable? MyTable { get; set; }
}

class MyTable
{
    public MyTable()
    {
        ListOfIntegers = new List<int>();
    }

    public int Key { get; set; }

    public bool Value { get; set; }

    // The type can be an interface if it is pre-instantiated by this instance.
    [DataMember(Name = "list")]
    public IList<int> ListOfIntegers { get; }

    [IgnoreDataMember]
    public string? ThisPropertyIsIgnored {get; set;}
}
```

## 2. Convert a runtime model to TOML string

You can easily write back a model to a TOML string by using the method `Toml.FromModel(object model)`.

### 2.1 `TomlTable` to a TOML string

```c#
var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

var model = Toml.ToModel(toml);
var tomlOut = Toml.FromModel(model);
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

### 2.2 A custom model to a TOML string


```c#
var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

var model = Toml.ToModel<MyModel>(toml);
var tomlOut = Toml.FromModel(model);
Console.WriteLine(tomlOut);
```

This will print the TOML without preserving comments:

```toml
global = "this is a string"
[my_table]
key = 1
value = true
list = [4, 5, 6]
```

### 2.3 Preserving comments on custom model

`TomlTable` is able to preserve the comments by implementing the interface `ITomlMetadataProvider` ([here](https://github.com/xoofx/Tomlyn/blob/main/src/Tomlyn/Model/ITomlMetadataProvider.cs)).

You can also preserve comments and whitespace with a custom model by implementing this interface. You just need to explicitly implement the interface:

```c#
public class MyModel : ITomlMetadataProvider
{
    // storage for comments and whitespace
    TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
    // ...
``` 

For example comments can be preserved on the previous model by adding the interface:

```c#
class MyModel : ITomlMetadataProvider
{
    public string? Global { get; set; }

    public MyTable? MyTable { get; set; }

    // storage for comments and whitespace
    TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}

class MyTable : ITomlMetadataProvider
{
    public MyTable()
    {
        ListOfIntegers = new List<int>();
    }

    public int Key { get; set; }

    public bool Value { get; set; }

    [DataMember(Name = "list")]
    public List<int> ListOfIntegers { get; }

    [IgnoreDataMember]
    public string? ThisPropertyIsIgnored { get; set; }

    // storage for comments and whitespace
    TomlPropertiesMetadata? ITomlMetadataProvider.PropertiesMetadata { get; set; }
}
```

## 3. Parse and tokenize a TOML string

It is sometimes require to work at lower level for authoring tools (IDE, syntax highlighting, syntax validator...etc.) via the method `Toml.Parse(string)`.

Tomlyn provides an a `DocumentSyntax` API to work directly on the syntax tree:

- Provides an exact representation of the original document, including comments, whitespace, newlines and invalid characters/tokens.
- Can be saved back to disk even if the TOML document is invalid.
- The document is validated with a TOML validator. You can disable the validator or run it separately via `Toml.Validate(DocumentSyntax)`.

### 3.1 Parsing a TOML string to a `DocumentSyntax` tree

```c#
var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

var documentSyntax = Toml.Parse(toml);
// Prints any errors on the syntax
if (documentSyntax.HasErrors) {
    foreach(var message in documentSyntax.Diagnostics) {
        Console.WriteLine(message);
    }
}
// Prints back the model to string
Console.WriteLine(documentSyntax);
``` 

The syntax tree provides convenient methods to navigate the syntax tree via the method `syntax.Descendants()`.

### 3.2 Fetching tokens from a TOML string

If you want to fetch tokens (e.g for syntax highlighting) you can parse to a `DocumentSyntax` and call the extension method `DocumentSyntax.Tokens()`.

```c#
var input = @"# This is a comment
[table]
key = 1 # This is another comment
test.sub.key = ""yes""
[[array]]
hello = true
";

var tokens = Toml.Parse(input).Tokens().ToList();
var builder = new StringBuilder();
foreach (var node in tokens)
{
    if (node is SyntaxTrivia trivia)
    {
        builder.AppendLine($"trivia: {trivia.Span}  {trivia.Kind} {(trivia.Text is not null ? TomlFormatHelper.ToString(trivia.Text, TomlPropertyDisplayKind.Default) : string.Empty)}");
    }
    else if (node is SyntaxToken token)
    {
        builder.AppendLine($"token: {token.Span} {(token.Text is not null ? TomlFormatHelper.ToString(token.Text, TomlPropertyDisplayKind.Default) : string.Empty)}");
    }
}
Console.WriteLine(builder);
```

It will print the following details:

```
trivia: (1,1)-(1,19)  Comment "# This is a comment"
trivia: (1,20)-(1,21)  NewLine "\r\n"
token: (2,1)-(2,1) "["
token: (2,2)-(2,6) "table"
token: (2,7)-(2,7) "]"
token: (2,8)-(2,9) "\r\n"
token: (3,1)-(3,3) "key"
trivia: (3,4)-(3,4)  Whitespaces " "
token: (3,5)-(3,5) "="
trivia: (3,6)-(3,6)  Whitespaces " "
token: (3,7)-(3,7) "1"
trivia: (3,8)-(3,8)  Whitespaces " "
trivia: (3,9)-(3,33)  Comment "# This is another comment"
token: (3,34)-(3,35) "\r\n"
token: (4,1)-(4,4) "test"
token: (4,5)-(4,5) "."
token: (4,6)-(4,8) "sub"
token: (4,9)-(4,9) "."
token: (4,10)-(4,12) "key"
trivia: (4,13)-(4,13)  Whitespaces " "
token: (4,14)-(4,14) "="
trivia: (4,15)-(4,15)  Whitespaces " "
token: (4,16)-(4,20) "\"yes\""
token: (4,21)-(4,22) "\r\n"
token: (5,1)-(5,2) "[["
token: (5,3)-(5,7) "array"
token: (5,8)-(5,9) "]]"
token: (5,10)-(5,11) "\r\n"
token: (6,1)-(6,5) "hello"
trivia: (6,6)-(6,6)  Whitespaces " "
token: (6,7)-(6,7) "="
trivia: (6,8)-(6,8)  Whitespaces " "
token: (6,9)-(6,12) "true"
token: (6,13)-(6,14) "\r\n"
```
