using System;
using NUnit.Framework;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Tomlyn.Tests;

internal sealed class GeneratedBytePairModel
{
    public byte First { get; set; }

    public byte Second { get; set; }
}

[TomlSerializable(typeof(GeneratedBytePairModel))]
internal partial class TestBuiltInConvertersContext : TomlSerializerContext
{
}

public sealed class NewApiBuiltInConvertersTests
{
    private sealed class GuidModel
    {
        public Guid Id { get; set; }
    }

    private enum SampleEnum
    {
        None = 0,
        First = 1,
        Second = 2,
    }

    private sealed class EnumModel
    {
        public SampleEnum Value { get; set; }
    }

    private sealed class ByteModel
    {
        public byte B { get; set; }
    }

    private sealed class BytePairModel
    {
        public byte First { get; set; }

        public byte Second { get; set; }
    }

    private sealed class DateTimeOffsetModel
    {
        public DateTimeOffset Value { get; set; }
    }

    [Test]
    public void Guid_Roundtrip()
    {
        var model = new GuidModel { Id = Guid.NewGuid() };
        var toml = TomlSerializer.Serialize(model);
        var roundtrip = TomlSerializer.Deserialize<GuidModel>(toml);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Id, Is.EqualTo(model.Id));
    }

    [Test]
    public void Enum_Roundtrip_Integer()
    {
        var model = new EnumModel { Value = SampleEnum.Second };
        var toml = TomlSerializer.Serialize(model);
        var roundtrip = TomlSerializer.Deserialize<EnumModel>(toml);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(model.Value));
    }

    [Test]
    public void Enum_ReadsStringName()
    {
        var model = TomlSerializer.Deserialize<EnumModel>("Value = \"Second\"");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Value, Is.EqualTo(SampleEnum.Second));
    }

    [Test]
    public void Byte_Overflow_ReportsLocation()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<ByteModel>("B = 256\n"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.EqualTo(5));
    }

    [Test]
    public void Byte_Overflow_AggregatesMultipleLocations()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<BytePairModel>("First = 256\nSecond = 300\n"));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Diagnostics.Count, Is.EqualTo(2));
        Assert.That(ex.Diagnostics[0].Span.Start.Line, Is.EqualTo(0));
        Assert.That(ex.Diagnostics[0].Span.Start.Column, Is.EqualTo(8));
        Assert.That(ex.Diagnostics[0].Message, Does.Contain("TOML integer value 256 is out of range."));
        Assert.That(ex.Diagnostics[1].Span.Start.Line, Is.EqualTo(1));
        Assert.That(ex.Diagnostics[1].Span.Start.Column, Is.EqualTo(9));
        Assert.That(ex.Diagnostics[1].Message, Does.Contain("TOML integer value 300 is out of range."));
    }

    [Test]
    public void Byte_Overflow_GeneratedContextAggregatesMultipleLocations()
    {
        var context = TestBuiltInConvertersContext.Default;

        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize("First = 256\nSecond = 300\n", context.GeneratedBytePairModel));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Diagnostics.Count, Is.EqualTo(2));
        Assert.That(ex.Diagnostics[0].Span.Start.Line, Is.EqualTo(0));
        Assert.That(ex.Diagnostics[0].Span.Start.Column, Is.EqualTo(8));
        Assert.That(ex.Diagnostics[0].Message, Does.Contain("TOML integer value 256 is out of range."));
        Assert.That(ex.Diagnostics[1].Span.Start.Line, Is.EqualTo(1));
        Assert.That(ex.Diagnostics[1].Span.Start.Column, Is.EqualTo(9));
        Assert.That(ex.Diagnostics[1].Message, Does.Contain("TOML integer value 300 is out of range."));
    }

    [Test]
    public void DateTimeOffset_RejectsLocalDateTime_ByDefault()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<DateTimeOffsetModel>("Value = 1979-05-27T07:32:00\n"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.Not.Null.And.GreaterThan(0));
    }

    private sealed class DateTimeModel
    {
        public DateTime Value { get; set; }
    }

    [TestCase(DateTimeKind.Utc)]
    [TestCase(DateTimeKind.Unspecified)]
    public void DateTime_Roundtrip_KeepsFractionalSeconds(DateTimeKind kind)
    {
        var value = new DateTime(new DateTime(1979, 5, 27, 7, 32, 0).Ticks + 1_234_567, kind);
        var model = new DateTimeModel { Value = value };
        var toml = TomlSerializer.Serialize(model);
        Assert.That(toml, Does.Contain("07:32:00.1234567"));

        var roundtrip = TomlSerializer.Deserialize<DateTimeModel>(toml);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(value));
    }

    [Test]
    public void DateTimeOffset_Roundtrip_KeepsFractionalSeconds()
    {
        var value = new DateTimeOffset(1979, 5, 27, 7, 32, 0, 120, TimeSpan.FromHours(2));
        var model = new DateTimeOffsetModel { Value = value };
        var toml = TomlSerializer.Serialize(model);
        Assert.That(toml, Does.Contain("07:32:00.12+02:00"));

        var roundtrip = TomlSerializer.Deserialize<DateTimeOffsetModel>(toml);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(value));
        Assert.That(roundtrip.Value.Offset, Is.EqualTo(value.Offset));
    }

    [Test]
    public void TomlTable_DateTimeValues_KeepFractionalSeconds()
    {
        var table = new TomlTable
        {
            ["local"] = new DateTime(1979, 5, 27, 7, 32, 0, 500),
            ["offset"] = new DateTimeOffset(1979, 5, 27, 7, 32, 0, 5, TimeSpan.Zero),
        };

        var toml = TomlSerializer.Serialize(table);
        Assert.That(toml, Does.Contain("local = 1979-05-27T07:32:00.5"));
        Assert.That(toml, Does.Contain("offset = 1979-05-27T07:32:00.005Z"));
    }

#if NET6_0_OR_GREATER
    private sealed class DateOnlyModel
    {
        public DateOnly Date { get; set; }
    }

    private sealed class TimeOnlyModel
    {
        public TimeOnly Time { get; set; }
    }

    [Test]
    public void DateOnly_Roundtrip()
    {
        var model = new DateOnlyModel { Date = new DateOnly(2024, 10, 31) };
        var toml = TomlSerializer.Serialize(model);
        var roundtrip = TomlSerializer.Deserialize<DateOnlyModel>(toml);
        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Date, Is.EqualTo(model.Date));
    }

    [Test]
    public void TimeOnly_Roundtrip_MinuteOnly()
    {
        var model = TomlSerializer.Deserialize<TimeOnlyModel>("Time = 07:32\n");
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Time, Is.EqualTo(new TimeOnly(7, 32, 0)));
    }
#endif
}
