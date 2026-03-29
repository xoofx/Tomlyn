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
        messageFormat: "Type '{0}' contains member '{1}' of unsupported type '{2}'. Add [TomlSerializable(typeof({2}))] to the context or change the member type.",
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

    private static readonly DiagnosticDescriptor InvalidExtensionDataMember = new(
        id: "TOMLYN006",
        title: "Invalid extension data member",
        messageFormat: "Type '{0}' extension data member '{1}' is invalid: {2}",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidPolymorphismConfiguration = new(
        id: "TOMLYN007",
        title: "Invalid polymorphism configuration",
        messageFormat: "Type '{0}' polymorphism configuration is invalid: {1}",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor JsonSerializableNotSupported = new(
        id: "TOMLYN008",
        title: "JsonSerializable is not supported on TOML contexts",
        messageFormat: "Type '{0}' derives from Tomlyn.Serialization.TomlSerializerContext and uses [JsonSerializable]. Replace [JsonSerializable(...)] with [TomlSerializable(...)] on the context.",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidDerivedTypeMapping = new(
        id: "TOMLYN009",
        title: "Invalid derived type mapping",
        messageFormat: "Context '{0}' derived type mapping is invalid: {1}",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingPolymorphicConfigurationOnDerivedTypeMappingBase = new(
        id: "TOMLYN010",
        title: "Derived type mapping base type has no polymorphic configuration",
        messageFormat: "Context '{0}' registers a derived type mapping for base type '{1}' without [TomlPolymorphic], [TomlDerivedType], [JsonPolymorphic], or [JsonDerivedType]. Serializer options defaults will be used.",
        category: "Tomlyn.SourceGeneration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private const string TomlSerializerContextMetadataName = "Tomlyn.Serialization.TomlSerializerContext";
    private const string TomlSerializableAttributeMetadataName = "Tomlyn.Serialization.TomlSerializableAttribute";
    private const string TomlDerivedTypeMappingAttributeMetadataName = "Tomlyn.Serialization.TomlDerivedTypeMappingAttribute";
    private const string JsonSerializableAttributeMetadataName = "System.Text.Json.Serialization.JsonSerializableAttribute";
    private const string JsonSourceGenerationOptionsAttributeMetadataName = "System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute";
    private const string JsonObjectCreationHandlingAttributeMetadataName = "System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute";
    private const string TomlSourceGenerationOptionsAttributeMetadataName = "Tomlyn.Serialization.TomlSourceGenerationOptionsAttribute";
    private const string TomlConverterMetadataName = "Tomlyn.Serialization.TomlConverter";
    private const string TomlSingleOrArrayAttributeMetadataName = "Tomlyn.Serialization.TomlSingleOrArrayAttribute";
    private const string TomlOnSerializingMetadataName = "Tomlyn.Serialization.ITomlOnSerializing";
    private const string TomlOnSerializedMetadataName = "Tomlyn.Serialization.ITomlOnSerialized";
    private const string TomlOnDeserializingMetadataName = "Tomlyn.Serialization.ITomlOnDeserializing";
    private const string TomlOnDeserializedMetadataName = "Tomlyn.Serialization.ITomlOnDeserialized";
    private const string SetsRequiredMembersAttributeMetadataName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute";

    private static readonly SymbolDisplayFormat FullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private sealed class ContextModel
    {
        public ContextModel(
            INamedTypeSymbol contextSymbol,
            string namespaceName,
            string typeName,
            ImmutableArray<RootTypeModel> rootTypes,
            ImmutableArray<DerivedTypeMappingModel> derivedTypeMappings,
            SourceGenOptions options,
            bool isValid,
            bool usesJsonSerializable)
        {
            ContextSymbol = contextSymbol;
            NamespaceName = namespaceName;
            TypeName = typeName;
            RootTypes = rootTypes;
            DerivedTypeMappings = derivedTypeMappings;
            Options = options;
            IsValid = isValid;
            UsesJsonSerializable = usesJsonSerializable;
        }

        public INamedTypeSymbol ContextSymbol { get; }
        public string NamespaceName { get; }
        public string TypeName { get; }
        public ImmutableArray<RootTypeModel> RootTypes { get; }
        public ImmutableArray<DerivedTypeMappingModel> DerivedTypeMappings { get; }
        public SourceGenOptions Options { get; }
        public bool IsValid { get; }
        public bool UsesJsonSerializable { get; }
    }

    private sealed class RootTypeModel
    {
        public RootTypeModel(ITypeSymbol type, string? typeInfoPropertyName)
        {
            Type = type;
            TypeInfoPropertyName = typeInfoPropertyName;
        }

        public ITypeSymbol Type { get; }
        public string? TypeInfoPropertyName { get; }
    }

    private sealed class DerivedTypeMappingModel
    {
        public DerivedTypeMappingModel(ITypeSymbol baseType, ITypeSymbol derivedType, string? discriminator, Location? location)
        {
            BaseType = baseType;
            DerivedType = derivedType;
            Discriminator = discriminator;
            Location = location;
        }

        public ITypeSymbol BaseType { get; }
        public ITypeSymbol DerivedType { get; }
        public string? Discriminator { get; }
        public Location? Location { get; }
    }

    private sealed class SourceGenOptions
    {
        public bool? WriteIndented { get; set; }
        public int? IndentSize { get; set; }
        public int? NewLine { get; set; }
        public string? PropertyNamingPolicyExpression { get; set; }
        public string? DictionaryKeyPolicyExpression { get; set; }
        public int? PreferredObjectCreationHandling { get; set; }
        public bool? PropertyNameCaseInsensitive { get; set; }
        public int? DefaultIgnoreCondition { get; set; }
        public int? DuplicateKeyHandling { get; set; }
        public int? MaxDepth { get; set; }
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

        var roots = ImmutableArray.CreateBuilder<RootTypeModel>();
        var derivedTypeMappings = ImmutableArray.CreateBuilder<DerivedTypeMappingModel>();
        var options = new SourceGenOptions();
        var usesJsonSerializable = false;

        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (IsTomlSerializableAttribute(attribute))
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

                roots.Add(new RootTypeModel(typeSymbol, GetTypeInfoPropertyNameOverride(attribute)));
                continue;
            }

            if (TryCreateDerivedTypeMappingModel(attribute, out var derivedTypeMapping))
            {
                derivedTypeMappings.Add(derivedTypeMapping);
                continue;
            }

            if (IsJsonSerializableAttribute(attribute))
            {
                usesJsonSerializable = true;
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

        if (roots.Count == 0 && !usesJsonSerializable)
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
            derivedTypeMappings.ToImmutable(),
            options,
            isValid: isPartial,
            usesJsonSerializable: usesJsonSerializable);
    }

    private static void EmitContext(SourceProductionContext context, Compilation compilation, ContextModel model)
    {
        if (model.UsesJsonSerializable)
        {
            context.ReportDiagnostic(Diagnostic.Create(JsonSerializableNotSupported, model.ContextSymbol.Locations.FirstOrDefault(), model.ContextSymbol.ToDisplayString()));
        }

        if (model.RootTypes.Length == 0)
        {
            return;
        }

        if (!model.IsValid)
        {
            context.ReportDiagnostic(Diagnostic.Create(ContextMustBePartial, model.ContextSymbol.Locations.FirstOrDefault(), model.ContextSymbol.ToDisplayString()));
            return;
        }

        ValidateConverters(context, model);
        ValidateSourceGenerationOptions(context, model);
        if (!ValidateRootAttributes(context, model))
        {
            return;
        }

        var derivedTypeMappings = ValidateDerivedTypeMappings(context, model);
        var expanded = ExpandTypeGraph(context, model, derivedTypeMappings);
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
            EmitTypeInfoProperty(builder, context, model, derivedTypeMappings, type);
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
            if (TryGetPocoShape(context, model, type, out var poco))
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

        if (model.Options.PreferredObjectCreationHandling is not null && TryGetJsonObjectCreationHandlingExpression(model.Options.PreferredObjectCreationHandling.Value, out var objectCreationHandlingExpression))
        {
            builder.Append("        options = options with { PreferredObjectCreationHandling = ").Append(objectCreationHandlingExpression).AppendLine(" };");
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

        if (model.Options.MaxDepth is not null)
        {
            builder.Append("        options = options with { MaxDepth = ").Append(model.Options.MaxDepth.Value.ToString(CultureInfo.InvariantCulture)).AppendLine(" };");
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

    private static void EmitTypeInfoProperty(
        StringBuilder builder,
        SourceProductionContext context,
        ContextModel model,
        ImmutableArray<DerivedTypeMappingModel> derivedTypeMappings,
        ITypeSymbol type)
    {
        var propertyName = GetTypeInfoPropertyName(type);
        var publicPropertyName = TryGetCustomTypeInfoPropertyName(model, type, out var customPropertyName)
            ? customPropertyName
            : propertyName;
        var propertyAccessibility = string.Equals(publicPropertyName, propertyName, StringComparison.Ordinal) ? "public" : "private";
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        builder.Append("    private TomlTypeInfo<").Append(typeName).Append(">? _").Append(propertyName).AppendLine(";");
        builder.Append("    ").Append(propertyAccessibility).Append(" TomlTypeInfo<").Append(typeName).Append("> ").Append(propertyName).AppendLine();
        builder.Append("        => _").Append(propertyName).Append(" ??= Create").Append(propertyName).AppendLine("();");
        builder.AppendLine();

        if (!string.Equals(publicPropertyName, propertyName, StringComparison.Ordinal))
        {
            builder.Append("    public TomlTypeInfo<").Append(typeName).Append("> ").Append(publicPropertyName).Append(" => ").Append(propertyName).AppendLine(";");
            builder.AppendLine();
        }

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
        else if (TryGetPolymorphicShape(context, model, derivedTypeMappings, type, out var polymorphic, reportDiagnostics: false))
        {
            builder.Append("        global::Tomlyn.TomlTypeInfo<").Append(typeName).AppendLine(">? __baseTypeInfo = null;");
            if (TryGetPocoShape(context, model, type, out _))
            {
                builder.Append("        __baseTypeInfo = new __TomlTypeInfo_").Append(propertyName).AppendLine("(this);");
            }

            builder.AppendLine("        var __derivedTypeInfoByDiscriminator = new global::System.Collections.Generic.Dictionary<string, global::Tomlyn.TomlTypeInfo>(global::System.StringComparer.Ordinal)");
            builder.AppendLine("        {");
            foreach (var derived in polymorphic.DerivedTypes)
            {
                if (derived.Discriminator is null) continue; // skip default derived type in dictionary
                builder.Append("            [\"").Append(EscapeStringLiteral(derived.Discriminator)).Append("\"] = ").Append(GetTypeInfoPropertyName(derived.Type)).AppendLine(",");
            }
            builder.AppendLine("        };");

            var discriminatorExpression = polymorphic.DiscriminatorPropertyName is null
                ? "null"
                : "\"" + EscapeStringLiteral(polymorphic.DiscriminatorPropertyName) + "\"";

            var defaultDerivedTypeExpression = polymorphic.DefaultDerivedType is not null
                ? GetTypeInfoPropertyName(polymorphic.DefaultDerivedType)
                : "null";

            var unknownHandlingExpression = polymorphic.UnknownDerivedTypeHandlingOverride is { } handlingValue
                ? $"(global::Tomlyn.TomlUnknownDerivedTypeHandling){handlingValue}"
                : "null";

            builder.Append("        return new global::Tomlyn.Serialization.TomlPolymorphicTypeInfo<")
                .Append(typeName)
                .Append(">(Options, __baseTypeInfo, ")
                .Append(discriminatorExpression)
                .Append(", __derivedTypeInfoByDiscriminator, ")
                .Append(defaultDerivedTypeExpression)
                .Append(", ")
                .Append(unknownHandlingExpression)
                .AppendLine(");");
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
        if (!type.IsValueType)
        {
            builder.AppendLine("            if (value is null) throw new TomlException(\"TOML does not support null values.\");");
        }
        if (callsOnSerializing)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnSerializing)value).OnTomlSerializing();");
        }
        builder.AppendLine("            writer.WriteStartTable();");
        string? usedKeysVariable = null;
        if (poco.ExtensionData is { } extensionData)
        {
            usedKeysVariable = "__usedKeys";
            builder.Append("            var __extensionData = value.").Append(extensionData.MemberName).AppendLine(";");
            builder.AppendLine("            global::System.Collections.Generic.HashSet<string>? __usedKeys = null;");
            builder.AppendLine("            if (__extensionData is not null)");
            builder.AppendLine("            {");
            builder.AppendLine("                __usedKeys = new global::System.Collections.Generic.HashSet<string>(Options.PropertyNameCaseInsensitive ? global::System.StringComparer.OrdinalIgnoreCase : global::System.StringComparer.Ordinal);");
            builder.AppendLine("            }");
        }
        for (var i = 0; i < poco.Members.Length; i++)
        {
            var member = poco.Members[i];
            builder.Append("            var __member").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = value.").Append(member.MemberName).AppendLine(";");
        }

        var declarationOrder = Enumerable.Range(0, poco.Members.Length).ToArray();
        var alphabeticalOrder = declarationOrder
            .OrderBy(i => poco.Members[i].SerializedName, StringComparer.Ordinal)
            .ToArray();
        var orderThenDeclaration = declarationOrder
            .OrderBy(i => poco.Members[i].Order)
            .ThenBy(i => i)
            .ToArray();
        var orderThenAlphabetical = declarationOrder
            .OrderBy(i => poco.Members[i].Order)
            .ThenBy(i => poco.Members[i].SerializedName, StringComparer.Ordinal)
            .ToArray();

        static void EmitWriteSequence(StringBuilder builder, PocoShape poco, int[] indices, string? usedKeysVariable)
        {
            for (var sequenceIndex = 0; sequenceIndex < indices.Length; sequenceIndex++)
            {
                var i = indices[sequenceIndex];
                var member = poco.Members[i];
                var memberTypeName = member.Type.ToDisplayString(FullyQualifiedNullableFormat);
                var canBeNull = CanBeNull(member.Type);
                EmitWriteMember(builder, member, i, memberTypeName, canBeNull, usedKeysVariable);
            }
        }

        builder.AppendLine("            switch (Options.MappingOrder)");
        builder.AppendLine("            {");
        builder.AppendLine("                case TomlMappingOrderPolicy.Declaration:");
        EmitWriteSequence(builder, poco, declarationOrder, usedKeysVariable);
        builder.AppendLine("                    break;");
        builder.AppendLine("                case TomlMappingOrderPolicy.Alphabetical:");
        EmitWriteSequence(builder, poco, alphabeticalOrder, usedKeysVariable);
        builder.AppendLine("                    break;");
        builder.AppendLine("                case TomlMappingOrderPolicy.OrderThenDeclaration:");
        EmitWriteSequence(builder, poco, orderThenDeclaration, usedKeysVariable);
        builder.AppendLine("                    break;");
        builder.AppendLine("                case TomlMappingOrderPolicy.OrderThenAlphabetical:");
        EmitWriteSequence(builder, poco, orderThenAlphabetical, usedKeysVariable);
        builder.AppendLine("                    break;");
        builder.AppendLine("                default:");
        EmitWriteSequence(builder, poco, declarationOrder, usedKeysVariable);
        builder.AppendLine("                    break;");
        builder.AppendLine("            }");
        if (poco.ExtensionData is { } extensionDataWrite)
        {
            var extensionValueTypeInfo = GetTypeInfoPropertyName(extensionDataWrite.ValueType);
            var writeValueArgument = CanBeNull(extensionDataWrite.ValueType) ? "__pair.Value!" : "__pair.Value";

            builder.AppendLine("            if (__extensionData is not null)");
            builder.AppendLine("            {");
            builder.AppendLine("                foreach (var __pair in __extensionData)");
            builder.AppendLine("                {");
            builder.AppendLine("                    var __key = __pair.Key;");
            builder.AppendLine("                    if (Options.DictionaryKeyPolicy is { } __keyPolicy)");
            builder.AppendLine("                    {");
            builder.AppendLine("                        __key = __keyPolicy.ConvertName(__key);");
            builder.AppendLine("                    }");
            builder.AppendLine("                    if (__usedKeys is not null && __usedKeys.Contains(__key)) throw new TomlException($\"Extension data key '{__key}' conflicts with an existing member key.\");");
            builder.AppendLine("                    writer.WritePropertyName(__key);");
            builder.Append("                    _context.").Append(extensionValueTypeInfo).Append(".Write(writer, ").Append(writeValueArgument).AppendLine(");");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
        }
        builder.AppendLine("            writer.WriteEndTable();");
        if (callsOnSerialized)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnSerialized)value).OnTomlSerialized();");
        }
        builder.AppendLine("        }");
        builder.AppendLine();

        var constructor = poco.Constructor;
        if (constructor is { ErrorMessage: not null } ctorError)
        {
            builder.Append("        public override ").Append(readReturnType).AppendLine(" Read(TomlReader reader)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (reader.TokenType != TomlTokenType.StartTable) throw reader.CreateException($\"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.\");");
            builder.Append("            throw new TomlException(\"").Append(EscapeStringLiteral(ctorError.ErrorMessage!)).AppendLine("\");");
            builder.AppendLine("        }");
        }
        else if (poco.RequiresGeneratedObjectInitializer)
        {
            var bufferedConstructor = constructor ?? new PocoConstructor(null, ImmutableArray<PocoConstructorParameter>.Empty, null, setsRequiredMembers: false);
            EmitPocoReadWithConstructor(builder, model, type, poco, bufferedConstructor, readReturnType, callsOnDeserializing, callsOnDeserialized, useObjectInitializerConstruction: true);
            if (!poco.Members.Any(static member => member.IsInitOnly))
            {
                EmitPocoReadIntoExisting(builder, typeName, poco, callsOnDeserializing, callsOnDeserialized);
            }
        }
        else if (constructor is { IsValid: true } ctor && !ctor.Parameters.IsDefaultOrEmpty)
        {
            EmitPocoReadWithConstructor(builder, model, type, poco, ctor, readReturnType, callsOnDeserializing, callsOnDeserialized, useObjectInitializerConstruction: false);
            if (!poco.Members.Any(static member => member.IsInitOnly))
            {
                EmitPocoReadIntoExisting(builder, typeName, poco, callsOnDeserializing, callsOnDeserialized);
            }
        }
        else
        {
            builder.Append("        public override ").Append(readReturnType).AppendLine(" Read(TomlReader reader)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (reader.TokenType != TomlTokenType.StartTable) throw reader.CreateException($\"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.\");");
            builder.AppendLine("            var tableStartSpan = reader.CurrentSpan;");
            builder.Append("            var value = new ").Append(typeName).AppendLine("();");
            if (callsOnDeserializing)
            {
                builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserializing)value).OnTomlDeserializing();");
            }
            var hasRequiredMembers = poco.Members.Any(static m => m.IsRequired);
            if (poco.Members.Length <= 64)
            {
                if (poco.Members.Length > 0)
                {
                    builder.AppendLine("            ulong seenMask = 0;");
                }
                if (hasRequiredMembers)
                {
                    ulong requiredMask = 0;
                    for (var i = 0; i < poco.Members.Length; i++)
                    {
                        if (poco.Members[i].IsRequired)
                        {
                            requiredMask |= 1UL << i;
                        }
                    }

                    builder.Append("            const ulong requiredMask = ").Append(requiredMask.ToString(CultureInfo.InvariantCulture)).AppendLine("UL;");
                }
            }
            else
            {
                builder.Append("            var seen = (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error");
                if (hasRequiredMembers)
                {
                    builder.Append(" || true");
                }
                builder.Append(") ? new bool[").Append(poco.Members.Length.ToString(CultureInfo.InvariantCulture)).AppendLine("] : null;");
            }
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
            builder.AppendLine("            var endTableSpan = reader.CurrentSpan;");
            builder.AppendLine("            reader.Read();");
            if (hasRequiredMembers)
            {
                if (poco.Members.Length <= 64)
                {
                    builder.AppendLine("            if (requiredMask != 0 && (seenMask & requiredMask) != requiredMask)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                var span = endTableSpan ?? tableStartSpan;");
                    for (var i = 0; i < poco.Members.Length; i++)
                    {
                        if (!poco.Members[i].IsRequired)
                        {
                            continue;
                        }

                        var serializedName = EscapeStringLiteral(poco.Members[i].SerializedName);
                        builder.Append("                if ((seenMask & (1UL << ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(")) == 0)");
                        builder.AppendLine("                {");
                        builder.Append("                    throw span is { } locatedSpan ? new TomlException(locatedSpan, $\"Missing required TOML key '")
                            .Append(serializedName)
                            .Append("' when deserializing '{typeof(")
                            .Append(typeName)
                            .Append(").FullName}'.\") : new TomlException($\"Missing required TOML key '")
                            .Append(serializedName)
                            .Append("' when deserializing '{typeof(")
                            .Append(typeName)
                            .Append(").FullName}'.\");");
                        builder.AppendLine("                }");
                    }
                    builder.AppendLine("            }");
                }
                else
                {
                    builder.AppendLine("            if (seen is not null)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                var span = endTableSpan ?? tableStartSpan;");
                    for (var i = 0; i < poco.Members.Length; i++)
                    {
                        if (!poco.Members[i].IsRequired)
                        {
                            continue;
                        }

                        var serializedName = EscapeStringLiteral(poco.Members[i].SerializedName);
                        builder.Append("                if (!seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                        builder.AppendLine("                {");
                        builder.Append("                    throw span is { } locatedSpan ? new TomlException(locatedSpan, $\"Missing required TOML key '")
                            .Append(serializedName)
                            .Append("' when deserializing '{typeof(")
                            .Append(typeName)
                            .Append(").FullName}'.\") : new TomlException($\"Missing required TOML key '")
                            .Append(serializedName)
                            .Append("' when deserializing '{typeof(")
                            .Append(typeName)
                            .Append(").FullName}'.\");");
                        builder.AppendLine("                }");
                    }
                    builder.AppendLine("            }");
                }
            }
            if (callsOnDeserialized)
            {
                builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserialized)value).OnTomlDeserialized();");
            }
            builder.AppendLine("            return value;");
            builder.AppendLine("        }");
            EmitPocoReadIntoExisting(builder, typeName, poco, callsOnDeserializing, callsOnDeserialized);
        }

        builder.AppendLine("    }");
    }

    private static void EmitPocoReadWithConstructor(
        StringBuilder builder,
        ContextModel model,
        ITypeSymbol type,
        PocoShape poco,
        PocoConstructor ctor,
        string readReturnType,
        bool callsOnDeserializing,
        bool callsOnDeserialized,
        bool useObjectInitializerConstruction)
    {
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var readNonNullableTypeName = typeName;
        var hasRequiredMembers = poco.Members.Any(static m => m.IsRequired);
        var useSeenMask = poco.Members.Length <= 64;
        var wrapConstructionErrors = !ctor.Parameters.IsDefaultOrEmpty;

        string? GetLinkedParameterExpression(int memberIndex)
        {
            for (var i = 0; i < ctor.Parameters.Length; i++)
            {
                if (ctor.Parameters[i].LinkedMemberIndex == memberIndex)
                {
                    return "__arg" + i.ToString(CultureInfo.InvariantCulture);
                }
            }

            return null;
        }

        builder.Append("        public override ").Append(readReturnType).AppendLine(" Read(TomlReader reader)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (reader.TokenType != TomlTokenType.StartTable) throw reader.CreateException($\"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.\");");
        builder.AppendLine("            var tableStartSpan = reader.CurrentSpan;");

        // Constructor argument locals.
        for (var i = 0; i < ctor.Parameters.Length; i++)
        {
            var parameter = ctor.Parameters[i];
            var parameterTypeName = parameter.ParameterType.ToDisplayString(FullyQualifiedNullableFormat);
            var defaultLiteral = GetDefaultLiteral(parameter.ParameterType);
            builder.Append("            ").Append(parameterTypeName).Append(" __arg").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = ").Append(defaultLiteral).AppendLine(";");
            builder.Append("            bool __argSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(" = false;");
        }

        // Deferred member assignment locals.
        for (var i = 0; i < poco.Members.Length; i++)
        {
            var member = poco.Members[i];
            if (!member.CanSet)
            {
                continue;
            }

            var memberTypeName = member.Type.ToDisplayString(FullyQualifiedNullableFormat);
            var defaultLiteral = GetDefaultLiteral(member.Type);
            builder.Append("            ").Append(memberTypeName).Append(" __memberValue").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = ").Append(defaultLiteral).AppendLine(";");
            builder.Append("            bool __memberSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(" = false;");
        }

        PocoExtensionData? extensionData = poco.ExtensionData;
        if (extensionData is not null)
        {
            var extensionValueTypeName = extensionData.ValueType.ToDisplayString(FullyQualifiedNullableFormat);
            builder.Append("            global::System.Collections.Generic.Dictionary<string, ").Append(extensionValueTypeName).AppendLine(">? __extensionData = null;");
        }

        // Required/duplicate tracking for members.
        if (useSeenMask)
        {
            if (poco.Members.Length > 0)
            {
                builder.AppendLine("            ulong seenMask = 0;");
            }

            if (hasRequiredMembers)
            {
                ulong requiredMask = 0;
                for (var i = 0; i < poco.Members.Length; i++)
                {
                    if (poco.Members[i].IsRequired)
                    {
                        requiredMask |= 1UL << i;
                    }
                }

                builder.Append("            const ulong requiredMask = ").Append(requiredMask.ToString(CultureInfo.InvariantCulture)).AppendLine("UL;");
            }
        }
        else
        {
            builder.Append("            var seen = (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error");
            if (hasRequiredMembers)
            {
                builder.Append(" || true");
            }
            builder.Append(") ? new bool[").Append(poco.Members.Length.ToString(CultureInfo.InvariantCulture)).AppendLine("] : null;");
        }

        builder.AppendLine("            reader.Read();");
        builder.AppendLine("            while (reader.TokenType != TomlTokenType.EndTable)");
        builder.AppendLine("            {");
        builder.AppendLine("                if (reader.TokenType != TomlTokenType.PropertyName) throw reader.CreateException($\"Expected {TomlTokenType.PropertyName} token but was {reader.TokenType}.\");");
        builder.AppendLine("                if (Options.PropertyNameCaseInsensitive)");
        builder.AppendLine("                {");
        builder.AppendLine("                    var name = reader.PropertyName!;");
        builder.AppendLine("                    reader.Read();");

        var comparison = "StringComparison.OrdinalIgnoreCase";
        var wroteAnyCondition = false;

        // Parameters first.
        for (var i = 0; i < ctor.Parameters.Length; i++)
        {
            var parameter = ctor.Parameters[i];
            var prefix = wroteAnyCondition ? "else if" : "if";
            wroteAnyCondition = true;
            builder.Append("                    ").Append(prefix).Append("(string.Equals(name, \"").Append(EscapeStringLiteral(parameter.KeyName)).Append("\", ").Append(comparison).AppendLine("))");
            builder.AppendLine("                    {");
            builder.AppendLine("                        if (__argSeen" + i.ToString(CultureInfo.InvariantCulture) + " && Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
            builder.AppendLine("                        {");
            if (CanEmitTableHeaderExtension(parameter.ParameterType))
            {
                EmitRepeatedTableExtensionIntoTarget(builder, parameter.ParameterType, "__arg" + i.ToString(CultureInfo.InvariantCulture), "__arg" + i.ToString(CultureInfo.InvariantCulture), "                            ");
            }
            builder.AppendLine("                            throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
            builder.AppendLine("                        }");
            builder.Append("                        __argSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(" = true;");
            if (parameter.LinkedMemberIndex >= 0 && parameter.LinkedMemberIndex < poco.Members.Length && poco.Members[parameter.LinkedMemberIndex].HasSingleOrArray)
            {
                var linkedMember = poco.Members[parameter.LinkedMemberIndex];
                EmitSingleOrArrayReadAssignment(builder, parameter.ParameterType, "__arg" + i.ToString(CultureInfo.InvariantCulture), "                        ", "Member '" + EscapeStringLiteral(linkedMember.MemberName) + "' on '" + EscapeStringLiteral(typeName) + "' uses [TomlSingleOrArray]");
            }
            else
            {
                builder.Append("                        __arg").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = _context.").Append(GetTypeInfoPropertyName(parameter.ParameterType)).AppendLine(".Read(reader)!;");
            }

            if (parameter.LinkedMemberIndex >= 0 && parameter.LinkedMemberIndex < poco.Members.Length)
            {
                var linkedMember = poco.Members[parameter.LinkedMemberIndex];
                if (linkedMember.IsRequired)
                {
                    if (useSeenMask)
                    {
                        builder.Append("                        seenMask |= 1UL << ").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                    }
                    else
                    {
                        builder.Append("                        if (seen is not null) seen[").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                    }
                }

                if (linkedMember.CanSet && SymbolEqualityComparer.Default.Equals(linkedMember.Type, parameter.ParameterType))
                {
                    builder.Append("                        __memberValue").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).Append(" = __arg").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                    builder.Append("                        __memberSeen").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).AppendLine(" = true;");
                }
            }

            builder.AppendLine("                        continue;");
            builder.AppendLine("                    }");
        }

        // Members.
        for (var i = 0; i < poco.Members.Length; i++)
        {
            var member = poco.Members[i];
            var prefix = wroteAnyCondition ? "else if" : "if";
            wroteAnyCondition = true;
            builder.Append("                    ").Append(prefix).Append("(string.Equals(name, \"").Append(EscapeStringLiteral(member.SerializedName)).Append("\", ").Append(comparison).AppendLine("))");
            builder.AppendLine("                    {");

                if (useSeenMask)
                {
                    builder.Append("                        const ulong bit = 1UL << ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                    builder.AppendLine("                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                    builder.AppendLine("                        {");
                    builder.AppendLine("                            if ((seenMask & bit) != 0)");
                    builder.AppendLine("                            {");
                    var existingExpression = member.CanSet
                        ? "__memberValue" + i.ToString(CultureInfo.InvariantCulture)
                        : GetLinkedParameterExpression(i);
                    if (existingExpression is not null && CanEmitTableHeaderExtension(member.Type))
                    {
                        EmitRepeatedTableExtensionIntoTarget(builder, member.Type, existingExpression, existingExpression, "                                ");
                    }
                    builder.AppendLine("                                throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
                    builder.AppendLine("                            }");
                    builder.AppendLine("                        }");
                    if (member.IsRequired)
                    {
                        builder.AppendLine("                        seenMask |= bit;");
                    }
                else
                {
                    builder.AppendLine("                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error) seenMask |= bit;");
                }
            }
            else
            {
                builder.AppendLine("                        if (seen is not null)");
                builder.AppendLine("                        {");
                builder.AppendLine("                            if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                builder.AppendLine("                            {");
                builder.Append("                                if (seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                builder.AppendLine("                                {");
                var existingExpression = member.CanSet
                    ? "__memberValue" + i.ToString(CultureInfo.InvariantCulture)
                    : GetLinkedParameterExpression(i);
                if (existingExpression is not null && CanEmitTableHeaderExtension(member.Type))
                {
                    EmitRepeatedTableExtensionIntoTarget(builder, member.Type, existingExpression, existingExpression, "                                    ");
                }
                builder.AppendLine("                                    throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
                builder.AppendLine("                                }");
                builder.AppendLine("                            }");
                builder.Append("                            seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                builder.AppendLine("                        }");
            }

            if (!member.CanSet && !member.HasSingleOrArray)
            {
                builder.AppendLine("                        reader.Skip();");
                builder.AppendLine("                        continue;");
                builder.AppendLine("                    }");
                continue;
            }

            if (member.HasSingleOrArray)
            {
                EmitSingleOrArrayReadAssignment(builder, member.Type, "__memberValue" + i.ToString(CultureInfo.InvariantCulture), "                        ", "Member '" + EscapeStringLiteral(member.MemberName) + "' on '" + EscapeStringLiteral(typeName) + "' uses [TomlSingleOrArray]");
            }
            else
            {
                builder.Append("                        __memberValue").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = _context.").Append(GetTypeInfoPropertyName(member.Type)).AppendLine(".Read(reader)!;");
            }
            builder.Append("                        __memberSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(" = true;");
            builder.AppendLine("                        continue;");
            builder.AppendLine("                    }");
        }

        if (extensionData is not null)
        {
            builder.AppendLine("                    if (__extensionData is null) __extensionData = new();");
            builder.Append("                    __extensionData[name] = _context.").Append(GetTypeInfoPropertyName(extensionData.ValueType)).AppendLine(".Read(reader)!;");
            builder.AppendLine("                    continue;");
        }

        builder.AppendLine("                    reader.Skip();");
        builder.AppendLine("                }");
        builder.AppendLine("                else");
        builder.AppendLine("                {");

        // Case-sensitive: hash dispatch for parameters + members.
        var actionsByHash = new Dictionary<ulong, List<(bool IsParameter, int Index, string KeyName)>>();
        for (var i = 0; i < ctor.Parameters.Length; i++)
        {
            var keyName = ctor.Parameters[i].KeyName;
            var hash = ComputePropertyNameHash56(keyName);
            if (!actionsByHash.TryGetValue(hash, out var bucket))
            {
                bucket = new List<(bool, int, string)>(capacity: 1);
                actionsByHash.Add(hash, bucket);
            }

            bucket.Add((true, i, keyName));
        }

        for (var i = 0; i < poco.Members.Length; i++)
        {
            var keyName = poco.Members[i].SerializedName;
            var hash = ComputePropertyNameHash56(keyName);
            if (!actionsByHash.TryGetValue(hash, out var bucket))
            {
                bucket = new List<(bool, int, string)>(capacity: 1);
                actionsByHash.Add(hash, bucket);
            }

            bucket.Add((false, i, keyName));
        }

        builder.AppendLine("                    if (reader.TryGetPropertyNameHash(out var hash))");
        builder.AppendLine("                    {");
        builder.AppendLine("                        switch (hash)");
        builder.AppendLine("                        {");

        foreach (var kvp in actionsByHash.OrderBy(static pair => pair.Key))
        {
            builder.Append("                            case 0x").Append(kvp.Key.ToString("X", CultureInfo.InvariantCulture)).AppendLine("UL:");
            builder.AppendLine("                            {");

            foreach (var action in kvp.Value)
            {
                builder.Append("                                if (reader.PropertyNameEquals(\"").Append(EscapeStringLiteral(action.KeyName)).AppendLine("\"))");
                builder.AppendLine("                                {");

                if (action.IsParameter)
                {
                    var parameter = ctor.Parameters[action.Index];
                    builder.Append("                                    if (__argSeen").Append(action.Index.ToString(CultureInfo.InvariantCulture)).AppendLine(" && Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                    builder.AppendLine("                                    {");
                    builder.AppendLine("                                        var __duplicateName = reader.PropertyName!;");
                    builder.AppendLine("                                        reader.Read();");
                    if (CanEmitTableHeaderExtension(parameter.ParameterType))
                    {
                        EmitRepeatedTableExtensionIntoTarget(builder, parameter.ParameterType, "__arg" + action.Index.ToString(CultureInfo.InvariantCulture), "__arg" + action.Index.ToString(CultureInfo.InvariantCulture), "                                        ");
                    }
                    builder.AppendLine("                                        throw reader.CreateException($\"Duplicate key '{__duplicateName}' was encountered.\");");
                    builder.AppendLine("                                    }");
                    builder.Append("                                    __argSeen").Append(action.Index.ToString(CultureInfo.InvariantCulture)).AppendLine(" = true;");
                    builder.AppendLine("                                    reader.Read();");
                    if (parameter.LinkedMemberIndex >= 0 && parameter.LinkedMemberIndex < poco.Members.Length && poco.Members[parameter.LinkedMemberIndex].HasSingleOrArray)
                    {
                        var linkedMember = poco.Members[parameter.LinkedMemberIndex];
                        EmitSingleOrArrayReadAssignment(builder, parameter.ParameterType, "__arg" + action.Index.ToString(CultureInfo.InvariantCulture), "                                    ", "Member '" + EscapeStringLiteral(linkedMember.MemberName) + "' on '" + EscapeStringLiteral(typeName) + "' uses [TomlSingleOrArray]");
                    }
                    else
                    {
                        builder.Append("                                    __arg").Append(action.Index.ToString(CultureInfo.InvariantCulture)).Append(" = _context.").Append(GetTypeInfoPropertyName(parameter.ParameterType)).AppendLine(".Read(reader)!;");
                    }

                    if (parameter.LinkedMemberIndex >= 0 && parameter.LinkedMemberIndex < poco.Members.Length)
                    {
                        var linkedMember = poco.Members[parameter.LinkedMemberIndex];
                        if (linkedMember.IsRequired)
                        {
                            if (useSeenMask)
                            {
                                builder.Append("                                    seenMask |= 1UL << ").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                            }
                            else
                            {
                                builder.Append("                                    if (seen is not null) seen[").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                            }
                        }

                        if (linkedMember.CanSet && SymbolEqualityComparer.Default.Equals(linkedMember.Type, parameter.ParameterType))
                        {
                            builder.Append("                                    __memberValue").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).Append(" = __arg").Append(action.Index.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                            builder.Append("                                    __memberSeen").Append(parameter.LinkedMemberIndex.ToString(CultureInfo.InvariantCulture)).AppendLine(" = true;");
                        }
                    }

                    builder.AppendLine("                                    continue;");
                }
                else
                {
                    var member = poco.Members[action.Index];
                    if (useSeenMask)
                    {
                        builder.Append("                                    const ulong bit = 1UL << ").Append(action.Index.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                        builder.AppendLine("                                    if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                                    {");
                        builder.AppendLine("                                        if ((seenMask & bit) != 0)");
                        builder.AppendLine("                                        {");
                        builder.AppendLine("                                            var __duplicateName = reader.PropertyName!;");
                        builder.AppendLine("                                            reader.Read();");
                        var existingExpression = member.CanSet
                            ? "__memberValue" + action.Index.ToString(CultureInfo.InvariantCulture)
                            : GetLinkedParameterExpression(action.Index);
                        if (existingExpression is not null && CanEmitTableHeaderExtension(member.Type))
                        {
                            EmitRepeatedTableExtensionIntoTarget(builder, member.Type, existingExpression, existingExpression, "                                            ");
                        }
                        builder.AppendLine("                                            throw reader.CreateException($\"Duplicate key '{__duplicateName}' was encountered.\");");
                        builder.AppendLine("                                        }");
                        builder.AppendLine("                                    }");
                        if (member.IsRequired)
                        {
                            builder.AppendLine("                                    seenMask |= bit;");
                        }
                        else
                        {
                            builder.AppendLine("                                    if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error) seenMask |= bit;");
                        }
                    }
                    else
                    {
                        builder.AppendLine("                                    if (seen is not null)");
                        builder.AppendLine("                                    {");
                        builder.AppendLine("                                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                                        {");
                        builder.Append("                                            if (seen[").Append(action.Index.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                        builder.AppendLine("                                            {");
                        builder.AppendLine("                                                var __duplicateName = reader.PropertyName!;");
                        builder.AppendLine("                                                reader.Read();");
                        var existingExpression = member.CanSet
                            ? "__memberValue" + action.Index.ToString(CultureInfo.InvariantCulture)
                            : GetLinkedParameterExpression(action.Index);
                        if (existingExpression is not null && CanEmitTableHeaderExtension(member.Type))
                        {
                            EmitRepeatedTableExtensionIntoTarget(builder, member.Type, existingExpression, existingExpression, "                                                ");
                        }
                        builder.AppendLine("                                                throw reader.CreateException($\"Duplicate key '{__duplicateName}' was encountered.\");");
                        builder.AppendLine("                                            }");
                        builder.AppendLine("                                        }");
                        builder.Append("                                        seen[").Append(action.Index.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                        builder.AppendLine("                                    }");
                    }

                    builder.AppendLine("                                    reader.Read();");
                    if (!member.CanSet && !member.HasSingleOrArray)
                    {
                        builder.AppendLine("                                    reader.Skip();");
                        builder.AppendLine("                                    continue;");
                    }
                    else
                    {
                        if (member.HasSingleOrArray)
                        {
                            EmitSingleOrArrayReadAssignment(builder, member.Type, "__memberValue" + action.Index.ToString(CultureInfo.InvariantCulture), "                                    ", "Member '" + EscapeStringLiteral(member.MemberName) + "' on '" + EscapeStringLiteral(typeName) + "' uses [TomlSingleOrArray]");
                        }
                        else
                        {
                            builder.Append("                                    __memberValue").Append(action.Index.ToString(CultureInfo.InvariantCulture)).Append(" = _context.").Append(GetTypeInfoPropertyName(member.Type)).AppendLine(".Read(reader)!;");
                        }
                        builder.Append("                                    __memberSeen").Append(action.Index.ToString(CultureInfo.InvariantCulture)).AppendLine(" = true;");
                        builder.AppendLine("                                    continue;");
                    }
                }

                builder.AppendLine("                                }");
            }

            builder.AppendLine("                                break;");
            builder.AppendLine("                            }");
        }

        builder.AppendLine("                        }");
        builder.AppendLine("                    }");

        if (extensionData is not null)
        {
            builder.AppendLine("                    {");
            builder.AppendLine("                        var name = reader.PropertyName!;");
            builder.AppendLine("                        reader.Read();");
            builder.AppendLine("                        if (__extensionData is null) __extensionData = new();");
            builder.Append("                        __extensionData[name] = _context.").Append(GetTypeInfoPropertyName(extensionData.ValueType)).AppendLine(".Read(reader)!;");
            builder.AppendLine("                        continue;");
            builder.AppendLine("                    }");
        }

        builder.AppendLine("                    reader.Read();");
        builder.AppendLine("                    reader.Skip();");
        builder.AppendLine("                }");
        builder.AppendLine("            }");

        builder.AppendLine("            var endTableSpan = reader.CurrentSpan;");
        builder.AppendLine("            reader.Read();");

        // Validate constructor parameters.
        for (var i = 0; i < ctor.Parameters.Length; i++)
        {
            var parameter = ctor.Parameters[i];
            builder.Append("            if (!__argSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(")");
            builder.AppendLine("            {");
            if (parameter.HasDefaultValue && parameter.DefaultValueExpression is not null)
            {
                builder.Append("                __arg").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = ").Append(parameter.DefaultValueExpression).AppendLine(";");
                builder.AppendLine("            }");
            }
            else
            {
                var keyName = EscapeStringLiteral(parameter.KeyName);
                builder.AppendLine("                var span = endTableSpan ?? tableStartSpan;");
                builder.Append("                throw span is { } locatedSpan ? new TomlException(locatedSpan, $\"Missing required constructor parameter '")
                    .Append(keyName)
                    .Append("' when deserializing '{typeof(")
                    .Append(typeName)
                    .Append(").FullName}'.\") : new TomlException($\"Missing required constructor parameter '")
                    .Append(keyName)
                    .Append("' when deserializing '{typeof(")
                    .Append(typeName)
                    .Append(").FullName}'.\");");
                builder.AppendLine();
                builder.AppendLine("            }");
            }
        }

        // Validate required members.
        if (hasRequiredMembers)
        {
            if (useSeenMask)
            {
                builder.AppendLine("            if (requiredMask != 0 && (seenMask & requiredMask) != requiredMask)");
                builder.AppendLine("            {");
                builder.AppendLine("                var span = endTableSpan ?? tableStartSpan;");
                for (var i = 0; i < poco.Members.Length; i++)
                {
                    if (!poco.Members[i].IsRequired)
                    {
                        continue;
                    }

                    var serializedName = EscapeStringLiteral(poco.Members[i].SerializedName);
                    builder.Append("                if ((seenMask & (1UL << ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(")) == 0)");
                    builder.AppendLine("                {");
                    builder.Append("                    throw span is { } locatedSpan ? new TomlException(locatedSpan, $\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\") : new TomlException($\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\");");
                    builder.AppendLine("                }");
                }
                builder.AppendLine("            }");
            }
            else
            {
                builder.AppendLine("            if (seen is not null)");
                builder.AppendLine("            {");
                builder.AppendLine("                var span = endTableSpan ?? tableStartSpan;");
                for (var i = 0; i < poco.Members.Length; i++)
                {
                    if (!poco.Members[i].IsRequired)
                    {
                        continue;
                    }

                    var serializedName = EscapeStringLiteral(poco.Members[i].SerializedName);
                    builder.Append("                if (!seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                    builder.AppendLine("                {");
                    builder.Append("                    throw span is { } locatedSpan ? new TomlException(locatedSpan, $\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\") : new TomlException($\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\");");
                    builder.AppendLine("                }");
                }
                builder.AppendLine("            }");
            }
        }

        // Construct instance.
        if (useObjectInitializerConstruction)
        {
            var templateInitializerAssignments = ImmutableArray.CreateBuilder<string>();
            var finalInitializerAssignments = ImmutableArray.CreateBuilder<string>();
            var needsTemplate = false;

            for (var i = 0; i < poco.Members.Length; i++)
            {
                var member = poco.Members[i];
                if (!member.CanSet)
                {
                    if (member.HasSingleOrArray)
                    {
                        var addCollectionExpression = GetSingleOrArrayAddCollectionExpression(member.Type, "__existing", "__memberValue" + i.ToString(CultureInfo.InvariantCulture) + "!");
                        builder.Append("            if (__memberSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(")");
                        builder.AppendLine("            {");
                        builder.Append("                var __existing = value.").Append(member.MemberName).AppendLine(";");
                        builder.AppendLine("                if (__existing is null)");
                        builder.AppendLine("                {");
                        builder.Append("                    throw new TomlException($\"Member '").Append(EscapeStringLiteral(member.MemberName))
                            .Append("' on '{value.GetType().FullName}' uses [TomlSingleOrArray] but the existing collection is null or cannot be populated.\");")
                            .AppendLine();
                        builder.AppendLine("                }");
                        if (addCollectionExpression is null)
                        {
                            builder.Append("                throw new TomlException($\"Member '").Append(EscapeStringLiteral(member.MemberName))
                                .Append("' on '{value.GetType().FullName}' uses [TomlSingleOrArray] but '")
                                .Append(EscapeStringLiteral(member.Type.ToDisplayString(FullyQualifiedNullableFormat)))
                                .AppendLine("' doesn't support populating the existing collection.\");");
                        }
                        else
                        {
                            builder.Append("                _ = ").Append(addCollectionExpression).AppendLine(";");
                        }
                        builder.AppendLine("            }");
                    }

                    continue;
                }

                needsTemplate = true;
                finalInitializerAssignments.Add(member.MemberName + " = __memberValue" + i.ToString(CultureInfo.InvariantCulture));

                if (!member.IsCompilerRequired)
                {
                    continue;
                }

                var templateValueExpression = "__memberValue" + i.ToString(CultureInfo.InvariantCulture);
                if (GetLinkedParameterExpression(i) is { } linkedParameterExpression)
                {
                    templateValueExpression = "__memberSeen" + i.ToString(CultureInfo.InvariantCulture) + " ? __memberValue" + i.ToString(CultureInfo.InvariantCulture) + " : " + linkedParameterExpression;
                }

                templateInitializerAssignments.Add(member.MemberName + " = " + templateValueExpression);
            }

            if (extensionData is { CanSet: true })
            {
                needsTemplate = true;
                finalInitializerAssignments.Add(extensionData.MemberName + " = __extensionDataValue");
                if (extensionData.IsCompilerRequired)
                {
                    templateInitializerAssignments.Add(extensionData.MemberName + " = " + extensionData.CreateExpression);
                }
            }

            if (needsTemplate)
            {
                builder.Append("            ").Append(readNonNullableTypeName).AppendLine(" __template;");
                EmitPocoConstructionAssignment(builder, "__template", typeName, ctor.Parameters, templateInitializerAssignments.ToImmutable(), wrapConstructionErrors);

                for (var i = 0; i < poco.Members.Length; i++)
                {
                    var member = poco.Members[i];
                    if (!member.CanSet)
                    {
                        continue;
                    }

                    builder.Append("            if (!__memberSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(")");
                    builder.AppendLine("            {");
                    builder.Append("                __memberValue").Append(i.ToString(CultureInfo.InvariantCulture)).Append(" = __template.").Append(member.MemberName).AppendLine(";");
                    builder.AppendLine("            }");
                }

                if (extensionData is { CanSet: true })
                {
                    builder.Append("            var __extensionDataValue = __template.").Append(extensionData.MemberName).AppendLine(";");
                    builder.AppendLine("            if (__extensionData is not null && __extensionData.Count != 0)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (__extensionDataValue is null)");
                    builder.AppendLine("                {");
                    builder.Append("                    __extensionDataValue = ").Append(extensionData.CreateExpression).AppendLine(";");
                    builder.AppendLine("                }");
                    builder.AppendLine("                foreach (var __pair in __extensionData)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    __extensionDataValue[__pair.Key] = __pair.Value;");
                    builder.AppendLine("                }");
                    builder.AppendLine("            }");
                }
            }

            builder.Append("            ").Append(readNonNullableTypeName).AppendLine(" value;");
            EmitPocoConstructionAssignment(builder, "value", typeName, ctor.Parameters, finalInitializerAssignments.ToImmutable(), wrapConstructionErrors);

            if (callsOnDeserializing)
            {
                builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserializing)value).OnTomlDeserializing();");
            }

            if (extensionData is { CanSet: false })
            {
                var nullInitializationMessage = EscapeStringLiteral($"Extension data member '{extensionData.MemberName}' is null and cannot be initialized.");
                builder.AppendLine("            if (__extensionData is not null && __extensionData.Count != 0)");
                builder.AppendLine("            {");
                builder.Append("                var __target = value.").Append(extensionData.MemberName).AppendLine(";");
                builder.AppendLine("                if (__target is null)");
                builder.AppendLine("                {");
                builder.Append("                    throw new TomlException(\"").Append(nullInitializationMessage).AppendLine("\");");
                builder.AppendLine("                }");
                builder.AppendLine("                foreach (var __pair in __extensionData)");
                builder.AppendLine("                {");
                builder.AppendLine("                    __target[__pair.Key] = __pair.Value;");
                builder.AppendLine("                }");
                builder.AppendLine("            }");
            }
        }
        else
        {
            builder.Append("            ").Append(readNonNullableTypeName).AppendLine(" value;");
            EmitPocoConstructionAssignment(builder, "value", typeName, ctor.Parameters, ImmutableArray<string>.Empty, wrapConstructionErrors);

            if (callsOnDeserializing)
            {
                builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserializing)value).OnTomlDeserializing();");
            }

            for (var i = 0; i < poco.Members.Length; i++)
            {
                var member = poco.Members[i];
                if (!member.CanSet)
                {
                    continue;
                }

                builder.Append("            if (__memberSeen").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(")");
                builder.AppendLine("            {");
                builder.Append("                value.").Append(member.MemberName).Append(" = __memberValue").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                builder.AppendLine("            }");
            }

            if (extensionData is not null)
            {
                builder.AppendLine("            if (__extensionData is not null && __extensionData.Count != 0)");
                builder.AppendLine("            {");
                builder.Append("                var __target = value.").Append(extensionData.MemberName).AppendLine(";");
                builder.AppendLine("                if (__target is null)");
                builder.AppendLine("                {");
                builder.Append("                    __target = ").Append(extensionData.CreateExpression).AppendLine(";");
                builder.Append("                    value.").Append(extensionData.MemberName).AppendLine(" = __target;");
                builder.AppendLine("                }");
                builder.AppendLine("                foreach (var __pair in __extensionData)");
                builder.AppendLine("                {");
                builder.AppendLine("                    __target[__pair.Key] = __pair.Value;");
                builder.AppendLine("                }");
                builder.AppendLine("            }");
            }
        }

        if (callsOnDeserialized)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserialized)value).OnTomlDeserialized();");
        }

        builder.AppendLine("            return value;");
        builder.AppendLine("        }");
    }

    private static void EmitPocoReadIntoExisting(
        StringBuilder builder,
        string typeName,
        PocoShape poco,
        bool callsOnDeserializing,
        bool callsOnDeserialized)
    {
        builder.AppendLine();
        builder.AppendLine("        public override object? ReadInto(TomlReader reader, object? existingValue)");
        builder.AppendLine("        {");
        builder.Append("            if (existingValue is not ").Append(typeName).AppendLine(" value)");
        builder.AppendLine("            {");
        builder.AppendLine("                return Read(reader);");
        builder.AppendLine("            }");
        builder.AppendLine("            if (reader.TokenType != TomlTokenType.StartTable) throw reader.CreateException($\"Expected {TomlTokenType.StartTable} token but was {reader.TokenType}.\");");
        builder.AppendLine("            var tableStartSpan = reader.CurrentSpan;");
        if (callsOnDeserializing)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserializing)value).OnTomlDeserializing();");
        }

        var hasRequiredMembers = poco.Members.Any(static m => m.IsRequired);
        if (poco.Members.Length <= 64)
        {
            if (poco.Members.Length > 0)
            {
                builder.AppendLine("            ulong seenMask = 0;");
            }

            if (hasRequiredMembers)
            {
                ulong requiredMask = 0;
                for (var i = 0; i < poco.Members.Length; i++)
                {
                    if (poco.Members[i].IsRequired)
                    {
                        requiredMask |= 1UL << i;
                    }
                }

                builder.Append("            const ulong requiredMask = ").Append(requiredMask.ToString(CultureInfo.InvariantCulture)).AppendLine("UL;");
            }
        }
        else
        {
            builder.Append("            var seen = (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error");
            if (hasRequiredMembers)
            {
                builder.Append(" || true");
            }

            builder.Append(") ? new bool[").Append(poco.Members.Length.ToString(CultureInfo.InvariantCulture)).AppendLine("] : null;");
        }

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
        builder.AppendLine("            var endTableSpan = reader.CurrentSpan;");
        builder.AppendLine("            reader.Read();");

        if (hasRequiredMembers)
        {
            if (poco.Members.Length <= 64)
            {
                builder.AppendLine("            if (requiredMask != 0 && (seenMask & requiredMask) != requiredMask)");
                builder.AppendLine("            {");
                builder.AppendLine("                var span = endTableSpan ?? tableStartSpan;");
                for (var i = 0; i < poco.Members.Length; i++)
                {
                    if (!poco.Members[i].IsRequired)
                    {
                        continue;
                    }

                    var serializedName = EscapeStringLiteral(poco.Members[i].SerializedName);
                    builder.Append("                if ((seenMask & (1UL << ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(")) == 0)");
                    builder.AppendLine("                {");
                    builder.Append("                    throw span is { } locatedSpan ? new TomlException(locatedSpan, $\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\") : new TomlException($\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\");");
                    builder.AppendLine("                }");
                }
                builder.AppendLine("            }");
            }
            else
            {
                builder.AppendLine("            if (seen is not null)");
                builder.AppendLine("            {");
                builder.AppendLine("                var span = endTableSpan ?? tableStartSpan;");
                for (var i = 0; i < poco.Members.Length; i++)
                {
                    if (!poco.Members[i].IsRequired)
                    {
                        continue;
                    }

                    var serializedName = EscapeStringLiteral(poco.Members[i].SerializedName);
                    builder.Append("                if (!seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                    builder.AppendLine("                {");
                    builder.Append("                    throw span is { } locatedSpan ? new TomlException(locatedSpan, $\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\") : new TomlException($\"Missing required TOML key '")
                        .Append(serializedName)
                        .Append("' when deserializing '{typeof(")
                        .Append(typeName)
                        .Append(").FullName}'.\");");
                    builder.AppendLine("                }");
                }
                builder.AppendLine("            }");
            }
        }

        if (callsOnDeserialized)
        {
            builder.AppendLine("            ((global::Tomlyn.Serialization.ITomlOnDeserialized)value).OnTomlDeserialized();");
        }

        builder.AppendLine("            return value;");
        builder.AppendLine("        }");
    }

    private static void EmitPocoConstructionAssignment(
        StringBuilder builder,
        string targetName,
        string typeName,
        ImmutableArray<PocoConstructorParameter> parameters,
        ImmutableArray<string> initializerAssignments,
        bool wrapConstructionErrors)
    {
        void EmitAssignmentBody(string indent)
        {
            builder.Append(indent).Append(targetName).Append(" = new ").Append(typeName).Append("(");
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                {
                    builder.Append(", ");
                }

                builder.Append("__arg").Append(i.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(")");
            if (initializerAssignments.IsDefaultOrEmpty)
            {
                builder.AppendLine(";");
                return;
            }

            builder.AppendLine();
            builder.Append(indent).AppendLine("{");
            for (var i = 0; i < initializerAssignments.Length; i++)
            {
                builder.Append(indent).Append("    ").Append(initializerAssignments[i]).AppendLine(",");
            }

            builder.Append(indent).AppendLine("};");
        }

        if (!wrapConstructionErrors)
        {
            EmitAssignmentBody("            ");
            return;
        }

        builder.AppendLine("            try");
        builder.AppendLine("            {");
        EmitAssignmentBody("                ");
        builder.AppendLine("            }");
        builder.AppendLine("            catch (Exception ex)");
        builder.AppendLine("            {");
        builder.AppendLine("                throw new TomlException($\"Failed to create an instance of '{typeof(" + typeName + ").FullName}'.\", ex);");
        builder.AppendLine("            }");
    }

    private static string? GetPopulateConditionExpression(PocoMember member)
    {
        return member.ObjectCreationHandling switch
        {
            ObjectCreationHandlingKind.Replace => null,
            ObjectCreationHandlingKind.Populate => "true",
            _ => "Options.PreferredObjectCreationHandling == global::System.Text.Json.Serialization.JsonObjectCreationHandling.Populate",
        };
    }

    private static string GetSingleOrArrayPopulateConditionExpression(PocoMember member)
    {
        var populateCondition = GetPopulateConditionExpression(member);
        if (!member.CanSet)
        {
            return "true";
        }

        return populateCondition ?? "false";
    }

    private static bool TryGetSingleOrArrayElementType(ITypeSymbol type, out ITypeSymbol elementType, out bool isArray, out SequenceKind kind)
    {
        if (TryGetArrayElementType(type, out elementType))
        {
            isArray = true;
            kind = default;
            return true;
        }

        if (TryGetSequenceElementType(type, out elementType, out kind))
        {
            isArray = false;
            return true;
        }

        elementType = null!;
        isArray = false;
        kind = default;
        return false;
    }

    private static bool CanPopulateSingleOrArraySequence(SequenceKind kind)
    {
        return kind is SequenceKind.List or SequenceKind.ListBackedEnumerable or SequenceKind.HashSet or SequenceKind.HashSetBackedEnumerable;
    }

    private static string? GetSingleOrArrayCreateExpression(ITypeSymbol collectionType)
    {
        if (!TryGetSingleOrArrayElementType(collectionType, out var elementType, out var isArray, out var kind))
        {
            return null;
        }

        var elementTypeName = elementType.ToDisplayString(FullyQualifiedNullableFormat);
        var readElementExpression = "_context." + GetTypeInfoPropertyName(elementType) + ".Read(reader)!";

        if (isArray)
        {
            return "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.CreateSingleElementArray<" + elementTypeName + ">(" + readElementExpression + ")";
        }

        return kind switch
        {
            SequenceKind.List or SequenceKind.ListBackedEnumerable => "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.CreateSingleElementList<" + elementTypeName + ">(" + readElementExpression + ")",
            SequenceKind.HashSet or SequenceKind.HashSetBackedEnumerable => "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.CreateSingleElementHashSet<" + elementTypeName + ">(" + readElementExpression + ")",
            SequenceKind.ImmutableArray => "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.CreateSingleElementImmutableArray<" + elementTypeName + ">(" + readElementExpression + ")",
            SequenceKind.ImmutableList => "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.CreateSingleElementImmutableList<" + elementTypeName + ">(" + readElementExpression + ")",
            SequenceKind.ImmutableHashSet => "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.CreateSingleElementImmutableHashSet<" + elementTypeName + ">(" + readElementExpression + ")",
            _ => null,
        };
    }

    private static string? GetSingleOrArrayCanPopulateExpression(ITypeSymbol collectionType, string existingExpression)
    {
        if (!TryGetSingleOrArrayElementType(collectionType, out var elementType, out var isArray, out var kind) || isArray || !CanPopulateSingleOrArraySequence(kind))
        {
            return null;
        }

        var elementTypeName = elementType.ToDisplayString(FullyQualifiedNullableFormat);
        return "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.CanPopulateCollection<" + elementTypeName + ">(" + existingExpression + ")";
    }

    private static string? GetSingleOrArrayAddSingleExpression(ITypeSymbol collectionType, string existingExpression)
    {
        if (!TryGetSingleOrArrayElementType(collectionType, out var elementType, out var isArray, out var kind) || isArray || !CanPopulateSingleOrArraySequence(kind))
        {
            return null;
        }

        var elementTypeName = elementType.ToDisplayString(FullyQualifiedNullableFormat);
        var readElementExpression = "_context." + GetTypeInfoPropertyName(elementType) + ".Read(reader)!";
        return "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.AddSingleElementToCollection<" + elementTypeName + ">(" + existingExpression + ", " + readElementExpression + ")";
    }

    private static string? GetSingleOrArrayAddCollectionExpression(ITypeSymbol collectionType, string existingExpression, string incomingExpression)
    {
        if (!TryGetSingleOrArrayElementType(collectionType, out var elementType, out var isArray, out var kind) || isArray || !CanPopulateSingleOrArraySequence(kind))
        {
            return null;
        }

        var elementTypeName = elementType.ToDisplayString(FullyQualifiedNullableFormat);
        return "global::Tomlyn.Serialization.Internal.TomlSingleOrArrayCollectionHelper.AddCollectionToExisting<" + elementTypeName + ">(" + existingExpression + ", (global::System.Collections.Generic.IEnumerable<" + elementTypeName + ">)" + incomingExpression + ")";
    }

    private static void EmitSingleOrArrayReadAssignment(StringBuilder builder, ITypeSymbol collectionType, string targetExpression, string indent, string errorPrefix)
    {
        var collectionTypeName = collectionType.ToDisplayString(FullyQualifiedNullableFormat);
        var createExpression = GetSingleOrArrayCreateExpression(collectionType);

        builder.Append(indent).AppendLine("if (reader.TokenType == TomlTokenType.StartArray)");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).Append("    ").Append(targetExpression).Append(" = _context.").Append(GetTypeInfoPropertyName(collectionType)).AppendLine(".Read(reader)!;");
        builder.Append(indent).AppendLine("}");
        builder.Append(indent).AppendLine("else");
        builder.Append(indent).AppendLine("{");
        if (createExpression is null)
        {
            builder.Append(indent).Append("    throw new TomlException(\"").Append(errorPrefix)
                .Append(" but '").Append(EscapeStringLiteral(collectionTypeName))
                .AppendLine("' is not a supported collection type.\");");
        }
        else
        {
            builder.Append(indent).Append("    ").Append(targetExpression).Append(" = (").Append(collectionTypeName).Append(")").Append(createExpression).AppendLine("!;");
        }
        builder.Append(indent).AppendLine("}");
    }

    private static void EmitRepeatedTableExtensionIntoTarget(StringBuilder builder, ITypeSymbol type, string existingExpression, string targetExpression, string indent)
    {
        var typeName = type.ToDisplayString(FullyQualifiedNullableFormat);
        builder.Append(indent).Append("if (reader.TokenType == TomlTokenType.StartTable && reader.CurrentSpan is null && ").Append(existingExpression).AppendLine(" is not null)");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).Append("    var __tableExtension = _context.").Append(GetTypeInfoPropertyName(type)).Append(".ReadInto(reader, ").Append(existingExpression).AppendLine(");");
        builder.Append(indent).Append("    ").Append(targetExpression).Append(" = (").Append(typeName).AppendLine(")__tableExtension!;");
        builder.Append(indent).AppendLine("    continue;");
        builder.Append(indent).AppendLine("}");
    }

    private static void EmitRepeatedTableExtensionIntoReadOnlyMember(StringBuilder builder, PocoMember member, string indent)
    {
        var memberAccess = "value." + member.MemberName;
        var memberTypeName = member.Type.ToDisplayString(FullyQualifiedNullableFormat);
        builder.Append(indent).Append("if (reader.TokenType == TomlTokenType.StartTable && reader.CurrentSpan is null && ").Append(memberAccess).AppendLine(" is not null)");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).Append("    var __tableExtension = _context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".ReadInto(reader, ").Append(memberAccess).AppendLine(");");
        builder.Append(indent).Append("    if (!object.ReferenceEquals(").Append(memberAccess).Append(", __tableExtension))").AppendLine();
        builder.Append(indent).AppendLine("    {");
        builder.Append(indent).Append("        throw new TomlException($\"Member '").Append(EscapeStringLiteral(member.MemberName))
            .Append("' on '{value.GetType().FullName}' cannot be extended by an additional TOML table definition because '")
            .Append(EscapeStringLiteral(memberTypeName))
            .AppendLine("' does not support in-place population.\");");
        builder.Append(indent).AppendLine("    }");
        builder.Append(indent).AppendLine("    continue;");
        builder.Append(indent).AppendLine("}");
    }

    private static bool CanEmitTableHeaderExtension(ITypeSymbol type) => !type.IsValueType;

    private static void EmitSingleOrArrayMemberRead(StringBuilder builder, PocoMember member, string indent)
    {
        var memberTypeName = member.Type.ToDisplayString(FullyQualifiedNullableFormat);
        var memberAccess = "value." + member.MemberName;
        var populateCondition = GetSingleOrArrayPopulateConditionExpression(member);
        var errorPrefix = $"Member '{EscapeStringLiteral(member.MemberName)}' on '{{value.GetType().FullName}}' uses [TomlSingleOrArray]";
        var createExpression = GetSingleOrArrayCreateExpression(member.Type);
        var canPopulateExpression = GetSingleOrArrayCanPopulateExpression(member.Type, memberAccess + "!");
        var addSingleExpression = GetSingleOrArrayAddSingleExpression(member.Type, memberAccess + "!");

        builder.Append(indent).AppendLine("if (reader.TokenType == TomlTokenType.StartArray)");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).Append("    if (").Append(populateCondition).Append(" && ").Append(memberAccess).AppendLine(" is not null)");
        builder.Append(indent).AppendLine("    {");
        if (canPopulateExpression is not null)
        {
            builder.Append(indent).Append("        if (").Append(canPopulateExpression).AppendLine(")");
            builder.Append(indent).AppendLine("        {");
            builder.Append(indent).Append("            var __populated = _context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".ReadInto(reader, ").Append(memberAccess).AppendLine(");");
            if (member.CanSet)
            {
                builder.Append(indent).Append("            ").Append(memberAccess).Append(" = (").Append(memberTypeName).AppendLine(")__populated!;");
            }
            else
            {
                builder.Append(indent).Append("            if (!object.ReferenceEquals(").Append(memberAccess).Append(", __populated))").AppendLine();
                builder.Append(indent).AppendLine("            {");
                builder.Append(indent).Append("                throw new TomlException($\"").Append(errorPrefix)
                    .Append(" but '").Append(EscapeStringLiteral(memberTypeName))
                    .AppendLine("' doesn't support populating the existing collection.\");");
                builder.Append(indent).AppendLine("            }");
            }
            builder.Append(indent).AppendLine("        }");
            builder.Append(indent).AppendLine("        else");
            builder.Append(indent).AppendLine("        {");
        }

        if (!member.CanSet)
        {
            builder.Append(indent).Append("            throw new TomlException($\"").Append(errorPrefix)
                .Append(" but '").Append(EscapeStringLiteral(memberTypeName))
                .AppendLine("' doesn't support populating the existing collection.\");");
        }
        else
        {
            builder.Append(indent).Append("            ").Append(memberAccess).Append(" = _context.").Append(GetTypeInfoPropertyName(member.Type)).AppendLine(".Read(reader)!;");
        }

        if (canPopulateExpression is not null)
        {
            builder.Append(indent).AppendLine("        }");
        }
        builder.Append(indent).AppendLine("    }");
        builder.Append(indent).AppendLine("    else");
        builder.Append(indent).AppendLine("    {");
        if (!member.CanSet)
        {
            builder.Append(indent).Append("        throw new TomlException($\"").Append(errorPrefix)
                .AppendLine(" but the existing collection is null or cannot be populated.\");");
        }
        else
        {
            builder.Append(indent).Append("        ").Append(memberAccess).Append(" = _context.").Append(GetTypeInfoPropertyName(member.Type)).AppendLine(".Read(reader)!;");
        }
        builder.Append(indent).AppendLine("    }");
        builder.Append(indent).AppendLine("}");
        builder.Append(indent).AppendLine("else");
        builder.Append(indent).AppendLine("{");
        if (createExpression is null)
        {
            builder.Append(indent).Append("    throw new TomlException($\"").Append(errorPrefix)
                .Append(" but '").Append(EscapeStringLiteral(memberTypeName))
                .AppendLine("' is not a supported collection type.\");");
        }
        else
        {
            builder.Append(indent).Append("    if (").Append(populateCondition).Append(" && ").Append(memberAccess).AppendLine(" is not null)");
            builder.Append(indent).AppendLine("    {");
            if (canPopulateExpression is not null && addSingleExpression is not null)
            {
                builder.Append(indent).Append("        if (").Append(canPopulateExpression).AppendLine(")");
                builder.Append(indent).AppendLine("        {");
                builder.Append(indent).Append("            _ = ").Append(addSingleExpression).AppendLine(";");
                builder.Append(indent).AppendLine("        }");
                builder.Append(indent).AppendLine("        else");
                builder.Append(indent).AppendLine("        {");
            }

            if (!member.CanSet)
            {
                builder.Append(indent).Append("            throw new TomlException($\"").Append(errorPrefix)
                    .Append(" but '").Append(EscapeStringLiteral(memberTypeName))
                    .AppendLine("' doesn't support populating the existing collection.\");");
            }
            else
            {
                builder.Append(indent).Append("            ").Append(memberAccess).Append(" = (").Append(memberTypeName).Append(")").Append(createExpression).AppendLine("!;");
            }

            if (canPopulateExpression is not null && addSingleExpression is not null)
            {
                builder.Append(indent).AppendLine("        }");
            }

            builder.Append(indent).AppendLine("    }");
            builder.Append(indent).AppendLine("    else");
            builder.Append(indent).AppendLine("    {");
            if (!member.CanSet)
            {
                builder.Append(indent).Append("        throw new TomlException($\"").Append(errorPrefix)
                    .AppendLine(" but the existing collection is null or cannot be populated.\");");
            }
            else
            {
                builder.Append(indent).Append("        ").Append(memberAccess).Append(" = (").Append(memberTypeName).Append(")").Append(createExpression).AppendLine("!;");
            }
            builder.Append(indent).AppendLine("    }");
        }
        builder.Append(indent).AppendLine("}");
    }

    private static void EmitMemberRead(StringBuilder builder, PocoMember member, int index, string indent)
    {
        if (member.HasSingleOrArray)
        {
            EmitSingleOrArrayMemberRead(builder, member, indent);
            return;
        }

        var memberTypeName = member.Type.ToDisplayString(FullyQualifiedNullableFormat);
        var typeInfoPropertyName = GetTypeInfoPropertyName(member.Type);
        var populateCondition = GetPopulateConditionExpression(member);
        var existingLocal = "__existing" + index.ToString(CultureInfo.InvariantCulture);
        var populatedLocal = "__populated" + index.ToString(CultureInfo.InvariantCulture);
        var memberAccess = "value." + member.MemberName;
        var populateErrorPrefix =
            $"Member '{EscapeStringLiteral(member.MemberName)}' on '{{value.GetType().FullName}}' uses JsonObjectCreationHandling.Populate";
        var isExplicitPopulate = member.HasExplicitObjectCreationHandling && member.ObjectCreationHandling == ObjectCreationHandlingKind.Populate;

        if (populateCondition is null)
        {
            if (!member.CanSet)
            {
                builder.Append(indent).AppendLine("reader.Skip();");
            }
            else
            {
                builder.Append(indent).Append(memberAccess).Append(" = _context.").Append(typeInfoPropertyName).AppendLine(".Read(reader)!;");
            }

            return;
        }

        if (populateCondition != "true")
        {
            builder.Append(indent).Append("if (").Append(populateCondition).AppendLine(")");
            builder.Append(indent).AppendLine("{");
        }

        if (!member.CanSet && member.Type.IsValueType)
        {
            if (isExplicitPopulate)
            {
                builder.Append(indent).Append("    throw new TomlException($\"")
                    .Append(populateErrorPrefix)
                    .Append(" but requires a setter because '")
                    .Append(EscapeStringLiteral(memberTypeName))
                    .AppendLine("' is a value type.\");");
            }
            else
            {
                builder.Append(populateCondition == "true" ? indent : indent + "    ").AppendLine("reader.Skip();");
            }
        }
        else
        {
            var branchIndent = populateCondition == "true" ? indent : indent + "    ";
            builder.Append(branchIndent).Append("var ").Append(existingLocal).Append(" = ").Append(memberAccess).AppendLine(";");

            if (CanBeNull(member.Type))
            {
                builder.Append(branchIndent).Append("if (").Append(existingLocal).AppendLine(" is null)");
                builder.Append(branchIndent).AppendLine("{");
                if (!member.CanSet)
                {
                    builder.Append(branchIndent).AppendLine("    reader.Skip();");
                }
                else
                {
                    builder.Append(branchIndent).Append("    ").Append(memberAccess).Append(" = _context.").Append(typeInfoPropertyName).AppendLine(".Read(reader)!;");
                }
                builder.Append(branchIndent).AppendLine("}");
                builder.Append(branchIndent).AppendLine("else");
                builder.Append(branchIndent).AppendLine("{");
            }

            var populateIndent = CanBeNull(member.Type)
                ? (populateCondition == "true" ? indent + "    " : indent + "        ")
                : (populateCondition == "true" ? indent : indent + "    ");
            builder.Append(populateIndent).Append("var ").Append(populatedLocal).Append(" = _context.").Append(typeInfoPropertyName).Append(".ReadInto(reader, ").Append(existingLocal).AppendLine(");");
            if (member.CanSet)
            {
                builder.Append(populateIndent).Append(memberAccess).Append(" = (").Append(memberTypeName).Append(")").Append(populatedLocal).AppendLine("!;");
            }
            else
            {
                builder.Append(populateIndent).Append("if (!object.ReferenceEquals(").Append(existingLocal).Append(", ").Append(populatedLocal).AppendLine("))");
                builder.Append(populateIndent).AppendLine("{");
                if (isExplicitPopulate)
                {
                    builder.Append(populateIndent).Append("    throw new TomlException($\"")
                        .Append(populateErrorPrefix)
                        .AppendLine(" but it doesn't support populating.\");");
                }
                builder.Append(populateIndent).AppendLine("}");
            }

            if (CanBeNull(member.Type))
            {
                builder.Append(branchIndent).AppendLine("}");
            }
        }

        if (populateCondition != "true")
        {
            builder.Append(indent).AppendLine("}");
        }

        if (populateCondition == "true")
        {
            return;
        }

        builder.Append(indent).AppendLine("else");
        builder.Append(indent).AppendLine("{");
        if (!member.CanSet)
        {
            builder.Append(indent).AppendLine("    reader.Skip();");
        }
        else
        {
            builder.Append(indent).Append("    ").Append(memberAccess).Append(" = _context.").Append(typeInfoPropertyName).AppendLine(".Read(reader)!;");
        }
        builder.Append(indent).AppendLine("}");
    }

    private static void EmitMemberDispatch(StringBuilder builder, PocoShape poco, bool ignoreCase)
    {
        var useSeenMask = poco.Members.Length <= 64;
        if (ignoreCase)
        {
            var comparison = "StringComparison.OrdinalIgnoreCase";
            for (var i = 0; i < poco.Members.Length; i++)
            {
                var member = poco.Members[i];
                var alwaysThrowsOnRead =
                    !member.CanSet &&
                    member.Type.IsValueType &&
                    member.HasExplicitObjectCreationHandling &&
                    member.ObjectCreationHandling == ObjectCreationHandlingKind.Populate;
                builder.Append("                    ").Append(i == 0 ? "if" : "else if").Append("(string.Equals(name, \"").Append(EscapeStringLiteral(member.SerializedName)).Append("\", ").Append(comparison).AppendLine("))");
                builder.AppendLine("                    {");
                if (useSeenMask)
                {
                    if (member.IsRequired)
                    {
                        builder.Append("                        const ulong bit = 1UL << ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                        builder.AppendLine("                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                        {");
                        builder.AppendLine("                            if ((seenMask & bit) != 0)");
                        builder.AppendLine("                            {");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                ");
                            }
                        }
                        builder.AppendLine("                                throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
                        builder.AppendLine("                            }");
                        builder.AppendLine("                        }");
                        builder.AppendLine("                        seenMask |= bit;");
                    }
                    else
                    {
                        builder.AppendLine("                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                        {");
                        builder.Append("                            const ulong bit = 1UL << ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                        builder.AppendLine("                            if ((seenMask & bit) != 0)");
                        builder.AppendLine("                            {");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                ");
                            }
                        }
                        builder.AppendLine("                                throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
                        builder.AppendLine("                            }");
                        builder.AppendLine("                            seenMask |= bit;");
                        builder.AppendLine("                        }");
                    }
                }
                else
                {
                    builder.AppendLine("                        if (seen is not null)");
                    builder.AppendLine("                        {");
                    if (member.IsRequired)
                    {
                        builder.AppendLine("                            if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                            {");
                        builder.Append("                                if (seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                        builder.AppendLine("                                {");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                    ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                    ");
                            }
                        }
                        builder.AppendLine("                                    throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
                        builder.AppendLine("                                }");
                        builder.AppendLine("                            }");
                        builder.Append("                            seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                    }
                    else
                    {
                        builder.AppendLine("                            if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                            {");
                        builder.Append("                                if (seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                        builder.AppendLine("                                {");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                    ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                    ");
                            }
                        }
                        builder.AppendLine("                                    throw reader.CreateException($\"Duplicate key '{name}' was encountered.\");");
                        builder.AppendLine("                                }");
                        builder.Append("                                seen[").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                        builder.AppendLine("                            }");
                    }
                    builder.AppendLine("                        }");
                }
                EmitMemberRead(builder, member, i, "                        ");
                if (!alwaysThrowsOnRead)
                {
                    builder.AppendLine("                        continue;");
                }
                builder.AppendLine("                    }");
            }

            if (poco.ExtensionData is { } extensionData)
            {
                var extensionValueTypeInfo = GetTypeInfoPropertyName(extensionData.ValueType);
                builder.Append("                    var __extensionData = value.").Append(extensionData.MemberName).AppendLine(";");
                builder.AppendLine("                    if (__extensionData is null)");
                builder.AppendLine("                    {");
                builder.Append("                        __extensionData = ").Append(extensionData.CreateExpression).AppendLine(";");
                builder.Append("                        value.").Append(extensionData.MemberName).AppendLine(" = __extensionData;");
                builder.AppendLine("                    }");
                builder.Append("                    __extensionData[name] = _context.").Append(extensionValueTypeInfo).AppendLine(".Read(reader)!;");
                builder.AppendLine("                    continue;");
                return;
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
                var alwaysThrowsOnRead =
                    !member.CanSet &&
                    member.Type.IsValueType &&
                    member.HasExplicitObjectCreationHandling &&
                    member.ObjectCreationHandling == ObjectCreationHandlingKind.Populate;
                builder.Append("                                if (reader.PropertyNameEquals(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\"))");
                builder.AppendLine("                                {");
                if (useSeenMask)
                {
                    if (member.IsRequired)
                    {
                        builder.Append("                                    const ulong bit = 1UL << ").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                        builder.AppendLine("                                    if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                                    {");
                        builder.AppendLine("                                        if ((seenMask & bit) != 0)");
                        builder.AppendLine("                                        {");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                            ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                            ");
                            }
                        }
                        builder.AppendLine("                                            throw reader.CreateException($\"Duplicate key '{reader.PropertyName}' was encountered.\");");
                        builder.AppendLine("                                        }");
                        builder.AppendLine("                                    }");
                        builder.AppendLine("                                    seenMask |= bit;");
                    }
                    else
                    {
                        builder.AppendLine("                                    if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                                    {");
                        builder.Append("                                        const ulong bit = 1UL << ").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
                        builder.AppendLine("                                        if ((seenMask & bit) != 0)");
                        builder.AppendLine("                                        {");
                        builder.AppendLine("                                            var __duplicateName = reader.PropertyName!;");
                        builder.AppendLine("                                            reader.Read();");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                            ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                            ");
                            }
                        }
                        builder.AppendLine("                                            throw reader.CreateException($\"Duplicate key '{__duplicateName}' was encountered.\");");
                        builder.AppendLine("                                        }");
                        builder.AppendLine("                                        seenMask |= bit;");
                        builder.AppendLine("                                    }");
                    }
                }
                else
                {
                    builder.AppendLine("                                    if (seen is not null)");
                    builder.AppendLine("                                    {");
                    if (member.IsRequired)
                    {
                        builder.AppendLine("                                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                                        {");
                        builder.Append("                                            if (seen[").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                        builder.AppendLine("                                            {");
                        builder.AppendLine("                                                var __duplicateName = reader.PropertyName!;");
                        builder.AppendLine("                                                reader.Read();");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                                ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                                ");
                            }
                        }
                        builder.AppendLine("                                                throw reader.CreateException($\"Duplicate key '{reader.PropertyName}' was encountered.\");");
                        builder.AppendLine("                                            }");
                        builder.AppendLine("                                        }");
                        builder.Append("                                        seen[").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                    }
                    else
                    {
                        builder.AppendLine("                                        if (Options.DuplicateKeyHandling == TomlDuplicateKeyHandling.Error)");
                        builder.AppendLine("                                        {");
                        builder.Append("                                            if (seen[").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine("])");
                        builder.AppendLine("                                            {");
                        if (member.CanSet)
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoTarget(builder, member.Type, "value." + member.MemberName, "value." + member.MemberName, "                                                ");
                            }
                        }
                        else
                        {
                            if (CanEmitTableHeaderExtension(member.Type))
                            {
                                EmitRepeatedTableExtensionIntoReadOnlyMember(builder, member, "                                                ");
                            }
                        }
                        builder.AppendLine("                                                throw reader.CreateException($\"Duplicate key '{__duplicateName}' was encountered.\");");
                        builder.AppendLine("                                            }");
                        builder.Append("                                            seen[").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine("] = true;");
                        builder.AppendLine("                                        }");
                    }
                    builder.AppendLine("                                    }");
                }
                builder.AppendLine("                                    reader.Read();");
                EmitMemberRead(builder, member, index, "                                    ");
                if (!alwaysThrowsOnRead)
                {
                    builder.AppendLine("                                    continue;");
                }
                builder.AppendLine("                                }");
            }

            builder.AppendLine("                                break;");
            builder.AppendLine("                            }");
        }

        builder.AppendLine("                        }");
        builder.AppendLine("                    }");
        if (poco.ExtensionData is { } extensionDataRead)
        {
            var extensionValueTypeInfo = GetTypeInfoPropertyName(extensionDataRead.ValueType);
            builder.AppendLine("                    {");
            builder.AppendLine("                        var name = reader.PropertyName!;");
            builder.AppendLine("                        reader.Read();");
            builder.Append("                        var __extensionData = value.").Append(extensionDataRead.MemberName).AppendLine(";");
            builder.AppendLine("                        if (__extensionData is null)");
            builder.AppendLine("                        {");
            builder.Append("                            __extensionData = ").Append(extensionDataRead.CreateExpression).AppendLine(";");
            builder.Append("                            value.").Append(extensionDataRead.MemberName).AppendLine(" = __extensionData;");
            builder.AppendLine("                        }");
            builder.Append("                        __extensionData[name] = _context.").Append(extensionValueTypeInfo).AppendLine(".Read(reader)!;");
            builder.AppendLine("                        continue;");
            builder.AppendLine("                    }");
            return;
        }
        builder.AppendLine("                    reader.Read();");
        builder.AppendLine("                    reader.Skip();");
    }

    private static void EmitWriteMember(StringBuilder builder, PocoMember member, int index, string memberTypeName, bool canBeNull, string? usedKeysVariable)
    {
        var localName = "__member" + index.ToString(CultureInfo.InvariantCulture);
        var writeArgument = canBeNull ? localName + "!" : localName;
        var writeIndent = "                ";
        var openIndent = "            ";
        var defaultLiteral = GetDefaultLiteral(member.Type);
        var defaultIsNull = member.Type.IsReferenceType || TryGetNullableUnderlyingType(member.Type, out _);

        if (member.WriteIgnore == WriteIgnoreKind.WhenWritingNull)
        {
            if (canBeNull)
            {
                builder.Append(openIndent).Append("if (").Append(localName).AppendLine(" is not null)");
                builder.Append(openIndent).AppendLine("{");
                builder.Append(writeIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
                if (usedKeysVariable is not null)
                {
                    builder.Append(writeIndent).Append(usedKeysVariable).Append("?.Add(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
                }
                builder.Append(writeIndent).Append("_context.").Append(GetTypeInfoPropertyName(member.Type)).Append(".Write(writer, ").Append(localName).AppendLine(");");
                builder.Append(openIndent).AppendLine("}");
                return;
            }

            builder.Append(openIndent).Append("writer.WritePropertyName(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
            if (usedKeysVariable is not null)
            {
                builder.Append(openIndent).Append(usedKeysVariable).Append("?.Add(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
            }
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
            if (usedKeysVariable is not null)
            {
                builder.Append(writeIndent).Append(usedKeysVariable).Append("?.Add(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
            }
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
        if (usedKeysVariable is not null)
        {
            builder.Append(writeIndent).Append(usedKeysVariable).Append("?.Add(\"").Append(EscapeStringLiteral(member.SerializedName)).AppendLine("\");");
        }
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

    private static string GetDefaultLiteral(ITypeSymbol type)
    {
        return type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated
            ? "default!"
            : "default";
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

    private static ImmutableArray<ITypeSymbol> ExpandTypeGraph(
        SourceProductionContext context,
        ContextModel model,
        ImmutableArray<DerivedTypeMappingModel> derivedTypeMappings)
    {
        var queue = new Queue<ITypeSymbol>(model.RootTypes.Select(static root => root.Type));
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var list = new List<ITypeSymbol>();

        foreach (var mapping in derivedTypeMappings)
        {
            queue.Enqueue(mapping.BaseType);
            queue.Enqueue(mapping.DerivedType);
        }

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
                if (IsSupportedMemberType(nullableUnderlyingType, derivedTypeMappings))
                {
                    queue.Enqueue(nullableUnderlyingType);
                }

                continue;
            }

            if (TryGetArrayElementType(current, out var arrayElementType))
            {
                if (IsSupportedMemberType(arrayElementType, derivedTypeMappings))
                {
                    queue.Enqueue(arrayElementType);
                }

                continue;
            }

            if (TryGetSequenceElementType(current, out var enumerableElementType, out _))
            {
                if (IsSupportedMemberType(enumerableElementType, derivedTypeMappings))
                {
                    queue.Enqueue(enumerableElementType);
                }

                continue;
            }

            if (TryGetDictionaryValueType(current, out var dictionaryValueType))
            {
                if (IsSupportedMemberType(dictionaryValueType, derivedTypeMappings))
                {
                    queue.Enqueue(dictionaryValueType);
                }

                continue;
            }

            if (TryGetPocoShape(context, model, current, out var poco))
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

                    if (!IsSupportedMemberType(member.Type, derivedTypeMappings))
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

                if (poco.Constructor is { IsValid: true } ctor)
                {
                    foreach (var parameter in ctor.Parameters)
                    {
                        if (!IsSupportedMemberType(parameter.ParameterType, derivedTypeMappings))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                UnsupportedMemberType,
                                model.ContextSymbol.Locations.FirstOrDefault(),
                                current.ToDisplayString(),
                                parameter.ParameterName,
                                parameter.ParameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                            continue;
                        }

                        queue.Enqueue(parameter.ParameterType);
                    }
                }

                if (poco.ExtensionData is { } extensionData)
                {
                    if (!IsSupportedMemberType(extensionData.ValueType, derivedTypeMappings))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            UnsupportedMemberType,
                            model.ContextSymbol.Locations.FirstOrDefault(),
                            current.ToDisplayString(),
                            extensionData.MemberName,
                            extensionData.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                    }
                    else
                    {
                        queue.Enqueue(extensionData.ValueType);
                    }
                }
            }

            if (TryGetPolymorphicShape(context, model, derivedTypeMappings, current, out var polymorphic))
            {
                foreach (var derived in polymorphic.DerivedTypes)
                {
                    if (!IsSupportedMemberType(derived.Type, derivedTypeMappings))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            InvalidPolymorphismConfiguration,
                            model.ContextSymbol.Locations.FirstOrDefault(),
                            current.ToDisplayString(),
                            $"Derived type '{derived.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' is not supported by the source generator."));
                        continue;
                    }

                    queue.Enqueue(derived.Type);
                }

                if (polymorphic.DefaultDerivedType is not null)
                {
                    queue.Enqueue(polymorphic.DefaultDerivedType);
                }
            }
        }

        return list.ToImmutableArray();
    }

    private static bool ValidateRootAttributes(SourceProductionContext context, ContextModel model)
    {
        foreach (var root in model.RootTypes)
        {
            if (root.TypeInfoPropertyName is null)
            {
                continue;
            }

            if (!SyntaxFacts.IsValidIdentifier(root.TypeInfoPropertyName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidSourceGenerationOption,
                    model.ContextSymbol.Locations.FirstOrDefault(),
                    model.ContextSymbol.ToDisplayString(),
                    $"TomlSerializable TypeInfoPropertyName '{root.TypeInfoPropertyName}' must be a valid C# identifier."));
                return false;
            }
        }

        return true;
    }

    private static bool HasPolymorphismAttributes(INamedTypeSymbol type)
    {
        foreach (var attr in type.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attrName == "global::Tomlyn.Serialization.TomlPolymorphicAttribute" ||
                attrName == "global::System.Text.Json.Serialization.JsonPolymorphicAttribute" ||
                attrName == "global::Tomlyn.Serialization.TomlDerivedTypeAttribute" ||
                     attrName == "global::System.Text.Json.Serialization.JsonDerivedTypeAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPolymorphismConfiguration(INamedTypeSymbol type, ImmutableArray<DerivedTypeMappingModel> derivedTypeMappings)
    {
        if (HasPolymorphismAttributes(type))
        {
            return true;
        }

        foreach (var mapping in derivedTypeMappings)
        {
            if (SymbolEqualityComparer.Default.Equals(mapping.BaseType, type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSupportedMemberType(ITypeSymbol type, ImmutableArray<DerivedTypeMappingModel> derivedTypeMappings)
    {
        if (TryGetNullableUnderlyingType(type, out var nullableUnderlyingType))
        {
            return IsSupportedMemberType(nullableUnderlyingType, derivedTypeMappings);
        }

        // v1 generator milestone: built-in scalars/containers and POCOs.
        if (IsBuiltInType(type))
        {
            return true;
        }

        if (TryGetArrayElementType(type, out var arrayElementType))
        {
            return IsSupportedMemberType(arrayElementType, derivedTypeMappings);
        }

        if (TryGetSequenceElementType(type, out var enumerableElementType, out _))
        {
            return IsSupportedMemberType(enumerableElementType, derivedTypeMappings);
        }

        if (TryGetDictionaryKeyValueTypes(type, out var dictKeyType, out var dictValueType))
        {
            if (dictKeyType.SpecialType != SpecialType.System_String)
            {
                return false;
            }

            return IsSupportedMemberType(dictValueType, derivedTypeMappings);
        }

        if (type is INamedTypeSymbol named)
        {
            if (named.TypeKind == TypeKind.Struct)
            {
                return true;
            }

            if (named.TypeKind == TypeKind.Class)
            {
                return !named.IsAbstract || HasPolymorphismConfiguration(named, derivedTypeMappings);
            }

            if (named.TypeKind == TypeKind.Interface)
            {
                return HasPolymorphismConfiguration(named, derivedTypeMappings);
            }
        }

        return false;
    }

    private sealed class PocoMember
    {
        public PocoMember(string memberName, string serializedName, ITypeSymbol type, int order, WriteIgnoreKind writeIgnore, ObjectCreationHandlingKind objectCreationHandling, bool hasExplicitObjectCreationHandling, bool hasSingleOrArray, bool isRequired, bool isCompilerRequired, bool canSet, bool isInitOnly)
        {
            MemberName = memberName;
            SerializedName = serializedName;
            Type = type;
            Order = order;
            WriteIgnore = writeIgnore;
            ObjectCreationHandling = objectCreationHandling;
            HasExplicitObjectCreationHandling = hasExplicitObjectCreationHandling;
            HasSingleOrArray = hasSingleOrArray;
            IsRequired = isRequired;
            IsCompilerRequired = isCompilerRequired;
            CanSet = canSet;
            IsInitOnly = isInitOnly;
        }

        public string MemberName { get; }
        public string SerializedName { get; }
        public ITypeSymbol Type { get; }
        public int Order { get; }
        public WriteIgnoreKind WriteIgnore { get; }
        public ObjectCreationHandlingKind ObjectCreationHandling { get; }
        public bool HasExplicitObjectCreationHandling { get; }
        public bool HasSingleOrArray { get; }
        public bool IsRequired { get; }
        public bool IsCompilerRequired { get; }
        public bool CanSet { get; }
        public bool IsInitOnly { get; }
    }

    private sealed class PocoExtensionData
    {
        public PocoExtensionData(string memberName, ITypeSymbol memberType, ITypeSymbol valueType, string createExpression, bool canSet, bool isInitOnly, bool isCompilerRequired)
        {
            MemberName = memberName;
            MemberType = memberType;
            ValueType = valueType;
            CreateExpression = createExpression;
            CanSet = canSet;
            IsInitOnly = isInitOnly;
            IsCompilerRequired = isCompilerRequired;
        }

        public string MemberName { get; }
        public ITypeSymbol MemberType { get; }
        public ITypeSymbol ValueType { get; }
        public string CreateExpression { get; }
        public bool CanSet { get; }
        public bool IsInitOnly { get; }
        public bool IsCompilerRequired { get; }
    }

    private sealed class PocoConstructor
    {
        public PocoConstructor(IMethodSymbol? constructor, ImmutableArray<PocoConstructorParameter> parameters, string? errorMessage, bool setsRequiredMembers)
        {
            Constructor = constructor;
            Parameters = parameters;
            ErrorMessage = errorMessage;
            SetsRequiredMembers = setsRequiredMembers;
        }

        public IMethodSymbol? Constructor { get; }
        public ImmutableArray<PocoConstructorParameter> Parameters { get; }
        public string? ErrorMessage { get; }
        public bool SetsRequiredMembers { get; }
        public bool IsValid => ErrorMessage is null && Constructor is not null;
    }

    private readonly struct PocoConstructorParameter
    {
        public PocoConstructorParameter(
            string keyName,
            string parameterName,
            ITypeSymbol parameterType,
            bool hasDefaultValue,
            string? defaultValueExpression,
            int linkedMemberIndex)
        {
            KeyName = keyName;
            ParameterName = parameterName;
            ParameterType = parameterType;
            HasDefaultValue = hasDefaultValue;
            DefaultValueExpression = defaultValueExpression;
            LinkedMemberIndex = linkedMemberIndex;
        }

        public string KeyName { get; }
        public string ParameterName { get; }
        public ITypeSymbol ParameterType { get; }
        public bool HasDefaultValue { get; }
        public string? DefaultValueExpression { get; }
        public int LinkedMemberIndex { get; }
    }

    private sealed class PocoShape
    {
        public PocoShape(ImmutableArray<PocoMember> members, PocoExtensionData? extensionData, PocoConstructor? constructor, bool requiresGeneratedObjectInitializer)
        {
            Members = members;
            ExtensionData = extensionData;
            Constructor = constructor;
            RequiresGeneratedObjectInitializer = requiresGeneratedObjectInitializer;
        }

        public ImmutableArray<PocoMember> Members { get; }
        public PocoExtensionData? ExtensionData { get; }
        public PocoConstructor? Constructor { get; }
        public bool RequiresGeneratedObjectInitializer { get; }
    }

    private static bool RequiresGeneratedObjectInitializer(
        ImmutableArray<PocoMember> members,
        PocoExtensionData? extensionData,
        PocoConstructor? constructor,
        bool parameterlessConstructorSetsRequiredMembers)
    {
        var hasInitOnlyMembers = members.Any(static member => member.IsInitOnly) || extensionData?.IsInitOnly == true;
        if (hasInitOnlyMembers)
        {
            return true;
        }

        var hasCompilerRequiredMembers = members.Any(static member => member.IsCompilerRequired) || extensionData?.IsCompilerRequired == true;
        if (!hasCompilerRequiredMembers)
        {
            return false;
        }

        return !(constructor?.SetsRequiredMembers ?? parameterlessConstructorSetsRequiredMembers);
    }

    private sealed class PolymorphicDerivedType
    {
        public PolymorphicDerivedType(ITypeSymbol type, string? discriminator)
        {
            Type = type;
            Discriminator = discriminator;
        }

        public ITypeSymbol Type { get; }
        public string? Discriminator { get; }
    }

    private sealed class PolymorphicShape
    {
        public PolymorphicShape(string? discriminatorPropertyName, ImmutableArray<PolymorphicDerivedType> derivedTypes, ITypeSymbol? defaultDerivedType, int? unknownDerivedTypeHandlingOverride)
        {
            DiscriminatorPropertyName = discriminatorPropertyName;
            DerivedTypes = derivedTypes;
            DefaultDerivedType = defaultDerivedType;
            UnknownDerivedTypeHandlingOverride = unknownDerivedTypeHandlingOverride;
        }

        public string? DiscriminatorPropertyName { get; }
        public ImmutableArray<PolymorphicDerivedType> DerivedTypes { get; }
        public ITypeSymbol? DefaultDerivedType { get; }
        /// <summary>
        /// Resolved unknown handling value (as int matching TomlUnknownDerivedTypeHandling enum), or null to use options default.
        /// </summary>
        public int? UnknownDerivedTypeHandlingOverride { get; }
    }

    private enum WriteIgnoreKind
    {
        None = 0,
        WhenWritingNull = 1,
        WhenWritingDefault = 2,
    }

    private enum ObjectCreationHandlingKind
    {
        Default = 0,
        Replace = 1,
        Populate = 2,
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

    private static bool TryGetPocoShape(SourceProductionContext context, ContextModel model, ITypeSymbol type, out PocoShape shape)
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

        IMethodSymbol? selectedConstructor = null;
        string? constructorError = null;
        var parameterlessConstructorSetsRequiredMembers = false;
        if (named.TypeKind == TypeKind.Class)
        {
            var publicConstructors = named.InstanceConstructors
                .Where(static ctor => !ctor.IsStatic && ctor.DeclaredAccessibility == Accessibility.Public)
                .ToArray();

            if (publicConstructors.Length == 0)
            {
                constructorError = "No suitable constructor was found for deserialization.";
            }
            else
            {
                var annotated = publicConstructors
                    .Where(static ctor =>
                        HasAttribute(ctor, "Tomlyn.Serialization.TomlConstructorAttribute") ||
                        HasAttribute(ctor, "System.Text.Json.Serialization.JsonConstructorAttribute"))
                    .ToArray();

                if (annotated.Length > 1)
                {
                    constructorError = "Multiple constructors were annotated for deserialization.";
                    selectedConstructor = annotated[0];
                }
                else if (annotated.Length == 1)
                {
                    selectedConstructor = annotated[0];
                }
                else if (publicConstructors.FirstOrDefault(static ctor => ctor.Parameters.Length == 0) is { } parameterlessConstructor)
                {
                    selectedConstructor = null;
                    parameterlessConstructorSetsRequiredMembers = HasAttribute(parameterlessConstructor, SetsRequiredMembersAttributeMetadataName);
                }
                else if (publicConstructors.Length == 1)
                {
                    selectedConstructor = publicConstructors[0];
                }
                else
                {
                    constructorError = "No suitable constructor was found for deserialization.";
                    selectedConstructor = publicConstructors[0];
                }
            }
        }

        var namingPolicy = model.Options.PropertyNamingPolicyExpression;
        var typeObjectCreationHandling = GetObjectCreationHandling(named);

        var members = ImmutableArray.CreateBuilder<PocoMember>();
        PocoExtensionData? extensionData = null;
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

            if (member.GetMethod is null)
            {
                continue;
            }

            var canSet = member.SetMethod is not null && member.SetMethod.DeclaredAccessibility == Accessibility.Public;
            var isInitOnly = member.SetMethod?.IsInitOnly == true;
            var isCompilerRequired = member.IsRequired;
            var hasSingleOrArray = HasAttribute(member, TomlSingleOrArrayAttributeMetadataName);
            var memberObjectCreationHandling = GetObjectCreationHandling(member);
            var hasExplicitObjectCreationHandling = memberObjectCreationHandling != ObjectCreationHandlingKind.Default;
            var objectCreationHandling = memberObjectCreationHandling;
            if (objectCreationHandling == ObjectCreationHandlingKind.Default)
            {
                objectCreationHandling = typeObjectCreationHandling;
            }

            if (IsExtensionData(member))
            {
                if (extensionData is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidExtensionDataMember,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        member.Name,
                        "Multiple extension data members were found. Only a single member can be annotated with [TomlExtensionData]/[JsonExtensionData]."));
                    continue;
                }

                if (!TryGetExtensionDataValueType(member.Type, out var extensionValueType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidExtensionDataMember,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        member.Name,
                        "Extension data members must be dictionary-like with string keys (for example IDictionary<string, object> or TomlTable)."));
                    continue;
                }

                if (!TryGetExtensionDataCreateExpression(member.Type, extensionValueType, out var createExpression))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidExtensionDataMember,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        member.Name,
                        "Extension data members must be instantiable (public parameterless constructor) or use an interface type such as IDictionary<string, TValue>."));
                    continue;
                }

                extensionData = new PocoExtensionData(member.Name, member.Type, extensionValueType, createExpression, canSet, isInitOnly, isCompilerRequired);
                continue;
            }

            var ignore = GetIgnoreBehavior(member);
            if (ignore.IgnoreAlways)
            {
                continue;
            }

            var serializedName = GetSerializedName(member, member.Name, namingPolicy);
            var order = GetOrder(member);
            var required = IsRequired(member);
            members.Add(new PocoMember(member.Name, serializedName, member.Type, order, ignore.WriteIgnore, objectCreationHandling, hasExplicitObjectCreationHandling, hasSingleOrArray, required, isCompilerRequired, canSet, isInitOnly));
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

            if (IsExtensionData(member))
            {
                if (extensionData is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidExtensionDataMember,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        member.Name,
                        "Multiple extension data members were found. Only a single member can be annotated with [TomlExtensionData]/[JsonExtensionData]."));
                    continue;
                }

                if (!TryGetExtensionDataValueType(member.Type, out var extensionValueType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidExtensionDataMember,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        member.Name,
                        "Extension data members must be dictionary-like with string keys (for example IDictionary<string, object> or TomlTable)."));
                    continue;
                }

                if (!TryGetExtensionDataCreateExpression(member.Type, extensionValueType, out var createExpression))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidExtensionDataMember,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        member.Name,
                        "Extension data members must be instantiable (public parameterless constructor) or use an interface type such as IDictionary<string, TValue>."));
                    continue;
                }

                extensionData = new PocoExtensionData(member.Name, member.Type, extensionValueType, createExpression, canSet: true, isInitOnly: false, isCompilerRequired: member.IsRequired);
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

            var hasSingleOrArray = HasAttribute(member, TomlSingleOrArrayAttributeMetadataName);
            var memberObjectCreationHandling = GetObjectCreationHandling(member);
            var hasExplicitObjectCreationHandling = memberObjectCreationHandling != ObjectCreationHandlingKind.Default;
            var objectCreationHandling = memberObjectCreationHandling;
            if (objectCreationHandling == ObjectCreationHandlingKind.Default)
            {
                objectCreationHandling = typeObjectCreationHandling;
            }

            var serializedName = GetSerializedName(member, member.Name, namingPolicy);
            var order = GetOrder(member);
            var required = IsRequired(member);
            members.Add(new PocoMember(member.Name, serializedName, member.Type, order, ignore.WriteIgnore, objectCreationHandling, hasExplicitObjectCreationHandling, hasSingleOrArray, required, member.IsRequired, canSet: true, isInitOnly: false));
        }

        PocoConstructor? constructorModel = null;
        if (constructorError is not null && selectedConstructor is null)
        {
            constructorModel = new PocoConstructor(null, ImmutableArray<PocoConstructorParameter>.Empty, constructorError, setsRequiredMembers: false);
        }
        else if (selectedConstructor is not null)
        {
            var setsRequiredMembers = HasAttribute(selectedConstructor, SetsRequiredMembersAttributeMetadataName);
            if (selectedConstructor.Parameters.Length > 0)
            {
                var memberIndexByClrName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var membersSoFar = members.ToImmutable();
                for (var i = 0; i < membersSoFar.Length; i++)
                {
                    memberIndexByClrName[membersSoFar[i].MemberName] = i;
                }

                var parameterComparer = model.Options.PropertyNameCaseInsensitive == true ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
                var parameterKeys = new HashSet<string>(parameterComparer);
                var parameters = ImmutableArray.CreateBuilder<PocoConstructorParameter>(selectedConstructor.Parameters.Length);

                for (var i = 0; i < selectedConstructor.Parameters.Length; i++)
                {
                    var parameter = selectedConstructor.Parameters[i];
                    var parameterName = string.IsNullOrEmpty(parameter.Name) ? $"arg{i}" : parameter.Name;

                    var linkedMemberIndex = memberIndexByClrName.TryGetValue(parameterName, out var linkedIndex) ? linkedIndex : -1;
                    var keyName = linkedMemberIndex >= 0
                        ? membersSoFar[linkedMemberIndex].SerializedName
                        : GetConstructorParameterKeyName(parameterName, namingPolicy);

                    if (!parameterKeys.Add(keyName))
                    {
                        constructorError ??= $"Constructor parameter name collision for key '{keyName}' on type '{type.ToDisplayString()}'.";
                    }

                    var hasDefaultValue = parameter.HasExplicitDefaultValue;
                    string? defaultValueExpression = null;
                    if (hasDefaultValue && !TryGetDefaultValueExpression(parameter.Type, parameter.ExplicitDefaultValue, out defaultValueExpression))
                    {
                        constructorError ??= $"Constructor parameter '{parameterName}' on type '{type.ToDisplayString()}' has an unsupported default value.";
                        hasDefaultValue = false;
                    }

                    parameters.Add(new PocoConstructorParameter(keyName, parameterName, parameter.Type, hasDefaultValue, defaultValueExpression, linkedMemberIndex));
                }

                constructorModel = new PocoConstructor(selectedConstructor, parameters.ToImmutable(), constructorError, setsRequiredMembers);
                shape = new PocoShape(
                    membersSoFar,
                    extensionData,
                    constructorModel,
                    RequiresGeneratedObjectInitializer(membersSoFar, extensionData, constructorModel, parameterlessConstructorSetsRequiredMembers));
                return true;
            }

            constructorModel = new PocoConstructor(selectedConstructor, ImmutableArray<PocoConstructorParameter>.Empty, constructorError, setsRequiredMembers);
        }

        var finalMembers = members.ToImmutable();
        shape = new PocoShape(
            finalMembers,
            extensionData,
            constructorModel,
            RequiresGeneratedObjectInitializer(finalMembers, extensionData, constructorModel, parameterlessConstructorSetsRequiredMembers));
        return true;
    }

    private static bool TryGetPolymorphicShape(
        SourceProductionContext context,
        ContextModel model,
        ImmutableArray<DerivedTypeMappingModel> derivedTypeMappings,
        ITypeSymbol type,
        out PolymorphicShape shape,
        bool reportDiagnostics = true)
    {
        shape = null!;

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.TypeKind is not (TypeKind.Class or TypeKind.Interface))
        {
            return false;
        }

        string? tomlDiscriminatorPropertyName = null;
        string? jsonDiscriminatorPropertyName = null;
        int? tomlUnknownHandling = null;
        int? jsonUnknownHandling = null;

        foreach (var attr in named.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attrName == "global::Tomlyn.Serialization.TomlPolymorphicAttribute")
            {
                foreach (var kvp in attr.NamedArguments)
                {
                    if (kvp.Key == "TypeDiscriminatorPropertyName" && kvp.Value.Value is string s && !string.IsNullOrEmpty(s))
                    {
                        tomlDiscriminatorPropertyName = s;
                    }
                    else if (kvp.Key == "UnknownDerivedTypeHandling" && kvp.Value.Value is int intVal)
                    {
                        // -1 = Unspecified, 0 = Fail, 1 = FallBackToBaseType
                        if (intVal != -1)
                        {
                            tomlUnknownHandling = intVal;
                        }
                    }
                }
            }
            else if (attrName == "global::System.Text.Json.Serialization.JsonPolymorphicAttribute")
            {
                foreach (var kvp in attr.NamedArguments)
                {
                    if (kvp.Key == "TypeDiscriminatorPropertyName" && kvp.Value.Value is string s && !string.IsNullOrEmpty(s))
                    {
                        jsonDiscriminatorPropertyName = s;
                    }
                    else if (kvp.Key == "UnknownDerivedTypeHandling" && kvp.Value.Value is int intVal)
                    {
                        // JsonUnknownDerivedTypeHandling: FailSerialization=0, FallBackToBaseType=1, FallBackToNearestAncestor=2
                        jsonUnknownHandling = intVal == 1 ? 1 : 0; // 1 => FallBackToBaseType, anything else => Fail
                    }
                }
            }
        }

        var derived = ImmutableArray.CreateBuilder<PolymorphicDerivedType>();
        var discriminatorSet = new HashSet<string>(StringComparer.Ordinal);
        var derivedTypeSet = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        ITypeSymbol? defaultDerivedType = null;

        foreach (var attr in named.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attrName == "global::Tomlyn.Serialization.TomlDerivedTypeAttribute")
            {
                if (attr.ConstructorArguments.Length == 1 &&
                    attr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    attr.ConstructorArguments[0].Value is ITypeSymbol defaultType)
                {
                    // TomlDerivedTypeAttribute(Type) - default derived type (no discriminator)
                    if (defaultDerivedType is not null)
                    {
                        if (reportDiagnostics)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                InvalidPolymorphismConfiguration,
                                model.ContextSymbol.Locations.FirstOrDefault(),
                                type.ToDisplayString(),
                                "Only one default derived type (no discriminator) can be registered."));
                        }
                        continue;
                    }

                    if (!ValidateDerivedType(defaultType, discriminator: null))
                    {
                        continue;
                    }

                    if (!derivedTypeSet.Add(defaultType))
                    {
                        if (reportDiagnostics)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                InvalidPolymorphismConfiguration,
                                model.ContextSymbol.Locations.FirstOrDefault(),
                                type.ToDisplayString(),
                                $"Derived type '{defaultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' is registered more than once."));
                        }
                        continue;
                    }

                    defaultDerivedType = defaultType;
                }
                else if (attr.ConstructorArguments.Length == 2 &&
                    attr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    attr.ConstructorArguments[0].Value is ITypeSymbol derivedType)
                {
                    // TomlDerivedTypeAttribute(Type, string) or TomlDerivedTypeAttribute(Type, int)
                    var discriminatorObj = attr.ConstructorArguments[1].Value;
                    var discriminator = discriminatorObj switch
                    {
                        string s => s,
                        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                        _ => discriminatorObj?.ToString() ?? string.Empty,
                    };

                    if (string.IsNullOrEmpty(discriminator))
                    {
                        if (reportDiagnostics)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                InvalidPolymorphismConfiguration,
                                model.ContextSymbol.Locations.FirstOrDefault(),
                                type.ToDisplayString(),
                            "TomlDerivedTypeAttribute must specify a derived type and a non-empty discriminator."));
                        }
                        continue;
                    }

                    AddStrictDerivedType(derivedType, discriminator);
                }
                else
                {
                    if (reportDiagnostics)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            InvalidPolymorphismConfiguration,
                            model.ContextSymbol.Locations.FirstOrDefault(),
                            type.ToDisplayString(),
                            "TomlDerivedTypeAttribute must specify a derived type and an optional discriminator."));
                    }
                    continue;
                }
            }
            else if (attrName == "global::System.Text.Json.Serialization.JsonDerivedTypeAttribute")
            {
                if (attr.ConstructorArguments.Length < 1 ||
                    attr.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
                    attr.ConstructorArguments[0].Value is not ITypeSymbol derivedType)
                {
                    if (reportDiagnostics)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            InvalidPolymorphismConfiguration,
                            model.ContextSymbol.Locations.FirstOrDefault(),
                            type.ToDisplayString(),
                            "JsonDerivedTypeAttribute must specify a derived type."));
                    }
                    continue;
                }

                if (attr.ConstructorArguments.Length < 2 || attr.ConstructorArguments[1].IsNull)
                {
                    TryAddLowerPrecedenceDefaultDerivedType(derivedType);
                    continue;
                }

                var discriminatorObj = attr.ConstructorArguments[1].Value;
                var discriminator = discriminatorObj switch
                {
                    string s => s,
                    IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                    _ => discriminatorObj?.ToString() ?? string.Empty,
                };

                if (string.IsNullOrEmpty(discriminator))
                {
                    continue;
                }

                TryAddLowerPrecedenceDerivedType(derivedType, discriminator);
            }
        }

        foreach (var mapping in derivedTypeMappings)
        {
            if (!SymbolEqualityComparer.Default.Equals(mapping.BaseType, type))
            {
                continue;
            }

            if (mapping.Discriminator is null)
            {
                TryAddLowerPrecedenceDefaultDerivedType(mapping.DerivedType);
            }
            else
            {
                TryAddLowerPrecedenceDerivedType(mapping.DerivedType, mapping.Discriminator);
            }
        }

        if (derived.Count == 0 && defaultDerivedType is null)
        {
            return false;
        }

        var discriminatorPropertyName = tomlDiscriminatorPropertyName ?? jsonDiscriminatorPropertyName;
        // Priority chain: TomlPolymorphicAttribute → JsonPolymorphicAttribute → options (null = use options)
        int? resolvedUnknownHandling = tomlUnknownHandling ?? jsonUnknownHandling;
        shape = new PolymorphicShape(discriminatorPropertyName, derived.ToImmutable(), defaultDerivedType, resolvedUnknownHandling);
        return true;

        void AddStrictDerivedType(ITypeSymbol derivedType, string discriminator)
        {
            if (!discriminatorSet.Add(discriminator))
            {
                if (reportDiagnostics)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidPolymorphismConfiguration,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        $"Discriminator '{discriminator}' is registered more than once."));
                }
                return;
            }

            if (!derivedTypeSet.Add(derivedType))
            {
                if (reportDiagnostics)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidPolymorphismConfiguration,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        $"Derived type '{derivedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' is registered more than once."));
                }
                return;
            }

            if (!ValidateDerivedType(derivedType, discriminator))
            {
                return;
            }

            derived.Add(new PolymorphicDerivedType(derivedType, discriminator));
        }

        void TryAddLowerPrecedenceDefaultDerivedType(ITypeSymbol derivedType)
        {
            if (!ValidateDerivedType(derivedType, discriminator: null))
            {
                return;
            }

            if (!CanAddLowerPrecedenceMapping(derivedType, discriminator: null, defaultDerivedType, discriminatorSet, derivedTypeSet))
            {
                return;
            }

            defaultDerivedType ??= derivedType;
            derivedTypeSet.Add(derivedType);
        }

        void TryAddLowerPrecedenceDerivedType(ITypeSymbol derivedType, string discriminator)
        {
            if (!ValidateDerivedType(derivedType, discriminator))
            {
                return;
            }

            if (!CanAddLowerPrecedenceMapping(derivedType, discriminator, defaultDerivedType, discriminatorSet, derivedTypeSet))
            {
                return;
            }

            derivedTypeSet.Add(derivedType);
            discriminatorSet.Add(discriminator);
            derived.Add(new PolymorphicDerivedType(derivedType, discriminator));
        }

        bool ValidateDerivedType(ITypeSymbol derivedType, string? discriminator)
        {
            if (discriminator is not null && discriminator.Length == 0)
            {
                if (reportDiagnostics)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidPolymorphismConfiguration,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        "Derived type discriminators cannot be empty."));
                }

                return false;
            }

            if (!IsAssignableTo(derivedType, type))
            {
                if (reportDiagnostics)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidPolymorphismConfiguration,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        $"Derived type '{derivedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' is not assignable to base type '{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}'."));
                }

                return false;
            }

            if (derivedType is INamedTypeSymbol derivedNamed && (derivedNamed.TypeKind != TypeKind.Class || derivedNamed.IsAbstract))
            {
                if (reportDiagnostics)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidPolymorphismConfiguration,
                        model.ContextSymbol.Locations.FirstOrDefault(),
                        type.ToDisplayString(),
                        $"Derived type '{derivedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}' must be a non-abstract class."));
                }

                return false;
            }

            return true;
        }
    }

    private static bool CanAddLowerPrecedenceMapping(
        ITypeSymbol derivedType,
        string? discriminator,
        ITypeSymbol? defaultDerivedType,
        HashSet<string> discriminatorSet,
        HashSet<ITypeSymbol> derivedTypeSet)
    {
        if (derivedTypeSet.Contains(derivedType))
        {
            return false;
        }

        if (discriminator is null)
        {
            return defaultDerivedType is null;
        }

        return !discriminatorSet.Contains(discriminator);
    }

    private static bool IsAssignableTo(ITypeSymbol derivedType, ITypeSymbol baseType)
    {
        if (SymbolEqualityComparer.Default.Equals(derivedType, baseType))
        {
            return true;
        }

        if (derivedType is not INamedTypeSymbol named)
        {
            return false;
        }

        for (var current = named.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        foreach (var iface in named.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRequired(ISymbol member)
    {
        return member switch
               {
                   IPropertySymbol property when property.IsRequired => true,
                   IFieldSymbol field when field.IsRequired => true,
                   _ => false,
               } ||
               HasAttribute(member, "Tomlyn.Serialization.TomlRequiredAttribute") ||
               HasAttribute(member, "System.Text.Json.Serialization.JsonRequiredAttribute");
    }

    private static bool IsExtensionData(ISymbol member)
    {
        return HasAttribute(member, "Tomlyn.Serialization.TomlExtensionDataAttribute") ||
               HasAttribute(member, "System.Text.Json.Serialization.JsonExtensionDataAttribute");
    }

    private static bool TryGetExtensionDataValueType(ITypeSymbol type, out ITypeSymbol valueType)
    {
        valueType = null!;

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        static bool IsStringDictionaryInterface(INamedTypeSymbol iface, out ITypeSymbol dictionaryValueType)
        {
            dictionaryValueType = null!;
            if (!iface.IsGenericType)
            {
                return false;
            }

            if (iface.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::System.Collections.Generic.IDictionary<TKey, TValue>")
            {
                return false;
            }

            if (iface.TypeArguments[0].SpecialType != SpecialType.System_String)
            {
                return false;
            }

            dictionaryValueType = iface.TypeArguments[1];
            return true;
        }

        if (IsStringDictionaryInterface(named, out var directValueType))
        {
            valueType = directValueType;
            return true;
        }

        foreach (var iface in named.AllInterfaces)
        {
            if (IsStringDictionaryInterface(iface, out var ifaceValueType))
            {
                valueType = ifaceValueType;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetExtensionDataCreateExpression(ITypeSymbol memberType, ITypeSymbol valueType, out string createExpression)
    {
        createExpression = null!;

        var memberTypeNoOuterNullability = memberType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        var memberTypeNameNoNullability = memberTypeNoOuterNullability.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var memberTypeName = memberTypeNoOuterNullability.ToDisplayString(FullyQualifiedNullableFormat);
        if (memberTypeNameNoNullability == "global::Tomlyn.Model.TomlTable")
        {
            createExpression = "new global::Tomlyn.Model.TomlTable()";
            return true;
        }

        if (memberTypeNoOuterNullability is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.TypeKind is (TypeKind.Class or TypeKind.Struct) &&
            !named.IsAbstract &&
            named.TypeKind != TypeKind.Interface)
        {
            if (named.TypeKind == TypeKind.Struct ||
                named.Constructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
            {
                createExpression = "new " + memberTypeName + "()";
                return true;
            }
        }

        if (named.TypeKind == TypeKind.Interface && TryGetExtensionDataValueType(memberTypeNoOuterNullability, out _))
        {
            var valueTypeName = valueType.ToDisplayString(FullyQualifiedNullableFormat);
            createExpression = $"new global::System.Collections.Generic.Dictionary<string, {valueTypeName}>()";
            return true;
        }

        return false;
    }

    private static string GetConstructorParameterKeyName(string parameterName, string? namingPolicyExpression)
    {
        if (namingPolicyExpression is not null && TryConvertKnownName(parameterName, namingPolicyExpression, out var converted))
        {
            return converted;
        }

        return parameterName;
    }

    private static bool TryGetDefaultValueExpression(ITypeSymbol type, object? value, out string expression)
    {
        expression = null!;

        if (value is null)
        {
            if (type.IsReferenceType || TryGetNullableUnderlyingType(type, out _))
            {
                expression = "null";
                return true;
            }

            return false;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            var enumTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (value is int i32)
            {
                expression = $"({enumTypeName}){i32.ToString(CultureInfo.InvariantCulture)}";
                return true;
            }

            if (value is byte b)
            {
                expression = $"({enumTypeName}){b.ToString(CultureInfo.InvariantCulture)}";
                return true;
            }

            if (value is short i16)
            {
                expression = $"({enumTypeName}){i16.ToString(CultureInfo.InvariantCulture)}";
                return true;
            }

            if (value is long i64)
            {
                expression = $"({enumTypeName}){i64.ToString(CultureInfo.InvariantCulture)}L";
                return true;
            }

            return false;
        }

        if (type.SpecialType == SpecialType.System_String && value is string s)
        {
            expression = $"\"{EscapeStringLiteral(s)}\"";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Boolean && value is bool bval)
        {
            expression = bval ? "true" : "false";
            return true;
        }

        if (type.SpecialType == SpecialType.System_Char && value is char c)
        {
            expression = c switch
            {
                '\'' => "'\\''",
                '\\' => "'\\\\'",
                '\n' => "'\\n'",
                '\r' => "'\\r'",
                '\t' => "'\\t'",
                _ => c >= ' ' && c <= '~' ? $"'{c}'" : $"'\\u{((int)c).ToString("X4", CultureInfo.InvariantCulture)}'",
            };
            return true;
        }

        switch (value)
        {
            case sbyte sb:
                expression = sb.ToString(CultureInfo.InvariantCulture);
                return true;
            case byte bb:
                expression = bb.ToString(CultureInfo.InvariantCulture);
                return true;
            case short sh:
                expression = sh.ToString(CultureInfo.InvariantCulture);
                return true;
            case ushort ush:
                expression = ush.ToString(CultureInfo.InvariantCulture);
                return true;
            case int ii:
                expression = ii.ToString(CultureInfo.InvariantCulture);
                return true;
            case uint ui:
                expression = ui.ToString(CultureInfo.InvariantCulture) + "u";
                return true;
            case long ll:
                expression = ll.ToString(CultureInfo.InvariantCulture) + "L";
                return true;
            case ulong ull:
                expression = ull.ToString(CultureInfo.InvariantCulture) + "UL";
                return true;
            case float ff:
                if (float.IsNaN(ff))
                {
                    expression = "float.NaN";
                }
                else if (float.IsPositiveInfinity(ff))
                {
                    expression = "float.PositiveInfinity";
                }
                else if (float.IsNegativeInfinity(ff))
                {
                    expression = "float.NegativeInfinity";
                }
                else
                {
                    expression = ff.ToString("R", CultureInfo.InvariantCulture) + "f";
                }
                return true;
            case double dd:
                if (double.IsNaN(dd))
                {
                    expression = "double.NaN";
                }
                else if (double.IsPositiveInfinity(dd))
                {
                    expression = "double.PositiveInfinity";
                }
                else if (double.IsNegativeInfinity(dd))
                {
                    expression = "double.NegativeInfinity";
                }
                else
                {
                    expression = dd.ToString("R", CultureInfo.InvariantCulture);
                }
                return true;
            case decimal dec:
                expression = dec.ToString(CultureInfo.InvariantCulture) + "m";
                return true;
        }

        return false;
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

    private static ObjectCreationHandlingKind GetObjectCreationHandling(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::" + JsonObjectCreationHandlingAttributeMetadataName)
            {
                continue;
            }

            if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is int constructorValue)
            {
                return constructorValue switch
                {
                    0 => ObjectCreationHandlingKind.Replace,
                    1 => ObjectCreationHandlingKind.Populate,
                    _ => ObjectCreationHandlingKind.Default,
                };
            }

            foreach (var namedArgument in attr.NamedArguments)
            {
                if (namedArgument.Key == "Handling" && namedArgument.Value.Value is int namedValue)
                {
                    return namedValue switch
                    {
                        0 => ObjectCreationHandlingKind.Replace,
                        1 => ObjectCreationHandlingKind.Populate,
                        _ => ObjectCreationHandlingKind.Default,
                    };
                }
            }
        }

        return ObjectCreationHandlingKind.Default;
    }

    private static int GetOrder(ISymbol member)
    {
        int? tomlOrder = null;
        int? jsonOrder = null;

        foreach (var attr in member.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attrName == "global::Tomlyn.Serialization.TomlPropertyOrderAttribute" &&
                attr.ConstructorArguments.Length == 1 &&
                attr.ConstructorArguments[0].Value is int toml)
            {
                tomlOrder = toml;
            }
            else if (attrName == "global::System.Text.Json.Serialization.JsonPropertyOrderAttribute" &&
                     attr.ConstructorArguments.Length == 1 &&
                     attr.ConstructorArguments[0].Value is int json)
            {
                jsonOrder = json;
            }
        }

        return tomlOrder ?? jsonOrder ?? 0;
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

    private static bool TryGetCustomTypeInfoPropertyName(ContextModel model, ITypeSymbol type, out string propertyName)
    {
        foreach (var root in model.RootTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(root.Type, type) && root.TypeInfoPropertyName is not null)
            {
                propertyName = root.TypeInfoPropertyName;
                return true;
            }
        }

        propertyName = null!;
        return false;
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

    private static bool IsTomlSerializableAttribute(AttributeData attribute)
        => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + TomlSerializableAttributeMetadataName;

    private static bool TryCreateDerivedTypeMappingModel(AttributeData attribute, out DerivedTypeMappingModel model)
    {
        model = null!;

        if (attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::" + TomlDerivedTypeMappingAttributeMetadataName)
        {
            return false;
        }

        if (attribute.ConstructorArguments.Length < 2 ||
            attribute.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
            attribute.ConstructorArguments[0].Value is not ITypeSymbol baseType ||
            attribute.ConstructorArguments[1].Kind != TypedConstantKind.Type ||
            attribute.ConstructorArguments[1].Value is not ITypeSymbol derivedType)
        {
            return false;
        }

        string? discriminator = null;
        if (attribute.ConstructorArguments.Length >= 3)
        {
            discriminator = attribute.ConstructorArguments[2].Value switch
            {
                string s => s,
                int i => i.ToString(CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => attribute.ConstructorArguments[2].Value?.ToString(),
            };
        }

        model = new DerivedTypeMappingModel(baseType, derivedType, discriminator, attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation());
        return true;
    }

    private static string? GetTypeInfoPropertyNameOverride(AttributeData attribute)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key == "TypeInfoPropertyName" && namedArgument.Value.Value is string propertyName)
            {
                return propertyName;
            }
        }

        return null;
    }

    private static bool IsJsonSourceGenerationOptionsAttribute(AttributeData attribute)
        => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + JsonSourceGenerationOptionsAttributeMetadataName;

    private static bool IsTomlSourceGenerationOptionsAttribute(AttributeData attribute)
        => attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + TomlSourceGenerationOptionsAttributeMetadataName;

    private static ImmutableArray<DerivedTypeMappingModel> ValidateDerivedTypeMappings(SourceProductionContext context, ContextModel model)
    {
        var builder = ImmutableArray.CreateBuilder<DerivedTypeMappingModel>(model.DerivedTypeMappings.Length);
        var warnedBaseTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var mapping in model.DerivedTypeMappings)
        {
            if (mapping.Discriminator is { Length: 0 })
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidDerivedTypeMapping,
                    mapping.Location ?? model.ContextSymbol.Locations.FirstOrDefault(),
                    model.ContextSymbol.ToDisplayString(),
                    $"Derived type mapping from '{mapping.BaseType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' to '{mapping.DerivedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' must use a non-empty discriminator."));
                continue;
            }

            if (!IsAssignableTo(mapping.DerivedType, mapping.BaseType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidDerivedTypeMapping,
                    mapping.Location ?? model.ContextSymbol.Locations.FirstOrDefault(),
                    model.ContextSymbol.ToDisplayString(),
                    $"Type '{mapping.DerivedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' is not assignable to base type '{mapping.BaseType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}'."));
                continue;
            }

            if (mapping.DerivedType is not INamedTypeSymbol derivedType ||
                derivedType.TypeKind != TypeKind.Class ||
                derivedType.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidDerivedTypeMapping,
                    mapping.Location ?? model.ContextSymbol.Locations.FirstOrDefault(),
                    model.ContextSymbol.ToDisplayString(),
                    $"Derived type '{mapping.DerivedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}' must be a non-abstract class."));
                continue;
            }

            if (warnedBaseTypes.Add(mapping.BaseType) &&
                mapping.BaseType is INamedTypeSymbol baseType &&
                !HasPolymorphismAttributes(baseType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingPolymorphicConfigurationOnDerivedTypeMappingBase,
                    mapping.Location ?? model.ContextSymbol.Locations.FirstOrDefault(),
                    model.ContextSymbol.ToDisplayString(),
                    mapping.BaseType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }

            builder.Add(mapping);
        }

        return builder.ToImmutable();
    }

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
                case "PreferredObjectCreationHandling":
                    if (options.PreferredObjectCreationHandling is null && value.Value is int objectCreationHandling) options.PreferredObjectCreationHandling = objectCreationHandling;
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
                case "PreferredObjectCreationHandling":
                    if (value.Value is int preferredObjectCreationHandling) options.PreferredObjectCreationHandling = preferredObjectCreationHandling;
                    break;
                case "DefaultIgnoreCondition":
                    if (value.Value is int dic) options.DefaultIgnoreCondition = dic;
                    break;
                case "DuplicateKeyHandling":
                    if (value.Value is int dkh) options.DuplicateKeyHandling = dkh;
                    break;
                case "MaxDepth":
                    if (value.Value is int maxDepth) options.MaxDepth = maxDepth;
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

        ValidateEnumOption(context, model, "PreferredObjectCreationHandling", model.Options.PreferredObjectCreationHandling, TryGetJsonObjectCreationHandlingExpression, v => model.Options.PreferredObjectCreationHandling = v);
        ValidateEnumOption(context, model, "NewLine", model.Options.NewLine, TryGetTomlNewLineKindExpression, v => model.Options.NewLine = v);
        ValidateEnumOption(context, model, "DefaultIgnoreCondition", model.Options.DefaultIgnoreCondition, TryGetTomlIgnoreConditionExpression, v => model.Options.DefaultIgnoreCondition = v);
        ValidateEnumOption(context, model, "DuplicateKeyHandling", model.Options.DuplicateKeyHandling, TryGetTomlDuplicateKeyHandlingExpression, v => model.Options.DuplicateKeyHandling = v);
        if (model.Options.MaxDepth is not null && model.Options.MaxDepth.Value < 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidSourceGenerationOption,
                model.ContextSymbol.Locations.FirstOrDefault(),
                model.ContextSymbol.ToDisplayString(),
                "MaxDepth must be greater than or equal to 0."));
            model.Options.MaxDepth = null;
        }

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

    private static bool TryGetJsonObjectCreationHandlingExpression(int value, out string expression)
    {
        expression = value switch
        {
            0 => "global::System.Text.Json.Serialization.JsonObjectCreationHandling.Replace",
            1 => "global::System.Text.Json.Serialization.JsonObjectCreationHandling.Populate",
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
