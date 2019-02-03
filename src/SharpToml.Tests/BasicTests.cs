using NUnit.Framework;

namespace SharpToml.Tests
{
    public class BasicTests
    {
        [Test]
        public void Test1()
        {
            var test = @"[table-1]
key1 = ""some string""    # This is a comment
key2 = 123
Key3 = true
Key4 = false
Key5 = +inf

[table-2]
key1 = ""another string""
key2 = 456
";
            var doc = Toml.Parse(test);
            Assert.AreEqual(test, doc.ToString());
        }
    }
}