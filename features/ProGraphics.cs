#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using UnityEngine;

namespace AeroMenu
{
    // ------------------------------------------------------------------------
    // Group M - Graphics (PRO). Inserted into the existing MENU tab.
    // All state is public static with the exact names required by the contract.
    // No Harmony patches: everything is driven from Tick + native Unity APIs.
    // ------------------------------------------------------------------------
    public partial class AeroMenuGUI : MonoBehaviour
    {
        public static bool proFpsLimit;
        public static int proFpsValue = 120;
        public static bool proVsync;
        public static bool proQualityOverride;
        public static int proQualityLevel = 3;
        public static bool proAntiAliasing;
        public static int proAaLevel = 4;
        public static bool proResolutionScale;
        public static float proResolutionScaleValue = 1f;

        // --------------------------------------------------------------------
        // Private working state (all suffixed with M)
        // --------------------------------------------------------------------
        private const int ProFpsMinM = 15;
        private const int ProFpsMaxM = 240;
        private const int ProQualityMinM = 0;
        private const int ProQualityMaxM = 5;
        private const float ProScaleMinM = 0.5f;
        private const float ProScaleMaxM = 1f;
        private static readonly int[] ProAaLevelsM = { 0, 2, 4, 8 };

        // Original values, captured ONCE, before we touch anything.
        private static bool proOriginalsCapturedM;
        private static int proFpsOriginalM = -1;
        private static int proVsyncOriginalM;
        private static int proQualityOriginalM = -1;
        private static int proAaOriginalM;
        private static int proResWidthOriginalM;
        private static int proResHeightOriginalM;
        private static bool proResFullscreenOriginalM;

        // Applied-state mirrors so Tick only acts on real transitions.
        private static bool proFpsAppliedM;
        private static bool proVsyncAppliedM;
        private static bool proQualityAppliedM;
        private static bool proAaAppliedM;
        private static bool proResAppliedM;

        private static int proFpsLastM;
        private static int proVsyncLastM = -1;
        private static int proQualityLastM = -1;
        private static int proAaLastM = -1;
        private static int proResLastWidthM;
        private static int proResLastHeightM;
        private static bool proResLastFullscreenM;

        // Lazily cached overlay resources (never allocated per frame).
        private static GUIStyle proOverlayStyleM;
        private static string proOverlayTextM = string.Empty;
        private static float proOverlayNextM;

        // Lazily cached instance style for numeric value labels.
        private GUIStyle proValueStyleM;

        // ====================================================================
        // Required public surface
        // ====================================================================

