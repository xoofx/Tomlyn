using System.Text.Json.Serialization;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public class NewApiPolymorphismTests
{
    [TomlPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [TomlDerivedType(typeof(Cat), "cat")]
    [TomlDerivedType(typeof(Dog), "dog")]
    private abstract class Animal
    {
        public string? Name { get; set; }
    }

    private sealed class Cat : Animal
    {
        public int Lives { get; set; }
    }

    private sealed class Dog : Animal
    {
        public bool GoodBoy { get; set; }
    }

    [Test]
    public void Serialize_Polymorphic_WritesDiscriminator()
    {
        Animal value = new Cat { Name = "Nyan", Lives = 9 };
        var toml = TomlSerializer.Serialize(value);

        Assert.That(toml, Does.Contain("kind"));
        Assert.That(toml, Does.Contain("cat"));
        Assert.That(toml, Does.Contain("Name"));
        Assert.That(toml, Does.Contain("Nyan"));
        Assert.That(toml, Does.Contain("Lives"));
        Assert.That(toml, Does.Contain("9"));
    }

    [Test]
    public void Deserialize_Polymorphic_UsesDiscriminatorWhenNotFirst()
    {
        var toml =
            """
            kind = "cat"
            Name = "Nyan"
            Lives = 9
            """;

        var result = TomlSerializer.Deserialize<Animal>(toml);

        Assert.That(result, Is.TypeOf<Cat>());
        var cat = (Cat)result!;
        Assert.That(cat.Name, Is.EqualTo("Nyan"));
        Assert.That(cat.Lives, Is.EqualTo(9));
    }

    [Test]
    public void Deserialize_Polymorphic_UnknownDiscriminator_FailsByDefault()
    {
        var toml =
            """
            kind = "fox"
            Name = "X"
            """;

        Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<Animal>(toml));
    }

    [TomlPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [TomlDerivedType(typeof(DerivedPet), "derived")]
    private class Pet
    {
        public string? Name { get; set; }
    }

    private sealed class DerivedPet : Pet
    {
        public int Age { get; set; }
    }

    [Test]
    public void Deserialize_Polymorphic_UnknownDiscriminator_CanFallbackToBase()
    {
        var toml =
            """
            kind = "unknown"
            Name = "Base"
            extra = 123
            """;

        var options = new TomlSerializerOptions
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "kind",
                UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.FallBackToBaseType,
            },
        };

        var result = TomlSerializer.Deserialize<Pet>(toml, options);
        Assert.That(result, Is.TypeOf<Pet>());
        Assert.That(result!.Name, Is.EqualTo("Base"));
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(JsonCat), "cat")]
    private abstract class JsonAnimal
    {
        public string? Name { get; set; }
    }

    private sealed class JsonCat : JsonAnimal
    {
        public int Lives { get; set; }
    }

    [Test]
    public void Deserialize_Polymorphic_RespectsSystemTextJsonAttributes()
    {
        var toml =
            """
            kind = "cat"
            Name = "Ada"
            Lives = 9
            """;

        var result = TomlSerializer.Deserialize<JsonAnimal>(toml);
        Assert.That(result, Is.TypeOf<JsonCat>());
        Assert.That(((JsonCat)result!).Lives, Is.EqualTo(9));
    }
}
