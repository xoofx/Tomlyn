// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 
// See license.txt file in the project root for full license information.
using NUnit.Framework;

namespace Tomlyn.Tests
{
    public class ValidatorTests
    {
        [Test]
        public void TestNoErrors()
        {
            var input = @"a = 1";
            var doc = Toml.Parse(input);
            Assert.False(doc.HasErrors, "The document should not have any errors");
            var docAsStr = doc.ToString();
            Assert.AreEqual(input, docAsStr);
        }

        [Test]
        public void TestRedefineKey()
        {
            var input = @"a = 1
a = true
";
            var doc = Toml.Parse(input);
            var roundTrip = doc.ToString();
            StandardTests.Dump(input, doc, roundTrip);
            Assert.True(doc.HasErrors, "The document should have errors");
        }

        [Test]
        public void TestRedefineKeyAndTable()
        {
            var input = @"a = 1
[a]
b = 1
";
            var doc = Toml.Parse(input);
            var roundTrip = doc.ToString();
            StandardTests.Dump(input, doc, roundTrip);
            Assert.True(doc.HasErrors, "The document should have errors");
        }


        [Test]
        public void TestTableArray()
        {
            var input = @"[[a]]
b = 1
[[a]]
b = true
";
            var doc = Toml.Parse(input);
            var roundTrip = doc.ToString();
            StandardTests.Dump(input, doc, roundTrip);
            Assert.False(doc.HasErrors, "The document should not have any errors");
        }


        [Test]
        public void TestTableArrayNested()
        {
            var input = @"[[a]]
b = 1
[[a.c]]
b = true
";
            var doc = Toml.Parse(input);
            var roundTrip = doc.ToString();
            StandardTests.Dump(input, doc, roundTrip);
            Assert.False(doc.HasErrors, "The document should not have any errors");
        }


        [Test]
        public void TestTableArrayAndInvalidTable()
        {
            var input = @"[[a]]
b = 1
[[a.b]]
c = true
[a.b]
d = true
";
            var doc = Toml.Parse(input);
            var roundTrip = doc.ToString();
            StandardTests.Dump(input, doc, roundTrip);
            Assert.True(doc.HasErrors, "The document should have errors");
        }

        [Test]
        public void TestAllowTab()
        {
            var input = "";
            input += "tab_multiline_basic=\"\"\"\t\"\"\"\n";
            input += "tab_singleline_literal='\t'\n";
            input += "tab_multiline_literal='''\t'''\n";
            var doc = Toml.Parse(input);
            Assert.False(doc.HasErrors, "The document should not have errors");
        }

    }
}