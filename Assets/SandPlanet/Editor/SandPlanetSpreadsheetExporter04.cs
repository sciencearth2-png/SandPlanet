#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEditor;
using UnityEngine;

namespace SandPlanet.EditorTools
{
    /// <summary>
    /// Human Master Excel -> runtime CSV exporter for Prototype 0.4.
    /// It reads .xlsx ZIP/XML directly, so no external Excel/Python package is required in Unity.
    /// </summary>
    public static class SandPlanetSpreadsheetExporter04
    {
        public const string MasterWorkbookPath = "Assets/SandPlanet/Data/Authoring/SandPlanet_Master.xlsx";
        public const string CsvFolderPath = "Assets/SandPlanet/Data/Generated/CSV";

        private sealed class Spec
        {
            public readonly string Sheet;
            public readonly string FirstKey;
            public readonly string File;
            public Spec(string sheet, string firstKey, string file) { Sheet = sheet; FirstKey = firstKey; File = file; }
        }

        private static readonly Spec[] Specs =
        {
            new Spec("01_장소", "LocationID", "Locations.csv"),
            new Spec("02_캐릭터", "CharacterID", "Characters.csv"),
            new Spec("03_사물·상호작용 대상", "WorldTargetID", "WorldTargets.csv"),
            new Spec("04_퀘스트", "QuestID", "Quests.csv"),
            new Spec("05_퀘스트 단계", "QuestStepID", "QuestSteps.csv"),
            new Spec("06_상호작용·진입", "InteractionID", "Interactions.csv"),
            new Spec("07_시간 소모 선택지", "ChoiceSetID", "Choices.csv"),
            new Spec("13_선택 후 연출", "BeatID", "ChoiceBeats.csv"),
            new Spec("08_상태·플래그", "StateID", "States.csv"),
            new Spec("09_이벤트", "EventID", "Events.csv"),
            new Spec("10_이벤트 트리거", "TriggerID", "EventTriggers.csv"),
            new Spec("11_NPC 일정", "ScheduleID", "NpcSchedules.csv")
        };

        [MenuItem("Tools/SandPlanet/Data 0.4/Export Master Excel to CSV")]
        public static void ExportMenu() => Export(true);

        [MenuItem("Tools/SandPlanet/Data 0.4/Validate Generated CSV")]
        public static void ValidateMenu()
        {
            List<string> errors = ValidateFolder();
            string body = errors.Count == 0 ? "CSV 참조 검증 통과" : string.Join("\n", errors.Take(30));
            if (errors.Count > 30) body += $"\n... +{errors.Count - 30}";
            EditorUtility.DisplayDialog("SandPlanet Data 0.4", body, "확인");
        }

