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
using System.Globalization;
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
private void DrawPlayersRoles()
        {
            if (allPlayersRoleAssignIdx < 0 || allPlayersRoleAssignIdx >= roleAssignOptions.Length ||
                allPlayersRoleAssignIdx >= roleAssignNames.Length) allPlayersRoleAssignIdx = 0;

            // A role forced while a player had no friend code is stored by PlayerId. Once a
            // friend code appears, every later force is keyed by FC: drop the stale Id key or
            // IsForcedImp() keeps returning the old (usually impostor) value via its fallback.
            if (Event.current != null && Event.current.type == EventType.Layout)
            {
                foreach (var pc in lockedPlayersList) NormalizeForcedRoleKeys(pc);
            }

            float roleMgrWidth = Mathf.Max(180f, GetMenuWorkWidth(220f, 760f));
            float cardPaddingWidth = menuCardStyle != null && menuCardStyle.padding != null
                ? menuCardStyle.padding.left + menuCardStyle.padding.right
                : 28f;
            float roleInnerWidth = Mathf.Max(96f, roleMgrWidth - cardPaddingWidth);
            bool narrowRoleMgr = roleMgrWidth < 420f;
            float roleActionWidth = Mathf.Floor(Mathf.Max(48f, narrowRoleMgr ? (roleInnerWidth - 4f) * 0.5f : (roleInnerWidth - 12f) * 0.25f));

            void ToggleRoleForcing()
            {
                enablePreGameRoleForce = !enablePreGameRoleForce;
                // Auto 2 Imps needs pre-game forcing; turning forcing off manually has to
                // turn the auto feature off too, otherwise its forced imps become inert.
                if (!enablePreGameRoleForce && autoTwoImpostors)
                {
                    autoTwoImpostors = false;
                    autoTwoImpostorsEnabledRoleForce = false;
                    ClearAutoTwoImpostorSelection();
                    autoTwoImpostorsNeedsGameStartRoll = true;
                    autoTwoImpostorsWasGameStarted = false;
                    autoTwoImpostorsLastLobbyFingerprint = 0;
                }
            }

            void ToggleAutoTwoImpostors()
            {
                autoTwoImpostors = !autoTwoImpostors;
                if (autoTwoImpostors)
                {
                    autoTwoImpostorsNeedsGameStartRoll = true;
                    autoTwoImpostorsLastLobbyFingerprint = 0;
                    RollAutoTwoImpostors(true);
                }
                else
                {
                    ClearAutoTwoImpostorSelection();
                    autoTwoImpostorsLastLobbyFingerprint = 0;
                }
            }

            void ClearAllForcedRoles()
            {
                autoTwoImpostors = false;
                autoTwoImpostorPlayerIds.Clear();
                autoTwoImpostorsLastLobbyFingerprint = 0;
                autoTwoImpostorsEnabledRoleForce = false;
                autoTwoImpostorsPrevRoleForce = false;
                autoTwoImpostorsNeedsGameStartRoll = true;
                autoTwoImpostorsWasGameStarted = false;
                forcedPreGameRoles.Clear();
                forcedImpostors.Clear();
                forcedPreGameRoleFcs.Clear();
                forcedImpostorFcs.Clear();
            }

            GUILayout.BeginVertical(GUILayout.Width(roleMgrWidth));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(roleMgrWidth));
            DrawMenuSectionHeader("PRE-GAME ROLE MANAGER");
            GUIStyle roleBtn = CreateClippedButtonStyle(btnStyle);
            GUIStyle roleActiveBtn = CreateClippedButtonStyle(activeTabStyle);
            GUILayout.BeginHorizontal(GUILayout.Width(roleInnerWidth));
            float forceToggleWidth = narrowRoleMgr ? roleInnerWidth - roleActionWidth - 4f : roleActionWidth;
            if (GUILayout.Button(enablePreGameRoleForce ? "Role Forcing: ON" : "Role Forcing: OFF",
                    enablePreGameRoleForce ? roleActiveBtn : roleBtn, GUILayout.Width(forceToggleWidth), GUILayout.Height(25))) ToggleRoleForcing();
            if (GUILayout.Button(L("Random 2 Imps", "2 СЛУЧАЙНЫХ ИМПОСТЕРА"), roleBtn, GUILayout.Width(roleActionWidth), GUILayout.Height(25)))
            {
                autoTwoImpostorPlayerIds.Clear();
                autoTwoImpostorsLastLobbyFingerprint = 0;
                RollAutoTwoImpostors(true);
            }
            if (!narrowRoleMgr)
            {
                if (GUILayout.Button(autoTwoImpostors ? "Auto 2 Imps: ON" : "Auto 2 Imps", autoTwoImpostors ? roleActiveBtn : roleBtn, GUILayout.Width(roleActionWidth), GUILayout.Height(25))) ToggleAutoTwoImpostors();
                if (GUILayout.Button(L("Clear All Roles", "ОЧИСТИТЬ ВСЕ РОЛИ"), roleBtn, GUILayout.Width(roleActionWidth), GUILayout.Height(25))) ClearAllForcedRoles();
            }
            GUILayout.EndHorizontal();
            if (narrowRoleMgr)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(roleInnerWidth));
                if (GUILayout.Button(autoTwoImpostors ? "Auto 2 Imps: ON" : "Auto 2 Imps", autoTwoImpostors ? roleActiveBtn : roleBtn, GUILayout.Width(roleActionWidth), GUILayout.Height(25))) ToggleAutoTwoImpostors();
                if (GUILayout.Button(L("Clear All Roles", "ОЧИСТИТЬ ВСЕ РОЛИ"), roleBtn, GUILayout.Width(roleActionWidth), GUILayout.Height(25))) ClearAllForcedRoles();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(roleMgrWidth));
            DrawMenuSectionHeader("LIVE ROLE DISTRIBUTOR (HOST)");
            GUIStyle distributorBtn = CreateClippedButtonStyle(btnStyle);
            GUIStyle distributorActiveBtn = CreateClippedButtonStyle(activeTabStyle);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<", distributorBtn, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx--;
                if (allPlayersRoleAssignIdx < 0) allPlayersRoleAssignIdx = roleAssignOptions.Length - 1;
            }

            GUILayout.Label(roleAssignNames[allPlayersRoleAssignIdx], accentValueStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", distributorBtn, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx++;
                if (allPlayersRoleAssignIdx >= roleAssignOptions.Length) allPlayersRoleAssignIdx = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button(L("SET ALL PLAYERS ROLE", "ВЫДАТЬ РОЛЬ ВСЕМ"), distributorActiveBtn, GUILayout.Height(28)))
            {
                if (IsGhostRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost();
                else if (IsGhostImpostorRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost(true);
                else
                    SetAllPlayersRole(roleAssignOptions[allPlayersRoleAssignIdx]);
            }
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("ALL -> GHOST", "ВСЕ -> ПРИЗРАКИ"), distributorBtn, GUILayout.Height(26)))
                SetAllPlayersGhost();
            if (GUILayout.Button(L("REVIVE ALL", "ВОСКРЕСИТЬ ВСЕХ"), distributorActiveBtn, GUILayout.Height(26)))
                ReviveAllPlayers();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("ALL -> GHOST IMP", "ВСЕ -> ПРИЗРАКИ-ИМПОСТЕРЫ"), distributorBtn, GUILayout.Height(26)))
                SetAllPlayersGhost(true);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            float listWidth = Mathf.Floor(Mathf.Clamp(roleMgrWidth * 0.36f, 96f, 150f));
            float actionWidth = Mathf.Max(96f, roleMgrWidth - listWidth - 8f);
            GUILayout.BeginHorizontal(GUILayout.Width(roleMgrWidth));
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(listWidth), GUILayout.Height(315));
            preRolesListScrollPos = GUILayout.BeginScrollView(preRolesListScrollPos, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.ExpandHeight(true));
            foreach (var pc in lockedPlayersList)
            {
                if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                string pName = pc.Data.PlayerName ?? "Unknown";
                string fc = GetRoleForceKey(pc);
                // Replace 18 before 8: "18" -> "1Noi" if the Noisemaker substitution runs first.
                if (TryGetForcedRole(pc, out RoleTypes rowRole)) { string rShort = rowRole.ToString().Replace("9", "Pha").Replace("10", "Tra").Replace("18", "Vip").Replace("12", "Det").Replace("8", "Noi"); if (rShort.Length > 3) rShort = rShort.Substring(0, 3); pName += $" [{rShort}]"; }
                else if (IsForcedImp(pc)) pName += " [Imp]";
                bool isSelected = !string.IsNullOrEmpty(fc) ? selectedPreRoleFc == fc : selectedPreRoleId == pc.PlayerId;
                try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }
                if (GUILayout.Button(pName, isSelected ? CreateClippedButtonStyle(activeTabStyle) : CreateClippedButtonStyle(btnStyle), GUILayout.Height(30))) { selectedPreRoleId = pc.PlayerId; selectedPreRoleFc = fc; }
                GUI.contentColor = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(actionWidth), GUILayout.Height(315));
            preRolesActionScrollPos = GUILayout.BeginScrollView(preRolesActionScrollPos, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.ExpandHeight(true));
            PlayerControl target = !string.IsNullOrEmpty(selectedPreRoleFc)
                ? lockedPlayersList.FirstOrDefault(p => GetRoleForceKey(p) == selectedPreRoleFc)
                : lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedPreRoleId);
            if (target != null && target.Data != null)
            {
                GUILayout.Label($"<color=#aaaaaa>Selecting role for:</color> {target.Data.PlayerName}", richLabelStyle14);
                RoleTypes currentForced = TryGetForcedRole(target, out RoleTypes targetRole) ? targetRole : RoleTypes.Crewmate;
                bool isForced = IsForced(target);
                // Replace 18 before 8, otherwise Viper (18) renders as "1Noisemaker".
                string roleNameStr = currentForced.ToString().Replace("9", "Phantom").Replace("10", "Tracker").Replace("18", "Viper").Replace("12", "Detective").Replace("8", "Noisemaker");
                if (IsForcedImp(target)) roleNameStr = "Impostor";
                string targetFc = GetRoleForceKey(target);
                GUILayout.Label($"<color=#aaaaaa>Status:</color> {(isForced ? $"<color=#00FF00>Forced ({roleNameStr})</color>" : "<color=#FF0000>Not Forced (Random)</color>")}", richLabelStyle14);
                GUILayout.Label($"<color=#aaaaaa>FC:</color> {(string.IsNullOrEmpty(targetFc) ? "none, fallback PlayerId" : targetFc)}", richLabelStyle11);
                GUILayout.Space(15);
                GUIStyle roleChoiceBtn = CreateClippedButtonStyle(btnStyle);
                GUIStyle roleChoiceActiveBtn = CreateClippedButtonStyle(activeTabStyle);
                DrawMenuSectionHeader("IMPOSTOR ROLES (Red Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(L("Impostor", "Импостер"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedImp(target); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Shapeshifter", "Шейпшифтер"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, RoleTypes.Shapeshifter); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Phantom", "Фантом"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, (RoleTypes)9); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Viper", "Гадюка"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, (RoleTypes)18); NormalizeForcedRoleKeys(target); }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                DrawMenuSectionHeader("CREWMATE ROLES (Blue Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(L("Crewmate", "Крюмен"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, RoleTypes.Crewmate); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Engineer", "Инженер"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, RoleTypes.Engineer); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Scientist", "Учёный"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, RoleTypes.Scientist); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Tracker", "Трекер"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, (RoleTypes)10); NormalizeForcedRoleKeys(target); }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(L("Noisemaker", "Шумовик"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, (RoleTypes)8); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Guardian Angel", "Ангел-хранитель"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, RoleTypes.GuardianAngel); NormalizeForcedRoleKeys(target); }
                if (GUILayout.Button(L("Detective", "Детектив"), roleChoiceBtn, GUILayout.Height(24))) { SetForcedRole(target, (RoleTypes)12); NormalizeForcedRoleKeys(target); }
                GUILayout.EndHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button(L("REMOVE FORCED ROLE", "УБРАТЬ ВЫДАННУЮ РОЛЬ"), roleChoiceActiveBtn, GUILayout.Height(35))) ClearForcedRole(target);
                GUILayout.Space(20);
            GUILayout.Label($"<color=#777777><b>{L("Hide & Seek Notice:", "Важно для Hide & Seek:")}</b>\n{L("Selecting Impostor/Shapeshifter/Phantom/Viper expands the Seeker limit in Hide & Seek.", "Выбор импостера, шейпшифтера, фантома или гадюки увеличивает лимит искателей в Hide & Seek.")}</color>", richWrapLabelStyle12);
            }
            else
            {
                GUILayout.FlexibleSpace();
            GUILayout.Label($"<color=#777777>{L("Select a player to set their role", "Выберите игрока для выдачи роли")}</color>", centeredRichLabelStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

private static void NormalizeForcedRoleKeys(PlayerControl pc)
        {
            if (pc == null || pc.Data == null) return;
            string fc = GetRoleForceKey(pc);
            if (string.IsNullOrEmpty(fc)) return;
            if (forcedPreGameRoleFcs.ContainsKey(fc) || forcedImpostorFcs.Contains(fc))
            {
                forcedPreGameRoles.Remove(pc.PlayerId);
                forcedImpostors.Remove(pc.PlayerId);
            }
        }
    }
}
