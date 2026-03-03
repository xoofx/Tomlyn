using System;
using NUnit.Framework;

namespace Tomlyn.Tests;

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
    public void DateTimeOffset_RejectsLocalDateTime_ByDefault()
    {
        var ex = Assert.Throws<TomlException>(() => TomlSerializer.Deserialize<DateTimeOffsetModel>("Value = 1979-05-27T07:32:00\n"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Line, Is.EqualTo(1));
        Assert.That(ex.Column, Is.Not.Null.And.GreaterThan(0));
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
