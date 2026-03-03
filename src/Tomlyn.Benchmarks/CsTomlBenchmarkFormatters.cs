using System.Buffers;
using System.Threading;
using CsToml;
using CsToml.Formatter;
using CsToml.Formatter.Resolver;

namespace Tomlyn.Benchmarks;

internal static class CsTomlBenchmarkFormatters
{
    private static int _registered;

    public static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        TomlValueFormatterResolver.Register(BenchmarkOwnerFormatter.Instance);
        TomlValueFormatterResolver.Register(BenchmarkEndpointFormatter.Instance);
        TomlValueFormatterResolver.Register(BenchmarkServiceFormatter.Instance);
        TomlValueFormatterResolver.Register(BenchmarkDocumentFormatter.Instance);
    }

    private sealed class BenchmarkOwnerFormatter : ITomlValueFormatter<BenchmarkOwner>
    {
        public static readonly BenchmarkOwnerFormatter Instance = new();

        public BenchmarkOwner Deserialize(ref TomlDocumentNode rootNode, CsTomlSerializerOptions options)
        {
            return new BenchmarkOwner
            {
                Name = rootNode["Name"u8].GetString(),
                Organization = rootNode["Organization"u8].GetString(),
            };
        }

        public void Serialize<TBufferWriter>(ref Utf8TomlDocumentWriter<TBufferWriter> writer, BenchmarkOwner target, CsTomlSerializerOptions options)
            where TBufferWriter : IBufferWriter<byte>
        {
            var stringFormatter = options.Resolver.GetFormatter<string>()!;

            writer.WriteKey("Name"u8);
            writer.WriteEqual();
            stringFormatter.Serialize(ref writer, target.Name, options);
            writer.EndKeyValue();

            writer.WriteKey("Organization"u8);
            writer.WriteEqual();
            stringFormatter.Serialize(ref writer, target.Organization, options);
            writer.EndKeyValue(lastValue: true);
        }
    }

    private sealed class BenchmarkEndpointFormatter : ITomlValueFormatter<BenchmarkEndpoint>
    {
        public static readonly BenchmarkEndpointFormatter Instance = new();

        public BenchmarkEndpoint Deserialize(ref TomlDocumentNode rootNode, CsTomlSerializerOptions options)
        {
            return new BenchmarkEndpoint
            {
                Name = rootNode["Name"u8].GetString(),
                Url = rootNode["Url"u8].GetString(),
                Enabled = rootNode["Enabled"u8].GetBool(),
            };
        }

        public void Serialize<TBufferWriter>(ref Utf8TomlDocumentWriter<TBufferWriter> writer, BenchmarkEndpoint target, CsTomlSerializerOptions options)
            where TBufferWriter : IBufferWriter<byte>
        {
            var stringFormatter = options.Resolver.GetFormatter<string>()!;
            var boolFormatter = options.Resolver.GetFormatter<bool>()!;

            writer.WriteKey("Name"u8);
            writer.WriteEqual();
            stringFormatter.Serialize(ref writer, target.Name, options);
            writer.EndKeyValue();

            writer.WriteKey("Url"u8);
            writer.WriteEqual();
            stringFormatter.Serialize(ref writer, target.Url, options);
            writer.EndKeyValue();

            writer.WriteKey("Enabled"u8);
            writer.WriteEqual();
            boolFormatter.Serialize(ref writer, target.Enabled, options);
            writer.EndKeyValue(lastValue: true);
        }
    }

    private sealed class BenchmarkServiceFormatter : ITomlValueFormatter<BenchmarkService>
    {
        public static readonly BenchmarkServiceFormatter Instance = new();

        public BenchmarkService Deserialize(ref TomlDocumentNode rootNode, CsTomlSerializerOptions options)
        {
            var service = new BenchmarkService
            {
                Name = rootNode["Name"u8].GetString(),
                Host = rootNode["Host"u8].GetString(),
                Port = (int)rootNode["Port"u8].GetInt64(),
            };

            var endpointsNode = rootNode["Endpoints"u8];
            if (endpointsNode.TryGetArray(out var endpoints))
            {
                var result = new BenchmarkEndpoint[endpoints.Count];
                for (var i = 0; i < result.Length; i++)
                {
                    var endpointNode = endpointsNode[i];
                    result[i] = BenchmarkEndpointFormatter.Instance.Deserialize(ref endpointNode, options);
                }
                service.Endpoints = result;
            }
            else
            {
                service.Endpoints = [];
            }

            return service;
        }

        public void Serialize<TBufferWriter>(ref Utf8TomlDocumentWriter<TBufferWriter> writer, BenchmarkService target, CsTomlSerializerOptions options)
            where TBufferWriter : IBufferWriter<byte>
        {
            var stringFormatter = options.Resolver.GetFormatter<string>()!;
            var intFormatter = options.Resolver.GetFormatter<int>()!;

            writer.WriteKey("Name"u8);
            writer.WriteEqual();
            stringFormatter.Serialize(ref writer, target.Name, options);
            writer.EndKeyValue();

            writer.WriteKey("Host"u8);
            writer.WriteEqual();
            stringFormatter.Serialize(ref writer, target.Host, options);
            writer.EndKeyValue();

            writer.WriteKey("Port"u8);
            writer.WriteEqual();
            intFormatter.Serialize(ref writer, target.Port, options);
            writer.EndKeyValue(lastValue: true);

            var endpoints = target.Endpoints;
            if (endpoints is null || endpoints.Length == 0)
            {
                return;
            }

            for (var i = 0; i < endpoints.Length; i++)
            {
                writer.BeginArrayOfTablesHeader();
                writer.WriteBytes("Services.Endpoints"u8);
                writer.EndArrayOfTablesHeader();
                writer.WriteNewLine();

                writer.BeginCurrentState(TomlValueState.ArrayOfTableForMulitiLine);
                BenchmarkEndpointFormatter.Instance.Serialize(ref writer, endpoints[i], options);
                writer.EndCurrentState();
                writer.EndKeyValue(false);
            }
        }
    }

    private sealed class BenchmarkDocumentFormatter : ITomlValueFormatter<BenchmarkDocument>
    {
        public static readonly BenchmarkDocumentFormatter Instance = new();

        public BenchmarkDocument Deserialize(ref TomlDocumentNode rootNode, CsTomlSerializerOptions options)
        {
            var ownerNode = rootNode["Owner"u8];
            var document = new BenchmarkDocument
            {
                Title = rootNode["Title"u8].GetString(),
                Owner = BenchmarkOwnerFormatter.Instance.Deserialize(ref ownerNode, options),
            };

            var servicesNode = rootNode["Services"u8];
            if (servicesNode.TryGetArray(out var services))
            {
                var result = new BenchmarkService[services.Count];
                for (var i = 0; i < result.Length; i++)
                {
                    var serviceNode = servicesNode[i];
                    result[i] = BenchmarkServiceFormatter.Instance.Deserialize(ref serviceNode, options);
                }
                document.Services = result;
            }
            else
            {
                document.Services = [];
            }

            return document;
        }

        public void Serialize<TBufferWriter>(ref Utf8TomlDocumentWriter<TBufferWriter> writer, BenchmarkDocument target, CsTomlSerializerOptions options)
            where TBufferWriter : IBufferWriter<byte>
        {
            var stringFormatter = options.Resolver.GetFormatter<string>()!;

            writer.WriteKey("Title"u8);
            writer.WriteEqual();
            stringFormatter.Serialize(ref writer, target.Title, options);
            writer.EndKeyValue();

            writer.WriteTableHeader("Owner"u8);
            writer.WriteNewLine();
            writer.BeginCurrentState(TomlValueState.Table);
            BenchmarkOwnerFormatter.Instance.Serialize(ref writer, target.Owner, options);
            writer.EndCurrentState();

            var services = target.Services;
            if (services is null || services.Length == 0)
            {
                return;
            }

            for (var i = 0; i < services.Length; i++)
            {
                writer.BeginArrayOfTablesHeader();
                writer.WriteKey("Services"u8);
                writer.EndArrayOfTablesHeader();
                writer.WriteNewLine();

                writer.BeginCurrentState(TomlValueState.ArrayOfTableForMulitiLine);
                BenchmarkServiceFormatter.Instance.Serialize(ref writer, services[i], options);
                writer.EndCurrentState();
                writer.EndKeyValue(false);
            }
        }
    }
}