        public static bool Export(bool showDialog)
        {
            if (!File.Exists(MasterWorkbookPath))
            {
                if (showDialog) EditorUtility.DisplayDialog("SandPlanet Data 0.4", "Master Excel이 없습니다.\n" + MasterWorkbookPath, "확인");
                Debug.LogWarning("[SandPlanet 0.4] Master workbook missing: " + MasterWorkbookPath);
                return false;
            }

            try
            {
                Directory.CreateDirectory(CsvFolderPath);
                using (FileStream fs = File.Open(MasterWorkbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    List<string> shared = ReadSharedStrings(archive);
                    Dictionary<string, string> sheets = ReadSheetTargets(archive);
                    foreach (Spec spec in Specs)
                    {
                        if (!sheets.TryGetValue(spec.Sheet, out string xmlPath)) throw new InvalidDataException("시트를 찾을 수 없음: " + spec.Sheet);
                        ZipArchiveEntry entry = archive.GetEntry(xmlPath);
                        if (entry == null) throw new InvalidDataException("Worksheet XML 없음: " + xmlPath);
                        List<string> csv = ExtractCsv(entry, shared, spec.FirstKey);
                        File.WriteAllLines(Path.Combine(CsvFolderPath, spec.File), csv, new UTF8Encoding(true));
                    }
                }
                AssetDatabase.Refresh();
                List<string> errors = ValidateFolder();
                if (errors.Count == 0) Debug.Log($"[SandPlanet 0.4] Excel → CSV 완료: {Specs.Length} sheets");
                else foreach (string error in errors) Debug.LogError("[SandPlanet 0.4 Data] " + error);
                if (showDialog) EditorUtility.DisplayDialog("SandPlanet Data 0.4", errors.Count == 0 ? $"Excel → CSV 완료\n{Specs.Length}개 시트\n참조 검증 통과" : $"CSV 생성 완료 / 검증 오류 {errors.Count}개\nConsole을 확인하세요.", "확인");
                return errors.Count == 0;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (showDialog) EditorUtility.DisplayDialog("SandPlanet Data 0.4", "Excel → CSV 실패\n" + ex.Message, "확인");
                return false;
            }
        }

        public static bool GeneratedCsvExists()
        {
            return Specs.All(s => File.Exists(Path.Combine(CsvFolderPath, s.File)));
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            List<string> result = new List<string>();
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return result;
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            using (Stream s = entry.Open())
            {
                XDocument doc = XDocument.Load(s);
                foreach (XElement item in doc.Descendants(ns + "si"))
                    result.Add(string.Concat(item.Descendants(ns + "t").Select(t => (string)t)));
            }
            return result;
        }

        private static Dictionary<string, string> ReadSheetTargets(ZipArchive archive)
        {
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace pkg = "http://schemas.openxmlformats.org/package/2006/relationships";
            XDocument workbook, relationships;
            using (Stream s = archive.GetEntry("xl/workbook.xml")?.Open() ?? throw new InvalidDataException("workbook.xml 없음")) workbook = XDocument.Load(s);
            using (Stream s = archive.GetEntry("xl/_rels/workbook.xml.rels")?.Open() ?? throw new InvalidDataException("workbook rels 없음")) relationships = XDocument.Load(s);
            Dictionary<string, string> targets = relationships.Descendants(pkg + "Relationship").ToDictionary(e => (string)e.Attribute("Id"), e => NormalizePath((string)e.Attribute("Target")), StringComparer.Ordinal);
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (XElement sheet in workbook.Descendants(main + "sheet"))
            {
                string name = (string)sheet.Attribute("name");
                string rid = (string)sheet.Attribute(rel + "id");
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(rid) && targets.TryGetValue(rid, out string path)) result[name] = path;
            }
            return result;
        }

        private static string NormalizePath(string target)
        {
            target = (target ?? string.Empty).Replace('\\', '/').TrimStart('/');
            while (target.StartsWith("../", StringComparison.Ordinal)) target = target.Substring(3);
            return target.StartsWith("xl/", StringComparison.Ordinal) ? target : "xl/" + target;
        }