        internal void DrawProM(float width)
        {
            try
            {
                int toggleW = Mathf.Clamp(
                    Mathf.RoundToInt(width > 0f ? width : GetMenuWorkWidth(200f, 460f)), 170, 460);

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    DrawMenuSectionHeader(L("GRAPHICS (PRO)", "ГРАФИКА (PRO)"));

                    // ---- 1. FPS limit ---------------------------------------
                    bool fps = DrawToggle(proFpsLimit, L("FPS Limit", "Лимит FPS"), toggleW, "proFpsLimit");
                    if (fps != proFpsLimit)
                    {
                        proFpsLimit = fps;
                        NotifyProM("[GFX] FPS " + (fps ? "ON" : "OFF"));
                    }
                    proFpsValue = IntRowM(L("Target FPS", "Целевой FPS"), proFpsValue, ProFpsMinM, ProFpsMaxM);
                    GUILayout.Label(L("Limits Application.targetFrameRate. The original value is restored when disabled.",
                                      "Ограничивает Application.targetFrameRate. Исходное значение восстанавливается при выключении."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- 2. VSync -------------------------------------------
                    bool vsync = DrawToggle(proVsync, L("VSync", "Вертикальная синхронизация"), toggleW, "proVsync");
                    if (vsync != proVsync)
                    {
                        proVsync = vsync;
                        NotifyProM("[GFX] VSync " + (vsync ? "ON" : "OFF"));
                    }
                    GUILayout.Label(L("Sets QualitySettings.vSyncCount to 1 (on) or 0 (off). Restored when disabled.",
                                      "Устанавливает QualitySettings.vSyncCount в 1 (вкл) или 0 (выкл). Восстанавливается при выключении."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- 3. Quality override -------------------------------
                    bool quality = DrawToggle(proQualityOverride, L("Quality Override", "Переопределение качества"), toggleW, "proQualityOverride");
                    if (quality != proQualityOverride)
                    {
                        proQualityOverride = quality;
                        NotifyProM("[GFX] Quality " + (quality ? "ON" : "OFF"));
                    }
                    proQualityLevel = IntRowM(L("Quality Level", "Уровень качества"), proQualityLevel, ProQualityMinM, ProQualityMaxM);
                    GUILayout.Label(L("Applies QualitySettings.SetQualityLevel (clamped to the levels available in this build).",
                                      "Применяет QualitySettings.SetQualityLevel (ограничено доступными в сборке уровнями)."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- 4. Anti-aliasing ----------------------------------
                    bool aa = DrawToggle(proAntiAliasing, L("Anti-Aliasing", "Сглаживание"), toggleW, "proAntiAliasing");
                    if (aa != proAntiAliasing)
                    {
                        proAntiAliasing = aa;
                        NotifyProM("[GFX] AA " + (aa ? "ON" : "OFF"));
                    }
                    proAaLevel = AaRowM(proAaLevel);
                    GUILayout.Label(L("Sets QualitySettings.antiAliasing to 0 / 2 / 4 / 8. Restored when disabled.",
                                      "Устанавливает QualitySettings.antiAliasing в 0 / 2 / 4 / 8. Восстанавливается при выключении."), menuDescStyle);
                    GUILayout.Space(6);

                    // ---- 5. Resolution scale -------------------------------
                    bool scale = DrawToggle(proResolutionScale, L("Resolution Scale", "Масштаб разрешения"), toggleW, "proResolutionScale");
                    if (scale != proResolutionScale)
                    {
                        proResolutionScale = scale;
                        NotifyProM("[GFX] Scale " + (scale ? "ON" : "OFF"));
                    }
                    proResolutionScaleValue = FloatRowM(L("Scale", "Масштаб"), proResolutionScaleValue, ProScaleMinM, ProScaleMaxM);
                    GUILayout.Label(L("Renders at a scaled Screen.SetResolution. The original resolution is restored when disabled.",
                                      "Рендерит через Screen.SetResolution с масштабом. Исходное разрешение восстанавливается при выключении."), menuDescStyle);
                }
                finally
                {
                    GUILayout.EndVertical();
                }
            }
            catch { }
        }

        internal static void TickProM(bool inputBlocked)
        {
            // This group has no hotkeys - inputBlocked is intentionally unused.
            try { UpdateFpsM(); } catch { }
            try { UpdateVsyncM(); } catch { }
            try { UpdateQualityM(); } catch { }
            try { UpdateAntiAliasingM(); } catch { }
            try { UpdateResolutionM(); } catch { }
            try { UpdateOverlayTextM(); } catch { }
        }

        internal static void LoadProM()
        {
            try
            {
                proFpsLimit = LoadBool("M_ProM_proFpsLimit", false);
                proFpsValue = Mathf.Clamp(PlayerPrefs.GetInt("M_ProM_proFpsValue", 120), ProFpsMinM, ProFpsMaxM);

                proVsync = LoadBool("M_ProM_proVsync", false);

                proQualityOverride = LoadBool("M_ProM_proQualityOverride", false);
                proQualityLevel = Mathf.Clamp(PlayerPrefs.GetInt("M_ProM_proQualityLevel", 3), ProQualityMinM, ProQualityMaxM);

                proAntiAliasing = LoadBool("M_ProM_proAntiAliasing", false);
                proAaLevel = NormalizeAaM(PlayerPrefs.GetInt("M_ProM_proAaLevel", 4));

                proResolutionScale = LoadBool("M_ProM_proResolutionScale", false);
                proResolutionScaleValue = Mathf.Clamp(
                    PlayerPrefs.GetFloat("M_ProM_proResolutionScaleValue", 1f), ProScaleMinM, ProScaleMaxM);

                // Force Tick to re-evaluate every feature on the next frame.
                proFpsAppliedM = false;
                proVsyncAppliedM = false;
                proQualityAppliedM = false;
                proAaAppliedM = false;
                proResAppliedM = false;
                proFpsLastM = 0;
                proVsyncLastM = -1;
                proQualityLastM = -1;
                proAaLastM = -1;
                proResLastWidthM = 0;
                proResLastHeightM = 0;
            }
            catch { }
        }

        internal static void SaveProM()
        {
            try
            {
                SaveBool("M_ProM_proFpsLimit", proFpsLimit);
                PlayerPrefs.SetInt("M_ProM_proFpsValue", proFpsValue);

                SaveBool("M_ProM_proVsync", proVsync);

                SaveBool("M_ProM_proQualityOverride", proQualityOverride);
                PlayerPrefs.SetInt("M_ProM_proQualityLevel", proQualityLevel);

                SaveBool("M_ProM_proAntiAliasing", proAntiAliasing);
                PlayerPrefs.SetInt("M_ProM_proAaLevel", proAaLevel);

                SaveBool("M_ProM_proResolutionScale", proResolutionScale);
                PlayerPrefs.SetFloat("M_ProM_proResolutionScaleValue", proResolutionScaleValue);
            }
            catch { }
        }

        internal static void DrawProMOverlay()
        {
            try
            {
                if (Screen.width <= 1 || Screen.height <= 1) return;
                if (string.IsNullOrEmpty(proOverlayTextM)) return;

                EnsureOverlayStyleM();
                if (proOverlayStyleM == null) return;

                GUI.Label(new Rect(10f, 10f, 320f, 96f), proOverlayTextM, proOverlayStyleM);
            }
            catch { }
        }

        // ====================================================================
        // Original-value capture (once per session)
        // ====================================================================

        private static void EnsureOriginalsM()
        {
            if (proOriginalsCapturedM) return;
            try
            {
                try { proFpsOriginalM = Application.targetFrameRate; } catch { proFpsOriginalM = -1; }
                try { proVsyncOriginalM = QualitySettings.vSyncCount; } catch { proVsyncOriginalM = 0; }
                try { proQualityOriginalM = QualitySettings.GetQualityLevel(); } catch { proQualityOriginalM = -1; }
                try { proAaOriginalM = QualitySettings.antiAliasing; } catch { proAaOriginalM = 0; }
                try
                {
                    proResWidthOriginalM = Screen.width;
                    proResHeightOriginalM = Screen.height;
                    proResFullscreenOriginalM = Screen.fullScreen;
                }
                catch
                {
                    proResWidthOriginalM = 0;
                    proResHeightOriginalM = 0;
                    proResFullscreenOriginalM = false;
                }
                proOriginalsCapturedM = true;
            }
            catch { }
        }

        // ====================================================================
        // Per-feature apply / restore
        // ====================================================================

        private static void UpdateFpsM()
        {
            try
            {
                if (proFpsLimit)
                {
                    EnsureOriginalsM();
                    int target = Mathf.Clamp(proFpsValue, ProFpsMinM, ProFpsMaxM);
                    if (!proFpsAppliedM || proFpsLastM != target)
                    {
                        Application.targetFrameRate = target;
                        proFpsLastM = target;
                        proFpsAppliedM = true;
                    }
                }
                else if (proFpsAppliedM)
                {
                    Application.targetFrameRate = proFpsOriginalM;
                    proFpsAppliedM = false;
                    proFpsLastM = 0;
                }
            }
            catch { }
        }

        private static void UpdateVsyncM()
        {
            try
            {
                if (proVsync)
                {
                    EnsureOriginalsM();
                    int value = 1;
                    if (!proVsyncAppliedM || proVsyncLastM != value)
                    {
                        QualitySettings.vSyncCount = value;
                        proVsyncLastM = value;
                        proVsyncAppliedM = true;
                    }
                }
                else if (proVsyncAppliedM)
                {
                    QualitySettings.vSyncCount = proVsyncOriginalM;
                    proVsyncAppliedM = false;
                    proVsyncLastM = -1;
                }
            }
            catch { }
        }

        private static void UpdateQualityM()
        {
            try
            {
                if (proQualityOverride)
                {
                    EnsureOriginalsM();
                    int level = Mathf.Clamp(proQualityLevel, ProQualityMinM, ProQualityMaxM);
                    try
                    {
                        string[] names = QualitySettings.names;
                        if (names != null && names.Length > 0)
                            level = Mathf.Clamp(level, 0, names.Length - 1);
                    }
                    catch { }

                    if (!proQualityAppliedM || proQualityLastM != level)
                    {
                        QualitySettings.SetQualityLevel(level, true);
                        proQualityLastM = level;
                        proQualityAppliedM = true;
                    }
                }
                else if (proQualityAppliedM)
                {
                    if (proQualityOriginalM >= 0)
                        QualitySettings.SetQualityLevel(proQualityOriginalM, true);
                    proQualityAppliedM = false;
                    proQualityLastM = -1;
                }
            }
            catch { }
        }

        private static void UpdateAntiAliasingM()
        {
            try
            {
                if (proAntiAliasing)
                {
                    EnsureOriginalsM();
                    int level = NormalizeAaM(proAaLevel);
                    if (!proAaAppliedM || proAaLastM != level)
                    {
                        QualitySettings.antiAliasing = level;
                        proAaLastM = level;
                        proAaAppliedM = true;
                    }
                }
                else if (proAaAppliedM)
                {
                    QualitySettings.antiAliasing = proAaOriginalM;
                    proAaAppliedM = false;
                    proAaLastM = -1;
                }
            }
            catch { }
        }

        private static void UpdateResolutionM()
        {
            try
            {
                if (proResolutionScale)
                {
                    EnsureOriginalsM();
                    if (proResWidthOriginalM <= 0 || proResHeightOriginalM <= 0) return;

                    float scale = Mathf.Clamp(proResolutionScaleValue, ProScaleMinM, ProScaleMaxM);
                    int w = Mathf.Max(64, Mathf.RoundToInt(proResWidthOriginalM * scale));
                    int h = Mathf.Max(64, Mathf.RoundToInt(proResHeightOriginalM * scale));

                    if (!proResAppliedM || proResLastWidthM != w || proResLastHeightM != h ||
                        proResLastFullscreenM != proResFullscreenOriginalM)
                    {
                        Screen.SetResolution(w, h, proResFullscreenOriginalM);
                        proResLastWidthM = w;
                        proResLastHeightM = h;
                        proResLastFullscreenM = proResFullscreenOriginalM;
                        proResAppliedM = true;
                    }
                }
                else if (proResAppliedM)
                {
                    if (proResWidthOriginalM > 0 && proResHeightOriginalM > 0)
                        Screen.SetResolution(proResWidthOriginalM, proResHeightOriginalM, proResFullscreenOriginalM);
                    proResAppliedM = false;
                    proResLastWidthM = 0;
                    proResLastHeightM = 0;
                }
            }
            catch { }
        }

        // ====================================================================
        // Value helpers
        // ====================================================================

        private static int NormalizeAaM(int value)
        {
            if (value <= 0) return 0;
            if (value < 3) return 2;
            if (value < 6) return 4;
            return 8;
        }

        private static int AaIndexFromLevelM(int level)
        {
            int n = NormalizeAaM(level);
            for (int i = 0; i < ProAaLevelsM.Length; i++)
                if (ProAaLevelsM[i] == n) return i;
            return 2;
        }

        private static int AaLevelFromIndexM(int index)
        {
            int i = Mathf.Clamp(index, 0, ProAaLevelsM.Length - 1);
            return ProAaLevelsM[i];
        }

        private static void NotifyProM(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text)) ShowNotification(text);
            }
            catch { }
        }

        // ====================================================================
        // UI helpers (instance only - they touch instance GUIStyles)
        // ====================================================================

        private GUIStyle ValueStyleM()
        {
            try
            {
                if (proValueStyleM == null)
                {
                    GUIStyle src = menuDescStyle ?? (GUI.skin != null ? GUI.skin.label : null);
                    proValueStyleM = src != null ? new GUIStyle(src) : new GUIStyle();
                    proValueStyleM.alignment = TextAnchor.MiddleRight;
                    proValueStyleM.clipping = TextClipping.Clip;
                    proValueStyleM.wordWrap = false;
                }
            }
            catch { proValueStyleM = null; }
            return proValueStyleM;
        }

        private int IntRowM(string label, int value, int min, int max)
        {
            try
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(MenuText(label), menuDescStyle, GUILayout.Width(120));
                float f = GUILayout.HorizontalSlider((float)value, (float)min, (float)max);
                GUILayout.Label(Mathf.RoundToInt(f).ToString(), ValueStyleM(), GUILayout.Width(38));
                GUILayout.EndHorizontal();
                return Mathf.Clamp(Mathf.RoundToInt(f), min, max);
            }
            catch { return value; }
        }

        private float FloatRowM(string label, float value, float min, float max)
        {
            try
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(MenuText(label), menuDescStyle, GUILayout.Width(120));
                float f = GUILayout.HorizontalSlider(value, min, max);
                GUILayout.Label(f.ToString("0.00"), ValueStyleM(), GUILayout.Width(38));
                GUILayout.EndHorizontal();
                return Mathf.Clamp(f, min, max);
            }
            catch { return value; }
        }

