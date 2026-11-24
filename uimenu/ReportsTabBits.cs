#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using AeroMenu;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using RewiredConsts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static AeroMenu.AeroMenuGUI;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {

public static Sprite LoadEmbeddedSprite(string fileName, float pixelsPerUnit = 1f)
        {
            string path = $"AeroMenu.{fileName}";

            try
            {
                if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var cachedSprite))
                    return cachedSprite;

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(path);

                if (stream == null)
                {
                    return null;
                }

                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using System.IO.MemoryStream ms = new System.IO.MemoryStream();
                stream.CopyTo(ms);

                ImageConversion.LoadImage(texture, ms.ToArray(), false);

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

                return CachedSprites[path + pixelsPerUnit] = sprite;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
        public void Start()
        {
            activeGui = this;
            LoadConfig();
            if (enableBackground) LoadBackgroundImage();
            if (enableMenuCharacter) LoadMenuCharacter();
            LoadBanList();
            LoadBotBanList();
            ClearSpamErrorLogOnStartup();


            try
            {
                int starts = UnityEngine.PlayerPrefs.GetInt("Aero_GameStarts", 0);
                starts++;

                string chatLogPath = System.IO.Path.Combine(Plugin.AeroFolder, "ChatLog.txt");

                if (starts >= 3)
                {
                    if (System.IO.File.Exists(chatLogPath))
                    {
                        System.IO.File.WriteAllText(chatLogPath, string.Empty);
                    }
                    starts = 0;
                }

                UnityEngine.PlayerPrefs.SetInt("Aero_GameStarts", starts);
                UnityEngine.PlayerPrefs.Save();
            }
            catch { }
        }

        internal static System.Action EmergencyFlush;
        internal float lastSavedAt = -999f;

        public void OnApplicationQuit()
        {
            FlushAllSaves();
        }

        internal void FlushAllSaves()
        {
            try { SaveConfig(); } catch { }
            try { SaveKeybinds(); } catch { }
            try { UnityEngine.PlayerPrefs.Save(); } catch { }
            try { lastSavedAt = Time.unscaledTime; } catch { }
        }

        public void OnDisable()
        {
            FlushAllSaves();
        }

        public void OnDestroy()
        {
            DestroyMenuStyleTextures();
            if (customMenuBg != null)
            {
                UnityEngine.Object.Destroy(customMenuBg);
                customMenuBg = null;
            }
            if (menuCharacterTexture != null)
            {
                UnityEngine.Object.Destroy(menuCharacterTexture);
                menuCharacterTexture = null;
            }
        }

        private static void ClearSpamErrorLogOnStartup()
        {
            try
            {
                string root = string.IsNullOrWhiteSpace(Plugin.AeroFolder)
                    ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "AeroMenu")
                    : Plugin.AeroFolder;

                if (!System.IO.Directory.Exists(root)) return;

                foreach (string file in System.IO.Directory.GetFiles(root, "SpamErrorLog*.txt", System.IO.SearchOption.AllDirectories))
                {
                    try { System.IO.File.Delete(file); }
                    catch { }
                }

            }
            catch
            {
            }
        }

        private static string lastKnownRoomCode = string.Empty;

private static float lastRoomCodeCopyAt = -10f;

public static string GetCurrentRoomCodeForStatus()
        {
            try
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameId == 0)
                    return string.IsNullOrWhiteSpace(lastKnownRoomCode) ? "No room" : lastKnownRoomCode;

                int gameId = AmongUsClient.Instance.GameId;
                string gameName = GameCode.IntToGameName(gameId);
                lastKnownRoomCode = string.IsNullOrWhiteSpace(gameName) ? gameId.ToString() : gameName;
                return lastKnownRoomCode;
            }
            catch
            {
                try
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.GameId != 0)
                    {
                        lastKnownRoomCode = AmongUsClient.Instance.GameId.ToString();
                        return lastKnownRoomCode;
                    }
                }
                catch { }

                return string.IsNullOrWhiteSpace(lastKnownRoomCode) ? "No room" : lastKnownRoomCode;
            }
        }

public static bool TryCopyRoomCodeToClipboard(bool notify)
        {
            if (!autoCopyCodeAndLeave)
                return false;

            try
            {
                if (Time.unscaledTime - lastRoomCodeCopyAt < 0.75f)
                    return false;

                string roomCode = GetCurrentRoomCodeForStatus();
                if (string.IsNullOrWhiteSpace(roomCode) || roomCode == "No room")
                    return false;

                GUIUtility.systemCopyBuffer = roomCode;
                lastRoomCodeCopyAt = Time.unscaledTime;
                if (notify)
                    ShowNotification($"<color=#00FFAA>[ROOM]</color> Copied code: <b>{roomCode}</b>");
                return true;
            }
            catch { return false; }
        }

public static KeyCode bindMagnetCursor = KeyCode.F9;

public static bool isWaitBindMagnetCursor = false;

