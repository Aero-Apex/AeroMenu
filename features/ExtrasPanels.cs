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
        // ---- Group G: overlay panels (exact field names from the contract) ----
        public static bool extraTaskListPanel = false;
        public static bool extraRoleChartPanel = false;
        public static bool extraPlayerStatusPanel = false;
        public static bool extraHotkeyReferencePanel = false;
        public static bool extraFpsGraphPanel = false;

        private const int ExtrasFpsSamplesG = 120;
        private const BindingFlags ExtrasReflectFlagsG = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        // FPS ring buffer (last 120 samples).
        private static readonly float[] extrasFpsHistoryG = new float[ExtrasFpsSamplesG];
        private static int extrasFpsCursorG = 0;
        private static int extrasFpsFilledG = 0;
        private static float extrasFpsLatestG = 0f;

        // Known role names + numeric RoleTypes ids (modded ids included).
        private static readonly string[] extrasRoleNamesG =
        {
            "Crewmate", "Impostor", "Scientist", "Engineer", "Shapeshifter",
            "Guardian Angel", "Noisemaker", "Phantom", "Tracker", "Detective", "Viper"
        };
        private static readonly int[] extrasRoleIdsG = { 0, 1, 2, 3, 4, 5, 8, 9, 10, 12, 18 };
        private static readonly int[] extrasRoleCountsG = new int[extrasRoleIdsG.Length];
        private static float extrasRoleCacheAtG = -1f;

        private static string[] extrasHotkeyLinesG = null;
        private static float extrasHotkeyCacheAtG = -1f;

        private static Texture2D extrasWhiteTexG = null;

        private static GUIStyle extrasPanelBgG = null;
        private static GUIStyle extrasPanelTitleG = null;
        private static GUIStyle extrasPanelTextG = null;
        private static GUIStyle extrasPanelValueG = null;
        private static bool extrasPanelStylesReadyG = false;

        // ------------------------------------------------------------------
        // Required surface
        // ------------------------------------------------------------------

        internal void DrawExtrasG(float width)
        {
            try
            {
                float w = width > 0f ? width : GetMenuWorkWidth(180f, 620f);
                GUILayout.BeginVertical(GUILayout.Width(w));
                try
                {
                    GUILayout.Label(MenuText(L("OVERLAY PANELS", "ИНФОРМАЦИОННЫЕ ПАНЕЛИ")), headerStyle);

                    int toggleW = Mathf.RoundToInt(Mathf.Clamp(w, 82f, 620f));
                    extraTaskListPanel = DrawToggle(extraTaskListPanel, L("Task List Panel", "Панель задач"), toggleW);
                    extraRoleChartPanel = DrawToggle(extraRoleChartPanel, L("Role Chart Panel", "Таблица ролей"), toggleW);
                    extraPlayerStatusPanel = DrawToggle(extraPlayerStatusPanel, L("Player Status Panel", "Статус игроков"), toggleW);
                    extraHotkeyReferencePanel = DrawToggle(extraHotkeyReferencePanel, L("Hotkey Reference Panel", "Справка по клавишам"), toggleW);
                    extraFpsGraphPanel = DrawToggle(extraFpsGraphPanel, L("FPS Graph Panel", "График FPS"), toggleW);

                    GUILayout.Space(4f);
                    GUILayout.Label(MenuText(L("Panels: LCTRL+LSHIFT+1..5", "Панели: LCTRL+LSHIFT+1..5")), menuDescStyle);
                }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }

        internal static void TickExtrasG(bool inputBlocked)
        {
            try
            {
                // FPS sampling is input-independent and always cheap.
                float dt = Time.unscaledDeltaTime;
                float fps = dt > 0.0001f ? 1f / dt : 0f;
                fps = Mathf.Clamp(fps, 0f, 300f);
                extrasFpsLatestG = fps;
                extrasFpsHistoryG[extrasFpsCursorG] = fps;
                extrasFpsCursorG = (extrasFpsCursorG + 1) % ExtrasFpsSamplesG;
                if (extrasFpsFilledG < ExtrasFpsSamplesG) extrasFpsFilledG++;

                if (inputBlocked) return;

                bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl);
                bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift);
                if (!ctrl || !shift) return;

                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) extraTaskListPanel = !extraTaskListPanel;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) extraRoleChartPanel = !extraRoleChartPanel;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) extraPlayerStatusPanel = !extraPlayerStatusPanel;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) extraHotkeyReferencePanel = !extraHotkeyReferencePanel;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) extraFpsGraphPanel = !extraFpsGraphPanel;
            }
            catch { }
        }

        internal static void LoadExtrasG()
        {
            try
            {
                extraTaskListPanel = LoadBoolG("extraTaskListPanel", extraTaskListPanel);
                extraRoleChartPanel = LoadBoolG("extraRoleChartPanel", extraRoleChartPanel);
                extraPlayerStatusPanel = LoadBoolG("extraPlayerStatusPanel", extraPlayerStatusPanel);
                extraHotkeyReferencePanel = LoadBoolG("extraHotkeyReferencePanel", extraHotkeyReferencePanel);
                extraFpsGraphPanel = LoadBoolG("extraFpsGraphPanel", extraFpsGraphPanel);
            }
            catch { }
        }

        internal static void SaveExtrasG()
        {
            try
            {
                SaveBoolG("extraTaskListPanel", extraTaskListPanel);
                SaveBoolG("extraRoleChartPanel", extraRoleChartPanel);
                SaveBoolG("extraPlayerStatusPanel", extraPlayerStatusPanel);
                SaveBoolG("extraHotkeyReferencePanel", extraHotkeyReferencePanel);
                SaveBoolG("extraFpsGraphPanel", extraFpsGraphPanel);
            }
            catch { }
        }

        internal static void DrawExtrasGOverlay()
        {
            try
            {
                Event e = Event.current;
                if (e == null || e.type != EventType.Repaint) return;

                EnsurePanelStylesG();
                EnsureWhiteTextureG();

                float sw = Mathf.Max(1f, Screen.width);
                float sh = Mathf.Max(1f, Screen.height);

                if (extraTaskListPanel) DrawTaskListPanelG(sw, sh);
                if (extraRoleChartPanel) DrawRoleChartPanelG(sw, sh);
                if (extraPlayerStatusPanel) DrawPlayerStatusPanelG(sw, sh);
                if (extraHotkeyReferencePanel) DrawHotkeyReferencePanelG(sw, sh);
                if (extraFpsGraphPanel) DrawFpsGraphPanelG(sw, sh);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Persistence helpers (keys: M_ExtrasG_<FieldName>)
        // ------------------------------------------------------------------

        private static string ExtrasKeyG(string fieldName)
        {
            return "M_ExtrasG_" + fieldName;
        }

        private static bool LoadBoolG(string fieldName, bool fallback)
        {
            try
            {
                string key = ExtrasKeyG(fieldName);
                return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) == 1 : fallback;
            }
            catch { return fallback; }
        }

        private static void SaveBoolG(string fieldName, bool value)
        {
            try { PlayerPrefs.SetInt(ExtrasKeyG(fieldName), value ? 1 : 0); }
            catch { }
        }

        // ------------------------------------------------------------------
        // Generic reflection helpers (all guarded)
        // ------------------------------------------------------------------

        private static object GetMemberValueG(object target, params string[] names)
        {
            if (target == null || names == null) return null;
            Type t = target.GetType();
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    PropertyInfo p = t.GetProperty(name, ExtrasReflectFlagsG);
                    if (p != null && p.GetIndexParameters().Length == 0)
                        return p.GetValue(target, null);
                }
                catch { }
                try
                {
                    FieldInfo f = t.GetField(name, ExtrasReflectFlagsG);
                    if (f != null) return f.GetValue(target);
                }
                catch { }
            }
            return null;
        }

        private static bool ReadBoolG(object target, params string[] names)
        {
            object raw = GetMemberValueG(target, names);
            if (raw == null) return false;
            try { return raw is bool b ? b : Convert.ToBoolean(raw); }
            catch { return false; }
        }

        private static bool TryReadIntG(object target, out int result, params string[] names)
        {
            result = 0;
            object raw = GetMemberValueG(target, names);
            if (raw == null) return false;
            try { result = Convert.ToInt32(raw); return true; }
            catch { return false; }
        }

        private static object ConvertRoleArgG(int id, Type targetType)
        {
            if (targetType == null) return id;
            try
            {
                if (targetType.IsEnum) return Enum.ToObject(targetType, id);
                if (targetType == typeof(int)) return id;
                if (targetType == typeof(byte)) return (byte)Mathf.Clamp(id, 0, 255);
                if (targetType == typeof(short)) return (short)id;
                if (targetType == typeof(long)) return (long)id;
                if (targetType == typeof(uint)) return (uint)Mathf.Max(0, id);
                return Convert.ChangeType(id, targetType);
            }
            catch { return id; }
        }

        private static bool RoleItemMatchesG(object item, string roleNameNoSpaces, int roleId)
        {
            if (item == null) return false;
            object raw = GetMemberValueG(item, "Role", "RoleType", "RoleId", "TypeId");
            if (raw != null)
            {
                try { if (Convert.ToInt32(raw) == roleId) return true; }
                catch { }
                string s = raw.ToString();
                if (!string.IsNullOrEmpty(s) &&
                    (s.IndexOf(roleNameNoSpaces, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     string.Equals(s, roleId.ToString(), StringComparison.Ordinal))) return true;
            }
            try
            {
                string txt = item.ToString();
                return !string.IsNullOrEmpty(txt) && txt.IndexOf(roleNameNoSpaces, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private static bool TryGetRoleCountG(object opts, string roleName, int roleId, out int count)
        {
            count = 0;
            if (opts == null) return false;

            object bag = null;
            foreach (string n in new[] { "RoleOptions", "roleOptions", "RoleSettings", "roleSettings", "Roles", "roles" })
            {
                bag = GetMemberValueG(opts, n);
                if (bag != null) break;
            }

            if (bag != null)
            {
                Type bt = bag.GetType();

                // Indexer access, e.g. roleOptions[RoleTypes.Engineer].
                try
                {
                    foreach (PropertyInfo p in bt.GetProperties(ExtrasReflectFlagsG))
                    {
                        ParameterInfo[] idx = p.GetIndexParameters();
                        if (idx.Length != 1) continue;
                        try
                        {
                            object item = p.GetValue(bag, new[] { ConvertRoleArgG(roleId, idx[0].ParameterType) });
                            if (item != null && TryReadIntG(item, out count, "Count", "RoleCount", "NumberOfRoles", "Value")) return true;
                        }
                        catch { }
                    }
                }
                catch { }

                // Single-argument accessors, e.g. GetRoleOptions(role).
                try
                {
                    foreach (MethodInfo m in bt.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m.IsGenericMethodDefinition) continue;
                        string mn = m.Name;
                        if (mn.IndexOf("Role", StringComparison.OrdinalIgnoreCase) < 0 &&
                            mn.IndexOf("Option", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        ParameterInfo[] ps = m.GetParameters();
                        if (ps.Length != 1) continue;
                        try
                        {
                            object item = m.Invoke(bag, new[] { ConvertRoleArgG(roleId, ps[0].ParameterType) });
                            if (item != null && TryReadIntG(item, out count, "Count", "RoleCount", "NumberOfRoles", "Value")) return true;
                        }
                        catch { }
                    }
                }
                catch { }

                // Enumerating the bag and matching by role id/name.
                if (bag is System.Collections.IEnumerable enBag)
                {
                    try
                    {
                        foreach (object item in enBag)
                        {
                            if (item == null) continue;
                            if (!RoleItemMatchesG(item, roleName, roleId)) continue;
                            if (TryReadIntG(item, out count, "Count", "RoleCount", "NumberOfRoles", "Value")) return true;
                        }
                    }
                    catch { }
                }
            }

            // Last resort: look for role-option objects directly on the options object.
            try
            {
                if (opts is System.Collections.IEnumerable enOpts)
                {
                    foreach (object item in enOpts)
                    {
                        if (item == null) continue;
                        if (!RoleItemMatchesG(item, roleName, roleId)) continue;
                        if (TryReadIntG(item, out count, "Count", "RoleCount", "NumberOfRoles", "Value")) return true;
                    }
                }
            }
            catch { }

            count = 0;
            return false;
        }

        private static bool TryGetIntOptionG(object opts, string optionName, out int value)
        {
            value = 0;
            if (opts == null) return false;
            try
            {
                foreach (MethodInfo m in opts.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.Name != "GetInt" && m.Name != "GetInteger") continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length != 1) continue;
                    Type pt = ps[0].ParameterType;
                    object arg;
                    try { arg = pt.IsEnum ? Enum.Parse(pt, optionName, true) : Convert.ChangeType(optionName, pt); }
                    catch { continue; }
                    try
                    {
                        object r = m.Invoke(opts, new[] { arg });
                        if (r == null) continue;
                        value = Convert.ToInt32(r);
                        return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private static void RefreshRoleCountsG()
        {
            try
            {
                object opts = null;
                try { opts = GameOptionsManager.Instance?.CurrentGameOptions; }
                catch { }

                for (int i = 0; i < extrasRoleIdsG.Length; i++)
                {
                    int c = 0;
                    string normalized = (extrasRoleNamesG[i] ?? string.Empty).Replace(" ", string.Empty);
                    if (!TryGetRoleCountG(opts, normalized, extrasRoleIdsG[i], out c))
                    {
                        if (extrasRoleIdsG[i] == 1)
                            TryGetIntOptionG(opts, "NumImpostors", out c);
                        else if (extrasRoleIdsG[i] == 0)
                            TryGetIntOptionG(opts, "NumCrewmates", out c);
                    }
                    extrasRoleCountsG[i] = Mathf.Clamp(c, 0, 999);
                }
            }
            catch { }
            extrasRoleCacheAtG = Time.unscaledTime + 0.5f;
        }

        private static void AddHotkeyLineG(List<string> lines, string label, string fieldName, KeyCode fallback)
        {
            KeyCode key = fallback;
            try
            {
                FieldInfo f = typeof(AeroMenuGUI).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (f != null)
                {
                    object v = f.GetValue(null);
                    if (v is KeyCode kc) key = kc;
                    else key = (KeyCode)Convert.ToInt32(v);
                }
            }
            catch { }

            string keyText = key == KeyCode.None ? "-" : key.ToString().ToUpperInvariant();
            lines.Add(label + " : " + keyText);
        }

        private static void RefreshHotkeyLinesG()
        {
            try
            {
                var lines = new List<string>();

                AddHotkeyLineG(lines, "Menu", "menuToggleKey", KeyCode.Insert);

                // Optional binds from other groups; missing fields are silently skipped.
                AddHotkeyLineG(lines, "Player Info", "bindTogglePlayerInfo", KeyCode.None);
                AddHotkeyLineG(lines, "See Roles", "bindToggleSeeRoles", KeyCode.None);
                AddHotkeyLineG(lines, "See Ghosts", "bindToggleSeeGhosts", KeyCode.None);
                AddHotkeyLineG(lines, "Tracers", "bindToggleTracers", KeyCode.None);
                AddHotkeyLineG(lines, "NoClip", "bindToggleNoClip", KeyCode.None);
                AddHotkeyLineG(lines, "Freecam", "bindToggleFreecam", KeyCode.None);
                AddHotkeyLineG(lines, "Camera Zoom", "bindToggleCameraZoom", KeyCode.None);
                AddHotkeyLineG(lines, "Full Bright", "bindToggleFullBright", KeyCode.None);
                AddHotkeyLineG(lines, "Kill All", "bindKillAll", KeyCode.None);
                AddHotkeyLineG(lines, "Call Meeting", "bindCallMeeting", KeyCode.None);
                AddHotkeyLineG(lines, "Kick All", "bindKickAll", KeyCode.None);
                AddHotkeyLineG(lines, "Fix Sabotages", "bindFixSabotages", KeyCode.None);
                AddHotkeyLineG(lines, "Copy Code", "bindCopyCode", KeyCode.None);
                AddHotkeyLineG(lines, "Mass Morph", "bindMassMorph", KeyCode.None);
                AddHotkeyLineG(lines, "Spawn Lobby", "bindSpawnLobby", KeyCode.None);
                AddHotkeyLineG(lines, "Despawn Lobby", "bindDespawnLobby", KeyCode.None);
                AddHotkeyLineG(lines, "Close Meeting", "bindCloseMeeting", KeyCode.None);
                AddHotkeyLineG(lines, "Insta Start", "bindInstaStart", KeyCode.None);
                AddHotkeyLineG(lines, "Magnet Cursor", "bindMagnetCursor", KeyCode.None);
                AddHotkeyLineG(lines, "Revive All", "bindReviveAll", KeyCode.None);
                AddHotkeyLineG(lines, "Screenshot", "extraScreenshotKey", KeyCode.None);
                AddHotkeyLineG(lines, "TP To Cursor", "extraTpToCursorKey", KeyCode.None);
                AddHotkeyLineG(lines, "Push / Pull", "extraPushPullKey", KeyCode.None);

                lines.Add("Panels : LCTRL+LSHIFT+1..5");
                extrasHotkeyLinesG = lines.ToArray();
            }
            catch { extrasHotkeyLinesG = new[] { "Menu : INSERT", "Panels : LCTRL+LSHIFT+1..5" }; }
            extrasHotkeyCacheAtG = Time.unscaledTime + 1f;
        }

        // ------------------------------------------------------------------
        // Overlay rendering helpers
        // ------------------------------------------------------------------

        private static void EnsurePanelStylesG()
        {
            if (extrasPanelStylesReadyG && extrasPanelBgG != null) return;
            try
            {
                extrasPanelBgG = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
                    margin = new RectOffset { left = 0, right = 0, top = 0, bottom = 0 }
                };
                extrasPanelTitleG = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    wordWrap = false
                };
                extrasPanelTextG = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    wordWrap = false
                };
                extrasPanelValueG = new GUIStyle(extrasPanelTextG)
                {
                    alignment = TextAnchor.MiddleRight
                };
                extrasPanelStylesReadyG = true;
            }
            catch { }
        }

        private static void EnsureWhiteTextureG()
        {
            if (extrasWhiteTexG != null) return;
            try
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                extrasWhiteTexG = tex;
            }
            catch { }
        }

        private static Rect PanelRectG(int slot, float w, float h, float sw, float sh)
        {
            w = Mathf.Clamp(w, 140f, Mathf.Max(140f, sw - 24f));
            h = Mathf.Clamp(h, 64f, Mathf.Max(64f, sh - 32f));
            const float margin = 12f;
            float x, y;
            switch (slot)
            {
                case 0: x = margin; y = 70f; break;
                case 1: x = sw - w - margin; y = 70f; break;
                case 2: x = margin; y = sh - h - margin; break;
                case 3: x = sw - w - margin; y = sh - h - margin; break;
                default: x = (sw - w) * 0.5f; y = 70f; break;
            }
            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, sw - w));
            y = Mathf.Clamp(y, 0f, Mathf.Max(0f, sh - h));
            return new Rect(x, y, w, h);
        }

        private static void DrawPanelFrameG(Rect r, string title)
        {
            Color old = GUI.color;
            try
            {
                GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);
                GUI.Box(r, GUIContent.none, extrasPanelBgG);
                GUI.color = old;

                GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, 18f), title, extrasPanelTitleG);

                if (extrasWhiteTexG != null)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.18f);
                    GUI.DrawTexture(new Rect(r.x + 10f, r.y + 26f, r.width - 20f, 1f), extrasWhiteTexG);
                    GUI.color = old;
                }
            }
            catch { GUI.color = old; }
        }

        private static string GetTaskLabelG(object task)
        {
            object raw = GetMemberValueG(task, "TypeId", "TaskType", "TaskId", "Id", "taskType", "taskId");
            if (raw == null) return "Task";
            try { return raw.ToString().Replace("_", " "); }
            catch { return "Task"; }
        }

        private static void DrawTaskListPanelG(float sw, float sh)
        {
            Rect r = PanelRectG(0, 250f, 200f, sw, sh);
            DrawPanelFrameG(r, MenuText(L("TASK LIST", "СПИСОК ЗАДАЧ")));
            if (extrasPanelTextG == null) return;

            try
            {
                float x = r.x + 10f;
                float y = r.y + 32f;
                float w = r.width - 20f;

                // Reserve one row for the "+N" overflow label and one for the
                // "Done" footer so neither is painted over the last task row.
                int maxRows = Mathf.Max(1, Mathf.FloorToInt((r.height - 70f) / 16f) - 1);
                int done = 0, total = 0, line = 0;
                PlayerControl local = PlayerControl.LocalPlayer;
                object data = local != null ? local.Data : null;
                object tasksObj = GetMemberValueG(data, "Tasks");

                if (tasksObj is System.Collections.IEnumerable en)
                {
                    foreach (object task in en)
                    {
                        if (task == null) continue;
                        total++;
                        bool complete = ReadBoolG(task, "Complete", "Completed", "IsComplete", "IsCompleted");
                        if (complete) done++;

                        if (line < maxRows)
                        {
                            string mark = complete ? "<color=#7CFC98>[X]</color> " : "<color=#FFB266>[ ]</color> ";
                            GUI.Label(new Rect(x, y + line * 16f, w, 16f), mark + GetTaskLabelG(task), extrasPanelTextG);
                            line++;
                        }
                    }
                }

                if (line == 0)
                {
                    GUI.Label(new Rect(x, y, w, 16f), MenuText(L("No tasks", "Нет задач")), extrasPanelTextG);
                    line = 1;
                }
                if (total > maxRows)
                {
                    GUI.Label(new Rect(x, y + line * 16f, w, 16f), "… +" + (total - maxRows), extrasPanelTextG);
                    line++;
                }

                string doneText = MenuText(L("Done", "Выполнено"));
                GUI.Label(new Rect(x, r.yMax - 22f, w, 16f),
                    "<color=#aaaaaa>" + doneText + "</color> " + done + "/" + total, extrasPanelTextG);
            }
            catch { }
        }

        private static void DrawRoleChartPanelG(float sw, float sh)
        {
            Rect r = PanelRectG(1, 240f, 250f, sw, sh);
            DrawPanelFrameG(r, MenuText(L("ROLE CHART", "ТАБЛИЦА РОЛЕЙ")));
            if (extrasPanelTextG == null) return;

            try
            {
                if (Time.unscaledTime >= extrasRoleCacheAtG) RefreshRoleCountsG();

                float x = r.x + 10f;
                float y = r.y + 32f;
                float w = r.width - 20f;

                GUI.Label(new Rect(x, y, w - 44f, 16f),
                    "<color=#aaaaaa>" + MenuText(L("ROLE", "РОЛЬ")), extrasPanelTextG);
                GUI.Label(new Rect(x + w - 44f, y, 44f, 16f),
                    "<color=#aaaaaa>" + MenuText(L("COUNT", "КОЛ-ВО")), extrasPanelValueG);
                y += 18f;

                int maxRows = Mathf.Max(1, Mathf.FloorToInt((r.height - 62f) / 16f));
                int rows = Mathf.Min(extrasRoleCountsG.Length, maxRows);
                for (int i = 0; i < rows; i++)
                {
                    GUI.Label(new Rect(x, y + i * 16f, w - 44f, 16f), extrasRoleNamesG[i], extrasPanelTextG);
                    GUI.Label(new Rect(x + w - 44f, y + i * 16f, 44f, 16f),
                        "<color=#7FC8FF>" + extrasRoleCountsG[i] + "</color>", extrasPanelValueG);
                }
            }
            catch { }
        }

        private static string RoleDisplayG(object data)
        {
            if (data == null) return "?";
            object raw = GetMemberValueG(data, "RoleType", "Role");
            if (raw == null) return "?";
            try
            {
                string s = raw.ToString();
                int id = -1;
                if (raw is Enum) id = Convert.ToInt32(raw);
                else int.TryParse(s, out id);

                switch (id)
                {
                    case 8: s = "Noisemaker"; break;
                    case 9: s = "Phantom"; break;
                    case 10: s = "Tracker"; break;
                    case 12: s = "Detective"; break;
                    case 18: s = "Viper"; break;
                }
                if (s == "GuardianAngel") s = "Guardian Angel";
                return s;
            }
            catch { return "?"; }
        }

        private static bool IsImpostorG(PlayerControl player)
        {
            if (player == null || player.Data == null) return false;
            try { if (player.Data.Role != null && player.Data.Role.IsImpostor) return true; }
            catch { }
            try { return RoleManager.IsImpostorRole(player.Data.RoleType); }
            catch { return false; }
        }

        private static void DrawPlayerStatusPanelG(float sw, float sh)
        {
            Rect r = PanelRectG(2, 250f, 190f, sw, sh);
            DrawPanelFrameG(r, MenuText(L("PLAYER STATUS", "СТАТУС ИГРОКОВ")));
            if (extrasPanelTextG == null) return;

            try
            {
                float x = r.x + 10f;
                float y = r.y + 32f;
                float w = r.width - 20f;
                int maxRows = Mathf.Max(1, Mathf.FloorToInt((r.height - 36f) / 32f));

                int shown = 0, skipped = 0;
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (p == null || p.Data == null || p.Data.Disconnected || p.PlayerId >= 100) continue;

                        if (shown >= maxRows) { skipped++; continue; }

                        string name = p.Data.PlayerName;
                        if (string.IsNullOrEmpty(name)) name = "Player " + p.PlayerId;
                        if (p == PlayerControl.LocalPlayer) name += " (you)";

                        bool dead = false;
                        try { dead = p.Data.IsDead; }
                        catch { }

                        string status = dead
                            ? "<color=#888888>" + MenuText(L("DEAD", "МЁРТВ")) + "</color>"
                            : "<color=#7CFC98>" + MenuText(L("ALIVE", "ЖИВ")) + "</color>";
                        string imp = IsImpostorG(p)
                            ? " <color=#FF5555>[IMP]</color>" : string.Empty;
                        string role = RoleDisplayG(p.Data);

                        GUI.Label(new Rect(x, y + shown * 32f, w, 16f), name + imp, extrasPanelTextG);
                        GUI.Label(new Rect(x, y + shown * 32f + 15f, w, 16f),
                            status + " <color=#aaaaaa>· " + role + "</color>", extrasPanelTextG);
                        shown++;
                    }
                }

                if (shown == 0)
                    GUI.Label(new Rect(x, y, w, 16f), MenuText(L("No players", "Нет игроков")), extrasPanelTextG);
                if (skipped > 0)
                    GUI.Label(new Rect(x, r.yMax - 18f, w, 16f), "… +" + skipped, extrasPanelTextG);
            }
            catch { }
        }

        private static void DrawHotkeyReferencePanelG(float sw, float sh)
        {
            Rect r = PanelRectG(3, 260f, 280f, sw, sh);
            DrawPanelFrameG(r, MenuText(L("KEYBINDS", "ГОРЯЧИЕ КЛАВИШИ")));
            if (extrasPanelTextG == null) return;

            try
            {
                if (Time.unscaledTime >= extrasHotkeyCacheAtG || extrasHotkeyLinesG == null)
                    RefreshHotkeyLinesG();

                float x = r.x + 10f;
                float y = r.y + 32f;
                float w = r.width - 20f;
                // Leave one row of headroom for the overflow label at r.yMax - 18.
                int maxRows = Mathf.Max(1, Mathf.FloorToInt((r.height - 56f) / 16f));
                string[] lines = extrasHotkeyLinesG ?? Array.Empty<string>();
                int rows = Mathf.Min(lines.Length, maxRows);

                for (int i = 0; i < rows; i++)
                    GUI.Label(new Rect(x, y + i * 16f, w, 16f), lines[i], extrasPanelTextG);

                if (lines.Length > rows)
                    GUI.Label(new Rect(x, r.yMax - 18f, w, 16f), "… +" + (lines.Length - rows), extrasPanelTextG);
            }
            catch { }
        }

        private static void DrawFpsGraphPanelG(float sw, float sh)
        {
            Rect r = PanelRectG(4, 240f, 130f, sw, sh);
            DrawPanelFrameG(r, MenuText(L("FPS GRAPH", "ГРАФИК FPS")));
            if (extrasWhiteTexG == null || extrasPanelTextG == null) return;

            try
            {
                float gx = r.x + 10f;
                float gy = r.y + 32f;
                float gw = r.width - 20f;
                float gh = Mathf.Max(10f, r.height - 60f);

                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                GUI.DrawTexture(new Rect(gx, gy, gw, gh), extrasWhiteTexG);
                GUI.color = old;

                int n = Mathf.Min(extrasFpsFilledG, ExtrasFpsSamplesG);
                if (n > 0)
                {
                    float barW = gw / ExtrasFpsSamplesG;
                    for (int i = 0; i < n; i++)
                    {
                        int idx = (extrasFpsCursorG - n + i + ExtrasFpsSamplesG) % ExtrasFpsSamplesG;
                        float fps = extrasFpsHistoryG[idx];
                        float barH = Mathf.Clamp01(fps / 120f) * gh;

                        Color c = fps >= 55f
                            ? new Color(0.35f, 1f, 0.45f, 1f)
                            : (fps >= 30f ? new Color(1f, 0.82f, 0.25f, 1f) : new Color(1f, 0.3f, 0.3f, 1f));
                        GUI.color = c;
                        GUI.DrawTexture(new Rect(gx + i * barW, gy + gh - barH, Mathf.Max(1f, barW), barH), extrasWhiteTexG);
                    }
                    GUI.color = old;
                }

                GUI.Label(new Rect(gx, r.yMax - 22f, gw, 16f),
                    "FPS " + extrasFpsLatestG.ToString("0") + "  <color=#aaaaaa>(last " + n + " frames)</color>",
                    extrasPanelTextG);
            }
            catch { GUI.color = Color.white; }
        }
    }
}
