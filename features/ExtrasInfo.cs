#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Reflection;
using UnityEngine;
using InnerNet;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ===== Group B state (public static, exact contract names) =====
        public static bool extraRoomCodePanel = false;
        public static bool extraPlayerCountPanel = false;
        public static bool extraTaskProgressPanel = false;
        public static bool extraMeetingTimerPanel = false;
        public static bool extraPingPanel = false;

        // ===== Cached overlay resources (created lazily, never per-frame) =====
        private static bool extrasBStylesReady;
        private static GUIStyle extrasBLabelStyle;
        private static GUIStyle extrasBTitleStyle;
        private static GUIStyle extrasBPanelStyle;
        private static Texture2D extrasBPanelTex;

        // Last valid room code so the panel can degrade gracefully outside a room.
        private static string extrasBLastCode = string.Empty;

        private const BindingFlags ExtrasBFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.FlattenHierarchy;

        // ===================================================================
        // Required public surface
        // ===================================================================

        internal void DrawExtrasB(float width)
        {
            bool began = false;
            try
            {
                int w = Mathf.RoundToInt(Mathf.Clamp(width, 180f, 640f));

                GUILayout.BeginVertical(menuCardStyle);
                began = true;

                GUILayout.Label(MenuText(L("Info Panels", "\u0418\u043D\u0444\u043E \u043F\u0430\u043D\u0435\u043B\u0438")), headerStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Room Code", "\u041A\u043E\u0434 \u043A\u043E\u043C\u043D\u0430\u0442\u044B")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraRoomCodePanel = DrawToggle(extraRoomCodePanel, MenuText(L("Show room code panel", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u043A\u043E\u0434 \u043A\u043E\u043C\u043D\u0430\u0442\u044B")), w);
                GUILayout.Label(MenuText(L("Overlay with the current lobby code (also shown lowercase).", "\u041E\u0432\u0435\u0440\u043B\u0435\u0439 \u0441 \u0442\u0435\u043A\u0443\u0449\u0438\u043C \u043A\u043E\u0434\u043E\u043C \u043B\u043E\u0431\u0431\u0438.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Player Counts", "\u0427\u0438\u0441\u043B\u0435\u043D\u043D\u043E\u0441\u0442\u044C \u0438\u0433\u0440\u043E\u043A\u043E\u0432")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraPlayerCountPanel = DrawToggle(extraPlayerCountPanel, MenuText(L("Show player count panel", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u0447\u0438\u0441\u043B\u043E \u0438\u0433\u0440\u043E\u043A\u043E\u0432")), w);
                GUILayout.Label(MenuText(L("Connected / alive / crewmate / impostor totals.", "\u041F\u043E\u0434\u043A\u043B\u044E\u0447\u0435\u043D\u043E / \u0436\u0438\u0432\u044B / \u043C\u0438\u0440\u043D\u044B\u0435 / \u043F\u0440\u0435\u0434\u0430\u0442\u0435\u043B\u0438.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Task Progress", "\u041F\u0440\u043E\u0433\u0440\u0435\u0441\u0441 \u0437\u0430\u0434\u0430\u043D\u0438\u0439")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraTaskProgressPanel = DrawToggle(extraTaskProgressPanel, MenuText(L("Show task progress panel", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u043F\u0440\u043E\u0433\u0440\u0435\u0441\u0441 \u0437\u0430\u0434\u0430\u043D\u0438\u0439")), w);
                GUILayout.Label(MenuText(L("Your completed vs total tasks.", "\u0412\u0430\u0448\u0438 \u0432\u044B\u043F\u043E\u043B\u043D\u0435\u043D\u043D\u044B\u0435 \u0438\u0437 \u043E\u0431\u0449\u0435\u0433\u043E \u0447\u0438\u0441\u043B\u0430 \u0437\u0430\u0434\u0430\u043D\u0438\u0439.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Meeting Timer", "\u0422\u0430\u0439\u043C\u0435\u0440 \u0441\u043E\u0431\u0440\u0430\u043D\u0438\u044F")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraMeetingTimerPanel = DrawToggle(extraMeetingTimerPanel, MenuText(L("Show meeting timer panel", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u0442\u0430\u0439\u043C\u0435\u0440 \u0441\u043E\u0431\u0440\u0430\u043D\u0438\u044F")), w);
                GUILayout.Label(MenuText(L("Meeting state and remaining countdown.", "\u0421\u043E\u0441\u0442\u043E\u044F\u043D\u0438\u0435 \u0441\u043E\u0431\u0440\u0430\u043D\u0438\u044F \u0438 \u043E\u0441\u0442\u0430\u0432\u0448\u0435\u0435\u0441\u044F \u0432\u0440\u0435\u043C\u044F.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Network Info", "\u0421\u0435\u0442\u0435\u0432\u0430\u044F \u0438\u043D\u0444\u043E\u0440\u043C\u0430\u0446\u0438\u044F")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraPingPanel = DrawToggle(extraPingPanel, MenuText(L("Show network info panel", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u0441\u0435\u0442\u0435\u0432\u0443\u044E \u0438\u043D\u0444\u043E\u0440\u043C\u0430\u0446\u0438\u044E")), w);
                GUILayout.Label(MenuText(L("Region / ping / connection state (N/A when unavailable).", "\u0420\u0435\u0433\u0438\u043E\u043D / ping / \u0441\u043E\u0441\u0442\u043E\u044F\u043D\u0438\u0435 (N/A \u0435\u0441\u043B\u0438 \u043D\u0435\u0434\u043E\u0441\u0442\u0443\u043F\u043D\u043E).")), menuDescStyle);

                GUILayout.EndVertical();
                began = false;
            }
            catch { }
            finally
            {
                if (began)
                {
                    try { GUILayout.EndVertical(); } catch { }
                }
            }
        }

        internal static void TickExtrasB(bool inputBlocked)
        {
            // This group has no hotkeys. Styles and data are resolved lazily on the
            // overlay's OnGUI pass, so nothing per-frame is required here.
            // Keep the input guard explicit for any future hotkey handling.
            if (inputBlocked) return;
        }

        internal static void LoadExtrasB()
        {
            try
            {
                extraRoomCodePanel = LoadBoolB("M_ExtrasB_extraRoomCodePanel", extraRoomCodePanel);
                extraPlayerCountPanel = LoadBoolB("M_ExtrasB_extraPlayerCountPanel", extraPlayerCountPanel);
                extraTaskProgressPanel = LoadBoolB("M_ExtrasB_extraTaskProgressPanel", extraTaskProgressPanel);
                extraMeetingTimerPanel = LoadBoolB("M_ExtrasB_extraMeetingTimerPanel", extraMeetingTimerPanel);
                extraPingPanel = LoadBoolB("M_ExtrasB_extraPingPanel", extraPingPanel);
            }
            catch { }
        }

        internal static void SaveExtrasB()
        {
            try
            {
                SaveBoolB("M_ExtrasB_extraRoomCodePanel", extraRoomCodePanel);
                SaveBoolB("M_ExtrasB_extraPlayerCountPanel", extraPlayerCountPanel);
                SaveBoolB("M_ExtrasB_extraTaskProgressPanel", extraTaskProgressPanel);
                SaveBoolB("M_ExtrasB_extraMeetingTimerPanel", extraMeetingTimerPanel);
                SaveBoolB("M_ExtrasB_extraPingPanel", extraPingPanel);
            }
            catch { }
        }

        internal static void DrawExtrasBOverlay()
        {
            if (Screen.width <= 1 || Screen.height <= 1) return;

            Event e = Event.current;
            if (e == null || e.type != EventType.Repaint) return;

            if (!extraRoomCodePanel && !extraPlayerCountPanel && !extraTaskProgressPanel &&
                !extraMeetingTimerPanel && !extraPingPanel) return;

            Color previousColor = GUI.color;
            try
            {
                EnsureStylesB();
                if (!extrasBStylesReady) return;

                const float x = 14f;
                const float w = 232f;
                float y = 14f;

                if (extraRoomCodePanel)
                {
                    y = DrawInfoPanelB(x, y, w, L("ROOM CODE", "\u041A\u043E\u0434 \u041A\u041E\u041C\u041D\u0410\u0422\u042B"), GetRoomCodeBodyB());
                    if (y + 60f > Screen.height) return;
                }
                if (extraPlayerCountPanel)
                {
                    y = DrawInfoPanelB(x, y, w, L("PLAYERS", "\u0418\u0413\u0420\u041E\u041A\u0418"), GetPlayerCountBodyB());
                    if (y + 60f > Screen.height) return;
                }
                if (extraTaskProgressPanel)
                {
                    y = DrawInfoPanelB(x, y, w, L("TASKS", "\u0417\u0410\u0414\u0410\u041D\u0418\u042F"), GetTaskProgressBodyB());
                    if (y + 60f > Screen.height) return;
                }
                if (extraMeetingTimerPanel)
                {
                    y = DrawInfoPanelB(x, y, w, L("MEETING", "\u0421\u041E\u0411\u0420\u0410\u041D\u0418\u0415"), GetMeetingBodyB());
                    if (y + 60f > Screen.height) return;
                }
                if (extraPingPanel)
                {
                    DrawInfoPanelB(x, y, w, L("NETWORK", "\u0421\u0415\u0422\u042C"), GetPingBodyB());
                }
            }
            catch { }
            finally
            {
                GUI.color = previousColor;
            }
        }

        // ===================================================================
        // Private helpers (all suffixed with B)
        // ===================================================================

        private static void EnsureStylesB()
        {
            if (extrasBStylesReady && extrasBLabelStyle != null &&
                extrasBTitleStyle != null && extrasBPanelStyle != null) return;

            try
            {
                if (extrasBPanelTex == null)
                {
                    extrasBPanelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    extrasBPanelTex.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.08f, 0.82f));
                    extrasBPanelTex.Apply(false, true);
                    extrasBPanelTex.hideFlags = HideFlags.HideAndDontSave;
                }

                if (extrasBLabelStyle == null)
                {
                    extrasBLabelStyle = new GUIStyle(GUI.skin.label);
                    extrasBLabelStyle.fontSize = 12;
                    extrasBLabelStyle.alignment = TextAnchor.UpperLeft;
                    extrasBLabelStyle.clipping = TextClipping.Clip;
                    extrasBLabelStyle.wordWrap = false;
                    extrasBLabelStyle.richText = true;
                    extrasBLabelStyle.normal.textColor = new Color(0.93f, 0.95f, 1f, 1f);
                }

                if (extrasBTitleStyle == null)
                {
                    extrasBTitleStyle = new GUIStyle(extrasBLabelStyle);
                    extrasBTitleStyle.fontSize = 13;
                    extrasBTitleStyle.fontStyle = FontStyle.Bold;
                    extrasBTitleStyle.normal.textColor = new Color(0.30f, 0.85f, 1f, 1f);
                }

                if (extrasBPanelStyle == null)
                {
                    extrasBPanelStyle = new GUIStyle();
                    extrasBPanelStyle.normal.background = extrasBPanelTex;
                    extrasBPanelStyle.padding = new RectOffset { left = 9, right = 9, top = 5, bottom = 5 };
                }

                extrasBStylesReady = true;
            }
            catch { }
        }

        private static float DrawInfoPanelB(float x, float y, float w, string title, string body)
        {
            int lines = 1;
            if (!string.IsNullOrEmpty(body))
            {
                for (int i = 0; i < body.Length; i++)
                    if (body[i] == '\n') lines++;
            }

            float lineH = 17f;
            float h = 27f + lines * lineH + 5f;
            Rect r = new Rect(x, y, w, h);

            GUI.color = Color.white;
            GUI.Box(r, GUIContent.none, extrasBPanelStyle);

            string titleText = "<b><color=#4DD8FF>" + (title ?? string.Empty) + "</color></b>";
            GUI.Label(new Rect(x + 9f, y + 5f, w - 18f, 18f), titleText, extrasBTitleStyle);

            if (!string.IsNullOrEmpty(body))
                GUI.Label(new Rect(x + 9f, y + 26f, w - 18f, lines * lineH + 2f), body, extrasBLabelStyle);

            return y + h + 6f;
        }

        // ---------- Data collectors (reflection, silent on failure) ----------

        private static string GetRoomCodeBodyB()
        {
            string code = "N/A";
            try
            {
                object client = AmongUsClient.Instance;
                object idObj = GetMemberValueB(client, "GameId", "gameId");
                int id;
                if (TryGetIntB(idObj, out id) && id != 0)
                {
                    string name = FormatGameIdB(id);
                    code = string.IsNullOrWhiteSpace(name) ? id.ToString() : name;
                    if (!string.IsNullOrWhiteSpace(code)) extrasBLastCode = code;
                }
                else if (!string.IsNullOrWhiteSpace(extrasBLastCode))
                {
                    code = extrasBLastCode + " (last)";
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(extrasBLastCode)) code = extrasBLastCode + " (last)";
            }

            string lower = code == "N/A" ? "N/A" : code.ToLowerInvariant();
            return "Code: <b>" + code + "</b>\nLower: " + lower;
        }

        private static string GetPlayerCountBodyB()
        {
            int connected = 0;
            int alive = 0;
            int crew = 0;
            int imp = 0;

            try
            {
                var list = PlayerControl.AllPlayerControls;
                if (list != null)
                {
                    foreach (PlayerControl pc in list)
                    {
                        if (pc == null) continue;
                        try
                        {
                            connected++;

                            object data = GetMemberValueB(pc, "Data");
                            if (data == null) continue;

                            bool dead = false;
                            TryGetBoolB(GetMemberValueB(data, "IsDead", "isDead"), out dead);
                            if (!dead) alive++;

                            bool isImp = false;
                            if (!TryGetBoolB(GetMemberValueB(data, "IsImpostor", "isImpostor"), out isImp))
                            {
                                object role = GetMemberValueB(data, "Role", "role");
                                TryGetBoolB(GetMemberValueB(role, "IsImpostor", "isImpostor"), out isImp);
                            }

                            if (isImp) imp++;
                            else crew++;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return "Connected: <b>" + connected + "</b>\nAlive: <b>" + alive + "</b>\nCrew: " + crew + "   Imp: " + imp;
        }

        private static string GetTaskProgressBodyB()
        {
            int completed = 0;
            int total = 0;
            bool found = false;

            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                object data = GetMemberValueB(local, "Data");
                object tasks = GetMemberValueB(data, "Tasks", "tasks");
                if (tasks != null)
                {
                    found = true;
                    total = GetCountB(tasks);
                    for (int i = 0; i < total; i++)
                    {
                        object task = GetIndexedValueB(tasks, i);
                        if (task == null) continue;

                        bool done = false;
                        object complete = GetMemberValueB(task,
                            "IsComplete", "Complete", "isComplete", "complete", "Completed", "_complete");
                        if (TryGetBoolB(complete, out done) && done) completed++;
                    }
                }
            }
            catch { }

            if (!found) return "Tasks: N/A";

            string pct = total > 0 ? Mathf.RoundToInt(100f * completed / total) + "%" : "0%";
            return "Completed: <b>" + completed + " / " + total + "</b>\nProgress: " + pct;
        }

        private static string GetMeetingBodyB()
        {
            try
            {
                object meeting = MeetingHud.Instance;
                if (meeting == null) return "State: <b>\u2014</b>\nTimer: N/A";

                object timerObj = GetMemberValueB(meeting,
                    "currentTimer", "CurrentTimer", "discussionTimer", "DiscussionTimer", "timer", "Timer");
                float timer;
                bool hasTimer = TryGetFloatB(timerObj, out timer);

                object stateObj = GetMemberValueB(meeting, "state", "State", "meetingState", "MeetingState");
                string state = stateObj != null ? stateObj.ToString() : "N/A";
                if (string.IsNullOrWhiteSpace(state)) state = "N/A";

                return "State: <b>" + state + "</b>\nTimer: " + (hasTimer ? FormatSecondsB(timer) : "N/A");
            }
            catch { }

            return "State: N/A\nTimer: N/A";
        }

        private static string GetPingBodyB()
        {
            string region = "N/A";
            string ping = "N/A";
            string state = "N/A";

            try
            {
                object client = AmongUsClient.Instance;
                if (client != null)
                {
                    try
                    {
                        MethodInfo getRegion = client.GetType().GetMethod(
                            "GetRegion", ExtrasBFlags, null, Type.EmptyTypes, null);
                        if (getRegion != null)
                        {
                            object reg = getRegion.Invoke(client, null);
                            object name = GetMemberValueB(reg, "name", "Name", "regionName", "RegionName");
                            if (name != null) region = name.ToString();
                        }
                    }
                    catch { }

                    if (region == "N/A")
                    {
                        object reg = GetMemberValueB(client, "Region", "region", "CurrentRegion", "currentRegion");
                        object name = GetMemberValueB(reg, "name", "Name", "regionName", "RegionName");
                        if (name != null) region = name.ToString();
                        else if (reg != null) region = reg.ToString();
                    }
                    if (string.IsNullOrWhiteSpace(region)) region = "N/A";

                    int p;
                    if (TryGetIntB(GetMemberValueB(client, "Ping", "ping", "AveragePing", "averagePing"), out p))
                        ping = p + " ms";

                    object stateObj = GetMemberValueB(client,
                        "GameState", "gameState", "State", "state", "Mode", "mode");
                    if (stateObj != null && !string.IsNullOrWhiteSpace(stateObj.ToString()))
                        state = stateObj.ToString();
                }
            }
            catch { }

            return "Region: <b>" + region + "</b>\nPing: " + ping + "\nState: " + state;
        }

        // ---------- Generic reflection helpers ----------

        private static object GetMemberValueB(object target, params string[] names)
        {
            if (target == null || names == null) return null;
            try
            {
                Type type = target.GetType();
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    if (string.IsNullOrEmpty(name)) continue;

                    try
                    {
                        PropertyInfo prop = type.GetProperty(name, ExtrasBFlags);
                        if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                        {
                            object value = prop.GetValue(target, null);
                            if (value != null) return value;
                        }
                    }
                    catch { }

                    try
                    {
                        FieldInfo field = type.GetField(name, ExtrasBFlags);
                        if (field != null)
                        {
                            object value = field.GetValue(target);
                            if (value != null) return value;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        private static object GetIndexedValueB(object target, int index)
        {
            if (target == null) return null;
            try
            {
                PropertyInfo prop = target.GetType().GetProperty("Item", ExtrasBFlags);
                if (prop != null && prop.GetIndexParameters().Length > 0)
                    return prop.GetValue(target, new object[] { index });
            }
            catch { }

            try
            {
                MethodInfo getter = target.GetType().GetMethod("get_Item", ExtrasBFlags);
                if (getter != null) return getter.Invoke(target, new object[] { index });
            }
            catch { }

            return null;
        }

        private static int GetCountB(object target)
        {
            if (target == null) return 0;
            int count;
            if (TryGetIntB(GetMemberValueB(target, "Count", "Length", "count", "length"), out count))
                return Mathf.Clamp(count, 0, 1024);
            return 0;
        }

        private static bool TryGetBoolB(object value, out bool result)
        {
            result = false;
            if (value == null) return false;
            try
            {
                if (value is bool b) { result = b; return true; }
                if (value is int i) { result = i != 0; return true; }

                string s = value.ToString();
                if (string.Equals(s, "True", StringComparison.OrdinalIgnoreCase) || s == "1")
                {
                    result = true;
                    return true;
                }
                if (string.Equals(s, "False", StringComparison.OrdinalIgnoreCase) || s == "0")
                {
                    result = false;
                    return true;
                }

                result = Convert.ToBoolean(value);
                return true;
            }
            catch { return false; }
        }

        private static bool TryGetIntB(object value, out int result)
        {
            result = 0;
            if (value == null) return false;
            try
            {
                if (value is int i) { result = i; return true; }
                if (value is long l) { result = (int)l; return true; }
                if (value is short sh) { result = sh; return true; }
                if (value is uint ui) { result = (int)ui; return true; }
                result = Convert.ToInt32(value);
                return true;
            }
            catch { return false; }
        }

        private static bool TryGetFloatB(object value, out float result)
        {
            result = 0f;
            if (value == null) return false;
            try
            {
                if (value is float f) { result = f; return true; }
                if (value is double d) { result = (float)d; return true; }
                if (value is int i) { result = i; return true; }
                result = Convert.ToSingle(value);
                return true;
            }
            catch { return false; }
        }

        private static string FormatSecondsB(float seconds)
        {
            try
            {
                if (float.IsNaN(seconds) || float.IsInfinity(seconds)) return "N/A";
                if (seconds < 0f) seconds = 0f;
                int total = Mathf.CeilToInt(seconds);
                return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
            }
            catch { return "N/A"; }
        }

        private static string FormatGameIdB(int id)
        {
            try
            {
                Assembly asm = typeof(AmongUsClient).Assembly;
                Type gameCode = asm.GetType("GameCode") ?? asm.GetType("InnerNet.GameCode");
                if (gameCode != null)
                {
                    MethodInfo convert = gameCode.GetMethod(
                        "IntToGameName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (convert != null)
                    {
                        object result = convert.Invoke(null, new object[] { id });
                        if (result != null) return result.ToString();
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        // ---------- Persistence ----------

        private static bool LoadBoolB(string key, bool def)
        {
            try { return PlayerPrefs.HasKey(key) ? LoadBool(key, def) : def; }
            catch { return def; }
        }

        private static void SaveBoolB(string key, bool value)
        {
            try { SaveBool(key, value); }
            catch { }
        }
    }
}
