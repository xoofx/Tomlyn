using System;
using NUnit.Framework;
using Tomlyn.Parsing;

namespace Tomlyn.Tests;

public sealed class AllocationFreeParsingTests
{
#if NET10_0_OR_GREATER
    [Test]
    public void TomlLexer_MoveNext_NoAllocations()
    {
        var toml = "# comment\n" +
                   "a = 1\n" +
                   "b = true\n" +
                   "arr = [1, 2, 3]\n" +
                   "inline = {\n" +
                   "  c = 1,\n" +
                   "  d = 2,\n" +
                   "}\n";

        var lexerOptions = new TomlLexerOptions { DecodeScalars = false };

        WarmUpLexer(toml, lexerOptions);

        var lexer = TomlLexer.Create(toml, lexerOptions, "alloc.toml");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var checksum = 0;
        while (lexer.MoveNext())
        {
            var token = lexer.Current;
            checksum = unchecked(checksum + (int)token.Kind + token.Start.Offset + token.End.Offset);
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.AreNotEqual(0, checksum, "Sanity check: token loop should execute.");
        Assert.AreEqual(0, after - before, "Lexer iteration should not allocate.");
    }

    [Test]
    public void TomlParser_MoveNext_NoAllocations()
    {
        var toml = "a = 1\n" +
                   "b = \"hello\"\n" +
                   "arr = [1, 2, 3]\n" +
                   "inline = {\n" +
                   "  c = 1,\n" +
                   "  d = 2,\n" +
                   "}\n";

        var parserOptions = new Tomlyn.Parsing.TomlParserOptions
        {
            DecodeScalars = false,
            Mode = TomlParserMode.Strict,
        };

        WarmUpParser(toml, parserOptions);

        var parser = TomlParser.Create(toml, parserOptions);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var checksum = 0;
        while (parser.MoveNext())
        {
            var evt = parser.Current;
            var span = evt.Span;
            checksum = unchecked(checksum + (int)evt.Kind + (span?.Start.Offset ?? 0) + (span?.End.Offset ?? 0));
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.AreNotEqual(0, checksum, "Sanity check: event loop should execute.");
        Assert.AreEqual(0, after - before, "Parser iteration should not allocate.");
    }

    private static void WarmUpLexer(string toml, TomlLexerOptions lexerOptions)
    {
        var lexer = TomlLexer.Create(toml, lexerOptions, "warmup.toml");
        while (lexer.MoveNext())
        {
        }
    }

    private static void WarmUpParser(string toml, Tomlyn.Parsing.TomlParserOptions parserOptions)
    {
        var parser = TomlParser.Create(toml, parserOptions);
        while (parser.MoveNext())
        {
        }
    }
#else
    [Test]
    public void TomlLexer_MoveNext_NoAllocations()
    {
        Assert.Ignore("Allocation assertions require GC.GetAllocatedBytesForCurrentThread (not available on NETFRAMEWORK).");
    }

    [Test]
    public void TomlParser_MoveNext_NoAllocations()
    {
        Assert.Ignore("Allocation assertions require GC.GetAllocatedBytesForCurrentThread (not available on NETFRAMEWORK).");
    }
#endif
}
