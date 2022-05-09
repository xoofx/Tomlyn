// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Tomlyn.Helpers;
using Tomlyn.Model;
using Tomlyn.Syntax;

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


    [Test]
    public void TestDescendants()
    {
        var input = @"# This is a comment
[table]
key = 1 # This is another comment
test.sub.key = ""yes""
[[array]]
hello = true
".ReplaceLineEndings("\r\n");

        var tokens = Toml.Parse(input).Tokens().ToList();
        var builder = new StringBuilder();
        foreach (var node in tokens)
        {
            if (node is SyntaxTrivia trivia)
            {
                builder.AppendLine($"trivia: {trivia.Span}  {trivia.Kind} {(trivia.Text is not null ? TomlFormatHelper.ToString(trivia.Text, TomlPropertyDisplayKind.Default) : string.Empty)}");
            }
            else if (node is SyntaxToken token)
            {
                builder.AppendLine($"token: {token.Span} {(token.Text is not null ? TomlFormatHelper.ToString(token.Text, TomlPropertyDisplayKind.Default) : string.Empty)}");
            }
        }


        var actual = builder.ToString();
        var expected = @"trivia: (1,1)-(1,19)  Comment ""# This is a comment""
trivia: (1,20)-(1,21)  NewLine ""\r\n""
token: (2,1)-(2,1) ""[""
token: (2,2)-(2,6) ""table""
token: (2,7)-(2,7) ""]""
token: (2,8)-(2,9) ""\r\n""
token: (3,1)-(3,3) ""key""
trivia: (3,4)-(3,4)  Whitespaces "" ""
token: (3,5)-(3,5) ""=""
trivia: (3,6)-(3,6)  Whitespaces "" ""
token: (3,7)-(3,7) ""1""
trivia: (3,8)-(3,8)  Whitespaces "" ""
trivia: (3,9)-(3,33)  Comment ""# This is another comment""
token: (3,34)-(3,35) ""\r\n""
token: (4,1)-(4,4) ""test""
token: (4,5)-(4,5) "".""
token: (4,6)-(4,8) ""sub""
token: (4,9)-(4,9) "".""
token: (4,10)-(4,12) ""key""
trivia: (4,13)-(4,13)  Whitespaces "" ""
token: (4,14)-(4,14) ""=""
trivia: (4,15)-(4,15)  Whitespaces "" ""
token: (4,16)-(4,20) ""\""yes\""""
token: (4,21)-(4,22) ""\r\n""
token: (5,1)-(5,2) ""[[""
token: (5,3)-(5,7) ""array""
token: (5,8)-(5,9) ""]]""
token: (5,10)-(5,11) ""\r\n""
token: (6,1)-(6,5) ""hello""
trivia: (6,6)-(6,6)  Whitespaces "" ""
token: (6,7)-(6,7) ""=""
trivia: (6,8)-(6,8)  Whitespaces "" ""
token: (6,9)-(6,12) ""true""
token: (6,13)-(6,14) ""\r\n""
";
        //Console.WriteLine(builder.ToString());

        AssertHelper.AreEqualNormalizeNewLine(expected, actual, true);
    }


}