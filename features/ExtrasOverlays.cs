#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Reflection;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ===== Group A state (public static, exact contract names) =====
        public static bool extraCrosshair = false;
        public static int extraCrosshairSize = 14;
        public static int extraCrosshairColor = 0; // 0 white, 1 red, 2 green, 3 cyan, 4 yellow

        public static bool extraFpsCounter = false;
        public static int extraFpsCorner = 3;      // 0 TL, 1 TR, 2 BL, 3 BR

        public static bool extraCompass = false;
        public static bool extraPlayerDistance = false;

        public static bool extraSessionClock = false;
        public static bool extraSessionClock24h = false;

        // ===== Cached overlay resources (created lazily, never per-frame) =====
        private static bool extrasAStylesReady;
        private static Texture2D extrasAWhiteTex;
        private static Texture2D extrasAPanelTex;
        private static GUIStyle extrasAHudStyle;
        private static GUIStyle extrasAHudCenterStyle;
        private static GUIStyle extrasAPanelStyle;
        private static GUIStyle extrasACompassStyle;

        private const BindingFlags ExtrasAFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private static readonly Color[] extrasACrosshairColors = new Color[]
        {
            Color.white,
            new Color(1f, 0.18f, 0.18f),
            new Color(0.20f, 1f, 0.32f),
            new Color(0.15f, 0.92f, 1f),
            new Color(1f, 0.92f, 0.18f)
        };

        private static readonly float[] extrasACompassHeadings = new float[]
        {
            0f, 45f, 90f, 135f, 180f, 225f, 270f, 292.5f, 315f
        };
        private static readonly string[] extrasACompassLabels = new string[]
        {
            "N", "NE", "E", "SE", "S", "SW", "W", "WNW", "NW"
        };

        // ===== Tick-time metrics =====
        private static float extrasAFpsLastTime;
        private static int extrasAFpsFrames;
        private static float extrasAFpsSmooth = 60f;
        private static int extrasAFpsDisplay = 60;
        private static string extrasAFpsText = "FPS --";

        private static float extrasASessionStart = -1f;
        private static float extrasAMetricsNext;
        private static float extrasAYaw;
        private static string extrasAHeadingText = "0\u00B0";
        private static string extrasADistanceText = string.Empty;
        private static string extrasASessionElapsedText = "00:00:00";
        private static string extrasAClockText = string.Empty;

        // ===================================================================
        // Required public surface
        // ===================================================================

        internal void DrawExtrasA(float width)
        {
            try
            {
                GUILayout.Label(MenuText(L("Overlay Extras", "\u0414\u043E\u043F. \u043E\u0432\u0435\u0440\u043B\u0435\u0438")), headerStyle);
                GUILayout.Space(6);

                // --- Crosshair ---
                GUILayout.Label(MenuText(L("Crosshair", "\u041F\u0440\u0438\u0446\u0435\u043B")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraCrosshair = DrawToggle(extraCrosshair, MenuText(L("Show Crosshair", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u043F\u0440\u0438\u0446\u0435\u043B")), 260);
                GUILayout.Label(MenuText(L("Draws a crosshair at the center of the screen.", "\u0420\u0438\u0441\u0443\u0435\u0442 \u043F\u0440\u0438\u0446\u0435\u043B \u0432 \u0446\u0435\u043D\u0442\u0440\u0435 \u044D\u043A\u0440\u0430\u043D\u0430.")), menuDescStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label(MenuText(L("Size", "\u0420\u0430\u0437\u043C\u0435\u0440")), GUILayout.Width(70));
                float chSize = ThemedSlider((float)extraCrosshairSize, 4f, 80f);
                extraCrosshairSize = Mathf.Clamp(Mathf.RoundToInt(chSize), 4, 80);
                GUILayout.Label(extraCrosshairSize.ToString(), GUILayout.Width(34));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(MenuText(L("Color", "\u0426\u0432\u0435\u0442")), GUILayout.Width(70));
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                    extraCrosshairColor = WrapIndexA(extraCrosshairColor - 1, 5);
                GUILayout.Label(GetCrosshairColorNameA(extraCrosshairColor), activeTabStyle != null ? activeTabStyle : btnStyle, GUILayout.Width(86), GUILayout.Height(24));
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                    extraCrosshairColor = WrapIndexA(extraCrosshairColor + 1, 5);
                GUILayout.EndHorizontal();
                GUILayout.Space(8);

                // --- FPS counter ---
                GUILayout.Label(MenuText(L("FPS Counter", "\u0421\u0447\u0435\u0442\u0447\u0438\u043A FPS")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraFpsCounter = DrawToggle(extraFpsCounter, MenuText(L("Show FPS", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C FPS")), 260);
                GUILayout.BeginHorizontal();
                GUILayout.Label(MenuText(L("Corner", "\u0423\u0433\u043E\u043B")), GUILayout.Width(70));
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                    extraFpsCorner = WrapIndexA(extraFpsCorner - 1, 4);
                GUILayout.Label(GetFpsCornerNameA(extraFpsCorner), activeTabStyle != null ? activeTabStyle : btnStyle, GUILayout.Width(110), GUILayout.Height(24));
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                    extraFpsCorner = WrapIndexA(extraFpsCorner + 1, 4);
                GUILayout.EndHorizontal();
                GUILayout.Space(8);

                // --- Compass / distance ---
                GUILayout.Label(MenuText(L("Compass & Distance", "\u041A\u043E\u043C\u043F\u0430\u0441 \u0438 \u0440\u0430\u0441\u0441\u0442\u043E\u044F\u043D\u0438\u0435")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraCompass = DrawToggle(extraCompass, MenuText(L("Show Compass", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u043A\u043E\u043C\u043F\u0430\u0441")), 260);
                GUILayout.Label(MenuText(L("Compass bar at the top of the screen.", "\u041F\u043E\u043B\u043E\u0441\u0430 \u043A\u043E\u043C\u043F\u0430\u0441\u0430 \u0432\u0432\u0435\u0440\u0445\u0443 \u044D\u043A\u0440\u0430\u043D\u0430.")), menuDescStyle);
                extraPlayerDistance = DrawToggle(extraPlayerDistance, MenuText(L("Nearest Player Distance", "\u0420\u0430\u0441\u0441\u0442\u043E\u044F\u043D\u0438\u0435 \u0434\u043E \u0438\u0433\u0440\u043E\u043A\u0430")), 260);
                GUILayout.Label(MenuText(L("Shows meters to the closest alive player near the crosshair.", "\u041F\u043E\u043A\u0430\u0437\u044B\u0432\u0430\u0435\u0442 \u043C\u0435\u0442\u0440\u044B \u0434\u043E \u0431\u043B\u0438\u0436\u0430\u0439\u0448\u0435\u0433\u043E \u0436\u0438\u0432\u043E\u0433\u043E \u0438\u0433\u0440\u043E\u043A\u0430.")), menuDescStyle);
                GUILayout.Space(8);

                // --- Clock / stopwatch ---
                GUILayout.Label(MenuText(L("Session Clock", "\u0427\u0430\u0441\u044B \u0441\u0435\u0441\u0441\u0438\u0438")), subTabStyle != null ? subTabStyle : activeTabStyle);
                extraSessionClock = DrawToggle(extraSessionClock, MenuText(L("Show Time & Elapsed", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u0432\u0440\u0435\u043C\u044F \u0438 \u0442\u0430\u0439\u043C\u0435\u0440")), 260);
                extraSessionClock24h = DrawToggle(extraSessionClock24h, MenuText(L("24-Hour Format", "24-\u0447\u0430\u0441\u043E\u0432\u043E\u0439 \u0444\u043E\u0440\u043C\u0430\u0442")), 260);
                GUILayout.Label(MenuText(L("Local time and time since the menu started.", "\u041C\u0435\u0441\u0442\u043D\u043E\u0435 \u0432\u0440\u0435\u043C\u044F \u0438 \u0432\u0440\u0435\u043C\u044F \u0441 \u0437\u0430\u043F\u0443\u0441\u043A\u0430 \u043C\u0435\u043D\u044E.")), menuDescStyle);
            }
            catch { }
        }

        internal static void TickExtrasA(bool inputBlocked)
        {
            try
            {
                UpdateFpsA();

                if (extrasASessionStart < 0f)
                    extrasASessionStart = Time.realtimeSinceStartup;

                float now = Time.realtimeSinceStartup;
                if (now >= extrasAMetricsNext)
                {
                    extrasAMetricsNext = now + 0.12f;

                    extrasAYaw = ComputeYawA();
                    if (float.IsNaN(extrasAYaw)) extrasAYaw = 0f;
                    extrasAHeadingText = Mathf.RoundToInt(NormalizeAngleA(extrasAYaw)).ToString() + "\u00B0";

                    extrasASessionElapsedText = FormatElapsedA(now - extrasASessionStart);
                    extrasAClockText = extraSessionClock24h
                        ? DateTime.Now.ToString("HH:mm:ss")
                        : DateTime.Now.ToString("hh:mm:ss tt");

                    extrasADistanceText = extraPlayerDistance ? ComputeNearestPlayerDistanceA() : string.Empty;
                }

                // No hotkeys belong to this group; keep the input guard explicit so
                // hotkey handling (if ever added) is skipped while input is blocked.
                if (inputBlocked) return;
            }
            catch { }
        }

        internal static void LoadExtrasA()
        {
            try
            {
                extraCrosshair = LoadBoolA("M_ExtrasA_extraCrosshair", extraCrosshair);
                extraCrosshairSize = Mathf.Clamp(LoadIntA("M_ExtrasA_extraCrosshairSize", extraCrosshairSize), 4, 80);
                extraCrosshairColor = Mathf.Clamp(LoadIntA("M_ExtrasA_extraCrosshairColor", extraCrosshairColor), 0, 4);
                extraFpsCounter = LoadBoolA("M_ExtrasA_extraFpsCounter", extraFpsCounter);
                extraFpsCorner = Mathf.Clamp(LoadIntA("M_ExtrasA_extraFpsCorner", extraFpsCorner), 0, 3);
                extraCompass = LoadBoolA("M_ExtrasA_extraCompass", extraCompass);
                extraPlayerDistance = LoadBoolA("M_ExtrasA_extraPlayerDistance", extraPlayerDistance);
                extraSessionClock = LoadBoolA("M_ExtrasA_extraSessionClock", extraSessionClock);
                extraSessionClock24h = LoadBoolA("M_ExtrasA_extraSessionClock24h", extraSessionClock24h);
            }
            catch { }
        }

        internal static void SaveExtrasA()
        {
            try
            {
                SaveBoolA("M_ExtrasA_extraCrosshair", extraCrosshair);
                SaveIntA("M_ExtrasA_extraCrosshairSize", extraCrosshairSize);
                SaveIntA("M_ExtrasA_extraCrosshairColor", extraCrosshairColor);
                SaveBoolA("M_ExtrasA_extraFpsCounter", extraFpsCounter);
                SaveIntA("M_ExtrasA_extraFpsCorner", extraFpsCorner);
                SaveBoolA("M_ExtrasA_extraCompass", extraCompass);
                SaveBoolA("M_ExtrasA_extraPlayerDistance", extraPlayerDistance);
                SaveBoolA("M_ExtrasA_extraSessionClock", extraSessionClock);
                SaveBoolA("M_ExtrasA_extraSessionClock24h", extraSessionClock24h);
            }
            catch { }
        }

        internal static void DrawExtrasAOverlay()
        {
            // Skip everything when minimized / no window.
            if (Screen.width <= 1 || Screen.height <= 1) return;

            Event e = Event.current;
            if (e == null || e.type != EventType.Repaint) return;

            Color previousColor = GUI.color;
            try
            {
                EnsureStylesA();
                if (!extrasAStylesReady) return;

                if (extraCrosshair) DrawCrosshairA();
                if (extraFpsCounter) DrawFpsA();
                if (extraCompass) DrawCompassA();
                if (extraPlayerDistance) DrawDistanceA();
                if (extraSessionClock) DrawSessionClockA();
            }
            catch { }
            finally
            {
                GUI.color = previousColor;
            }
        }

        // ===================================================================
        // Private helpers (all suffixed with A)
        // ===================================================================

        private static void EnsureStylesA()
        {
            if (extrasAStylesReady && extrasAWhiteTex != null && extrasAHudStyle != null) return;
            try
            {
                if (extrasAWhiteTex == null)
                {
                    extrasAWhiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    extrasAWhiteTex.SetPixel(0, 0, Color.white);
                    extrasAWhiteTex.Apply();
                    extrasAWhiteTex.hideFlags = HideFlags.HideAndDontSave;
                }

                if (extrasAPanelTex == null)
                {
                    extrasAPanelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    extrasAPanelTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
                    extrasAPanelTex.Apply();
                    extrasAPanelTex.hideFlags = HideFlags.HideAndDontSave;
                }

                if (extrasAHudStyle == null)
                {
                    extrasAHudStyle = new GUIStyle(GUI.skin.label);
                    extrasAHudStyle.fontSize = 14;
                    extrasAHudStyle.fontStyle = FontStyle.Bold;
                    extrasAHudStyle.alignment = TextAnchor.MiddleLeft;
                    extrasAHudStyle.normal.textColor = Color.white;
                    extrasAHudStyle.clipping = TextClipping.Clip;
                    extrasAHudStyle.wordWrap = false;
                }

                if (extrasAHudCenterStyle == null)
                {
                    extrasAHudCenterStyle = new GUIStyle(extrasAHudStyle);
                    extrasAHudCenterStyle.alignment = TextAnchor.MiddleCenter;
                }

                if (extrasAPanelStyle == null)
                {
                    extrasAPanelStyle = new GUIStyle();
                    extrasAPanelStyle.normal.background = extrasAPanelTex;
                    extrasAPanelStyle.padding = new RectOffset { left = 6, right = 6, top = 3, bottom = 3 };
                }

                if (extrasACompassStyle == null)
                {
                    extrasACompassStyle = new GUIStyle(GUI.skin.label);
                    extrasACompassStyle.fontSize = 13;
                    extrasACompassStyle.fontStyle = FontStyle.Bold;
                    extrasACompassStyle.alignment = TextAnchor.MiddleCenter;
                    extrasACompassStyle.normal.textColor = Color.white;
                    extrasACompassStyle.clipping = TextClipping.Clip;
                    extrasACompassStyle.wordWrap = false;
                }

                extrasAStylesReady = true;
            }
            catch { }
        }

        private static int WrapIndexA(int index, int count)
        {
            if (count <= 0) return 0;
            int r = index % count;
            return r < 0 ? r + count : r;
        }

        private static string GetCrosshairColorNameA(int index)
        {
            switch (WrapIndexA(index, 5))
            {
                case 0: return L("White", "\u0411\u0435\u043B\u044B\u0439");
                case 1: return L("Red", "\u041A\u0440\u0430\u0441\u043D\u044B\u0439");
                case 2: return L("Green", "\u0417\u0435\u043B\u0451\u043D\u044B\u0439");
                case 3: return L("Cyan", "\u0413\u043E\u043B\u0443\u0431\u043E\u0439");
                default: return L("Yellow", "\u0416\u0451\u043B\u0442\u044B\u0439");
            }
        }

        private static string GetFpsCornerNameA(int index)
        {
            switch (WrapIndexA(index, 4))
            {
                case 0: return L("Top Left", "\u0421\u0432\u0435\u0440\u0445\u0443 \u0441\u043B\u0435\u0432\u0430");
                case 1: return L("Top Right", "\u0421\u0432\u0435\u0440\u0445\u0443 \u0441\u043F\u0440\u0430\u0432\u0430");
                case 2: return L("Bottom Left", "\u0421\u043D\u0438\u0437\u0443 \u0441\u043B\u0435\u0432\u0430");
                default: return L("Bottom Right", "\u0421\u043D\u0438\u0437\u0443 \u0441\u043F\u0440\u0430\u0432\u0430");
            }
        }

        private static bool LoadBoolA(string key, bool def)
        {
            try { return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) == 1 : def; }
            catch { return def; }
        }

        private static int LoadIntA(string key, int def)
        {
            try { return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : def; }
            catch { return def; }
        }

        private static void SaveBoolA(string key, bool value)
        {
            try { PlayerPrefs.SetInt(key, value ? 1 : 0); }
            catch { }
        }

        private static void SaveIntA(string key, int value)
        {
            try { PlayerPrefs.SetInt(key, value); }
            catch { }
        }

        private static PlayerControl GetLocalPlayerA()
        {
            try { return PlayerControl.LocalPlayer; }
            catch { return null; }
        }

        private static object ReadMemberA(object target, string name)
        {
            if (target == null) return null;
            try
            {
                Type type = target.GetType();
                FieldInfo field = type.GetField(name, ExtrasAFlags);
                if (field != null) return field.GetValue(target);
                PropertyInfo prop = type.GetProperty(name, ExtrasAFlags);
                if (prop != null) return prop.GetValue(target, null);
            }
            catch { }
            return null;
        }

        private static bool ReadBoolA(object target, string name, bool fallback)
        {
            object value = ReadMemberA(target, name);
            return value is bool b ? b : fallback;
        }

        private static bool IsAlivePlayerA(PlayerControl pc)
        {
            try
            {
                object data = ReadMemberA(pc, "Data");
                if (data == null) return false;
                if (ReadBoolA(data, "Disconnected", false)) return false;
                if (ReadBoolA(data, "IsDead", false)) return false;
                return true;
            }
            catch { return false; }
        }

        private static void UpdateFpsA()
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                if (extrasAFpsLastTime <= 0f)
                {
                    extrasAFpsLastTime = now;
                    extrasAFpsFrames = 0;
                    return;
                }

                extrasAFpsFrames++;
                float delta = now - extrasAFpsLastTime;
                if (delta >= 0.25f)
                {
                    float fps = extrasAFpsFrames / delta;
                    extrasAFpsSmooth = extrasAFpsSmooth <= 0f ? fps : Mathf.Lerp(extrasAFpsSmooth, fps, 0.5f);
                    extrasAFpsDisplay = Mathf.Clamp(Mathf.RoundToInt(extrasAFpsSmooth), 0, 9999);
                    extrasAFpsText = "FPS " + extrasAFpsDisplay.ToString();
                    extrasAFpsFrames = 0;
                    extrasAFpsLastTime = now;
                }
            }
            catch { }
        }

        private static float ComputeYawA()
        {
            try
            {
                PlayerControl local = GetLocalPlayerA();
                if (local == null) return extrasAYaw;

                Transform t = local.transform;
                if (t == null) return extrasAYaw;

                // Prefer the euler yaw; fall back to forward vector if it looks invalid.
                float yaw = t.eulerAngles.y;
                if (float.IsNaN(yaw) || float.IsInfinity(yaw))
                {
                    Vector3 forward = t.forward;
                    yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                }
                return yaw;
            }
            catch { return extrasAYaw; }
        }

        private static string ComputeNearestPlayerDistanceA()
        {
            try
            {
                PlayerControl local = GetLocalPlayerA();
                if (local == null) return string.Empty;

                Transform localTransform = local.transform;
                if (localTransform == null) return string.Empty;

                var all = PlayerControl.AllPlayerControls;
                if (all == null) return string.Empty;

                Vector3 origin = localTransform.position;
                float best = float.MaxValue;

                foreach (PlayerControl pc in all)
                {
                    if (pc == null || pc == local) continue;
                    if (!IsAlivePlayerA(pc)) continue;

                    Transform other = pc.transform;
                    if (other == null) continue;

                    float dist = Vector3.Distance(origin, other.position);
                    if (dist < best) best = dist;
                }

                if (best >= float.MaxValue) return string.Empty;
                return best.ToString("0.0") + " m";
            }
            catch { return string.Empty; }
        }

        private static float NormalizeAngleA(float angle)
        {
            angle = angle % 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        private static string FormatElapsedA(float seconds)
        {
            try
            {
                if (seconds < 0f || float.IsNaN(seconds)) seconds = 0f;
                int total = Mathf.FloorToInt(seconds);
                int hours = total / 3600;
                int minutes = (total % 3600) / 60;
                int secs = total % 60;
                return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + secs.ToString("00");
            }
            catch { return "00:00:00"; }
        }

        private static void DrawCrosshairA()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            int size = Mathf.Clamp(extraCrosshairSize, 4, 80);
            int colorIndex = WrapIndexA(extraCrosshairColor, extrasACrosshairColors.Length);
            Color color = extrasACrosshairColors[colorIndex];

            float thickness = Mathf.Max(1f, size * 0.09f);
            float arm = size * 0.5f;
            float gap = Mathf.Max(2f, size * 0.14f);

            GUI.color = color;
            float drawLen = Mathf.Max(1f, arm - gap);

            GUI.DrawTexture(new Rect(cx - arm, cy - thickness * 0.5f, drawLen, thickness), extrasAWhiteTex, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(cx + gap, cy - thickness * 0.5f, drawLen, thickness), extrasAWhiteTex, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - arm, thickness, drawLen), extrasAWhiteTex, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + gap, thickness, drawLen), extrasAWhiteTex, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - thickness * 0.5f, thickness, thickness), extrasAWhiteTex, ScaleMode.StretchToFill, true);
        }

        private static void DrawFpsA()
        {
            if (string.IsNullOrEmpty(extrasAFpsText)) return;
            DrawCornerPanelA(extrasAFpsText, extraFpsCorner);
        }

        private static void DrawCornerPanelA(string text, int corner)
        {
            float w = Mathf.Max(72f, 26f + text.Length * 9f);
            float h = 26f;
            float margin = 10f;

            int c = WrapIndexA(corner, 4);
            float x = (c == 1 || c == 3) ? Screen.width - w - margin : margin;
            float y = (c == 0 || c == 1) ? margin : Screen.height - h - margin;

            Rect rect = new Rect(x, y, w, h);
            GUI.color = Color.white;
            GUI.Box(rect, GUIContent.none, extrasAPanelStyle);
            GUI.Label(rect, text, extrasAHudStyle);
        }

        private static void DrawCompassA()
        {
            float barWidth = Mathf.Min(Screen.width - 40f, 560f);
            if (barWidth < 140f) return;

            const float barHeight = 30f;
            const float halfSpan = 90f;
            float barX = (Screen.width - barWidth) * 0.5f;
            float barY = 6f;
            float centerX = barX + barWidth * 0.5f;
            float pixelsPerDegree = (barWidth * 0.5f) / halfSpan;

            Rect bar = new Rect(barX, barY, barWidth, barHeight);
            GUI.color = Color.white;
            GUI.Box(bar, GUIContent.none, extrasAPanelStyle);

            float yaw = float.IsNaN(extrasAYaw) ? 0f : extrasAYaw;

            for (int i = 0; i < extrasACompassHeadings.Length && i < extrasACompassLabels.Length; i++)
            {
                float relative = NormalizeAngleA(extrasACompassHeadings[i] - yaw);
                if (relative < -halfSpan || relative > halfSpan) continue;

                float x = centerX + relative * pixelsPerDegree;
                GUI.Label(new Rect(x - 16f, barY + 3f, 32f, 18f), extrasACompassLabels[i], extrasACompassStyle);
            }

            // Needle fixed at the center marks the current heading.
            Color before = GUI.color;
            GUI.color = new Color(1f, 0.88f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(centerX - 1.5f, barY + 2f, 3f, barHeight - 4f), extrasAWhiteTex, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(new Rect(centerX - 5f, barY + barHeight - 6f, 10f, 4f), extrasAWhiteTex, ScaleMode.StretchToFill, true);
            GUI.color = before;

            GUI.Label(new Rect(centerX - 34f, barY + barHeight + 1f, 68f, 16f), extrasAHeadingText, extrasACompassStyle);
        }

        private static void DrawDistanceA()
        {
            if (string.IsNullOrEmpty(extrasADistanceText)) return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float w = Mathf.Max(58f, 22f + extrasADistanceText.Length * 8f);
            Rect rect = new Rect(cx + 18f, cy + 16f, w, 22f);

            GUI.color = Color.white;
            GUI.Box(rect, GUIContent.none, extrasAPanelStyle);
            GUI.Label(rect, extrasADistanceText, extrasAHudStyle);
        }

        private static void DrawSessionClockA()
        {
            string time = extrasAClockText;
            string text = string.IsNullOrEmpty(time)
                ? L("Session", "\u0421\u0435\u0441\u0441\u0438\u044F") + " " + extrasASessionElapsedText
                : time + "   |   " + L("Session", "\u0421\u0435\u0441\u0441\u0438\u044F") + " " + extrasASessionElapsedText;

            float w = Mathf.Max(190f, 34f + text.Length * 8.5f);
            // The compass heading label occupies y=37..53 when the compass is on.
            float y = extraCompass ? 54f : 6f;
            Rect rect = new Rect((Screen.width - w) * 0.5f, y, w, 26f);

            GUI.color = Color.white;
            GUI.Box(rect, GUIContent.none, extrasAPanelStyle);
            GUI.Label(rect, text, extrasAHudCenterStyle);
        }
    }
}
