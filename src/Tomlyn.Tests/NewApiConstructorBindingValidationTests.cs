using NUnit.Framework;

namespace Tomlyn.Tests;

public sealed class NewApiConstructorBindingValidationTests
{
    private sealed class CollisionType
    {
        public CollisionType(int a, int A)
        {
            Lower = a;
            Upper = A;
        }

        public int Lower { get; }

        public int Upper { get; }
    }

    [Test]
    public void ConstructorParameterNameCollision_ThrowsTomlException()
    {
        var options = TomlSerializerOptions.Default with
        {
            PropertyNameCaseInsensitive = true,
        };

        Assert.Throws<TomlException>(() => TomlSerializer.Serialize(new CollisionType(1, 2), options));
    }
}

