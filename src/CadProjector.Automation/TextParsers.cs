using System.Text.Json;

namespace CadProjector.Automation;

public readonly record struct TextCommandToken(string Name, string Argument);

/// <summary>Legacy TCP/UDP text: Name:arg;Name2:arg2 (ToCommand.ParseDummys).</summary>
public static class TextCommandParser
{
    public static IReadOnlyList<TextCommandToken> Parse(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return [];

        var result = new List<TextCommandToken>();
        // Legacy splits on ';' with count 2 — only first segment? Actually Split(';', 2) yields at most 2 parts.
        // Real usage often sends multiple; support all ';' separated tokens.
        foreach (var part in message.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var split = part.Split([':', ' '], 2, StringSplitOptions.TrimEntries);
            var name = split.Length > 0 ? split[0] : "";
            var arg = split.Length > 1 ? split[1] : "";
            if (!string.IsNullOrEmpty(name))
                result.Add(new TextCommandToken(name, arg));
        }
        return result;
    }

    public static RemoteCommand ToRemoteCommand(
        string message,
        string endpointId,
        RemoteEndpointType transport,
        ConnectedClient client)
    {
        var tokens = Parse(message);
        var commands = tokens.Select(t => t.Name).ToArray();
        string? path = null;
        var kind = RemoteCommandKind.Custom;
        var upper = tokens.Select(t => t.Name.ToUpperInvariant()).ToHashSet();

        foreach (var t in tokens)
        {
            var n = t.Name.ToUpperInvariant();
            if (n is "LOAD" or "OPEN" or "FILE" or "FILENAME" or "FILEPATH")
            {
                path = t.Argument;
                kind = RemoteCommandKind.LoadFile;
            }
        }

        if (kind == RemoteCommandKind.Custom)
        {
            if (upper.Contains("PLAY") || upper.Contains("SHOW")) kind = RemoteCommandKind.Play;
            else if (upper.Contains("OFF") || upper.Contains("STOP")) kind = RemoteCommandKind.Stop;
            else if (upper.Contains("CLEAR")) kind = RemoteCommandKind.Clear;
            else if (upper.Contains("ALIGN")) kind = RemoteCommandKind.Align;
        }

        return new RemoteCommand
        {
            EndpointId = endpointId,
            Transport = transport,
            Client = client,
            Header = "Text",
            Commands = commands,
            Kind = kind,
            Path = string.IsNullOrWhiteSpace(path) ? null : path,
            RawText = message,
            ReplyRequested = true
        };
    }
}

public static class JsonCommandParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class JsonDto
    {
        public string? Cmd { get; set; }
        public string? Command { get; set; }
        public string? Path { get; set; }
        public string? File { get; set; }
        public string[]? Commands { get; set; }
    }

    public static bool TryParse(
        string line,
        string endpointId,
        RemoteEndpointType transport,
        ConnectedClient client,
        out RemoteCommand command)
    {
        command = null!;
        try
        {
            var dto = JsonSerializer.Deserialize<JsonDto>(line, Options);
            if (dto is null) return false;
            var cmd = (dto.Cmd ?? dto.Command ?? "").Trim();
            var path = dto.Path ?? dto.File;
            var list = new List<string>();
            if (!string.IsNullOrEmpty(cmd)) list.Add(cmd);
            if (dto.Commands is { Length: > 0 }) list.AddRange(dto.Commands);

            var kind = RemoteCommandKind.Custom;
            var u = cmd.ToUpperInvariant();
            if (u is "LOAD" or "OPEN" or "FILE")
            {
                kind = RemoteCommandKind.LoadFile;
            }
            else if (u is "PLAY" or "SHOW") kind = RemoteCommandKind.Play;
            else if (u is "STOP" or "OFF") kind = RemoteCommandKind.Stop;
            else if (u is "CLEAR") kind = RemoteCommandKind.Clear;
            else if (u is "ALIGN") kind = RemoteCommandKind.Align;

            command = new RemoteCommand
            {
                EndpointId = endpointId,
                Transport = transport,
                Client = client,
                Header = "Json",
                Commands = list,
                Kind = kind,
                Path = path,
                RawText = line,
                ReplyRequested = true
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public static class LineCommandParser
{
    public static RemoteCommand Parse(
        string line,
        string endpointId,
        RemoteEndpointType transport,
        ConnectedClient client)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('{'))
        {
            if (JsonCommandParser.TryParse(trimmed, endpointId, transport, client, out var json))
                return json;
        }
        return TextCommandParser.ToRemoteCommand(trimmed, endpointId, transport, client);
    }
}