        private int AaRowM(int level)
        {
            try
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(MenuText(L("AA Samples", "Сэмплы AA")), menuDescStyle, GUILayout.Width(120));
                int index = AaIndexFromLevelM(level);
                float f = GUILayout.HorizontalSlider((float)index, 0f, (float)(ProAaLevelsM.Length - 1));
                int next = AaLevelFromIndexM(Mathf.RoundToInt(f));
                GUILayout.Label(next == 0 ? "OFF" : (next + "x"), ValueStyleM(), GUILayout.Width(38));
                GUILayout.EndHorizontal();
                return next;
            }
            catch { return level; }
        }

        // ====================================================================
        // Overlay state / lazy style
        // ====================================================================

        private static void EnsureOverlayStyleM()
        {
            try
            {
                if (proOverlayStyleM != null) return;
                GUIStyle src = GUI.skin != null ? GUI.skin.label : null;
                proOverlayStyleM = src != null ? new GUIStyle(src) : new GUIStyle();
                proOverlayStyleM.fontSize = 12;
                proOverlayStyleM.richText = true;
                proOverlayStyleM.wordWrap = false;
                proOverlayStyleM.clipping = TextClipping.Clip;
                proOverlayStyleM.normal.textColor = new Color(0.60f, 1f, 0.78f, 1f);
            }
            catch { proOverlayStyleM = null; }
        }

        private static void UpdateOverlayTextM()
        {
            try
            {
                if (Time.unscaledTime < proOverlayNextM) return;
                proOverlayNextM = Time.unscaledTime + 0.25f;

                string text = string.Empty;
                if (proFpsLimit)
                    text += "[GFX] FPS " + Mathf.Clamp(proFpsValue, ProFpsMinM, ProFpsMaxM) + "\n";
                if (proVsync)
                    text += "[GFX] VSync ON\n";
                if (proQualityOverride)
                    text += "[GFX] Quality " + Mathf.Clamp(proQualityLevel, ProQualityMinM, ProQualityMaxM) + "\n";
                if (proAntiAliasing)
                    text += "[GFX] AA " + NormalizeAaM(proAaLevel) + "x\n";
                if (proResolutionScale)
                    text += "[GFX] Scale " + Mathf.Clamp(proResolutionScaleValue, ProScaleMinM, ProScaleMaxM).ToString("0.00") + "\n";

                proOverlayTextM = text.TrimEnd('\n', '\r');
            }
            catch { }
        }
    }
}
