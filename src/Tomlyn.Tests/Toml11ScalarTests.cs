using System;
using NUnit.Framework;

namespace Tomlyn.Tests;

public sealed class Toml11ScalarTests
{
    [Test]
    public void Deserialize_BasicString_SupportsEscapeAndHexEscapes()
    {
        var options = new TomlSerializerOptions
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
        };

        var value = TomlSerializer.Deserialize<string>("value = \"A\\e\\x41\"\n", options);
        Assert.That(value, Is.EqualTo("A\u001BA"));
    }

    [Test]
    public void Deserialize_TomlDateTime_SupportsMinuteOnlyLocalTime()
    {
        var options = new TomlSerializerOptions
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
        };

        var value = TomlSerializer.Deserialize<TomlDateTime>("value = 07:32\n", options);
        Assert.That(value.Kind, Is.EqualTo(TomlDateTimeKind.LocalTime));
        Assert.That(value.DateTime.TimeOfDay, Is.EqualTo(new TimeSpan(7, 32, 0)));
    }

    [Test]
    public void Deserialize_TomlDateTime_SupportsMinuteOnlyOffsetDateTime()
    {
        var options = new TomlSerializerOptions
        {
            RootValueHandling = TomlRootValueHandling.WrapInRootKey,
        };

        var value = TomlSerializer.Deserialize<TomlDateTime>("value = 1979-05-27T07:32Z\n", options);
        Assert.That(value.Kind, Is.EqualTo(TomlDateTimeKind.OffsetDateTimeByZ));
        Assert.That(value.DateTime.ToUniversalTime().Year, Is.EqualTo(1979));
        Assert.That(value.DateTime.ToUniversalTime().Month, Is.EqualTo(5));
        Assert.That(value.DateTime.ToUniversalTime().Day, Is.EqualTo(27));
        Assert.That(value.DateTime.ToUniversalTime().Hour, Is.EqualTo(7));
        Assert.That(value.DateTime.ToUniversalTime().Minute, Is.EqualTo(32));
    }
}
