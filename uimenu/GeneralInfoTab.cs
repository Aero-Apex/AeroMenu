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
                    $"<b><color=#{goldHex}>v{versionText}</color></b>",
                    textStyle);
                GUILayout.Space(4);
                GUILayout.Label(L(
                    "Aero Menu is a lightweight BepInEx IL2CPP utility for Among Us with lobby tools, visuals, spoofing and host-side controls.",
                    "Aero Menu is a lightweight BepInEx IL2CPP utility for Among Us with lobby tools, visuals, spoofing and host-side controls."), textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"<b><color=#{accentHex}>Quick Hotkeys</color></b>", textStyle);
                string menuKeyText = (menuToggleKey == KeyCode.None ? KeyCode.Insert : menuToggleKey).ToString();
                GUILayout.Label($"{L("Menu key", "Menu key")}: <b>{menuKeyText}</b>", textStyle);
                GUILayout.Label(L("Right Click: teleport to cursor", "Right Click: teleport to cursor"), textStyle);
                GUILayout.Label(L("F9: magnet cursor", "F9: magnet cursor"), textStyle);
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
