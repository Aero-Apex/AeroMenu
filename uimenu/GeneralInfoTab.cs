#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using AeroMenu;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using RewiredConsts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static AeroMenu.AeroMenuGUI;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
private void DrawGeneralInfoTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("AERO OVERVIEW", headerStyle);
            GUILayout.Space(6);

            if (!generalInfoSubTabWidthsReady)
            {
                for (int i = 0; i < generalInfoSubTabs.Length; i++)
                {
                    tabSizeContent.text = generalInfoSubTabs[i];
                    generalInfoSubTabWidths[i] = Mathf.Max(116f, Mathf.Ceil(subTabStyle.CalcSize(tabSizeContent).x) + 28f);
                }
                generalInfoSubTabWidthsReady = true;
            }

            GUILayout.BeginHorizontal();
            for (int i = 0; i < generalInfoSubTabs.Length; i++)
            {
                GUIStyle tabStyle = currentGeneralInfoSubTab == i ? activeSubTabStyle : subTabStyle;
                if (GUILayout.Button(generalInfoSubTabs[i], tabStyle, GUILayout.Width(generalInfoSubTabWidths[i]), GUILayout.Height(24)))
                    SetMultiTab("generalInfo", ref currentGeneralInfoSubTab, i, generalInfoSubTabs.Length, false);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            BeginMultiTabContent("generalInfo", out Matrix4x4 oldMatrix, out Color oldColor);
            try
            {
            string accentHex = GetMenuAccentHex();
            bool rgbText = RgbMenuTextActive();
            string goldHex = rgbText ? accentHex : ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(255, 187, 54, 255)) : new Color32(255, 187, 54, 255));
            string versionText = Plugin.DisplayVersion;

            GUIStyle textStyle = richWrapLabelStyle12;
            textStyle.normal.textColor = whiteMenuTheme ? new Color(0.16f, 0.16f, 0.16f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);

            if (currentGeneralInfoSubTab == 0)
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(
                    $"{L("Welcome to", "Welcome to")} <b><color=#{accentHex}>Aero Menu</color></b> " +
                    $"<b><color=#{goldHex}>{versionText}</color></b>",
                    textStyle);
                GUILayout.Space(4);
                GUILayout.Label(L(
                    "Aero Menu is a lightweight BepInEx IL2CPP utility for Among Us with lobby tools, visuals, spoofing and host-side controls.",
                    "Aero Menu is a lightweight BepInEx IL2CPP utility for Among Us with lobby tools, visuals, spoofing and host-side controls."), textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"<b><color=#{accentHex}>Quick Hotkeys</color></b>", textStyle);
                string menuKeyText = (menuToggleKey == KeyCode.None ? DefaultMenuKey() : menuToggleKey).ToString();
                if (menuToggleKeyAlt != KeyCode.None) menuKeyText += " / " + menuToggleKeyAlt;
                GUILayout.Label($"{L("Menu key", "Menu key")}: <b>{menuKeyText}</b>{L(" (Windows: Insert / F3 | macOS: ` / F3)", " (Windows: Insert / F3 | macOS: ` / F3)")}", textStyle);
                GUILayout.Label(L("Right Click: teleport to cursor", "Right Click: teleport to cursor"), textStyle);
                GUILayout.Label(L("F9: magnet cursor", "F9: magnet cursor"), textStyle);

                // Live read-only snapshot of the current session.
                GUILayout.Space(8);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Live Session", "Текущая сессия")}</color></b>", textStyle);
                GUILayout.Label(GetGeneralInfoSessionText(), textStyle);

                // Copyable one-line summary for support / bug reports.
                GUILayout.Space(8);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Copy Session Info", "Скопировать данные сессии")}</color></b>", textStyle);
                LoadGeneralInfoReport();

                GUIStyle reportFieldStyle = pseudoInputStyle ?? inputBlockStyle ?? GUI.skin.textField;
                GUIStyle reportButtonStyle = btnStyle ?? GUI.skin.button;
                GUILayout.Label(MenuText(L("Optional note (included when copied):", "Заметка (включается при копировании):")), menuDescStyle);
                string typedNote = AeroTextField(generalInfoReportNote ?? string.Empty, reportFieldStyle, GUILayout.Height(24));
                if (!string.Equals(typedNote, generalInfoReportNote, StringComparison.Ordinal))
                {
                    generalInfoReportNote = typedNote ?? string.Empty;
                    try { PlayerPrefs.SetString(GeneralInfoReportNoteKey, generalInfoReportNote); }
                    catch { }
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(MenuText(L("Refresh", "Обновить")), reportButtonStyle, GUILayout.Width(104), GUILayout.Height(24)))
                    RefreshGeneralInfoReport();
                if (GUILayout.Button(MenuText(L("Copy to Clipboard", "Скопировать")), reportButtonStyle, GUILayout.Width(150), GUILayout.Height(24)))
                    CopyGeneralInfoReport();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                string typedReport = AeroTextField(generalInfoReportText ?? string.Empty, reportFieldStyle, GUILayout.Height(24));
                if (!string.Equals(typedReport, generalInfoReportText, StringComparison.Ordinal))
                    generalInfoReportText = typedReport ?? string.Empty;

                GUILayout.Label(MenuText(L(
                    "Builds a one-line summary of your player, room and network info that you can copy for bug reports.",
                    "Создаёт однострочную сводку об игроке, комнате и сети, которую можно скопировать для отчёта.")), menuDescStyle);
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label($"<b><color=#{accentHex}>Aero Menu</color></b>", textStyle);
                GUILayout.Space(4);
                GUILayout.Label(L(
                    "Aero Menu is a lightweight Among Us utility with lobby tools, visuals, spoofing and host-side controls.",
                    "Aero Menu is a lightweight Among Us utility with lobby tools, visuals, spoofing and host-side controls."), textStyle);
                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{accentHex}>Notes</color></b>", textStyle);
                GUILayout.Label(L(
                    "This is a private build. No public repository or update channel is associated with it.",
                    "This is a private build. No public repository or update channel is associated with it."), textStyle);
                GUILayout.EndVertical();
            }            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            }
            finally
            {
                EndMultiTabContent(oldMatrix, oldColor);
            }
        }

        // ===== General / Information state =====
        private const string GeneralInfoReportNoteKey = "M_GeneralInfoReportNote";
        private static bool generalInfoReportLoaded;
        private static string generalInfoReportNote = string.Empty;
        private static string generalInfoReportText = string.Empty;

        private static string generalInfoSessionCache = string.Empty;
        private static float generalInfoSessionNextRefresh;

        private static void LoadGeneralInfoReport()
        {
            if (generalInfoReportLoaded) return;
            generalInfoReportLoaded = true;
            try
            {
                generalInfoReportNote = PlayerPrefs.GetString(GeneralInfoReportNoteKey, string.Empty) ?? string.Empty;
            }
            catch { generalInfoReportNote = string.Empty; }

            if (string.IsNullOrEmpty(generalInfoReportText))
                RefreshGeneralInfoReportSilent();
        }

        private static string GetGeneralInfoSessionText()
        {
            float now = Time.realtimeSinceStartup;
            if (now >= generalInfoSessionNextRefresh)
            {
                generalInfoSessionNextRefresh = now + 0.5f;
                generalInfoSessionCache = BuildGeneralInfoSessionText(true);
            }
            return generalInfoSessionCache;
        }

        private static void RefreshGeneralInfoReport()
        {
            RefreshGeneralInfoReportSilent();
            ShowNotification(L("<color=#66CCFF>[INFO]</color> Session summary refreshed.",
                               "<color=#66CCFF>[ИНФО]</color> Сводка сессии обновлена."));
        }

        private static void RefreshGeneralInfoReportSilent()
        {
            StringBuilder sb = new StringBuilder(BuildGeneralInfoSessionText(false));
            string note = (generalInfoReportNote ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            if (note.Length > 0)
                sb.Append(" | Note: ").Append(SanitizeGeneralInfoValue(note, 200));
            generalInfoReportText = sb.ToString();
        }

        private static void CopyGeneralInfoReport()
        {
            if (string.IsNullOrWhiteSpace(generalInfoReportText))
                RefreshGeneralInfoReportSilent();
            try
            {
                GUIUtility.systemCopyBuffer = generalInfoReportText ?? string.Empty;
                ShowNotification(L("<color=#00FFAA>[INFO]</color> Session summary copied to clipboard.",
                                   "<color=#00FFAA>[ИНФО]</color> Сводка сессии скопирована в буфер."));
            }
            catch { }
        }

        // Keeps player-supplied text from injecting rich-text tags and from
        // growing the one-line report without bound.
        private static string SanitizeGeneralInfoValue(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "N/A";
            string cleaned = value.Replace("<", string.Empty).Replace(">", string.Empty)
                                  .Replace("\r", " ").Replace("\n", " ");
            if (cleaned.Length > maxLength) cleaned = cleaned.Substring(0, maxLength) + "...";
            return cleaned;
        }

        private static string BuildGeneralInfoSessionText(bool rich)
        {
            string player = "N/A";
            string friendCode = "N/A";
            string platform = "N/A";
            string room = "N/A";
            string players = "N/A";
            string region = "N/A";
            string ping = "N/A";

            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local != null && local.Data != null)
                {
                    player = SanitizeGeneralInfoValue(local.Data.PlayerName, 64);
                    uint rawLevel = local.Data.PlayerLevel;
                    if (rawLevel != uint.MaxValue && rawLevel < 10000)
                        player += " (Lv " + (rawLevel + 1) + ")";
                    friendCode = SanitizeGeneralInfoValue(GetDisplayedFriendCode(local.Data, "N/A"), 64);

                    AmongUsClient clientRoot = AmongUsClient.Instance;
                    if (clientRoot != null)
                    {
                        ClientData localClient = clientRoot.GetClientFromCharacter(local);
                        if (localClient != null) platform = SanitizeGeneralInfoValue(GetPlatform(localClient), 32);
                    }
                }

                object clientObj = AmongUsClient.Instance;
                int gameId;
                if (TryGetIntB(GetMemberValueB(clientObj, "GameId", "gameId"), out gameId) && gameId != 0)
                {
                    string code = FormatGameIdB(gameId);
                    room = SanitizeGeneralInfoValue(string.IsNullOrWhiteSpace(code) ? gameId.ToString() : code, 24);
                }

                var allPlayers = PlayerControl.AllPlayerControls;
                if (allPlayers != null)
                {
                    int connected = 0;
                    int alive = 0;
                    foreach (PlayerControl pc in allPlayers)
                    {
                        if (pc == null) continue;
                        connected++;
                        if (pc.Data != null)
                        {
                            bool dead;
                            if (!TryGetBoolB(GetMemberValueB(pc.Data, "IsDead", "isDead"), out dead) || !dead)
                                alive++;
                        }
                        if (connected >= 64) break;
                    }
                    if (connected > 0) players = alive + " / " + connected;
                }

                int pingMs;
                if (TryGetIntB(GetMemberValueB(clientObj, "Ping", "ping", "AveragePing", "averagePing"), out pingMs))
                    ping = pingMs + " ms";

                object regionObj = GetMemberValueB(clientObj, "Region", "region", "CurrentRegion", "currentRegion");
                object regionName = GetMemberValueB(regionObj, "name", "Name", "regionName", "RegionName");
                if (regionName != null) region = SanitizeGeneralInfoValue(regionName.ToString(), 32);
            }
            catch { }

            StringBuilder sb = new StringBuilder();
            AppendGeneralInfoField(sb, rich, L("Player", "Игрок"), player);
            AppendGeneralInfoField(sb, rich, L("Friend code", "Код друга"), friendCode);
            AppendGeneralInfoField(sb, rich, L("Platform", "Платформа"), platform);
            AppendGeneralInfoField(sb, rich, L("Room", "Комната"), room);
            AppendGeneralInfoField(sb, rich, L("Alive / connected", "Живы / подключено"), players);
            AppendGeneralInfoField(sb, rich, L("Region", "Регион"), region);
            AppendGeneralInfoField(sb, rich, L("Ping", "Пинг"), ping);
            return sb.ToString();
        }

        private static void AppendGeneralInfoField(StringBuilder sb, bool rich, string label, string value)
        {
            if (sb.Length > 0) sb.Append(rich ? "\n" : " | ");
            string safe = SanitizeGeneralInfoValue(value, 64);
            if (rich) sb.Append(label).Append(": <b>").Append(safe).Append("</b>");
            else sb.Append(label).Append(": ").Append(safe);
        }

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        public static class ChatLogger_Patch
        {
            public static void Prefix(PlayerControl sourcePlayer, ref string chatText)
            {
                if (!AeroMenuGUI.enableChatLog || string.IsNullOrWhiteSpace(chatText)) return;

                try
                {
                    string time = System.DateTime.Now.ToString("HH:mm:ss");

                    string name = "System/Unknown";
                    string levelStr = "?";
                    string fc = "Hidden";
                    string puid = "Unknown";
                    string platformStr = "Unknown";

                    if (sourcePlayer != null && sourcePlayer.Data != null)
                    {
                        name = sourcePlayer.Data.PlayerName;

                        uint rawLevel = sourcePlayer.Data.PlayerLevel;
                        if (rawLevel != uint.MaxValue && rawLevel < 10000) levelStr = (rawLevel + 1).ToString();

                        fc = GetDisplayedFriendCode(sourcePlayer.Data, "Hidden");

                        var client = AmongUsClient.Instance?.GetClientFromCharacter(sourcePlayer);
                        if (client != null)
                        {
                            puid = GetPlayerPuid(sourcePlayer);
                            platformStr = AeroMenuGUI.GetPlatform(client);
                        }
                    }

                    string cleanText = System.Text.RegularExpressions.Regex.Replace(chatText, "<.*?>", string.Empty);

                    string logLine = $"[{time}] [{name}] [Lv:{levelStr}] [FC:{fc}] [ID:{puid}] [{platformStr}] : {cleanText}\n";

                    string chatLogPath = System.IO.Path.Combine(Plugin.AeroFolder, "ChatLog.txt");
                    System.IO.File.AppendAllText(chatLogPath, logLine);
                }
                catch { }
            }
        }

    }
}
