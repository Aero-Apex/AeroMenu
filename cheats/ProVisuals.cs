#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ===================================================================
        // Group O state (public static, exact contract names)
        // ===================================================================
        public static bool proRoleOutlines = false;
        public static bool proNameBackgrounds = false;
        public static bool proCooldownBars = false;
        public static bool proMinimapZoom = false;
        public static float proMinimapZoomValue = 1.5f;
        public static bool proBodyArrows = false;

        // ===================================================================
        // Lazily cached overlay resources (never allocated per frame)
        // ===================================================================
        private static bool proOStylesReadyO;
        private static Texture2D proOWhiteTexO;
        private static Texture2D proODarkTexO;
        private static Texture2D proOArrowTexO;
        private static GUIStyle proOLabelStyleO;

        // Tick-time caches (lists are reused, no per-frame allocation)
        private static readonly List<object> proOPlayersO = new List<object>();
        private static readonly List<Vector3> proOBodiesO = new List<Vector3>();
        private static float proONextScanO;
        private static float proOKillCooldownO = 25f;

        // Minimap zoom baseline / restore state
        private static bool proOZoomAppliedO;
        private static float proORadarScaleBaselineO = 1f;
        private static object proOMapTransformO;
        private static Vector3 proOMapScaleBaselineO = Vector3.one;

        private const BindingFlags ProOFlagsO =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private static readonly Dictionary<string, Type> proOTypeCacheO = new Dictionary<string, Type>();

        private static readonly int[] proORoleIdsO = { 0, 1, 2, 3, 4, 5, 8, 9, 10, 12, 18 };
        private static readonly Color[] proORoleColorsO =
        {
            new Color32(110, 220, 130, 255), // Crewmate
            new Color32(255, 40, 40, 255),   // Impostor
            new Color32(0, 0, 128, 255),     // Scientist / Engineer fallback
            new Color32(127, 255, 212, 255), // Engineer fallback
            new Color32(176, 196, 222, 255), // Crewmate alt
            new Color32(255, 140, 0, 255),   // Shapeshifter
            new Color32(255, 105, 180, 255), // Noisemaker
            new Color32(139, 0, 0, 255),     // Phantom
            new Color32(106, 90, 205, 255),  // Tracker
            new Color32(189, 183, 107, 255), // Detective
            new Color32(173, 255, 47, 255)   // Viper
        };
        private static readonly int[] proOImpostorRolesO = { 1, 5, 9, 18 };

        // ===================================================================
        // Required public surface
        // ===================================================================

        internal void DrawProO(float width)
        {
            try
            {
                float workWidth = GetMenuWorkWidth(width > 0f ? width : 160f, 620f);

                GUILayout.Label(MenuText(L("Pro Visuals", "Pro \u0432\u0438\u0437\u0443\u0430\u043B")), headerStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Role Outlines", "\u041A\u043E\u043D\u0442\u0443\u0440\u044B \u0440\u043E\u043B\u0435\u0439")),
                    subTabStyle != null ? subTabStyle : activeTabStyle);
                proRoleOutlines = DrawToggle(proRoleOutlines,
                    MenuText(L("Show Role Outlines", "\u041F\u043E\u043A\u0430\u0437\u044B\u0432\u0430\u0442\u044C \u043A\u043E\u043D\u0442\u0443\u0440\u044B")), 280);
                GUILayout.Label(MenuText(L("Colored outline above players, colored by role.",
                    "\u0426\u0432\u0435\u0442\u043D\u043E\u0439 \u043A\u043E\u043D\u0442\u0443\u0440 \u043D\u0430\u0434 \u0438\u0433\u0440\u043E\u043A\u0430\u043C\u0438 \u043F\u043E \u0440\u043E\u043B\u0438.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Name Backgrounds", "\u0424\u043E\u043D \u0438\u043C\u0451\u043D")),
                    subTabStyle != null ? subTabStyle : activeTabStyle);
                proNameBackgrounds = DrawToggle(proNameBackgrounds,
                    MenuText(L("Dark Name Boxes", "\u0422\u0451\u043C\u043D\u044B\u0435 \u043F\u043B\u0430\u0448\u043A\u0438 \u0438\u043C\u0451\u043D")), 280);
                GUILayout.Label(MenuText(L("Draws a dark box behind each player name tag.",
                    "\u0420\u0438\u0441\u0443\u0435\u0442 \u0442\u0435\u043C\u043D\u0443\u044E \u043F\u043B\u0430\u0448\u043A\u0443 \u0437\u0430 \u0438\u043C\u0435\u043D\u0435\u043C \u0438\u0433\u0440\u043E\u043A\u0430.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Cooldown Bars", "\u041F\u043E\u043B\u043E\u0441\u044B \u043A\u0414")),
                    subTabStyle != null ? subTabStyle : activeTabStyle);
                proCooldownBars = DrawToggle(proCooldownBars,
                    MenuText(L("Show Kill Cooldown", "\u041F\u043E\u043A\u0430\u0437\u044B\u0432\u0430\u0442\u044C \u041A\u0414 \u043A\u0438\u043B\u043B\u0430")), 280);
                GUILayout.Label(MenuText(L("Progress bars above impostors showing remaining kill cooldown.",
                    "\u041F\u043E\u043B\u043E\u0441\u044B \u043F\u0440\u043E\u0433\u0440\u0435\u0441\u0441\u0430 \u043D\u0430\u0434 \u0438\u043C\u043F\u043E\u0441\u0442\u0435\u0440\u0430\u043C\u0438.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Minimap Zoom", "\u041C\u0430\u0441\u0448\u0442\u0430\u0431 \u0440\u0430\u0434\u0430\u0440\u0430")),
                    subTabStyle != null ? subTabStyle : activeTabStyle);
                proMinimapZoom = DrawToggle(proMinimapZoom,
                    MenuText(L("Enable Minimap Zoom", "\u0412\u043A\u043B\u044E\u0447\u0438\u0442\u044C \u043C\u0430\u0441\u0448\u0442\u0430\u0431")), 280);
                GUILayout.BeginHorizontal(GUILayout.Width(workWidth));
                GUILayout.Label(MenuText(L("Zoom", "\u041C\u0430\u0441\u0448\u0442\u0430\u0431")), GUILayout.Width(70));
                proMinimapZoomValue = GUILayout.HorizontalSlider(proMinimapZoomValue, 0.5f, 2.5f);
                GUILayout.Label(proMinimapZoomValue.ToString("0.00") + "x", GUILayout.Width(52));
                GUILayout.EndHorizontal();
                GUILayout.Label(MenuText(L("Scales the radar rendering matrix.",
                    "\u041C\u0430\u0441\u0448\u0442\u0430\u0431\u0438\u0440\u0443\u0435\u0442 \u043C\u0430\u0442\u0440\u0438\u0446\u0443 \u043E\u0442\u0440\u0438\u0441\u043E\u0432\u043A\u0438 \u0440\u0430\u0434\u0430\u0440\u0430.")), menuDescStyle);
                GUILayout.Space(6);

                GUILayout.Label(MenuText(L("Body Arrows", "\u0421\u0442\u0440\u0435\u043B\u043A\u0438 \u043A \u0442\u0435\u043B\u0430\u043C")),
                    subTabStyle != null ? subTabStyle : activeTabStyle);
                proBodyArrows = DrawToggle(proBodyArrows,
                    MenuText(L("Off-screen Body Arrows", "\u0421\u0442\u0440\u0435\u043B\u043A\u0438 \u043A \u0442\u0435\u043B\u0430\u043C \u0432\u043D\u0435 \u0448\u043A\u0440\u0430\u043D\u0430")), 280);
                GUILayout.Label(MenuText(L("Directional arrows point to dead bodies outside the view.",
                    "\u0421\u0442\u0440\u0435\u043B\u043A\u0438 \u0443\u043A\u0430\u0437\u044B\u0432\u0430\u044E\u0442 \u043D\u0430 \u0442\u0435\u043B\u0430 \u0432\u043D\u0435 \u0448\u043A\u0440\u0430\u043D\u0430.")), menuDescStyle);
            }
            catch { }
        }

        internal static void TickProO(bool inputBlocked)
        {
            try
            {
                ApplyMinimapZoomO();

                float now = Time.realtimeSinceStartup;
                if (now >= proONextScanO)
                {
                    proONextScanO = now + 0.1f;
                    ScanPlayersO();
                    ScanBodiesO();
                    if (proCooldownBars) proOKillCooldownO = KillCooldownO();
                }

                // No hotkeys belong to this group; keep the guard explicit so
                // any future hotkey handling is skipped while input is blocked.
                if (inputBlocked) return;
            }
            catch { }
        }

        internal static void LoadProO()
        {
            try
            {
                proRoleOutlines = LoadBool("M_ProO_proRoleOutlines", proRoleOutlines);
                proNameBackgrounds = LoadBool("M_ProO_proNameBackgrounds", proNameBackgrounds);
                proCooldownBars = LoadBool("M_ProO_proCooldownBars", proCooldownBars);
                proMinimapZoom = LoadBool("M_ProO_proMinimapZoom", proMinimapZoom);
                proMinimapZoomValue = Mathf.Clamp(
                    PlayerPrefs.GetFloat("M_ProO_proMinimapZoomValue", proMinimapZoomValue), 0.5f, 2.5f);
                proBodyArrows = LoadBool("M_ProO_proBodyArrows", proBodyArrows);
            }
            catch { }
        }

        internal static void SaveProO()
        {
            try
            {
                SaveBool("M_ProO_proRoleOutlines", proRoleOutlines);
                SaveBool("M_ProO_proNameBackgrounds", proNameBackgrounds);
                SaveBool("M_ProO_proCooldownBars", proCooldownBars);
                SaveBool("M_ProO_proMinimapZoom", proMinimapZoom);
                PlayerPrefs.SetFloat("M_ProO_proMinimapZoomValue", proMinimapZoomValue);
                SaveBool("M_ProO_proBodyArrows", proBodyArrows);
            }
            catch { }
        }

        internal static void DrawProOOverlay()
        {
            // Skip everything when minimized / no window.
            if (Screen.width <= 1 || Screen.height <= 1) return;

            Event e = Event.current;
            if (e == null || e.type != EventType.Repaint) return;

            if (!proRoleOutlines && !proNameBackgrounds && !proCooldownBars && !proBodyArrows) return;

            Color previousColor = GUI.color;
            try
            {
                EnsureStylesO();
                if (!proOStylesReadyO) return;

                Camera cam = Camera.main;
                if (cam == null) return;

                try
                {
                    if (proRoleOutlines || proNameBackgrounds || proCooldownBars)
                    {
                        for (int i = 0; i < proOPlayersO.Count; i++)
                        {
                            object pc = proOPlayersO[i];
                            if (pc == null) continue;
                            if (pc is UnityEngine.Object uo && uo == null) continue;
                            try { DrawPlayerVisualO(pc, cam); } catch { }
                        }
                    }

                    if (proBodyArrows) DrawBodyArrowsO(cam);
                }
                catch { }
            }
            catch { }
            finally
            {
                GUI.color = previousColor;
            }
        }

        // ===================================================================
        // Private helpers (all suffixed with O)
        // ===================================================================

        private static void EnsureStylesO()
        {
            if (proOStylesReadyO && proOWhiteTexO != null && proOArrowTexO != null) return;
            try
            {
                if (proOWhiteTexO == null)
                {
                    proOWhiteTexO = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    proOWhiteTexO.SetPixel(0, 0, Color.white);
                    proOWhiteTexO.Apply();
                    proOWhiteTexO.hideFlags = HideFlags.HideAndDontSave;
                }

                if (proODarkTexO == null)
                {
                    proODarkTexO = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    proODarkTexO.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.62f));
                    proODarkTexO.Apply();
                    proODarkTexO.hideFlags = HideFlags.HideAndDontSave;
                }

                if (proOArrowTexO == null)
                {
                    const int size = 32;
                    proOArrowTexO = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    Color[] pixels = new Color[size * size];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

                    // Right-pointing triangle (texture y is bottom-up).
                    for (int y = 4; y <= 28; y++)
                    {
                        float t = 1f - Mathf.Abs(y - 16) / 12f;
                        int right = 3 + Mathf.RoundToInt(26f * t);
                        for (int x = 3; x <= right && x < size; x++)
                            pixels[y * size + x] = Color.white;
                    }
                    proOArrowTexO.SetPixels(pixels);
                    proOArrowTexO.Apply();
                    proOArrowTexO.hideFlags = HideFlags.HideAndDontSave;
                    proOArrowTexO.filterMode = FilterMode.Bilinear;
                }

                if (proOLabelStyleO == null)
                {
                    proOLabelStyleO = new GUIStyle(GUI.skin.label);
                    proOLabelStyleO.fontSize = 11;
                    proOLabelStyleO.fontStyle = FontStyle.Bold;
                    proOLabelStyleO.alignment = TextAnchor.MiddleCenter;
                    proOLabelStyleO.normal.textColor = new Color(1f, 0.72f, 0.2f, 1f);
                    proOLabelStyleO.clipping = TextClipping.Clip;
                    proOLabelStyleO.wordWrap = false;
                }

                proOStylesReadyO = true;
            }
            catch { }
        }

        private static Type ResolveTypeO(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                if (proOTypeCacheO.TryGetValue(name, out Type cached)) return cached;

                Type found = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        Type[] types = asm.GetTypes();
                        for (int i = 0; i < types.Length; i++)
                        {
                            if (string.Equals(types[i].Name, name, StringComparison.Ordinal))
                            {
                                found = types[i];
                                break;
                            }
                        }
                    }
                    catch { }
                    if (found != null) break;
                }

                proOTypeCacheO[name] = found;
                return found;
            }
            catch { return null; }
        }

        private static object ReadMemberO(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                bool isType = target is Type;
                Type t = isType ? (Type)target : target.GetType();
                object inst = isType ? null : target;

                FieldInfo field = t.GetField(name, ProOFlagsO);
                if (field != null) return field.GetValue(field.IsStatic ? null : inst);

                PropertyInfo prop = t.GetProperty(name, ProOFlagsO);
                if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    MethodInfo getter = prop.GetGetMethod(true);
                    if (getter != null) return prop.GetValue(getter.IsStatic ? null : inst, null);
                }
            }
            catch { }
            return null;
        }

        private static object ReadStaticO(Type type, string name)
        {
            if (type == null) return null;
            try
            {
                FieldInfo field = type.GetField(name, ProOFlagsO);
                if (field != null && field.IsStatic) return field.GetValue(null);

                PropertyInfo prop = type.GetProperty(name, ProOFlagsO);
                if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    MethodInfo getter = prop.GetGetMethod(true);
                    if (getter != null && getter.IsStatic) return prop.GetValue(null, null);
                }
            }
            catch { }
            return null;
        }

        private static object CallMethodO(object target, string name, object[] args)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                bool isType = target is Type;
                Type t = isType ? (Type)target : target.GetType();
                object inst = isType ? null : target;
                int count = args == null ? 0 : args.Length;

                MethodInfo[] methods = t.GetMethods(ProOFlagsO);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (m.Name != name || m.GetParameters().Length != count) continue;
                    return m.Invoke(inst, args);
                }
            }
            catch { }
            return null;
        }

        private static bool ReadBoolO(object target, string name, bool fallback)
        {
            object v = ReadMemberO(target, name);
            return v is bool b ? b : fallback;
        }

        private static int ReadIntO(object target, string name, int fallback)
        {
            try
            {
                object v = ReadMemberO(target, name);
                return v == null ? fallback : Convert.ToInt32(v);
            }
            catch { return fallback; }
        }

        private static float ReadFloatO(object target, string name, float fallback)
        {
            try
            {
                object v = ReadMemberO(target, name);
                return v == null ? fallback : Convert.ToSingle(v);
            }
            catch { return fallback; }
        }

        private static string ReadStringO(object target, string name, string fallback)
        {
            try
            {
                object v = ReadMemberO(target, name);
                return v == null ? fallback : v.ToString();
            }
            catch { return fallback; }
        }

        private static Vector3? PositionO(object obj)
        {
            if (obj == null) return null;
            try
            {
                object t = ReadMemberO(obj, "transform");
                if (t is Transform tr && tr != null) return tr.position;
            }
            catch { }
            try
            {
                if (obj is UnityEngine.Component comp && comp != null) return comp.transform.position;
            }
            catch { }
            return null;
        }

        private static Vector3 ReadLocalScaleO(object tr)
        {
            object v = ReadMemberO(tr, "localScale");
            return v is Vector3 s ? s : Vector3.one;
        }

        private static void SetLocalScaleO(object tr, Vector3 scale)
        {
            if (tr == null) return;
            try
            {
                Type t = tr.GetType();
                PropertyInfo prop = t.GetProperty("localScale", ProOFlagsO);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(tr, scale, null);
                    return;
                }
                FieldInfo field = t.GetField("localScale", ProOFlagsO);
                if (field != null) field.SetValue(tr, scale);
            }
            catch { }
        }

        private static void ScanPlayersO()
        {
            proOPlayersO.Clear();
            try
            {
                Type pt = ResolveTypeO("PlayerControl");
                if (pt == null) return;

                object all = ReadStaticO(pt, "AllPlayerControls");
                if (all == null) return;

                if (all is System.Collections.IEnumerable enumerable)
                {
                    foreach (object item in enumerable)
                        if (item != null) proOPlayersO.Add(item);
                    return;
                }

                object countObj = ReadMemberO(all, "Count");
                int count = countObj == null ? 0 : Convert.ToInt32(countObj);
                for (int i = 0; i < count; i++)
                {
                    object item = CallMethodO(all, "get_Item", new object[] { i });
                    if (item != null) proOPlayersO.Add(item);
                }
            }
            catch { }
        }

        private static UnityEngine.Object[] FindObjectsGenericO<T>() where T : UnityEngine.Object
        {
            try { return UnityEngine.Object.FindObjectsOfType<T>(); }
            catch { return null; }
        }

        private static UnityEngine.Object[] FindObjectsByTypeO(Type type)
        {
            if (type == null) return null;
            try
            {
                MethodInfo generic = typeof(AeroMenuGUI).GetMethod("FindObjectsGenericO", ProOFlagsO);
                if (generic == null) return null;
                MethodInfo closed = generic.MakeGenericMethod(type);
                object result = closed.Invoke(null, null);
                return result as UnityEngine.Object[];
            }
            catch { return null; }
        }

        private static void ScanBodiesO()
        {
            proOBodiesO.Clear();
            try
            {
                Type bt = ResolveTypeO("DeadBody");
                if (bt == null) return;

                UnityEngine.Object[] found = FindObjectsByTypeO(bt);
                if (found == null) return;

                for (int i = 0; i < found.Length; i++)
                {
                    UnityEngine.Object body = found[i];
                    if (body == null) continue;
                    Vector3? pos = PositionO(body);
                    if (pos.HasValue) proOBodiesO.Add(pos.Value);
                }
            }
            catch { }
        }

        private static void ApplyMinimapZoomO()
        {
            try
            {
                float z = Mathf.Clamp(proMinimapZoomValue, 0.5f, 2.5f);

                if (!proMinimapZoom)
                {
                    if (proOZoomAppliedO)
                    {
                        try
                        {
                            FieldInfo radar = typeof(AeroMenuGUI).GetField("radarScale", ProOFlagsO);
                            if (radar != null) radar.SetValue(null, proORadarScaleBaselineO);
                        }
                        catch { }
                        try { SetLocalScaleO(proOMapTransformO, proOMapScaleBaselineO); } catch { }
                        proOMapTransformO = null;
                        proOZoomAppliedO = false;
                    }
                    return;
                }

                // Menu radar: scale the value used by its rendering matrix.
                FieldInfo radarField = typeof(AeroMenuGUI).GetField("radarScale", ProOFlagsO);
                if (radarField != null && radarField.FieldType == typeof(float))
                {
                    if (!proOZoomAppliedO)
                    {
                        try { proORadarScaleBaselineO = Convert.ToSingle(radarField.GetValue(null)); }
                        catch { proORadarScaleBaselineO = 1f; }
                    }
                    radarField.SetValue(null, Mathf.Clamp(z, 0.65f, 1.6f));
                }

                // In-game map: scale the rendered map transform if present.
                Type mapType = ResolveTypeO("MapBehaviour");
                if (mapType != null)
                {
                    object instance = ReadStaticO(mapType, "Instance");
                    if (instance != null)
                    {
                        object tr = ReadMemberO(instance, "transform");
                        if (tr != null)
                        {
                            object child = CallMethodO(tr, "Find", new object[] { "Map" });
                            if (child == null) child = CallMethodO(tr, "Find", new object[] { "map" });
                            if (child == null) child = tr;

                            if (child != null)
                            {
                                if (proOMapTransformO == null || !ReferenceEquals(proOMapTransformO, child))
                                {
                                    proOMapTransformO = child;
                                    proOMapScaleBaselineO = ReadLocalScaleO(child);
                                }
                                SetLocalScaleO(child, new Vector3(z, z, 1f));
                            }
                        }
                    }
                }

                proOZoomAppliedO = true;
            }
            catch { }
        }

        private static float KillCooldownO()
        {
            try
            {
                Type mgrType = ResolveTypeO("GameOptionsManager");
                object mgr = ReadStaticO(mgrType, "Instance");
                object opts = ReadMemberO(mgr, "CurrentGameOptions");
                if (opts == null) return 25f;

                MethodInfo[] methods = opts.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (m.Name != "GetFloat" || m.GetParameters().Length != 1) continue;

                    Type enumType = m.GetParameters()[0].ParameterType;
                    if (!enumType.IsEnum) continue;

                    foreach (object val in Enum.GetValues(enumType))
                    {
                        string n = val.ToString().ToLowerInvariant();
                        if (n.Contains("kill") && n.Contains("cool"))
                            return Convert.ToSingle(m.Invoke(opts, new object[] { val }));
                    }
                }
            }
            catch { }
            return 25f;
        }

        private static int RoleIdO(object data)
        {
            object role = ReadMemberO(data, "Role");
            int id = ReadIntO(role, "Role", -1);
            if (id < 0) id = ReadIntO(data, "RoleType", -1);
            return id;
        }

        private static Color RoleColorO(int roleId, Color fallback)
        {
            for (int i = 0; i < proORoleIdsO.Length; i++)
                if (proORoleIdsO[i] == roleId) return proORoleColorsO[i];
            return fallback;
        }

        private static bool IsImpostorRoleO(int roleId)
        {
            for (int i = 0; i < proOImpostorRolesO.Length; i++)
                if (proOImpostorRolesO[i] == roleId) return true;
            return false;
        }

        private static bool TryProjectO(Camera cam, Vector3 world, out Vector2 gui)
        {
            gui = Vector2.zero;
            try
            {
                Vector3 sp = cam.WorldToScreenPoint(world);
                if (sp.z <= 0f) return false;
                gui = new Vector2(sp.x, Screen.height - sp.y);
                return !float.IsNaN(gui.x) && !float.IsInfinity(gui.x) &&
                       !float.IsNaN(gui.y) && !float.IsInfinity(gui.y);
            }
            catch { return false; }
        }

        private static void DrawPlayerVisualO(object pc, Camera cam)
        {
            object data = ReadMemberO(pc, "Data");
            if (data == null) return;
            if (ReadBoolO(data, "Disconnected", false)) return;
            if (ReadBoolO(data, "IsDead", false)) return;

            Vector3? pp = PositionO(pc);
            if (!pp.HasValue) return;

            Vector3 headWorld = pp.Value + new Vector3(0f, 0.85f, 0f);
            if (!TryProjectO(cam, headWorld, out Vector2 head)) return;
            if (head.x < -60f || head.x > Screen.width + 60f ||
                head.y < -60f || head.y > Screen.height + 60f) return;

            object role = ReadMemberO(data, "Role");
            int roleId = RoleIdO(data);

            Color fallback = Color.white;
            object teamColor = ReadMemberO(role, "TeamColor");
            if (teamColor is Color tc) fallback = tc;
            Color roleColor = RoleColorO(roleId, fallback);

            if (proRoleOutlines)
                DrawOutlineMarkerO(head.x, head.y - 14f, roleColor);

            if (proNameBackgrounds)
            {
                object cosmetics = ReadMemberO(pc, "cosmetics");
                object nameText = ReadMemberO(cosmetics, "nameText");

                Vector2 namePos = head;
                Vector3? np = PositionO(nameText);
                if (np.HasValue && TryProjectO(cam, np.Value, out Vector2 projected)) namePos = projected;

                string name = ReadStringO(data, "PlayerName", string.Empty);
                if (string.IsNullOrEmpty(name))
                {
                    object outfit = ReadMemberO(data, "DefaultOutfit");
                    name = ReadStringO(outfit, "PlayerName", "???");
                }
                DrawNameBackgroundO(namePos.x, namePos.y - 2f, name.Length, roleColor);
            }

            if (proCooldownBars && IsImpostorRoleO(roleId))
            {
                float timer = ReadFloatO(pc, "killTimer", -1f);
                if (timer >= 0f)
                    DrawCooldownBarO(head.x, head.y - 34f, timer, proOKillCooldownO);
            }
        }

        private static void DrawOutlineMarkerO(float x, float y, Color color)
        {
            const float size = 16f;
            const float t = 2f;
            float left = x - size * 0.5f;
            float top = y - size * 0.5f;

            GUI.color = color;
            GUI.DrawTexture(new Rect(left, top, size, t), proOWhiteTexO, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(left, top + size - t, size, t), proOWhiteTexO, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(left, top, t, size), proOWhiteTexO, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(left + size - t, top, t, size), proOWhiteTexO, ScaleMode.StretchToFill, true);
        }

        private static void DrawNameBackgroundO(float cx, float bottomY, int nameLength, Color accent)
        {
            float w = Mathf.Clamp(14f + nameLength * 8f, 34f, 220f);
            const float h = 18f;
            Rect r = new Rect(cx - w * 0.5f, bottomY - h, w, h);

            GUI.color = Color.white;
            GUI.DrawTexture(r, proODarkTexO, ScaleMode.StretchToFill, true);

            GUI.color = accent;
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), proOWhiteTexO, ScaleMode.StretchToFill, true);
        }

        private static void DrawCooldownBarO(float cx, float bottomY, float remaining, float total)
        {
            const float w = 44f;
            const float h = 6f;
            Rect back = new Rect(cx - w * 0.5f, bottomY - h, w, h);

            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(back, proOWhiteTexO, ScaleMode.StretchToFill, true);

            float progress = total > 0.01f ? Mathf.Clamp01(1f - remaining / total) : 1f;
            if (progress > 0.001f)
            {
                GUI.color = Color.Lerp(new Color(1f, 0.25f, 0.2f, 1f), new Color(0.3f, 1f, 0.42f, 1f), progress);
                GUI.DrawTexture(new Rect(back.x, back.y, back.width * progress, back.height),
                    proOWhiteTexO, ScaleMode.StretchToFill, true);
            }
        }

        private static void DrawBodyArrowsO(Camera cam)
        {
            for (int i = 0; i < proOBodiesO.Count; i++)
            {
                Vector3 body = proOBodiesO[i];
                try
                {
                    Vector3 sp = cam.WorldToScreenPoint(body);
                    bool behind = sp.z <= 0f;
                    float x = sp.x;
                    float y = Screen.height - sp.y;
                    if (behind)
                    {
                        x = Screen.width - x;
                        y = Screen.height - y;
                    }
                    if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y)) continue;

                    const float margin = 28f;
                    bool onScreen = !behind &&
                                    x >= margin && x <= Screen.width - margin &&
                                    y >= margin && y <= Screen.height - margin;

                    if (onScreen)
                    {
                        GUI.color = new Color(1f, 0.3f, 0.25f, 1f);
                        GUI.DrawTexture(new Rect(x - 4f, y - 4f, 8f, 8f), proOWhiteTexO, ScaleMode.StretchToFill, true);
                        continue;
                    }

                    Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 d = new Vector2(x, y) - center;
                    if (d.sqrMagnitude < 1f) d = new Vector2(0f, -1f);

                    Vector2 half = new Vector2(Mathf.Max(1f, Screen.width * 0.5f - margin),
                                               Mathf.Max(1f, Screen.height * 0.5f - margin));
                    float sx = Mathf.Abs(d.x) > 0.001f ? half.x / Mathf.Abs(d.x) : float.MaxValue;
                    float sy = Mathf.Abs(d.y) > 0.001f ? half.y / Mathf.Abs(d.y) : float.MaxValue;
                    float k = Mathf.Min(sx, sy);
                    Vector2 edge = center + d * k;
                    float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

                    Matrix4x4 old = GUI.matrix;
                    try
                    {
                        GUIUtility.RotateAroundPivot(angle, edge);
                        GUI.color = new Color(1f, 0.55f, 0.1f, 1f);
                        GUI.DrawTexture(new Rect(edge.x - 14f, edge.y - 14f, 28f, 28f),
                            proOArrowTexO, ScaleMode.StretchToFill, true);
                    }
                    finally
                    {
                        GUI.matrix = old;
                    }

                    GUI.Label(new Rect(edge.x - 28f, edge.y + 12f, 56f, 16f), "BODY", proOLabelStyleO);
                }
                catch { }
            }
        }
    }
}
