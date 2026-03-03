using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class NewApiPolymorphismValidationTests
{
    [TomlPolymorphic]
    [TomlDerivedType(typeof(DerivedEmptyDiscriminator), "")]
    private abstract class BaseEmptyDiscriminator
    {
    }

    private sealed class DerivedEmptyDiscriminator : BaseEmptyDiscriminator
    {
        public int Value { get; set; } = 1;
    }

    [TomlPolymorphic]
    [TomlDerivedType(typeof(string), "x")]
    private abstract class BaseNotAssignable
    {
    }

    private sealed class DerivedNotAssignable : BaseNotAssignable
    {
        public int Value { get; set; } = 1;
    }

    [Test]
    public void TomlDerivedType_EmptyDiscriminator_ThrowsTomlException()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize<BaseEmptyDiscriminator>(new DerivedEmptyDiscriminator()));
    }

    [Test]
    public void TomlDerivedType_NotAssignable_ThrowsTomlException()
    {
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize<BaseNotAssignable>(new DerivedNotAssignable()));
    }

    [Test]
    public void PolymorphismOptions_EmptyDiscriminatorPropertyName_ThrowsTomlException()
    {
        var options = TomlSerializerOptions.Default with
        {
            PolymorphismOptions = new TomlPolymorphismOptions { TypeDiscriminatorPropertyName = "" },
        };

        Assert.Throws<TomlException>(() => TomlSerializer.Serialize<BaseNotAssignable>(new DerivedNotAssignable(), options));
    }
}

