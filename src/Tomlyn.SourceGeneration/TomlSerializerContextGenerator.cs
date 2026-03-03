using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    private static readonly DiagnosticDescriptor InvalidSourceGenerationOption = new(
        id: "TOMLYN005",
        title: "Invalid source generation option",
        messageFormat: "Invalid source generation option on context '{0}': {1}",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private const string TomlSerializerContextMetadataName = "Tomlyn.Serialization.TomlSerializerContext";
    private const string JsonSerializableAttributeMetadataName = "System.Text.Json.Serialization.JsonSerializableAttribute";
    private const string JsonSourceGenerationOptionsAttributeMetadataName = "System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute";
    private const string TomlSourceGenerationOptionsAttributeMetadataName = "Tomlyn.Serialization.TomlSourceGenerationOptionsAttribute";
    private const string TomlConverterMetadataName = "Tomlyn.Serialization.TomlConverter";
    private const string TomlOnSerializingMetadataName = "Tomlyn.Serialization.ITomlOnSerializing";
    private const string TomlOnSerializedMetadataName = "Tomlyn.Serialization.ITomlOnSerialized";
    private const string TomlOnDeserializingMetadataName = "Tomlyn.Serialization.ITomlOnDeserializing";
    private const string TomlOnDeserializedMetadataName = "Tomlyn.Serialization.ITomlOnDeserialized";

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
        public int? NewLine { get; set; }
        public string? PropertyNamingPolicyExpression { get; set; }
        public string? DictionaryKeyPolicyExpression { get; set; }
        public bool? PropertyNameCaseInsensitive { get; set; }
        public int? DefaultIgnoreCondition { get; set; }
        public int? DuplicateKeyHandling { get; set; }
        public int? MappingOrder { get; set; }
        public int? DottedKeyHandling { get; set; }
        public int? RootValueHandling { get; set; }
        public string? RootValueKeyName { get; set; }
        public int? InlineTablePolicy { get; set; }
        public int? TableArrayStyle { get; set; }
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
        ValidateSourceGenerationOptions(context, model);

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

        if (model.Options.DefaultIgnoreCondition is not null && TryGetTomlIgnoreConditionExpression(model.Options.DefaultIgnoreCondition.Value, out var ignoreConditionExpression))
        {
            builder.Append("        options = options with { DefaultIgnoreCondition = ").Append(ignoreConditionExpression).AppendLine(" };");
        }

        if (model.Options.DuplicateKeyHandling is not null && TryGetTomlDuplicateKeyHandlingExpression(model.Options.DuplicateKeyHandling.Value, out var duplicateKeyHandlingExpression))
        {
            builder.Append("        options = options with { DuplicateKeyHandling = ").Append(duplicateKeyHandlingExpression).AppendLine(" };");
        }

        if (model.Options.MappingOrder is not null && TryGetTomlMappingOrderPolicyExpression(model.Options.MappingOrder.Value, out var mappingOrderExpression))
        {
            builder.Append("        options = options with { MappingOrder = ").Append(mappingOrderExpression).AppendLine(" };");
        }

        if (model.Options.DottedKeyHandling is not null && TryGetTomlDottedKeyHandlingExpression(model.Options.DottedKeyHandling.Value, out var dottedKeyHandlingExpression))
        {
            builder.Append("        options = options with { DottedKeyHandling = ").Append(dottedKeyHandlingExpression).AppendLine(" };");
        }

        if (model.Options.WriteIndented is not null)
        {
            builder.Append("        options = options with { WriteIndented = ").Append(model.Options.WriteIndented.Value ? "true" : "false").AppendLine(" };");
        }

        if (model.Options.IndentSize is not null)
        {
            builder.Append("        options = options with { IndentSize = ").Append(model.Options.IndentSize.Value.ToString(CultureInfo.InvariantCulture)).AppendLine(" };");
        }

        if (model.Options.NewLine is not null && TryGetTomlNewLineKindExpression(model.Options.NewLine.Value, out var newLineExpression))
        {
            builder.Append("        options = options with { NewLine = ").Append(newLineExpression).AppendLine(" };");
        }

        if (model.Options.RootValueHandling is not null && TryGetTomlRootValueHandlingExpression(model.Options.RootValueHandling.Value, out var rootValueHandlingExpression))
        {
            builder.Append("        options = options with { RootValueHandling = ").Append(rootValueHandlingExpression).AppendLine(" };");
        }

        if (!string.IsNullOrWhiteSpace(model.Options.RootValueKeyName))
        {
            builder.Append("        options = options with { RootValueKeyName = \"").Append(EscapeStringLiteral(model.Options.RootValueKeyName!)).AppendLine("\" };");
        }

        if (model.Options.InlineTablePolicy is not null && TryGetTomlInlineTablePolicyExpression(model.Options.InlineTablePolicy.Value, out var inlineTableExpression))
        {
            builder.Append("        options = options with { InlineTablePolicy = ").Append(inlineTableExpression).AppendLine(" };");
        }

        if (model.Options.TableArrayStyle is not null && TryGetTomlTableArrayStyleExpression(model.Options.TableArrayStyle.Value, out var tableArrayExpression))
        {
            builder.Append("        options = options with { TableArrayStyle = ").Append(tableArrayExpression).AppendLine(" };");
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
        else if (TryGetNullableUnderlyingType(type, out var nullableUnderlyingType))
        {
            builder.Append("        return CreateSourceGeneratedNullableTypeInfo<")
                .Append(nullableUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .AppendLine(">(this);");
        }
        else if (TryGetArrayElementType(type, out var arrayElementType))
        {
            builder.Append("        return CreateSourceGeneratedArrayTypeInfo<")
                .Append(arrayElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .AppendLine(">(this);");
        }
        else if (TryGetSequenceElementType(type, out var enumerableElementType, out var kind))
        {
            if (kind == SequenceKind.List)
            {
                builder.Append("        return CreateSourceGeneratedListTypeInfo<")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else if (kind == SequenceKind.ListBackedEnumerable)
            {
                builder.Append("        return CreateSourceGeneratedListBackedEnumerableTypeInfo<")
                    .Append(typeName)
                    .Append(", ")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else if (kind == SequenceKind.HashSet)
            {
                builder.Append("        return CreateSourceGeneratedHashSetTypeInfo<")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else if (kind == SequenceKind.HashSetBackedEnumerable)
            {
                builder.Append("        return CreateSourceGeneratedHashSetBackedEnumerableTypeInfo<")
                    .Append(typeName)
                    .Append(", ")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else if (kind == SequenceKind.ImmutableArray)
            {
                builder.Append("        return CreateSourceGeneratedImmutableArrayTypeInfo<")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else if (kind == SequenceKind.ImmutableList)
            {
                builder.Append("        return CreateSourceGeneratedImmutableListTypeInfo<")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else if (kind == SequenceKind.ImmutableHashSet)
            {
                builder.Append("        return CreateSourceGeneratedImmutableHashSetTypeInfo<")
                    .Append(enumerableElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .AppendLine(">(this);");
            }
            else
            {
                builder.Append("        throw new global::System.InvalidOperationException(\"Unsupported sequence kind.\");");
            }
        }
        else if (TryGetDictionaryValueType(type, out var dictionaryValueType))
        {
            builder.Append("        return CreateSourceGeneratedDictionaryTypeInfo<")
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
        var callsOnSerializing = ImplementsInterface(type, TomlOnSerializingMetadataName);
        var callsOnSerialized = ImplementsInterface(type, TomlOnSerializedMetadataName);
        var callsOnDeserializing = ImplementsInterface(type, TomlOnDeserializingMetadataName);
        var callsOnDeserialized = ImplementsInterface(type, TomlOnDeserializedMetadataName);

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
        if (callsOnSerializing)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnSerializing)value).OnTomlSerializing();");
        }
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
        if (callsOnSerialized)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnSerialized)value).OnTomlSerialized();");
        }
        builder.AppendLine("        }");
        builder.AppendLine();

        builder.Append("        public override ").Append(readReturnType).AppendLine(" Read(TomlReader reader)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (reader.TokenType != TomlTokenType.StartTable) throw reader.CreateException($\"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.\");");
        builder.Append("            var value = new ").Append(typeName).AppendLine("();");
        if (callsOnDeserializing)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserializing)value).OnTomlDeserializing();");
        }
        builder.AppendLine("            var seenMask = 0;");
        builder.AppendLine("            reader.Read();");
        builder.AppendLine("            while (reader.TokenType != TomlTokenType.EndTable)");
        builder.AppendLine("            {");
        builder.AppendLine("                if (reader.TokenType != TomlTokenType.PropertyName) throw reader.CreateException($\"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.\");");
        builder.AppendLine("                if (Options.PropertyNameCaseInsensitive)");
        builder.AppendLine("                {");
        builder.AppendLine("                    var name = reader.PropertyName!;");
        builder.AppendLine("                    reader.Read();");
        EmitMemberDispatch(builder, poco, ignoreCase: true);
        builder.AppendLine("                }");
        builder.AppendLine("                else");
        builder.AppendLine("                {");
        EmitMemberDispatch(builder, poco, ignoreCase: false);
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        builder.AppendLine("            reader.Read();");
        if (callsOnDeserialized)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserialized)value).OnTomlDeserialized();");
        }
        builder.AppendLine("            return value;");
        builder.AppendLine("        }");

        builder.AppendLine("    }");
    }

    private static void EmitMemberDispatch(StringBuilder builder, PocoShape poco, bool ignoreCase)
    {
        if (ignoreCase)
        {
            var comparison = "StringComparison.OrdinalIgnoreCase";
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
            return;
        }

        var membersByHash = new Dictionary<ulong, List<(int Index, PocoMember Member)>>();
        for (var i = 0; i < poco.Members.Length; i++)
        {
            var member = poco.Members[i];
            var hash = ComputePropertyNameHash56(member.SerializedName);
            if (!membersByHash.TryGetValue(hash, out var bucket))
            {
                bucket = new List<(int, PocoMember)>(capacity: 1);
                membersByHash.Add(hash, bucket);
            }

            bucket.Add((i, member));
        }

        builder.AppendLine("                    if (reader.TryGetPropertyNameHash(out var hash))");
        builder.AppendLine("                    {");
        builder.AppendLine("                        switch (hash)");
        builder.AppendLine("                        {");

        foreach (var kvp in membersByHash.OrderBy(static pair => pair.Key))
        {
            builder.Append("                            case 0x").Append(kvp.Key.ToString("X", CultureInfo.InvariantCulture)).AppendLine("UL:");
            builder.AppendLine("                            {");

            foreach (var (index, member) in kvp.Value)
            {
                builder.Append("                                if (reader.PropertyNameEquals(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\"))");
                builder.AppendLine("                                {");
                builder.AppendLine("                                    if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                builder.AppendLine("                                    {");
                builder.Append("                                        const int bit = 1 << ").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                builder.AppendLine("                                        if ((seenMask & bit) != 0) throw reader.CreateException($\"Duplicate key '{reader.PropertyName}' was encountered.\");");
                builder.AppendLine("                                        seenMask |= bit;");
                builder.AppendLine("                                    }");
                builder.AppendLine("                                    reader.Read();");
                builder.Append("                                    value.").Append(member.MemberName).Append(" = _context.").Append(GetTypeInfoPropertyName(member.Type)).AppendLine(".Read(reader)!;");
                builder.AppendLine("                                    continue;");
                builder.AppendLine("                                }");
            }

            builder.AppendLine("                                break;");
            builder.AppendLine("                            }");
        }

        builder.AppendLine("                        }");
        builder.AppendLine("                    }");
        builder.AppendLine("                    reader.Read();");
        builder.AppendLine("                    reader.Skip();");
    }

    private static void EmitWriteMember(StringBuilder builder, PocoMember member, int index, string memberTypeName, bool canBeNull)
    {
        var localName = "__member" + index.ToString(CultureInfo.InvariantCulture);
        var writeArgument = canBeNull ? localName + "!" : localName;
        var writeIndent = "                ";
        var openIndent = "            ";
        var defaultLiteral = member.Type.IsReferenceType ? "default!" : "default";
        var defaultIsNull = member.Type.IsReferenceType || TryGetNullableUnderlyingType(member.Type, out _);

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
            if (defaultIsNull && canBeNull)
            {
                builder.Append(openIndent).Append("if (").Append(localName).AppendLine(" is not null)");
            }
            else
            {
                builder.Append(openIndent).Append("if (!global::System.Collections.Generic.EqualityComparer<")
                    .Append(memberTypeName)
                    .Append(">.Default.Equals(")
                    .Append(localName)
                    .Append(", ")
                    .Append(defaultLiteral)
                    .AppendLine("))");
            }

            builder.Append(openIndent).AppendLine("{");
            builder.Append(writeIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
            builder.Append(writeIndent).Append("_context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".Write(writer, ").Append(writeArgument).AppendLine(");");
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
        builder.Append("Options.DefaultIgnoreCondition == global::Tomlyn.TomlIgnoreCondition.WhenWritingDefault && ");
        if (defaultIsNull && canBeNull)
        {
            builder.Append(localName).Append(" is null");
        }
        else
        {
            builder.Append("global::System.Collections.Generic.EqualityComparer<")
                .Append(memberTypeName)
                .Append(">.Default.Equals(")
                .Append(localName)
                .Append(", ")
                .Append(defaultLiteral)
                .Append(")");
        }
        builder.AppendLine("))");
        builder.Append(openIndent).AppendLine("{");
        builder.Append(writeIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
        builder.Append(writeIndent).Append("_context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".Write(writer, ").Append(writeArgument).AppendLine(");");
        builder.Append(openIndent).AppendLine("}");
    }

    private static bool CanBeNull(ITypeSymbol type)
    {
        if (type.IsReferenceType)
        {
            return true;
        }

        return TryGetNullableUnderlyingType(type, out _);
    }

    private static bool TryGetNullableUnderlyingType(ITypeSymbol type, out ITypeSymbol underlyingType)
    {
        underlyingType = null!;

        if (type is not INamedTypeSymbol named ||
            !named.IsGenericType ||
            named.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::System.Nullable<T>")
        {
            return false;
        }

        underlyingType = named.TypeArguments[0];
        return true;
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

            if (TryGetNullableUnderlyingType(current, out var nullableUnderlyingType))
            {
                if (IsSupportedMemberType(nullableUnderlyingType))
                {
                    queue.Enqueue(nullableUnderlyingType);
                }

                continue;
            }

            if (TryGetArrayElementType(current, out var arrayElementType))
            {
                if (IsSupportedMemberType(arrayElementType))
                {
                    queue.Enqueue(arrayElementType);
                }

                continue;
            }

            if (TryGetSequenceElementType(current, out var enumerableElementType, out _))
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
        if (TryGetNullableUnderlyingType(type, out var nullableUnderlyingType))
        {
            return IsSupportedMemberType(nullableUnderlyingType);
        }

        // v1 generator milestone: built-in scalars/containers and POCOs.
        if (IsBuiltInType(type))
        {
            return true;
        }

        if (TryGetArrayElementType(type, out var arrayElementType))
        {
            return IsSupportedMemberType(arrayElementType);
        }

        if (TryGetSequenceElementType(type, out var enumerableElementType, out _))
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

        if (TryGetNullableUnderlyingType(type, out _))
        {
            return false;
        }

        if (IsBuiltInType(type))
        {
            return false;
        }

        if (TryGetArrayElementType(type, out _) ||
            TryGetSequenceElementType(type, out _, out _) ||
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

            var serializedName = GetSerializedName(member, member.Name, namingPolicy);
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

            var serializedName = GetSerializedName(member, member.Name, namingPolicy);
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

    private enum SequenceKind
    {
        List = 0,
        ListBackedEnumerable = 1,
        HashSet = 2,
        HashSetBackedEnumerable = 3,
        ImmutableArray = 4,
        ImmutableList = 5,
        ImmutableHashSet = 6,
    }

    private static bool TryGetSequenceElementType(ITypeSymbol type, out ITypeSymbol elementType, out SequenceKind kind)
    {
        elementType = null!;
        kind = default;

        if (type is not INamedTypeSymbol named || !named.IsGenericType)
        {
            return false;
        }

        var constructedFrom = named.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (constructedFrom == "global::System.Collections.Generic.List<T>")
        {
            elementType = named.TypeArguments[0];
            kind = SequenceKind.List;
            return true;
        }

        if (constructedFrom == "global::System.Collections.Generic.HashSet<T>")
        {
            elementType = named.TypeArguments[0];
            kind = SequenceKind.HashSet;
            return true;
        }

        if (constructedFrom == "global::System.Collections.Generic.ISet<T>")
        {
            elementType = named.TypeArguments[0];
            kind = SequenceKind.HashSetBackedEnumerable;
            return true;
        }

        if (constructedFrom == "global::System.Collections.Immutable.ImmutableArray<T>")
        {
            elementType = named.TypeArguments[0];
            kind = SequenceKind.ImmutableArray;
            return true;
        }

        if (constructedFrom == "global::System.Collections.Immutable.ImmutableList<T>")
        {
            elementType = named.TypeArguments[0];
            kind = SequenceKind.ImmutableList;
            return true;
        }

        if (constructedFrom == "global::System.Collections.Immutable.ImmutableHashSet<T>")
        {
            elementType = named.TypeArguments[0];
            kind = SequenceKind.ImmutableHashSet;
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
            kind = SequenceKind.ListBackedEnumerable;
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

    private static bool ImplementsInterface(ITypeSymbol type, string interfaceMetadataName)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        foreach (var iface in named.AllInterfaces)
        {
            if (iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + interfaceMetadataName)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSerializedName(ISymbol member, string memberName, string? namingPolicyExpression)
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

        if (namingPolicyExpression is not null && TryConvertKnownName(memberName, namingPolicyExpression, out var converted))
        {
            return converted;
        }

        return memberName;
    }

    private static bool TryConvertKnownName(string name, string namingPolicyExpression, out string converted)
    {
        JsonNamingPolicy? policy = namingPolicyExpression switch
        {
            "global::System.Text.Json.JsonNamingPolicy.CamelCase" => JsonNamingPolicy.CamelCase,
            "global::System.Text.Json.JsonNamingPolicy.SnakeCaseLower" => JsonNamingPolicy.SnakeCaseLower,
            "global::System.Text.Json.JsonNamingPolicy.SnakeCaseUpper" => JsonNamingPolicy.SnakeCaseUpper,
            "global::System.Text.Json.JsonNamingPolicy.KebabCaseLower" => JsonNamingPolicy.KebabCaseLower,
            "global::System.Text.Json.JsonNamingPolicy.KebabCaseUpper" => JsonNamingPolicy.KebabCaseUpper,
            _ => null,
        };

        if (policy is null)
        {
            converted = string.Empty;
            return false;
        }

        converted = policy.ConvertName(name);
        return true;
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

    private static bool IsBuiltInType(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Char:
            case SpecialType.System_String:
            case SpecialType.System_Boolean:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_DateTime:
            case SpecialType.System_Object:
                return true;
        }

        var metadataName = type is INamedTypeSymbol named
            ? named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return metadataName is
            "global::System.DateTimeOffset" or
            "global::System.Guid" or
            "global::System.TimeSpan" or
            "global::System.Uri" or
            "global::System.Version" or
            "global::System.Half" or
            "global::System.Int128" or
            "global::System.UInt128" or
            "global::Tomlyn.TomlDateTime" or
            "global::System.DateOnly" or
            "global::System.TimeOnly" or
            "global::Tomlyn.Model.TomlTable" or
            "global::Tomlyn.Model.TomlArray" or
            "global::Tomlyn.Model.TomlTableArray";
    }

    private static string GetTypeInfoPropertyName(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            var suffix = array.Rank == 1
                ? "Array"
                : "Array" + array.Rank.ToString(CultureInfo.InvariantCulture);

            return SanitizeIdentifier(GetTypeInfoPropertyName(array.ElementType) + suffix);
        }

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
                case "NewLine":
                    if (value.Value is int nl) options.NewLine = nl;
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
                case "DefaultIgnoreCondition":
                    if (value.Value is int dic) options.DefaultIgnoreCondition = dic;
                    break;
                case "DuplicateKeyHandling":
                    if (value.Value is int dkh) options.DuplicateKeyHandling = dkh;
                    break;
                case "MappingOrder":
                    if (value.Value is int mappingOrder) options.MappingOrder = mappingOrder;
                    break;
                case "DottedKeyHandling":
                    if (value.Value is int dottedKeyHandling) options.DottedKeyHandling = dottedKeyHandling;
                    break;
                case "RootValueHandling":
                    if (value.Value is int rootValueHandling) options.RootValueHandling = rootValueHandling;
                    break;
                case "RootValueKeyName":
                    if (value.Value is string rootValueKeyName) options.RootValueKeyName = rootValueKeyName;
                    break;
                case "InlineTablePolicy":
                    if (value.Value is int inlineTablePolicy) options.InlineTablePolicy = inlineTablePolicy;
                    break;
                case "TableArrayStyle":
                    if (value.Value is int tableArrayStyle) options.TableArrayStyle = tableArrayStyle;
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

            if (named.TypeKind != TypeKind.Class)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidConverterType, model.ContextSymbol.Locations.FirstOrDefault(), type.ToDisplayString(), "Converters must be classes."));
                continue;
            }

            if (named.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidConverterType, model.ContextSymbol.Locations.FirstOrDefault(), type.ToDisplayString(), "Converters must not be abstract."));
                continue;
            }

            if (!DerivesFrom(named, "global::" + TomlConverterMetadataName))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidConverterType, model.ContextSymbol.Locations.FirstOrDefault(), type.ToDisplayString(), $"Converters must derive from {TomlConverterMetadataName}."));
                continue;
            }

            if (!named.Constructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidConverterType, model.ContextSymbol.Locations.FirstOrDefault(), type.ToDisplayString(), "Converters must have a public parameterless constructor."));
            }
        }
    }

    private static void ValidateSourceGenerationOptions(SourceProductionContext context, ContextModel model)
    {
        if (model.Options.IndentSize is not null && model.Options.IndentSize.Value < 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidSourceGenerationOption,
                model.ContextSymbol.Locations.FirstOrDefault(),
                model.ContextSymbol.ToDisplayString(),
                "IndentSize must be at least 1."));
            model.Options.IndentSize = null;
        }

        ValidateEnumOption(context, model, "NewLine", model.Options.NewLine, TryGetTomlNewLineKindExpression, v => model.Options.NewLine = v);
        ValidateEnumOption(context, model, "DefaultIgnoreCondition", model.Options.DefaultIgnoreCondition, TryGetTomlIgnoreConditionExpression, v => model.Options.DefaultIgnoreCondition = v);
        ValidateEnumOption(context, model, "DuplicateKeyHandling", model.Options.DuplicateKeyHandling, TryGetTomlDuplicateKeyHandlingExpression, v => model.Options.DuplicateKeyHandling = v);
        ValidateEnumOption(context, model, "MappingOrder", model.Options.MappingOrder, TryGetTomlMappingOrderPolicyExpression, v => model.Options.MappingOrder = v);
        ValidateEnumOption(context, model, "DottedKeyHandling", model.Options.DottedKeyHandling, TryGetTomlDottedKeyHandlingExpression, v => model.Options.DottedKeyHandling = v);
        ValidateEnumOption(context, model, "RootValueHandling", model.Options.RootValueHandling, TryGetTomlRootValueHandlingExpression, v => model.Options.RootValueHandling = v);
        ValidateEnumOption(context, model, "InlineTablePolicy", model.Options.InlineTablePolicy, TryGetTomlInlineTablePolicyExpression, v => model.Options.InlineTablePolicy = v);
        ValidateEnumOption(context, model, "TableArrayStyle", model.Options.TableArrayStyle, TryGetTomlTableArrayStyleExpression, v => model.Options.TableArrayStyle = v);

        if (model.Options.RootValueKeyName is not null && string.IsNullOrWhiteSpace(model.Options.RootValueKeyName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidSourceGenerationOption,
                model.ContextSymbol.Locations.FirstOrDefault(),
                model.ContextSymbol.ToDisplayString(),
                "RootValueKeyName cannot be empty or whitespace."));
            model.Options.RootValueKeyName = null;
        }
    }

    private delegate bool TryGetEnumExpression(int value, out string expression);

    private static void ValidateEnumOption(
        SourceProductionContext context,
        ContextModel model,
        string optionName,
        int? value,
        TryGetEnumExpression tryGetExpression,
        Action<int?> clear)
    {
        if (value is null)
        {
            return;
        }

        if (tryGetExpression(value.Value, out _))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            InvalidSourceGenerationOption,
            model.ContextSymbol.Locations.FirstOrDefault(),
            model.ContextSymbol.ToDisplayString(),
            $"{optionName} has unsupported value {(value.Value).ToString(CultureInfo.InvariantCulture)}."));
        clear(null);
    }

    private static bool DerivesFrom(INamedTypeSymbol symbol, string baseTypeMetadataName)
    {
        for (var current = symbol.BaseType; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == baseTypeMetadataName)
            {
                return true;
            }
        }

        return false;
    }

    private static string? ToJsonNamingPolicyExpression(TypedConstant constant)
    {
        if (constant.Kind != TypedConstantKind.Enum || constant.Value is not int value)
        {
            return null;
        }

        var policy = (JsonKnownNamingPolicy)value;
        return policy switch
        {
            JsonKnownNamingPolicy.CamelCase => "global::System.Text.Json.JsonNamingPolicy.CamelCase",
            JsonKnownNamingPolicy.SnakeCaseLower => "global::System.Text.Json.JsonNamingPolicy.SnakeCaseLower",
            JsonKnownNamingPolicy.SnakeCaseUpper => "global::System.Text.Json.JsonNamingPolicy.SnakeCaseUpper",
            JsonKnownNamingPolicy.KebabCaseLower => "global::System.Text.Json.JsonNamingPolicy.KebabCaseLower",
            JsonKnownNamingPolicy.KebabCaseUpper => "global::System.Text.Json.JsonNamingPolicy.KebabCaseUpper",
            _ => null,
        };
    }

    private static bool TryGetTomlIgnoreConditionExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlIgnoreCondition.Never",
            1 => "global::Tomlyn.TomlIgnoreCondition.WhenWritingNull",
            2 => "global::Tomlyn.TomlIgnoreCondition.WhenWritingDefault",
            _ => null!,
        };

        return expression is not null;
    }

    private static bool TryGetTomlDuplicateKeyHandlingExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlDuplicateKeyHandling.Error",
            1 => "global::Tomlyn.TomlDuplicateKeyHandling.LastWins",
            _ => null!,
        };

        return expression is not null;
    }

    private static bool TryGetTomlMappingOrderPolicyExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlMappingOrderPolicy.Declaration",
            1 => "global::Tomlyn.TomlMappingOrderPolicy.Alphabetical",
            2 => "global::Tomlyn.TomlMappingOrderPolicy.OrderThenDeclaration",
            3 => "global::Tomlyn.TomlMappingOrderPolicy.OrderThenAlphabetical",
            _ => null!,
        };

        return expression is not null;
    }

    private static bool TryGetTomlDottedKeyHandlingExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlDottedKeyHandling.Literal",
            1 => "global::Tomlyn.TomlDottedKeyHandling.Expand",
            _ => null!,
        };

        return expression is not null;
    }

    private static bool TryGetTomlRootValueHandlingExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlRootValueHandling.Error",
            1 => "global::Tomlyn.TomlRootValueHandling.WrapInRootKey",
            _ => null!,
        };

        return expression is not null;
    }

    private static bool TryGetTomlNewLineKindExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlNewLineKind.Lf",
            1 => "global::Tomlyn.TomlNewLineKind.CrLf",
            _ => null!,
        };

        return expression is not null;
    }

    private static bool TryGetTomlInlineTablePolicyExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlInlineTablePolicy.Never",
            1 => "global::Tomlyn.TomlInlineTablePolicy.WhenSmall",
            2 => "global::Tomlyn.TomlInlineTablePolicy.Always",
            _ => null!,
        };

        return expression is not null;
    }

    private static bool TryGetTomlTableArrayStyleExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::Tomlyn.TomlTableArrayStyle.Headers",
            1 => "global::Tomlyn.TomlTableArrayStyle.InlineArrayOfTables",
            _ => null!,
        };

        return expression is not null;
    }

    private static string EscapeStringLiteral(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static ulong ComputePropertyNameHash56(string value)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        const ulong mask = 0x00FF_FFFF_FFFF_FFFFUL;

        var hash = offsetBasis;
        for (var i = 0; i < value.Length; i++)
        {
            var c1 = value[i];
            var codePoint = (int)c1;
            if (char.IsHighSurrogate(c1) && i + 1 < value.Length)
            {
                var c2 = value[i + 1];
                if (char.IsLowSurrogate(c2))
                {
                    codePoint = char.ConvertToUtf32(c1, c2);
                    i++;
                }
            }

            hash ^= unchecked((uint)codePoint);
            hash *= prime;
        }

        return hash & mask;
    }
}
