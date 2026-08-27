using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SandPlanet.Prototype.DataDriven
{
    /// <summary>
    /// Small RFC4180-compatible CSV reader shared by runtime and editor tooling.
    /// It intentionally keeps every field as text; typed conversion happens in definition loaders.
    /// </summary>
    public static class SandPlanetCsv04
    {
        public static List<Dictionary<string, string>> Parse(string csvText)
        {
            List<List<string>> records = ParseRecords(csvText ?? string.Empty);
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            if (records.Count == 0)
                return result;

            List<string> headers = records[0];
            if (headers.Count > 0)
                headers[0] = headers[0].TrimStart('\uFEFF');

            for (int r = 1; r < records.Count; r++)
            {
                List<string> record = records[r];
                bool hasAny = false;
                Dictionary<string, string> row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int c = 0; c < headers.Count; c++)
                {
                    string key = headers[c] ?? string.Empty;
                    if (string.IsNullOrEmpty(key))
                        continue;

                    string value = c < record.Count ? record[c] ?? string.Empty : string.Empty;
                    if (!string.IsNullOrEmpty(value))
                        hasAny = true;
                    row[key] = value;
                }

                if (hasAny)
                    result.Add(row);
            }

            return result;
        }

        private static List<List<string>> ParseRecords(string text)
        {
            List<List<string>> records = new List<List<string>>();
            List<string> currentRecord = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(ch);
                    }
                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == ',')
                {
                    currentRecord.Add(field.ToString());
                    field.Length = 0;
                }
                else if (ch == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                    currentRecord.Add(field.ToString());
                    field.Length = 0;
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                }
                else if (ch == '\n')
                {
                    currentRecord.Add(field.ToString());
                    field.Length = 0;
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                }
                else
                {
                    field.Append(ch);
                }
            }

            if (field.Length > 0 || currentRecord.Count > 0)
            {
                currentRecord.Add(field.ToString());
                records.Add(currentRecord);
            }

            return records;
        }

        public static string Get(Dictionary<string, string> row, string key, string fallback = "")
        {
            if (row != null && row.TryGetValue(key, out string value) && value != null)
                return value;
            return fallback;
        }

        public static int GetInt(Dictionary<string, string> row, string key, int fallback = 0)
        {
            string value = Get(row, key, string.Empty);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
        }

        public static bool GetBool(Dictionary<string, string> row, string key, bool fallback = false)
        {
            string value = Get(row, key, string.Empty);
            if (string.IsNullOrWhiteSpace(value))
                return fallback;
            return string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "YES", StringComparison.OrdinalIgnoreCase);
        }
    }
}
