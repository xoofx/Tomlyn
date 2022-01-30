// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Globalization;
using NUnit.Framework;

namespace Tomlyn.Tests;

public class TomlDateTimeTest
{
    [Test]
    [TestCase(null)]
    [TestCase("IV")]
    [TestCase("fr-FR")]
    [TestCase("es-ES")]
    public void TestIConvertibleToString(string? cultureName)
    {
        var cultureInfo = cultureName is null ? null : new CultureInfo(cultureName);

        var dateTime = new TomlDateTime(new DateTime(2022, 1, 27), 0, TomlDateTimeKind.LocalDate);

        var converted = Convert.ToString(dateTime, cultureInfo);

        Assert.AreEqual("2022-01-27", converted);
    }
}