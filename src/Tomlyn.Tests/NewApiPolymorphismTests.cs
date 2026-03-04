using System;
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

    // --- Feature 1: Default Derived Type (no discriminator) ---

    [TomlPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [TomlDerivedType(typeof(DefaultCircle))]
    [TomlDerivedType(typeof(DefaultSquare), "square")]
    private abstract class DefaultShape
    {
        public string? Color { get; set; }
    }

    private sealed class DefaultCircle : DefaultShape
    {
        public double Radius { get; set; }
    }

    private sealed class DefaultSquare : DefaultShape
    {
        public double Side { get; set; }
    }

    [Test]
    public void Serialize_DefaultDerivedType_OmitsDiscriminator()
    {
        DefaultShape value = new DefaultCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(value);

        Assert.That(toml, Does.Not.Contain("type"));
        Assert.That(toml, Does.Contain("Color"));
        Assert.That(toml, Does.Contain("red"));
        Assert.That(toml, Does.Contain("Radius"));
        Assert.That(toml, Does.Contain("5"));
    }

    [Test]
    public void Serialize_NonDefaultDerivedType_WritesDiscriminator()
    {
        DefaultShape value = new DefaultSquare { Color = "blue", Side = 3.0 };
        var toml = TomlSerializer.Serialize(value);

        Assert.That(toml, Does.Contain("type = \"square\""));
        Assert.That(toml, Does.Contain("Color"));
        Assert.That(toml, Does.Contain("blue"));
        Assert.That(toml, Does.Contain("Side"));
        Assert.That(toml, Does.Contain("3"));
    }

    [Test]
    public void Deserialize_DefaultDerivedType_WhenMissingDiscriminator()
    {
        var toml =
            """
            Color = "red"
            Radius = 5.0
            """;

        var result = TomlSerializer.Deserialize<DefaultShape>(toml);

        Assert.That(result, Is.TypeOf<DefaultCircle>());
        var circle = (DefaultCircle)result!;
        Assert.That(circle.Color, Is.EqualTo("red"));
        Assert.That(circle.Radius, Is.EqualTo(5.0));
    }

    [Test]
    public void Deserialize_DefaultDerivedType_WhenUnknownDiscriminator()
    {
        var toml =
            """
            type = "triangle"
            Color = "green"
            """;

        var result = TomlSerializer.Deserialize<DefaultShape>(toml);

        Assert.That(result, Is.TypeOf<DefaultCircle>());
        var circle = (DefaultCircle)result!;
        Assert.That(circle.Color, Is.EqualTo("green"));
    }

    [Test]
    public void Deserialize_NonDefaultDerivedType_WithDiscriminator()
    {
        var toml =
            """
            type = "square"
            Color = "blue"
            Side = 3.0
            """;

        var result = TomlSerializer.Deserialize<DefaultShape>(toml);

        Assert.That(result, Is.TypeOf<DefaultSquare>());
        var square = (DefaultSquare)result!;
        Assert.That(square.Color, Is.EqualTo("blue"));
        Assert.That(square.Side, Is.EqualTo(3.0));
    }

    [Test]
    public void Roundtrip_DefaultDerivedType()
    {
        DefaultShape original = new DefaultCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(original);
        var result = TomlSerializer.Deserialize<DefaultShape>(toml);

        Assert.That(result, Is.TypeOf<DefaultCircle>());
        var circle = (DefaultCircle)result!;
        Assert.That(circle.Color, Is.EqualTo("red"));
        Assert.That(circle.Radius, Is.EqualTo(5.0));
    }

    [Test]
    public void Roundtrip_NonDefaultDerivedType()
    {
        DefaultShape original = new DefaultSquare { Color = "blue", Side = 3.0 };
        var toml = TomlSerializer.Serialize(original);
        var result = TomlSerializer.Deserialize<DefaultShape>(toml);

        Assert.That(result, Is.TypeOf<DefaultSquare>());
        var square = (DefaultSquare)result!;
        Assert.That(square.Color, Is.EqualTo("blue"));
        Assert.That(square.Side, Is.EqualTo(3.0));
    }

    // Default derived type with JsonDerivedType (no discriminator is 1-arg constructor)
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(JsonDefaultCircle))]
    [JsonDerivedType(typeof(JsonDefaultSquare), "square")]
    private abstract class JsonDefaultShape
    {
        public string? Color { get; set; }
    }

    private sealed class JsonDefaultCircle : JsonDefaultShape
    {
        public double Radius { get; set; }
    }

    private sealed class JsonDefaultSquare : JsonDefaultShape
    {
        public double Side { get; set; }
    }

    [Test]
    public void Serialize_JsonDefaultDerivedType_OmitsDiscriminator()
    {
        JsonDefaultShape value = new JsonDefaultCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(value);

        Assert.That(toml, Does.Not.Contain("kind"));
        Assert.That(toml, Does.Contain("Color"));
        Assert.That(toml, Does.Contain("Radius"));
    }

    [Test]
    public void Deserialize_JsonDefaultDerivedType_WhenMissingDiscriminator()
    {
        var toml =
            """
            Color = "red"
            Radius = 5.0
            """;

        var result = TomlSerializer.Deserialize<JsonDefaultShape>(toml);

        Assert.That(result, Is.TypeOf<JsonDefaultCircle>());
        Assert.That(((JsonDefaultCircle)result!).Color, Is.EqualTo("red"));
        Assert.That(((JsonDefaultCircle)result).Radius, Is.EqualTo(5.0));
    }

    // --- Feature 2: UnknownDerivedTypeHandling on attribute ---

    [TomlPolymorphic(TypeDiscriminatorPropertyName = "kind", UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.FallBackToBaseType)]
    [TomlDerivedType(typeof(AttrFallbackDerived), "derived")]
    private class AttrFallbackBase
    {
        public string? Name { get; set; }
    }

    private sealed class AttrFallbackDerived : AttrFallbackBase
    {
        public int Extra { get; set; }
    }

    [Test]
    public void Deserialize_UnknownDiscriminator_FallbackViaAttribute()
    {
        var toml =
            """
            kind = "unknown"
            Name = "test"
            """;

        // No options override needed — attribute sets FallBackToBaseType
        var result = TomlSerializer.Deserialize<AttrFallbackBase>(toml);

        Assert.That(result, Is.TypeOf<AttrFallbackBase>());
        Assert.That(result!.Name, Is.EqualTo("test"));
    }

    [Test]
    public void Deserialize_UnknownDiscriminator_AttributeOverridesOptions()
    {
        var toml =
            """
            kind = "unknown"
            Name = "test"
            """;

        // Options say Fail, but attribute says FallBackToBaseType — attribute wins
        var options = new TomlSerializerOptions
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.Fail,
            },
        };

        var result = TomlSerializer.Deserialize<AttrFallbackBase>(toml, options);

        Assert.That(result, Is.TypeOf<AttrFallbackBase>());
        Assert.That(result!.Name, Is.EqualTo("test"));
    }

    // Test: attribute says Fail explicitly, options say FallBackToBaseType — attribute wins
    [TomlPolymorphic(TypeDiscriminatorPropertyName = "kind", UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.Fail)]
    [TomlDerivedType(typeof(AttrFailDerived), "derived")]
    private class AttrFailBase
    {
        public string? Name { get; set; }
    }

    private sealed class AttrFailDerived : AttrFailBase
    {
        public int Extra { get; set; }
    }

    [Test]
    public void Deserialize_UnknownDiscriminator_AttributeFailOverridesFallbackOptions()
    {
        var toml =
            """
            kind = "unknown"
            Name = "test"
            """;

        var options = new TomlSerializerOptions
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.FallBackToBaseType,
            },
        };

        Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<AttrFailBase>(toml, options));
    }

    // Test: JsonPolymorphic attribute sets FallBackToBaseType
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
    [JsonDerivedType(typeof(JsonAttrFallbackDerived), "derived")]
    private class JsonAttrFallbackBase
    {
        public string? Name { get; set; }
    }

    private sealed class JsonAttrFallbackDerived : JsonAttrFallbackBase
    {
        public int Extra { get; set; }
    }

    [Test]
    public void Deserialize_UnknownDiscriminator_JsonAttributeFallback()
    {
        var toml =
            """
            kind = "unknown"
            Name = "test"
            """;

        var result = TomlSerializer.Deserialize<JsonAttrFallbackBase>(toml);

        Assert.That(result, Is.TypeOf<JsonAttrFallbackBase>());
        Assert.That(result!.Name, Is.EqualTo("test"));
    }

    // Test: TomlPolymorphic overrides JsonPolymorphic when both present
    [TomlPolymorphic(TypeDiscriminatorPropertyName = "kind", UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.Fail)]
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
    [TomlDerivedType(typeof(TomlOverridesJsonDerived), "derived")]
    private class TomlOverridesJsonBase
    {
        public string? Name { get; set; }
    }

    private sealed class TomlOverridesJsonDerived : TomlOverridesJsonBase
    {
        public int Extra { get; set; }
    }

    [Test]
    public void Deserialize_UnknownDiscriminator_TomlAttributeOverridesJsonAttribute()
    {
        var toml =
            """
            kind = "unknown"
            Name = "test"
            """;

        // TomlPolymorphic says Fail, JsonPolymorphic says FallBack — Toml wins
        Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<TomlOverridesJsonBase>(toml));
    }

    [Test]
    public void Options_RejectsUnspecifiedUnknownDerivedTypeHandling()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new TomlPolymorphismOptions
            {
                UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.Unspecified,
            };
        });
    }

    // --- Feature 3: Integer Discriminators ---

    [TomlPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [TomlDerivedType(typeof(IntDiscrimCircle), 1)]
    [TomlDerivedType(typeof(IntDiscrimSquare), 2)]
    private abstract class IntDiscrimShape
    {
        public string? Color { get; set; }
    }

    private sealed class IntDiscrimCircle : IntDiscrimShape
    {
        public double Radius { get; set; }
    }

    private sealed class IntDiscrimSquare : IntDiscrimShape
    {
        public double Side { get; set; }
    }

    [Test]
    public void Serialize_IntDiscriminator_WritesAsString()
    {
        IntDiscrimShape value = new IntDiscrimCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(value);

        Assert.That(toml, Does.Contain("type = \"1\""));
        Assert.That(toml, Does.Contain("Color"));
        Assert.That(toml, Does.Contain("Radius"));
    }

    [Test]
    public void Deserialize_IntDiscriminator_MatchesByString()
    {
        var toml =
            """
            type = "1"
            Color = "red"
            Radius = 5.0
            """;

        var result = TomlSerializer.Deserialize<IntDiscrimShape>(toml);

        Assert.That(result, Is.TypeOf<IntDiscrimCircle>());
        var circle = (IntDiscrimCircle)result!;
        Assert.That(circle.Color, Is.EqualTo("red"));
        Assert.That(circle.Radius, Is.EqualTo(5.0));
    }

    [Test]
    public void Roundtrip_IntDiscriminator()
    {
        IntDiscrimShape original = new IntDiscrimSquare { Color = "blue", Side = 3.0 };
        var toml = TomlSerializer.Serialize(original);
        var result = TomlSerializer.Deserialize<IntDiscrimShape>(toml);

        Assert.That(result, Is.TypeOf<IntDiscrimSquare>());
        var square = (IntDiscrimSquare)result!;
        Assert.That(square.Color, Is.EqualTo("blue"));
        Assert.That(square.Side, Is.EqualTo(3.0));
    }

    // JsonDerivedType int discriminators (already supported at runtime)
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(JsonIntDiscrimCircle), 1)]
    [JsonDerivedType(typeof(JsonIntDiscrimSquare), 2)]
    private abstract class JsonIntDiscrimShape
    {
        public string? Color { get; set; }
    }

    private sealed class JsonIntDiscrimCircle : JsonIntDiscrimShape
    {
        public double Radius { get; set; }
    }

    private sealed class JsonIntDiscrimSquare : JsonIntDiscrimShape
    {
        public double Side { get; set; }
    }

    [Test]
    public void Roundtrip_JsonIntDiscriminator()
    {
        JsonIntDiscrimShape original = new JsonIntDiscrimCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(original);

        Assert.That(toml, Does.Contain("type = \"1\""));

        var result = TomlSerializer.Deserialize<JsonIntDiscrimShape>(toml);

        Assert.That(result, Is.TypeOf<JsonIntDiscrimCircle>());
        Assert.That(((JsonIntDiscrimCircle)result!).Color, Is.EqualTo("red"));
        Assert.That(((JsonIntDiscrimCircle)result).Radius, Is.EqualTo(5.0));
    }
}
