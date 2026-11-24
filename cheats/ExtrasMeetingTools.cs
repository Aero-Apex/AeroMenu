#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using InnerNet;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Group H state (exact field names required by the contract)
        // --------------------------------------------------------------------
        public static bool extraEjectionCountdown;
        public static bool extraAutoSkipVote;
        public static bool extraAutoCloseMeeting;
        public static int extraAutoCloseSeconds;
        public static bool extraAutoReportBody;
        public static bool extraMeetingCam;

        // --------------------------------------------------------------------
        // Private working state (all suffixed with H)
        // --------------------------------------------------------------------
        private const BindingFlags FlagsH = BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static;

        private const float SkipIntervalH = 0.5f;
        private const float ReportScanIntervalH = 0.4f;
        private const float ReportRadiusH = 2.6f;
        private const float MeetingCamSpeedH = 8f;
        private const byte SkippedVoteFallbackH = 252;
        private const byte NoVoteValueH = 255;

        // Countdown overlay (36)
        private static FieldInfo countdownFieldH;
        private static Type countdownOwnerH;
        private static GUIStyle countdownStyleH;
        private static GUIStyle countdownBgStyleH;
        private static Texture2D countdownBgTexH;

        // Auto skip (37)
        private static MeetingHud skipLastHudH;
        private static float nextSkipAtH;

        // Auto close (38)
        private static MeetingHud closeLastHudH;
        private static float closeStartAtH;
        private static bool closeIssuedH;

        // Auto report (39)
        private static float nextReportScanH;
        private static UnityEngine.Object lastReportedBodyH;

        // Meeting cam (40)
        private static bool camCapturedH;
        private static Vector3 camSavedPosH;

        // UI text buffer for the auto-close delay.
        private static string autoCloseTextH = "30";

        // ====================================================================
        // Public surface
        // ====================================================================

        internal void DrawExtrasH(float width)
        {
            int toggleW = Mathf.Clamp(Mathf.RoundToInt(width > 0f ? width : 260f), 170, 460);
            GUIStyle fieldStyle = inputBlockStyle ?? GUI.skin.textField;

            try
            {
                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    DrawMenuSectionHeader(L("MEETING TOOLS", "ИНСТРУМЕНТЫ СОБРАНИЯ"));

                    // 36 - Ejection / meeting countdown -----------------------
                    bool countdown = DrawToggle(extraEjectionCountdown,
                        L("Ejection Countdown", "Таймер изгнания"), toggleW, "extrasEjectionCountdown");
                    if (countdown != extraEjectionCountdown) extraEjectionCountdown = countdown;
                    GUILayout.Label(L("Shows the meeting / exile countdown read from the game state via reflection.",
                                      "Показывает таймер собрания / изгнания, считанный из состояния игры через рефлексию."), menuDescStyle);
                    GUILayout.Space(6);

                    // 37 - Auto skip vote -------------------------------------
                    bool autoSkip = DrawToggle(extraAutoSkipVote,
                        L("Auto Skip Vote", "Авто-пропуск голоса"), toggleW, "extrasAutoSkipVote");
                    if (autoSkip != extraAutoSkipVote) extraAutoSkipVote = autoSkip;
                    GUILayout.Label(L("Automatically presses Skip Vote during a meeting. Your own manual votes are left alone.",
                                      "Автоматически нажимает «Пропустить голос» во время собрания. Ваши ручные голоса не затрагиваются."), menuDescStyle);
                    GUILayout.Space(6);

                    // 38 - Auto close meeting ---------------------------------
                    bool autoClose = DrawToggle(extraAutoCloseMeeting,
                        L("Auto Close Meeting (Host)", "Авто-закрытие собрания (хост)"), toggleW, "extrasAutoCloseMeeting");
                    if (autoClose != extraAutoCloseMeeting) extraAutoCloseMeeting = autoClose;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(L("Close after (s):", "Закрыть через (с):"), GUILayout.Width(Mathf.Min(148f, toggleW * 0.62f)));
                    string typed = AeroTextField(autoCloseTextH, fieldStyle, GUILayout.Width(56));
                    if (!string.Equals(typed, autoCloseTextH, StringComparison.Ordinal))
                    {
                        autoCloseTextH = typed ?? string.Empty;
                        int parsed;
                        if (int.TryParse(autoCloseTextH, out parsed))
                            extraAutoCloseSeconds = Mathf.Clamp(parsed, 1, 120);
                    }
                    GUILayout.Label(extraAutoCloseSeconds + "s", GUILayout.Width(36));
                    GUILayout.EndHorizontal();
                    GUILayout.Label(L("Host only. Ends an active meeting after the configured delay (1-120 s).",
                                      "Только для хоста. Завершает активное собрание через заданную задержку (1-120 с)."), menuDescStyle);
                    GUILayout.Space(6);

                    // 39 - Auto report body -----------------------------------
                    bool autoReport = DrawToggle(extraAutoReportBody,
                        L("Auto Report Body", "Авто-сообщение о теле"), toggleW, "extrasAutoReportBody");
                    if (autoReport != extraAutoReportBody) extraAutoReportBody = autoReport;
                    GUILayout.Label(L("Reports the nearest dead body when you walk close enough to it.",
                                      "Сообщает о ближайшем мёртвом теле, когда вы подходите достаточно близко."), menuDescStyle);
                    GUILayout.Space(6);

                    // 40 - Meeting camera -------------------------------------
                    bool meetingCam = DrawToggle(extraMeetingCam,
                        L("Meeting Camera (WASD)", "Камера на собрании (WASD)"), toggleW, "extrasMeetingCam");
                    if (meetingCam != extraMeetingCam) extraMeetingCam = meetingCam;
                    GUILayout.Label(L("Move the camera with WASD during meetings. The original position is restored afterwards.",
                                      "Двигайте камеру клавишами WASD во время собрания. Исходная позиция восстанавливается после его окончания."), menuDescStyle);
                    GUILayout.Space(6);
                    DrawMeetingQuickTools(width);
                }
                finally
                {
                    GUILayout.EndVertical();
                }
            }
            catch { }
        }

        internal static void TickExtrasH(bool inputBlocked)
        {
            try
            {
                TickAutoSkipH();
                TickAutoCloseH();
                TickAutoReportH();
                TickMeetingCamH(inputBlocked);
            }
            catch { }
        }

        internal static void LoadExtrasH()
        {
            try
            {
                extraEjectionCountdown = LoadBool("M_ExtrasH_extraEjectionCountdown", false);
                extraAutoSkipVote = LoadBool("M_ExtrasH_extraAutoSkipVote", false);
                extraAutoCloseMeeting = LoadBool("M_ExtrasH_extraAutoCloseMeeting", false);
                extraAutoCloseSeconds = Mathf.Clamp(PlayerPrefs.GetInt("M_ExtrasH_extraAutoCloseSeconds", 30), 1, 120);
                autoCloseTextH = extraAutoCloseSeconds.ToString();
                extraAutoReportBody = LoadBool("M_ExtrasH_extraAutoReportBody", false);
                extraMeetingCam = LoadBool("M_ExtrasH_extraMeetingCam", false);
                LoadMeetingQuickTools();

                // Clear runtime caches so Tick re-evaluates from a clean slate.
                countdownFieldH = null;
                countdownOwnerH = null;
                skipLastHudH = null;
                nextSkipAtH = 0f;
                closeLastHudH = null;
                closeStartAtH = 0f;
                closeIssuedH = false;
                nextReportScanH = 0f;
                lastReportedBodyH = null;
                RestoreMeetingCamH();
            }
            catch { }
        }

        internal static void SaveExtrasH()
        {
            try
            {
                SaveBool("M_ExtrasH_extraEjectionCountdown", extraEjectionCountdown);
                SaveBool("M_ExtrasH_extraAutoSkipVote", extraAutoSkipVote);
                SaveBool("M_ExtrasH_extraAutoCloseMeeting", extraAutoCloseMeeting);
                PlayerPrefs.SetInt("M_ExtrasH_extraAutoCloseSeconds", Mathf.Clamp(extraAutoCloseSeconds, 1, 120));
                SaveBool("M_ExtrasH_extraAutoReportBody", extraAutoReportBody);
                SaveBool("M_ExtrasH_extraMeetingCam", extraMeetingCam);
                SaveMeetingQuickTools();
            }
            catch { }
        }

        internal static void DrawExtrasHOverlay()
        {
            // Quick tools manage their own on/off state and meeting checks.
            try { DrawMeetingQuickToolsOverlay(); } catch { }

            if (!extraEjectionCountdown) return;

            try
            {
                if (Event.current == null || Event.current.type != EventType.Repaint) return;

                float seconds;
                if (!TryGetCountdownSecondsH(out seconds)) return;

                if (countdownStyleH == null || countdownBgStyleH == null)
                    CreateCountdownStylesH();

                string phase = MeetingHud.Instance != null ? "MEETING" : "EJECTION";
                string text = phase + "  " + seconds.ToString("0.0") + "s";

                const float w = 260f;
                const float h = 54f;
                Rect r = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.08f, w, h);

                if (countdownBgStyleH != null) GUI.Box(r, GUIContent.none, countdownBgStyleH);
                if (countdownStyleH != null) GUI.Label(r, text, countdownStyleH);
            }
            catch { }
        }

        // ====================================================================
        // Feature 36 - Ejection / meeting countdown overlay
        // ====================================================================

        private static void CreateCountdownStylesH()
        {
            try
            {
                if (countdownBgTexH == null)
                {
                    countdownBgTexH = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    countdownBgTexH.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.62f));
                    countdownBgTexH.Apply();
                    countdownBgTexH.hideFlags = HideFlags.HideAndDontSave;
                }

                if (countdownBgStyleH == null)
                {
                    countdownBgStyleH = new GUIStyle(GUIStyle.none);
                    countdownBgStyleH.normal.background = countdownBgTexH;
                }

                if (countdownStyleH == null)
                {
                    countdownStyleH = new GUIStyle(GUI.skin.label);
                    countdownStyleH.fontSize = 24;
                    countdownStyleH.fontStyle = FontStyle.Bold;
                    countdownStyleH.alignment = TextAnchor.MiddleCenter;
                    countdownStyleH.normal.textColor = new Color(1f, 0.93f, 0.55f, 1f);
                }
            }
            catch { }
        }

        private static bool TryGetCountdownSecondsH(out float seconds)
        {
            seconds = 0f;
            try
            {
                object target;
                string[] names;

                MeetingHud hud = MeetingHud.Instance;
                if (hud != null)
                {
                    target = hud;
                    names = new string[]
                    {
                        "discussionTimer", "discussionTime", "timer", "Timer",
                        "timeLeft", "stateTimer", "votingTimer"
                    };
                }
                else
                {
                    ExileController exile = ExileController.Instance;
                    if (exile == null) return false;
                    target = exile;
                    names = new string[]
                    {
                        "textTimer", "timer", "Timer", "timeLeft", "discussionTimer"
                    };
                }

                Type type = target.GetType();
                if (countdownFieldH == null || countdownOwnerH != type)
                {
                    countdownFieldH = FindFirstFieldH(type, names);
                    countdownOwnerH = type;
                }

                if (countdownFieldH == null) return false;

                object value = countdownFieldH.GetValue(target);
                if (value == null) return false;

                if (value is float f) seconds = f;
                else if (value is int i) seconds = i;
                else if (value is double d) seconds = (float)d;
                else if (!float.TryParse(value.ToString(), out seconds)) return false;

                if (seconds < 0f) seconds = 0f;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static FieldInfo FindFirstFieldH(Type type, string[] names)
        {
            try
            {
                if (type == null || names == null) return null;
                for (int i = 0; i < names.Length; i++)
                {
                    FieldInfo field = type.GetField(names[i], FlagsH);
                    if (field != null) return field;
                }

                // Fallback: any float-like field that looks like a timer.
                FieldInfo[] fields = type.GetFields(FlagsH);
                if (fields == null) return null;
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field == null) continue;
                    string name = field.Name ?? string.Empty;
                    if (name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) < 0 &&
                        name.IndexOf("countdown", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (field.FieldType == typeof(float) || field.FieldType == typeof(int) ||
                        field.FieldType == typeof(double))
                        return field;
                }
            }
            catch { }
            return null;
        }

        // ====================================================================
        // Feature 37 - Auto skip vote
        // ====================================================================

        private static void TickAutoSkipH()
        {
            if (!extraAutoSkipVote)
            {
                skipLastHudH = null;
                return;
            }

            try
            {
                MeetingHud hud = MeetingHud.Instance;
                if (hud == null)
                {
                    skipLastHudH = null;
                    return;
                }

                if (hud != skipLastHudH)
                {
                    skipLastHudH = hud;
                    nextSkipAtH = Time.unscaledTime + SkipIntervalH;
                }

                float now = Time.unscaledTime;
                if (now < nextSkipAtH) return;
                nextSkipAtH = now + SkipIntervalH;

                // Never interfere once the local player has cast a vote.
                if (HasLocalPlayerVotedH(hud)) return;

                TryInvokeAutoSkipH(hud);
            }
            catch { }
        }

        private static bool HasLocalPlayerVotedH(MeetingHud hud)
        {
            // Conservative: when in doubt, report "voted" so we do nothing.
            try
            {
                if (hud == null) return true;
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null) return true;

                byte localId = local.PlayerId;
                FieldInfo statesField = hud.GetType().GetField("playerStates", FlagsH);
                object statesValue = statesField == null ? null : statesField.GetValue(hud);
                System.Collections.IEnumerable states = statesValue as System.Collections.IEnumerable;
                if (states == null) return true;

                foreach (object state in states)
                {
                    if (state == null) continue;
                    Type stateType = state.GetType();

                    object idValue = GetFirstValueH(state, stateType, new string[] { "PlayerId", "playerId" });
                    if (idValue == null) continue;
                    byte id;
                    try { id = Convert.ToByte(idValue); } catch { continue; }
                    if (id != localId) continue;

                    object voteFor = GetFirstValueH(state, stateType, new string[] { "VotedForId", "votedForId", "VoteFor", "voteFor" });
                    if (voteFor != null)
                    {
                        byte vote;
                        try { vote = Convert.ToByte(voteFor); } catch { return true; }
                        return vote != NoVoteValueH;
                    }

                    object hasVoted = GetFirstValueH(state, stateType, new string[] { "DidVote", "didVote", "HasVoted", "hasVoted", "Voted" });
                    if (hasVoted is bool voted) return voted;

                    return true;
                }
            }
            catch { }
            return true;
        }

        private static object GetFirstValueH(object target, Type type, string[] names)
        {
            try
            {
                if (target == null || type == null || names == null) return null;
                for (int i = 0; i < names.Length; i++)
                {
                    FieldInfo field = type.GetField(names[i], FlagsH);
                    if (field != null) return field.GetValue(target);

                    PropertyInfo property = type.GetProperty(names[i], FlagsH);
                    if (property != null) return property.GetValue(target, null);
                }
            }
            catch { }
            return null;
        }

        private static bool TryInvokeAutoSkipH(MeetingHud hud)
        {
            if (hud == null) return false;
            try
            {
                Type type = hud.GetType();

                // 1) A direct no-argument skip method on MeetingHud.
                string[] methodNames = { "SkipVote", "VoteSkip", "SkipVoting", "OnSkipVote" };
                for (int i = 0; i < methodNames.Length; i++)
                {
                    try
                    {
                        MethodInfo method = type.GetMethod(methodNames[i], FlagsH);
                        if (method == null || method.GetParameters().Length != 0) continue;
                        method.Invoke(hud, null);
                        return true;
                    }
                    catch { }
                }

                // 2) A vote-state helper that accepts the skip id.
                string[] idMethodNames = { "VoteState", "CastVote", "SendVote" };
                for (int i = 0; i < idMethodNames.Length; i++)
                {
                    try
                    {
                        MethodInfo method = type.GetMethod(idMethodNames[i], FlagsH);
                        if (method == null) continue;
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length != 1) continue;
                        if (parameters[0].ParameterType != typeof(byte) &&
                            parameters[0].ParameterType != typeof(int))
                            continue;
                        object skipId = parameters[0].ParameterType == typeof(byte)
                            ? (object)GetSkippedVoteIdH()
                            : (object)((int)GetSkippedVoteIdH());
                        method.Invoke(hud, new object[] { skipId });
                        return true;
                    }
                    catch { }
                }

                // 3) The Skip Vote button (field) and its click callback.
                string[] fieldNames = { "ButtonSkip", "buttonSkip", "SkipButton", "skipButton" };
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    try
                    {
                        FieldInfo field = type.GetField(fieldNames[i], FlagsH);
                        if (field == null) continue;
                        object button = field.GetValue(hud);
                        if (button == null) continue;
                        if (TryInvokeOnClickH(button)) return true;
                    }
                    catch { }
                }

                // 4) Fallback: reproduce the skip via the vote state / cast RPC.
                return TryCastSkipVoteH(hud);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeOnClickH(object button)
        {
            try
            {
                if (button == null) return false;
                Type type = button.GetType();

                string[] eventFields = { "onClick", "m_OnClick", "OnClick" };
                for (int i = 0; i < eventFields.Length; i++)
                {
                    try
                    {
                        FieldInfo field = type.GetField(eventFields[i], FlagsH);
                        if (field == null) continue;
                        object unityEvent = field.GetValue(button);
                        if (unityEvent == null) continue;
                        MethodInfo invoke = unityEvent.GetType().GetMethod("Invoke", FlagsH);
                        if (invoke == null || invoke.GetParameters().Length != 0) continue;
                        invoke.Invoke(unityEvent, null);
                        return true;
                    }
                    catch { }
                }

                string[] clickMethods = { "OnClick", "onClick", "ReceiveClick", "Click" };
                for (int i = 0; i < clickMethods.Length; i++)
                {
                    try
                    {
                        MethodInfo method = type.GetMethod(clickMethods[i], FlagsH);
                        if (method == null || method.GetParameters().Length != 0) continue;
                        method.Invoke(button, null);
                        return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private static bool TryCastSkipVoteH(MeetingHud hud)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null) return false;

                byte localId = local.PlayerId;
                byte skipId = GetSkippedVoteIdH();

                object localState = null;
                FieldInfo statesField = hud.GetType().GetField("playerStates", FlagsH);
                object statesValue = statesField == null ? null : statesField.GetValue(hud);
                System.Collections.IEnumerable states = statesValue as System.Collections.IEnumerable;
                if (states != null)
                {
                    foreach (object state in states)
                    {
                        if (state == null) continue;
                        object idValue = GetFirstValueH(state, state.GetType(), new string[] { "PlayerId", "playerId" });
                        if (idValue == null) continue;
                        byte id;
                        try { id = Convert.ToByte(idValue); } catch { continue; }
                        if (id == localId) { localState = state; break; }
                    }
                }

                if (localState != null)
                {
                    Type stateType = localState.GetType();
                    try
                    {
                        MethodInfo setVote = stateType.GetMethod("SetVote", FlagsH);
                        if (setVote != null && setVote.GetParameters().Length == 1)
                            setVote.Invoke(localState, new object[] { skipId });
                    }
                    catch { }
                    try
                    {
                        MethodInfo setHasVoted = stateType.GetMethod("SetHasVoted", FlagsH);
                        if (setHasVoted != null && setHasVoted.GetParameters().Length == 0)
                            setHasVoted.Invoke(localState, null);
                    }
                    catch { }
                }

                try
                {
                    MethodInfo mark = hud.GetType().GetMethod("MarkPlayerAsHasVoted", FlagsH);
                    if (mark != null && mark.GetParameters().Length == 1)
                        mark.Invoke(hud, new object[] { localId });
                }
                catch { }

                MethodInfo cast = hud.GetType().GetMethod("CmdCastVote", FlagsH);
                if (cast == null || cast.GetParameters().Length != 2) return false;
                cast.Invoke(hud, new object[] { localId, skipId });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte GetSkippedVoteIdH()
        {
            try
            {
                FieldInfo field = typeof(PlayerVoteArea).GetField("SkippedVote", FlagsH);
                if (field != null)
                {
                    object value = field.GetValue(null);
                    if (value != null) return Convert.ToByte(value);
                }
            }
            catch { }
            return SkippedVoteFallbackH;
        }

        // ====================================================================
        // Feature 38 - Auto close meeting (host only)
        // ====================================================================

        private static void TickAutoCloseH()
        {
            if (!extraAutoCloseMeeting)
            {
                closeLastHudH = null;
                closeIssuedH = false;
                return;
            }

            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

                MeetingHud hud = MeetingHud.Instance;
                if (hud == null)
                {
                    closeLastHudH = null;
                    closeIssuedH = false;
                    return;
                }

                if (hud != closeLastHudH)
                {
                    closeLastHudH = hud;
                    closeStartAtH = Time.unscaledTime;
                    closeIssuedH = false;
                }

                if (closeIssuedH) return;

                float delay = Mathf.Max(1f, (float)extraAutoCloseSeconds);
                if (Time.unscaledTime - closeStartAtH < delay) return;

                closeIssuedH = true;
                try { hud.RpcClose(); } catch { }
            }
            catch { }
        }

        // ====================================================================
        // Feature 39 - Auto report nearby dead body
        // ====================================================================

        private static void TickAutoReportH()
        {
            if (!extraAutoReportBody)
            {
                lastReportedBodyH = null;
                return;
            }

            try
            {
                float now = Time.unscaledTime;
                if (now < nextReportScanH) return;
                nextReportScanH = now + ReportScanIntervalH;

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null || local.Data.IsDead) return;
                if (MeetingHud.Instance != null || ExileController.Instance != null) return;

                MonoBehaviour[] all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                if (all == null) return;

                Vector3 position = local.transform.position;
                float bestDistance = ReportRadiusH * ReportRadiusH;
                MonoBehaviour nearest = null;

                for (int i = 0; i < all.Length; i++)
                {
                    MonoBehaviour mb = all[i];
                    if (mb == null) continue;
                    Type type;
                    try { type = mb.GetType(); } catch { continue; }
                    if (type == null || !string.Equals(type.Name, "DeadBody", StringComparison.Ordinal)) continue;

                    float distance = (mb.transform.position - position).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = mb;
                    }
                }

                if (nearest == null) return;
                if (lastReportedBodyH == nearest) return;

                if (TryReportBodyH(nearest)) lastReportedBodyH = nearest;
            }
            catch { }
        }

        private static bool TryReportBodyH(MonoBehaviour body)
        {
            try
            {
                if (body == null) return false;
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || GameData.Instance == null) return false;

                byte parentId = GetBodyParentIdH(body);
                if (parentId == byte.MaxValue) return false;

                NetworkedPlayerInfo reported = null;
                try { reported = GameData.Instance.GetPlayerById(parentId); } catch { }
                if (reported == null) return false;

                MethodInfo report = local.GetType().GetMethod("CmdReportDeadBody", FlagsH);
                if (report == null) return false;
                ParameterInfo[] parameters = report.GetParameters();
                if (parameters.Length != 1) return false;

                report.Invoke(local, new object[] { reported });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte GetBodyParentIdH(MonoBehaviour body)
        {
            try
            {
                if (body == null) return byte.MaxValue;
                Type type = body.GetType();

                PropertyInfo property = type.GetProperty("ParentId", FlagsH);
                if (property != null) return ToByteH(property.GetValue(body, null));

                FieldInfo field = type.GetField("ParentId", FlagsH);
                if (field != null) return ToByteH(field.GetValue(body));
            }
            catch { }
            return byte.MaxValue;
        }

        private static byte ToByteH(object value)
        {
            try
            {
                if (value == null) return byte.MaxValue;
                return Convert.ToByte(value);
            }
            catch
            {
                return byte.MaxValue;
            }
        }

        // ====================================================================
        // Feature 40 - Free camera movement during meetings
        // ====================================================================

        private static void TickMeetingCamH(bool inputBlocked)
        {
            try
            {
                bool active = extraMeetingCam && MeetingHud.Instance != null;
                if (!active)
                {
                    RestoreMeetingCamH();
                    return;
                }

                Camera camera = Camera.main;
                if (camera == null) return;

                if (!camCapturedH)
                {
                    camSavedPosH = camera.transform.position;
                    camCapturedH = true;
                }

                // Do not move the camera while the menu is capturing input.
                if (inputBlocked) return;

                Vector3 forward = camera.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
                else forward = Vector3.forward;

                Vector3 right = camera.transform.right;
                right.y = 0f;
                if (right.sqrMagnitude > 0.0001f) right.Normalize();
                else right = Vector3.right;

                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) move += forward;
                if (Input.GetKey(KeyCode.S)) move -= forward;
                if (Input.GetKey(KeyCode.D)) move += right;
                if (Input.GetKey(KeyCode.A)) move -= right;

                if (move.sqrMagnitude > 0f)
                {
                    move.Normalize();
                    camera.transform.position = camera.transform.position +
                                                move * (MeetingCamSpeedH * Time.unscaledDeltaTime);
                }
            }
            catch { }
        }

        private static void RestoreMeetingCamH()
        {
            if (!camCapturedH) return;
            try
            {
                Camera camera = Camera.main;
                if (camera != null) camera.transform.position = camSavedPosH;
            }
            catch { }
            camCapturedH = false;
        }
    }
}
