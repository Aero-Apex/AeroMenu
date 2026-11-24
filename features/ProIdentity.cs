#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using InnerNet;
using AmongUs.GameOptions;
using BepInEx;
using HarmonyLib;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ===================================================================
        // Group U state (public static, exact contract names)
        // ===================================================================
        public static bool proRandomName = false;
        public static bool proRandomColorOnStart = false;
        public static bool proOutfitRotation = false;
        public static bool proNameplateFx = false;
        public static bool proPetSpin = false;

        // ===================================================================
        // Private helpers / caches (ALL suffixed with U)
        // ===================================================================
        private const BindingFlags ProUFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private const string ProUNameplateMark = " \u2605";
        private const float ProUWriteInterval = 0.25f;
        private const float ProUOutfitInterval = 8f;

        // Per-stream write throttles. Every name/cosmetic write stream is
        // limited to at most one write per 0.25s (IL2CPP string crashes).
        private static float proUNextNameWriteAt;
        private static float proUNextColorWriteAt;
        private static float proUNextOutfitWriteAt;
        private static float proUNextPetWriteAt;

        // Cached originals - restored whenever the matching feature is off.
        private static bool proUHasOriginalName;
        private static string proUOriginalName;
        private static string proULastWrittenName;
        private static string proURandomName;

        // Composed-name cache: avoids rebuilding the display string every frame.
        private static bool proUHasComposedName;
        private static string proUComposedName;
        private static string proUComposedFromName;
        private static string proUComposedRandomName;
        private static bool proUComposedMark;

        private static bool proUHasOriginalColor;
        private static int proUOriginalColor;

        private static bool proUHasOriginalOutfit;
        private static int proUOriginalColorId;
        private static string proUOriginalHatId;
        private static string proUOriginalSkinId;
        private static string proUOriginalVisorId;
        private static string proUOriginalNamePlateId;
        private static string proUOriginalPetId;

        private static object proUPetTransform;
        private static Quaternion proUPetOriginalRotation;
        private static bool proUHasPetRotation;
        private static float proUPetAngle;

        private static float proUNextOutfitRotationAt;
        private static int proULastGameId = int.MinValue;
        private static bool proUPrevColorOnStart;

        private static System.Random proURandom;
        private static readonly string[] ProUNamePrefixes =
        {
            "Suspicious", "Sneaky", "Turbo", "Cosmic", "Toasty", "Wobbly",
            "Ninja", "Fluffy", "Cargo", "Neon", "Quirky", "Sleepy"
        };
        private static readonly string[] ProUNameSuffixes =
        {
            "Bean", "Fox", "Crew", "Ghost", "Pingu", "Mango",
            "Rocket", "Noodle", "Panda", "Wizard", "Duck", "Kitten"
        };

        // ===================================================================
        // Required public surface
        // ===================================================================

        internal void DrawProU(float width)
        {
            try
            {
                float workWidth = GetMenuWorkWidth(160f, Mathf.Max(220f, width));
                float buttonWidth = Mathf.Clamp(workWidth * 0.36f, 90f, 150f);

                GUILayout.Label(MenuText(L("IDENTITY FX", "\u0418\u0414\u0415\u041D\u0422\u0418\u0424\u0418\u041A\u0410\u0426\u0418\u042F")), headerStyle);
                GUILayout.Space(6);
                GUILayout.Label(MenuText(L("Local-only cosmetic identity tweaks. Nothing is sent to the server.",
                    "\u0422\u043E\u043B\u044C\u043A\u043E \u043B\u043E\u043A\u0430\u043B\u044C\u043D\u044B\u0435 \u043A\u043E\u0441\u043C\u0435\u0442\u0438\u0447\u0435\u0441\u043A\u0438\u0435 \u044D\u0444\u0444\u0435\u043A\u0442\u044B. \u041D\u0438\u0447\u0435\u0433\u043E \u043D\u0435 \u043E\u0442\u043F\u0440\u0430\u0432\u043B\u044F\u0435\u0442\u0441\u044F \u043D\u0430 \u0441\u0435\u0440\u0432\u0435\u0440.")), menuDescStyle);
                GUILayout.Space(6);

                // --- Random nickname ---
                bool wasRandomName = proRandomName;
                proRandomName = DrawToggle(proRandomName, L("Random Nickname", "\u0421\u043B\u0443\u0447\u0430\u0439\u043D\u044B\u0439 \u043D\u0438\u043A"), 260);
                GUILayout.Label(MenuText(L("Generates a random local nickname. Restores your real name when disabled.",
                    "\u0413\u0435\u043D\u0435\u0440\u0438\u0440\u0443\u0435\u0442 \u0441\u043B\u0443\u0447\u0430\u0439\u043D\u044B\u0439 \u043B\u043E\u043A\u0430\u043B\u044C\u043D\u044B\u0439 \u043D\u0438\u043A. \u041F\u0440\u0438 \u043E\u0442\u043A\u043B\u044E\u0447\u0435\u043D\u0438\u0438 \u0432\u043E\u0437\u0432\u0440\u0430\u0449\u0430\u0435\u0442 \u0432\u0430\u0448\u0435 \u0438\u043C\u044F.")), menuDescStyle);

                GUILayout.BeginHorizontal();
                string shownName = proRandomName && !string.IsNullOrEmpty(proURandomName)
                    ? proURandomName
                    : (proUOriginalName ?? L("(your name)", "(\u0432\u0430\u0448\u0435 \u0438\u043C\u044F)"));
                GUILayout.Label(shownName, toggleLabelStyle != null ? toggleLabelStyle : btnStyle,
                    GUILayout.ExpandWidth(true), GUILayout.Height(26));
                GUI.enabled = proRandomName;
                if (GUILayout.Button(L("Roll Name", "\u0413\u0435\u043D\u0435\u0440\u0438\u0440\u043E\u0432\u0430\u0442\u044C"), btnStyle,
                        GUILayout.Width(buttonWidth), GUILayout.Height(26)))
                    RollRandomNameU();
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (wasRandomName && !proRandomName) proURandomName = null;
                GUILayout.Space(8);

                // --- Random color on round start ---
                proRandomColorOnStart = DrawToggle(proRandomColorOnStart,
                    L("Random Color On Start", "\u0421\u043B\u0443\u0447\u0430\u0439\u043D\u044B\u0439 \u0446\u0432\u0435\u0442 \u043D\u0430 \u0441\u0442\u0430\u0440\u0442\u0435"), 260);
                GUILayout.Label(MenuText(L("Picks a new local color when a new round begins.",
                    "\u0412\u044B\u0431\u0438\u0440\u0430\u0435\u0442 \u043D\u043E\u0432\u044B\u0439 \u043B\u043E\u043A\u0430\u043B\u044C\u043D\u044B\u0439 \u0446\u0432\u0435\u0442 \u0432 \u043D\u0430\u0447\u0430\u043B\u0435 \u0440\u0430\u0443\u043D\u0434\u0430.")), menuDescStyle);
                GUILayout.Space(6);

                // --- Outfit rotation ---
                proOutfitRotation = DrawToggle(proOutfitRotation,
                    L("Outfit Rotation", "\u0421\u043C\u0435\u043D\u0430 \u043E\u0431\u0440\u0430\u0437\u0430"), 260);
                GUILayout.Label(MenuText(L("Periodically rotates your local hat, skin, visor, nameplate, pet and color.",
                    "\u041F\u0435\u0440\u0438\u043E\u0434\u0438\u0447\u0435\u0441\u043A\u0438 \u043C\u0435\u043D\u044F\u0435\u0442 \u043B\u043E\u043A\u0430\u043B\u044C\u043D\u044B\u0435 \u0448\u0430\u043F\u043A\u0443, \u043A\u043E\u0441\u0442\u044E\u043C, \u0432\u0438\u0437\u043E\u0440, \u0442\u0430\u0431\u043B\u0438\u0447\u043A\u0443, \u043F\u0438\u0442\u043E\u043C\u0446\u0430 \u0438 \u0446\u0432\u0435\u0442.")), menuDescStyle);
                GUILayout.Space(6);

                // --- Nameplate marker ---
                proNameplateFx = DrawToggle(proNameplateFx,
                    L("Nameplate Marker", "\u041C\u0435\u0442\u043A\u0430 \u043D\u0430 \u0438\u043C\u044F"), 260);
                GUILayout.Label(MenuText(L("Adds a small decorative mark to your local name.",
                    "\u0414\u043E\u0431\u0430\u0432\u043B\u044F\u0435\u0442 \u043D\u0435\u0431\u043E\u043B\u044C\u0448\u0443\u044E \u0434\u0435\u043A\u043E\u0440\u0430\u0442\u0438\u0432\u043D\u0443\u044E \u043C\u0435\u0442\u043A\u0443 \u043A \u0432\u0430\u0448\u0435\u043C\u0443 \u0438\u043C\u0435\u043D\u0438.")), menuDescStyle);
                GUILayout.Space(6);

                // --- Pet spin ---
                proPetSpin = DrawToggle(proPetSpin, L("Pet Spin", "\u0412\u0440\u0430\u0449\u0435\u043D\u0438\u0435 \u043F\u0438\u0442\u043E\u043C\u0446\u0430"), 260);
                GUILayout.Label(MenuText(L("Spins the local pet cosmetic, if one is equipped.",
                    "\u0412\u0440\u0430\u0449\u0430\u0435\u0442 \u043B\u043E\u043A\u0430\u043B\u044C\u043D\u043E\u0433\u043E \u043F\u0438\u0442\u043E\u043C\u0446\u0430, \u0435\u0441\u043B\u0438 \u043E\u043D \u043D\u0430\u0434\u0435\u0442.")), menuDescStyle);
            }
            catch { }
        }

        internal static void TickProU(bool inputBlocked)
        {
            try
            {
                // Restore cached originals while the matching feature is disabled.
                if (!proRandomName && !proNameplateFx && proUHasOriginalName) RestoreLocalNameU();
                if (!proRandomColorOnStart && proUHasOriginalColor) RestoreColorU();
                if (!proOutfitRotation && proUHasOriginalOutfit) RestoreOutfitU();
                if (!proPetSpin && proUHasPetRotation) RestorePetSpinU();

                // Apply the color right away when the toggle is turned on mid-session.
                if (proRandomColorOnStart && !proUPrevColorOnStart) ApplyRandomColorU();
                proUPrevColorOnStart = proRandomColorOnStart;

                TickRandomColorOnStartU();
                ApplyDesiredLocalNameU();
                TickOutfitRotationU();
                TickPetSpinU();

                // This group has no hotkeys. Keep the input guard explicit so any
                // future Input.GetKeyDown handling stays skipped while typing.
                if (inputBlocked) return;
            }
            catch { }
        }

        internal static void LoadProU()
        {
            try
            {
                proRandomName = LoadBool("M_ProU_proRandomName", proRandomName);
                proRandomColorOnStart = LoadBool("M_ProU_proRandomColorOnStart", proRandomColorOnStart);
                proOutfitRotation = LoadBool("M_ProU_proOutfitRotation", proOutfitRotation);
                proNameplateFx = LoadBool("M_ProU_proNameplateFx", proNameplateFx);
                proPetSpin = LoadBool("M_ProU_proPetSpin", proPetSpin);

                proUPrevColorOnStart = proRandomColorOnStart;

                // The toggle persists but the generated name did not; restore
                // it on load so the feature still works after a relaunch.
                if (proRandomName && string.IsNullOrEmpty(proURandomName))
                    proURandomName = GenerateNicknameU();
            }
            catch { }
        }

        internal static void SaveProU()
        {
            try
            {
                SaveBool("M_ProU_proRandomName", proRandomName);
                SaveBool("M_ProU_proRandomColorOnStart", proRandomColorOnStart);
                SaveBool("M_ProU_proOutfitRotation", proOutfitRotation);
                SaveBool("M_ProU_proNameplateFx", proNameplateFx);
                SaveBool("M_ProU_proPetSpin", proPetSpin);
            }
            catch { }
        }

        internal static void DrawProUOverlay()
        {
            // Identity changes are applied straight to the local player's
            // cosmetics/name; no screen-space overlay is needed for this group.
            if (Screen.width <= 1 || Screen.height <= 1) return;
        }

        // ===================================================================
        // Name helpers
        // ===================================================================

        private static System.Random GetRandomU()
        {
            if (proURandom == null)
            {
                try { proURandom = new System.Random(); }
                catch { proURandom = new System.Random(12345); }
            }
            return proURandom;
        }

        private static string GenerateNicknameU()
        {
            try
            {
                System.Random rng = GetRandomU();
                string first = ProUNamePrefixes[rng.Next(ProUNamePrefixes.Length)];
                string second = ProUNameSuffixes[rng.Next(ProUNameSuffixes.Length)];
                string name = first + second;
                if (name.Length > 12) name = name.Substring(0, 12);
                if (rng.Next(3) == 0) name += rng.Next(10).ToString();
                return name;
            }
            catch { return "AeroCrew"; }
        }

        private static void RollRandomNameU()
        {
            string name = GenerateNicknameU();
            proURandomName = name;
            proRandomName = true;

            try
            {
                object player = GetLocalPlayerU();
                object data = player != null ? ReadMemberU(player, "Data") : null;
                if (data != null && !proUHasOriginalName)
                {
                    string original = ReadStringMemberU(data, "PlayerName");
                    if (!string.IsNullOrEmpty(original))
                    {
                        proUOriginalName = original;
                        proUHasOriginalName = true;
                    }
                }
            }
            catch { }

            ShowNotification("<color=#00FFAA>[ID]</color> " +
                L("Random name", "\u0421\u043B\u0443\u0447\u0430\u0439\u043D\u043E\u0435 \u0438\u043C\u044F") + ": <b>" + name + "</b>");
        }

        private static void ApplyDesiredLocalNameU()
        {
            object player = GetLocalPlayerU();
            if (player == null) return;
            object data = ReadMemberU(player, "Data");
            if (data == null) return;

            if (!proUHasOriginalName)
            {
                string original = ReadStringMemberU(data, "PlayerName");
                if (string.IsNullOrEmpty(original)) return;
                proUOriginalName = original;
                proUHasOriginalName = true;
            }

            string randomKey = proRandomName && !string.IsNullOrEmpty(proURandomName)
                ? proURandomName : string.Empty;
            if (!proUHasComposedName ||
                proUComposedFromName != proUOriginalName ||
                proUComposedRandomName != randomKey ||
                proUComposedMark != proNameplateFx)
            {
                string baseName = string.IsNullOrEmpty(randomKey) ? proUOriginalName : randomKey;
                proUComposedName = proNameplateFx ? baseName + ProUNameplateMark : baseName;
                proUComposedFromName = proUOriginalName;
                proUComposedRandomName = randomKey;
                proUComposedMark = proNameplateFx;
                proUHasComposedName = true;
            }

            string desired = proUComposedName;
            if (string.IsNullOrEmpty(desired)) return;
            if (desired == proULastWrittenName) return;
            if (!TryTakeWriteSlotU(ref proUNextNameWriteAt)) return;

            WriteLocalNameU(player, data, desired);
            proULastWrittenName = desired;
        }

        private static void WriteLocalNameU(object player, object data, string text)
        {
            if (player == null || data == null) return;
            InvokeCosmeticMethodU(ReadMemberU(player, "cosmetics"), "SetName", text);
            WriteMemberU(data, "PlayerName", text);

            object defaultOutfit = ReadMemberU(data, "DefaultOutfit");
            if (defaultOutfit != null) WriteMemberU(defaultOutfit, "PlayerName", text);

            object currentOutfit = ReadMemberU(player, "CurrentOutfit");
            if (currentOutfit != null) WriteMemberU(currentOutfit, "PlayerName", text);
        }

        private static void RestoreLocalNameU()
        {
            if (!proUHasOriginalName) return;
            if (!TryTakeWriteSlotU(ref proUNextNameWriteAt)) return;

            object player = GetLocalPlayerU();
            if (player == null) return;
            object data = ReadMemberU(player, "Data");
            if (data == null) return;

            WriteLocalNameU(player, data, proUOriginalName);
            proULastWrittenName = proUOriginalName;
            proURandomName = null;
            proUHasOriginalName = false;
            proUHasComposedName = false;
            proUComposedName = null;
        }

        // ===================================================================
        // Color helpers
        // ===================================================================

        private static void TickRandomColorOnStartU()
        {
            if (!proRandomColorOnStart) return;
            int gameId = ReadIntMemberU(AmongUsClient.Instance, "GameId", int.MinValue);
            if (gameId == int.MinValue) return;
            if (gameId == proULastGameId) return;

            proULastGameId = gameId;
            ApplyRandomColorU();
        }

        private static void ApplyRandomColorU()
        {
            object player = GetLocalPlayerU();
            if (player == null) return;
            object data = ReadMemberU(player, "Data");
            object outfit = data != null ? ReadMemberU(data, "DefaultOutfit") : null;
            if (outfit == null) return;

            int current = ReadIntMemberU(outfit, "ColorId", -1);
            if (!proUHasOriginalColor)
            {
                proUOriginalColor = current;
                proUHasOriginalColor = true;
            }

            int max = MaxColorIdU();
            if (max <= 0) return;
            int next = GetRandomU().Next(max + 1);
            if (next == current) next = (next + 1) % (max + 1);
            if (next == current) return;
            if (!TryTakeWriteSlotU(ref proUNextColorWriteAt)) return;

            WriteMemberU(outfit, "ColorId", next);
            InvokeCosmeticMethodU(ReadMemberU(player, "cosmetics"), "SetColor", next);
        }

        private static void RestoreColorU()
        {
            if (!proUHasOriginalColor) return;
            if (!TryTakeWriteSlotU(ref proUNextColorWriteAt)) return;

            object player = GetLocalPlayerU();
            if (player == null) return;
            object data = ReadMemberU(player, "Data");
            object outfit = data != null ? ReadMemberU(data, "DefaultOutfit") : null;
            if (outfit == null) return;

            WriteMemberU(outfit, "ColorId", proUOriginalColor);
            InvokeCosmeticMethodU(ReadMemberU(player, "cosmetics"), "SetColor", proUOriginalColor);
            proUHasOriginalColor = false;
        }

        private static int MaxColorIdU()
        {
            try
            {
                Type palette = typeof(PlayerControl).Assembly.GetType("Palette");
                if (palette != null)
                {
                    FieldInfo field = palette.GetField("PlayerColors", ProUFlags);
                    object colors = field != null ? field.GetValue(null) : null;
                    if (colors is Array array) return Mathf.Max(0, array.Length - 1);
                    object length = ReadMemberU(colors, "Length");
                    if (length is int len) return Mathf.Max(0, len - 1);
                }
            }
            catch { }
            return 17;
        }

        // ===================================================================
        // Outfit rotation helpers
        // ===================================================================

        private static void TickOutfitRotationU()
        {
            if (!proOutfitRotation) return;
            float now = Time.realtimeSinceStartup;
            if (now < proUNextOutfitRotationAt) return;
            proUNextOutfitRotationAt = now + ProUOutfitInterval;
            ApplyRandomOutfitU();
        }

        private static void ApplyRandomOutfitU()
        {
            object player = GetLocalPlayerU();
            if (player == null) return;
            object data = ReadMemberU(player, "Data");
            object outfit = data != null ? ReadMemberU(data, "DefaultOutfit") : null;
            if (outfit == null) return;

            if (!proUHasOriginalOutfit)
            {
                proUOriginalColorId = ReadIntMemberU(outfit, "ColorId", -1);
                proUOriginalHatId = ReadStringMemberU(outfit, "HatId");
                proUOriginalSkinId = ReadStringMemberU(outfit, "SkinId");
                proUOriginalVisorId = ReadStringMemberU(outfit, "VisorId");
                proUOriginalNamePlateId = ReadStringMemberU(outfit, "NamePlateId");
                proUOriginalPetId = ReadStringMemberU(outfit, "PetId");
                proUHasOriginalOutfit = true;
            }

            if (!TryTakeWriteSlotU(ref proUNextOutfitWriteAt)) return;

            object manager = GetHatManagerU();
            string hat = PickRandomIdU(manager, "allHats", proUOriginalHatId);
            string skin = PickRandomIdU(manager, "allSkins", proUOriginalSkinId);
            string visor = PickRandomIdU(manager, "allVisors", proUOriginalVisorId);
            string plate = PickRandomIdU(manager, "allNamePlates", proUOriginalNamePlateId);
            string pet = PickRandomIdU(manager, "allPets", proUOriginalPetId);

            int max = MaxColorIdU();
            int color = max > 0 ? GetRandomU().Next(max + 1) : proUOriginalColorId;

            WriteMemberU(outfit, "HatId", hat ?? string.Empty);
            WriteMemberU(outfit, "SkinId", skin ?? string.Empty);
            WriteMemberU(outfit, "VisorId", visor ?? string.Empty);
            WriteMemberU(outfit, "NamePlateId", plate ?? string.Empty);
            WriteMemberU(outfit, "PetId", pet ?? string.Empty);
            if (color >= 0) WriteMemberU(outfit, "ColorId", color);

            object cosmetics = ReadMemberU(player, "cosmetics");
            InvokeCosmeticMethodU(cosmetics, "SetHat", hat ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetSkin", skin ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetVisor", visor ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetNamePlate", plate ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetPet", pet ?? string.Empty);
            if (color >= 0) InvokeCosmeticMethodU(cosmetics, "SetColor", color);
        }

        private static void RestoreOutfitU()
        {
            if (!proUHasOriginalOutfit) return;
            if (!TryTakeWriteSlotU(ref proUNextOutfitWriteAt)) return;

            object player = GetLocalPlayerU();
            if (player == null) return;
            object data = ReadMemberU(player, "Data");
            object outfit = data != null ? ReadMemberU(data, "DefaultOutfit") : null;
            if (outfit == null) return;

            WriteMemberU(outfit, "HatId", proUOriginalHatId ?? string.Empty);
            WriteMemberU(outfit, "SkinId", proUOriginalSkinId ?? string.Empty);
            WriteMemberU(outfit, "VisorId", proUOriginalVisorId ?? string.Empty);
            WriteMemberU(outfit, "NamePlateId", proUOriginalNamePlateId ?? string.Empty);
            WriteMemberU(outfit, "PetId", proUOriginalPetId ?? string.Empty);
            if (proUOriginalColorId >= 0) WriteMemberU(outfit, "ColorId", proUOriginalColorId);

            object cosmetics = ReadMemberU(player, "cosmetics");
            InvokeCosmeticMethodU(cosmetics, "SetHat", proUOriginalHatId ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetSkin", proUOriginalSkinId ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetVisor", proUOriginalVisorId ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetNamePlate", proUOriginalNamePlateId ?? string.Empty);
            InvokeCosmeticMethodU(cosmetics, "SetPet", proUOriginalPetId ?? string.Empty);
            if (proUOriginalColorId >= 0) InvokeCosmeticMethodU(cosmetics, "SetColor", proUOriginalColorId);

            proUHasOriginalOutfit = false;
        }

        private static object GetHatManagerU()
        {
            try
            {
                Type type = typeof(PlayerControl).Assembly.GetType("HatManager");
                if (type == null) return null;
                PropertyInfo property = type.GetProperty("Instance", ProUFlags);
                if (property != null) return property.GetValue(null, null);
                FieldInfo field = type.GetField("Instance", ProUFlags);
                if (field != null) return field.GetValue(null);
            }
            catch { }
            return null;
        }

        private static List<string> GetCosmeticIdsU(object manager, string fieldName)
        {
            List<string> ids = new List<string>();
            if (manager == null) return ids;

            object array = ReadMemberU(manager, fieldName);
            if (array == null) return ids;

            try
            {
                if (array is Array plain)
                {
                    foreach (object item in plain)
                    {
                        string id = ReadStringMemberU(item, "ProdId");
                        if (!string.IsNullOrEmpty(id)) ids.Add(id);
                    }
                    return ids;
                }
            }
            catch { }

            try
            {
                int count = 0;
                object countValue = ReadMemberU(array, "Count");
                if (countValue is int c) count = c;
                else
                {
                    object lengthValue = ReadMemberU(array, "Length");
                    if (lengthValue is int len) count = len;
                }

                PropertyInfo indexer = array.GetType().GetProperty("Item", ProUFlags);
                for (int i = 0; i < count; i++)
                {
                    object item = indexer != null ? indexer.GetValue(array, new object[] { i }) : null;
                    string id = ReadStringMemberU(item, "ProdId");
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
            }
            catch { }

            return ids;
        }

        private static string PickRandomIdU(object manager, string fieldName, string fallback)
        {
            try
            {
                List<string> ids = GetCosmeticIdsU(manager, fieldName);
                if (ids.Count == 0) return fallback;
                return ids[GetRandomU().Next(ids.Count)];
            }
            catch { return fallback; }
        }

        // ===================================================================
        // Pet spin helpers
        // ===================================================================

        private static void TickPetSpinU()
        {
            if (!proPetSpin) return;

            object player = GetLocalPlayerU();
            if (player == null) return;
            object pet = FindPetTransformU(player);
            if (pet == null) return;

            if (!ReferenceEquals(proUPetTransform, pet))
            {
                Quaternion rotation = ReadQuaternionMemberU(pet, "localRotation");
                proUPetOriginalRotation = rotation;
                proUHasPetRotation = true;
                proUPetTransform = pet;
                proUPetAngle = 0f;
            }

            if (!TryTakeWriteSlotU(ref proUNextPetWriteAt)) return;

            proUPetAngle = (proUPetAngle + 22.5f) % 360f;
            Quaternion spin = Quaternion.AngleAxis(proUPetAngle, Vector3.forward) * proUPetOriginalRotation;
            WriteMemberU(pet, "localRotation", spin);
        }

        private static void RestorePetSpinU()
        {
            if (!proUHasPetRotation || proUPetTransform == null) return;
            if (!TryTakeWriteSlotU(ref proUNextPetWriteAt)) return;

            WriteMemberU(proUPetTransform, "localRotation", proUPetOriginalRotation);
            proUHasPetRotation = false;
            proUPetTransform = null;
            proUPetAngle = 0f;
        }

        private static object FindPetTransformU(object player)
        {
            object cosmetics = ReadMemberU(player, "cosmetics");
            if (cosmetics != null)
            {
                string[] candidates = { "petSprite", "PetSprite", "pet", "Pet", "petObject", "PetObject" };
                foreach (string name in candidates)
                {
                    Transform found = AsTransformU(ReadMemberU(cosmetics, name));
                    if (found != null) return found;
                }

                try
                {
                    foreach (FieldInfo field in cosmetics.GetType().GetFields(ProUFlags))
                    {
                        if (field.FieldType != typeof(Transform) && field.FieldType != typeof(GameObject)) continue;
                        Transform found = AsTransformU(field.GetValue(cosmetics));
                        if (found == null) continue;
                        if (field.Name.IndexOf("pet", StringComparison.OrdinalIgnoreCase) >= 0) return found;
                        if (found.name != null && found.name.IndexOf("pet", StringComparison.OrdinalIgnoreCase) >= 0) return found;
                    }
                }
                catch { }
            }

            try
            {
                Component component = player as Component;
                if (component != null)
                {
                    Transform[] children = component.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        if (child == null || child.name == null) continue;
                        if (child.name.IndexOf("pet", StringComparison.OrdinalIgnoreCase) >= 0) return child;
                    }
                }
            }
            catch { }

            return null;
        }

        private static Transform AsTransformU(object value)
        {
            if (value is Transform transform) return transform;
            if (value is GameObject gameObject) return gameObject.transform;
            return null;
        }

        private static Quaternion ReadQuaternionMemberU(object target, string name)
        {
            object value = ReadMemberU(target, name);
            return value is Quaternion quaternion ? quaternion : Quaternion.identity;
        }

        // ===================================================================
        // Generic reflection helpers (all guarded, silent no-op on failure)
        // ===================================================================

        private static object GetLocalPlayerU()
        {
            try { return PlayerControl.LocalPlayer; }
            catch { return null; }
        }

        private static object ReadMemberU(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                FieldInfo field = target.GetType().GetField(name, ProUFlags);
                if (field != null) return field.GetValue(target);
            }
            catch { }
            try
            {
                PropertyInfo property = target.GetType().GetProperty(name, ProUFlags);
                if (property != null && property.CanRead) return property.GetValue(target, null);
            }
            catch { }
            return null;
        }

        private static void WriteMemberU(object target, string name, object value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return;
            try
            {
                PropertyInfo property = target.GetType().GetProperty(name, ProUFlags);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(target, ConvertArgU(value, property.PropertyType), null);
                    return;
                }
            }
            catch { }
            try
            {
                FieldInfo field = target.GetType().GetField(name, ProUFlags);
                if (field != null) field.SetValue(target, ConvertArgU(value, field.FieldType));
            }
            catch { }
        }

        private static string ReadStringMemberU(object target, string name)
        {
            object value = ReadMemberU(target, name);
            return value is string text ? text : null;
        }

        private static int ReadIntMemberU(object target, string name, int fallback)
        {
            object value = ReadMemberU(target, name);
            if (value == null) return fallback;
            try { return Convert.ToInt32(value); }
            catch { return fallback; }
        }

        private static object ConvertArgU(object value, Type targetType)
        {
            try
            {
                if (value == null)
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                if (targetType.IsInstanceOfType(value)) return value;
                if (targetType == typeof(byte)) return (byte)(Convert.ToInt32(value) & 0xFF);
                if (targetType == typeof(int)) return Convert.ToInt32(value);
                if (targetType == typeof(string)) return value.ToString();
            }
            catch { }
            return value;
        }

        private static void InvokeCosmeticMethodU(object target, string methodName, object argument)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;
            try
            {
                IEnumerable<MethodInfo> methods = target.GetType().GetMethods(ProUFlags)
                    .Where(m => m.Name == methodName && m.GetParameters().Length == 1);
                foreach (MethodInfo method in methods)
                {
                    try
                    {
                        method.Invoke(target, new[] { argument });
                        return;
                    }
                    catch { }

                    try
                    {
                        Type parameterType = method.GetParameters()[0].ParameterType;
                        object converted = ConvertArgU(argument, parameterType);
                        if (converted != null)
                        {
                            method.Invoke(target, new[] { converted });
                            return;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static bool TryTakeWriteSlotU(ref float slot)
        {
            float now = Time.realtimeSinceStartup;
            if (now < slot) return false;
            slot = now + ProUWriteInterval;
            return true;
        }
    }
}
