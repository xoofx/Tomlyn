using System.Linq;
using NUnit.Framework;
using Tomlyn.Serialization.Internal;

namespace Tomlyn.Tests;

public class PooledArrayBuilderTests
{
    [Test]
    public void ToArrayAndReturn_EmptyBuilder_ReturnsEmptyArrayAndResetsCount()
    {
        var builder = new PooledArrayBuilder<int>(initialCapacity: 4);

        var result = builder.ToArrayAndReturn();

        Assert.That(result, Is.Empty);
        Assert.That(builder.Count, Is.Zero);
    }

    [Test]
    public void Add_GrowingBeyondInitialCapacity_PreservesOrderAndCount()
    {
        var builder = new PooledArrayBuilder<int>(initialCapacity: 2);

        for (var value = 1; value <= 33; value++)
        {
            builder.Add(value);
            Assert.That(builder.Count, Is.EqualTo(value));
        }

        var result = builder.ToArrayAndReturn();

        Assert.That(result, Is.EqualTo(Enumerable.Range(1, 33).ToArray()));
        Assert.That(builder.Count, Is.Zero);
    }

    [Test]
    public void ToArrayAndReturn_AfterReuse_ReturnsOnlyNewValues()
    {
        var builder = new PooledArrayBuilder<int>(initialCapacity: 1);
        builder.Add(1);
        builder.Add(2);

        var first = builder.ToArrayAndReturn();

        builder.Add(3);
        builder.Add(4);
        var second = builder.ToArrayAndReturn();

        Assert.That(first, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(second, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void Add_AfterDispose_UsesFreshStorage()
    {
        var builder = new PooledArrayBuilder<string>(initialCapacity: 1);
        builder.Add("alpha");
        builder.Add("beta");

        builder.Dispose();
        builder.Add("gamma");

        var result = builder.ToArrayAndReturn();

        Assert.That(builder.Count, Is.Zero);
        Assert.That(result, Is.EqualTo(new[] { "gamma" }));
    }
}