private bool CanRunHostBind(string actionName)
        {
            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) return true;
            }
            catch { }

            ShowNotification($"<color=#FF0000>[BIND]</color> {actionName}: host only");
            return false;
        }

        // ===== Report history (drawn inside the existing Players > Player History section) =====

        private sealed class ReportLogEntry
        {
            public string TimeUtc;
            public string Kind;
            public string Target;
            public string Reason;
            public string Room;
        }

        private const int MaxReportLogEntries = 200;
        private const float ReportLogRowHeight = 30f;
        private static readonly List<ReportLogEntry> reportLogEntries = new List<ReportLogEntry>();
        private static bool reportLogLoaded;
        private static Vector2 reportLogScroll = Vector2.zero;
        private static string reportLogFilter = string.Empty;

        private static string ReportLogFilePath()
        {
            string folder = string.IsNullOrWhiteSpace(Plugin.AeroFolder)
                ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "AeroMenu")
                : Plugin.AeroFolder;
            return System.IO.Path.Combine(folder, "AeroReportLog.txt");
        }

        private static string ReportLogToken(string value)
        {
            if (string.IsNullOrEmpty(value)) return "-";
            return value.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static void EnsureReportLogLoaded()
        {
            if (reportLogLoaded) return;
            reportLogLoaded = true;

            try
            {
                string path = ReportLogFilePath();
                if (!System.IO.File.Exists(path)) return;

                foreach (string rawLine in System.IO.File.ReadLines(path, Encoding.UTF8))
                {
                    if (reportLogEntries.Count >= MaxReportLogEntries) break;
                    string line = rawLine ?? string.Empty;
                    if (line.Length == 0 || line.StartsWith("#")) continue;

                    string[] parts = line.Split('|');
                    if (parts.Length < 5) continue;

                    reportLogEntries.Add(new ReportLogEntry
                    {
                        TimeUtc = parts[0],
                        Kind = parts[1],
                        Target = parts[2],
                        Reason = parts[3],
                        Room = parts[4]
                    });
                }
            }
            catch { }
        }

        private static void WriteReportLogFile()
        {
            try
            {
                string path = ReportLogFilePath();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                List<string> lines = new List<string>
                {
                    "# AeroMenu Report History - UTC|Kind|Target|Reason|RoomCode",
                    $"# Updated UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
                };

                for (int i = 0; i < reportLogEntries.Count && i < MaxReportLogEntries; i++)
                {
                    ReportLogEntry entry = reportLogEntries[i];
                    lines.Add($"{ReportLogToken(entry.TimeUtc)}|{ReportLogToken(entry.Kind)}|{ReportLogToken(entry.Target)}|{ReportLogToken(entry.Reason)}|{ReportLogToken(entry.Room)}");
                }

                System.IO.File.WriteAllLines(path, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        internal static void RecordReportLog(string kind, string target, string reason)
        {
            try
            {
                EnsureReportLogLoaded();
                reportLogEntries.Insert(0, new ReportLogEntry
                {
                    TimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Kind = ReportLogToken(kind),
                    Target = ReportLogToken(target),
                    Reason = ReportLogToken(reason),
                    Room = ReportLogToken(GetCurrentRoomCodeForStatus())
                });
                while (reportLogEntries.Count > MaxReportLogEntries)
                    reportLogEntries.RemoveAt(reportLogEntries.Count - 1);

                WriteReportLogFile();
            }
            catch { }
        }

        private static void ClearReportLog()
        {
            try
            {
                reportLogEntries.Clear();
                reportLogScroll = Vector2.zero;
                WriteReportLogFile();
                ShowNotification("<color=#00FFAA>[REPORT]</color> Report history cleared.");
            }
            catch { }
        }

        private static void CopyReportLogToClipboard()
        {
            try
            {
                EnsureReportLogLoaded();
                StringBuilder text = new StringBuilder();
                for (int i = 0; i < reportLogEntries.Count && i < MaxReportLogEntries; i++)
                {
                    ReportLogEntry entry = reportLogEntries[i];
                    text.Append(entry.TimeUtc).Append(" UTC | ").Append(entry.Kind).Append(" | ")
                        .Append(entry.Target).Append(" | ").Append(entry.Reason).Append(" | ")
                        .Append(entry.Room).Append('\n');
                }

                GUIUtility.systemCopyBuffer = text.Length == 0
                    ? "AeroMenu Report History: no entries"
                    : text.ToString();
                ShowNotification($"<color=#00FF00>[REPORT]</color> Copied {Mathf.Min(reportLogEntries.Count, MaxReportLogEntries)} entries.");
            }
            catch { }
        }

        private static bool ReportLogMatchesFilter(ReportLogEntry entry, string filter)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(filter)) return true;

            string query = filter.Trim();
            return ContainsIgnoreCase(entry.Target, query) ||
                   ContainsIgnoreCase(entry.Reason, query) ||
                   ContainsIgnoreCase(entry.Room, query) ||
                   ContainsIgnoreCase(entry.Kind, query) ||
                   ContainsIgnoreCase(entry.TimeUtc, query);
        }

        private static bool ContainsIgnoreCase(string value, string query)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal void DrawReportHistorySection()
        {
            EnsureReportLogLoaded();
            if (menuCardStyle == null) return;

            GUILayout.Space(6);
            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                DrawMenuSectionHeader("REPORT HISTORY");

                GUIStyle infoStyle = historyInfoStyle ?? GUI.skin.label;
                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Entries: {reportLogEntries.Count}", infoStyle, GUILayout.MinWidth(104), GUILayout.ExpandWidth(false), GUILayout.Height(22));
                    GUILayout.Label(L("File: AeroReportLog.txt", "Файл: AeroReportLog.txt"), infoStyle, GUILayout.MinWidth(190), GUILayout.ExpandWidth(false), GUILayout.Height(22));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(L("COPY ALL", "СКОПИРОВАТЬ ВСЁ"), btnStyle, GUILayout.Width(104), GUILayout.Height(22)))
                        CopyReportLogToClipboard();
                    if (GUILayout.Button(L("CLEAR", "ОЧИСТИТЬ"), btnStyle, GUILayout.Width(84), GUILayout.Height(22)))
                        ClearReportLog();
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label(L("Filter:", "Фильтр:"), labelStyle11 ?? GUI.skin.label, GUILayout.Width(46), GUILayout.Height(22));
                    GUIStyle fieldStyle = pseudoInputStyle ?? inputBlockStyle ?? GUI.skin.textField;
                    string typed = AeroTextField(reportLogFilter ?? string.Empty, fieldStyle, GUILayout.Height(22), GUILayout.ExpandWidth(true));
                    if (!string.Equals(typed, reportLogFilter, StringComparison.Ordinal))
                    {
                        reportLogFilter = typed ?? string.Empty;
                        reportLogScroll = Vector2.zero;
                    }
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(4);
                List<ReportLogEntry> visibleEntries = new List<ReportLogEntry>();
                for (int i = 0; i < reportLogEntries.Count && visibleEntries.Count < MaxReportLogEntries; i++)
                {
                    if (ReportLogMatchesFilter(reportLogEntries[i], reportLogFilter))
                        visibleEntries.Add(reportLogEntries[i]);
                }

                reportLogScroll = GUILayout.BeginScrollView(reportLogScroll, GUILayout.Height(120));
                try
                {
                    if (visibleEntries.Count == 0)
                    {
                        GUILayout.Label(L("<color=#777777>No reports recorded yet.</color>", "<color=#777777>Репортов пока нет.</color>"), centeredRichLabelStyle);
                    }
                    else
                    {
                        int rowCount = visibleEntries.Count;
                        int firstIndex = Mathf.Clamp(Mathf.FloorToInt(reportLogScroll.y / ReportLogRowHeight), 0, Mathf.Max(0, rowCount - 1));
                        int endIndex = Mathf.Min(rowCount, firstIndex + 8);
                        string accentHex = GetMenuAccentHex();

                        GUILayout.Space(firstIndex * ReportLogRowHeight);
                        for (int i = firstIndex; i < endIndex; i++)
                        {
                            ReportLogEntry entry = visibleEntries[i];
                            GUILayout.BeginVertical(GUILayout.Height(ReportLogRowHeight - 2f));
                            try
                            {
                                GUILayout.Label($"<color=#{accentHex}>{entry.Kind}</color> {entry.TimeUtc} UTC | {entry.Room}", historyHeaderStyle ?? GUI.skin.label, GUILayout.Height(16));
                                GUILayout.Label($"{entry.Target} - {entry.Reason}", historyLineStyle ?? GUI.skin.label, GUILayout.Height(14));
                            }
                            finally { GUILayout.EndVertical(); }
                        }
                        GUILayout.Space(Mathf.Max(0, rowCount - endIndex) * ReportLogRowHeight);
                    }
                }
                finally { GUILayout.EndScrollView(); }
            }
            finally { GUILayout.EndVertical(); }
        }
    }

    [HarmonyPatch(typeof(AeroMenuGUI), "DrawPlayersHistoryTab")]
    public static class AeroReportLog_HistoryTab_Patch
    {
        public static void Postfix(AeroMenuGUI __instance)
        {
            try { __instance.DrawReportHistorySection(); } catch { }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ReportPlayer))]
    public static class AeroReportLog_ReportPlayer_Patch
    {
        public static void Postfix(object[] __args)
        {
            try
            {
                if (__args == null || __args.Length < 2) return;

                int clientId = -1;
                try { clientId = Convert.ToInt32(__args[0]); } catch { }

                string reason = __args[1] != null ? __args[1].ToString() : "Unknown";
                string target = "Client " + clientId;
                try
                {
                    ClientData client = AmongUsClient.Instance != null ? AmongUsClient.Instance.FindClientById(clientId) : null;
                    if (client != null && !string.IsNullOrWhiteSpace(client.PlayerName))
                        target = client.PlayerName;
                }
                catch { }

                AeroMenuGUI.RecordReportLog("PLAYER", target, reason);
            }
            catch { }
        }
    }
}
