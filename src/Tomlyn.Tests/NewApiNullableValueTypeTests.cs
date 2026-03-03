using System;
using NUnit.Framework;

namespace Tomlyn.Tests;

public sealed class NewApiNullableValueTypeTests
{
    private sealed class NullablePayload
    {
        public int? Count { get; set; }

        public DateTimeOffset? When { get; set; }
    }

    [Test]
    public void Deserialize_NullableValueTypes_WhenMissing_ShouldRemainNull()
    {
        var payload = TomlSerializer.Deserialize<NullablePayload>(string.Empty);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Count, Is.Null);
        Assert.That(payload.When, Is.Null);
    }

    [Test]
    public void Deserialize_NullableValueTypes_WhenPresent_ShouldBind()
    {
        var toml = """
            Count = 1
            When = 1979-05-27T07:32:00Z
            """;

        var payload = TomlSerializer.Deserialize<NullablePayload>(toml);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Count, Is.EqualTo(1));
        Assert.That(payload.When, Is.Not.Null);
        Assert.That(payload.When!.Value.UtcDateTime, Is.EqualTo(new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void Serialize_NullableValueTypes_WhenNull_ShouldBeOmittedByDefault()
    {
        var payload = new NullablePayload
        {
            Count = null,
            When = null,
        };

        var toml = TomlSerializer.Serialize(payload);

        Assert.That(toml, Does.Not.Contain("Count"));
        Assert.That(toml, Does.Not.Contain("When"));
    }
}

