using System;
using System.Linq;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public class NewApiAttributeContractTests
{
    [Test]
    public void ExportedTomlAttributes_HaveUniqueTypeNames()
    {
        var duplicateNames = typeof(TomlSerializer).Assembly
            .GetExportedTypes()
            .Where(static type => type.Name.StartsWith("Toml", StringComparison.Ordinal) &&
                                  type.Name.EndsWith("Attribute", StringComparison.Ordinal))
            .GroupBy(static type => type.Name, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        Assert.That(duplicateNames, Is.Empty);
    }

    [Test]
    public void ExportedTomlAttributes_InheritTomlAttribute()
    {
        var nonDerivedAttributes = typeof(TomlSerializer).Assembly
            .GetExportedTypes()
            .Where(static type => type != typeof(TomlAttribute) &&
                                  type.IsSubclassOf(typeof(Attribute)) &&
                                  type.Name.StartsWith("Toml", StringComparison.Ordinal) &&
                                  type.Name.EndsWith("Attribute", StringComparison.Ordinal))
            .Where(static type => !typeof(TomlAttribute).IsAssignableFrom(type))
            .Select(static type => type.FullName)
            .ToArray();

        Assert.That(nonDerivedAttributes, Is.Empty);
    }

    [Test]
    public void TomlPropertyNameAttribute_IsOnlyAvailableFromSerializationNamespace()
    {
        var exportedTypes = typeof(TomlSerializer).Assembly
            .GetExportedTypes()
            .Where(static type => type.Name == nameof(TomlPropertyNameAttribute))
            .Select(static type => type.FullName)
            .OrderBy(static fullName => fullName, StringComparer.Ordinal)
            .ToArray();

        Assert.That(exportedTypes, Is.EqualTo(new[] { "Tomlyn.Serialization.TomlPropertyNameAttribute" }));
    }
}
