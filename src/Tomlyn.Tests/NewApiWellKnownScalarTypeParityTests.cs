using System;
using NUnit.Framework;

namespace Tomlyn.Tests;

public sealed class NewApiWellKnownScalarTypeParityTests
{
#if NET5_0_OR_GREATER
    private sealed class HalfPayload
    {
        public Half Value { get; set; }
    }

    [Test]
    public void Deserialize_Half_ShouldRoundTrip()
    {
        var toml = """
            Value = 1.5
            """;

        var payload = TomlSerializer.Deserialize<HalfPayload>(toml);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Value, Is.EqualTo((Half)1.5));

        var roundtripToml = TomlSerializer.Serialize(payload);
        var roundtrip = TomlSerializer.Deserialize<HalfPayload>(roundtripToml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(payload.Value));
    }

    [Test]
    public void Deserialize_Half_OutOfRange_ShouldThrowWithLocation()
    {
        var toml = """
            Value = 1e100
            """;

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<HalfPayload>(toml));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.GreaterThan(0));
    }
#endif

#if NET7_0_OR_GREATER
    private sealed class Int128Payload
    {
        public Int128 Value { get; set; }
    }

    private sealed class UInt128Payload
    {
        public UInt128 Value { get; set; }
    }

    [Test]
    public void Deserialize_Int128_ShouldRoundTripWithinTomlRange()
    {
        var toml = """
            Value = 123
            """;

        var payload = TomlSerializer.Deserialize<Int128Payload>(toml);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Value, Is.EqualTo((Int128)123));

        var roundtripToml = TomlSerializer.Serialize(payload);
        var roundtrip = TomlSerializer.Deserialize<Int128Payload>(roundtripToml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(payload.Value));
    }

    [Test]
    public void Serialize_Int128_OutOfTomlRange_ShouldThrow()
    {
        var payload = new Int128Payload { Value = (Int128)long.MaxValue + 1 };
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(payload));
    }

    [Test]
    public void Deserialize_UInt128_ShouldRoundTripWithinTomlRange()
    {
        var toml = """
            Value = 42
            """;

        var payload = TomlSerializer.Deserialize<UInt128Payload>(toml);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Value, Is.EqualTo((UInt128)42));

        var roundtripToml = TomlSerializer.Serialize(payload);
        var roundtrip = TomlSerializer.Deserialize<UInt128Payload>(roundtripToml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(payload.Value));
    }

    [Test]
    public void Serialize_UInt128_OutOfTomlRange_ShouldThrow()
    {
        var payload = new UInt128Payload { Value = (UInt128)long.MaxValue + 1 };
        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(payload));
    }
#endif
}
