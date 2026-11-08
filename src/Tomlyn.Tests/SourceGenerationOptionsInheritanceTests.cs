using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class InheritedOptionsModel
{
    public string DisplayName { get; set; } = "app";

    public List<int> PortNumbers { get; set; } = [80, 443];
}

// Issue #133: options shared by several contexts are declared once on a base context.
[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, ArrayLayout = TomlArrayLayout.Multiline)]
public abstract class SharedOptionsContextBase : TomlSerializerContext
{
    protected SharedOptionsContextBase(TomlSerializerOptions options) : base(options)
    {
    }
}

public abstract class IntermediateSharedOptionsContextBase : SharedOptionsContextBase
{
    protected IntermediateSharedOptionsContextBase(TomlSerializerOptions options) : base(options)
    {
    }
}

[TomlSerializable(typeof(InheritedOptionsModel))]
internal partial class InheritingOptionsContext : SharedOptionsContextBase
{
}

[TomlSerializable(typeof(InheritedOptionsModel))]
internal partial class IndirectlyInheritingOptionsContext : IntermediateSharedOptionsContextBase
{
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[TomlSerializable(typeof(InheritedOptionsModel))]
internal partial class OverridingOptionsContext : SharedOptionsContextBase
{
}

public class SourceGenerationOptionsInheritanceTests
{
    [Test]
    public void DerivedContext_UsesOptionsOfBaseContext()
    {
        var context = InheritingOptionsContext.Default;

        var toml = TomlSerializer.Serialize(new InheritedOptionsModel(), context.InheritedOptionsModel);

        Assert.That(context.Options.PropertyNamingPolicy, Is.SameAs(JsonNamingPolicy.SnakeCaseLower));
        Assert.That(context.Options.ArrayLayout, Is.EqualTo(TomlArrayLayout.Multiline));
        Assert.That(toml, Is.EqualTo("display_name = \"app\"\nport_numbers = [\n  80,\n  443,\n]\n"));
    }

    [Test]
    public void IndirectlyDerivedContext_UsesOptionsOfBaseContext()
    {
        var context = IndirectlyInheritingOptionsContext.Default;

        var toml = TomlSerializer.Serialize(new InheritedOptionsModel(), context.InheritedOptionsModel);

        Assert.That(toml, Does.StartWith("display_name = \"app\"\nport_numbers = [\n"));
    }

    [Test]
    public void DerivedContext_OwnOptionsOverrideBaseOptions()
    {
        var context = OverridingOptionsContext.Default;

        var toml = TomlSerializer.Serialize(new InheritedOptionsModel(), context.InheritedOptionsModel);
        var roundtrip = TomlSerializer.Deserialize(toml, context.InheritedOptionsModel);

        // The naming policy is overridden, the array style is still inherited from the base context.
        Assert.That(toml, Is.EqualTo("displayName = \"app\"\nportNumbers = [\n  80,\n  443,\n]\n"));
        Assert.That(roundtrip!.PortNumbers, Is.EqualTo(new[] { 80, 443 }));
    }
}
