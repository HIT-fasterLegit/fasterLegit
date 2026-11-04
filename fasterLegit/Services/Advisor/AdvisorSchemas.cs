using System.Text.Json.Nodes;
using fasterLegit.Domain.Advisor;

namespace fasterLegit.Services.Advisor;

public static class AdvisorSchemas
{
    public const string Planner = """
        {"type":"object","properties":{"requestedTools":{"type":"array","maxItems":4,"items":{"type":"string","enum":["measure_resources","inspect_storage","inspect_startup","inspect_apps"]}},"followUpQuestion":{"type":"string"}},"required":["requestedTools","followUpQuestion"],"additionalProperties":false}
        """;

    public static string Analysis(AdvisorCollection collection)
    {
        var node = JsonNode.Parse(
            """
            {"type":"object","properties":{"summary":{"type":"string","minLength":1},"findings":{"type":"array","maxItems":6,"items":{"type":"object","properties":{"title":{"type":"string","minLength":1},"explanation":{"type":"string","minLength":1},"evidenceIds":{"type":"array","minItems":1,"items":{"type":"string","enum":[]}}},"required":["title","explanation","evidenceIds"],"additionalProperties":false}},"actionIds":{"type":"array","maxItems":8,"items":{"type":"string","enum":[]}}},"required":["summary","findings","actionIds"],"additionalProperties":false}
            """
        )!;
        var properties = node["properties"]!;
        properties["findings"]!["items"]!["properties"]!["evidenceIds"]!["items"]!["enum"] =
            new JsonArray(
                collection.Evidence.Select(e => (JsonNode?)JsonValue.Create(e.Id)).ToArray()
            );
        properties["actionIds"]!["items"]!["enum"] = new JsonArray(
            collection.Actions.Select(a => (JsonNode?)JsonValue.Create(a.Id)).ToArray()
        );
        return node.ToJsonString();
    }
}
