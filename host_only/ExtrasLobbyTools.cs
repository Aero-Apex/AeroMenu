#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using InnerNet;
using AmongUs.GameOptions;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Group I state (exact field names required by the contract)
        // --------------------------------------------------------------------
        public static bool extraLobbyTimer = false;

        public static bool extraAutoStartAtCount = false;
        public static int extraAutoStartCount = 4;

        public static bool extraAutoKickNamePattern = false;
        public static string extraKickNamePattern = string.Empty;

        public static bool extraVanillaRulesPreset = false;

        public static bool extraRoomCodeInStatus = false;

        // --------------------------------------------------------------------
        // Internal bookkeeping (all helpers/fields end with I)
        // --------------------------------------------------------------------
        private const BindingFlags AllMembersI = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private const string StatusRoomMarkerI = "  [room: ";

        private static int lobbyTimerGameIdI = -1;
        private static float lobbyTimerStartI;

        private static int autoStartGameIdI = -1;
        private static bool autoStartFiredI;
        private static float nextAutoStartCheckI;

        private static int kickPatternGameIdI = -1;
        private static readonly HashSet<int> kickedPatternOwnersI = new HashSet<int>();
        private static float nextAutoKickCheckI;

        private static string statusBackupI;
        private static string statusLastCodeI = string.Empty;

        private static GUIStyle lobbyTimerStyleI;

        // --------------------------------------------------------------------
        // Required public surface
        // --------------------------------------------------------------------

        internal void DrawExtrasI(float width)
        {
            try
            {
                int toggleWidth = Mathf.RoundToInt(Mathf.Clamp(width * 0.55f, 150f, 310f));
                int inputWidth = Mathf.RoundToInt(Mathf.Clamp(width * 0.28f, 70f, 150f));

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    GUILayout.Label(MenuText(L("LOBBY", "ЛОББИ")), headerStyle);

                    bool host = IsHostI();
                    GUILayout.Label(MenuText(host
                        ? L("Host rights: active", "Права хоста: активны")
                        : L("Host rights: inactive", "Права хоста: неактивны")), menuDescStyle);
                    GUILayout.Space(6);

                    // 41 - lobby elapsed timer overlay
                    extraLobbyTimer = DrawToggle(extraLobbyTimer,
                        L("Lobby elapsed timer", "Таймер лобби"), toggleWidth);
                    GUILayout.Label(MenuText(L("Overlay counts time since the current lobby was joined.",
                        "Оверлей считает время с входа в текущее лобби.")), menuDescStyle);
                    GUILayout.Space(6);

                    // 42 - host auto start at connected player count
                    extraAutoStartAtCount = DrawToggle(extraAutoStartAtCount,
                        L("Auto start at player count", "Авто-старт по числу игроков"), toggleWidth);
                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label(MenuText(L("Players:", "Игроков:")), toggleLabelStyle, GUILayout.Width(inputWidth));
                        string countText = GUILayout.TextField(extraAutoStartCount.ToString(),
                            inputBlockStyle, GUILayout.Width(inputWidth));
                        if (int.TryParse(countText, out int count))
                            extraAutoStartCount = Mathf.Clamp(count, 1, 15);
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Label(MenuText(L("Host only. Fires once per lobby when everyone is loaded.",
                        "Только хост. Срабатывает один раз за лобби, когда все загружены.")), menuDescStyle);
                    GUILayout.Space(6);

                    // 43 - host kick by name pattern
                    extraAutoKickNamePattern = DrawToggle(extraAutoKickNamePattern,
                        L("Auto kick by name pattern", "Авто-кик по шаблону имени"), toggleWidth);
                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label(MenuText(L("Pattern:", "Шаблон:")), toggleLabelStyle, GUILayout.Width(inputWidth));
                        string patternText = GUILayout.TextField(extraKickNamePattern ?? string.Empty,
                            inputBlockStyle, GUILayout.Width(Mathf.Max(inputWidth, 120)));
                        if (patternText != null && patternText.Length > 40) patternText = patternText.Substring(0, 40);
                        extraKickNamePattern = patternText ?? string.Empty;
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Label(MenuText(L("Host only. Case-insensitive, one kick per player.",
                        "Только хост. Без учёта регистра, один кик на игрока.")), menuDescStyle);
                    GUILayout.Space(6);

                    // 44 - vanilla rules preset button
                    extraVanillaRulesPreset = DrawToggle(extraVanillaRulesPreset,
                        L("Vanilla rules preset", "Ванильные правила"), toggleWidth);
                    if (extraVanillaRulesPreset)
                    {
                        if (GUILayout.Button(MenuText(L("Apply Vanilla Rules", "Применить ванильные правила")),
                                btnStyle, GUILayout.Height(24)))
                            ApplyVanillaRulesI();
                        GUILayout.Label(MenuText(L("Host only. Restores standard lobby rule values.",
                            "Только хост. Восстанавливает стандартные правила лобби.")), menuDescStyle);
                    }
                    GUILayout.Space(6);

                    // 45 - room code appended to the menu status bar
                    extraRoomCodeInStatus = DrawToggle(extraRoomCodeInStatus,
                        L("Room code in status bar", "Код комнаты в статус-баре"), toggleWidth);
                    GUILayout.Label(MenuText(L("Appends the room code, keeping custom status text intact.",
                        "Добавляет код комнаты, сохраняя пользовательский текст статуса.")), menuDescStyle);
                }
                catch { }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }

        internal static void TickExtrasI(bool inputBlocked)
        {
            try
            {
                TickLobbyTimerI();
                TickAutoStartI();
                TickAutoKickPatternI();
                TickRoomCodeStatusI();
            }
            catch { }
        }

        internal static void LoadExtrasI()
        {
            try
            {
                extraLobbyTimer = LoadBool("M_ExtrasI_extraLobbyTimer", false);

                extraAutoStartAtCount = LoadBool("M_ExtrasI_extraAutoStartAtCount", false);
                extraAutoStartCount = PlayerPrefs.GetInt("M_ExtrasI_extraAutoStartCount", 4);

                extraAutoKickNamePattern = LoadBool("M_ExtrasI_extraAutoKickNamePattern", false);
                extraKickNamePattern = PlayerPrefs.GetString("M_ExtrasI_extraKickNamePattern", string.Empty);

                extraVanillaRulesPreset = LoadBool("M_ExtrasI_extraVanillaRulesPreset", false);

                extraRoomCodeInStatus = LoadBool("M_ExtrasI_extraRoomCodeInStatus", false);
            }
            catch { }
        }

        internal static void SaveExtrasI()
        {
            try
            {
                SaveBool("M_ExtrasI_extraLobbyTimer", extraLobbyTimer);

                SaveBool("M_ExtrasI_extraAutoStartAtCount", extraAutoStartAtCount);
                PlayerPrefs.SetInt("M_ExtrasI_extraAutoStartCount", extraAutoStartCount);

                SaveBool("M_ExtrasI_extraAutoKickNamePattern", extraAutoKickNamePattern);
                PlayerPrefs.SetString("M_ExtrasI_extraKickNamePattern", extraKickNamePattern ?? string.Empty);

                SaveBool("M_ExtrasI_extraVanillaRulesPreset", extraVanillaRulesPreset);

                SaveBool("M_ExtrasI_extraRoomCodeInStatus", extraRoomCodeInStatus);
            }
            catch { }
        }

        internal static void DrawExtrasIOverlay()
        {
            if (!extraLobbyTimer) return;

            try
            {
                float start = lobbyTimerStartI;
                if (start <= 0f) return;

                GUIStyle style = GetLobbyTimerStyleI();
                if (style == null) return;

                int secs = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - start));
                string elapsed = secs >= 3600
                    ? $"{secs / 3600}:{(secs / 60) % 60:00}:{secs % 60:00}"
                    : $"{secs / 60:00}:{secs % 60:00}";
                string text = MenuText(L("Lobby", "Лобби") + "  " + elapsed);

                float w = 190f;
                float h = 34f;
                Rect rect = new Rect(Screen.width * 0.5f - w * 0.5f, 8f, w, h);
                GUI.Box(rect, text, style);
            }
            catch { }
        }

        // --------------------------------------------------------------------
        // Feature ticks
        // --------------------------------------------------------------------

        private static void TickLobbyTimerI()
        {
            try
            {
                int gameId = GetGameIdI();
                if (gameId != lobbyTimerGameIdI)
                {
                    lobbyTimerGameIdI = gameId;
                    lobbyTimerStartI = gameId > 0 ? Time.unscaledTime : 0f;
                }

                if (gameId <= 0) lobbyTimerStartI = 0f;
            }
            catch { }
        }

        private static void TickAutoStartI()
        {
            if (!extraAutoStartAtCount || !IsHostI()) return;

            float now = Time.unscaledTime;
            if (now < nextAutoStartCheckI) return;
            nextAutoStartCheckI = now + 0.25f;

            try
            {
                int gameId = GetGameIdI();
                if (gameId != autoStartGameIdI)
                {
                    autoStartGameIdI = gameId;
                    autoStartFiredI = false;
                }

                if (autoStartFiredI || gameId <= 0 || !InLobbyI()) return;

                int target = Mathf.Clamp(extraAutoStartCount, 1, 15);
                if (!AreAllLobbyPlayersLoadedForStart(out int connected, out int ready, out string loadingName))
                    return;
                if (connected < target || ready < connected) return;

                // Same start path the menu "Insta Start" button uses.
                bool started = TryInstaStartAfterEveryoneLoaded(false);
                if (!started) return;

                autoStartFiredI = true;
                ShowNotification("<color=#7CFC98>[AUTO START]</color> " + connected + " players, starting.");
            }
            catch { }
        }

        private static void TickAutoKickPatternI()
        {
            if (!extraAutoKickNamePattern || !IsHostI()) return;

            string pattern = extraKickNamePattern ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pattern)) return;

            float now = Time.unscaledTime;
            if (now < nextAutoKickCheckI) return;
            nextAutoKickCheckI = now + 0.3f;

            try
            {
                int gameId = GetGameIdI();
                if (gameId != kickPatternGameIdI)
                {
                    kickPatternGameIdI = gameId;
                    kickedPatternOwnersI.Clear();
                }

                if (gameId <= 0) return;

                PlayerControl local = PlayerControl.LocalPlayer;
                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;

                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc == local || pc.Data == null) continue;

                        object bot = GetValueI(pc.Data, "Bot");
                        if (bot is bool isBot && isBot) continue;

                        string playerName = pc.Data.PlayerName;
                        if (string.IsNullOrEmpty(playerName)) continue;
                        if (playerName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        int owner = Convert.ToInt32(pc.OwnerId);
                        if (owner < 0 || kickedPatternOwnersI.Contains(owner)) continue;

                        kickedPatternOwnersI.Add(owner);
                        AmongUsClient.Instance.KickPlayer(owner, false);
                        ShowNotification("<color=#FFAC1C>[AUTO KICK]</color> " + playerName);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TickRoomCodeStatusI()
        {
            try
            {
                string code = string.Empty;
                if (extraRoomCodeInStatus)
                {
                    string raw = GetCurrentRoomCodeForStatus();
                    if (!string.IsNullOrWhiteSpace(raw) &&
                        !string.Equals(raw, "No room", StringComparison.OrdinalIgnoreCase))
                        code = raw;
                }

                if (!extraRoomCodeInStatus || string.IsNullOrEmpty(code))
                {
                    if (statusBackupI != null)
                    {
                        statusBarText = statusBackupI;
                        statusBackupI = null;
                    }
                    statusLastCodeI = string.Empty;
                    return;
                }

                if (statusBackupI == null) statusBackupI = statusBarText ?? string.Empty;

                string current = statusBarText ?? string.Empty;
                string expected = BuildStatusWithCodeI(statusBackupI, statusLastCodeI);
                bool externallyEdited = !string.IsNullOrEmpty(statusLastCodeI) &&
                                        !string.Equals(current, expected, StringComparison.Ordinal);
                if (externallyEdited)
                    statusBackupI = StripStatusSuffixI(current);

                if (string.Equals(statusLastCodeI, code, StringComparison.Ordinal) && !externallyEdited)
                    return;

                statusLastCodeI = code;
                statusBarText = BuildStatusWithCodeI(statusBackupI, code);
            }
            catch { }
        }

        // --------------------------------------------------------------------
        // Vanilla rules preset (reflection, guarded)
        // --------------------------------------------------------------------

        private static void ApplyVanillaRulesI()
        {
            if (!IsHostI())
            {
                ShowNotification("<color=#FF4444>[VANILLA]</color> Host only.");
                return;
            }

            try
            {
                object manager = GameOptionsManager.Instance;
                if (manager == null)
                {
                    ShowNotification("<color=#FF4444>[VANILLA]</color> Game options not ready.");
                    return;
                }

                int applied = 0;
                List<object> done = new List<object>();

                foreach (string member in new[]
                         {
                             "CurrentGameOptions", "currentNormalGameOptions",
                             "currentGameOptions", "GameHostOptions"
                         })
                {
                    object opts = GetValueI(manager, member);
                    if (opts == null) continue;

                    bool duplicate = false;
                    foreach (object existing in done)
                    {
                        if (ReferenceEquals(existing, opts)) { duplicate = true; break; }
                    }
                    if (duplicate) continue;
                    done.Add(opts);

                    if (SetVanillaOptionI(opts, 1, "SetInt", "NumImpostors")) applied++;
                    if (SetVanillaOptionI(opts, 30f, "SetFloat", "KillCooldown")) applied++;
                    if (SetVanillaOptionI(opts, 1f, "SetFloat", "PlayerSpeedMod")) applied++;
                    if (SetVanillaOptionI(opts, 1f, "SetFloat", "CrewmateVision")) applied++;
                    if (SetVanillaOptionI(opts, 1.5f, "SetFloat", "ImpostorVision")) applied++;
                    if (SetVanillaOptionI(opts, 120f, "SetFloat", "VotingTime")) applied++;
                    if (SetVanillaOptionI(opts, 120, "SetInt", "VotingTime")) applied++;
                    if (SetVanillaOptionI(opts, 15f, "SetFloat", "DiscussionTime")) applied++;
                    if (SetVanillaOptionI(opts, 15, "SetInt", "DiscussionTime")) applied++;
                    if (SetVanillaOptionI(opts, 1, "SetInt", "NumCommonTasks")) applied++;
                    if (SetVanillaOptionI(opts, 1, "SetInt", "NumLongTasks")) applied++;
                    if (SetVanillaOptionI(opts, 2, "SetInt", "NumShortTasks")) applied++;
                }

                if (applied > 0)
                    ShowNotification("<color=#7CFC98>[VANILLA]</color> Applied " + applied + " rules.");
                else
                    ShowNotification("<color=#FF4444>[VANILLA]</color> Could not apply rules.");
            }
            catch
            {
                ShowNotification("<color=#FF4444>[VANILLA]</color> Could not apply rules.");
            }
        }

        private static bool SetVanillaOptionI(object opts, object value, string setter, string name)
        {
            if (opts == null || string.IsNullOrEmpty(name)) return false;

            try
            {
                Type type = opts.GetType();

                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    foreach (MethodInfo method in cur.GetMethods(AllMembersI))
                    {
                        if (!string.Equals(method.Name, setter, StringComparison.Ordinal)) continue;

                        ParameterInfo[] ps = method.GetParameters();
                        if (ps.Length != 2 || !ps[0].ParameterType.IsEnum) continue;

                        object enumValue = FindEnumI(ps[0].ParameterType, name);
                        if (enumValue == null) continue;

                        try
                        {
                            object arg = CastValueI(value, ps[1].ParameterType);
                            method.Invoke(opts, new object[] { enumValue, arg });
                            return true;
                        }
                        catch { }
                    }
                }

                // Fallback for versions exposing option values as plain properties.
                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    PropertyInfo property = cur.GetProperty(name, AllMembersI);
                    if (property == null || !property.CanWrite) continue;
                    try
                    {
                        object arg = CastValueI(value, property.PropertyType);
                        property.SetValue(opts, arg, null);
                        return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private static object FindEnumI(Type enumType, string name)
        {
            if (enumType == null || !enumType.IsEnum || string.IsNullOrEmpty(name)) return null;
            try
            {
                Array values = Enum.GetValues(enumType);
                foreach (object value in values)
                {
                    if (string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase))
                        return value;
                }
            }
            catch { }
            return null;
        }

        private static object CastValueI(object value, Type target)
        {
            try
            {
                if (target == typeof(float)) return Convert.ToSingle(value);
                if (target == typeof(double)) return Convert.ToDouble(value);
                if (target == typeof(int)) return Convert.ToInt32(value);
                if (target == typeof(byte)) return Convert.ToByte(value);
                if (target == typeof(bool)) return Convert.ToBoolean(value);
                if (target.IsEnum) return Enum.ToObject(target, Convert.ToInt32(value));
            }
            catch { }
            return value;
        }

        // --------------------------------------------------------------------
        // Status-bar helpers
        // --------------------------------------------------------------------

        private static string BuildStatusWithCodeI(string baseText, string code)
        {
            string clean = StripStatusSuffixI(baseText ?? string.Empty);
            return string.IsNullOrEmpty(code) ? clean : clean + StatusRoomMarkerI + code + "]";
        }

        private static string StripStatusSuffixI(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            try
            {
                int idx = text.LastIndexOf(StatusRoomMarkerI, StringComparison.Ordinal);
                if (idx >= 0 && text.EndsWith("]", StringComparison.Ordinal))
                    return text.Substring(0, idx);
            }
            catch { }
            return text;
        }

        // --------------------------------------------------------------------
        // Shared helpers (reflection, guarded)
        // --------------------------------------------------------------------

        private static GUIStyle GetLobbyTimerStyleI()
        {
            if (lobbyTimerStyleI != null) return lobbyTimerStyleI;
            try
            {
                GUIStyle baseStyle = GUI.skin != null ? GUI.skin.box : null;
                lobbyTimerStyleI = baseStyle != null ? new GUIStyle(baseStyle) : new GUIStyle();
                lobbyTimerStyleI.alignment = TextAnchor.MiddleCenter;
                lobbyTimerStyleI.fontSize = 15;
                lobbyTimerStyleI.fontStyle = FontStyle.Bold;
                lobbyTimerStyleI.richText = true;
                lobbyTimerStyleI.padding = new RectOffset { left = 8, right = 8, top = 4, bottom = 4 };
                lobbyTimerStyleI.normal.textColor = new Color(1f, 0.84f, 0.36f, 1f);
            }
            catch { }
            return lobbyTimerStyleI;
        }

        private static bool IsHostI()
        {
            try { return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost; }
            catch { return false; }
        }

        private static int GetGameIdI()
        {
            try
            {
                if (AmongUsClient.Instance == null) return -1;
                return AmongUsClient.Instance.GameId;
            }
            catch { return -1; }
        }

        private static bool InLobbyI()
        {
            try
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.IsGameStarted) return false;
                return LobbyBehaviour.Instance != null;
            }
            catch { return false; }
        }

        private static object GetValueI(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                for (Type cur = instance.GetType(); cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        PropertyInfo property = cur.GetProperty(name, AllMembersI);
                        if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                            return property.GetValue(instance, null);
                    }
                    catch { }
                    try
                    {
                        FieldInfo field = cur.GetField(name, AllMembersI);
                        if (field != null) return field.GetValue(instance);
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }
    }
}