        private static List<string> ExtractCsv(ZipArchiveEntry entry, List<string> shared, string firstKey)
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XDocument doc;
            using (Stream s = entry.Open()) doc = XDocument.Load(s);
            List<Dictionary<int, string>> rows = new List<Dictionary<int, string>>();
            foreach (XElement row in doc.Descendants(ns + "row"))
            {
                Dictionary<int, string> values = new Dictionary<int, string>();
                foreach (XElement cell in row.Elements(ns + "c"))
                {
                    int col = ColumnIndex((string)cell.Attribute("r"));
                    if (col > 0) values[col] = CellValue(cell, ns, shared);
                }
                rows.Add(values);
            }
            int header = rows.FindIndex(r => r.TryGetValue(1, out string value) && value == firstKey);
            if (header < 0) throw new InvalidDataException("머신 헤더를 찾을 수 없음: " + firstKey);
            int maxCol = rows[header].Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).DefaultIfEmpty(1).Max();
            List<string> output = new List<string>();
            output.Add(CsvLine(Enumerable.Range(1, maxCol).Select(c => rows[header].TryGetValue(c, out string v) ? v : string.Empty)));
            for (int r = header + 1; r < rows.Count; r++)
            {
                if (!rows[r].TryGetValue(1, out string id) || string.IsNullOrWhiteSpace(id)) continue;
                output.Add(CsvLine(Enumerable.Range(1, maxCol).Select(c => rows[r].TryGetValue(c, out string v) ? v : string.Empty)));
            }
            return output;
        }

        private static string CellValue(XElement cell, XNamespace ns, List<string> shared)
        {
            string type = (string)cell.Attribute("t") ?? string.Empty;
            if (type == "inlineStr") return string.Concat(cell.Descendants(ns + "t").Select(t => (string)t));
            string raw = (string)cell.Element(ns + "v") ?? string.Empty;
            if (type == "s" && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) && index >= 0 && index < shared.Count) return shared[index];
            if (type == "b") return raw == "1" ? "TRUE" : "FALSE";
            return raw;
        }

        private static int ColumnIndex(string reference)
        {
            int value = 0;
            foreach (char ch in reference ?? string.Empty)
            {
                if (!char.IsLetter(ch)) break;
                value = value * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
            }
            return value;
        }

        private static string CsvLine(IEnumerable<string> fields) => string.Join(",", fields.Select(Escape));
        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            value = value.Replace("\"", "\"\"");
            return quote ? "\"" + value + "\"" : value;
        }

        private static List<string> ValidateFolder()
        {
            List<string> errors = new List<string>();
            Dictionary<string, List<Dictionary<string, string>>> t = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (Spec spec in Specs)
            {
                string path = Path.Combine(CsvFolderPath, spec.File);
                if (!File.Exists(path)) { errors.Add("CSV 없음: " + spec.File); continue; }
                t[Path.GetFileNameWithoutExtension(spec.File)] = SandPlanetCsv04.Parse(File.ReadAllText(path, Encoding.UTF8));
            }
            if (errors.Count > 0) return errors;

            HashSet<string> locations = Ids(t, "Locations", "LocationID", errors);
            HashSet<string> characters = Ids(t, "Characters", "CharacterID", errors);
            HashSet<string> targets = Ids(t, "WorldTargets", "WorldTargetID", errors);
            HashSet<string> quests = Ids(t, "Quests", "QuestID", errors);
            HashSet<string> steps = Ids(t, "QuestSteps", "QuestStepID", errors);
            HashSet<string> choices = Ids(t, "Choices", "ChoiceID", errors);
            HashSet<string> states = Ids(t, "States", "StateID", errors);
            HashSet<string> events = Ids(t, "Events", "EventID", errors);
            Ids(t, "Interactions", "InteractionID", errors);
            Ids(t, "ChoiceBeats", "BeatID", errors);
            Ids(t, "EventTriggers", "TriggerID", errors);
            Ids(t, "NpcSchedules", "ScheduleID", errors);

            HashSet<string> choiceSets = new HashSet<string>(t["Choices"].Select(r => G(r, "ChoiceSetID")).Where(x => x.Length > 0), StringComparer.Ordinal);
            Dictionary<string, string> stepOwner = t["QuestSteps"].Where(r => G(r, "QuestStepID").Length > 0).ToDictionary(r => G(r, "QuestStepID"), r => G(r, "QuestID"), StringComparer.Ordinal);

            foreach (Dictionary<string, string> r in t["Quests"])
            {
                string q = G(r, "QuestID"), s = G(r, "InitialStepID");
                if (!steps.Contains(s) || !stepOwner.TryGetValue(s, out string owner) || owner != q) errors.Add($"Quest {q}: InitialStepID {s} 오류");
            }
            foreach (Dictionary<string, string> r in t["WorldTargets"])
                if (!locations.Contains(G(r, "LocationID"))) errors.Add($"WorldTarget {G(r, "WorldTargetID")}: Location 없음");
            foreach (Dictionary<string, string> r in t["Interactions"])
            {
                string id = G(r, "InteractionID"), type = G(r, "TargetType"), target = G(r, "TargetID"), q = G(r, "QuestID"), s = G(r, "QuestStepID"), set = G(r, "ChoiceSetID");
                if (type == "CHARACTER" && !characters.Contains(target)) errors.Add($"Interaction {id}: Character 없음 {target}");
                if (type == "WORLD_TARGET" && !targets.Contains(target)) errors.Add($"Interaction {id}: WorldTarget 없음 {target}");
                if (q.Length > 0 && !quests.Contains(q)) errors.Add($"Interaction {id}: Quest 없음 {q}");
                if (s.Length > 0 && (!steps.Contains(s) || !stepOwner.TryGetValue(s, out string owner) || owner != q)) errors.Add($"Interaction {id}: QuestStep 오류 {s}");
                if (!choiceSets.Contains(set)) errors.Add($"Interaction {id}: ChoiceSet 없음 {set}");
            }

            HashSet<string> interactionSets = new HashSet<string>(t["Interactions"].Select(r => G(r, "ChoiceSetID")), StringComparer.Ordinal);
            foreach (Dictionary<string, string> r in t["Choices"])
            {
                if (interactionSets.Contains(G(r, "ChoiceSetID")) && SandPlanetCsv04.GetInt(r, "TimeCost") < 1) errors.Add($"Choice {G(r, "ChoiceID")}: 일반 Interaction은 최소 1시간");
                ValidateResult(r, "Result1", states, events, quests, errors);
                ValidateResult(r, "Result2", states, events, quests, errors);
                ValidateResult(r, "Result3", states, events, quests, errors);
            }

            HashSet<string> beatOrderKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Dictionary<string, string> r in t["ChoiceBeats"])
            {
                string beat = G(r, "BeatID");
                string choice = G(r, "ChoiceID");
                int order = SandPlanetCsv04.GetInt(r, "BeatOrder");
                string speakerType = G(r, "SpeakerType");
                string speaker = G(r, "SpeakerID");
                if (!choices.Contains(choice)) errors.Add($"ChoiceBeat {beat}: Choice 없음 {choice}");
                if (order < 1) errors.Add($"ChoiceBeat {beat}: BeatOrder는 1 이상이어야 함");
                if (!beatOrderKeys.Add(choice + "#" + order.ToString(CultureInfo.InvariantCulture))) errors.Add($"ChoiceBeat {beat}: 같은 ChoiceID에 BeatOrder {order} 중복");
                if (speakerType == "CHARACTER" && !characters.Contains(speaker)) errors.Add($"ChoiceBeat {beat}: Character 없음 {speaker}");
            }

            foreach (Dictionary<string, string> r in t["Events"])
            {
                string q = G(r, "QuestID"), s = G(r, "QuestStepID"), set = G(r, "ChoiceSetID");
                if (q.Length > 0 && !quests.Contains(q)) errors.Add($"Event {G(r, "EventID")}: Quest 없음 {q}");
                if (s.Length > 0 && !steps.Contains(s)) errors.Add($"Event {G(r, "EventID")}: QuestStep 없음 {s}");
                if (set.Length > 0 && !choiceSets.Contains(set)) errors.Add($"Event {G(r, "EventID")}: ChoiceSet 없음 {set}");
                ValidateResult(r, "EventResult1", states, events, quests, errors);
                ValidateResult(r, "EventResult2", states, events, quests, errors);
                ValidateResult(r, "EventResult3", states, events, quests, errors);
            }
            foreach (Dictionary<string, string> r in t["EventTriggers"])
            {
                if (!events.Contains(G(r, "EventID"))) errors.Add($"Trigger {G(r, "TriggerID")}: Event 없음 {G(r, "EventID")}");
                string loc = G(r, "LocationID");
                if (loc.Length > 0 && !locations.Contains(loc)) errors.Add($"Trigger {G(r, "TriggerID")}: Location 없음 {loc}");
            }
            foreach (Dictionary<string, string> r in t["NpcSchedules"])
            {
                if (!characters.Contains(G(r, "CharacterID"))) errors.Add($"Schedule {G(r, "ScheduleID")}: Character 없음");
                if (!locations.Contains(G(r, "LocationID"))) errors.Add($"Schedule {G(r, "ScheduleID")}: Location 없음");
            }
            return errors;
        }

        private static HashSet<string> Ids(Dictionary<string, List<Dictionary<string, string>>> tables, string table, string key, List<string> errors)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);
            foreach (Dictionary<string, string> r in tables[table])
            {
                string id = G(r, key);
                if (id.Length > 0 && !set.Add(id)) errors.Add("중복 " + key + ": " + id);
            }
            return set;
        }

        private static void ValidateResult(Dictionary<string, string> r, string prefix, HashSet<string> states, HashSet<string> events, HashSet<string> quests, List<string> errors)
        {
            string type = G(r, prefix + "Type"), targetType = G(r, prefix + "TargetType"), target = G(r, prefix + "TargetID");
            if (type.Length == 0) return;
            if (type == "EMIT_EVENT" && !events.Contains(target)) errors.Add(prefix + ": Event 없음 " + target);
            if (targetType == "STATE" && !states.Contains(target)) errors.Add(prefix + ": State 없음 " + target);
            if (targetType == "QUEST" && !quests.Contains(target)) errors.Add(prefix + ": Quest 없음 " + target);
        }

        private static string G(Dictionary<string, string> r, string key) => SandPlanetCsv04.Get(r, key, string.Empty);
    }

    public sealed class SandPlanetMasterExcelWatcher04 : AssetPostprocessor
    {
        private static bool exporting;
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (exporting || imported == null || !imported.Contains(SandPlanetSpreadsheetExporter04.MasterWorkbookPath)) return;
            exporting = true;
            try { SandPlanetSpreadsheetExporter04.Export(false); }
            finally { exporting = false; }
        }
    }
}
#endif