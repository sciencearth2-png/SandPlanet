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
    /// SandPlanet_Master.xlsx (v1.6 integrated flows + explicit QuestRole/QuestAction) -> runtime CSV.
    /// Reads XLSX ZIP/XML directly so the Unity project has no Excel package dependency.
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
            new Spec("06_상호작용 플로우", "FlowID", "Interactions.csv"),
            new Spec("07_상태·플래그", "StateID", "States.csv"),
            new Spec("08_이벤트 플로우", "EventID", "Events.csv"),
            new Spec("09_이벤트 트리거", "TriggerID", "EventTriggers.csv"),
            new Spec("10_NPC 일정", "ScheduleID", "NpcSchedules.csv")
        };

        [MenuItem("Tools/SandPlanet/Data v1.6/Export Master Excel to CSV")]
        public static void ExportMenu() => Export(true);

        [MenuItem("Tools/SandPlanet/Data v1.6/Validate Generated CSV")]
        public static void ValidateMenu()
        {
            List<string> errors = ValidateFolder();
            string body = errors.Count == 0 ? "v1.6 CSV 참조 검증 통과" : string.Join("\n", errors.Take(40));
            if (errors.Count > 40) body += $"\n... +{errors.Count - 40}";
            EditorUtility.DisplayDialog("SandPlanet Data v1.6", body, "확인");
        }

        // Compatibility aliases while the scene still has the 0.4 prototype name.
        [MenuItem("Tools/SandPlanet/Data 0.4/Export Master Excel to CSV")]
        private static void ExportLegacyMenu() => Export(true);

        [MenuItem("Tools/SandPlanet/Data 0.4/Validate Generated CSV")]
        private static void ValidateLegacyMenu() => ValidateMenu();

        public static bool Export(bool showDialog)
        {
            if (!File.Exists(MasterWorkbookPath))
            {
                if (showDialog) EditorUtility.DisplayDialog("SandPlanet Data v1.6", "Master Excel이 없습니다.\n" + MasterWorkbookPath, "확인");
                Debug.LogWarning("[SandPlanet v1.6] Master workbook missing: " + MasterWorkbookPath);
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
                        if (!sheets.TryGetValue(spec.Sheet, out string xmlPath))
                            throw new InvalidDataException("시트를 찾을 수 없음: " + spec.Sheet + " (v1.6 Master인지 확인하세요)");
                        ZipArchiveEntry entry = archive.GetEntry(xmlPath);
                        if (entry == null) throw new InvalidDataException("Worksheet XML 없음: " + xmlPath);
                        List<string> csv = ExtractCsv(entry, shared, spec.FirstKey);
                        File.WriteAllLines(Path.Combine(CsvFolderPath, spec.File), csv, new UTF8Encoding(true));
                    }
                }

                // Existing 0.4 scenes still serialize these legacy TextAsset slots.
                // v1.6 no longer uses them, but harmless placeholders avoid forcing scene regeneration.
                File.WriteAllText(Path.Combine(CsvFolderPath, "Choices.csv"), "LegacyUnused\n", new UTF8Encoding(true));
                File.WriteAllText(Path.Combine(CsvFolderPath, "ChoiceBeats.csv"), "LegacyUnused\n", new UTF8Encoding(true));

                AssetDatabase.Refresh();
                List<string> errors = ValidateFolder();
                if (errors.Count == 0) Debug.Log($"[SandPlanet v1.6] Excel → CSV 완료: {Specs.Length} sheets");
                else foreach (string error in errors) Debug.LogError("[SandPlanet v1.6 Data] " + error);

                if (showDialog)
                {
                    string message = errors.Count == 0
                        ? $"Excel → CSV 완료\n{Specs.Length}개 시트\nQuestRole / Interaction / Event Flow 참조 검증 통과"
                        : $"CSV 생성 완료 / 검증 오류 {errors.Count}개\nConsole을 확인하세요.";
                    EditorUtility.DisplayDialog("SandPlanet Data v1.6", message, "확인");
                }
                return errors.Count == 0;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (showDialog) EditorUtility.DisplayDialog("SandPlanet Data v1.6", "Excel → CSV 실패\n" + ex.Message, "확인");
                return false;
            }
        }

        public static bool GeneratedCsvExists() => Specs.All(s => File.Exists(Path.Combine(CsvFolderPath, s.File)));

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
            Dictionary<string, string> targets = relationships.Descendants(pkg + "Relationship")
                .ToDictionary(e => (string)e.Attribute("Id"), e => NormalizePath((string)e.Attribute("Target")), StringComparer.Ordinal);
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
            List<string> output = new List<string>
            {
                CsvLine(Enumerable.Range(1, maxCol).Select(c => rows[header].TryGetValue(c, out string v) ? v : string.Empty))
            };
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

            HashSet<string> locations = UniqueIds(t["Locations"], "LocationID", "Location", errors);
            HashSet<string> characters = UniqueIds(t["Characters"], "CharacterID", "Character", errors);
            HashSet<string> targets = UniqueIds(t["WorldTargets"], "WorldTargetID", "WorldTarget", errors);
            HashSet<string> quests = UniqueIds(t["Quests"], "QuestID", "Quest", errors);
            HashSet<string> steps = UniqueIds(t["QuestSteps"], "QuestStepID", "QuestStep", errors);
            HashSet<string> states = UniqueIds(t["States"], "StateID", "State", errors);
            HashSet<string> events = new HashSet<string>(t["Events"].Select(r => G(r, "EventID")).Where(x => x.Length > 0), StringComparer.Ordinal);
            UniqueIds(t["EventTriggers"], "TriggerID", "Trigger", errors);
            UniqueIds(t["NpcSchedules"], "ScheduleID", "Schedule", errors);

            Dictionary<string, string> stepOwner = t["QuestSteps"].Where(r => G(r, "QuestStepID").Length > 0)
                .ToDictionary(r => G(r, "QuestStepID"), r => G(r, "QuestID"), StringComparer.Ordinal);
            foreach (Dictionary<string, string> r in t["Quests"])
            {
                string q = G(r, "QuestID"), s = G(r, "InitialStepID");
                if (!steps.Contains(s) || !stepOwner.TryGetValue(s, out string owner) || owner != q)
                    errors.Add($"Quest {q}: InitialStepID {s} 오류");
            }
            foreach (Dictionary<string, string> r in t["QuestSteps"])
            {
                string e = G(r, "ProgressEventID");
                if (e.Length > 0 && !events.Contains(e)) errors.Add($"QuestStep {G(r, "QuestStepID")}: ProgressEvent 없음 {e}");
            }
            foreach (Dictionary<string, string> r in t["WorldTargets"])
                if (!locations.Contains(G(r, "LocationID"))) errors.Add($"WorldTarget {G(r, "WorldTargetID")}: Location 없음");

            ValidateCharacterQuestPolicy(t["Quests"], errors);
            ValidateInteractionFlows(t["Interactions"], characters, targets, quests, steps, stepOwner, states, events, errors);
            ValidateEventFlows(t["Events"], quests, steps, stepOwner, states, events, errors);

            foreach (Dictionary<string, string> r in t["EventTriggers"])
            {
                string id = G(r, "TriggerID"), e = G(r, "EventID"), loc = G(r, "LocationID");
                if (!events.Contains(e)) errors.Add($"Trigger {id}: Event 없음 {e}");
                if (loc.Length > 0 && !locations.Contains(loc)) errors.Add($"Trigger {id}: Location 없음 {loc}");
            }
            foreach (Dictionary<string, string> r in t["NpcSchedules"])
            {
                string id = G(r, "ScheduleID"), c = G(r, "CharacterID"), loc = G(r, "LocationID");
                if (!characters.Contains(c)) errors.Add($"Schedule {id}: Character 없음 {c}");
                if (!locations.Contains(loc)) errors.Add($"Schedule {id}: Location 없음 {loc}");
            }
            return errors;
        }

        private static void ValidateCharacterQuestPolicy(List<Dictionary<string, string>> quests, List<string> errors)
        {
            foreach (Dictionary<string, string> q in quests)
            {
                if (!string.Equals(G(q, "QuestType"), "CHARACTER", StringComparison.OrdinalIgnoreCase)) continue;
                string id = G(q, "QuestID");
                if (!id.StartsWith("QST_CHAR_", StringComparison.Ordinal))
                    errors.Add($"Character Quest {id}: v1.6 장기 퀘스트 ID는 QST_CHAR_... 형식을 권장/요구합니다.");
            }
        }

        private static void ValidateInteractionFlows(
            List<Dictionary<string, string>> rows, HashSet<string> characters, HashSet<string> targets,
            HashSet<string> quests, HashSet<string> steps, Dictionary<string, string> stepOwner,
            HashSet<string> states, HashSet<string> events, List<string> errors)
        {
            Dictionary<string, HashSet<string>> nodes = rows.GroupBy(r => G(r, "FlowID"))
                .ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(r => G(r, "NodeID")).Where(x => x.Length > 0), StringComparer.Ordinal), StringComparer.Ordinal);
            HashSet<string> rowKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (IGrouping<string, Dictionary<string, string>> group in rows.GroupBy(r => G(r, "FlowID")))
            {
                Dictionary<string, string> first = group.First();
                string flow = group.Key;
                string role = G(first, "QuestRole").ToUpperInvariant();
                string q = G(first, "QuestID");
                if (!(role == "NONE" || role == "OFFER" || role == "PROGRESS"))
                    errors.Add($"Flow {flow}: QuestRole은 NONE/OFFER/PROGRESS 중 하나여야 함 ({role})");
                if ((role == "OFFER" || role == "PROGRESS") && q.Length == 0)
                    errors.Add($"Flow {flow}: QuestRole {role}에는 QuestID가 필요함");
                if (role == "OFFER" && !group.Any(r => string.Equals(G(r, "QuestAction"), "ACTIVATE_QUEST", StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"Flow {flow}: OFFER 플로우에는 ACTIVATE_QUEST 결과가 최소 1개 필요함");
            }

            foreach (Dictionary<string, string> r in rows)
            {
                string flow = G(r, "FlowID"), node = G(r, "NodeID"), choice = G(r, "ChoiceID"), target = G(r, "TargetID"), q = G(r, "QuestID"), step = G(r, "QuestStepID");
                if (flow.Length == 0 || node.Length == 0) { errors.Add("InteractionFlow: FlowID/NodeID 빈 값"); continue; }
                string key = flow + "#" + node + "#" + choice;
                if (!rowKeys.Add(key)) errors.Add($"InteractionFlow 중복 행 키: {key}");
                if (!characters.Contains(target) && !targets.Contains(target)) errors.Add($"Flow {flow}: Target 없음 {target}");
                if (q.Length > 0 && !quests.Contains(q)) errors.Add($"Flow {flow}: Quest 없음 {q}");
                if (step.Length > 0 && (!steps.Contains(step) || !stepOwner.TryGetValue(step, out string owner) || owner != q)) errors.Add($"Flow {flow}: QuestStep 오류 {step}");
                string next = G(r, "NextNodeID");
                if (next.Length > 0 && (!nodes.TryGetValue(flow, out HashSet<string> set) || !set.Contains(next))) errors.Add($"Flow {flow}/{node}: NextNode 없음 {next}");
                string emit = G(r, "EmitEventID");
                if (emit.Length > 0 && !events.Contains(emit)) errors.Add($"Flow {flow}/{node}: Event 없음 {emit}");
                ValidateStateSlot(r, "State1", states, "Flow " + flow + "/" + node, errors);
                ValidateStateSlot(r, "State2", states, "Flow " + flow + "/" + node, errors);
                ValidateAffinity(r, "Affinity1CharacterID", characters, "Flow " + flow + "/" + node, errors);
                ValidateAffinity(r, "Affinity2CharacterID", characters, "Flow " + flow + "/" + node, errors);

                string action = G(r, "QuestAction").ToUpperInvariant();
                string actionStep = G(r, "QuestActionStepID");
                if (action.Length > 0 && action != "NONE")
                {
                    if (!quests.Contains(q)) errors.Add($"Flow {flow}/{node}: QuestAction 대상 Quest 없음 {q}");
                    if (action == "SET_QUEST_STEP")
                    {
                        if (!steps.Contains(actionStep)) errors.Add($"Flow {flow}/{node}: 처리 단계 없음 {actionStep}");
                        else if (!stepOwner.TryGetValue(actionStep, out string actionOwner) || actionOwner != q)
                            errors.Add($"Flow {flow}/{node}: 처리 단계 {actionStep}가 Quest {q} 소속이 아님");
                    }
                }

                if (choice.Length > 0 && SandPlanetCsv04.GetInt(r, "TimeCost") < 1)
                    errors.Add($"Flow {flow}/{choice}: 플레이어 선택 TimeCost는 최소 1시간");
                if (action == "ACTIVATE_QUEST" && SandPlanetCsv04.GetInt(r, "TimeCost") < 1)
                    errors.Add($"Flow {flow}/{node}: Quest 수락 상호작용은 최소 1시간");
            }
        }

        private static void ValidateEventFlows(
            List<Dictionary<string, string>> rows, HashSet<string> quests, HashSet<string> steps,
            Dictionary<string, string> stepOwner, HashSet<string> states, HashSet<string> events, List<string> errors)
        {
            Dictionary<string, HashSet<string>> nodes = rows.GroupBy(r => G(r, "EventID"))
                .ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(r => G(r, "NodeID")).Where(x => x.Length > 0), StringComparer.Ordinal), StringComparer.Ordinal);
            HashSet<string> rowKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Dictionary<string, string> r in rows)
            {
                string e = G(r, "EventID"), node = G(r, "NodeID"), choice = G(r, "ChoiceID");
                string key = e + "#" + node + "#" + choice;
                if (!rowKeys.Add(key)) errors.Add($"EventFlow 중복 행 키: {key}");
                string next = G(r, "NextNodeID");
                if (next.Length > 0 && (!nodes.TryGetValue(e, out HashSet<string> set) || !set.Contains(next))) errors.Add($"Event {e}/{node}: NextNode 없음 {next}");
                string emit = G(r, "EmitEventID");
                if (emit.Length > 0 && !events.Contains(emit)) errors.Add($"Event {e}/{node}: 후속 Event 없음 {emit}");
                ValidateStateSlot(r, "State1", states, "Event " + e + "/" + node, errors);
                ValidateStateSlot(r, "State2", states, "Event " + e + "/" + node, errors);
                ValidateStateSlot(r, "State3", states, "Event " + e + "/" + node, errors);
                string action = G(r, "QuestAction").ToUpperInvariant(), q = G(r, "QuestID"), step = G(r, "QuestStepID");
                if (action.Length > 0 && action != "NONE" && !quests.Contains(q)) errors.Add($"Event {e}/{node}: QuestAction 대상 없음 {q}");
                if (action == "SET_QUEST_STEP")
                {
                    if (!steps.Contains(step)) errors.Add($"Event {e}/{node}: QuestStep 없음 {step}");
                    else if (!stepOwner.TryGetValue(step, out string owner) || owner != q) errors.Add($"Event {e}/{node}: QuestStep {step}가 Quest {q} 소속이 아님");
                }
            }
        }

        private static void ValidateStateSlot(Dictionary<string, string> r, string prefix, HashSet<string> states, string owner, List<string> errors)
        {
            string id = G(r, prefix + "ID");
            if (id.Length == 0) return;
            if (!states.Contains(id)) errors.Add(owner + ": State 없음 " + id);
            string change = G(r, prefix + "Change");
            if (!(change.StartsWith("SET ", StringComparison.OrdinalIgnoreCase) || change.StartsWith("ADD ", StringComparison.OrdinalIgnoreCase)))
                errors.Add(owner + $": {prefix}Change는 'SET 값' 또는 'ADD 수치' 형식이어야 함");
        }

        private static void ValidateAffinity(Dictionary<string, string> r, string key, HashSet<string> characters, string owner, List<string> errors)
        {
            string id = G(r, key);
            if (id.Length > 0 && !characters.Contains(id)) errors.Add(owner + ": 호감도 Character 없음 " + id);
        }

        private static HashSet<string> UniqueIds(IEnumerable<Dictionary<string, string>> rows, string key, string label, List<string> errors)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            foreach (Dictionary<string, string> r in rows)
            {
                string id = G(r, key);
                if (id.Length == 0) continue;
                if (!result.Add(id)) errors.Add(label + " ID 중복: " + id);
            }
            return result;
        }

        private static string G(Dictionary<string, string> r, string key) => SandPlanetCsv04.Get(r, key, string.Empty);
    }

    /// <summary>Auto-export when the master workbook changes in the Unity project.</summary>
    internal sealed class SandPlanetSpreadsheetPostprocessor04 : AssetPostprocessor
    {
        private static bool exporting;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (exporting || importedAssets == null || !importedAssets.Contains(SandPlanetSpreadsheetExporter04.MasterWorkbookPath)) return;
            try
            {
                exporting = true;
                SandPlanetSpreadsheetExporter04.Export(false);
            }
            finally
            {
                exporting = false;
            }
        }
    }
}
#endif
