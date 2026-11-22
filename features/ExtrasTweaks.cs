#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Globalization;
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
        // --------------------------------------------------------------------
        // Group C state (exact field names required by the contract)
        // --------------------------------------------------------------------
        public static bool extraHideHud;
        public static bool extraNoVignette;
        public static bool extraCustomFov;
        public static float extraFovValue;
        public static bool extraFreeCursor;
        public static bool extraFullscreenState;

        // --------------------------------------------------------------------
        // Private working state (all suffixed with C)
        // --------------------------------------------------------------------
        private const float FovMinC = 20f;
        private const float FovMaxC = 170f;

        // HUD: original active state of every GameObject we touched.
        private static readonly Dictionary<GameObject, bool> hudSavedStateC = new Dictionary<GameObject, bool>();

        // Vignette / post-process: original enabled state of every component we touched.
        private static readonly Dictionary<MonoBehaviour, bool> vignetteSavedStateC = new Dictionary<MonoBehaviour, bool>();
        private static float vignetteNextScanC;

        // FOV: original fieldOfView of every camera we touched.
        private static readonly Dictionary<Camera, float> fovSavedStateC = new Dictionary<Camera, float>();

        // Free cursor: values captured before we took control.
        private static CursorLockMode cursorSavedLockC = CursorLockMode.None;
        private static bool cursorSavedVisibleC = true;
        private static bool cursorCapturedC;
        private static bool cursorAppliedC;

        // Applied-state mirrors so Tick only reacts on real transitions.
        private static bool hudAppliedC;
        private static bool vignetteAppliedC;
        private static bool fovAppliedC;

        // Text buffer for the FOV input field (kept stable while typing).
        private static string fovTextC = "60";

        private static readonly string[] HudFieldNamesC =
        {
            "TaskBar", "Chat", "KillButton", "UseButton", "Vitals",
            "ReportButton", "ImpostorVentButton", "VentButton", "SabotageButton",
            "MapButton", "TaskButton", "CallMeetingButton", "MeetingButton"
        };

        // ====================================================================
        // Public surface
        // ====================================================================

        internal void DrawExtrasC(float width)
        {
            int toggleW = Mathf.Clamp(Mathf.RoundToInt(width > 0f ? width : 260f), 170, 460);
            GUIStyle buttonStyle = btnStyle ?? GUI.skin.button;
            GUIStyle fieldStyle = inputBlockStyle ?? GUI.skin.textField;

            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                DrawMenuSectionHeader(L("DISPLAY TWEAKS", "НАСТРОЙКИ ЭКРАНА"));

                // 11 - Hide HUD -------------------------------------------------
                bool hideHud = DrawToggle(extraHideHud, L("Hide HUD", "Скрыть HUD"), toggleW, "extrasHideHud");
                if (hideHud != extraHideHud) extraHideHud = hideHud;
                GUILayout.Label(L("Hides HUD elements (tasks, chat, buttons, vitals). Original states are restored when disabled.",
                                  "Скрывает элементы HUD (таски, чат, кнопки, виталки). Исходное состояние восстанавливается при выключении."), menuDescStyle);
                GUILayout.Space(8);

                // 12 - No vignette ---------------------------------------------
                bool noVignette = DrawToggle(extraNoVignette, L("No Vignette", "Без виньетки"), toggleW, "extrasNoVignette");
                if (noVignette != extraNoVignette) extraNoVignette = noVignette;
                GUILayout.Label(L("Disables vignette / post-process components found at runtime.",
                                  "Отключает компоненты виньетки / пост-обработки, найденные во время игры."), menuDescStyle);
                GUILayout.Space(8);

                // 13 - Custom FOV ----------------------------------------------
                bool customFov = DrawToggle(extraCustomFov, L("Custom FOV", "Свой угол обзора"), toggleW, "extrasCustomFov");
                if (customFov != extraCustomFov) extraCustomFov = customFov;

                GUILayout.BeginHorizontal();
                GUILayout.Label(L("FOV:", "Угол:"), GUILayout.Width(46));
                string typed = AeroTextField(fovTextC, fieldStyle, GUILayout.Width(64));
                if (!string.Equals(typed, fovTextC, StringComparison.Ordinal))
                {
                    fovTextC = typed ?? string.Empty;
                    float parsed;
                    if (float.TryParse(fovTextC, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                        extraFovValue = Mathf.Clamp(parsed, FovMinC, FovMaxC);
                }
                GUILayout.Label("[" + FormatFovC(extraFovValue) + "]", GUILayout.Width(56));
                GUILayout.EndHorizontal();
                GUILayout.Label(L("Field of view (" + FovMinC.ToString("0", CultureInfo.InvariantCulture) + "-" +
                                  FovMaxC.ToString("0", CultureInfo.InvariantCulture) + "). Restored when disabled.",
                                  "Угол обзора (" + FovMinC.ToString("0", CultureInfo.InvariantCulture) + "-" +
                                  FovMaxC.ToString("0", CultureInfo.InvariantCulture) + "). Восстанавливается при выключении."), menuDescStyle);
                GUILayout.Space(8);

                // 14 - Free cursor ---------------------------------------------
                bool freeCursor = DrawToggle(extraFreeCursor, L("Free Cursor", "Свободный курсор"), toggleW, "extrasFreeCursor");
                if (freeCursor != extraFreeCursor) extraFreeCursor = freeCursor;
                GUILayout.Label(L("Unlocks the mouse cursor while enabled, restores the previous state when disabled.",
                                  "Разблокирует курсор при включении, восстанавливает предыдущее состояние при выключении."), menuDescStyle);
                GUILayout.Space(8);

                // 15 - Fullscreen button --------------------------------------
                DrawMenuSectionHeader(L("WINDOW MODE", "РЕЖИМ ОКНА"));
                if (GUILayout.Button(L("Toggle Fullscreen", "Переключить полный экран"), buttonStyle, GUILayout.Height(26)))
                {
                    try
                    {
                        Screen.fullScreen = !Screen.fullScreen;
                        extraFullscreenState = Screen.fullScreen;
                        ShowNotification(L("[SCREEN] Fullscreen: ", "[ЭКРАН] Полный экран: ") +
                                         (extraFullscreenState ? "ON" : "OFF"));
                    }
                    catch { }
                }
                GUILayout.Label(L("Current: ", "Сейчас: ") +
                                (extraFullscreenState ? L("Fullscreen", "Полный экран") : L("Windowed", "Оконный режим")), menuDescStyle);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        internal static void TickExtrasC(bool inputBlocked)
        {
            // No hotkeys in this group - inputBlocked is intentionally unused.
            try
            {
                bool hideHud = extraHideHud;
                if (hideHud != hudAppliedC || hideHud)
                {
                    ApplyHideHudC(hideHud);
                    hudAppliedC = hideHud;
                }

                bool noVignette = extraNoVignette;
                if (noVignette != vignetteAppliedC || noVignette)
                {
                    ApplyNoVignetteC(noVignette);
                    vignetteAppliedC = noVignette;
                }

                bool customFov = extraCustomFov;
                if (customFov != fovAppliedC || customFov)
                {
                    ApplyFovC(customFov);
                    fovAppliedC = customFov;
                }

                ApplyFreeCursorC(extraFreeCursor);
            }
            catch { }
        }

        internal static void LoadExtrasC()
        {
            try
            {
                extraHideHud = LoadBool("M_ExtrasC_extraHideHud", false);
                extraNoVignette = LoadBool("M_ExtrasC_extraNoVignette", false);
                extraCustomFov = LoadBool("M_ExtrasC_extraCustomFov", false);

                float defaultFov = 60f;
                try { if (Camera.main != null) defaultFov = Camera.main.fieldOfView; } catch { }
                extraFovValue = Mathf.Clamp(PlayerPrefs.GetFloat("M_ExtrasC_extraFovValue", defaultFov), FovMinC, FovMaxC);
                fovTextC = FormatFovC(extraFovValue);

                extraFreeCursor = LoadBool("M_ExtrasC_extraFreeCursor", false);

                bool fsDefault = false;
                try { fsDefault = Screen.fullScreen; } catch { }
                extraFullscreenState = LoadBool("M_ExtrasC_extraFullscreenState", fsDefault);

                // Force Tick to re-evaluate on the next frame from a clean slate.
                hudAppliedC = false;
                vignetteAppliedC = false;
                fovAppliedC = false;
                cursorAppliedC = false;
                cursorCapturedC = false;

                // Restore the remembered fullscreen preference (best effort).
                try
                {
                    if (Screen.fullScreen != extraFullscreenState)
                        Screen.fullScreen = extraFullscreenState;
                }
                catch { }
            }
            catch { }
        }

        internal static void SaveExtrasC()
        {
            try
            {
                SaveBool("M_ExtrasC_extraHideHud", extraHideHud);
                SaveBool("M_ExtrasC_extraNoVignette", extraNoVignette);
                SaveBool("M_ExtrasC_extraCustomFov", extraCustomFov);
                PlayerPrefs.SetFloat("M_ExtrasC_extraFovValue", extraFovValue);
                SaveBool("M_ExtrasC_extraFreeCursor", extraFreeCursor);
                SaveBool("M_ExtrasC_extraFullscreenState", extraFullscreenState);
            }
            catch { }
        }

        internal static void DrawExtrasCOverlay()
        {
            // Group C has no full-screen overlay drawing. All features are
            // world/state tweaks drawn from their native Unity components.
        }

        // ====================================================================
        // Feature 11 - Hide HUD
        // ====================================================================

        private static void ApplyHideHudC(bool hide)
        {
            try
            {
                if (hide)
                {
                    List<GameObject> objects = FindHudObjectsC();

                    // Drop destroyed references from the cache.
                    if (hudSavedStateC.Count > 0)
                    {
                        List<GameObject> dead = new List<GameObject>();
                        foreach (var kv in hudSavedStateC)
                            if (kv.Key == null) dead.Add(kv.Key);
                        for (int i = 0; i < dead.Count; i++) hudSavedStateC.Remove(dead[i]);
                    }

                    for (int i = 0; i < objects.Count; i++)
                    {
                        GameObject go = objects[i];
                        if (go == null) continue;
                        try
                        {
                            if (!hudSavedStateC.ContainsKey(go))
                                hudSavedStateC[go] = go.activeSelf;
                            if (go.activeSelf)
                                go.SetActive(false);
                        }
                        catch { }
                    }
                }
                else
                {
                    RestoreHudC();
                }
            }
            catch { }
        }

        private static void RestoreHudC()
        {
            try
            {
                foreach (var kv in hudSavedStateC)
                {
                    try
                    {
                        if (kv.Key != null && kv.Key.activeSelf != kv.Value)
                            kv.Key.SetActive(kv.Value);
                    }
                    catch { }
                }
                hudSavedStateC.Clear();
            }
            catch { }
        }

        private static List<GameObject> FindHudObjectsC()
        {
            List<GameObject> result = new List<GameObject>();
            try
            {
                var hud = HudManager.Instance;
                if (hud == null) return result;

                FieldInfo[] fields = hud.GetType().GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fields == null) return result;

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    try
                    {
                        if (!IsHudFieldNameC(field.Name)) continue;
                        object value = field.GetValue(hud);
                        GameObject go = ExtractGameObjectC(value);
                        if (go != null && !result.Contains(go)) result.Add(go);
                    }
                    catch { }
                }
            }
            catch { }
            return result;
        }

        private static bool IsHudFieldNameC(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < HudFieldNamesC.Length; i++)
            {
                if (string.Equals(name, HudFieldNamesC[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            string n = name.ToLowerInvariant();
            return n.Contains("task") || n.Contains("chat") || n.Contains("kill") ||
                   n.Contains("vitals") || n.Contains("vital") || n.Contains("report") ||
                   n.Contains("vent") || n.Contains("sabotage") || n.Contains("meeting") ||
                   n.Contains("hud") || n.Contains("button");
        }

        private static GameObject ExtractGameObjectC(object value)
        {
            try
            {
                GameObject go = value as GameObject;
                if (go != null) return go;

                Component component = value as Component;
                if (component != null) return component.gameObject;
            }
            catch { }
            return null;
        }

        // ====================================================================
        // Feature 12 - No vignette / post-process
        // ====================================================================

        private static void ApplyNoVignetteC(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    if (vignetteSavedStateC.Count == 0 || Time.unscaledTime >= vignetteNextScanC)
                    {
                        vignetteNextScanC = Time.unscaledTime + 0.75f;
                        ScanAndDisableVignetteC();
                    }

                    // Keep them disabled even if something re-enables them.
                    foreach (var kv in vignetteSavedStateC)
                    {
                        try
                        {
                            if (kv.Key != null && kv.Key.enabled)
                                kv.Key.enabled = false;
                        }
                        catch { }
                    }
                }
                else
                {
                    RestoreVignetteC();
                }
            }
            catch { }
        }

        private static void ScanAndDisableVignetteC()
        {
            try
            {
                if (vignetteSavedStateC.Count > 0)
                {
                    List<MonoBehaviour> dead = new List<MonoBehaviour>();
                    foreach (var kv in vignetteSavedStateC)
                        if (kv.Key == null) dead.Add(kv.Key);
                    for (int i = 0; i < dead.Count; i++) vignetteSavedStateC.Remove(dead[i]);
                }

                MonoBehaviour[] all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                if (all == null) return;

                for (int i = 0; i < all.Length; i++)
                {
                    MonoBehaviour mb = all[i];
                    if (mb == null) continue;

                    string typeName;
                    try { typeName = mb.GetType().Name; } catch { continue; }
                    if (string.IsNullOrEmpty(typeName)) continue;
                    if (typeName.IndexOf("Vignette", StringComparison.OrdinalIgnoreCase) < 0 &&
                        typeName.IndexOf("PostProcess", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    try
                    {
                        if (!vignetteSavedStateC.ContainsKey(mb))
                            vignetteSavedStateC[mb] = mb.enabled;
                        if (mb.enabled)
                            mb.enabled = false;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void RestoreVignetteC()
        {
            try
            {
                foreach (var kv in vignetteSavedStateC)
                {
                    try
                    {
                        if (kv.Key != null && kv.Key.enabled != kv.Value)
                            kv.Key.enabled = kv.Value;
                    }
                    catch { }
                }
                vignetteSavedStateC.Clear();
            }
            catch { }
        }

        // ====================================================================
        // Feature 13 - Custom FOV
        // ====================================================================

        private static void ApplyFovC(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    float target = Mathf.Clamp(extraFovValue, FovMinC, FovMaxC);
                    List<Camera> cameras = GetAllCamerasC();

                    for (int i = 0; i < cameras.Count; i++)
                    {
                        Camera cam = cameras[i];
                        if (cam == null) continue;
                        try
                        {
                            if (!fovSavedStateC.ContainsKey(cam))
                                fovSavedStateC[cam] = cam.fieldOfView;
                            if (!Mathf.Approximately(cam.fieldOfView, target))
                                cam.fieldOfView = target;
                        }
                        catch { }
                    }
                }
                else
                {
                    RestoreFovC();
                }
            }
            catch { }
        }

        private static List<Camera> GetAllCamerasC()
        {
            List<Camera> cameras = new List<Camera>();
            try
            {
                try
                {
                    Camera main = Camera.main;
                    if (main != null) cameras.Add(main);
                }
                catch { }

                try
                {
                    Camera[] all = Camera.allCameras;
                    if (all != null)
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            Camera cam = all[i];
                            if (cam != null && !cameras.Contains(cam)) cameras.Add(cam);
                        }
                    }
                }
                catch { }
            }
            catch { }
            return cameras;
        }

        private static void RestoreFovC()
        {
            try
            {
                foreach (var kv in fovSavedStateC)
                {
                    try
                    {
                        if (kv.Key != null && !Mathf.Approximately(kv.Key.fieldOfView, kv.Value))
                            kv.Key.fieldOfView = kv.Value;
                    }
                    catch { }
                }
                fovSavedStateC.Clear();
            }
            catch { }
        }

        // ====================================================================
        // Feature 14 - Free cursor
        // ====================================================================

        private static void ApplyFreeCursorC(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    if (!cursorCapturedC)
                    {
                        cursorSavedLockC = Cursor.lockState;
                        cursorSavedVisibleC = Cursor.visible;
                        cursorCapturedC = true;
                    }

                    if (Cursor.lockState != CursorLockMode.None)
                        Cursor.lockState = CursorLockMode.None;
                    if (!Cursor.visible)
                        Cursor.visible = true;
                    cursorAppliedC = true;
                }
                else if (cursorAppliedC)
                {
                    Cursor.lockState = cursorSavedLockC;
                    Cursor.visible = cursorSavedVisibleC;
                    cursorAppliedC = false;
                    cursorCapturedC = false;
                }
            }
            catch { }
        }

        // ====================================================================
        // Shared helpers
        // ====================================================================

        private static string FormatFovC(float value)
        {
            try { return Mathf.Clamp(value, FovMinC, FovMaxC).ToString("0.#", CultureInfo.InvariantCulture); }
            catch { return "60"; }
        }
    }
}
