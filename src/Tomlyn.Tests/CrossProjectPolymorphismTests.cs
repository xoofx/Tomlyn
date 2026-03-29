using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

public sealed class CrossProjectPolymorphismTests
{
    [Test]
    public void RuntimeMappings_CanSerializeAndDeserializeCrossProjectRoot()
    {
        RuntimeAnimal value = new RuntimeDog { Name = "Rex", GoodBoy = true };
        var options = CreateRuntimeAnimalOptions();

        var toml = TomlSerializer.Serialize(value, options);
        var roundtrip = TomlSerializer.Deserialize<RuntimeAnimal>(toml, options);

        Assert.That(toml, Does.Contain("kind = \"dog\""));
        Assert.That(roundtrip, Is.TypeOf<RuntimeDog>());
        Assert.That(((RuntimeDog)roundtrip!).Name, Is.EqualTo("Rex"));
        Assert.That(((RuntimeDog)roundtrip).GoodBoy, Is.True);
    }

    [Test]
    public void RuntimeMappings_CanRoundtripCollectionsOfPolymorphicValues()
    {
        var payload = new RuntimeShelter
        {
            Animals =
            [
                new RuntimeCat { Name = "Mog", Lives = 9 },
                new RuntimeDog { Name = "Rex", GoodBoy = true },
            ],
        };

        var options = CreateRuntimeAnimalOptions();
        var toml = TomlSerializer.Serialize(payload, options);
        var roundtrip = TomlSerializer.Deserialize<RuntimeShelter>(toml, options);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Animals, Has.Count.EqualTo(2));
        Assert.That(roundtrip.Animals[0], Is.TypeOf<RuntimeCat>());
        Assert.That(roundtrip.Animals[1], Is.TypeOf<RuntimeDog>());
    }

    [Test]
    public void RuntimeMappings_AttributeMappingsTakePrecedenceButRemainAdditive()
    {
        var options = new TomlSerializerOptions
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "kind",
                DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                {
                    [typeof(AttributedAnimal)] =
                    [
                        new(typeof(RuntimeMappedDog), "cat"),
                        new(typeof(RuntimeMappedFox), "fox"),
                    ],
                },
            },
        };

        var preferredToml =
            """
            kind = "cat"
            Name = "Ada"
            Lives = 9
            """;

        var additiveToml =
            """
            kind = "fox"
            Name = "Pip"
            Tricks = 3
            """;

        var preferred = TomlSerializer.Deserialize<AttributedAnimal>(preferredToml, options);
        var additive = TomlSerializer.Deserialize<AttributedAnimal>(additiveToml, options);

        Assert.That(preferred, Is.TypeOf<AttributedCat>());
        Assert.That(((AttributedCat)preferred!).Lives, Is.EqualTo(9));
        Assert.That(additive, Is.TypeOf<RuntimeMappedFox>());
        Assert.That(((RuntimeMappedFox)additive!).Tricks, Is.EqualTo(3));
    }

    [Test]
    public void RuntimeMappings_DefaultDerivedType_WorksWithoutBaseTypeAttributes()
    {
        var options = CreateRuntimeShapeOptions();

        RuntimeShape value = new RuntimeCircle { Color = "red", Radius = 5.0 };
        var toml = TomlSerializer.Serialize(value, options);
        var missingDiscriminator =
            """
            Color = "red"
            Radius = 5.0
            """;
        var unknownDiscriminator =
            """
            type = "triangle"
            Color = "blue"
            Radius = 2.0
            """;

        var missing = TomlSerializer.Deserialize<RuntimeShape>(missingDiscriminator, options);
        var unknown = TomlSerializer.Deserialize<RuntimeShape>(unknownDiscriminator, options);

        Assert.That(toml, Does.Not.Contain("type"));
        Assert.That(missing, Is.TypeOf<RuntimeCircle>());
        Assert.That(unknown, Is.TypeOf<RuntimeCircle>());
    }

    [Test]
    public void RuntimeMappings_IntDiscriminators_UseInvariantStringValues()
    {
        var options = new TomlSerializerOptions
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "type",
                DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                {
                    [typeof(RuntimeNumberShape)] =
                    [
                        new(typeof(RuntimeNumberCircle), 1),
                        new(typeof(RuntimeNumberSquare), 2),
                    ],
                },
            },
        };

        RuntimeNumberShape value = new RuntimeNumberCircle { Radius = 4.5 };
        var toml = TomlSerializer.Serialize(value, options);
        var deserialized = TomlSerializer.Deserialize<RuntimeNumberShape>(
            """
            type = "2"
            Side = 3.0
            """,
            options);

        Assert.That(toml, Does.Contain("type = \"1\""));
        Assert.That(deserialized, Is.TypeOf<RuntimeNumberSquare>());
        Assert.That(((RuntimeNumberSquare)deserialized!).Side, Is.EqualTo(3.0));
    }

    [Test]
    public void RuntimeMappings_InvalidDerivedType_ThrowsTomlException()
    {
        var options = new TomlSerializerOptions
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "kind",
                DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                {
                    [typeof(RuntimeAnimal)] =
                    [
                        new(typeof(string), "cat"),
                    ],
                },
            },
        };

        var exception = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<RuntimeAnimal>("kind = \"cat\"", options));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("is not assignable"));
    }

    [Test]
    public void RuntimeMappings_EmptyDiscriminator_ThrowsTomlException()
    {
        var options = new TomlSerializerOptions
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "kind",
                DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                {
                    [typeof(RuntimeAnimal)] =
                    [
                        new(typeof(RuntimeCat), string.Empty),
                    ],
                },
            },
        };

        var exception = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<RuntimeAnimal>("kind = \"cat\"", options));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("cannot be empty"));
    }

    private static TomlSerializerOptions CreateRuntimeAnimalOptions()
        => new()
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "kind",
                DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                {
                    [typeof(RuntimeAnimal)] =
                    [
                        new(typeof(RuntimeCat), "cat"),
                        new(typeof(RuntimeDog), "dog"),
                    ],
                },
            },
        };

    private static TomlSerializerOptions CreateRuntimeShapeOptions()
        => new()
        {
            PolymorphismOptions = new TomlPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "type",
                DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                {
                    [typeof(RuntimeShape)] =
                    [
                        new(typeof(RuntimeCircle)),
                        new(typeof(RuntimeSquare), "square"),
                    ],
                },
            },
        };

    private abstract class RuntimeAnimal
    {
        public string? Name { get; set; }
    }

    private sealed class RuntimeCat : RuntimeAnimal
    {
        public int Lives { get; set; }
    }

    private sealed class RuntimeDog : RuntimeAnimal
    {
        public bool GoodBoy { get; set; }
    }

    private sealed class RuntimeShelter
    {
        public List<RuntimeAnimal> Animals { get; set; } = [];
    }

    [TomlPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [TomlDerivedType(typeof(AttributedCat), "cat")]
    private abstract class AttributedAnimal
    {
        public string? Name { get; set; }
    }

    private sealed class AttributedCat : AttributedAnimal
    {
        public int Lives { get; set; }
    }

    private sealed class RuntimeMappedDog : AttributedAnimal
    {
        public bool GoodBoy { get; set; }
    }

    private sealed class RuntimeMappedFox : AttributedAnimal
    {
        public int Tricks { get; set; }
    }

    private abstract class RuntimeShape
    {
        public string? Color { get; set; }
    }

    private sealed class RuntimeCircle : RuntimeShape
    {
        public double Radius { get; set; }
    }

    private sealed class RuntimeSquare : RuntimeShape
    {
        public double Side { get; set; }
    }

    private abstract class RuntimeNumberShape
    {
    }

    private sealed class RuntimeNumberCircle : RuntimeNumberShape
    {
        public double Radius { get; set; }
    }

    private sealed class RuntimeNumberSquare : RuntimeNumberShape
    {
        public double Side { get; set; }
    }
}
