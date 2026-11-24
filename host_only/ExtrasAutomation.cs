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

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ---------------------------------------------------------------------
        // Group L - Automation (feature state; exact field names required)
        // ---------------------------------------------------------------------
        public static bool extraAutoOpenDoors = false;
        public static bool extraAutoFixLights = false;
        public static bool extraAutoFixSabotages = false;
        public static int extraAutoFixSabotagesSeconds = 2;
        public static bool extraAutoCloseDoors = false;
        public static bool extraAutoFillBots = false;
        public static int extraAutoFillBotsCount = 10;
        public static bool extraAutoClearBotsAfterMatch = false;
        public static int extraAutoClearBotsDelay = 3;
        public static bool extraAutoRefreshLobby = false;
        public static int extraAutoRefreshSeconds = 5;

        // ---------------------------------------------------------------------
        // Group L - private helpers state (suffix L, never collides)
        // ---------------------------------------------------------------------
        private static float nextAutoOpenDoorsAtL;
        private static float nextAutoFixLightsAtL;
        private static float nextAutoFixSabotagesAtL;
        private static float nextAutoCloseDoorsAtL;
        private static float nextAutoRefreshLobbyAtL;

        private static int autoFillBotsLobbyTokenL = int.MinValue;
        private static bool autoFillBotsDoneForLobbyL;

        private static bool autoClearBotsArmedL;
        private static bool autoClearBotsDoneL;
        private static float autoClearBotsClearAtL;

        private static string autoFixSabotagesSecondsTextL;
        private static string autoFillBotsCountTextL;
        private static string autoClearBotsDelayTextL;
        private static string autoRefreshSecondsTextL;

        private static readonly Dictionary<string, Type> extrasTypeCacheL = new Dictionary<string, Type>();

        // Fixed, bounded set of non-light sabotage systems handled by auto repair.
        // Electrical is intentionally excluded: the dedicated lights toggle owns it.
        private static readonly SystemTypes[] autoRepairTargetsL =
        {
            SystemTypes.LifeSupp,
            SystemTypes.Reactor,
            SystemTypes.Laboratory,
            SystemTypes.HeliSabotage,
            SystemTypes.Comms,
            SystemTypes.MushroomMixupSabotage
        };

        private const BindingFlags ExtrasFlagsL = BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Instance | BindingFlags.Static;

        // ---------------------------------------------------------------------
        // Public surface (exact names)
        // ---------------------------------------------------------------------

        internal void DrawExtrasL(float width)
        {
            try
            {
                float w = width > 10f ? width : GetMenuWorkWidth(160f, 620f);

                GUILayout.Label(MenuText(L("AUTOMATION", "АВТОМАТИЗАЦИЯ")), headerStyle, GUILayout.Width(w));
                GUILayout.Space(4f);

                extraAutoOpenDoors = DrawToggle(extraAutoOpenDoors,
                    L("Auto Open Doors (Host)", "Авто-открытие дверей (Хост)"), 0, "extraAutoOpenDoors");
                extraAutoFixLights = DrawToggle(extraAutoFixLights,
                    L("Auto Fix Lights (Host)", "Авто-ремонт света (Хост)"), 0, "extraAutoFixLights");
                extraAutoFixSabotages = DrawToggle(extraAutoFixSabotages,
                    L("Auto Fix Sabotages (Host)", "Авто-ремонт саботажей (Хост)"), 0, "extraAutoFixSabotages");
                DrawIntRowL(w, L("Check interval (s)", "Интервал проверки (с)"),
                    ref extraAutoFixSabotagesSeconds, 1, 60, ref autoFixSabotagesSecondsTextL);
                extraAutoCloseDoors = DrawToggle(extraAutoCloseDoors,
                    L("Auto Close Doors (Host)", "Авто-закрытие дверей (Хост)"), 0, "extraAutoCloseDoors");

                GUILayout.Space(6f);

                extraAutoFillBots = DrawToggle(extraAutoFillBots,
                    L("Auto Fill Bots (Host)", "Авто-заполнение ботами (Хост)"), 0, "extraAutoFillBots");
                DrawIntRowL(w, L("Target players", "Цель игроков"),
                    ref extraAutoFillBotsCount, 1, 20, ref autoFillBotsCountTextL);
                extraAutoClearBotsAfterMatch = DrawToggle(extraAutoClearBotsAfterMatch,
                    L("Clear Bots After Match (Host)", "Удалить ботов после матча (Хост)"), 0, "extraAutoClearBotsAfterMatch");
                DrawIntRowL(w, L("Clear delay (s)", "Задержка удаления (с)"),
                    ref extraAutoClearBotsDelay, 0, 60, ref autoClearBotsDelayTextL);

                GUILayout.Space(6f);

                extraAutoRefreshLobby = DrawToggle(extraAutoRefreshLobby,
                    L("Auto Refresh Lobby", "Авто-обновление лобби"), 0, "extraAutoRefreshLobby");
                DrawIntRowL(w, L("Refresh interval (s)", "Интервал обновления (с)"),
                    ref extraAutoRefreshSeconds, 1, 3600, ref autoRefreshSecondsTextL);

                GUILayout.Space(6f);
                GUILayout.Label(MenuText(L(
                    "Host options work only while you host. Sabotage repairs skip lights (use the lights toggle). Bots fill once per lobby and can be cleared after a match.",
                    "Опции хоста работают только когда вы хост. Ремонт саботажей не трогает свет (используйте отдельную опцию). Боты добавляются один раз за лобби и могут удаляться после матча.")),
                    menuDescStyle, GUILayout.Width(w));
            }
            catch { }
        }

        internal static void TickExtrasL(bool inputBlocked)
        {
            // No hotkeys in this group, so inputBlocked is intentionally unused.
            try { TickAutoOpenDoorsL(); } catch { }
            try { TickAutoFixLightsL(); } catch { }
            try { TickAutoFixSabotagesL(); } catch { }
            try { TickAutoCloseDoorsL(); } catch { }
            try { TickAutoFillBotsL(); } catch { }
            try { TickAutoClearBotsL(); } catch { }
            try { TickAutoRefreshLobbyL(); } catch { }
        }

        internal static void LoadExtrasL()
        {
            try
            {
                extraAutoOpenDoors = PlayerPrefs.GetInt("M_ExtrasL_extraAutoOpenDoors", 0) == 1;
                extraAutoFixLights = PlayerPrefs.GetInt("M_ExtrasL_extraAutoFixLights", 0) == 1;
                extraAutoFixSabotages = PlayerPrefs.GetInt("M_ExtrasL_extraAutoFixSabotages", 0) == 1;
                extraAutoFixSabotagesSeconds = Mathf.Clamp(PlayerPrefs.GetInt("M_ExtrasL_extraAutoFixSabotagesSeconds", 2), 1, 60);
                extraAutoCloseDoors = PlayerPrefs.GetInt("M_ExtrasL_extraAutoCloseDoors", 0) == 1;
                extraAutoFillBots = PlayerPrefs.GetInt("M_ExtrasL_extraAutoFillBots", 0) == 1;
                extraAutoFillBotsCount = Mathf.Clamp(PlayerPrefs.GetInt("M_ExtrasL_extraAutoFillBotsCount", 10), 1, 20);
                extraAutoClearBotsAfterMatch = PlayerPrefs.GetInt("M_ExtrasL_extraAutoClearBotsAfterMatch", 0) == 1;
                extraAutoClearBotsDelay = Mathf.Clamp(PlayerPrefs.GetInt("M_ExtrasL_extraAutoClearBotsDelay", 3), 0, 60);
                extraAutoRefreshLobby = PlayerPrefs.GetInt("M_ExtrasL_extraAutoRefreshLobby", 0) == 1;
                extraAutoRefreshSeconds = Mathf.Clamp(PlayerPrefs.GetInt("M_ExtrasL_extraAutoRefreshSeconds", 5), 1, 3600);
            }
            catch { }

            autoFixSabotagesSecondsTextL = extraAutoFixSabotagesSeconds.ToString();
            autoFillBotsCountTextL = extraAutoFillBotsCount.ToString();
            autoClearBotsDelayTextL = extraAutoClearBotsDelay.ToString();
            autoRefreshSecondsTextL = extraAutoRefreshSeconds.ToString();
        }

        internal static void SaveExtrasL()
        {
            try
            {
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoOpenDoors", extraAutoOpenDoors ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoFixLights", extraAutoFixLights ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoFixSabotages", extraAutoFixSabotages ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoFixSabotagesSeconds", Mathf.Clamp(extraAutoFixSabotagesSeconds, 1, 60));
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoCloseDoors", extraAutoCloseDoors ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoFillBots", extraAutoFillBots ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoFillBotsCount", Mathf.Clamp(extraAutoFillBotsCount, 1, 20));
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoClearBotsAfterMatch", extraAutoClearBotsAfterMatch ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoClearBotsDelay", Mathf.Clamp(extraAutoClearBotsDelay, 0, 60));
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoRefreshLobby", extraAutoRefreshLobby ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasL_extraAutoRefreshSeconds", Mathf.Clamp(extraAutoRefreshSeconds, 1, 3600));
            }
            catch { }
        }

        internal static void DrawExtrasLOverlay()
        {
            // This group has no full-screen overlay visuals.
        }

        // ---------------------------------------------------------------------
        // Feature ticks
        // ---------------------------------------------------------------------

        private static void TickAutoOpenDoorsL()
        {
            if (!extraAutoOpenDoors) return;
            if (!IsHostL() || !IsInActiveGameL()) return;
            if (Time.unscaledTime < nextAutoOpenDoorsAtL) return;
            nextAutoOpenDoorsAtL = Time.unscaledTime + 0.75f;

            try
            {
                ShipStatus ship = ShipStatus.Instance;
                if (ship == null || ship.AllDoors == null) return;

                foreach (var door in ship.AllDoors)
                {
                    if (door == null) continue;
                    try { door.SetDoorway(true); } catch { }
                    // Pattern from host_only/SabotageActions.cs: Id | 64 = open.
                    try { ship.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch { }
                }
            }
            catch { }
        }

        private static void TickAutoFixLightsL()
        {
            if (!extraAutoFixLights) return;
            if (!IsHostL() || !IsInActiveGameL()) return;
            if (Time.unscaledTime < nextAutoFixLightsAtL) return;
            nextAutoFixLightsAtL = Time.unscaledTime + 0.5f;

            try
            {
                ShipStatus ship = ShipStatus.Instance;
                if (ship == null || ship.Systems == null) return;

                SystemTypes electric = ResolveElectricSystemL();

                object electricalSystem = null;
                try
                {
                    if (ship.Systems.TryGetValue(electric, out var found)) electricalSystem = found;
                }
                catch { }

                // Only repair while the lights sabotage is actually active.
                if (electricalSystem == null || !IsSystemActiveL(electricalSystem)) return;

                // Requested repair signal (resolved against Electricity/Electrical enum member).
                try { ship.RpcUpdateSystem(electric, 128); } catch { }

                // Align actual switches with expected so the switch system resolves to repaired.
                try
                {
                    if (!ship.Systems.TryGetValue(electric, out var current) || current == null) return;
                    byte expected = ReadByteFieldL(current, "ExpectedSwitches");
                    byte actual = ReadByteFieldL(current, "ActualSwitches");
                    byte diff = (byte)(expected ^ actual);
                    for (int i = 0; i < 5; i++)
                    {
                        if ((diff & (1 << i)) == 0) continue;
                        try { ship.RpcUpdateSystem(electric, (byte)i); } catch { }
                    }
                }
                catch { }
            }
            catch { }
        }

        private static void TickAutoFixSabotagesL()
        {
            if (!extraAutoFixSabotages) return;
            if (!IsHostL() || !IsInActiveGameL()) return;
            if (Time.unscaledTime < nextAutoFixSabotagesAtL) return;
            nextAutoFixSabotagesAtL = Time.unscaledTime + Mathf.Clamp(extraAutoFixSabotagesSeconds, 1, 60);

            try
            {
                ShipStatus ship = ShipStatus.Instance;
                if (ship == null || ship.Systems == null) return;

                for (int i = 0; i < autoRepairTargetsL.Length; i++)
                {
                    SystemTypes type = autoRepairTargetsL[i];
                    try
                    {
                        object system = null;
                        if (ship.Systems.TryGetValue(type, out var found)) system = found;
                        if (system == null || !IsSystemActiveL(system)) continue;

                        // Signals mirror host_only/SabotageActions.cs: 16/17 clear most
                        // sabotages, 0 clears Mushroom Mixup.
                        byte first = 16;
                        byte second = 255;
                        if (type == SystemTypes.MushroomMixupSabotage) first = 0;
                        else if (type == SystemTypes.Comms || type == SystemTypes.HeliSabotage) second = 17;

                        ship.RpcUpdateSystem(type, first);
                        if (second != 255) ship.RpcUpdateSystem(type, second);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TickAutoCloseDoorsL()
        {
            if (!extraAutoCloseDoors) return;
            if (!IsHostL() || !IsInActiveGameL()) return;
            if (Time.unscaledTime < nextAutoCloseDoorsAtL) return;
            nextAutoCloseDoorsAtL = Time.unscaledTime + 5f;

            try
            {
                ShipStatus ship = ShipStatus.Instance;
                if (ship == null || ship.AllDoors == null) return;

                HashSet<SystemTypes> rooms = new HashSet<SystemTypes>();
                foreach (var door in ship.AllDoors)
                {
                    if (door == null) continue;
                    rooms.Add(door.Room);
                    // Pattern from host_only/SabotageActions.cs: plain Id = close.
                    try { ship.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id); } catch { }
                }
                foreach (SystemTypes room in rooms)
                {
                    try { ship.RpcCloseDoorsOfType(room); } catch { }
                }
            }
            catch { }
        }

        private static void TickAutoFillBotsL()
        {
            // Detect lobby changes and reset the "once per lobby" guard.
            int lobbyToken = 0;
            try { if (LobbyBehaviour.Instance != null) lobbyToken = LobbyBehaviour.Instance.GetInstanceID(); }
            catch { lobbyToken = 0; }

            if (lobbyToken != autoFillBotsLobbyTokenL)
            {
                autoFillBotsLobbyTokenL = lobbyToken;
                autoFillBotsDoneForLobbyL = false;
            }

            if (!extraAutoFillBots) return;
            if (lobbyToken == 0) return;          // only inside a lobby
            if (!IsHostL()) return;
            if (autoFillBotsDoneForLobbyL) return; // run once per lobby

            autoFillBotsDoneForLobbyL = true;

            try
            {
                int target = Mathf.Clamp(extraAutoFillBotsCount, 1, 20);
                int current = 0;
                try { if (PlayerControl.AllPlayerControls != null) current = PlayerControl.AllPlayerControls.Count; }
                catch { current = 0; }

                int need = target - current;
                if (need <= 0) return;

                Type botType = FindTypeL("AeroMenu.AeroBots");
                if (botType == null) return;

                MethodInfo addBots = null;
                try
                {
                    addBots = botType.GetMethod("AddBots", ExtrasFlagsL, null, new[] { typeof(int) }, null);
                }
                catch { }
                if (addBots == null || !addBots.IsStatic) return;

                addBots.Invoke(null, new object[] { need });
            }
            catch { }
        }

        private static void TickAutoClearBotsL()
        {
            if (!extraAutoClearBotsAfterMatch)
            {
                autoClearBotsArmedL = false;
                autoClearBotsDoneL = false;
                autoClearBotsClearAtL = 0f;
                return;
            }
            if (!IsHostL()) return;

            bool started = false;
            try { started = AmongUsClient.Instance != null && AmongUsClient.Instance.IsGameStarted; }
            catch { started = false; }

            if (started)
            {
                // Arm only on a real match start, so loading into results does not fire.
                autoClearBotsArmedL = true;
                autoClearBotsDoneL = false;
                autoClearBotsClearAtL = 0f;
                return;
            }

            if (!autoClearBotsArmedL || autoClearBotsDoneL) return;

            float now = Time.unscaledTime;
            if (autoClearBotsClearAtL <= 0f)
                autoClearBotsClearAtL = now + Mathf.Clamp(extraAutoClearBotsDelay, 0, 60);

            if (now < autoClearBotsClearAtL) return;

            autoClearBotsDoneL = true;
            autoClearBotsArmedL = false;

            try
            {
                Type botType = FindTypeL("AeroMenu.AeroBots");
                if (botType == null) return;

                MethodInfo clear = null;
                try { clear = botType.GetMethod("ClearAllBots", ExtrasFlagsL); }
                catch { }
                if (clear == null || !clear.IsStatic) return;

                clear.Invoke(null, null);
            }
            catch { }
        }

        private static void TickAutoRefreshLobbyL()
        {
            if (!extraAutoRefreshLobby) return;
            if (!IsInMainMenuL()) return;
            if (Time.unscaledTime < nextAutoRefreshLobbyAtL) return;

            nextAutoRefreshLobbyAtL = Time.unscaledTime + Mathf.Clamp(extraAutoRefreshSeconds, 1, 3600);

            try
            {
                Type managerType = FindTypeL("FindAGameManager");
                if (managerType == null) return;

                UnityEngine.Object manager = FindFirstObjectL(managerType);
                if (manager == null) return;

                MethodInfo refresh = null;
                try { refresh = managerType.GetMethod("RefreshList", ExtrasFlagsL); }
                catch { }
                if (refresh == null) return;

                if (refresh.IsStatic) refresh.Invoke(null, null);
                else refresh.Invoke(manager, null);
            }
            catch { }
        }

        // ---------------------------------------------------------------------
        // Reflection / guard helpers (all suffixed L)
        // ---------------------------------------------------------------------

        private static bool IsHostL()
        {
            try { return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost; }
            catch { return false; }
        }

        private static bool IsInActiveGameL()
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted) return false;
                if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return false;
                if (MeetingHud.Instance != null) return false;
                return true;
            }
            catch { return false; }
        }

        private static bool IsInMainMenuL()
        {
            try
            {
                if (LobbyBehaviour.Instance != null) return false;
                if (ShipStatus.Instance != null) return false;
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.IsGameStarted) return false;
                return true;
            }
            catch { return false; }
        }

        private static SystemTypes ResolveElectricSystemL()
        {
            try
            {
                string[] names = { "Electricity", "Electrical" };
                foreach (string name in names)
                {
                    try
                    {
                        if (Enum.IsDefined(typeof(SystemTypes), name))
                            return (SystemTypes)Enum.Parse(typeof(SystemTypes), name);
                    }
                    catch { }
                }
            }
            catch { }
            return SystemTypes.Electrical;
        }

        private static bool IsSystemActiveL(object system)
        {
            if (system == null) return false;
            try
            {
                PropertyInfo prop = system.GetType().GetProperty("IsActive", ExtrasFlagsL);
                if (prop != null && prop.CanRead) return (bool)prop.GetValue(system, null);
            }
            catch { }
            try
            {
                FieldInfo field = system.GetType().GetField("IsActive", ExtrasFlagsL);
                if (field != null) return (bool)field.GetValue(system);
            }
            catch { }
            // Unknown shape: return true so repair is still attempted (harmless when already fixed).
            return true;
        }

        private static byte ReadByteFieldL(object target, string fieldName)
        {
            if (target == null) return 0;
            try
            {
                FieldInfo field = target.GetType().GetField(fieldName, ExtrasFlagsL);
                if (field != null)
                {
                    object value = field.GetValue(target);
                    if (value is byte) return (byte)value;
                    if (value != null) return Convert.ToByte(value);
                }
            }
            catch { }
            return 0;
        }

        private static Type FindTypeL(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                Type cached;
                if (extrasTypeCacheL.TryGetValue(name, out cached)) return cached;

                Type found = null;
                try { found = Type.GetType(name, false); } catch { }

                if (found == null)
                {
                    try
                    {
                        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try { found = asm.GetType(name, false); } catch { found = null; }
                            if (found != null) break;
                        }
                    }
                    catch { }
                }

                if (found == null)
                {
                    try
                    {
                        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            Type[] types;
                            try { types = asm.GetTypes(); } catch { continue; }
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

                extrasTypeCacheL[name] = found;
                return found;
            }
            catch { return null; }
        }

        private static UnityEngine.Object FindFirstObjectL(Type type)
        {
            if (type == null) return null;
            try
            {
                MethodInfo[] methods = typeof(UnityEngine.Object).GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (MethodInfo method in methods)
                {
                    if (method == null || method.Name != "FindObjectOfType" || !method.IsGenericMethodDefinition)
                        continue;
                    Type[] args = method.GetGenericArguments();
                    if (args == null || args.Length != 1) continue;

                    MethodInfo generic;
                    try { generic = method.MakeGenericMethod(type); } catch { continue; }
                    try { return generic.Invoke(null, null) as UnityEngine.Object; } catch { }
                }
            }
            catch { }

            // Fallback to the non-generic FindObjectOfType(Type) overload when exposed.
            try
            {
                MethodInfo byType = typeof(UnityEngine.Object).GetMethod(
                    "FindObjectOfType", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Type) }, null);
                if (byType != null) return byType.Invoke(null, new object[] { type }) as UnityEngine.Object;
            }
            catch { }
            return null;
        }

        private void DrawIntRowL(float width, string label, ref int value, int min, int max, ref string buffer)
        {
            try
            {
                value = Mathf.Clamp(value, min, max);
                if (string.IsNullOrEmpty(buffer) || !int.TryParse(buffer, out _))
                    buffer = value.ToString();

                GUILayout.BeginHorizontal(GUILayout.Width(width));
                try
                {
                    GUILayout.Label(label, toggleLabelStyle, GUILayout.Width(Mathf.Max(100f, width * 0.52f)));

                    if (GUILayout.Button("-", btnStyle, GUILayout.Width(26f)))
                    {
                        value = Mathf.Clamp(value - 1, min, max);
                        buffer = value.ToString();
                    }

                    string typed = AeroTextField(buffer, inputBlockStyle, GUILayout.Width(52f));
                    if (!string.Equals(typed, buffer, StringComparison.Ordinal))
                    {
                        buffer = typed;
                        int parsed;
                        if (int.TryParse(buffer, out parsed)) value = Mathf.Clamp(parsed, min, max);
                    }

                    if (GUILayout.Button("+", btnStyle, GUILayout.Width(26f)))
                    {
                        value = Mathf.Clamp(value + 1, min, max);
                        buffer = value.ToString();
                    }
                }
                finally { GUILayout.EndHorizontal(); }
            }
            catch { }
        }
    }
}
