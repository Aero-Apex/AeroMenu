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
    [BepInPlugin("com.aeromenu.menu", "AeroMenu", Plugin.PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginVersion = "1.0.0";
        public const string DisplayVersion = "v1";
        public static ModPlayer modClass;

        public static Plugin Instance { get; private set; } = null!;
        public static string AeroFolder = "";
        public static ConfigFile MenuConfig;
        public static ConfigEntry<float> RpcSpoofDelayConfig;
    public static ConfigEntry<long> DiscordAppIdConfig;
    public static ConfigEntry<string> DiscordImageKeyConfig;
        public static ConfigEntry<KeyCode> MenuKeybind;
        public static ConfigEntry<string> SpoofedLevel;
        public static ConfigEntry<bool> EnableLevelSpoofConfig;
        public static ConfigEntry<bool> EnableFriendCodeSpoofConfig;
        public static ConfigEntry<string> SpoofFriendCodeConfig;
        public static ConfigEntry<bool> EnablePlatformSpoof;
        public static ConfigEntry<bool> AutoBanBrokenFriendCodeConfig;
        public static ConfigEntry<int> PlatformIndex;
        private static ConfigEntry<bool> StorePlatformMigrated;
        public static ConfigEntry<bool> ShowWatermarkConfig;
        public static ConfigEntry<int> MenuColorIndexConfig;
        public static ConfigEntry<bool> RgbMenuModeConfig;
        public static ConfigEntry<bool> RgbMenuTextConfig;
        public static ConfigEntry<bool> BoldMenuTextConfig;
        public static ConfigEntry<bool> UnlockCosmeticsConfig;
        public static ConfigEntry<bool> MoreLobbyInfoConfig;
        public static ConfigEntry<bool> EnableChatDarkModeConfig;
        public static ConfigEntry<string> GhostChatColorConfig;
        public static ConfigEntry<bool> ThrottleDefaultLogsConfig;
        public static ConfigEntry<bool> DetailedLogsEnabledConfig;
        public static ConfigEntry<bool> ShowEspFriendCodeConfig;


        public override void Load()
        {
            Instance = this;

            AeroFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "AeroMenu");
            if (!System.IO.Directory.Exists(AeroFolder))
            {
                System.IO.Directory.CreateDirectory(AeroFolder);
            }

            string banFile = System.IO.Path.Combine(AeroFolder, "AeroMenuBanList.txt");
            if (!System.IO.File.Exists(banFile))
            {
                System.IO.File.Create(banFile).Dispose();
            }

            string platformBanFile = System.IO.Path.Combine(AeroFolder, "AeroPlatformBanList.txt");
            if (!System.IO.File.Exists(platformBanFile))
            {
                System.IO.File.WriteAllText(platformBanFile, "# One custom platform token per line. Matching PlatformName values are host-banned when enabled.\n# Example: custom\n");
            }

            string friendEspFile = System.IO.Path.Combine(AeroFolder, "AeroFriendEspIgnore.txt");
            if (!System.IO.File.Exists(friendEspFile))
            {
                System.IO.File.WriteAllText(friendEspFile, "# One nickname, Friend Code, or PUID per line. Matching players will not show ESP info.\n");
            }

            string botBanFile = System.IO.Path.Combine(AeroFolder, "AeroBotBanList.txt");
            if (!System.IO.File.Exists(botBanFile))
            {
                System.IO.File.WriteAllText(botBanFile, "# Auto bot ban list. Format: FriendCode|PUID|Nickname|Date|Reason\n# You can also add one nickname, Friend Code, or PUID per line to always ban matching players.\nHoly bot\nbot\n");
            }

            string configPath = System.IO.Path.Combine(AeroFolder, "AeroMenu.cfg");
            MigratePlatformSpoofKey(configPath);
            MenuConfig = new ConfigFile(configPath, true);
            RpcSpoofDelayConfig = MenuConfig.Bind("AeroMenu.Spoofing", "RpcDelay", 4f, "");
            MenuKeybind = MenuConfig.Bind("AeroMenu.GUI", "Keybind", KeyCode.Insert, "");
            SpoofedLevel = MenuConfig.Bind("AeroMenu.Spoofing", "Level", "100", "");
            EnableLevelSpoofConfig = MenuConfig.Bind("AeroMenu.Spoofing", "EnableLevelSpoof", true, "");
            EnableFriendCodeSpoofConfig = MenuConfig.Bind("AeroMenu.Spoofing", "EnableFriendCodeSpoof", false, "");
            SpoofFriendCodeConfig = MenuConfig.Bind("AeroMenu.Spoofing", "FriendCode", "crewmate01", "");
            EnablePlatformSpoof = MenuConfig.Bind(
                "AeroMenu.Spoofing",
                "AeroPlatformSpoof",
                true,
                "True: sends the AeroMenu name in raw PlatformName and may be detected by other mods or anti-cheats. False: keeps the game's original raw platform name.");
            AutoBanBrokenFriendCodeConfig = MenuConfig.Bind("AeroMenu.Anticheat", "AutoBanBrokenFriendCode", false, "");
            int nativePlatformIndex = DetectNativePlatformIndex();
            PlatformIndex = MenuConfig.Bind("AeroMenu.Spoofing", "PlatformIndex", nativePlatformIndex, "");
            StorePlatformMigrated = MenuConfig.Bind("AeroMenu.Compatibility", "StorePlatformMigrated", false, "Internal one-time Epic/Steam platform migration flag.");
            if (!StorePlatformMigrated.Value)
            {
                PlatformIndex.Value = nativePlatformIndex;
                StorePlatformMigrated.Value = true;
            }
            ShowWatermarkConfig = MenuConfig.Bind("AeroMenu.GUI", "ShowWatermark", true, "");
            MenuColorIndexConfig = MenuConfig.Bind("AeroMenu.GUI", "MenuColorIndex", 10, "");
            RgbMenuModeConfig = MenuConfig.Bind("AeroMenu.GUI", "RgbMenuMode", false, "");
            RgbMenuTextConfig = MenuConfig.Bind("AeroMenu.GUI", "RgbMenuText", false, "When true, RGB Menu Mode also recolors menu text.");
            BoldMenuTextConfig = MenuConfig.Bind("AeroMenu.GUI", "BoldMenuText", true, "When true, menu text is drawn bold.");
            UnlockCosmeticsConfig = MenuConfig.Bind("AeroMenu.General", "UnlockCosmetics", true, "");
            MoreLobbyInfoConfig = MenuConfig.Bind("AeroMenu.Visuals", "MoreLobbyInfo", true, "");
            EnableChatDarkModeConfig = MenuConfig.Bind("AeroMenu.Chat", "EnableChatDarkMode", true, "Turns the custom dark chat input and bubble colors on/off.");
            GhostChatColorConfig = MenuConfig.Bind("AeroMenu.Chat", "GhostChatColor", "#AFAFAF", "Hex color or mode for visible ghost chat messages. Supports rainbow/lgbt and shimmer.");
            ThrottleDefaultLogsConfig = MenuConfig.Bind("AeroMenu.Diagnostics", "ThrottleDefaultLogs", true, "Legacy compatibility setting. DetailedLogsEnabled now controls routine log output.");
            DetailedLogsEnabledConfig = MenuConfig.Bind("AeroMenu.Diagnostics", "DetailedLogsEnabled", false, "Enables verbose Unity/BepInEx Message, Info and Debug output. Warnings and errors are always shown.");
            ShowEspFriendCodeConfig = MenuConfig.Bind("AeroMenu.Visuals", "ShowEspFriendCode", true, "Show Friend Code in ESP player info.");
            DiscordAppIdConfig = MenuConfig.Bind("AeroMenu.Discord", "AppId", 1540319887463948380L, "Discord Application ID shown as the game title in Rich Presence. Create your own app at discord.com/developers/applications and paste its Application ID here.");
            DiscordImageKeyConfig = MenuConfig.Bind("AeroMenu.Discord", "LargeImageKey", "aero_menu", "Rich Presence large image asset key from your Discord application's Art Assets.");
            AeroDiscordPresence.AppId = DiscordAppIdConfig.Value;
            AeroDiscordPresence.LargeImageKey = DiscordImageKeyConfig.Value;
            MenuConfig.Save();
            AeroMenuGUI.detailedLogsEnabled = DetailedLogsEnabledConfig.Value;
            RepeatedLogFilter.Install();

            ClassInjector.RegisterTypeInIl2Cpp<AeroMenuGUI>();
            ClassInjector.RegisterTypeInIl2Cpp<ModPlayer>();
            ClassInjector.RegisterTypeInIl2Cpp<AeroNetGuard.NetworkGuardDriver>();
            ClassInjector.RegisterTypeInIl2Cpp<AeroDiscordPresence>();

            var guiObject = new GameObject("AeroMenu_Object");
            UnityEngine.Object.DontDestroyOnLoad(guiObject);
            guiObject.hideFlags = HideFlags.HideAndDontSave;
            AeroMenuGUI guiComp = guiObject.AddComponent<AeroMenuGUI>();
            guiObject.AddComponent<AeroNetGuard.NetworkGuardDriver>();
            guiObject.AddComponent<AeroDiscordPresence>();

            modClass = AddComponent<ModPlayer>();

            AeroMenuGUI.EmergencyFlush = () => { try { guiComp.FlushAllSaves(); } catch { } };
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                try { AeroMenuGUI.EmergencyFlush?.Invoke(); } catch { }
            };

            var harmony = new Harmony("com.aeromenu.harmony");
            harmony.PatchAll();
        }

        private static void MigratePlatformSpoofKey(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path)) return;

                var lines = new List<string>(System.IO.File.ReadAllLines(path));
                var oldKeys = new List<int>();
                bool inSpoofing = false;
                bool hasNewKey = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        inSpoofing = line.Equals("[AeroMenu.Spoofing]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (!inSpoofing) continue;
                    int separator = line.IndexOf('=');
                    if (separator < 0) continue;

                    string key = line.Substring(0, separator).Trim();
                    if (key.Equals("AeroPlatformSpoof", StringComparison.OrdinalIgnoreCase))
                        hasNewKey = true;
                    else if (key.Equals("EnablePlatformSpoof", StringComparison.OrdinalIgnoreCase))
                        oldKeys.Add(i);
                }

                if (oldKeys.Count == 0) return;

                if (!hasNewKey)
                {
                    int index = oldKeys[0];
                    int keyStart = lines[index].IndexOf("EnablePlatformSpoof", StringComparison.OrdinalIgnoreCase);
                    lines[index] = lines[index].Substring(0, keyStart) +
                                   "AeroPlatformSpoof" +
                                   lines[index].Substring(keyStart + "EnablePlatformSpoof".Length);
                    oldKeys.RemoveAt(0);
                }

                for (int i = oldKeys.Count - 1; i >= 0; i--)
                    lines.RemoveAt(oldKeys[i]);

                System.IO.File.WriteAllLines(path, lines);
            }
            catch { }
        }

        private static int DetectNativePlatformIndex()
        {
            try
            {
                string gameRoot = System.IO.Directory.GetCurrentDirectory();
                bool epicInstall = System.IO.Directory.Exists(System.IO.Path.Combine(gameRoot, ".egstore"));
                bool epicLaunch = Environment.GetCommandLineArgs().Any(argument =>
                    argument.IndexOf("epic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    argument.StartsWith("-AUTH_", StringComparison.OrdinalIgnoreCase));

                return epicInstall || epicLaunch ? 0 : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}

