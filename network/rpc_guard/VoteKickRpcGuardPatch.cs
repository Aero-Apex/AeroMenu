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

namespace AeroNetGuard
{
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;


[HarmonyPatch(typeof(VoteBanSystem), "HandleRpc")]
internal static class VoteKickRpcProtectionPatch
{
	public static bool Prefix(VoteBanSystem __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
	{
		if (!AeroMenu.AeroMenuGUI.oldAntiCheatVersion) return true;
		return NetworkGuard.CheckVoteKickRpc(__instance, callId, reader);
	}
}

// Both NetworkGuard.CheckVoteKickRpc and the legacy VoteBanSystemPatch.Prefix
// (uimenu/MapsTab.cs) call BanVoteKickVoter for the same vote-kick RPC, which
// double-bans the voter. Suppress the duplicate within a short window. Keying on
// (game, voter) is intentional: the two callers format the target name differently.
[HarmonyPatch(typeof(AeroMenu.AeroMenuGUI.VoteBanSystemPatch), "BanVoteKickVoter")]
internal static class BanVoteKickVoterDedupPatch
{
	private const float DedupWindowSeconds = 5f;
	private static readonly Dictionary<string, float> recentBans = new Dictionary<string, float>();
	private static readonly object sync = new object();

	public static bool Prefix(int voterClientId, string voterName, string targetName)
	{
		try
		{
			int gameId = AmongUsClient.Instance != null ? ((InnerNetClient)AmongUsClient.Instance).GameId : -1;
			string key = gameId + ":" + voterClientId;
			float now = Time.realtimeSinceStartup;
			lock (sync)
			{
				if (recentBans.TryGetValue(key, out float at) && now - at < DedupWindowSeconds)
					return false;
				recentBans[key] = now;

				List<string> stale = null;
				foreach (KeyValuePair<string, float> pair in recentBans)
				{
					if (now - pair.Value > DedupWindowSeconds * 2f)
					{
						if (stale == null) stale = new List<string>();
						stale.Add(pair.Key);
					}
				}
				if (stale != null)
				{
					foreach (string s in stale) recentBans.Remove(s);
				}
			}
		}
		catch { }
		return true;
	}
}
}

