#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using InnerNet;
using AmongUs.GameOptions;
using BepInEx;
using HarmonyLib;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ---------------------------------------------------------------------
        // Group V - Automation (exact feature field names from the contract)
        // ---------------------------------------------------------------------
        public static bool proAutoReady = false;
        public static bool proAutoExitGame = false;
        public static bool proScreenshotOnKill = false;
        public static bool proCopyCodeOnStart = false;
        public static bool proPlayerStatsLog = false;

        // ---------------------------------------------------------------------
        // Group V - private state (all helpers/fields suffixed V)
        // ---------------------------------------------------------------------
        private const BindingFlags FlagsV = BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static;

        private static readonly Dictionary<string, Type> typeCacheV = new Dictionary<string, Type>();

        private static readonly string[] KillNamesV =
        {
            "kills", "Kills", "numKills", "NumKills", "killCount", "KillCount",
            "TotalKills", "totalKills", "KillsCount"
        };

        private static readonly string[] TaskNamesV =
        {
            "completedTasks", "CompletedTasks", "tasksCompleted", "TasksCompleted",
            "numTasks", "NumTasks", "taskCount", "TaskCount", "TotalTasks"
        };

        private static readonly string[] DeathNamesV =
        {
            "deaths", "Deaths", "numDeaths", "NumDeaths", "deathCount", "DeathCount",
            "TotalDeaths", "totalDeaths"
        };

        private static readonly string[] ReadyNamesV =
        {
            "RpcSetReady", "SetReady", "RpcSetIsReady", "SetIsReady",
            "CmdSetReady", "SetPlayerReady", "ToggleReady"
        };

        private static float nextAutoReadyAtV;
        private static float nextAutoExitAtV;
        private static float lastKillShotAtV = -10f;
        private static float nextStatsLogAtV;

        private static long baselineKillsV = long.MinValue;
        private static int killGameIdV = int.MinValue;

        private static bool copyStateInitV;
        private static bool prevGameStartedV;
        private static int copiedForGameIdV = int.MinValue;

        private static bool statsHeaderV;

        private const string StatsHeaderV =
            "Timestamp,PlayerName,PlayerId,Kills,TasksCompleted,Deaths,IsDead,Disconnected";

        // ---------------------------------------------------------------------
        // Public surface (exact names required by the contract)
        // ---------------------------------------------------------------------

        internal void DrawProV(float width)
        {
            try
            {
                float w = width > 10f ? width : GetMenuWorkWidth(160f, 620f);

                GUILayout.Label(MenuText(L("AUTOMATION", "АВТОМАТИЗАЦИЯ")), headerStyle,
                    GUILayout.Width(w));
                GUILayout.Space(4f);

                proAutoReady = DrawToggle(proAutoReady,
                    L("Auto Ready In Lobby", "Авто-готовность в лобби"));
                proAutoExitGame = DrawToggle(proAutoExitGame,
                    L("Auto Exit After Results", "Авто-выход после результатов"));
                proScreenshotOnKill = DrawToggle(proScreenshotOnKill,
                    L("Screenshot On Kill", "Скриншот при убийстве"));
                proCopyCodeOnStart = DrawToggle(proCopyCodeOnStart,
                    L("Copy Room Code On Start", "Копировать код при старте"));
                proPlayerStatsLog = DrawToggle(proPlayerStatsLog,
                    L("Log Player Stats (CSV)", "Логирование статистики (CSV)"));

                GUILayout.Space(6f);
                GUILayout.Label(MenuText(L(
                    "Screenshots and the CSV log are saved to the AeroMenu folder.",
                    "Скриншоты и CSV-лог сохраняются в папку AeroMenu.")),
                    menuDescStyle, GUILayout.Width(w));
            }
            catch { }
        }

        internal static void TickProV(bool inputBlocked)
        {
            // No hotkeys in this group; inputBlocked is intentionally unused.
            try { TickAutoReadyV(); } catch { }
            try { TickAutoExitGameV(); } catch { }
            try { TickScreenshotOnKillV(); } catch { }
            try { TickCopyCodeOnStartV(); } catch { }
            try { TickPlayerStatsLogV(); } catch { }
        }

        internal static void LoadProV()
        {
            try
            {
                proAutoReady = LoadBool("M_ProV_proAutoReady", false);
                proAutoExitGame = LoadBool("M_ProV_proAutoExitGame", false);
                proScreenshotOnKill = LoadBool("M_ProV_proScreenshotOnKill", false);
                proCopyCodeOnStart = LoadBool("M_ProV_proCopyCodeOnStart", false);
                proPlayerStatsLog = LoadBool("M_ProV_proPlayerStatsLog", false);
            }
            catch { }

            ResetDetectionStateV();
        }

        internal static void SaveProV()
        {
            try
            {
                SaveBool("M_ProV_proAutoReady", proAutoReady);
                SaveBool("M_ProV_proAutoExitGame", proAutoExitGame);
                SaveBool("M_ProV_proScreenshotOnKill", proScreenshotOnKill);
                SaveBool("M_ProV_proCopyCodeOnStart", proCopyCodeOnStart);
                SaveBool("M_ProV_proPlayerStatsLog", proPlayerStatsLog);
            }
            catch { }

            ResetDetectionStateV();
        }

        internal static void DrawProVOverlay()
        {
            // No full-screen overlay visuals for this group.
            try { if (Screen.width <= 1) return; } catch { }
        }

        // ---------------------------------------------------------------------
        // Detection-state helpers
        // ---------------------------------------------------------------------

        private static void ResetDetectionStateV()
        {
            baselineKillsV = long.MinValue;
            killGameIdV = int.MinValue;
            copyStateInitV = false;
            prevGameStartedV = false;
            copiedForGameIdV = int.MinValue;
            lastKillShotAtV = -10f;
        }

        private static int CurrentGameIdV()
        {
            try { return AmongUsClient.Instance == null ? 0 : AmongUsClient.Instance.GameId; }
            catch { return 0; }
        }

        private static bool IsInLobbyV()
        {
            try
            {
                if (LobbyBehaviour.Instance == null) return false;
                AmongUsClient client = AmongUsClient.Instance;
                if (client == null || client.IsGameStarted) return false;
                return client.GameState == InnerNetClient.GameStates.Joined;
            }
            catch { return false; }
        }

        private static bool IsInActiveGameV()
        {
            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                if (client == null || !client.IsGameStarted) return false;
                if (LobbyBehaviour.Instance != null) return false;
                return true;
            }
            catch { return false; }
        }

        // ---------------------------------------------------------------------
        // V1 - Auto Ready
        // ---------------------------------------------------------------------

        private static void TickAutoReadyV()
        {
            if (!proAutoReady) return;
            if (!IsInLobbyV()) return;
            if (Time.unscaledTime < nextAutoReadyAtV) return;

            nextAutoReadyAtV = Time.unscaledTime + 1f;
            TryMarkReadyV();
        }

        private static void TryMarkReadyV()
        {
            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null) return;

            bool alreadyReady = false;
            try
            {
                bool? ready = GetBoolMemberV(local, "IsReady", "isReady", "Ready", "ready");
                if (ready.HasValue && ready.Value) alreadyReady = true;
            }
            catch { }
            if (!alreadyReady)
            {
                try
                {
                    if (local.Data != null)
                    {
                        bool? ready = GetBoolMemberV(local.Data, "IsReady", "isReady", "Ready", "ready");
                        if (ready.HasValue && ready.Value) alreadyReady = true;
                    }
                }
                catch { }
            }
            if (alreadyReady) return;

            TryInvokeReadyMethodsV(local);
            try { if (local.Data != null) TryInvokeReadyMethodsV(local.Data); } catch { }
            try { if (AmongUsClient.Instance != null) TryInvokeReadyMethodsV(AmongUsClient.Instance); } catch { }
        }

        private static void TryInvokeReadyMethodsV(object target)
        {
            if (target == null) return;
            Type type = target.GetType();

            // Prefer the explicit "set ready to true" overloads.
            foreach (string name in ReadyNamesV)
            {
                MethodInfo method = null;
                try { method = type.GetMethod(name, FlagsV, null, new[] { typeof(bool) }, null); }
                catch { }
                if (method == null) continue;
                try { method.Invoke(target, new object[] { true }); return; }
                catch { }
            }

            // Fall back to parameterless variants (e.g. a toggle) only if nothing else worked.
            foreach (string name in ReadyNamesV)
            {
                MethodInfo method = null;
                try { method = type.GetMethod(name, FlagsV, null, Type.EmptyTypes, null); }
                catch { }
                if (method == null) continue;
                try { method.Invoke(target, null); return; }
                catch { }
            }
        }

        // ---------------------------------------------------------------------
        // V2 - Auto exit after results
        // ---------------------------------------------------------------------

        private static void TickAutoExitGameV()
        {
            if (!proAutoExitGame) return;
            if (!IsResultsScreenV()) return;
            if (Time.unscaledTime < nextAutoExitAtV) return;

            nextAutoExitAtV = Time.unscaledTime + 2f;
            ExitGameV();
        }

        private static bool IsResultsScreenV()
        {
            try
            {
                Type endType = FindTypeV("EndGameManager");
                if (endType != null && FindFirstObjectV(endType) != null) return true;
            }
            catch { }

            // InnerNetClient.GameStates.Ended == 4 in the shipped game assembly.
            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                if (client == null) return false;
                object state = GetMemberV(client, "GameState", "gameState", "State", "state");
                if (state == null) return false;
                return Convert.ToInt32(state) == 4;
            }
            catch { return false; }
        }

        private static void ExitGameV()
        {
            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                if (client == null) return;
                Type type = client.GetType();

                MethodInfo method = null;
                try { method = type.GetMethod("ExitGame", FlagsV, null, new[] { typeof(DisconnectReasons) }, null); }
                catch { }
                if (method != null)
                {
                    try { method.Invoke(client, new object[] { DisconnectReasons.ExitGame }); return; }
                    catch { }
                }

                try { method = type.GetMethod("ExitGame", FlagsV, null, Type.EmptyTypes, null); }
                catch { }
                if (method != null)
                {
                    try { method.Invoke(client, null); return; }
                    catch { }
                }
            }
            catch { }
        }

        // ---------------------------------------------------------------------
        // V3 - Screenshot on successful local kill
        // ---------------------------------------------------------------------

        private static void TickScreenshotOnKillV()
        {
            if (!proScreenshotOnKill)
            {
                baselineKillsV = long.MinValue;
                return;
            }

            int gameId = CurrentGameIdV();
            if (gameId != killGameIdV)
            {
                killGameIdV = gameId;
                baselineKillsV = long.MinValue;
            }
            if (!IsInActiveGameV()) return;

            long kills = GetLocalKillsV();
            if (kills == long.MinValue) return; // Unsupported shape: silent no-op.

            if (baselineKillsV == long.MinValue)
            {
                baselineKillsV = kills;
                return;
            }

            if (kills <= baselineKillsV)
            {
                if (kills < baselineKillsV) baselineKillsV = kills;
                return;
            }

            baselineKillsV = kills;
            if (Time.unscaledTime - lastKillShotAtV < 0.5f) return;
            lastKillShotAtV = Time.unscaledTime;
            TakeKillScreenshotV();
        }

        private static long GetLocalKillsV()
        {
            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null) return long.MinValue;

            long value = GetLongMemberV(local, KillNamesV);
            if (value != long.MinValue) return value;

            try
            {
                if (local.Data != null)
                {
                    value = GetLongMemberV(local.Data, KillNamesV);
                    if (value != long.MinValue) return value;
                }
            }
            catch { }

            // Last resort: the client-side lifetime stats singleton.
            try
            {
                Type statsType = FindTypeV("StatsManager");
                if (statsType == null) return long.MinValue;
                object instance = GetStaticMemberV(statsType, "Instance", "instance", "Singleton");
                if (instance == null) return long.MinValue;
                return GetLongMemberV(instance, "totalKills", "TotalKills", "kills", "Kills");
            }
            catch { return long.MinValue; }
        }

        private static void TakeKillScreenshotV()
        {
            try
            {
                string folder = Plugin.AeroFolder;
                if (string.IsNullOrWhiteSpace(folder)) folder = Directory.GetCurrentDirectory();
                try { if (!Directory.Exists(folder)) Directory.CreateDirectory(folder); }
                catch { }

                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string file = Path.Combine(folder, "AeroMenu_Kill_" + stamp + ".png");
                ScreenCapture.CaptureScreenshot(file);

                try
                {
                    ShowNotification(L("<color=#00FFAA>[AUTO]</color> Kill screenshot saved.",
                                       "<color=#00FFAA>[АВТО]</color> Скриншот убийства сохранён."));
                }
                catch { }
            }
            catch { }
        }

        // ---------------------------------------------------------------------
        // V4 - Copy room code on game start
        // ---------------------------------------------------------------------

        private static void TickCopyCodeOnStartV()
        {
            bool started = false;
            try { started = AmongUsClient.Instance != null && AmongUsClient.Instance.IsGameStarted; }
            catch { started = false; }

            // First tick only establishes a baseline so we never fire on load.
            if (!copyStateInitV)
            {
                copyStateInitV = true;
                prevGameStartedV = started;
                return;
            }

            if (!started)
            {
                prevGameStartedV = false;
                return;
            }

            if (prevGameStartedV) return;
            prevGameStartedV = true;

            if (!proCopyCodeOnStart) return;

            int gameId = CurrentGameIdV();
            if (gameId == copiedForGameIdV) return;
            copiedForGameIdV = gameId;

            string code = "";
            try { code = GetCurrentRoomCodeForStatus(); }
            catch { }
            if (string.IsNullOrWhiteSpace(code) ||
                string.Equals(code, "No room", StringComparison.OrdinalIgnoreCase)) return;

            try { GUIUtility.systemCopyBuffer = code; }
            catch { }

            try
            {
                ShowNotification(L("<color=#00FFAA>[AUTO]</color> Copied room code: ",
                                   "<color=#00FFAA>[АВТО]</color> Скопирован код: ") +
                                 "<b>" + code + "</b>");
            }
            catch { }
        }

        // ---------------------------------------------------------------------
        // V5 - Player stats CSV logging
        // ---------------------------------------------------------------------

        private static void TickPlayerStatsLogV()
        {
            if (!proPlayerStatsLog) return;
            if (!IsInActiveGameV()) return;
            if (Time.unscaledTime < nextStatsLogAtV) return;

            nextStatsLogAtV = Time.unscaledTime + 1f;
            AppendStatsRowV();
        }

        private static void AppendStatsRowV()
        {
            try
            {
                string folder = Plugin.AeroFolder;
                if (string.IsNullOrWhiteSpace(folder)) folder = Directory.GetCurrentDirectory();
                try { if (!Directory.Exists(folder)) Directory.CreateDirectory(folder); }
                catch { }

                string path = Path.Combine(folder, "ProStats.csv");

                if (!statsHeaderV)
                {
                    bool needsHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
                    if (needsHeader) File.AppendAllText(path, StatsHeaderV + "\n");
                    statsHeaderV = true;
                }

                if (PlayerControl.AllPlayerControls == null) return;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null) continue;
                    try { File.AppendAllText(path, BuildStatsRowV(pc, timestamp)); }
                    catch { }
                }
            }
            catch { }
        }

        private static string BuildStatsRowV(PlayerControl pc, string timestamp)
        {
            object data = null;
            try { data = pc.Data; }
            catch { }

            string name = "";
            try { name = GetStringMemberV(data, "PlayerName", "playerName", "Name", "name"); }
            catch { }
            if (string.IsNullOrWhiteSpace(name))
            {
                try { name = pc.name; }
                catch { }
            }
            if (string.IsNullOrWhiteSpace(name)) name = "?";

            long playerId = long.MinValue;
            try { playerId = GetLongMemberV(data, "PlayerId", "playerId"); }
            catch { }
            if (playerId == long.MinValue)
            {
                try { playerId = pc.PlayerId; }
                catch { }
            }

            long kills = long.MinValue;
            try { kills = GetLongMemberV(data, KillNamesV); }
            catch { }
            if (kills == long.MinValue)
            {
                try { kills = GetLongMemberV(pc, KillNamesV); }
                catch { }
            }

            long tasks = GetCompletedTasksV(data);
            if (tasks == long.MinValue)
            {
                try { tasks = GetLongMemberV(data, TaskNamesV); }
                catch { }
            }

            long deaths = long.MinValue;
            try { deaths = GetLongMemberV(data, DeathNamesV); }
            catch { }

            bool isDead = false;
            try
            {
                bool? value = GetBoolMemberV(data, "IsDead", "isDead", "Dead", "dead");
                if (value.HasValue) isDead = value.Value;
            }
            catch { }

            bool disconnected = false;
            try
            {
                bool? value = GetBoolMemberV(data, "Disconnected", "disconnected", "IsDisconnected");
                if (value.HasValue) disconnected = value.Value;
            }
            catch { }

            string row = timestamp + "," +
                         EscapeCsvV(name) + "," +
                         (playerId == long.MinValue ? "" : playerId.ToString()) + "," +
                         (kills == long.MinValue ? "" : kills.ToString()) + "," +
                         (tasks == long.MinValue ? "" : tasks.ToString()) + "," +
                         (deaths == long.MinValue ? "" : deaths.ToString()) + "," +
                         (isDead ? "1" : "0") + "," +
                         (disconnected ? "1" : "0") + "\n";
            return row;
        }

        private static long GetCompletedTasksV(object data)
        {
            if (data == null) return long.MinValue;
            try
            {
                object tasks = GetMemberV(data, "Tasks", "tasks");
                if (tasks == null) return long.MinValue;

                System.Collections.IEnumerable enumerable = tasks as System.Collections.IEnumerable;
                if (enumerable == null) return long.MinValue;

                int completed = 0;
                foreach (object task in enumerable)
                {
                    if (task == null) continue;
                    bool? done = GetBoolMemberV(task, "Complete", "IsComplete", "completed",
                                                "Completed", "IsDone", "isDone");
                    if (done.HasValue && done.Value) completed++;
                }
                return completed;
            }
            catch { return long.MinValue; }
        }

        private static string EscapeCsvV(string value)
        {
            if (value == null) return "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // ---------------------------------------------------------------------
        // Reflection helpers (all guarded, all suffixed V)
        // ---------------------------------------------------------------------

        private static object GetMemberV(object target, params string[] names)
        {
            if (target == null || names == null) return null;
            Type type = target.GetType();

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    PropertyInfo property = type.GetProperty(name, FlagsV);
                    if (property != null && property.CanRead)
                        return property.GetValue(target, null);
                }
                catch { }
                try
                {
                    FieldInfo field = type.GetField(name, FlagsV);
                    if (field != null) return field.GetValue(target);
                }
                catch { }
            }
            return null;
        }

        private static object GetStaticMemberV(Type type, params string[] names)
        {
            if (type == null || names == null) return null;
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    PropertyInfo property = type.GetProperty(name, FlagsV);
                    if (property != null && property.CanRead)
                        return property.GetValue(null, null);
                }
                catch { }
                try
                {
                    FieldInfo field = type.GetField(name, FlagsV);
                    if (field != null) return field.GetValue(null);
                }
                catch { }
            }
            return null;
        }

        private static long GetLongMemberV(object target, params string[] names)
        {
            object value = GetMemberV(target, names);
            if (value == null) return long.MinValue;
            try { return Convert.ToInt64(value); }
            catch { return long.MinValue; }
        }

        private static bool? GetBoolMemberV(object target, params string[] names)
        {
            object value = GetMemberV(target, names);
            if (value == null) return null;
            try { return Convert.ToBoolean(value); }
            catch { return null; }
        }

        private static string GetStringMemberV(object target, params string[] names)
        {
            object value = GetMemberV(target, names);
            if (value == null) return null;
            try { return Convert.ToString(value); }
            catch { return null; }
        }

        private static Type FindTypeV(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                Type cached;
                if (typeCacheV.TryGetValue(name, out cached)) return cached;

                Type found = null;
                try { found = Type.GetType(name, false); }
                catch { }

                if (found == null)
                {
                    try
                    {
                        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try { found = assembly.GetType(name, false); }
                            catch { found = null; }
                            if (found != null) break;
                        }
                    }
                    catch { }
                }

                if (found == null)
                {
                    try
                    {
                        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            Type[] types;
                            try { types = assembly.GetTypes(); }
                            catch { continue; }
                            foreach (Type type in types)
                            {
                                if (type != null && string.Equals(type.Name, name, StringComparison.Ordinal))
                                {
                                    found = type;
                                    break;
                                }
                            }
                            if (found != null) break;
                        }
                    }
                    catch { }
                }

                typeCacheV[name] = found;
                return found;
            }
            catch { return null; }
        }

        private static UnityEngine.Object FindFirstObjectV(Type type)
        {
            if (type == null) return null;
            try
            {
                MethodInfo[] methods = typeof(UnityEngine.Object).GetMethods(
                    BindingFlags.Public | BindingFlags.Static);
                foreach (MethodInfo method in methods)
                {
                    if (method == null || method.Name != "FindObjectOfType" ||
                        !method.IsGenericMethodDefinition) continue;
                    Type[] args = method.GetGenericArguments();
                    if (args == null || args.Length != 1) continue;

                    MethodInfo generic;
                    try { generic = method.MakeGenericMethod(type); }
                    catch { continue; }
                    try { return generic.Invoke(null, null) as UnityEngine.Object; }
                    catch { }
                }
            }
            catch { }

            try
            {
                MethodInfo byType = typeof(UnityEngine.Object).GetMethod(
                    "FindObjectOfType", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Type) }, null);
                if (byType != null)
                    return byType.Invoke(null, new object[] { type }) as UnityEngine.Object;
            }
            catch { }
            return null;
        }
    }
}
