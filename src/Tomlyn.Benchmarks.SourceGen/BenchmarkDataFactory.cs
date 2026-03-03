using System.Text;

namespace Tomlyn.Benchmarks;

internal static class BenchmarkDataFactory
{
    public static BenchmarkDocument CreateDocument(int serviceCount, int endpointCountPerService)
    {
        var document = new BenchmarkDocument
        {
            Title = "Benchmark",
            Owner = new BenchmarkOwner
            {
                Name = "Ada",
                Organization = "OpenAI",
            },
            Services = new BenchmarkService[serviceCount],
        };

        for (var i = 0; i < serviceCount; i++)
        {
            var service = new BenchmarkService
            {
                Name = "service-" + i.ToString(),
                Host = "10.0.0." + (i % 255).ToString(),
                Port = 8000 + (i % 1000),
                Endpoints = new BenchmarkEndpoint[endpointCountPerService],
            };

            for (var j = 0; j < endpointCountPerService; j++)
            {
                service.Endpoints[j] = new BenchmarkEndpoint
                {
                    Name = "endpoint-" + j.ToString(),
                    Url = "https://example.com/" + i.ToString() + "/" + j.ToString(),
                    Enabled = true,
                };
            }

            document.Services[i] = service;
        }

        return document;
    }

    public static string CreateToml(BenchmarkDocument document)
    {
        return TomlSerializer.Serialize(document, options: new TomlSerializerOptions
        {
            WriteIndented = true,
        });
    }

    public static byte[] ToUtf8Bytes(string text) => Encoding.UTF8.GetBytes(text);
}

