using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sanalink.API.Middleware
{
    public static class SensitiveDataMasker
    {
        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "passwordHash",
            "token",
            "accessToken",
            "refreshToken",
            "secret",
            "key",
            "jwt",
            "authorization"
        };

        public static string? Mask(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            try
            {
                var node = JsonNode.Parse(json);
                if (node is JsonObject obj)
                {
                    MaskObject(obj);
                    return obj.ToJsonString();
                }
                return json;
            }
            catch
            {
                // Not valid JSON — return as-is (no sensitive data to mask)
                return json;
            }
        }

        private static void MaskObject(JsonObject obj)
        {
            foreach (var key in obj.Select(k => k.Key).ToList())
            {
                if (SensitiveKeys.Contains(key))
                {
                    obj[key] = "***";
                }
                else if (obj[key] is JsonObject nested)
                {
                    MaskObject(nested);
                }
            }
        }
    }
}
