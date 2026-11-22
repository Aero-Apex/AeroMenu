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
        // --------------------------------------------------------------------
        // Group K - Diagnostics. Public feature flags (exact contract names).
        // --------------------------------------------------------------------
        public static bool extraFrameTimeGraph;
        public static bool extraFpsMinMax;
        public static bool extraMemoryUsage;
        public static bool extraBuildInfoPanel;
        public static bool extraOpenConfigFolder;

        // --------------------------------------------------------------------
        // Internal state. All names end with K to avoid partial-class clashes.
        // --------------------------------------------------------------------
        private const int FrameHistoryLengthK = 120;
        private static readonly float[] frameTimeHistoryK = new float[FrameHistoryLengthK];
        private static int frameTimeIndexK;
        private static int frameTimeCountK;

        private static float fpsMinK;
        private static float fpsMaxK;
        private static bool fpsStatsValidK;

        private static float cachedMemoryMbK;
        private static float lastMemoryRefreshK = -1f;

        private static Texture2D frameGraphTextureK;
        private static GUIStyle extrasOverlayLabelStyleK;
        private static GUIStyle extrasPanelBoxStyleK;

        // ====================================================================
        // Required public surface
        // ====================================================================

        internal void DrawExtrasK(float width)
        {
            float workWidth = width > 40f ? width : GetMenuWorkWidth(260f, 520f);
            int toggleWidth = Mathf.Max(120, Mathf.RoundToInt(workWidth) - 14);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(workWidth));
            GUILayout.Label(MenuText("<b>Diagnostics</b>"), headerStyle);
            GUILayout.Space(4f);

            bool v = DrawToggle(extraFrameTimeGraph, L("Frame time graph (ms)", "Frame time graph (ms)"), toggleWidth);
            if (v != extraFrameTimeGraph) { extraFrameTimeGraph = v; settingsDirty = true; }

            v = DrawToggle(extraFpsMinMax, L("FPS min / max tracker", "FPS min / max tracker"), toggleWidth);
            if (v != extraFpsMinMax) { extraFpsMinMax = v; settingsDirty = true; }

            v = DrawToggle(extraMemoryUsage, L("Managed memory usage", "Managed memory usage"), toggleWidth);
            if (v != extraMemoryUsage) { extraMemoryUsage = v; settingsDirty = true; }

            v = DrawToggle(extraBuildInfoPanel, L("Build info panel", "Build info panel"), toggleWidth);
            if (v != extraBuildInfoPanel) { extraBuildInfoPanel = v; settingsDirty = true; }

            v = DrawToggle(extraOpenConfigFolder, L("Config folder opener", "Config folder opener"), toggleWidth);
            if (v != extraOpenConfigFolder) { extraOpenConfigFolder = v; settingsDirty = true; }

            GUILayout.Space(6f);

            if (extraFpsMinMax)
            {
                GUILayout.Label(MenuText(FormatFpsStatsK()), menuDescStyle);
                if (GUILayout.Button(MenuText(L("Reset FPS Min/Max", "Reset FPS Min/Max")), btnStyle, GUILayout.Height(24f)))
                    ResetFpsStatsK();
            }

            if (extraMemoryUsage)
            {
                RefreshMemoryK();
                GUILayout.Label(MenuText($"Managed memory: {cachedMemoryMbK:0.0} MB"), menuDescStyle);
            }

            if (extraBuildInfoPanel)
            {
                string[] lines = GetBuildInfoLinesK();
                for (int i = 0; i < lines.Length; i++)
                    GUILayout.Label(MenuText(lines[i]), menuDescStyle);
            }

            if (extraOpenConfigFolder)
            {
                if (GUILayout.Button(MenuText(L("Open Config Folder", "Open Config Folder")), btnStyle, GUILayout.Height(24f)))
                    OpenConfigFolderK();
            }

            GUILayout.EndVertical();
        }

        internal static void TickExtrasK(bool inputBlocked)
        {
            try
            {
                // Group K has no hotkeys, so inputBlocked is intentionally ignored.
                float ms = Time.unscaledDeltaTime * 1000f;
                if (ms < 0f || ms > 10000f) ms = 0f;

                if (extraFrameTimeGraph && ms > 0f)
                {
                    frameTimeHistoryK[frameTimeIndexK] = ms;
                    frameTimeIndexK = (frameTimeIndexK + 1) % FrameHistoryLengthK;
                    if (frameTimeCountK < FrameHistoryLengthK) frameTimeCountK++;
                }

                if (extraFpsMinMax && ms > 0.001f)
                {
                    float fps = 1000f / ms;
                    if (fps > 10000f) fps = 10000f;
                    if (!fpsStatsValidK)
                    {
                        fpsMinK = fps;
                        fpsMaxK = fps;
                        fpsStatsValidK = true;
                    }
                    else
                    {
                        if (fps < fpsMinK) fpsMinK = fps;
                        if (fps > fpsMaxK) fpsMaxK = fps;
                    }
                }

                if (extraMemoryUsage) RefreshMemoryK();
            }
            catch { }
        }

        internal static void LoadExtrasK()
        {
            try { extraFrameTimeGraph = PlayerPrefs.GetInt("M_ExtrasK_extraFrameTimeGraph", 0) != 0; } catch { }
            try { extraFpsMinMax = PlayerPrefs.GetInt("M_ExtrasK_extraFpsMinMax", 0) != 0; } catch { }
            try { extraMemoryUsage = PlayerPrefs.GetInt("M_ExtrasK_extraMemoryUsage", 0) != 0; } catch { }
            try { extraBuildInfoPanel = PlayerPrefs.GetInt("M_ExtrasK_extraBuildInfoPanel", 0) != 0; } catch { }
            try { extraOpenConfigFolder = PlayerPrefs.GetInt("M_ExtrasK_extraOpenConfigFolder", 0) != 0; } catch { }

            ResetFpsStatsK();
            frameTimeIndexK = 0;
            frameTimeCountK = 0;
            lastMemoryRefreshK = -1f;
        }

        internal static void SaveExtrasK()
        {
            try { PlayerPrefs.SetInt("M_ExtrasK_extraFrameTimeGraph", extraFrameTimeGraph ? 1 : 0); } catch { }
            try { PlayerPrefs.SetInt("M_ExtrasK_extraFpsMinMax", extraFpsMinMax ? 1 : 0); } catch { }
            try { PlayerPrefs.SetInt("M_ExtrasK_extraMemoryUsage", extraMemoryUsage ? 1 : 0); } catch { }
            try { PlayerPrefs.SetInt("M_ExtrasK_extraBuildInfoPanel", extraBuildInfoPanel ? 1 : 0); } catch { }
            try { PlayerPrefs.SetInt("M_ExtrasK_extraOpenConfigFolder", extraOpenConfigFolder ? 1 : 0); } catch { }
        }

        internal static void DrawExtrasKOverlay()
        {
            if (!extraFrameTimeGraph && !extraFpsMinMax && !extraMemoryUsage && !extraBuildInfoPanel) return;

            try
            {
                EnsureExtrasStylesK();
                float sw = Screen.width;
                float sh = Screen.height;

                if (extraFrameTimeGraph)
                {
                    UpdateFrameGraphTextureK();
                    Rect graphRect = new Rect(10f, sh - 112f, 240f, 90f);
                    DrawPanelK(new Rect(graphRect.x - 4f, graphRect.y - 20f, graphRect.width + 8f, graphRect.height + 26f));
                    DrawLabelK(new Rect(graphRect.x, graphRect.y - 18f, graphRect.width, 18f), "Frame time (ms)");
                    Texture2D tex = frameGraphTextureK;
                    if (tex != null)
                    {
                        try { GUI.DrawTexture(graphRect, tex, ScaleMode.StretchToFill, true); }
                        catch { }
                    }
                    if (frameTimeCountK <= 0)
                        DrawLabelK(new Rect(graphRect.x + 6f, graphRect.y + 6f, graphRect.width - 12f, 18f), "collecting...");
                }

                if (extraFpsMinMax)
                {
                    // Sit fully above the frame-time graph panel frame (top at sh - 132).
                    Rect panel = new Rect(6f, sh - 164f, 248f, 28f);
                    DrawPanelK(panel);
                    DrawLabelK(new Rect(panel.x + 6f, panel.y + 4f, panel.width - 12f, 20f), FormatFpsStatsK());
                }

                if (extraMemoryUsage)
                {
                    RefreshMemoryK();
                    Rect panel = new Rect(6f, sh - 196f, 248f, 28f);
                    DrawPanelK(panel);
                    DrawLabelK(new Rect(panel.x + 6f, panel.y + 4f, panel.width - 12f, 20f), $"Managed memory: {cachedMemoryMbK:0.0} MB");
                }

                if (extraBuildInfoPanel)
                {
                    Rect panel = new Rect(sw - 262f, 10f, 252f, 104f);
                    DrawPanelK(panel);
                    string[] lines = GetBuildInfoLinesK();
                    float y = panel.y + 6f;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        DrawLabelK(new Rect(panel.x + 8f, y, panel.width - 16f, 20f), lines[i]);
                        y += 23f;
                    }
                }
            }
            catch { }
        }

        // ====================================================================
        // Private helpers (all suffixed with K)
        // ====================================================================

        private static void EnsureExtrasStylesK()
        {
            try
            {
                if (extrasOverlayLabelStyleK == null)
                {
                    extrasOverlayLabelStyleK = new GUIStyle(GUI.skin.label);
                    extrasOverlayLabelStyleK.fontSize = 12;
                    extrasOverlayLabelStyleK.fontStyle = FontStyle.Normal;
                    extrasOverlayLabelStyleK.normal.textColor = new Color(0.82f, 1f, 0.88f, 1f);
                    extrasOverlayLabelStyleK.richText = true;
                    extrasOverlayLabelStyleK.alignment = TextAnchor.UpperLeft;
                    extrasOverlayLabelStyleK.clipping = TextClipping.Clip;
                }
                if (extrasPanelBoxStyleK == null)
                {
                    extrasPanelBoxStyleK = new GUIStyle(GUI.skin.box);
                    extrasPanelBoxStyleK.richText = true;
                }
            }
            catch { }
        }

        private static void DrawPanelK(Rect rect)
        {
            try
            {
                if (extrasPanelBoxStyleK == null) EnsureExtrasStylesK();
                GUI.Box(rect, GUIContent.none, extrasPanelBoxStyleK);
            }
            catch { }
        }

        private static void DrawLabelK(Rect rect, string text)
        {
            try
            {
                if (extrasOverlayLabelStyleK == null) EnsureExtrasStylesK();
                GUI.Label(rect, text ?? string.Empty, extrasOverlayLabelStyleK);
            }
            catch { }
        }

        private static Texture2D EnsureFrameGraphTextureK()
        {
            try
            {
                if (frameGraphTextureK == null)
                {
                    frameGraphTextureK = new Texture2D(FrameHistoryLengthK, 64, TextureFormat.RGBA32, false);
                    frameGraphTextureK.name = "AeroMenu_ExtrasK_FrameGraph";
                    frameGraphTextureK.wrapMode = TextureWrapMode.Clamp;
                    frameGraphTextureK.filterMode = FilterMode.Point;
                    frameGraphTextureK.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            catch { }
            return frameGraphTextureK;
        }

        private static void UpdateFrameGraphTextureK()
        {
            try
            {
                Texture2D tex = EnsureFrameGraphTextureK();
                if (tex == null) return;

                int w = tex.width;
                int h = tex.height;
                Color[] pixels = new Color[w * h];
                Color bg = new Color(0.04f, 0.05f, 0.08f, 0.92f);
                for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

                int count = frameTimeCountK;
                if (count > 0)
                {
                    const float scaleMs = 50f;
                    Color good = new Color(0.20f, 0.90f, 0.45f, 1f);
                    Color warn = new Color(1.00f, 0.85f, 0.20f, 1f);
                    Color bad = new Color(1.00f, 0.30f, 0.30f, 1f);

                    for (int x = 0; x < w; x++)
                    {
                        int sampleIndex = count < FrameHistoryLengthK ? x : (frameTimeIndexK + x) % FrameHistoryLengthK;
                        if (sampleIndex < 0 || sampleIndex >= FrameHistoryLengthK || sampleIndex >= count) continue;

                        float ms = frameTimeHistoryK[sampleIndex];
                        if (ms <= 0f) continue;

                        int bar = Mathf.Clamp(Mathf.RoundToInt((ms / scaleMs) * h), 1, h);
                        Color c = ms <= 16.7f ? good : (ms <= 33.4f ? warn : bad);
                        for (int y = 0; y < bar; y++) pixels[(y * w) + x] = c;
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply(false);
            }
            catch { }
        }

        private static void RefreshMemoryK()
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                if (lastMemoryRefreshK >= 0f && now - lastMemoryRefreshK < 1f) return;
                cachedMemoryMbK = (float)System.GC.GetTotalMemory(false) / (1024f * 1024f);
                lastMemoryRefreshK = now;
            }
            catch { }
        }

        private static void ResetFpsStatsK()
        {
            fpsMinK = float.MaxValue;
            fpsMaxK = 0f;
            fpsStatsValidK = false;
        }

        private static string FormatFpsStatsK()
        {
            if (!fpsStatsValidK) return L("FPS min: --   max: --", "FPS min: --   max: --");
            return $"FPS min: {fpsMinK:0}   max: {fpsMaxK:0}";
        }

        private static string[] GetBuildInfoLinesK()
        {
            string version;
            try { version = GetPluginVersionK(); } catch { version = "?"; }
            if (string.IsNullOrEmpty(version)) version = "?";

            string unity;
            try { unity = Application.unityVersion; } catch { unity = "?"; }
            if (string.IsNullOrEmpty(unity)) unity = "?";

            string platform;
            try { platform = Application.platform.ToString(); } catch { platform = "?"; }
            if (string.IsNullOrEmpty(platform)) platform = "?";

            string resolution;
            try { resolution = $"{Screen.width}x{Screen.height}"; } catch { resolution = "?"; }

            return new[]
            {
                $"AeroMenu v{version}",
                $"Unity {unity}",
                $"Platform: {platform}",
                $"Screen: {resolution}"
            };
        }

        // Reflection is guarded: missing/renamed member degrades to the const.
        private static string GetPluginVersionK()
        {
            try
            {
                FieldInfo field = typeof(Plugin).GetField("PluginVersion", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (field != null)
                {
                    object value = field.GetValue(null);
                    if (value != null)
                    {
                        string text = value.ToString();
                        if (!string.IsNullOrEmpty(text)) return text;
                    }
                }
            }
            catch { }

            try { return Plugin.PluginVersion; }
            catch { return "?"; }
        }

        private static void OpenConfigFolderK()
        {
            try
            {
                string folder = Plugin.AeroFolder;
                if (string.IsNullOrWhiteSpace(folder))
                    folder = Path.Combine(Directory.GetCurrentDirectory(), "AeroMenu");
                folder = Path.GetFullPath(folder);

                try
                {
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                }
                catch { }

                bool isMac = false;
                try
                {
                    isMac = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.OSX);
                }
                catch { }

                if (!isMac)
                {
                    try
                    {
                        PlatformID pid = Environment.OSVersion.Platform;
                        isMac = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
                    }
                    catch { }
                }

                string arg = "\"" + folder + "\"";
                bool opened = false;

                if (isMac)
                {
                    try
                    {
                        System.Diagnostics.Process.Start("open", arg);
                        opened = true;
                    }
                    catch { }
                }

                if (!opened)
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", arg);
                        opened = true;
                    }
                    catch { }
                }

                if (!opened && !isMac)
                {
                    try
                    {
                        System.Diagnostics.Process.Start("open", arg);
                        opened = true;
                    }
                    catch { }
                }

                ShowNotification(opened
                    ? "<color=#00FFAA>[CONFIG]</color> Opened AeroMenu folder."
                    : "<color=#FF4444>[CONFIG]</color> Could not open folder.");
            }
            catch { }
        }
    }
}
