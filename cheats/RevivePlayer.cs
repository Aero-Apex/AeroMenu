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
public static void RevivePlayer(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Target not found!");
                return;
            }
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Host required!");
                return;
            }
            if (!target.Data.IsDead)
            {
                ShowNotification($"{target.Data.PlayerName} is already alive!");
                return;
            }

            try
            {
                target.Data.IsDead = false;

                if (target.Collider != null) target.Collider.enabled = true;

                if (target.MyPhysics != null)
                    target.MyPhysics.gameObject.layer = LayerMask.NameToLayer("Players");

                try
                {
                    var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    foreach (var mb in allBehaviours)
                    {
                        if (mb == null || mb.gameObject == null) continue;
                        Type t = mb.GetType();
                        if (t == null || t.Name != "DeadBody") continue;

                        byte parentId = byte.MaxValue;

                        var parentProp = t.GetProperty("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (parentProp != null)
                        {
                            object val = parentProp.GetValue(mb, null);
                            if (val is byte b) parentId = b;
                            else if (val is int i) parentId = (byte)i;
                        }
                        else
                        {
                            var parentField = t.GetField("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (parentField != null)
                            {
                                object val = parentField.GetValue(mb);
                                if (val is byte b) parentId = b;
                                else if (val is int i) parentId = (byte)i;
                            }
                        }

                        if (parentId == target.PlayerId)
                            mb.gameObject.SetActive(false);
                    }
                }
                catch { }

                bool wasImpTeam = false;
                try
                {
                    // A dead player holds a ghost role; ImpostorGhost (7) must still
                    // count as impostor team or they revive as a crewmate.
                    wasImpTeam = IsImpostorTeamRole(target.Data.RoleType) || (int)target.Data.RoleType == 7;
                }
                catch { }

                RoleTypes revivedRole = wasImpTeam ? RoleTypes.Impostor : RoleTypes.Crewmate;
                // RpcSetRole alone leaves the local role behaviour stale until a late
                // RPC comes back, so apply it locally as well.
                try { RoleManager.Instance?.SetRole(target, revivedRole); } catch { }
                target.RpcSetRole(revivedRole, true);
                RefreshRoleBehaviour(target);

                var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);

                ShowNotification($"<color=#00FF00>[REVIVE]</color> {target.Data.PlayerName} revived!");
            }
            catch (Exception)
            {
                ShowNotification("<color=#FF0000>Revive failed!</color>");
            }
    }
}
}
