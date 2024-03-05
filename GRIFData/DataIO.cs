using DAGS;
using GROD;
using System.Text;
using System.Text.Json;

namespace GRIFData;

public static class DataIO
{
    public static void LoadData(string path, Grod grod)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }
        var data = File.ReadAllText(path);
        // Fix non-standard JSON
        data = data.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        // Deserialize into a new dictionary
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(data, READ_OPTIONS) ??
            throw new FileLoadException($"Error loading file: {path}");
        // Load into GROD
        foreach (KeyValuePair<string, string> kv in dict)
        {
            var value = kv.Value;
            if (value.TrimStart().StartsWith('@'))
            {
                // clean up any whitespace
                value = Dags.PrettyScript(value);
            }
            grod.Add(kv.Key, value);
        }
    }

    public static void SaveData(string path, Grod grod)
    {
        var keys = grod.Keys.ToList();
        keys.Sort(CompareKeys);
        WriteData(path, grod, keys);
    }

    public static void SaveDataOverlay(string path, Grod grod)
    {
        var keys = grod.KeysOverlay.ToList();
        keys.Sort(CompareKeys);
        WriteData(path, grod, keys);
    }

    #region Private

    private static readonly StringComparison OIC = StringComparison.OrdinalIgnoreCase;

    private static readonly JsonSerializerOptions READ_OPTIONS = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static void WriteData(string path, Grod grod, List<string> keys)
    {
        StringBuilder result = new();
        result.AppendLine("{");
        foreach (string key in keys)
        {
            var value = grod[key];
            result.Append("\t\"");
            result.Append(EncodeString(key));
            result.Append("\":");
            if (value.TrimStart().StartsWith('@'))
            {
                result.AppendLine();
                result.Append("\t\t\"");
                result.Append(EncodeString(Dags.PrettyScript(value).TrimStart().Replace("\r\n", "\r\n\t\t")));
            }
            else
            {
                result.Append(" \"");
                result.Append(EncodeString(value));
            }
            result.AppendLine("\",");
        }
        result.AppendLine("}");
        File.WriteAllText(path, result.ToString());
    }

    private static string EncodeString(string value)
    {
        StringBuilder result = new();
        foreach (char c in value)
        {
            if (c < ' ' || c > '~')
            {
                if (value.StartsWith('@') && (c == '\r' || c == '\n' || c == '\t'))
                {
                    result.Append(c);
                }
                else
                {
                    result.Append("\\u");
                    result.Append($"{(int)c:x4}");
                }
            }
            else if (c == '"' || c == '\\')
            {
                result.Append('\\');
                result.Append(c);
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Key comparison function, returns -1/0/1. Used in keys.Sort(CompareKeys);
    /// Handles numeric key sections in numeric order, not alphabetic order.
    /// </summary>
    private static int CompareKeys(string x, string y)
    {
        if (x == null)
        {
            if (y == null) return 0;
            return -1;
        }
        if (y == null)
        {
            return 1;
        }
        if (x.Equals(y, OIC)) return 0;
        var xTokens = x.Split('.');
        var yTokens = y.Split('.');
        for (int i = 0; i < Math.Max(xTokens.Length, yTokens.Length); i++)
        {
            if (i >= xTokens.Length) return -1; // x is shorter and earlier
            if (i >= yTokens.Length) return 1; // y is shorter and earlier
            if (xTokens[i].Equals(yTokens[i], OIC)) continue;
            if (xTokens[i] == "*") return -1; // "*" comes first so x is earlier
            if (yTokens[i] == "*") return 1; // "*" comes first so y is earlier
            if (int.TryParse(xTokens[i], out int xVal) && int.TryParse(yTokens[i], out int yVal))
            {
                if (xVal == yVal) continue;
                return (xVal < yVal) ? -1 : 1;
            }
            return string.Compare(xTokens[i], yTokens[i], OIC);
        }
        return 0;
    }

    #endregion
}
