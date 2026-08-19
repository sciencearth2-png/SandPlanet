using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.3 playtest instrumentation.
    ///
    /// This component attaches itself at runtime when a Prototype 0.3 controller exists.
    /// It intentionally reads the prototype controller through reflection so logging can be
    /// added without coupling the gameplay controller to debug-only code.
    ///
    /// Visible output:
    /// - bottom-left latest result panel
    /// - bottom-left recent state-change history
    ///
    /// Persistent output:
    /// - Unity Console with [P0.3 LOG] prefix
    /// - ProjectRoot/Logs/SandPlanet_Prototype03_latest.log
    /// - ProjectRoot/Logs/SandPlanet_Prototype03_<timestamp>.log
    /// </summary>
    public sealed class SandPlanetPrototype03DebugLogOverlay : MonoBehaviour
    {
        private const int MaxStoredEntries = 120;
        private const int VisibleEntryCount = 9;
        private const BindingFlags ControllerFieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags NestedFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class StateSnapshot
        {
            public int Day;
            public int Hour;
            public int Will;
            public int MaxWill;

            public int PersonalLevel;
            public int PersonalXp;
            public int SocialLevel;
            public int SocialXp;
            public int TechnicalLevel;
            public int TechnicalXp;

            public string Mode;
            public string Status;

            public Dictionary<string, bool> Flags;
            public Dictionary<string, int> Affinities;

            public object EncounterObject;
            public string EncounterTitle;
            public string EncounterId;
            public bool EncounterCompleted;
            public int EncounterLastCompletedDay;

            public object StoryEventObject;
            public string StoryEventTitle;
            public string StoryEventId;
            public bool StoryEventCompleted;
        }

        private SandPlanetPrototype03Controller controller;
        private readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
        private readonly List<string> entries = new List<string>();

        private StateSnapshot previous;
        private string latestResult = "아직 수행 결과가 없습니다.";
        private string sessionLogPath;
        private string latestLogPath;

        private GUIStyle headerStyle;
        private GUIStyle resultStyle;
        private GUIStyle logStyle;
        private GUIStyle subtleStyle;
        private Texture2D backgroundTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToPrototype03()
        {
            SandPlanetPrototype03Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03Controller>();
            if (found == null)
                return;

            if (found.GetComponent<SandPlanetPrototype03DebugLogOverlay>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype03DebugLogOverlay>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype03Controller>();
            if (controller == null)
                controller = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03Controller>();

            if (controller == null)
            {
                enabled = false;
                return;
            }

            BindFields();
            PrepareLogFiles();
            previous = CaptureSnapshot();

            AddEntry("SYSTEM", "Prototype 0.3 플레이 로그 시작");
            AddEntry("SYSTEM", "세션 로그: " + sessionLogPath);
        }

        private void LateUpdate()
        {
            if (controller == null)
                return;

            StateSnapshot current = CaptureSnapshot();
            if (previous == null)
            {
                previous = current;
                return;
            }

            List<string> changes = new List<string>();

            if (current.Day != previous.Day)
                changes.Add($"DAY {previous.Day} → DAY {current.Day}");

            if (current.Hour != previous.Hour)
            {
                int delta = current.Hour - previous.Hour;
                string deltaText = delta >= 0 ? $"+{delta}h" : $"{delta}h";
                changes.Add($"시간 {previous.Hour:00}:00 → {current.Hour:00}:00 ({deltaText})");
            }

            if (current.Will != previous.Will || current.MaxWill != previous.MaxWill)
                changes.Add($"의지 {previous.Will}/{previous.MaxWill} → {current.Will}/{current.MaxWill}");

            AddStatChange(changes, "개인", previous.PersonalLevel, previous.PersonalXp, current.PersonalLevel, current.PersonalXp);
            AddStatChange(changes, "대인", previous.SocialLevel, previous.SocialXp, current.SocialLevel, current.SocialXp);
            AddStatChange(changes, "기술", previous.TechnicalLevel, previous.TechnicalXp, current.TechnicalLevel, current.TechnicalXp);

            AddBoolDictionaryChanges(changes, "플래그", previous.Flags, current.Flags);
            AddIntDictionaryChanges(changes, "호감도", previous.Affinities, current.Affinities);

            bool encounterResolved = WasEncounterResolved(previous, current);
            bool eventResolved = WasStoryEventResolved(previous, current);
            bool stateChanged = changes.Count > 0;
            bool statusChanged = !string.Equals(previous.Status, current.Status, StringComparison.Ordinal);

            if (encounterResolved)
            {
                string title = string.IsNullOrEmpty(previous.EncounterTitle) ? previous.EncounterId : previous.EncounterTitle;
                AddEntry("ENCOUNTER", $"{title} 완료");
                RecordLatestResult(current.Status, "인카운터 결과");
            }
            else if (eventResolved)
            {
                string title = string.IsNullOrEmpty(previous.StoryEventTitle) ? previous.StoryEventId : previous.StoryEventTitle;
                AddEntry("EVENT", $"{title} 완료");
                RecordLatestResult(current.Status, "이벤트 결과");
            }
            else if (stateChanged && statusChanged && !string.IsNullOrWhiteSpace(current.Status))
            {
                // Rest, sleep/day transition, or another state-changing action outside an encounter.
                RecordLatestResult(current.Status, "행동 결과");
            }

            foreach (string change in changes)
                AddEntry("CHANGE", change);

            previous = current;
        }

        private void OnGUI()
        {
            if (controller == null)
                return;

            EnsureGuiStyles();

            float width = Mathf.Min(520f, Screen.width * 0.42f);
            float height = Mathf.Min(344f, Screen.height * 0.42f);
            Rect panel = new Rect(12f, Screen.height - height - 12f, width, height);

            GUI.DrawTexture(panel, backgroundTexture, ScaleMode.StretchToFill);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, panel.height - 20f));
            GUILayout.Label("PLAYTEST LOG", headerStyle);
            GUILayout.Label("최근 결과", subtleStyle);
            GUILayout.Label(latestResult, resultStyle, GUILayout.MinHeight(54f));
            GUILayout.Space(6f);
            GUILayout.Label("최근 변화", subtleStyle);

            int start = Mathf.Max(0, entries.Count - VisibleEntryCount);
            for (int i = start; i < entries.Count; i++)
                GUILayout.Label(entries[i], logStyle);

            GUILayout.FlexibleSpace();
            GUILayout.Label("전체 기록은 Unity Console과 Project/Logs에 저장됩니다.", subtleStyle);
            GUILayout.EndArea();
        }

        private void BindFields()
        {
            Type type = controller.GetType();
            string[] names =
            {
                "day", "currentHour", "currentWillpower", "maxWillpower",
                "personalLevel", "personalXp", "socialLevel", "socialXp", "technicalLevel", "technicalXp",
                "mode", "statusMessage", "flags", "affinities", "currentEncounter", "currentStoryEvent"
            };

            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, ControllerFieldFlags);
                if (field != null)
                    fields[name] = field;
                else
                    Debug.LogWarning("[P0.3 LOG] Debug overlay could not bind field: " + name);
            }
        }

        private StateSnapshot CaptureSnapshot()
        {
            object encounter = ReadField("currentEncounter");
            object storyEvent = ReadField("currentStoryEvent");

            return new StateSnapshot
            {
                Day = ReadInt("day"),
                Hour = ReadInt("currentHour"),
                Will = ReadInt("currentWillpower"),
                MaxWill = ReadInt("maxWillpower"),

                PersonalLevel = ReadInt("personalLevel"),
                PersonalXp = ReadInt("personalXp"),
                SocialLevel = ReadInt("socialLevel"),
                SocialXp = ReadInt("socialXp"),
                TechnicalLevel = ReadInt("technicalLevel"),
                TechnicalXp = ReadInt("technicalXp"),

                Mode = ReadField("mode")?.ToString() ?? string.Empty,
                Status = ReadField("statusMessage") as string ?? string.Empty,

                Flags = CopyBoolDictionary(ReadField("flags")),
                Affinities = CopyIntDictionary(ReadField("affinities")),

                EncounterObject = encounter,
                EncounterTitle = ReadNestedString(encounter, "Title"),
                EncounterId = ReadNestedString(encounter, "Id"),
                EncounterCompleted = ReadNestedBool(encounter, "Completed"),
                EncounterLastCompletedDay = ReadNestedInt(encounter, "LastCompletedDay", -1),

                StoryEventObject = storyEvent,
                StoryEventTitle = ReadNestedString(storyEvent, "Title"),
                StoryEventId = ReadNestedString(storyEvent, "Id"),
                StoryEventCompleted = ReadNestedBool(storyEvent, "Completed")
            };
        }

        private static bool WasEncounterResolved(StateSnapshot before, StateSnapshot after)
        {
            if (before.EncounterObject == null)
                return false;

            bool leftEncounterScreen = string.Equals(before.Mode, "Encounter", StringComparison.Ordinal) &&
                                       !string.Equals(after.Mode, "Encounter", StringComparison.Ordinal);

            bool completedNow = ReadNestedBool(before.EncounterObject, "Completed");
            int lastCompletedDayNow = ReadNestedInt(before.EncounterObject, "LastCompletedDay", -1);
            bool completionChanged = completedNow && !before.EncounterCompleted;
            bool repeatCompletionChanged = lastCompletedDayNow != before.EncounterLastCompletedDay;

            return leftEncounterScreen && (completionChanged || repeatCompletionChanged);
        }

        private static bool WasStoryEventResolved(StateSnapshot before, StateSnapshot after)
        {
            if (before.StoryEventObject == null)
                return false;

            bool leftEventScreen = string.Equals(before.Mode, "StoryEvent", StringComparison.Ordinal) &&
                                   !string.Equals(after.Mode, "StoryEvent", StringComparison.Ordinal);
            bool completedNow = ReadNestedBool(before.StoryEventObject, "Completed");

            return leftEventScreen && completedNow && !before.StoryEventCompleted;
        }

        private void RecordLatestResult(string status, string fallback)
        {
            latestResult = string.IsNullOrWhiteSpace(status) ? fallback : status;
            AddEntry("RESULT", latestResult);
        }

        private static void AddStatChange(
            List<string> changes,
            string label,
            int beforeLevel,
            int beforeXp,
            int afterLevel,
            int afterXp)
        {
            if (beforeLevel == afterLevel && beforeXp == afterXp)
                return;

            changes.Add($"{label} Lv.{beforeLevel} {beforeXp}/6 XP → Lv.{afterLevel} {afterXp}/6 XP");
        }

        private static void AddBoolDictionaryChanges(
            List<string> changes,
            string label,
            Dictionary<string, bool> before,
            Dictionary<string, bool> after)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (string key in before.Keys)
                keys.Add(key);
            foreach (string key in after.Keys)
                keys.Add(key);

            foreach (string key in keys)
            {
                bool beforeValue = before.TryGetValue(key, out bool b) && b;
                bool afterValue = after.TryGetValue(key, out bool a) && a;
                if (beforeValue != afterValue)
                    changes.Add($"{label} {key}: {(beforeValue ? "ON" : "OFF")} → {(afterValue ? "ON" : "OFF")}");
            }
        }

        private static void AddIntDictionaryChanges(
            List<string> changes,
            string label,
            Dictionary<string, int> before,
            Dictionary<string, int> after)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (string key in before.Keys)
                keys.Add(key);
            foreach (string key in after.Keys)
                keys.Add(key);

            foreach (string key in keys)
            {
                int beforeValue = before.TryGetValue(key, out int b) ? b : 0;
                int afterValue = after.TryGetValue(key, out int a) ? a : 0;
                if (beforeValue != afterValue)
                    changes.Add($"{label} {key}: {beforeValue} → {afterValue}");
            }
        }

        private void PrepareLogFiles()
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string logDirectory = Path.Combine(projectRoot, "Logs");
                Directory.CreateDirectory(logDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                sessionLogPath = Path.Combine(logDirectory, $"SandPlanet_Prototype03_{timestamp}.log");
                latestLogPath = Path.Combine(logDirectory, "SandPlanet_Prototype03_latest.log");

                string header = $"SandPlanet Prototype 0.3 Playtest Log{Environment.NewLine}" +
                                $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                                $"Unity: {Application.unityVersion}{Environment.NewLine}" +
                                new string('-', 72) + Environment.NewLine;

                File.WriteAllText(sessionLogPath, header);
                File.WriteAllText(latestLogPath, header);
            }
            catch (Exception exception)
            {
                sessionLogPath = "로그 파일 생성 실패";
                latestLogPath = null;
                Debug.LogWarning("[P0.3 LOG] Could not create log file: " + exception.Message);
            }
        }

        private void AddEntry(string category, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            int day = ReadInt("day");
            int hour = ReadInt("currentHour");
            string gameStamp = day > 0 ? $"D{day} {hour:00}:00" : "SETUP";
            string entry = $"[{gameStamp}] [{category}] {message}";

            entries.Add(entry);
            if (entries.Count > MaxStoredEntries)
                entries.RemoveAt(0);

            Debug.Log("[P0.3 LOG] " + entry);
            AppendToLogFile(entry);
        }

        private void AppendToLogFile(string entry)
        {
            try
            {
                string line = $"{DateTime.Now:HH:mm:ss.fff} {entry}{Environment.NewLine}";
                if (!string.IsNullOrEmpty(sessionLogPath) && !sessionLogPath.StartsWith("로그 파일", StringComparison.Ordinal))
                    File.AppendAllText(sessionLogPath, line);
                if (!string.IsNullOrEmpty(latestLogPath))
                    File.AppendAllText(latestLogPath, line);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[P0.3 LOG] Could not append log file: " + exception.Message);
            }
        }

        private void EnsureGuiStyles()
        {
            if (headerStyle != null)
                return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.86f, 0.86f, 0.86f) },
                margin = new RectOffset(0, 0, 1, 1)
            };

            subtleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.66f, 0.70f, 0.74f) }
            };

            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.06f, 0.92f));
            backgroundTexture.Apply();
        }

        private object ReadField(string name)
        {
            return fields.TryGetValue(name, out FieldInfo field) ? field.GetValue(controller) : null;
        }

        private int ReadInt(string name)
        {
            object value = ReadField(name);
            return value is int intValue ? intValue : 0;
        }

        private static string ReadNestedString(object source, string fieldName)
        {
            object value = ReadNestedField(source, fieldName);
            return value as string ?? string.Empty;
        }

        private static bool ReadNestedBool(object source, string fieldName)
        {
            object value = ReadNestedField(source, fieldName);
            return value is bool boolValue && boolValue;
        }

        private static int ReadNestedInt(object source, string fieldName, int fallback)
        {
            object value = ReadNestedField(source, fieldName);
            return value is int intValue ? intValue : fallback;
        }

        private static object ReadNestedField(object source, string fieldName)
        {
            if (source == null)
                return null;

            FieldInfo field = source.GetType().GetField(fieldName, NestedFieldFlags);
            return field?.GetValue(source);
        }

        private static Dictionary<string, bool> CopyBoolDictionary(object source)
        {
            Dictionary<string, bool> copy = new Dictionary<string, bool>();
            if (!(source is IDictionary dictionary))
                return copy;

            foreach (DictionaryEntry item in dictionary)
            {
                if (item.Key is string key && item.Value is bool value)
                    copy[key] = value;
            }

            return copy;
        }

        private static Dictionary<string, int> CopyIntDictionary(object source)
        {
            Dictionary<string, int> copy = new Dictionary<string, int>();
            if (!(source is IDictionary dictionary))
                return copy;

            foreach (DictionaryEntry item in dictionary)
            {
                if (item.Key is string key && item.Value is int value)
                    copy[key] = value;
            }

            return copy;
        }

        private void OnDestroy()
        {
            if (backgroundTexture != null)
                Destroy(backgroundTexture);
        }
    }
}
