// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.

using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests
{
    public class SerializationTests
    {
        [Test]
        public void TestCrlfInMultilineString()
        {
            var model = new TomlTable
            {
                PropertiesMetadata = new TomlPropertiesMetadata()
            };
            model.PropertiesMetadata.SetProperty("property", new TomlPropertyMetadata
            {
                DisplayKind = TomlPropertyDisplayKind.StringLiteralMulti
            });

            model["property"] = "string\r\nwith\r\nnewlines";

            Assert.AreEqual("property = '''string\r\nwith\r\nnewlines'''\r\n", Toml.FromModel(model));
        }
    }
}