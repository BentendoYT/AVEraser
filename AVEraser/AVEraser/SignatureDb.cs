using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace AVEraser
{
    public class CommunityAVEntry
    {
        public string DisplayName;
        public string[] Processes   = new string[0];
        public string[] Services    = new string[0];
        public string[] RegistryKeys = new string[0];
        public string[] Folders     = new string[0];
        public string   Contributor;
        public string   AddedDate;
    }

    public class SignatureDbResult
    {
        public int       NewEntries;
        public int       UpdatedEntries;
        public string    DbVersion;
        public int       TotalEntries;
        public List<CommunityAVEntry> Entries = new List<CommunityAVEntry>();
    }

    public static class SignatureDb
    {
        private const string SIGNATURES_URL =
            "https://raw.githubusercontent.com/BentendoYT/AVEraser/main/signatures.json";

        private const string REPORT_ISSUE_URL =
            "https://github.com/BentendoYT/AVEraser/issues/new?template=new_signature.md&title=New+AV+Signature%3A+";

        private static SignatureDbResult _cached = null;
        public static async Task<SignatureDbResult> FetchAsync()
        {
            if (_cached != null) return _cached;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("User-Agent", "AVEraser-SignatureDB");

                    string json = await client.GetStringAsync(SIGNATURES_URL);
                    var result = Parse(json);
                    _cached = result;
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public static (int added, int updated) MergeInto(
            Dictionary<string, AVEntry> target,
            SignatureDbResult db)
        {
            if (db == null) return (0, 0);
            int added = 0, updated = 0;

            foreach (var community in db.Entries)
            {
                string key = community.DisplayName;

                if (!target.ContainsKey(key))
                {
                    target[key] = new AVEntry
                    {
                        DisplayName  = community.DisplayName,
                        Processes    = community.Processes,
                        Services     = community.Services,
                        RegistryKeys = community.RegistryKeys,
                        Folders      = community.Folders,
                        BundledApps  = new BundledApp[0]
                    };
                    added++;
                }
                else
                {
                    var existing = target[key];
                    bool changed = false;

                    existing.Processes    = MergeArrays(existing.Processes,    community.Processes,    ref changed);
                    existing.Services     = MergeArrays(existing.Services,     community.Services,     ref changed);
                    existing.RegistryKeys = MergeArrays(existing.RegistryKeys, community.RegistryKeys, ref changed);
                    existing.Folders      = MergeArrays(existing.Folders,      community.Folders,      ref changed);

                    if (changed) updated++;
                }
            }

            return (added, updated);
        }

        private static string[] MergeArrays(string[] existing, string[] incoming, ref bool changed)
        {
            if (incoming == null || incoming.Length == 0) return existing;
            var set = new System.Collections.Generic.HashSet<string>(
                existing, StringComparer.OrdinalIgnoreCase);
            foreach (var s in incoming)
            {
                if (set.Add(s)) changed = true;
            }
            if (!changed) return existing;
            var result = new string[set.Count];
            set.CopyTo(result);
            return result;
        }

        private static SignatureDbResult Parse(string json)
        {
            var result = new SignatureDbResult();
            result.DbVersion   = ExtractString(json, "db_version") ?? "unknown";
            result.TotalEntries = ExtractInt(json,    "total_entries");

            int arrStart = json.IndexOf("\"entries\"");
            if (arrStart < 0) return result;
            int bracketOpen = json.IndexOf('[', arrStart);
            if (bracketOpen < 0) return result;

            int pos = bracketOpen + 1;
            while (pos < json.Length)
            {
                int objStart = json.IndexOf('{', pos);
                if (objStart < 0) break;
                int objEnd = FindMatchingBrace(json, objStart);
                if (objEnd < 0) break;

                string obj = json.Substring(objStart, objEnd - objStart + 1);
                var entry = ParseEntry(obj);
                if (entry != null && !string.IsNullOrWhiteSpace(entry.DisplayName))
                    result.Entries.Add(entry);

                pos = objEnd + 1;
                int nextBracket = json.IndexOf(']', pos);
                int nextObj     = json.IndexOf('{', pos);
                if (nextBracket >= 0 && (nextObj < 0 || nextBracket < nextObj)) break;
            }

            result.NewEntries = result.Entries.Count;
            return result;
        }

        private static CommunityAVEntry ParseEntry(string obj)
        {
            var e = new CommunityAVEntry();
            e.DisplayName  = ExtractString(obj, "name")        ?? "";
            e.Contributor  = ExtractString(obj, "contributor") ?? "community";
            e.AddedDate    = ExtractString(obj, "added")       ?? "";
            e.Processes    = ExtractArray(obj,  "processes");
            e.Services     = ExtractArray(obj,  "services");
            e.RegistryKeys = ExtractArray(obj,  "registry_keys");
            e.Folders      = ExtractArray(obj,  "folders");
            return e;
        }

        private static string ExtractString(string json, string key)
        {
            var m = Regex.Match(json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return m.Success
                ? m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\")
                : null;
        }

        private static int ExtractInt(string json, string key)
        {
            var m = Regex.Match(json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        private static string[] ExtractArray(string json, string key)
        {
            int idx = json.IndexOf("\"" + key + "\"");
            if (idx < 0) return new string[0];
            int open = json.IndexOf('[', idx);
            if (open < 0) return new string[0];
            int close = json.IndexOf(']', open);
            if (close < 0) return new string[0];
            string inner = json.Substring(open + 1, close - open - 1);
            var matches = Regex.Matches(inner, "\"((?:[^\"\\\\]|\\\\.)*)\"");
            var list = new List<string>();
            foreach (Match m in matches) list.Add(m.Groups[1].Value);
            return list.ToArray();
        }

        private static int FindMatchingBrace(string s, int open)
        {
            int depth = 0;
            bool inStr = false;
            for (int i = open; i < s.Length; i++)
            {
                if (inStr) { if (s[i] == '\\') i++; else if (s[i] == '"') inStr = false; continue; }
                if (s[i] == '"') { inStr = true; continue; }
                if (s[i] == '{') depth++;
                else if (s[i] == '}') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        public static void OpenReportPage(string avName = "")
        {
            string url = REPORT_ISSUE_URL + Uri.EscapeDataString(avName);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                { UseShellExecute = true });
        }
    }
}
