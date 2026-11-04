using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using fasterLegit.Domain.Advisor;

namespace fasterLegit.Services.Advisor;

public sealed class AdvisorModelClient(HttpClient client, AdvisorSettingsStore store)
    : IAdvisorModelClient
{
    public async Task<string> CompleteAsync(
        string instruction,
        string data,
        CancellationToken cancellationToken,
        string? responseSchema = null
    )
    {
        var settings = store.Load();
        var endpoint = AdvisorSettingsStore.Validate(settings);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(
                endpoint,
                settings.Provider == AdvisorProvider.Local ? "api/chat" : "chat/completions"
            )
        );
        if (settings.Provider == AdvisorProvider.Api)
        {
            var key = store.ReadKey();
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("AI.Error.Key");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }
        var messages = new[]
        {
            new
            {
                role = "system",
                content = instruction
                    + (responseSchema == null ? "" : "\nRequired JSON schema: " + responseSchema),
            },
            new { role = "user", content = data + "\n/no_think" },
        };
        object payload =
            settings.Provider == AdvisorProvider.Local
                ? new
                {
                    model = settings.Model,
                    stream = false,
                    format = responseSchema == null
                        ? (object)"json"
                        : JsonSerializer.Deserialize<JsonElement>(responseSchema),
                    think = false,
                    keep_alive = 0,
                    options = new
                    {
                        temperature = 0.2,
                        num_ctx = 8192,
                        num_predict = 1800,
                    },
                    messages,
                }
                : new
                {
                    model = settings.Model,
                    stream = false,
                    temperature = 0.2,
                    max_tokens = 1800,
                    response_format = new { type = "json_object" },
                    messages,
                };
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"AI.Error.Http:{(int)response.StatusCode}");
        await using var stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > 262144)
                throw new InvalidDataException("AI.Error.Response");
            buffer.Write(chunk, 0, read);
        }
        using var json = JsonDocument.Parse(
            buffer.ToArray(),
            new JsonDocumentOptions { MaxDepth = 32 }
        );
        string? content;
        if (settings.Provider == AdvisorProvider.Local)
            content = json.RootElement.GetProperty("message").GetProperty("content").GetString();
        else
        {
            var choices = json.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                throw new InvalidDataException("AI.Error.Response");
            content = choices[0].GetProperty("message").GetProperty("content").GetString();
        }
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidDataException("AI.Error.Response");
        return content;
    }
}
