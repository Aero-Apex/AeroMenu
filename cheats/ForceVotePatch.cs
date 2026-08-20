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
        public static bool forceVote = false;

        internal static bool ForceCastVote(byte targetPlayerId, bool skip)
        {
            try
            {
                if (MeetingHud.Instance == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                    return false;

                byte localId = (byte)PlayerControl.LocalPlayer.PlayerId;
                byte suspectId = skip ? PlayerVoteArea.SkippedVote : targetPlayerId;

                foreach (var state in MeetingHud.Instance.playerStates)
                {
                    if (state == null) continue;
                    if ((byte)state.PlayerId == localId)
                    {
                        try { state.SetVote(suspectId); } catch { }
                        try { state.SetHasVoted(); } catch { }
                        break;
                    }
                }

                try { MeetingHud.Instance.MarkPlayerAsHasVoted(localId); } catch { }
                MeetingHud.Instance.CmdCastVote(localId, suspectId);
                return true;
            }
            catch { return false; }
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), "CanBeHighlighted")]
    public static class ForceVoteCanHighlightPatch
    {
        public static bool Prefix(PlayerVoteArea __instance, ref bool __result)
        {
            if (!AeroMenuGUI.forceVote) return true;
            try
            {
                __result = true;
                return false;
            }
            catch { return true; }
        }
    }
}