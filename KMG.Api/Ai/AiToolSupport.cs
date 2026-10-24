using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace KMG.Api.Ai
{
    public class ToolArgumentException : Exception
    {
        public ToolArgumentException(string message) : base(message) { }
    }

    public class ToolArgs
    {
        private readonly JsonObject _args;

        public ToolArgs(JsonObject? args)
        {
            _args = args ?? new JsonObject();
        }

        public string? GetString(string name, int maxLength)
        {
            var node = _args[name];
            if (node == null) return null;
            if (node is not JsonValue value || !value.TryGetValue<string>(out var text))
                throw new ToolArgumentException($"'{name}' must be a string.");

            text = text.Trim();
            if (text.Length > maxLength)
                throw new ToolArgumentException($"'{name}' must be at most {maxLength} characters.");
            return text.Length == 0 ? null : text;
        }

        public int GetInt(string name, int defaultValue, int min, int max) =>
            GetOptionalInt(name, min, max) ?? defaultValue;

        public int GetRequiredInt(string name, int min, int max) =>
            GetOptionalInt(name, min, max) ?? throw new ToolArgumentException($"'{name}' is required.");

        public int? GetOptionalInt(string name, int min, int max)
        {
            var node = _args[name];
            if (node == null) return null;

            int? number = node.GetValueKind() switch
            {
                JsonValueKind.Number when double.TryParse(node.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                                          && d == Math.Floor(d) && d is >= int.MinValue and <= int.MaxValue => (int)d,
                JsonValueKind.String when int.TryParse(node.GetValue<string>(), out var i) => i,
                _ => null
            };

            if (number == null)
                throw new ToolArgumentException($"'{name}' must be an integer.");
            if (number < min || number > max)
                throw new ToolArgumentException($"'{name}' must be between {min} and {max}.");
            return number;
        }

        public bool GetBool(string name)
        {
            var node = _args[name];
            return node?.GetValueKind() switch
            {
                null => false,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new ToolArgumentException($"'{name}' must be true or false.")
            };
        }

        public DateTime? GetDate(string name)
        {
            var text = GetString(name, 40);
            if (text == null) return null;
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
                return date;
            throw new ToolArgumentException($"'{name}' must be a date in yyyy-MM-dd format.");
        }

        public HashSet<string> GetStringSet(string name, IReadOnlyCollection<string> allowed)
        {
            var node = _args[name];
            if (node == null) return new HashSet<string>();
            var values = node is JsonArray array
                ? array.Select(v => v is JsonValue jv && jv.TryGetValue<string>(out var s) ? s : null).ToList()
                : throw new ToolArgumentException($"'{name}' must be an array.");

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                var match = allowed.FirstOrDefault(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ToolArgumentException($"'{name}' values must be from: {string.Join(", ", allowed)}.");
                result.Add(match);
            }
            return result;
        }

        public TEnum? GetEnum<TEnum>(string name) where TEnum : struct, Enum
        {
            var text = GetString(name, 50);
            if (text == null) return null;
            if (!Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) || !Enum.IsDefined(value) || int.TryParse(text, out _))
                throw new ToolArgumentException($"'{name}' must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return value;
        }
    }

    public static class ArabicText
    {
        private static readonly Regex Diacritics = new("[ً-ْـ]", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalized = Diacritics.Replace(text, "")
                .Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا')
                .Replace('ى', 'ي').Replace('ة', 'ه')
                .ToLowerInvariant();
            return Spaces.Replace(normalized, " ").Trim();
        }

        public static bool ContainsAllTokens(string? query, params string?[] fields)
        {
            var tokens = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return true;

            var haystack = Normalize(string.Join(" ", fields));
            var haystackSkeleton = Skeleton(haystack);
            return tokens.All(t =>
                haystack.Contains(t)
                || (t.Length > 4 && t.StartsWith("ال") && haystack.Contains(t[2..]))
                || (Skeleton(t) is { Length: >= 3 } s && haystackSkeleton.Contains(s)));
        }

        private static string Skeleton(string normalized)
        {
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(StripDefiniteArticle)
                .Select(w => new string(w.Where(c => c is not ('ا' or 'و' or 'ي')).ToArray()));
            return string.Join(" ", words);
        }

        private static string StripDefiniteArticle(string word)
        {
            for (var i = 0; i < 2 && word.Length > 4 && word.StartsWith("ال"); i++)
                word = word[2..];
            return word;
        }

        public static string? Truncate(string? text, int max) =>
            text == null || text.Length <= max ? text : text[..max] + "…";
    }
}
