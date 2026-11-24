#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using AmongUs.GameOptions;
using InnerNet;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Group D state (exact field names required by the contract)
        // --------------------------------------------------------------------
        public static bool extraFreezePlayers = false;

        public static bool extraKillCooldownOverride = false;
        public static float extraKillCooldownSeconds = 10f;

        public static bool extraAutoEndGame = false;
        public static int extraAutoEndMinutes = 5;

        public static bool extraKickNoFriendCode = false;

        public static bool extraMuteAllToggle = false;

        public static bool extraHostAnnounce = false;
        public static string extraHostAnnounceText = string.Empty;

        public static bool extraAfkKick = false;
        public static int extraAfkKickSeconds = 120;

        // --------------------------------------------------------------------
        // Internal bookkeeping (all helpers/fields end with D)
        // --------------------------------------------------------------------
        private const BindingFlags AllMembersD = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static float nextFreezeD;
        private static float nextKillCooldownD;

        private static int autoEndGameIdD = -1;
        private static float autoEndStartD;
        private static bool autoEndFiredD;

        private static readonly Dictionary<int, float> noFcFirstSeenD = new Dictionary<int, float>();
        private static readonly HashSet<int> noFcProcessedOwnersD = new HashSet<int>();
        private static int kickGameIdD = -1;

        private static int muteGameIdD = -1;
        private static bool muteAllAppliedD;
        private static bool muteAllPrevD;

        private static float nextAfkCheckD;
        private static int afkGameIdD = -1;
        private static readonly Dictionary<int, Vector3> afkLastPosD = new Dictionary<int, Vector3>();
        private static readonly Dictionary<int, float> afkLastMoveD = new Dictionary<int, float>();
        private static readonly HashSet<int> afkKickedOwnersD = new HashSet<int>();

        private static Type cachedSnapTypeD;
        private static MethodInfo cachedSnapMethodD;
        private static Type cachedKillTypeD;
        private static FieldInfo cachedKillFieldD;

        // --------------------------------------------------------------------
        // Required public surface
        // --------------------------------------------------------------------

        internal void DrawExtrasD(float width)
        {
            try
            {
                int toggleWidth = Mathf.RoundToInt(Mathf.Clamp(width * 0.55f, 150f, 310f));
                int inputWidth = Mathf.RoundToInt(Mathf.Clamp(width * 0.28f, 70f, 150f));

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    GUILayout.Label(MenuText(L("HOST TOOLS", "ХОСТ ИНСТРУМЕНТЫ")), headerStyle);

                    bool host = IsHostD();
                    GUILayout.Label(MenuText(host
                        ? L("Host rights: active", "Права хоста: активны")
                        : L("Host rights: inactive", "Права хоста: неактивны")), menuDescStyle);
                    GUILayout.Space(6);

                    extraFreezePlayers = DrawToggle(extraFreezePlayers,
                        L("Freeze other players", "Заморозка других игроков"), toggleWidth);
                    GUILayout.Label(MenuText(L("Repeatedly snaps remote players to their position.",
                        "Постоянно привязывает удалённых игроков к их позиции.")), menuDescStyle);
                    GUILayout.Space(6);

                    extraKillCooldownOverride = DrawToggle(extraKillCooldownOverride,
                        L("Force kill cooldown", "Принудительный КД убийства"), toggleWidth);
                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label(MenuText(L("Seconds:", "Секунды:")), toggleLabelStyle, GUILayout.Width(inputWidth));
                        string secondsText = AeroTextField(
                            extraKillCooldownSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                            inputBlockStyle, GUILayout.Width(inputWidth));
                        if (float.TryParse(secondsText, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float seconds))
                            extraKillCooldownSeconds = Mathf.Clamp(seconds, 0f, 600f);
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Label(MenuText(L("Applies to every impostor-team player.",
                        "Применяется ко всем игрокам команды предателей.")), menuDescStyle);
                    GUILayout.Space(6);

                    extraAutoEndGame = DrawToggle(extraAutoEndGame,
                        L("Auto end game", "Авто-конец игры"), toggleWidth);
                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label(MenuText(L("Minutes:", "Минуты:")), toggleLabelStyle, GUILayout.Width(inputWidth));
                        string minutesText = AeroTextField(extraAutoEndMinutes.ToString(),
                            inputBlockStyle, GUILayout.Width(inputWidth));
                        if (int.TryParse(minutesText, out int minutes))
                            extraAutoEndMinutes = Mathf.Clamp(minutes, 0, 240);
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Label(MenuText(L("Ends the match after the configured time.",
                        "Завершает матч по истечении заданного времени.")), menuDescStyle);
                    GUILayout.Space(6);

                    extraKickNoFriendCode = DrawToggle(extraKickNoFriendCode,
                        L("Kick players without friend code", "Кик игроков без френд-кода"), toggleWidth);
                    GUILayout.Label(MenuText(L("2s grace period, one kick per owner.",
                        "2 секунды ожидания, один кик на владельца.")), menuDescStyle);
                    GUILayout.Space(6);

                    extraMuteAllToggle = DrawToggle(extraMuteAllToggle,
                        L("Mute all remote players", "Заглушить всех удалённых игроков"), toggleWidth);
                    GUILayout.Label(MenuText(L("Best-effort reflective mute toggle (host only).",
                        "Переключатель заглушения через рефлексию (только хост).")), menuDescStyle);
                    GUILayout.Space(6);

                    extraHostAnnounce = DrawToggle(extraHostAnnounce,
                        L("Host announcement", "Объявление от хоста"), toggleWidth);
                    if (extraHostAnnounce)
                    {
                        GUILayout.BeginHorizontal();
                        try
                        {
                            GUILayout.Label(MenuText(L("Message:", "Сообщение:")), toggleLabelStyle, GUILayout.Width(inputWidth));
                            string message = AeroTextField(extraHostAnnounceText ?? string.Empty,
                                inputBlockStyle, GUILayout.Width(Mathf.Max(inputWidth, 120)));
                            if (message != null && message.Length > 90) message = message.Substring(0, 90);
                            extraHostAnnounceText = message ?? string.Empty;
                        }
                        finally { GUILayout.EndHorizontal(); }
                        GUILayout.BeginHorizontal();
                        try
                        {
                            if (GUILayout.Button(MenuText(L("Send to chat", "Отправить в чат")),
                                    btnStyle, GUILayout.Height(24)))
                                SendHostAnnouncementD(extraHostAnnounceText);
                            if (GUILayout.Button(MenuText(L("Post room code", "Код комнаты в чат")),
                                    btnStyle, GUILayout.Height(24)))
                                PostRoomCodeD();
                        }
                        finally { GUILayout.EndHorizontal(); }
                        GUILayout.Label(MenuText(L("Host only. Sends a visible chat message to every player.",
                            "Только хост. Отправляет видимое сообщение в чат всем игрокам.")), menuDescStyle);
                    }
                    GUILayout.Space(6);

                    extraAfkKick = DrawToggle(extraAfkKick,
                        L("Kick idle players", "Кик неактивных игроков"), toggleWidth);
                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label(MenuText(L("Idle (s):", "Неактив (с):")), toggleLabelStyle, GUILayout.Width(inputWidth));
                        string idleText = AeroTextField(extraAfkKickSeconds.ToString(),
                            inputBlockStyle, GUILayout.Width(inputWidth));
                        if (int.TryParse(idleText, out int idle))
                            extraAfkKickSeconds = Mathf.Clamp(idle, 30, 600);
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Label(MenuText(L("Host only. Kicks alive, non-bot players that stop moving for too long.",
                        "Только хост. Кикает живых игроков не-ботов, которые слишком долго не двигаются.")), menuDescStyle);
                }
                catch { }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }

        internal static void TickExtrasD(bool inputBlocked)
        {
            try
            {
                if (!IsHostD())
                {
                    ResetD();
                    return;
                }

                TickFreezeD();
                TickKillCooldownD();
                TickAutoEndD();
                TickKickNoFriendCodeD();
                TickMuteAllD();
                TickAfkKickD();
            }
            catch { }
        }

        internal static void LoadExtrasD()
        {
            try
            {
                extraFreezePlayers = LoadBool("M_ExtrasD_extraFreezePlayers", false);

                extraKillCooldownOverride = LoadBool("M_ExtrasD_extraKillCooldownOverride", false);
                extraKillCooldownSeconds = PlayerPrefs.GetFloat("M_ExtrasD_extraKillCooldownSeconds", 10f);

                extraAutoEndGame = LoadBool("M_ExtrasD_extraAutoEndGame", false);
                extraAutoEndMinutes = PlayerPrefs.GetInt("M_ExtrasD_extraAutoEndMinutes", 5);

                extraKickNoFriendCode = LoadBool("M_ExtrasD_extraKickNoFriendCode", false);

                extraMuteAllToggle = LoadBool("M_ExtrasD_extraMuteAllToggle", false);

                extraHostAnnounce = LoadBool("M_ExtrasD_extraHostAnnounce", false);
                extraHostAnnounceText = PlayerPrefs.GetString("M_ExtrasD_extraHostAnnounceText", string.Empty);

                extraAfkKick = LoadBool("M_ExtrasD_extraAfkKick", false);
                extraAfkKickSeconds = Mathf.Clamp(PlayerPrefs.GetInt("M_ExtrasD_extraAfkKickSeconds", 120), 30, 600);
            }
            catch { }
        }

        internal static void SaveExtrasD()
        {
            try
            {
                SaveBool("M_ExtrasD_extraFreezePlayers", extraFreezePlayers);

                SaveBool("M_ExtrasD_extraKillCooldownOverride", extraKillCooldownOverride);
                PlayerPrefs.SetFloat("M_ExtrasD_extraKillCooldownSeconds", extraKillCooldownSeconds);

                SaveBool("M_ExtrasD_extraAutoEndGame", extraAutoEndGame);
                PlayerPrefs.SetInt("M_ExtrasD_extraAutoEndMinutes", extraAutoEndMinutes);

                SaveBool("M_ExtrasD_extraKickNoFriendCode", extraKickNoFriendCode);

                SaveBool("M_ExtrasD_extraMuteAllToggle", extraMuteAllToggle);

                SaveBool("M_ExtrasD_extraHostAnnounce", extraHostAnnounce);
                PlayerPrefs.SetString("M_ExtrasD_extraHostAnnounceText", extraHostAnnounceText ?? string.Empty);

                SaveBool("M_ExtrasD_extraAfkKick", extraAfkKick);
                PlayerPrefs.SetInt("M_ExtrasD_extraAfkKickSeconds", Mathf.Clamp(extraAfkKickSeconds, 30, 600));
            }
            catch { }
        }

        internal static void DrawExtrasDOverlay()
        {
            // No overlay elements in this group.
        }

        // --------------------------------------------------------------------
        // Feature ticks
        // --------------------------------------------------------------------

        private static void TickFreezeD()
        {
            if (!extraFreezePlayers || !IsHostD()) return;

            float now = Time.unscaledTime;
            if (now < nextFreezeD) return;
            nextFreezeD = now + 0.1f;

            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null) return;

                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;

                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc == local) continue;
                        if (pc.Data == null || pc.Data.IsDead) continue;

                        object netTransform = pc.NetTransform;
                        if (netTransform == null) continue;

                        MethodInfo snap = FindSnapMethodD(netTransform.GetType());
                        if (snap == null) continue;

                        Vector3 pos = pc.transform.position;
                        object vec = new Vector2(pos.x, pos.y);
                        ParameterInfo[] ps = snap.GetParameters();

                        if (ps.Length == 1)
                            snap.Invoke(netTransform, new object[] { vec });
                        else if (ps.Length == 2 && ps[1].ParameterType == typeof(int))
                            snap.Invoke(netTransform, new object[] { vec, 0 });
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TickKillCooldownD()
        {
            if (!extraKillCooldownOverride || !IsHostD()) return;

            float now = Time.unscaledTime;
            if (now < nextKillCooldownD) return;
            nextKillCooldownD = now + 0.2f;

            float seconds = Mathf.Clamp(extraKillCooldownSeconds, 0f, 600f);

            try
            {
                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;

                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc.Data == null) continue;
                        if (!IsImpostorTeamD(pc)) continue;

                        FieldInfo timer = FindKillTimerFieldD(pc.GetType());
                        if (timer == null || timer.FieldType != typeof(float)) continue;

                        timer.SetValue(pc, seconds);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TickAutoEndD()
        {
            if (!extraAutoEndGame || !IsHostD()) return;

            try
            {
                int gameId = GetGameIdD();
                bool started = IsGameStartedD();

                if (gameId != autoEndGameIdD)
                {
                    autoEndGameIdD = gameId;
                    autoEndStartD = started ? Time.unscaledTime : 0f;
                    autoEndFiredD = false;
                }

                if (!started)
                {
                    autoEndStartD = 0f;
                    autoEndFiredD = false;
                    return;
                }

                if (autoEndStartD <= 0f) autoEndStartD = Time.unscaledTime;
                if (autoEndFiredD) return;

                float limit = Mathf.Max(0f, extraAutoEndMinutes) * 60f;
                if (Time.unscaledTime - autoEndStartD < limit) return;

                autoEndFiredD = true;
                InvokeRpcEndGameD();
            }
            catch { }
        }

        private static void TickKickNoFriendCodeD()
        {
            if (!extraKickNoFriendCode || !IsHostD()) return;

            try
            {
                int gameId = GetGameIdD();
                if (gameId != kickGameIdD)
                {
                    kickGameIdD = gameId;
                    noFcFirstSeenD.Clear();
                    noFcProcessedOwnersD.Clear();
                }

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null) return;

                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;

                float now = Time.unscaledTime;

                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc == local || pc.Data == null) continue;

                        object bot = GetValueD(pc.Data, "Bot");
                        if (bot is bool isBot && isBot) continue;

                        int owner = Convert.ToInt32(pc.OwnerId);
                        if (owner < 0) continue;
                        if (noFcProcessedOwnersD.Contains(owner)) continue;

                        string friendCode = GetFriendCodeD(pc);
                        if (!string.IsNullOrWhiteSpace(friendCode))
                        {
                            noFcFirstSeenD.Remove(owner);
                            continue;
                        }

                        if (!noFcFirstSeenD.TryGetValue(owner, out float firstSeen))
                        {
                            firstSeen = now;
                            noFcFirstSeenD[owner] = firstSeen;
                        }

                        if (now - firstSeen < 2f) continue;

                        noFcProcessedOwnersD.Add(owner);
                        AmongUsClient.Instance.KickPlayer(owner, false);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TickMuteAllD()
        {
            if (!IsHostD()) return;

            try
            {
                int gameId = GetGameIdD();
                if (gameId != muteGameIdD)
                {
                    muteGameIdD = gameId;
                    muteAllAppliedD = false;
                    muteAllPrevD = false;
                }

                if (!extraMuteAllToggle)
                {
                    muteAllAppliedD = false;
                    muteAllPrevD = false;
                    return;
                }

                bool toggleChanged = muteAllPrevD != extraMuteAllToggle;
                if (muteAllAppliedD && !toggleChanged) return;

                muteAllPrevD = extraMuteAllToggle;
                muteAllAppliedD = true;

                PlayerControl local = PlayerControl.LocalPlayer;
                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;

                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc == local) continue;
                        TrySetMutedD(pc, true);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TickAfkKickD()
        {
            if (!extraAfkKick || !IsHostD()) return;

            float now = Time.unscaledTime;
            if (now < nextAfkCheckD) return;
            nextAfkCheckD = now + 0.5f;

            try
            {
                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;

                int gameId = GetGameIdD();
                if (gameId != afkGameIdD)
                {
                    afkGameIdD = gameId;
                    afkLastPosD.Clear();
                    afkLastMoveD.Clear();
                    afkKickedOwnersD.Clear();
                }

                // A meeting holds everyone in place; do not treat that as idling.
                if (MeetingHud.Instance != null)
                {
                    foreach (PlayerControl pc in all)
                    {
                        try
                        {
                            if (pc == null || pc.Data == null) continue;
                            int owner = Convert.ToInt32(pc.OwnerId);
                            if (owner >= 0) afkLastMoveD[owner] = now;
                        }
                        catch { }
                    }
                    return;
                }

                if (!IsGameStartedD()) return;

                PlayerControl local = PlayerControl.LocalPlayer;
                float timeout = Mathf.Clamp(extraAfkKickSeconds, 30, 600);

                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc == local || pc.Data == null) continue;
                        if (pc.Data.Disconnected || pc.Data.IsDead) continue;

                        object bot = GetValueD(pc.Data, "Bot");
                        if (bot is bool isBot && isBot) continue;

                        int owner = Convert.ToInt32(pc.OwnerId);
                        if (owner < 0 || afkKickedOwnersD.Contains(owner)) continue;

                        Vector3 pos = pc.transform.position;
                        if (!afkLastPosD.TryGetValue(owner, out Vector3 last))
                        {
                            afkLastPosD[owner] = pos;
                            afkLastMoveD[owner] = now;
                            continue;
                        }

                        if ((pos - last).sqrMagnitude > 0.04f)
                        {
                            afkLastPosD[owner] = pos;
                            afkLastMoveD[owner] = now;
                            continue;
                        }

                        if (!afkLastMoveD.TryGetValue(owner, out float moved))
                        {
                            afkLastMoveD[owner] = now;
                            continue;
                        }

                        if (now - moved < timeout) continue;

                        afkKickedOwnersD.Add(owner);
                        AmongUsClient.Instance.KickPlayer(owner, false);
                        ShowNotification("<color=#FFAC1C>[AFK]</color> " + (pc.Data.PlayerName ?? "player"));
                    }
                    catch { }
                }
            }
            catch { }
        }

        // --------------------------------------------------------------------
        // Host announcement (button tools)
        // --------------------------------------------------------------------

        private static void SendHostAnnouncementD(string message)
        {
            if (!IsHostD()) return;
            if (string.IsNullOrWhiteSpace(message)) return;

            string text = message.Trim();
            if (text.Length > 90) text = text.Substring(0, 90);

            SendChatAsHostD("[HOST] " + text);
            ShowNotification(L("<color=#7CFC98>[HOST]</color> Announcement sent.",
                "<color=#7CFC98>[HOST]</color> Объявление отправлено."));
        }

        private static void PostRoomCodeD()
        {
            if (!IsHostD()) return;
            try
            {
                string code = GetCurrentRoomCodeForStatus();
                if (string.IsNullOrWhiteSpace(code)) return;
                if (string.Equals(code, "No room", StringComparison.OrdinalIgnoreCase)) return;

                SendChatAsHostD("[HOST] " + L("Room code:", "Код комнаты:") + " " + code);
            }
            catch { }
        }

        private static void SendChatAsHostD(string text)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || string.IsNullOrEmpty(text)) return;
                local.RpcSendChat(text);
            }
            catch { }
        }

        // --------------------------------------------------------------------
        // Reflection helpers
        // --------------------------------------------------------------------

        private static bool IsHostD()
        {
            try { return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost; }
            catch { return false; }
        }

        private static void ResetD()
        {
            try
            {
                nextFreezeD = 0f;
                nextKillCooldownD = 0f;
                autoEndGameIdD = -1;
                autoEndStartD = 0f;
                autoEndFiredD = false;
                noFcFirstSeenD.Clear();
                noFcProcessedOwnersD.Clear();
                kickGameIdD = -1;
                muteGameIdD = -1;
                muteAllAppliedD = false;
                muteAllPrevD = false;
                nextAfkCheckD = 0f;
                afkGameIdD = -1;
                afkLastPosD.Clear();
                afkLastMoveD.Clear();
                afkKickedOwnersD.Clear();
            }
            catch { }
        }

        private static int GetGameIdD()
        {
            try
            {
                object value = GetValueD(AmongUsClient.Instance, "GameId");
                return value == null ? -1 : Convert.ToInt32(value);
            }
            catch { return -1; }
        }

        private static bool IsGameStartedD()
        {
            try
            {
                object value = GetValueD(AmongUsClient.Instance, "IsGameStarted");
                if (value is bool started) return started;
            }
            catch { }
            try { return ShipStatus.Instance != null; }
            catch { return false; }
        }

        private static object GetValueD(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                for (Type cur = instance.GetType(); cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        PropertyInfo prop = cur.GetProperty(name, AllMembersD);
                        if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                            return prop.GetValue(instance, null);
                    }
                    catch { }
                    try
                    {
                        FieldInfo field = cur.GetField(name, AllMembersD);
                        if (field != null) return field.GetValue(instance);
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static FieldInfo FindFieldD(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        FieldInfo field = cur.GetField(name, AllMembersD);
                        if (field != null) return field;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static MethodInfo FindMethodD(Type type, string name, int parameterCount, Type firstParameterType)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        foreach (MethodInfo method in cur.GetMethods(AllMembersD))
                        {
                            if (method.Name != name) continue;
                            ParameterInfo[] ps = method.GetParameters();
                            if (ps.Length != parameterCount) continue;
                            if (firstParameterType != null && (ps.Length == 0 || ps[0].ParameterType != firstParameterType)) continue;
                            return method;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static MethodInfo FindSnapMethodD(Type type)
        {
            if (type == null) return null;
            if (cachedSnapTypeD == type) return cachedSnapMethodD;

            MethodInfo fallback = null;
            try
            {
                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        foreach (MethodInfo method in cur.GetMethods(AllMembersD))
                        {
                            if (method.Name != "RpcSnapTo") continue;
                            ParameterInfo[] ps = method.GetParameters();
                            if (ps.Length < 1 || ps[0].ParameterType != typeof(Vector2)) continue;

                            if (ps.Length == 1)
                            {
                                cachedSnapTypeD = type;
                                cachedSnapMethodD = method;
                                return method;
                            }
                            if (ps.Length == 2 && ps[1].ParameterType == typeof(int) && fallback == null)
                                fallback = method;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            cachedSnapTypeD = type;
            cachedSnapMethodD = fallback;
            return fallback;
        }

        private static FieldInfo FindKillTimerFieldD(Type type)
        {
            if (type == null) return null;
            if (cachedKillTypeD == type) return cachedKillFieldD;

            cachedKillTypeD = type;
            cachedKillFieldD = FindFieldD(type, "killTimer");
            return cachedKillFieldD;
        }

        private static bool IsImpostorTeamD(PlayerControl player)
        {
            try
            {
                if (player == null || player.Data == null) return false;

                object role = GetValueD(player.Data, "Role");
                if (role != null)
                {
                    object isImpostor = GetValueD(role, "IsImpostor");
                    if (isImpostor is bool imp && imp) return true;
                    object canKill = GetValueD(role, "CanUseKillButton");
                    if (canKill is bool kill && kill) return true;
                }
                return false;
            }
            catch { return false; }
        }

        private static string GetFriendCodeD(PlayerControl player)
        {
            if (player == null) return string.Empty;
            try
            {
                string fc = GetValueD(player.Data, "FriendCode") as string;
                if (!string.IsNullOrWhiteSpace(fc)) return fc;

                fc = GetValueD(player, "FriendCode") as string;
                if (!string.IsNullOrWhiteSpace(fc)) return fc;

                try
                {
                    MethodInfo getClient = FindMethodD(typeof(AmongUsClient), "GetClientFromCharacter", 1, typeof(PlayerControl));
                    if (getClient != null && getClient.IsStatic)
                    {
                        object client = getClient.Invoke(null, new object[] { player });
                        fc = GetValueD(client, "FriendCode") as string;
                        if (!string.IsNullOrWhiteSpace(fc)) return fc;
                    }
                }
                catch { }
            }
            catch { }
            return string.Empty;
        }

        private static void InvokeRpcEndGameD()
        {
            try
            {
                GameManager manager = GameManager.Instance;
                if (manager == null) return;

                MethodInfo end = null;
                try
                {
                    for (Type cur = manager.GetType(); cur != null && end == null; cur = cur.BaseType)
                    {
                        try
                        {
                            foreach (MethodInfo method in cur.GetMethods(AllMembersD))
                            {
                                if (method.Name != "RpcEndGame") continue;
                                ParameterInfo[] ps = method.GetParameters();
                                if (ps.Length == 2 && ps[0].ParameterType.IsEnum && ps[1].ParameterType == typeof(bool))
                                {
                                    end = method;
                                    break;
                                }
                                if (ps.Length == 1 && ps[0].ParameterType.IsEnum) end = method;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                if (end == null) return;

                ParameterInfo[] par = end.GetParameters();
                object[] args = new object[par.Length];
                for (int i = 0; i < par.Length; i++)
                {
                    Type pt = par[i].ParameterType;
                    if (pt.IsEnum) args[i] = Enum.ToObject(pt, 3);
                    else if (pt == typeof(bool)) args[i] = false;
                    else if (pt == typeof(int)) args[i] = 0;
                    else if (par[i].IsOptional) args[i] = Type.Missing;
                    else return;
                }

                bool previous = neverEndGame;
                neverEndGame = false;
                try { end.Invoke(manager, args); }
                finally { neverEndGame = previous; }
            }
            catch { }
        }

        private static bool TrySetMutedD(PlayerControl player, bool muted)
        {
            if (player == null) return false;

            try
            {
                object[] targets = { player, player.Data };
                foreach (object target in targets)
                {
                    if (target == null) continue;
                    Type t = target.GetType();

                    foreach (string methodName in new[] { "RpcSetMuted", "SetMuted", "RpcSetMute", "SetMute" })
                    {
                        try
                        {
                            MethodInfo method = FindMethodD(t, methodName, 1, typeof(bool));
                            if (method != null)
                            {
                                method.Invoke(target, new object[] { muted });
                                return true;
                            }
                        }
                        catch { }
                    }

                    foreach (string memberName in new[] { "IsMuted", "isMuted", "Muted", "muted" })
                    {
                        try
                        {
                            for (Type cur = t; cur != null; cur = cur.BaseType)
                            {
                                PropertyInfo prop = cur.GetProperty(memberName, AllMembersD);
                                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
                                {
                                    prop.SetValue(target, muted, null);
                                    return true;
                                }
                            }
                        }
                        catch { }
                        try
                        {
                            FieldInfo field = FindFieldD(t, memberName);
                            if (field != null && field.FieldType == typeof(bool))
                            {
                                field.SetValue(target, muted);
                                return true;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
