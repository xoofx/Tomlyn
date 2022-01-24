// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using NUnit.Framework;

namespace Tomlyn.Tests;

/// <summary>
/// Tests for the <see cref="Toml"/> frontend
/// </summary>
public class TomlTests
{

    [Test]
    public void TestVersion()
    {
        Assert.False(string.IsNullOrEmpty(Toml.Version));
    }
    
}