#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace AeroMenu
{
    // Self-updater.
    //
    // BepInEx cannot hot-swap the plugin that is currently running, so an update is
    // staged to AeroMenu/AeroMenu.dll.pending and installed at the next startup.
    // "Restart & Update" makes that painless: it stages the DLL, launches a tiny
    // helper that waits for the game to exit, applies the DLL, and relaunches the
    // game with the same arguments.
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private const string ApiUrl = "https://api.github.com/repos/Aero-Apex/AeroMenu/releases/latest";
        private const string DlUrl = "https://github.com/Aero-Apex/AeroMenu/releases/latest/download/AeroMenu.dll";
        private const string UserAgent = "AeroMenu-Updater";

        public static bool autoUpdateEnabled = true;
        public static bool autoUpdateNotify = true;

        private static string updateStatus = "Not checked yet";
        private static string updateLatestVersion = string.Empty;
        private static string updateError = string.Empty;
        private static bool updateBusy;
        private static bool updateChecked;
        private static Thread updateThread;
        private static readonly object updateLock = new object();

        // After a failed check, try again after this many seconds (checked in Update).
        private static float updateRetryAt;
        private static float updateNextAutoCheckAt;
        private static bool updatePrefsLoaded;

        private static string GameDir
        {
            get
            {
                try
                {
                    string cwd = Directory.GetCurrentDirectory();
                    if (!string.IsNullOrEmpty(cwd)) return cwd;
                }
                catch { }
                return string.Empty;
            }
        }

        private static string PendingPath
        {
            get { return Path.Combine(Plugin.AeroFolder, "AeroMenu.dll.pending"); }
        }

        private static string BackupPath
        {
            get { return Path.Combine(Plugin.AeroFolder, "AeroMenu.dll.bak"); }
        }

        private static string HelperPath
        {
            get { return Path.Combine(Plugin.AeroFolder, "AeroMenu_Update.cmd"); }
        }

        private static string PluginPath
        {
            get
            {
                // Prefer the known game path. Assembly.Location is empty for
                // IL2CPP plugins loaded from memory and must not be trusted.
                string dir = GameDir;
                if (!string.IsNullOrEmpty(dir))
                {
                    string p = Path.Combine(dir, "BepInEx", "plugins", "AeroMenu.dll");
                    if (File.Exists(p)) return p;
                }
                try
                {
                    string loc = typeof(Plugin).Assembly.Location;
                    if (!string.IsNullOrEmpty(loc) && File.Exists(loc) &&
                        string.Equals(Path.GetFileName(loc), "AeroMenu.dll", StringComparison.OrdinalIgnoreCase))
                        return loc;
                }
                catch { }
                return Path.Combine(dir, "BepInEx", "plugins", "AeroMenu.dll");
            }
        }

        private static bool LooksLikeDll(byte[] bytes)
        {
            return bytes != null && bytes.Length > 10000 && bytes[0] == (byte)'M' && bytes[1] == (byte)'Z';
        }

        internal static void InitAutoUpdater()
        {
            LoadAutoUpdatePrefs();
            try
            {
                if (!Directory.Exists(Plugin.AeroFolder)) Directory.CreateDirectory(Plugin.AeroFolder);
                ApplyPendingUpdate();
                // Stale helper from a previous failed update is harmless, but remove
                // a stale pending file that is too small to be a real DLL.
                string pending = PendingPath;
                if (File.Exists(pending) && !LooksLikeDll(File.ReadAllBytes(pending))) File.Delete(pending);
            }
            catch { }
            updateNextAutoCheckAt = Time.unscaledTime + 3f;
            updateStatus = "Ready"; LogUpdater("Ready");
        }

        private static void LoadAutoUpdatePrefs()
        {
            if (updatePrefsLoaded) return;
            updatePrefsLoaded = true;
            try
            {
                autoUpdateEnabled = PlayerPrefs.GetInt("M_AutoUpdateEnabled", 1) == 1;
                autoUpdateNotify = PlayerPrefs.GetInt("M_AutoUpdateNotify", 1) == 1;
            }
            catch { }
        }

        // Applies a staged update at startup, before the old DLL is needed again.
        private static void ApplyPendingUpdate()
        {
            try
            {
                string pending = PendingPath;
                if (!File.Exists(pending)) return;

                byte[] bytes = File.ReadAllBytes(pending);
                if (!LooksLikeDll(bytes)) { File.Delete(pending); return; }

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
            LoadAutoUpdatePrefs();

            if (updateBusy) return;
            if (updateRetryAt > 0f && Time.unscaledTime >= updateRetryAt)
            {
                updateRetryAt = 0f;
                StartUpdateCheck(true);
                return;
            }
            if (!autoUpdateEnabled || updateChecked) return;
            if (Time.unscaledTime < updateNextAutoCheckAt) return;
            updateChecked = true;
            StartUpdateCheck(false);
        }

        private static void StartUpdateCheck(bool manual)
        {
            lock (updateLock)
            {
                if (updateThread != null && updateThread.IsAlive) return;
            }
            updateBusy = true;
            updateError = string.Empty;
            updateStatus = "Checking GitHub..."; LogUpdater("Checking GitHub");
            updateThread = new Thread(() => UpdateWorker(manual));
            updateThread.IsBackground = true;
            updateThread.Start();
        }

        private static void UpdateWorker(bool manual)
        {
            string json = string.Empty;
            string tag = string.Empty;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    json = client.GetStringAsync(ApiUrl).GetAwaiter().GetResult();
                }

                Match m = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                tag = m.Success ? m.Groups[1].Value.Trim() : string.Empty;
                if (string.IsNullOrEmpty(tag)) throw new Exception("Could not read release version from GitHub.");

                updateLatestVersion = tag;
                bool newer = IsNewer(tag, Plugin.PluginVersion);
                if (!newer && !manual)
                {
                    updateStatus = "Up to date (v" + Plugin.PluginVersion + ")"; LogUpdater(updateStatus);
                        return;
                }

                updateStatus = "Downloading " + tag + "..."; LogUpdater(updateStatus);
                byte[] dll;
                using (HttpClient dl = new HttpClient())
                {
                    dl.Timeout = TimeSpan.FromSeconds(45);
                    dl.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    dll = dl.GetByteArrayAsync(DlUrl).GetAwaiter().GetResult();
                }
                if (!LooksLikeDll(dll)) throw new Exception("Downloaded file was not a valid DLL.");

                if (!Directory.Exists(Plugin.AeroFolder)) Directory.CreateDirectory(Plugin.AeroFolder);
                File.WriteAllBytes(PendingPath, dll);

                updateStatus = "v" + tag + " ready - Restart & Update to install"; LogUpdater(updateStatus);
                updateError = string.Empty;
                if (!newer && manual)
                    updateStatus = "v" + tag + " re-downloaded - Restart & Update to reinstall";

                if (autoUpdateNotify && !manual)
                    ShowNotification("<color=#00FF00>[UPDATE]</color> AeroMenu " + tag + " downloaded. Use MENU > Restart & Update.");
            }
            catch (Exception ex)
            {
                updateError = ex.Message;
                updateStatus = "Update check failed: " + ShortError(ex); LogUpdater(updateStatus);
                updateRetryAt = Time.unscaledTime + 60f; // keep trying in the background
                updateChecked = false;
            }
            finally
            {
                updateBusy = false;
            }
        }

        private static string updaterLastLogged;

        private static void LogUpdater(string message)
        {
            if (message == updaterLastLogged) return;
            updaterLastLogged = message;
            try { Plugin.Instance?.Log?.LogInfo("[UPDATE] " + message); } catch { }
        }

        private static string ShortError(Exception ex)
        {
            string s = ex != null ? ex.Message : "unknown error";
            if (s.Length > 90) s = s.Substring(0, 90) + "...";
            return s;
        }

        private static bool IsNewer(string latest, string current)
        {
            try
            {
                Version va, vb;
                if (!Version.TryParse(latest.TrimStart('v', 'V'), out va)) return false;
                if (!Version.TryParse(current.TrimStart('v', 'V'), out vb)) return false;
                return va > vb;
            }
            catch { return false; }
        }

        // Staged update present and valid?
        internal static bool HasPendingUpdate()
        {
            try
            {
                string p = PendingPath;
                return File.Exists(p) && LooksLikeDll(File.ReadAllBytes(p));
            }
            catch { return false; }
        }

        // Writes a Windows batch helper that waits for Among Us to exit, replaces
        // the plugin DLL and relaunches the game with its original arguments.
        private static bool WriteUpdateHelper()
        {
            try
            {
                string helper = HelperPath;
                string target = PluginPath;
                string pending = PendingPath;
                string backup = BackupPath;
                string log = Path.Combine(Plugin.AeroFolder, "AeroMenu_Update.log");
                string gameExe = Path.Combine(GameDir, "Among Us.exe");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("title AeroMenu Update");
                sb.AppendLine("echo AeroMenu update started > \"" + log + "\"");
                sb.AppendLine(":waitloop");
                sb.AppendLine("tasklist /FI \"IMAGENAME eq Among Us.exe\" 2>NUL | find /I /N \"Among Us.exe\" >NUL");
                sb.AppendLine("if not errorlevel 1 goto waitloop");
                sb.AppendLine("timeout /t 2 /nobreak >NUL");
                sb.AppendLine("echo Game exited. Replacing plugin... >> \"" + log + "\"");
                sb.AppendLine("if exist \"" + backup + "\" del /F /Q \"" + backup + "\" >NUL 2>&1");
                sb.AppendLine("if exist \"" + target + "\" move /Y \"" + target + "\" \"" + backup + "\" >NUL 2>&1");
                sb.AppendLine("move /Y \"" + pending + "\" \"" + target + "\" >NUL 2>&1");
                sb.AppendLine("if not exist \"" + target + "\" (");
                sb.AppendLine("  echo Install failed, restoring backup >> \"" + log + "\"");
                sb.AppendLine("  if exist \"" + backup + "\" move /Y \"" + backup + "\" \"" + target + "\" >NUL 2>&1");
                sb.AppendLine("  goto launch");
                sb.AppendLine(")");
                sb.AppendLine("echo Installed successfully >> \"" + log + "\"");
                sb.AppendLine(":launch");
                sb.AppendLine("echo Relaunching game >> \"" + log + "\"");
                sb.AppendLine("start \"\" \"" + gameExe + "\"");
                sb.AppendLine("del \"%~f0\" >NUL 2>&1");
                File.WriteAllText(helper, sb.ToString());
                return File.Exists(helper);
            }
            catch { return false; }
        }

        // Stages the update (if needed) and restarts the game through the helper.
        internal static void RestartAndUpdate()
        {
            try
            {
                // If nothing is staged yet, download synchronously on a thread is
                // already handled by Check now; tell the user in that case.
                if (!HasPendingUpdate())
                {
                    updateStatus = "No update downloaded. Press Check now first.";
                    return;
                }
                if (!WriteUpdateHelper())
                {
                    updateStatus = "Could not create update helper - update is staged for next launch.";
                    return;
                }

                string helper = HelperPath;
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c start \"\" \"" + helper + "\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process.Start(psi);

                updateStatus = "Restarting to install " + (string.IsNullOrEmpty(updateLatestVersion) ? "update" : updateLatestVersion.StartsWith("v", System.StringComparison.OrdinalIgnoreCase) ? updateLatestVersion : "v" + updateLatestVersion) + "..."; LogUpdater(updateStatus);
                ShowNotification("<color=#00FF00>[UPDATE]</color> Restarting to install update.");

                // Give the helper a moment to start before the game exits.
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(400);
                    try { UnityEngine.Object.FindObjectOfType<AeroMenuGUI>()?.FlushAllSaves(); } catch { }
                    try { AmongUsClient.Instance?.ExitGame(DisconnectReasons.ExitGame); } catch { }
                    Thread.Sleep(200);
                    try { Application.Quit(); } catch { }
                    try { Process.GetCurrentProcess().Kill(); } catch { }
                });
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception ex)
            {
                updateStatus = "Restart failed: " + ShortError(ex);
            }
        }

        internal void DrawAutoUpdater(float width)
        {
            try
            {
                int w = Mathf.RoundToInt(Mathf.Clamp(width, 180f, 560f));
                GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(w));
                try
                {
                    DrawMenuSectionHeader(L("AUTO UPDATE", "АВТО ОБНОВЛЕНИЕ"));
                    int toggleW = Mathf.RoundToInt(Mathf.Min(260f, width - 12f));

                    bool prev = autoUpdateEnabled;
                    autoUpdateEnabled = DrawToggle(autoUpdateEnabled, L("Check for updates on launch", "Проверять обновления при запуске"), toggleW);
                    if (prev != autoUpdateEnabled) PlayerPrefs.SetInt("M_AutoUpdateEnabled", autoUpdateEnabled ? 1 : 0);

                    bool prevN = autoUpdateNotify;
                    autoUpdateNotify = DrawToggle(autoUpdateNotify, L("Notify on new version", "Уведомлять о новой версии"), toggleW);
                    if (prevN != autoUpdateNotify) PlayerPrefs.SetInt("M_AutoUpdateNotify", autoUpdateNotify ? 1 : 0);

                    GUILayout.Label(L("Current: ", "Текущая: ") + Plugin.DisplayVersion +
                                    (string.IsNullOrEmpty(updateLatestVersion) ? string.Empty : "   Latest: " + updateLatestVersion), menuDescStyle);
                    GUILayout.Label(L("Status: ", "Статус: ") + updateStatus, menuDescStyle);
                    if (!string.IsNullOrEmpty(updateError))
                        GUILayout.Label(L("Error: ", "Ошибка: ") + updateError, menuDescStyle);

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(L("Check now", "Проверить сейчас"), btnStyle, GUILayout.Width(110f)))
                    {
                        updateChecked = true;
                        updateRetryAt = 0f;
                        updateStatus = "Checking GitHub...";
                        StartUpdateCheck(true);
                    }
                    if (GUILayout.Button(L("Restart & Update", "Перезапуск и обновление"), btnStyle, GUILayout.Width(150f)))
                        RestartAndUpdate();
                    GUILayout.EndHorizontal();

                    GUILayout.Label(L("Updates install on the next game start. Restart & Update does that automatically.",
                                      "Обновления устанавливаются при следующем запуске. Кнопка перезапуска делает это автоматически."), menuDescStyle);
                }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }
    }
}
