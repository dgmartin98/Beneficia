using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Bup;

internal static class BupJsonUtils
{
    public static bool TryGetProperty(JsonElement element, out JsonElement value, params string[] candidateNames)
    {
        var normalizedCandidates = candidateNames.Select(NormalizeName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            if (normalizedCandidates.Contains(NormalizeName(property.Name)))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static string? GetString(JsonElement element, params string[] candidateNames)
    {
        if (!TryGetProperty(element, out var value, candidateNames))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => value.ToString()
        };
    }

    public static int? GetInt(JsonElement element, params string[] candidateNames)
    {
        if (!TryGetProperty(element, out var value, candidateNames))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    public static bool GetBool(JsonElement element, params string[] candidateNames)
    {
        if (!TryGetProperty(element, out var value, candidateNames))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => false
        };
    }

    private static string NormalizeName(string name)
    {
        var builder = new StringBuilder(name.Length);

        foreach (var ch in name)
        {
            if (ch == '_' || ch == '-' || char.IsWhiteSpace(ch))
                continue;

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }
}
