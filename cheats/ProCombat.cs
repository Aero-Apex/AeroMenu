#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ====================================================================
        // Group N - PRO COMBAT state (exact field names required by contract)
        // ====================================================================

        public static bool proKillThroughWalls;
        public static bool proKillAll;
        public static bool proInstantMeeting;
        public static bool proVoteImmunity;
        public static bool proTaskSpeed;
        public static float proTaskSpeedValue = 3f;

        // --------------------------------------------------------------------
        // Private working state (every private member ends with the group "N")
        // --------------------------------------------------------------------

        private const BindingFlags FlagsN = BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static;

        private const KeyCode KillKeyN = KeyCode.K;
        private const KeyCode KillAllKeyN = KeyCode.L;
        private const KeyCode MeetingKeyN = KeyCode.M;

        private const float KillAllIntervalN = 0.30f;
        private const float VoteIntervalN = 0.20f;
        private const float TaskIntervalN = 0.10f;

        private const string OverlayKillAllTextN = "PRO KILL ALL";

        private static Type typePlayerControlN;
        private static Type typeAmongUsClientN;
        private static Type typeMeetingHudN;
        private static Type typeMinigameN;
        private static bool typesResolvedN;

        private static bool killAllActiveN;
        private static float nextKillAllAtN;
        private static float nextVoteAtN;
        private static float nextTaskAtN;

        private static string taskSpeedTextN = "3";
        private static float lastTaskSpeedForTextN = 3f;
        private static string taskSpeedDisplayN = "3.0x";

        private static GUIStyle overlayStyleN;

        // ====================================================================
        // Public surface (called by the coordinator / ProHub)
        // ====================================================================

        internal void DrawProN(float width)
        {
            try
            {
                float work = width > 0f ? width : GetMenuWorkWidth(220f, 520f);
                int toggleW = Mathf.Clamp(Mathf.RoundToInt(work), 170, 560);
                GUIStyle fieldStyle = inputBlockStyle ?? GUI.skin.textField;

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    DrawMenuSectionHeader(L("PRO COMBAT", "ПРО БОЙ"));

                    // ---- Kill Through Walls ---------------------------------
                    bool killThrough = DrawToggle(proKillThroughWalls,
                        L("Kill Through Walls [K]", "Килл сквозь стены [K]"), toggleW, "proKillThroughWalls");
                    if (killThrough != proKillThroughWalls) proKillThroughWalls = killThrough;
                    GUILayout.Label(L("Host only. Press K to murder the nearest player, ignoring line of sight.",
                                      "Только хост. Нажмите K, чтобы убить ближайшего игрока, игнорируя линию видимости."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- Kill All ------------------------------------------
                    bool killAll = DrawToggle(proKillAll,
                        L("Kill All [L]", "Убить всех [L]"), toggleW, "proKillAll");
                    if (killAll != proKillAll) proKillAll = killAll;
                    GUILayout.Label(L("Host only. Press L to start killing every alive player one by one (throttled).",
                                      "Только хост. Нажмите L, чтобы по очереди убить всех живых игроков (с задержкой)."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- Instant Meeting -----------------------------------
                    bool instantMeeting = DrawToggle(proInstantMeeting,
                        L("Instant Meeting [M]", "Мгновенное собрание [M]"), toggleW, "proInstantMeeting");
                    if (instantMeeting != proInstantMeeting) proInstantMeeting = instantMeeting;
                    GUILayout.Label(L("Host only. Press M to call an emergency meeting from anywhere.",
                                      "Только хост. Нажмите M, чтобы созвать экстренное собрание из любой точки."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- Vote Immunity -------------------------------------
                    bool voteImmunity = DrawToggle(proVoteImmunity,
                        L("Vote Immunity (Host)", "Иммунитет к голосам (хост)"), toggleW, "proVoteImmunity");
                    if (voteImmunity != proVoteImmunity) proVoteImmunity = voteImmunity;
                    GUILayout.Label(L("Host only. Forces the vote tally so the local player is never ejected.",
                                      "Только хост. Подменяет подсчёт голосов, чтобы локальный игрок никогда не был изгнан."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- Task Speed ----------------------------------------
                    bool taskSpeed = DrawToggle(proTaskSpeed,
                        L("Task Speed", "Скорость тасков"), toggleW, "proTaskSpeed");
                    if (taskSpeed != proTaskSpeed) proTaskSpeed = taskSpeed;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(L("Multiplier:", "Множитель:"), GUILayout.Width(Mathf.Min(170f, toggleW * 0.55f)));
                    string typed = GUILayout.TextField(taskSpeedTextN, fieldStyle, GUILayout.Width(70f));
                    if (!string.Equals(typed, taskSpeedTextN, StringComparison.Ordinal))
                    {
                        taskSpeedTextN = typed ?? string.Empty;
                        float parsed;
                        if (float.TryParse(taskSpeedTextN, out parsed))
                        {
                            proTaskSpeedValue = Mathf.Clamp(parsed, 1f, 20f);
                            lastTaskSpeedForTextN = proTaskSpeedValue;
                            taskSpeedDisplayN = proTaskSpeedValue.ToString("0.0") + "x";
                        }
                    }
                    GUILayout.Label(taskSpeedDisplayN, GUILayout.Width(56f));
                    GUILayout.EndHorizontal();
                    GUILayout.Label(L("Local/host. Speeds up active task minigame timers (1x - 20x).",
                                      "Локально/хост. Ускоряет таймеры активных мини-игр тасков (1x - 20x)."), menuDescStyle);
                }
                finally
                {
                    GUILayout.EndVertical();
                }
            }
            catch { }
        }

        internal static void TickProN(bool inputBlocked)
        {
            try
            {
                ResolveTypesN();
                if (!inputBlocked) TickHotkeysN();
                TickKillAllN();
                TickVoteImmunityN();
                TickTaskSpeedN();
            }
            catch { }
        }

        internal static void LoadProN()
        {
            try
            {
                proKillThroughWalls = LoadBool("M_ProN_proKillThroughWalls", false);
                proKillAll = LoadBool("M_ProN_proKillAll", false);
                proInstantMeeting = LoadBool("M_ProN_proInstantMeeting", false);
                proVoteImmunity = LoadBool("M_ProN_proVoteImmunity", false);
                proTaskSpeed = LoadBool("M_ProN_proTaskSpeed", false);
                proTaskSpeedValue = Mathf.Clamp(PlayerPrefs.GetFloat("M_ProN_proTaskSpeedValue", 3f), 1f, 20f);
                taskSpeedTextN = proTaskSpeedValue.ToString("0.0");
                lastTaskSpeedForTextN = proTaskSpeedValue;
                taskSpeedDisplayN = proTaskSpeedValue.ToString("0.0") + "x";

                killAllActiveN = false;
                nextKillAllAtN = 0f;
                nextVoteAtN = 0f;
                nextTaskAtN = 0f;
                typesResolvedN = false;
            }
            catch { }
        }

        internal static void SaveProN()
        {
            try
            {
                SaveBool("M_ProN_proKillThroughWalls", proKillThroughWalls);
                SaveBool("M_ProN_proKillAll", proKillAll);
                SaveBool("M_ProN_proInstantMeeting", proInstantMeeting);
                SaveBool("M_ProN_proVoteImmunity", proVoteImmunity);
                SaveBool("M_ProN_proTaskSpeed", proTaskSpeed);
                PlayerPrefs.SetFloat("M_ProN_proTaskSpeedValue", Mathf.Clamp(proTaskSpeedValue, 1f, 20f));
            }
            catch { }
        }

        internal static void DrawProNOverlay()
        {
            try
            {
                if (Screen.width <= 1) return;
                if (Event.current == null || Event.current.type != EventType.Repaint) return;
                if (!proKillAll || !killAllActiveN) return;

                if (overlayStyleN == null)
                {
                    overlayStyleN = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                    overlayStyleN.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);
                }

                Rect r = new Rect(Screen.width * 0.5f - 120f, Screen.height * 0.06f, 240f, 24f);
                GUI.Label(r, OverlayKillAllTextN, overlayStyleN);
            }
            catch { }
        }

        // ====================================================================
        // Type resolution / generic reflection helpers
        // ====================================================================

        private static void ResolveTypesN()
        {
            if (typesResolvedN) return;
            typesResolvedN = true;
            try
            {
                typePlayerControlN = FindTypeN("PlayerControl");
                typeAmongUsClientN = FindTypeN("InnerNet.AmongUsClient");
                if (typeAmongUsClientN == null) typeAmongUsClientN = FindTypeN("AmongUsClient");
                typeMeetingHudN = FindTypeN("MeetingHud");
                typeMinigameN = FindTypeN("Minigame");
            }
            catch { }
        }

        private static Type FindTypeN(string fullName)
        {
            try
            {
                if (string.IsNullOrEmpty(fullName)) return null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                int i;
                for (i = 0; i < assemblies.Length; i++)
                {
                    Assembly asm = assemblies[i];
                    if (asm == null) continue;
                    Type t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }

                string simple = fullName;
                int dot = fullName.LastIndexOf('.');
                if (dot >= 0) simple = fullName.Substring(dot + 1);
                for (i = 0; i < assemblies.Length; i++)
                {
                    Assembly asm = assemblies[i];
                    if (asm == null) continue;
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    if (types == null) continue;
                    for (int j = 0; j < types.Length; j++)
                    {
                        if (types[j] != null && string.Equals(types[j].Name, simple, StringComparison.Ordinal))
                            return types[j];
                    }
                }
            }
            catch { }
            return null;
        }

        private static object GetMemberN(object target, Type type, params string[] names)
        {
            try
            {
                if (type == null || names == null) return null;
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    if (string.IsNullOrEmpty(name)) continue;
                    FieldInfo field = type.GetField(name, FlagsN);
                    if (field != null) return field.GetValue(target);
                    PropertyInfo property = type.GetProperty(name, FlagsN);
                    if (property != null && property.CanRead) return property.GetValue(target, null);
                }
            }
            catch { }
            return null;
        }

        private static object GetStaticMemberN(Type type, params string[] names)
        {
            try
            {
                if (type == null || names == null) return null;
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    if (string.IsNullOrEmpty(name)) continue;
                    FieldInfo field = type.GetField(name, FlagsN | BindingFlags.Static);
                    if (field != null) return field.GetValue(null);
                    PropertyInfo property = type.GetProperty(name, FlagsN | BindingFlags.Static);
                    if (property != null && property.CanRead) return property.GetValue(null, null);
                }
            }
            catch { }
            return null;
        }

        private static bool IsNumericN(Type t)
        {
            return t == typeof(float) || t == typeof(int) || t == typeof(double) ||
                   t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
                   t == typeof(uint) || t == typeof(ushort) || t == typeof(sbyte);
        }

        private static FieldInfo FindNumericFieldN(Type type, params string[] names)
        {
            try
            {
                if (type == null || names == null) return null;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.IsNullOrEmpty(names[i])) continue;
                    FieldInfo field = type.GetField(names[i], FlagsN);
                    if (field != null && !field.IsStatic && !field.IsLiteral && IsNumericN(field.FieldType))
                        return field;
                }
            }
            catch { }
            return null;
        }

        private static bool GetNumericFieldValueN(FieldInfo field, object target, out double value)
        {
            value = 0d;
            try
            {
                if (field == null || target == null) return false;
                object raw = field.GetValue(target);
                if (raw == null) return false;
                value = Convert.ToDouble(raw);
                return true;
            }
            catch { return false; }
        }

        private static void SetNumericFieldValueN(FieldInfo field, object target, double value)
        {
            try
            {
                if (field == null || target == null || field.IsStatic || field.IsLiteral) return;
                object converted = Convert.ChangeType(value, field.FieldType);
                field.SetValue(target, converted);
            }
            catch { }
        }

        private static byte ToByteN(object value)
        {
            try
            {
                if (value == null) return 255;
                return Convert.ToByte(value);
            }
            catch { return 255; }
        }

        private static object ParseEnumN(Type enumType, string valueName)
        {
            try
            {
                if (enumType == null || !enumType.IsEnum) return null;
                string[] names = Enum.GetNames(enumType);
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], valueName, StringComparison.OrdinalIgnoreCase))
                        return Enum.Parse(enumType, names[i]);
                }
            }
            catch { }
            return null;
        }

        // ====================================================================
        // Game state helpers (all via reflection, silent no-op on failure)
        // ====================================================================

        private static bool AmHostN()
        {
            try
            {
                if (typeAmongUsClientN == null) return false;
                object instance = GetStaticMemberN(typeAmongUsClientN, "Instance", "instance");
                if (instance == null) return false;
                object value = GetMemberN(instance, typeAmongUsClientN, "AmHost", "amHost");
                return value is bool host && host;
            }
            catch { return false; }
        }

        private static bool IsGameStartedN()
        {
            try
            {
                if (typeAmongUsClientN == null) return false;
                object instance = GetStaticMemberN(typeAmongUsClientN, "Instance", "instance");
                if (instance == null) return false;
                object value = GetMemberN(instance, typeAmongUsClientN, "IsGameStarted", "isGameStarted");
                return value is bool started && started;
            }
            catch { return false; }
        }

        private static bool IsMeetingActiveN()
        {
            try
            {
                if (typeMeetingHudN == null) return false;
                return GetStaticMemberN(typeMeetingHudN, "Instance", "instance") != null;
            }
            catch { return false; }
        }

        private static bool IsHostAndInGameN()
        {
            try
            {
                if (!AmHostN()) return false;
                if (!IsGameStartedN()) return false;
                if (IsMeetingActiveN()) return false;
                return LocalPlayerN() != null;
            }
            catch { return false; }
        }

        private static object LocalPlayerN()
        {
            try
            {
                if (typePlayerControlN == null) return null;
                return GetStaticMemberN(typePlayerControlN, "LocalPlayer", "localPlayer");
            }
            catch { return null; }
        }

        private static object PlayerDataN(object player)
        {
            if (player == null) return null;
            return GetMemberN(player, player.GetType(), "Data", "data");
        }

        private static byte PlayerIdN(object player)
        {
            try
            {
                if (player == null) return 255;
                object data = PlayerDataN(player);
                object id = data != null ? GetMemberN(data, data.GetType(), "PlayerId", "playerId") : null;
                if (id == null) id = GetMemberN(player, player.GetType(), "PlayerId", "playerId");
                if (id == null) return 255;
                return ToByteN(id);
            }
            catch { return 255; }
        }

        private static bool IsPlayerDeadOrDisconnectedN(object player)
        {
            try
            {
                object data = PlayerDataN(player);
                if (data == null) return true;
                object dead = GetMemberN(data, data.GetType(), "IsDead", "isDead");
                object disconnected = GetMemberN(data, data.GetType(), "Disconnected", "disconnected");
                if (dead is bool isDead && isDead) return true;
                if (disconnected is bool isDisconnected && isDisconnected) return true;
                return false;
            }
            catch { return true; }
        }

        private static Vector3 PlayerPositionN(object player)
        {
            try
            {
                if (player == null) return Vector3.zero;
                object transform = GetMemberN(player, player.GetType(), "transform", "Transform");
                if (transform == null) return Vector3.zero;
                object position = GetMemberN(transform, transform.GetType(), "position", "Position");
                if (position is Vector3 vector) return vector;
            }
            catch { }
            return Vector3.zero;
        }

        private static object NearestAliveTargetN(object local)
        {
            try
            {
                if (local == null || typePlayerControlN == null) return null;
                object all = GetStaticMemberN(typePlayerControlN, "AllPlayerControls", "allPlayerControls");
                if (!(all is System.Collections.IEnumerable enumerable)) return null;

                Vector3 localPosition = PlayerPositionN(local);
                byte localId = PlayerIdN(local);
                object best = null;
                float bestDistance = float.MaxValue;

                foreach (object player in enumerable)
                {
                    if (player == null) continue;
                    if (PlayerIdN(player) == localId) continue;
                    if (IsPlayerDeadOrDisconnectedN(player)) continue;
                    float distance = Vector3.Distance(localPosition, PlayerPositionN(player));
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = player;
                    }
                }
                return best;
            }
            catch { return null; }
        }

        // ====================================================================
        // Killing (host only) - RpcMurderPlayer via reflection
        // ====================================================================

        private static bool TryKillN(object killer, object target)
        {
            try
            {
                if (killer == null || target == null) return false;
                Type type = killer.GetType();
                MethodInfo[] methods = type.GetMethods(FlagsN);
                if (methods == null) return false;

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null) continue;
                    string name = method.Name;
                    if (name != "RpcMurderPlayer" && name != "MurderPlayer") continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        method.Invoke(killer, new object[] { target });
                        return true;
                    }
                    if (parameters.Length != 2) continue;

                    object[] args = new object[2];
                    args[0] = target;
                    Type second = parameters[1].ParameterType;
                    if (second == typeof(bool)) args[1] = true;
                    else if (second == typeof(int)) args[1] = 1;
                    else if (second.IsEnum)
                    {
                        object enumValue = ParseEnumN(second, "Succeeded");
                        if (enumValue == null) continue;
                        args[1] = enumValue;
                    }
                    else continue;

                    method.Invoke(killer, args);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static bool TryKillNearestN()
        {
            try
            {
                object local = LocalPlayerN();
                if (local == null) return false;
                object target = NearestAliveTargetN(local);
                if (target == null) return false;
                return TryKillN(local, target);
            }
            catch { return false; }
        }

        // ====================================================================
        // Hotkeys
        // ====================================================================

        private static void TickHotkeysN()
        {
            try
            {
                if (proKillThroughWalls && Input.GetKeyDown(KillKeyN))
                {
                    if (IsHostAndInGameN()) TryKillNearestN();
                }

                if (proKillAll && Input.GetKeyDown(KillAllKeyN))
                {
                    if (IsHostAndInGameN() && !killAllActiveN)
                    {
                        killAllActiveN = true;
                        nextKillAllAtN = 0f;
                    }
                }

                if (proInstantMeeting && Input.GetKeyDown(MeetingKeyN))
                {
                    if (IsHostAndInGameN()) TryInstantMeetingN();
                }
            }
            catch { }
        }

        private static void TickKillAllN()
        {
            if (!killAllActiveN) return;
            try
            {
                if (!proKillAll || !IsHostAndInGameN())
                {
                    killAllActiveN = false;
                    return;
                }

                float now = Time.unscaledTime;
                if (now < nextKillAllAtN) return;
                nextKillAllAtN = now + KillAllIntervalN;

                object local = LocalPlayerN();
                if (local == null)
                {
                    killAllActiveN = false;
                    return;
                }

                object target = NearestAliveTargetN(local);
                if (target == null)
                {
                    killAllActiveN = false;
                    return;
                }
                TryKillN(local, target);
            }
            catch { killAllActiveN = false; }
        }

        // ====================================================================
        // Instant meeting (host only)
        // ====================================================================

        private static bool TryInstantMeetingN()
        {
            try
            {
                object local = LocalPlayerN();
                if (local == null) return false;
                if (IsMeetingActiveN()) return false;

                Type type = local.GetType();
                MethodInfo[] methods = type.GetMethods(FlagsN);
                if (methods == null) return false;

                // Preferred: PlayerControl.RpcStartMeeting()
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || method.Name != "RpcStartMeeting") continue;
                    if (method.GetParameters().Length != 0) continue;
                    method.Invoke(local, null);
                    return true;
                }

                // Fallback: CmdReportDeadBody(null) opens a meeting with no body.
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || method.Name != "CmdReportDeadBody") continue;
                    if (method.GetParameters().Length != 1) continue;
                    method.Invoke(local, new object[] { null });
                    return true;
                }

                // Fallback: instance StartMeeting()
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || method.Name != "StartMeeting") continue;
                    if (method.GetParameters().Length != 0) continue;
                    method.Invoke(local, null);
                    return true;
                }
            }
            catch { }
            return false;
        }

        // ====================================================================
        // Vote immunity (host only) - force the vote tally via reflection
        // ====================================================================

        private static void TickVoteImmunityN()
        {
            if (!proVoteImmunity) return;
            try
            {
                if (!AmHostN()) return;
                object hud = GetStaticMemberN(typeMeetingHudN, "Instance", "instance");
                if (hud == null) return;

                float now = Time.unscaledTime;
                if (now < nextVoteAtN) return;
                nextVoteAtN = now + VoteIntervalN;

                ForceVoteTallyN(hud);
            }
            catch { }
        }

        private static void ForceVoteTallyN(object hud)
        {
            try
            {
                if (hud == null) return;
                Type hudType = hud.GetType();
                object statesObject = GetMemberN(hud, hudType,
                    "playerStates", "PlayerStates", "playerVotes", "PlayerVotes");
                if (!(statesObject is System.Collections.IEnumerable enumerable)) return;

                byte localId = PlayerIdN(LocalPlayerN());
                if (localId == 255) return;

                byte boostId = 255;
                foreach (object state in enumerable)
                {
                    if (state == null) continue;
                    byte id = ToByteN(GetMemberN(state, state.GetType(), "PlayerId", "playerId"));
                    if (id != localId && id != 255)
                    {
                        boostId = id;
                        break;
                    }
                }
                if (boostId == 255) return;

                foreach (object state in enumerable)
                {
                    if (state == null) continue;
                    Type stateType = state.GetType();

                    FieldInfo voteFor = FindNumericFieldN(stateType, "voteFor", "VoteFor", "votedFor", "VotedFor");
                    if (voteFor != null)
                    {
                        double current;
                        if (!GetNumericFieldValueN(voteFor, state, out current)) current = 255d;
                        if (Math.Abs(current - localId) < 0.5d)
                            SetNumericFieldValueN(voteFor, state, boostId);
                    }

                    FieldInfo votes = FindNumericFieldN(stateType, "votes", "Votes", "voteCount", "VoteCount", "numVotes");
                    if (votes != null)
                    {
                        byte id = ToByteN(GetMemberN(state, stateType, "PlayerId", "playerId"));
                        if (id == localId) SetNumericFieldValueN(votes, state, 0d);
                        else if (id == boostId) SetNumericFieldValueN(votes, state, 99d);
                    }
                }
            }
            catch { }
        }

        // ====================================================================
        // Task speed (local / host) - accelerate active minigame timers
        // ====================================================================

        private static void TickTaskSpeedN()
        {
            if (!proTaskSpeed) return;
            if (proTaskSpeedValue <= 1f) return;
            try
            {
                if (Mathf.Abs(proTaskSpeedValue - lastTaskSpeedForTextN) > 0.001f)
                {
                    lastTaskSpeedForTextN = proTaskSpeedValue;
                    taskSpeedDisplayN = proTaskSpeedValue.ToString("0.0") + "x";
                }

                float now = Time.unscaledTime;
                if (now < nextTaskAtN) return;
                nextTaskAtN = now + TaskIntervalN;

                if (typeMinigameN == null) return;
                object current = GetStaticMemberN(typeMinigameN, "Instance", "instance");
                if (current != null) AccelerateMinigameN(current);
                AccelerateAllMinigamesN();
            }
            catch { }
        }

        private static void AccelerateAllMinigamesN()
        {
            try
            {
                MethodInfo finder = typeof(UnityEngine.Object).GetMethod(
                    "FindObjectsOfType", FlagsN, null, new[] { typeof(Type) }, null);
                if (finder == null) return;

                object result = finder.Invoke(null, new object[] { typeMinigameN });
                if (!(result is System.Collections.IEnumerable enumerable)) return;
                foreach (object minigame in enumerable)
                {
                    if (minigame != null) AccelerateMinigameN(minigame);
                }
            }
            catch { }
        }

        private static void AccelerateMinigameN(object minigame)
        {
            try
            {
                if (minigame == null) return;
                float delta = Time.deltaTime * (proTaskSpeedValue - 1f);
                if (delta <= 0f) return;

                Type type = minigame.GetType();
                while (type != null)
                {
                    AccelerateTimerFieldsN(minigame, type, delta);
                    type = type.BaseType;
                }
            }
            catch { }
        }

        private static void AccelerateTimerFieldsN(object minigame, Type type, float delta)
        {
            try
            {
                FieldInfo[] fields = type.GetFields(FlagsN | BindingFlags.DeclaredOnly);
                if (fields == null) return;
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field == null || field.IsStatic || field.IsLiteral) continue;
                    if (!IsNumericN(field.FieldType)) continue;

                    string name = field.Name ?? string.Empty;
                    bool isProgress = name.IndexOf("progress", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isTimer = name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   name.IndexOf("timeleft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   name.IndexOf("timeremaining", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   isProgress;
                    if (!isTimer) continue;

                    double current;
                    if (!GetNumericFieldValueN(field, minigame, out current)) continue;
                    if (double.IsNaN(current) || double.IsInfinity(current)) continue;

                    double next = isProgress ? current + delta : current - delta;
                    if (!isProgress && next < 0d) next = 0d;
                    SetNumericFieldValueN(field, minigame, next);
                }
            }
            catch { }
        }
    }
}
