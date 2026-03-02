using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tomlyn.SourceGeneration;

[Generator]
public sealed class TomlSerializerContextGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor ContextMustBePartial = new(
        id: "TOMLYN001",
        title: "Toml serializer context must be partial",
        messageFormat: "Type '{0}' derives from Tomlyn.Serialization.TomlSerializerContext and must be declared partial to support source generation",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidConverterType = new(
        id: "TOMLYN002",
        title: "Invalid converter type",
        messageFormat: "Converter type '{0}' is invalid: {1}",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedMemberType = new(
        id: "TOMLYN003",
        title: "Unsupported member type",
        messageFormat: "Type '{0}' contains member '{1}' of unsupported type '{2}'. Add [JsonSerializable(typeof({2}))] to the context or change the member type.",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedDictionaryKeyType = new(
        id: "TOMLYN004",
        title: "Unsupported dictionary key type",
        messageFormat: "Type '{0}' contains member '{1}' of dictionary-like type '{2}' with non-string keys. TOML table keys must be strings.",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private const string TomlSerializerContextMetadataName = "Tomlyn.Serialization.TomlSerializerContext";
    private const string JsonSerializableAttributeMetadataName = "System.Text.Json.Serialization.JsonSerializableAttribute";
    private const string JsonSourceGenerationOptionsAttributeMetadataName = "System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute";
    private const string TomlSourceGenerationOptionsAttributeMetadataName = "Tomlyn.Serialization.TomlSourceGenerationOptionsAttribute";

    private sealed class ContextModel
    {
        public ContextModel(
            INamedTypeSymbol contextSymbol,
            string namespaceName,
            string typeName,
            ImmutableArray<ITypeSymbol> rootTypes,
            SourceGenOptions options,
            bool isValid)
        {
            ContextSymbol = contextSymbol;
            NamespaceName = namespaceName;
            TypeName = typeName;
            RootTypes = rootTypes;
            Options = options;
            IsValid = isValid;
        }

        public INamedTypeSymbol ContextSymbol { get; }
        public string NamespaceName { get; }
        public string TypeName { get; }
        public ImmutableArray<ITypeSymbol> RootTypes { get; }
        public SourceGenOptions Options { get; }
        public bool IsValid { get; }
    }

    private sealed class SourceGenOptions
    {
        public bool? WriteIndented { get; set; }
        public int? IndentSize { get; set; }
        public string? PropertyNamingPolicyExpression { get; set; }
        public string? DictionaryKeyPolicyExpression { get; set; }
        public bool? PropertyNameCaseInsensitive { get; set; }
        public ImmutableArray<ITypeSymbol> ConverterTypes { get; set; } = ImmutableArray<ITypeSymbol>.Empty;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidateContexts = context.SyntaxProvider
            .CreateSyntaxProvider(static (node, _) => node is ClassDeclarationSyntax, static (ctx, _) => TryCreateContextModel(ctx))
            .Where(static model => model is not null)!
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(candidateContexts), static (spc, input) =>
        {
            var compilation = input.Left;
            var models = input.Right;

            // De-dup contexts by metadata name.
            var byMetadataName = new Dictionary<string, ContextModel>(StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model is null)
                {
                    continue;
                }

                byMetadataName[model.ContextSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)] = model;
            }

            foreach (var model in byMetadataName.Values)
            {
                EmitContext(spc, compilation, model);
            }
        });
    }

    private static ContextModel? TryCreateContextModel(GeneratorSyntaxContext syntaxContext)
    {
        if (syntaxContext.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        if (syntaxContext.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        if (!DerivesFromTomlSerializerContext(classSymbol))
        {
            return null;
        }

        var roots = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var options = new SourceGenOptions();

        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (IsJsonSerializableAttribute(attribute))
            {
                if (attribute.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                var argument = attribute.ConstructorArguments[0];
                if (argument.Kind != TypedConstantKind.Type || argument.Value is not ITypeSymbol typeSymbol)
                {
                    continue;
                }

                roots.Add(typeSymbol);
                continue;
            }

            if (IsJsonSourceGenerationOptionsAttribute(attribute))
            {
                ApplyJsonSourceGenerationOptionsAttribute(attribute, options);
                continue;
            }

            if (IsTomlSourceGenerationOptionsAttribute(attribute))
            {
                ApplyTomlSourceGenerationOptionsAttribute(attribute, options);
            }
        }

        if (roots.Count == 0)
        {
            return null;
        }

        var isPartial = classDeclaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        var containingNamespace = classSymbol.ContainingNamespace;
        var namespaceName = containingNamespace is { IsGlobalNamespace: false } ? containingNamespace.ToDisplayString() : string.Empty;
        var typeName = classSymbol.Name;

        return new ContextModel(
            classSymbol,
            namespaceName,
            typeName,
            roots.ToImmutable(),
            options,
            isValid: isPartial);
    }

    private static void EmitContext(SourceProductionContext context, Compilation compilation, ContextModel model)
    {
        if (!model.IsValid)
        {
            context.ReportDiagnostic(Diagnostic.Create(ContextMustBePartial, model.ContextSymbol.Locations.FirstOrDefault(), model.ContextSymbol.ToDisplayString()));
            return;
        }

        ValidateConverters(context, model);

        var expanded = ExpandTypeGraph(context, model);
        var ordered = expanded
            .OrderBy(static t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
            .ToImmutableArray();

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine("using Tomlyn;");
        builder.AppendLine("using Tomlyn.Serialization;");
        builder.AppendLine();

        if (!string.IsNullOrEmpty(model.NamespaceName))
        {
            builder.Append("namespace ").Append(model.NamespaceName).AppendLine(";");
            builder.AppendLine();
        }

        builder.Append("partial class ").Append(model.TypeName).AppendLine();
        builder.AppendLine("{");

        builder.Append("    private ").Append(model.TypeName).AppendLine("(TomlSerializerOptions options, bool _generated) : base(options) { }");
        builder.AppendLine();

        builder.Append("    public static ").Append(model.TypeName).AppendLine(" Default { get; } = new(CreateDefaultOptions(), _generated: true);");
        builder.AppendLine();

        EmitCreateDefaultOptions(builder, model);

        foreach (var type in ordered)
        {
            EmitTypeInfoProperty(builder, model, type);
        }

        builder.AppendLine();
        builder.AppendLine("    public override TomlTypeInfo? GetTypeInfo(Type type, TomlSerializerOptions options)");
        builder.AppendLine("    {");
        foreach (var type in ordered)
        {
            builder.Append("        if (type == typeof(")
                .Append(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Append(")) return ")
                .Append(GetTypeInfoPropertyName(type))
                .AppendLine(";");
        }
        builder.AppendLine("        return null;");
        builder.AppendLine("    }");

        foreach (var type in ordered)
        {
            if (TryGetPocoShape(type, model.Options, out var poco))
            {
                EmitPocoTypeInfo(builder, model, type, poco);
            }
        }

        builder.AppendLine("}");

        context.AddSource($"{model.TypeName}.TomlSerializerContext.g.cs", builder.ToString());
    }

    private static void EmitCreateDefaultOptions(StringBuilder builder, ContextModel model)
    {
        builder.AppendLine("    private static TomlSerializerOptions CreateDefaultOptions()");
        builder.AppendLine("    {");
        builder.AppendLine("        var options = TomlSerializerOptions.Default;");

        if (!model.Options.ConverterTypes.IsDefaultOrEmpty)
        {
            builder.AppendLine("        var converters = new TomlConverter[]");
            builder.AppendLine("        {");
            foreach (var converterType in model.Options.ConverterTypes)
            {
                builder.Append("            new ").Append(converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).AppendLine("(),");
            }
            builder.AppendLine("        };");
            builder.AppendLine("        options = options with { Converters = converters };");
        }

        if (!string.IsNullOrEmpty(model.Options.PropertyNamingPolicyExpression))
        {
            builder.Append("        options = options with { PropertyNamingPolicy = ").Append(model.Options.PropertyNamingPolicyExpression).AppendLine(" };");
        }

        if (!string.IsNullOrEmpty(model.Options.DictionaryKeyPolicyExpression))
        {
            builder.Append("        options = options with { DictionaryKeyPolicy = ").Append(model.Options.DictionaryKeyPolicyExpression).AppendLine(" };");
        }

        if (model.Options.PropertyNameCaseInsensitive is not null)
        {
            builder.Append("        options = options with { PropertyNameCaseInsensitive = ").Append(model.Options.PropertyNameCaseInsensitive.Value ? "true" : "false").AppendLine(" };");
        }

        if (model.Options.WriteIndented is not null)
        {
            builder.Append("        options = options with { WriteIndented = ").Append(model.Options.WriteIndented.Value ? "true" : "false").AppendLine(" };");
        }

        if (model.Options.IndentSize is not null)
        {
            builder.Append("        options = options with { IndentSize = ").Append(model.Options.IndentSize.Value.ToString(CultureInfo.InvariantCulture)).AppendLine(" };");
        }

        builder.AppendLine("        return options;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitTypeInfoProperty(StringBuilder builder, ContextModel model, ITypeSymbol type)
    {
        var propertyName = GetTypeInfoPropertyName(type);
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        builder.Append("    private TomlTypeInfo<").Append(typeName).Append(">? _").Append(propertyName).AppendLine(";");
        builder.Append("    public TomlTypeInfo<").Append(typeName).Append("> ").Append(propertyName).AppendLine();
        builder.Append("        => _").Append(propertyName).Append(" ??= Create").Append(propertyName).AppendLine("();");
        builder.AppendLine();

        builder.Append("    private TomlTypeInfo<").Append(typeName).Append("> Create").Append(propertyName).AppendLine("()");
        builder.AppendLine("    {");

        if (IsBuiltInType(type))
        {
            builder.Append("        return GetBuiltInTypeInfo<").Append(typeName).AppendLine(">(Options);");
        }
        else if (TryGetArrayElementType(type, out var arrayElementType))
        {
            builder.Append("        return CreateArrayTypeInfo<")
                .Append(arrayElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .AppendLine(">(this);");
        }
        else if (TryGetEnumerableElementType(type, out var enumerableElementType, out var isConcreteList))
        {
            if (isConcreteList)
            {
                builder.Append("        return CreateListTypeInfo<")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else
            {
                builder.Append("        return CreateListBackedEnumerableTypeInfo<")
                    .Append(typeName)
                    .Append(", ")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
        }
        else if (TryGetDictionaryValueType(type, out var dictionaryValueType))
        {
            builder.Append("        return CreateDictionaryTypeInfo<")
                .Append(typeName)
                .Append(", ")
                .Append(dictionaryValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .AppendLine(">(this);");
        }
        else
        {
            builder.Append("        return new __TomlTypeInfo_").Append(propertyName).AppendLine("(this);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitPocoTypeInfo(StringBuilder builder, ContextModel model, ITypeSymbol type, PocoShape poco)
    {
        var propertyName = GetTypeInfoPropertyName(type);
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var readReturnType = type.IsValueType ? typeName : typeName + "?";

        builder.AppendLine();
        builder.Append("    private sealed class __TomlTypeInfo_").Append(propertyName).Append(" : TomlTypeInfo<").Append(typeName).AppendLine(">");
        builder.AppendLine("    {");
        builder.Append("        private readonly ").Append(model.TypeName).AppendLine(" _context;");
        builder.AppendLine();
        builder.Append("        public __TomlTypeInfo_").Append(propertyName).Append("(").Append(model.TypeName).AppendLine(" context) : base(context.Options)");
        builder.AppendLine("        {");
        builder.AppendLine("            _context = context;");
        builder.AppendLine("        }");
        builder.AppendLine();

        builder.Append("        public override void Write(TomlWriter writer, ").Append(typeName).AppendLine(" value)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (value is null) throw new TomlException(\"TOML does not support null values.\");");
        builder.AppendLine("            writer.WriteStartTable();");
        for (var i = 0; i < poco.Members.Length; i++)
        {
            var member = poco.Members[i];
            var memberTypeName = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var canBeNull = CanBeNull(member.Type);

            builder.Append("            var __member").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = value.").Append(member.MemberName).AppendLine(";");
            EmitWriteMember(builder, member, i, memberTypeName, canBeNull);
        }
        builder.AppendLine("            writer.WriteEndTable();");
        builder.AppendLine("        }");
        builder.AppendLine();

        builder.Append("        public override ").Append(readReturnType).AppendLine(" Read(TomlReader reader)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (reader.TokenType != TomlTokenType.StartTable) throw reader.CreateException($\"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.\");");
        builder.Append("            var value = new ").Append(typeName).AppendLine("();");
        builder.AppendLine("            var seenMask = 0;");
        builder.AppendLine("            reader.Read();");
        builder.AppendLine("            while (reader.TokenType != TomlTokenType.EndTable)");
        builder.AppendLine("            {");
        builder.AppendLine("                if (reader.TokenType != TomlTokenType.PropertyName) throw reader.CreateException($\"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.\");");
        builder.AppendLine("                var name = reader.PropertyName!;");
        builder.AppendLine("                reader.Read();");
        builder.AppendLine();
        builder.AppendLine("                if (Options.PropertyNameCaseInsensitive)");
        builder.AppendLine("                {");
        EmitMemberDispatch(builder, poco, ignoreCase: true);
        builder.AppendLine("                }");
        builder.AppendLine("                else");
        builder.AppendLine("                {");
        EmitMemberDispatch(builder, poco, ignoreCase: false);
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        builder.AppendLine("            reader.Read();");
        builder.AppendLine("            return value;");
        builder.AppendLine("        }");

        builder.AppendLine("    }");
    }

    private static void EmitMemberDispatch(StringBuilder builder, PocoShape poco, bool ignoreCase)
    {
        var comparison = ignoreCase ? "StringComparison.OrdinalIgnoreCase" : "StringComparison.Ordinal";
        for (var i = 0; i < poco.Members.Length; i++)
        {
            var member = poco.Members[i];
            builder.Append("                    ").Append(i == 0 ? "if" : "else if").Append("(string.Equals(name, \"").Append(EscapeStringLiteral(member.SerializedName)).Append("\", ").Append(comparison).AppendLine("))");
            builder.AppendLine("                    {");
            builder.AppendLine("                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
            builder.AppendLine("                        {");
            builder.Append("                            const int bit = 1 << ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
            builder.AppendLine("                            if ((seenMask & bit) != 0) throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
            builder.AppendLine("                            seenMask |= bit;");
            builder.AppendLine("                        }");
            builder.Append("                        value.").Append(member.MemberName).Append(" = _context.").Append(GetTypeInfoPropertyName(member.Type)).AppendLine(".Read(reader)!;");
            builder.AppendLine("                        continue;");
            builder.AppendLine("                    }");
        }
        builder.AppendLine("                    reader.Skip();");
    }

    private static void EmitWriteMember(StringBuilder builder, PocoMember member, int index, string memberTypeName, bool canBeNull)
    {
        var localName = "__member" + index.ToString(CultureInfo.InvariantCulture);
        var writeIndent = "                ";
        var openIndent = "            ";
        var defaultLiteral = member.Type.IsReferenceType ? "default!" : "default";

        if (member.WriteIgnore == WriteIgnoreKind.WhenWritingNull)
        {
            if (canBeNull)
            {
                builder.Append(openIndent).Append("if (").Append(localName).AppendLine(" is not null)");
                builder.Append(openIndent).AppendLine("{");
                builder.Append(writeIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
                builder.Append(writeIndent).Append("_context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".Write(writer, ").Append(localName).AppendLine(");");
                builder.Append(openIndent).AppendLine("}");
                return;
            }

            builder.Append(openIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
            builder.Append(openIndent).Append("_context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".Write(writer, ").Append(localName).AppendLine(");");
            return;
        }

        if (member.WriteIgnore == WriteIgnoreKind.WhenWritingDefault)
        {
            builder.Append(openIndent).Append("if (!global::System.Collections.Generic.EqualityComparer<")
                .Append(memberTypeName)
                .Append(">.Default.Equals(")
                .Append(localName)
                .Append(", ")
                .Append(defaultLiteral)
                .AppendLine("))");
            builder.Append(openIndent).AppendLine("{");
            builder.Append(writeIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
            builder.Append(writeIndent).Append("_context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".Write(writer, ").Append(localName).AppendLine(");");
            builder.Append(openIndent).AppendLine("}");
            return;
        }

        builder.Append(openIndent).Append("if (!(");
        var hasPrevious = false;
        if (canBeNull)
        {
            builder.Append("Options.DefaultIgnoreCondition == global::Tomlyn.TomlIgnoreCondition.WhenWritingNull && ")
                .Append(localName)
                .Append(" is null");
            hasPrevious = true;
        }

        builder.Append(hasPrevious ? ") && !(" : string.Empty);
        builder.Append("Options.DefaultIgnoreCondition == global::Tomlyn.TomlIgnoreCondition.WhenWritingDefault && global::System.Collections.Generic.EqualityComparer<")
            .Append(memberTypeName)
            .Append(">.Default.Equals(")
            .Append(localName)
            .Append(", ")
            .Append(defaultLiteral)
            .Append(")");
        builder.AppendLine("))");
        builder.Append(openIndent).AppendLine("{");
        builder.Append(writeIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
        builder.Append(writeIndent).Append("_context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".Write(writer, ").Append(localName).AppendLine(");");
        builder.Append(openIndent).AppendLine("}");
    }

    private static bool CanBeNull(ITypeSymbol type)
    {
        if (type.IsReferenceType)
        {
            return true;
        }

        if (type is INamedTypeSymbol named &&
            named.IsGenericType &&
            named.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Nullable<T>")
        {
            return true;
        }

        return false;
    }

    private static ImmutableArray<ITypeSymbol> ExpandTypeGraph(SourceProductionContext context, ContextModel model)
    {
        var queue = new Queue<ITypeSymbol>(model.RootTypes);
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var list = new List<ITypeSymbol>();

        while (queue.Count != 0)
        {
            var current = queue.Dequeue();
            if (!seen.Add(current))
            {
                continue;
            }

            list.Add(current);

            if (TryGetArrayElementType(current, out var arrayElementType))
            {
                if (IsSupportedMemberType(arrayElementType))
                {
                    queue.Enqueue(arrayElementType);
                }

                continue;
            }

            if (TryGetEnumerableElementType(current, out var enumerableElementType, out _))
            {
                if (IsSupportedMemberType(enumerableElementType))
                {
                    queue.Enqueue(enumerableElementType);
                }

                continue;
            }

            if (TryGetDictionaryValueType(current, out var dictionaryValueType))
            {
                if (IsSupportedMemberType(dictionaryValueType))
                {
                    queue.Enqueue(dictionaryValueType);
                }

                continue;
            }

            if (TryGetPocoShape(current, model.Options, out var poco))
            {
                foreach (var member in poco.Members)
                {
                    if (TryGetDictionaryKeyValueTypes(member.Type, out var dictKey, out var dictValue) &&
                        dictKey.SpecialType != SpecialType.System_String)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            UnsupportedDictionaryKeyType,
                            model.ContextSymbol.Locations.FirstOrDefault(),
                            current.ToDisplayString(),
                            member.MemberName,
                            member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                        continue;
                    }

                    if (!IsSupportedMemberType(member.Type))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            UnsupportedMemberType,
                            model.ContextSymbol.Locations.FirstOrDefault(),
                            current.ToDisplayString(),
                            member.MemberName,
                            member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                        continue;
                    }

                    queue.Enqueue(member.Type);
                }
            }
        }

        return list.ToImmutableArray();
    }

    private static bool IsSupportedMemberType(ITypeSymbol type)
    {
        // v1 generator milestone: built-in scalars/containers and POCOs.
        if (IsBuiltInType(type))
        {
            return true;
        }

        if (TryGetArrayElementType(type, out var arrayElementType))
        {
            return IsSupportedMemberType(arrayElementType);
        }

        if (TryGetEnumerableElementType(type, out var enumerableElementType, out _))
        {
            return IsSupportedMemberType(enumerableElementType);
        }

        if (TryGetDictionaryKeyValueTypes(type, out var dictKeyType, out var dictValueType))
        {
            if (dictKeyType.SpecialType != SpecialType.System_String)
            {
                return false;
            }

            return IsSupportedMemberType(dictValueType);
        }

        if (type is INamedTypeSymbol named && named.TypeKind is (TypeKind.Class or TypeKind.Struct))
        {
            return true;
        }

        return false;
    }

    private sealed class PocoMember
    {
        public PocoMember(string memberName, string serializedName, ITypeSymbol type, WriteIgnoreKind writeIgnore)
        {
            MemberName = memberName;
            SerializedName = serializedName;
            Type = type;
            WriteIgnore = writeIgnore;
        }

        public string MemberName { get; }
        public string SerializedName { get; }
        public ITypeSymbol Type { get; }
        public WriteIgnoreKind WriteIgnore { get; }
    }

    private sealed class PocoShape
    {
        public PocoShape(ImmutableArray<PocoMember> members)
        {
            Members = members;
        }

        public ImmutableArray<PocoMember> Members { get; }
    }

    private enum WriteIgnoreKind
    {
        None = 0,
        WhenWritingNull = 1,
        WhenWritingDefault = 2,
    }

    private readonly struct IgnoreBehavior
    {
        public IgnoreBehavior(bool ignoreAlways, WriteIgnoreKind writeIgnore)
        {
            IgnoreAlways = ignoreAlways;
            WriteIgnore = writeIgnore;
        }

        public bool IgnoreAlways { get; }
        public WriteIgnoreKind WriteIgnore { get; }
    }

    private static bool TryGetPocoShape(ITypeSymbol type, SourceGenOptions options, out PocoShape shape)
    {
        shape = null!;

        if (IsBuiltInType(type))
        {
            return false;
        }

        if (TryGetArrayElementType(type, out _) ||
            TryGetEnumerableElementType(type, out _, out _) ||
            TryGetDictionaryKeyValueTypes(type, out _, out _))
        {
            return false;
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return false;
        }

        if (named.IsAbstract)
        {
            return false;
        }

        if (named.TypeKind == TypeKind.Class && !named.Constructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
        {
            return false;
        }

        var namingPolicy = options.PropertyNamingPolicyExpression;
        var useCamelCase = string.Equals(namingPolicy, "global::System.Text.Json.JsonNamingPolicy.CamelCase", StringComparison.Ordinal);

        var members = ImmutableArray.CreateBuilder<PocoMember>();
        foreach (var member in named.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsIndexer)
            {
                continue;
            }

            if (member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (member.SetMethod is null || member.GetMethod is null)
            {
                continue;
            }

            var ignore = GetIgnoreBehavior(member);
            if (ignore.IgnoreAlways)
            {
                continue;
            }

            var serializedName = GetSerializedName(member, member.Name, useCamelCase);
            members.Add(new PocoMember(member.Name, serializedName, member.Type, ignore.WriteIgnore));
        }

        foreach (var member in named.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsImplicitlyDeclared || member.IsConst || member.IsStatic || member.IsReadOnly)
            {
                continue;
            }

            if (member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (!HasAttribute(member, "Tomlyn.Serialization.TomlIncludeAttribute") &&
                !HasAttribute(member, "System.Text.Json.Serialization.JsonIncludeAttribute"))
            {
                continue;
            }

            var ignore = GetIgnoreBehavior(member);
            if (ignore.IgnoreAlways)
            {
                continue;
            }

            var serializedName = GetSerializedName(member, member.Name, useCamelCase);
            members.Add(new PocoMember(member.Name, serializedName, member.Type, ignore.WriteIgnore));
        }

        shape = new PocoShape(members.ToImmutable());
        return true;
    }

    private static bool TryGetArrayElementType(ITypeSymbol type, out ITypeSymbol elementType)
    {
        if (type is IArrayTypeSymbol array && array.Rank == 1)
        {
            elementType = array.ElementType;
            return true;
        }

        elementType = null!;
        return false;
    }

    private static bool TryGetEnumerableElementType(ITypeSymbol type, out ITypeSymbol elementType, out bool isConcreteList)
    {
        elementType = null!;
        isConcreteList = false;

        if (type is not INamedTypeSymbol named || !named.IsGenericType)
        {
            return false;
        }

        var constructedFrom = named.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (constructedFrom == "global::System.Collections.Generic.List<T>")
        {
            elementType = named.TypeArguments[0];
            isConcreteList = true;
            return true;
        }

        if (constructedFrom is
            "global::System.Collections.Generic.IEnumerable<T>" or
            "global::System.Collections.Generic.ICollection<T>" or
            "global::System.Collections.Generic.IReadOnlyCollection<T>" or
            "global::System.Collections.Generic.IList<T>" or
            "global::System.Collections.Generic.IReadOnlyList<T>")
        {
            elementType = named.TypeArguments[0];
            return true;
        }

        return false;
    }

    private static bool TryGetDictionaryKeyValueTypes(ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType)
    {
        keyType = null!;
        valueType = null!;

        if (type is not INamedTypeSymbol named || !named.IsGenericType)
        {
            return false;
        }

        var constructedFrom = named.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (constructedFrom is not
            "global::System.Collections.Generic.Dictionary<TKey, TValue>" and not
            "global::System.Collections.Generic.IDictionary<TKey, TValue>" and not
            "global::System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>")
        {
            return false;
        }

        keyType = named.TypeArguments[0];
        valueType = named.TypeArguments[1];
        return true;
    }

    private static bool TryGetDictionaryValueType(ITypeSymbol type, out ITypeSymbol valueType)
    {
        valueType = null!;
        if (!TryGetDictionaryKeyValueTypes(type, out var keyType, out var innerValueType))
        {
            return false;
        }

        if (keyType.SpecialType != SpecialType.System_String)
        {
            return false;
        }

        valueType = innerValueType;
        return true;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeMetadataName)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + attributeMetadataName)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSerializedName(ISymbol member, string memberName, bool useCamelCase)
    {
        foreach (var attr in member.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attrName == "global::Tomlyn.Serialization.TomlPropertyNameAttribute" &&
                attr.ConstructorArguments.Length == 1 &&
                attr.ConstructorArguments[0].Value is string tomlName)
            {
                return tomlName;
            }

            if (attrName == "global::System.Text.Json.Serialization.JsonPropertyNameAttribute" &&
                attr.ConstructorArguments.Length == 1 &&
                attr.ConstructorArguments[0].Value is string jsonName)
            {
                return jsonName;
            }
        }

        if (useCamelCase)
        {
            return ToCamelCase(memberName);
        }

        return memberName;
    }

    private static IgnoreBehavior GetIgnoreBehavior(ISymbol symbol)
    {
        TomlIgnoreAttributeModel? toml = null;
        JsonIgnoreAttributeModel? json = null;

        foreach (var attr in symbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attrName == "global::Tomlyn.Serialization.TomlIgnoreAttribute")
            {
                toml = TomlIgnoreAttributeModel.From(attr);
            }
            else if (attrName == "global::System.Text.Json.Serialization.JsonIgnoreAttribute")
            {
                json = JsonIgnoreAttributeModel.From(attr);
            }
        }

        if (toml is not null)
        {
            // Interpret [TomlIgnore] with default Condition as "ignore always".
            return toml.Value.Condition switch
            {
                1 => new IgnoreBehavior(ignoreAlways: false, writeIgnore: WriteIgnoreKind.WhenWritingNull),
                2 => new IgnoreBehavior(ignoreAlways: false, writeIgnore: WriteIgnoreKind.WhenWritingDefault),
                _ => new IgnoreBehavior(ignoreAlways: true, writeIgnore: WriteIgnoreKind.None),
            };
        }

        if (json is not null)
        {
            var condition = json.Value.Condition ?? JsonIgnoreCondition.Always;
            return condition switch
            {
                JsonIgnoreCondition.Never => new IgnoreBehavior(ignoreAlways: false, writeIgnore: WriteIgnoreKind.None),
                JsonIgnoreCondition.WhenWritingNull => new IgnoreBehavior(ignoreAlways: false, writeIgnore: WriteIgnoreKind.WhenWritingNull),
                JsonIgnoreCondition.WhenWritingDefault => new IgnoreBehavior(ignoreAlways: false, writeIgnore: WriteIgnoreKind.WhenWritingDefault),
                _ => new IgnoreBehavior(ignoreAlways: true, writeIgnore: WriteIgnoreKind.None),
            };
        }

        return new IgnoreBehavior(ignoreAlways: false, writeIgnore: WriteIgnoreKind.None);
    }

    private readonly struct TomlIgnoreAttributeModel
    {
        public TomlIgnoreAttributeModel(int condition)
        {
            Condition = condition;
        }

        public int Condition { get; }

        public static TomlIgnoreAttributeModel From(AttributeData attribute)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "Condition" && namedArg.Value.Value is int value)
                {
                    return new TomlIgnoreAttributeModel(value);
                }
            }

            // Default: Never (treated as "ignore always" at generation time).
            return new TomlIgnoreAttributeModel(0);
        }
    }

    private readonly struct JsonIgnoreAttributeModel
    {
        public JsonIgnoreAttributeModel(JsonIgnoreCondition? condition)
        {
            Condition = condition;
        }

        public JsonIgnoreCondition? Condition { get; }

        public static JsonIgnoreAttributeModel From(AttributeData attribute)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "Condition" && namedArg.Value.Value is int value)
                {
                    return new JsonIgnoreAttributeModel((JsonIgnoreCondition)value);
                }
            }

            // Default: Always.
            return new JsonIgnoreAttributeModel(null);
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (!char.IsUpper(name[0]))
        {
            return name;
        }

        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var hasNext = i + 1 < chars.Length;
            if (i > 0 && hasNext && char.IsUpper(chars[i + 1]))
            {
                chars[i] = char.ToLowerInvariant(chars[i]);
                continue;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
            break;
        }

        return new string(chars);
    }

    private static bool IsBuiltInType(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_String:
            case SpecialType.System_Boolean:
            case SpecialType.System_Int64:
            case SpecialType.System_Double:
                return true;
        }

        var metadataName = type is INamedTypeSymbol named
            ? named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return metadataName is
            "global::Tomlyn.TomlDateTime" or
            "global::Tomlyn.Model.TomlTable" or
            "global::Tomlyn.Model.TomlArray" or
            "global::Tomlyn.Model.TomlTableArray";
    }

    private static string GetTypeInfoPropertyName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var baseName = named.Name;
            var args = string.Concat(named.TypeArguments.Select(static a => GetTypeInfoPropertyName(a)));
            return SanitizeIdentifier(baseName + args);
        }

        return SanitizeIdentifier(type.Name);
    }

    private static string SanitizeIdentifier(string name)
    {
        var builder = new StringBuilder(name.Length);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                builder.Append(c);
            }
        }

        if (builder.Length == 0)
        {
            return "Type";
        }

        if (!char.IsLetter(builder[0]) && builder[0] != '_')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private static bool DerivesFromTomlSerializerContext(INamedTypeSymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + TomlSerializerContextMetadataName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsJsonSerializableAttribute(AttributeData attribute)
        => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + JsonSerializableAttributeMetadataName;

    private static bool IsJsonSourceGenerationOptionsAttribute(AttributeData attribute)
        => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + JsonSourceGenerationOptionsAttributeMetadataName;

    private static bool IsTomlSourceGenerationOptionsAttribute(AttributeData attribute)
        => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + TomlSourceGenerationOptionsAttributeMetadataName;

    private static void ApplyJsonSourceGenerationOptionsAttribute(AttributeData attribute, SourceGenOptions options)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            var name = namedArgument.Key;
            var value = namedArgument.Value;
            switch (name)
            {
                case "PropertyNamingPolicy":
                    options.PropertyNamingPolicyExpression ??= ToJsonNamingPolicyExpression(value);
                    break;
                case "DictionaryKeyPolicy":
                    options.DictionaryKeyPolicyExpression ??= ToJsonNamingPolicyExpression(value);
                    break;
                case "PropertyNameCaseInsensitive":
                    if (options.PropertyNameCaseInsensitive is null && value.Value is bool pnci) options.PropertyNameCaseInsensitive = pnci;
                    break;
            }
        }
    }

    private static void ApplyTomlSourceGenerationOptionsAttribute(AttributeData attribute, SourceGenOptions options)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            var name = namedArgument.Key;
            var value = namedArgument.Value;
            switch (name)
            {
                case "WriteIndented":
                    if (value.Value is bool wi) options.WriteIndented = wi;
                    break;
                case "IndentSize":
                    if (value.Value is int indent) options.IndentSize = indent;
                    break;
                case "PropertyNameCaseInsensitive":
                    if (value.Value is bool pnci) options.PropertyNameCaseInsensitive = pnci;
                    break;
                case "PropertyNamingPolicy":
                    options.PropertyNamingPolicyExpression = ToJsonNamingPolicyExpression(value);
                    break;
                case "DictionaryKeyPolicy":
                    options.DictionaryKeyPolicyExpression = ToJsonNamingPolicyExpression(value);
                    break;
                case "Converters":
                    if (value.Kind == TypedConstantKind.Array && !value.Values.IsDefault)
                    {
                        var converterTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
                        foreach (var item in value.Values)
                        {
                            if (item.Kind == TypedConstantKind.Type && item.Value is ITypeSymbol typeSymbol)
                            {
                                converterTypes.Add(typeSymbol);
                            }
                        }

                        options.ConverterTypes = converterTypes.ToImmutable();
                    }
                    break;
            }
        }
    }

    private static void ValidateConverters(SourceProductionContext context, ContextModel model)
    {
        if (model.Options.ConverterTypes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var type in model.Options.ConverterTypes)
        {
            if (type is not INamedTypeSymbol named)
            {
                continue;
            }

            if (named.TypeKind is not (TypeKind.Class or TypeKind.Struct))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidConverterType, model.ContextSymbol.Locations.FirstOrDefault(), type.ToDisplayString(), "Converters must be classes or structs."));
                continue;
            }

            if (!named.Constructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidConverterType, model.ContextSymbol.Locations.FirstOrDefault(), type.ToDisplayString(), "Converters must have a public parameterless constructor."));
            }
        }
    }

    private static string? ToJsonNamingPolicyExpression(TypedConstant constant)
    {
        if (constant.Kind != TypedConstantKind.Enum || constant.Value is not int value)
        {
            return null;
        }

        var policy = (JsonKnownNamingPolicy)value;
        if (policy == JsonKnownNamingPolicy.CamelCase)
        {
            return "global::System.Text.Json.JsonNamingPolicy.CamelCase";
        }

        return null;
    }

    private static string EscapeStringLiteral(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
