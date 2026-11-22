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


[HarmonyPatch(typeof(CustomNetworkTransform), "Deserialize")]
internal static class CntPositionSanityPatch
{
	private const float MaxCoord = 1000f;

	private static readonly FieldInfo[] SyncVectorFields = FindSyncVectorFields();

	private static FieldInfo[] FindSyncVectorFields()
	{
		try
		{
			List<FieldInfo> fields = new List<FieldInfo>();
			FieldInfo[] all = typeof(CustomNetworkTransform).GetFields(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo field in all)
			{
				if (field.FieldType == typeof(Vector2) || field.FieldType == typeof(Vector3))
					fields.Add(field);
			}
			return fields.ToArray();
		}
		catch { return new FieldInfo[0]; }
	}

	private static float SanitizeCoord(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
		return Mathf.Clamp(value, -MaxCoord, MaxCoord);
	}

	private static Vector3 Sanitize(Vector3 v)
	{
		return new Vector3(SanitizeCoord(v.x), SanitizeCoord(v.y), SanitizeCoord(v.z));
	}

	private static Vector2 Sanitize(Vector2 v)
	{
		return new Vector2(SanitizeCoord(v.x), SanitizeCoord(v.y));
	}

	// Deserialize may have already copied attacker-controlled values into the
	// transform's internal sync fields; normalize those too, not just Transform.position.
	private static void NormalizeSyncFields(CustomNetworkTransform cnt)
	{
		if (SyncVectorFields == null) return;
		foreach (FieldInfo field in SyncVectorFields)
		{
			try
			{
				object value = field.GetValue(cnt);
				if (value is Vector3 v3)
				{
					Vector3 clean = Sanitize(v3);
					if (clean != v3) field.SetValue(cnt, clean);
				}
				else if (value is Vector2 v2)
				{
					Vector2 clean = Sanitize(v2);
					if (clean != v2) field.SetValue(cnt, clean);
				}
			}
			catch { }
		}
	}

	public static void Postfix(CustomNetworkTransform __instance)
	{
		if (!AeroMenu.AeroMenuGUI.oldAntiCheatVersion) return;
		try
		{
			if (__instance == null)
			{
				return;
			}
			if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

			PlayerControl pl = __instance.myPlayer;
			if (pl == null || pl == PlayerControl.LocalPlayer) return;

			Transform t = ((Component)pl).transform;
			if (t == null)
			{
				return;
			}

			Vector3 p = t.position;
			bool invalid = float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
				float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z) ||
				Mathf.Abs(p.x) > MaxCoord || Mathf.Abs(p.y) > MaxCoord || Mathf.Abs(p.z) > MaxCoord;
			if (!invalid) return;

			NormalizeSyncFields(__instance);

			Vector3 q = t.position;
			if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) ||
				float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z))
			{
				t.position = Vector3.zero;
				return;
			}

			if (Mathf.Abs(q.x) > MaxCoord || Mathf.Abs(q.y) > MaxCoord || Mathf.Abs(q.z) > MaxCoord)
			{
				t.position = new Vector3(
					Mathf.Clamp(q.x, -MaxCoord, MaxCoord),
					Mathf.Clamp(q.y, -MaxCoord, MaxCoord),
					Mathf.Clamp(q.z, -MaxCoord, MaxCoord));
			}
		}
		catch
		{
		}
	}
}
}

