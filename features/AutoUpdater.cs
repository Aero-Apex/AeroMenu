#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace AeroMenu
{
    // Auto-updater: checks the GitHub releases API, downloads the newest AeroMenu.dll
    // to a "pending" file, and atomically swaps it in on the NEXT game launch
    // (BepInEx cannot hot-reload the currently executing plugin DLL).
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private const string UpdateRepoApi = "https://api.github.com/repos/Aero-Apex/AeroMenu/releases/latest";
        private const string UpdateDllUrl = "https://github.com/Aero-Apex/AeroMenu/releases/latest/download/AeroMenu.dll";

        public static bool autoUpdateEnabled = true;
        public static bool autoUpdateNotify = true;

        private static bool updateCheckDone;
        private static string updateLatestVersion = string.Empty;
        private static string updateStatus = string.Empty;
        private static Thread updateThread;
        private static readonly object updateLock = new object();

        private static string PendingPath
        {
            get { return Path.Combine(Plugin.AeroFolder, "AeroMenu.dll.pending"); }
        }

        private static string BackupPath
        {
            get { return Path.Combine(Plugin.AeroFolder, "AeroMenu.dll.bak"); }
        }

        private static string PluginPath
        {
            get
            {
                try
                {
                    string code = typeof(Plugin).Assembly.Location;
                    if (!string.IsNullOrEmpty(code) && File.Exists(code)) return code;
                }
                catch { }
                return Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "AeroMenu.dll");
            }
        }

        internal static void InitAutoUpdater()
        {
            try
            {
                if (!Directory.Exists(Plugin.AeroFolder)) Directory.CreateDirectory(Plugin.AeroFolder);
                ApplyPendingUpdate();
            }
            catch { }

            autoUpdateEnabled = PlayerPrefs.GetInt("M_AutoUpdateEnabled", 1) == 1;
            autoUpdateNotify = PlayerPrefs.GetInt("M_AutoUpdateNotify", 1) == 1;
        }

        private static void ApplyPendingUpdate()
        {
            try
            {
                string pending = PendingPath;
                if (!File.Exists(pending)) return;

                byte[] bytes = File.ReadAllBytes(pending);
                if (bytes == null || bytes.Length < 10000 || bytes[0] != (byte)'M' || bytes[1] != (byte)'Z')
                {
                    File.Delete(pending);
                    return;
                }

                string target = PluginPath;
                string backup = BackupPath;
                if (File.Exists(target))
                {
                    try { File.Copy(target, backup, true); } catch { }
                }
                File.WriteAllBytes(target, bytes);
                File.Delete(pending);
            }
            catch { }
        }

        internal static void TickAutoUpdater()
        {
            if (updateCheckDone || !autoUpdateEnabled) return;
            updateCheckDone = true;
            lock (updateLock)
            {
                if (updateThread != null && updateThread.IsAlive) return;
            }
            updateThread = new Thread(() =>
            {
                try
                {
                    string json;
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        client.DefaultRequestHeaders.Add("User-Agent", "AeroMenu-Updater");
                        json = client.GetStringAsync(UpdateRepoApi).GetAwaiter().GetResult();
                    }

                    Match tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    string latest = tag.Success ? tag.Groups[1].Value.Trim() : string.Empty;
                    if (string.IsNullOrEmpty(latest)) { updateStatus = "Update check failed"; return; }

                    if (!IsNewer(latest, Plugin.DisplayVersion))
                    {
                        updateStatus = "Up to date";
                        updateLatestVersion = latest;
                        return;
                    }

                    updateLatestVersion = latest;
                    using (HttpClient dl = new HttpClient())
                    {
                        dl.Timeout = TimeSpan.FromSeconds(30);
                        dl.DefaultRequestHeaders.Add("User-Agent", "AeroMenu-Updater");
                        byte[] dll = dl.GetByteArrayAsync(UpdateDllUrl).GetAwaiter().GetResult();
                        if (dll == null || dll.Length < 10000 || dll[0] != (byte)'M' || dll[1] != (byte)'Z')
                        {
                            updateStatus = "Update download failed";
                            return;
                        }
                        string pending = PendingPath;
                        string dir = Path.GetDirectoryName(pending);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.WriteAllBytes(pending, dll);
                        updateStatus = "v" + latest + " downloaded - restarts on next launch";
                        if (autoUpdateNotify)
                            ShowNotification("<color=#00FF00>[UPDATE]</color> AeroMenu " + latest + " downloaded. Restarts on next launch.");
                    }
                }
                catch
                {
                    updateStatus = "Update check failed";
                }
            });
            updateThread.IsBackground = true;
            updateThread.Start();
        }

        private static bool IsNewer(string latest, string current)
        {
            try
            {
                string a = latest.TrimStart('v', 'V');
                string b = current.TrimStart('v', 'V');
                Version va, vb;
                if (!Version.TryParse(a, out va) || !Version.TryParse(b, out vb)) return false;
                return va > vb;
            }
            catch { return false; }
        }

        internal void DrawAutoUpdater(float width)
        {
            try
            {
                GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(width));
                try
                {
                    DrawMenuSectionHeader(L("AUTO UPDATE", "АВТО ОБНОВЛЕНИЕ"));
                    bool prev = autoUpdateEnabled;
                    autoUpdateEnabled = DrawToggle(autoUpdateEnabled, L("Check for updates on launch", "Проверять обновления при запуске"), Mathf.RoundToInt(Mathf.Min(260f, width - 12f)));
                    if (prev != autoUpdateEnabled) PlayerPrefs.SetInt("M_AutoUpdateEnabled", autoUpdateEnabled ? 1 : 0);

                    bool prevN = autoUpdateNotify;
                    autoUpdateNotify = DrawToggle(autoUpdateNotify, L("Notify on new version", "Уведомлять о новой версии"), Mathf.RoundToInt(Mathf.Min(260f, width - 12f)));
                    if (prevN != autoUpdateNotify) PlayerPrefs.SetInt("M_AutoUpdateNotify", autoUpdateNotify ? 1 : 0);

                    GUILayout.Label(L("Current: ", "Текущая: ") + Plugin.DisplayVersion +
                                    (string.IsNullOrEmpty(updateLatestVersion) ? string.Empty : "   Latest: " + updateLatestVersion), menuDescStyle);
                    if (!string.IsNullOrEmpty(updateStatus))
                        GUILayout.Label(updateStatus, menuDescStyle);

                    if (GUILayout.Button(L("Check now", "Проверить сейчас"), btnStyle, GUILayout.Width(120f)))
                    {
                        updateCheckDone = false;
                        updateStatus = string.Empty;
                        TickAutoUpdater();
                    }

                    if (File.Exists(PendingPath) &&
                        GUILayout.Button(L("Install downloaded update now", "Установить обновление сейчас"), btnStyle, GUILayout.Width(200f)))
                    {
                        ApplyPendingUpdate();
                        ShowNotification("<color=#00FF00>[UPDATE]</color> Installed - restart the game.");
                    }
                }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }
    }
}
