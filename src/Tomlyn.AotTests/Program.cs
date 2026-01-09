using System.Text.Json.Serialization;
using Tomlyn;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

var input = @"key_type = ""value""
json_name = ""json_value""";

var model = Toml.ToModel<AotModel>(input);
Assert(model.KeyType == "value", "TomlPropertyName mapping failed.");
Assert(model.JsonName == "json_value", "JsonPropertyName mapping failed.");

var output = Toml.FromModel(model).Trim();
Assert(output.Contains("key_type = \"value\""), "TomlPropertyName serialization failed.");
Assert(output.Contains("json_name = \"json_value\""), "JsonPropertyName serialization failed.");

Console.WriteLine("AOT mapping test passed.");

sealed class AotModel
{
    [TomlPropertyName("key_type")]
    public string? KeyType { get; set; }

    [JsonPropertyName("json_name")]
    public string? JsonName { get; set; }
}
