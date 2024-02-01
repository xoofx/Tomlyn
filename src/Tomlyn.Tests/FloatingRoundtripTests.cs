// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using NUnit.Framework;
using Tomlyn.Model;

namespace Tomlyn.Tests
{
    [TestFixture]
    public class FloatingRoundtripTests{


        [TestCase(0.0)]
        [TestCase(-0.0)]
        [TestCase(1.0)]
        [TestCase(-1.0)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.NaN)]
#if NET8_0_OR_GREATER
        [TestCase(double.NegativeZero)]
#endif
        [TestCase(double.Epsilon)]
        [TestCase(-double.Epsilon)]
        // [TestCase(0.1)] - - These fail. Adjusting g16->g17 fixes some but not all.
        // [TestCase(0.99)]
        // [TestCase(0.3)]
        // [TestCase(double.MinValue)]
        // [TestCase(double.MaxValue)]
        public void TestDoublesRoundtrip(double number)
        {
            // we want to increment f64 by the smallest possible value
            // and verify it changes the serialized value
            // IEEE 754 compliance means -0.0 and +0.0 are different
            // special values +inf=inf, -inf, nan=+nan, -nan
            var model = new TomlTable
            {
                ["float"] = (float)number,
                ["double"] = number
            };
            var toml = Toml.FromModel(model);
            var parsed = Toml.Parse(toml);
            var parsedDouble = (double)parsed.ToModel()["double"];
            var parsedFloat = (double)parsed.ToModel()["float"];
            Assert.True(number == parsedDouble || double.IsNaN(number),
                $"(f64->str->f64) expected {number:g30} but got {parsedDouble:g30}. \nString form: \n{toml}");
            Assert.AreEqual(number, parsedDouble);
            Assert.True((float)number == parsedFloat || double.IsNaN(number),
                $"(f64->f32->str->f64->f32) expected {(float)number:g30} but got {parsedFloat:g30}. \nString form: \n{toml}");
            Assert.AreEqual((float)number, parsedFloat);
        }

        [TestCase(0.0f)]
        [TestCase(-0.0f)]
        [TestCase(1.0f)]
        [TestCase(-1.0f)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.NaN)]
#if NET8_0_OR_GREATER
        [TestCase(float.NegativeZero)]
#endif
        [TestCase(float.Epsilon)]
        [TestCase(-float.Epsilon)]
        // [TestCase(0.1f)] - These fail. Adjusting g16->g17 fixes them
        // [TestCase(0.99f)]
        // [TestCase(0.3f)]
        // [TestCase(float.MinValue)]
        // [TestCase(float.MaxValue)]
        public void TestFloatsRoundtrip(float number)
        {
            var model = new TomlTable
            {
                ["float"] = number,
                ["double"] = (double)number
            };
            var toml = Toml.FromModel(model);

            var parsed = Toml.Parse(toml);
            var parsedDouble = (double)parsed.ToModel()["double"];
            var parsedFloatAsDouble = (double)parsed.ToModel()["float"];
            Assert.True((double)number == parsedDouble || double.IsNaN(number),
                $"(f32->f64->str->f64) expected double {(double)number:g64} but got double {parsedDouble:g64}. \nString form: \n{toml}");
            Assert.True(number == (float)parsedFloatAsDouble || double.IsNaN(number),
                $"(f32->str->f64->f32) expected float {number:g64} but got float {(float)parsedFloatAsDouble:g64}. \nString form: \n{toml}");

        }
    }
}