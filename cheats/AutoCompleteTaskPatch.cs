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
        public static bool autoCompleteTasks = false;

        internal static bool AutoCompleteStartedTask(NormalPlayerTask task)
        {
            try
            {
                if (task == null) return false;
                return AeroMenuGUI.CompleteLocalTask(task);
            }
            catch { return false; }
        }
    }

    [HarmonyPatch(typeof(global::Console), nameof(global::Console.Use))]
    public static class AutoCompleteTaskConsoleUsePatch
    {
        public static bool Prefix(global::Console __instance)
        {
            if (!AeroMenuGUI.autoCompleteTasks) return true;
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.myTasks == null || local.Data == null || local.Data.IsDead) return true;
                if (__instance == null) return true;

                foreach (PlayerTask task in local.myTasks)
                {
                    if (task == null || !(task is NormalPlayerTask normal)) continue;
                    if (normal.taskStep >= normal.MaxStep) continue;
                    if (AeroMenuGUI.TaskAcceptsConsole(task, __instance))
                    {
                        if (AeroMenuGUI.AutoCompleteStartedTask(normal))
                            return false;
                    }
                }

                return true;
            }
            catch { return true; }
        }
    }

    [HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
    public static class AutoCompleteTaskBeginPatch
    {
        public static bool Prefix(Minigame __instance, PlayerTask task)
        {
            if (!AeroMenuGUI.autoCompleteTasks) return true;
            try
            {
                if (!(task is NormalPlayerTask normal)) return true;
                if (AeroMenuGUI.AutoCompleteStartedTask(normal))
                {
                    try { __instance.ForceClose(); } catch { }
                    return false;
                }
                return true;
            }
            catch { return true; }
        }
    }
}