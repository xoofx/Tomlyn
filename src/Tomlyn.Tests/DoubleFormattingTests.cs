// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using NUnit.Framework;

namespace Tomlyn.Tests;

[TestFixture]
public sealed class DoubleFormattingTests
{
    private sealed class Model
    {
        public double Value { get; set; }
    }

    [Test]
    public void SerializeDeserialize_Double_Roundtrips()
    {
        const double value = 126.92842769438576;

        var toml = TomlSerializer.Serialize(new Model { Value = value });
        var roundtrip = TomlSerializer.Deserialize<Model>(toml);

        Assert.That(roundtrip, Is.Not.Null);
        Assert.That(roundtrip!.Value, Is.EqualTo(value));
    }
}
