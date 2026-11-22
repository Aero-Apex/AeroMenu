#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ---------------------------------------------------------------------
        // Group J - Cosmetics (LOCAL cosmetic effects only, no server claims)
        // Public state (exact field names required by the contract)
        // ---------------------------------------------------------------------
        public static bool extraNameSuffix = false;
        public static string extraNameSuffixText = " [Aero]";
        public static bool extraRainbowName = false;
        public static bool extraPlayerScale = false;
        public static float extraPlayerScaleValue = 1.3f;
        public static bool extraCosmeticSpin = false;
        public static bool extraLocalColorLock = false;
        public static int extraLocalColorLockId = 0;

        // ---------------------------------------------------------------------
        // Private caches / throttles (all helpers end with J)
        // ---------------------------------------------------------------------
        private const BindingFlags J_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] J_RAINBOW = new string[]
        {
            "FF3B3B", "FF9F1C", "FFE600", "3BFF66", "3BD6FF", "6A5BFF", "D94DFF", "FF5FA2"
        };

        private static readonly uint[] J_COSMETIC_IDS = new uint[]
        {
            0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u, 9u, 10u,
            11u, 12u, 13u, 14u, 15u, 16u, 17u, 18u, 19u, 20u
        };

        private static readonly string[] J_HAT_FIELDS = new string[]
        {
            "HatId", "hatId", "HatID", "hatID", "Hat"
        };
        private static readonly string[] J_VISOR_FIELDS = new string[]
        {
            "VisorId", "visorId", "VisorID", "visorID", "Visor"
        };
        private static readonly string[] J_SKIN_FIELDS = new string[]
        {
            "SkinId", "skinId", "SkinID", "skinID", "Skin"
        };
        private static readonly string[] J_LAYER_MEMBERS = new string[]
        {
            "CosmeticsLayer", "cosmeticsLayer", "Layer", "layer"
        };
        private static readonly string[] J_REFRESH_METHODS = new string[]
        {
            "UpdateCosmetics", "RefreshCosmetics", "SetCosmetics", "UpdateVisuals", "Refresh"
        };

        private static PlayerControl jCacheOwner;

        private static bool jNameCached;
        private static string jOriginalName;
        private static bool jNameWasActive;
        private static int jRainbowIndex;
        private static float jNextNameWriteAt;
        private static float jNextRainbowAt;

        private static bool jScaleWasActive;

        private static bool jCosmeticsCached;
        private static bool jHatFound;
        private static bool jVisorFound;
        private static bool jSkinFound;
        private static uint jOriginalHat;
        private static uint jOriginalVisor;
        private static uint jOriginalSkin;
        private static bool jCosmeticsWasActive;
        private static int jCosmeticIndex;
        private static float jNextCosmeticAt;

        private static bool jColorCached;
        private static int jOriginalColorId;
        private static bool jColorWasActive;
        private static float jNextColorWriteAt;

        // Lazily cached GUI styles for the Draw method (never allocated per frame).
        private static GUIStyle jSuffixInputStyle;
        private static GUIStyle jValueLabelStyle;

        // ---------------------------------------------------------------------
        // Required public surface
        // ---------------------------------------------------------------------

        internal void DrawExtrasJ(float width)
        {
            float work = GetMenuWorkWidth(180f, 760f);
            int toggleWidth = Mathf.RoundToInt(Mathf.Clamp(width > 0f ? width : work, 120f, 900f));

            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                GUILayout.Label(L("COSMETICS (LOCAL ONLY)", "КОСМЕТИКА (ТОЛЬКО ЛОКАЛЬНО)"), headerStyle);
                GUILayout.Label(L("Visual-only changes for your own player.", "Только визуальные изменения вашего игрока."), menuDescStyle);
                GUILayout.Space(6f);

                // 1. Name suffix
                extraNameSuffix = DrawToggle(extraNameSuffix, L("Name Suffix", "Суффикс имени"), toggleWidth, "extraNameSuffixJ");
                GUILayout.BeginHorizontal(GUILayout.Width(toggleWidth));
                GUILayout.Label(L("Text:", "Текст:"), toggleLabelStyle, GUILayout.Width(58f));
                if (jSuffixInputStyle == null)
                {
                    jSuffixInputStyle = new GUIStyle(inputBlockStyle != null ? inputBlockStyle : btnStyle)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        wordWrap = false
                    };
                }
                extraNameSuffixText = GUILayout.TextField(
                    extraNameSuffixText ?? string.Empty,
                    jSuffixInputStyle,
                    GUILayout.Width(Mathf.Max(80f, toggleWidth - 66f)),
                    GUILayout.Height(22f));
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);

                // 2. Rainbow name
                extraRainbowName = DrawToggle(extraRainbowName, L("Rainbow Name", "Радужное имя"), toggleWidth, "extraRainbowNameJ");
                GUILayout.Space(4f);

                // 3. Player scale
                extraPlayerScale = DrawToggle(extraPlayerScale, L("Player Scale", "Масштаб игрока"), toggleWidth, "extraPlayerScaleJ");
                GUILayout.BeginHorizontal(GUILayout.Width(toggleWidth));
                GUILayout.Label(L("Scale:", "Масштаб:"), toggleLabelStyle, GUILayout.Width(58f));
                extraPlayerScaleValue = GUILayout.HorizontalSlider(
                    Mathf.Clamp(extraPlayerScaleValue, 0.05f, 5f), 0.05f, 5f,
                    GUILayout.Width(Mathf.Max(60f, toggleWidth - 130f)));
                extraPlayerScaleValue = Mathf.Clamp(extraPlayerScaleValue, 0.05f, 5f);
                if (jValueLabelStyle == null)
                {
                    jValueLabelStyle = new GUIStyle(toggleLabelStyle)
                    {
                        alignment = TextAnchor.MiddleRight,
                        clipping = TextClipping.Clip,
                        wordWrap = false,
                        richText = true
                    };
                }
                GUILayout.Label(extraPlayerScaleValue.ToString("0.00"), jValueLabelStyle, GUILayout.Width(52f));
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);

                // 4. Cosmetic spin
                extraCosmeticSpin = DrawToggle(extraCosmeticSpin, L("Cosmetic Spin", "Смена косметики"), toggleWidth, "extraCosmeticSpinJ");
                GUILayout.Space(4f);

                // 5. Local color lock
                extraLocalColorLock = DrawToggle(extraLocalColorLock, L("Local Color Lock", "Фикс. цвет (локально)"), toggleWidth, "extraLocalColorLockJ");
                GUILayout.BeginHorizontal(GUILayout.Width(toggleWidth));
                GUILayout.Label(L("Color ID:", "ID цвета:"), toggleLabelStyle, GUILayout.Width(58f));
                extraLocalColorLockId = Mathf.RoundToInt(GUILayout.HorizontalSlider(
                    Mathf.Clamp(extraLocalColorLockId, 0, 17), 0f, 17f,
                    GUILayout.Width(Mathf.Max(60f, toggleWidth - 130f))));
                extraLocalColorLockId = Mathf.Clamp(extraLocalColorLockId, 0, 17);
                GUILayout.Label(extraLocalColorLockId.ToString(), jValueLabelStyle, GUILayout.Width(52f));
                GUILayout.EndHorizontal();
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        internal static void TickExtrasJ(bool inputBlocked)
        {
            try
            {
                PlayerControl lp = PlayerControl.LocalPlayer;
                if (lp == null || lp.Data == null) return;

                CheckOwnerJ();

                try { TickSuffixAndRainbowJ(lp); } catch { }
                try { TickScaleJ(lp); } catch { }
                try { TickCosmeticSpinJ(lp); } catch { }
                try { TickColorLockJ(lp); } catch { }
            }
            catch { }
        }

        internal static void LoadExtrasJ()
        {
            extraNameSuffix = PlayerPrefs.GetInt("M_ExtrasJ_ExtraNameSuffix", 0) == 1;
            extraNameSuffixText = PlayerPrefs.GetString("M_ExtrasJ_ExtraNameSuffixText", " [Aero]");
            extraRainbowName = PlayerPrefs.GetInt("M_ExtrasJ_ExtraRainbowName", 0) == 1;
            extraPlayerScale = PlayerPrefs.GetInt("M_ExtrasJ_ExtraPlayerScale", 0) == 1;
            extraPlayerScaleValue = Mathf.Clamp(PlayerPrefs.GetFloat("M_ExtrasJ_ExtraPlayerScaleValue", 1.3f), 0.05f, 5f);
            extraCosmeticSpin = PlayerPrefs.GetInt("M_ExtrasJ_ExtraCosmeticSpin", 0) == 1;
            extraLocalColorLock = PlayerPrefs.GetInt("M_ExtrasJ_ExtraLocalColorLock", 0) == 1;
            extraLocalColorLockId = Mathf.Clamp(PlayerPrefs.GetInt("M_ExtrasJ_ExtraLocalColorLockId", 0), 0, 17);
        }

        internal static void SaveExtrasJ()
        {
            PlayerPrefs.SetInt("M_ExtrasJ_ExtraNameSuffix", extraNameSuffix ? 1 : 0);
            PlayerPrefs.SetString("M_ExtrasJ_ExtraNameSuffixText", extraNameSuffixText ?? string.Empty);
            PlayerPrefs.SetInt("M_ExtrasJ_ExtraRainbowName", extraRainbowName ? 1 : 0);
            PlayerPrefs.SetInt("M_ExtrasJ_ExtraPlayerScale", extraPlayerScale ? 1 : 0);
            PlayerPrefs.SetFloat("M_ExtrasJ_ExtraPlayerScaleValue", Mathf.Clamp(extraPlayerScaleValue, 0.05f, 5f));
            PlayerPrefs.SetInt("M_ExtrasJ_ExtraCosmeticSpin", extraCosmeticSpin ? 1 : 0);
            PlayerPrefs.SetInt("M_ExtrasJ_ExtraLocalColorLock", extraLocalColorLock ? 1 : 0);
            PlayerPrefs.SetInt("M_ExtrasJ_ExtraLocalColorLockId", Mathf.Clamp(extraLocalColorLockId, 0, 17));
        }

        internal static void DrawExtrasJOverlay()
        {
            // No overlay for this group.
        }

        // ---------------------------------------------------------------------
        // Owner / cache lifecycle
        // ---------------------------------------------------------------------

        private static void CheckOwnerJ()
        {
            PlayerControl lp = PlayerControl.LocalPlayer;
            if (lp == null) return;
            if (jCacheOwner == lp) return;

            jCacheOwner = lp;

            jNameCached = false;
            jOriginalName = null;
            jNameWasActive = false;
            jRainbowIndex = 0;
            jNextNameWriteAt = 0f;
            jNextRainbowAt = 0f;

            jScaleWasActive = false;

            jCosmeticsCached = false;
            jHatFound = false;
            jVisorFound = false;
            jSkinFound = false;
            jCosmeticsWasActive = false;
            jCosmeticIndex = 0;
            jNextCosmeticAt = 0f;

            jColorCached = false;
            jColorWasActive = false;
            jNextColorWriteAt = 0f;
        }

        // ---------------------------------------------------------------------
        // Feature 1 + 2: name suffix and rainbow name (shared name write path)
        // ---------------------------------------------------------------------

        private static void TickSuffixAndRainbowJ(PlayerControl lp)
        {
            bool active = extraNameSuffix || extraRainbowName;
            if (!active)
            {
                if (jNameWasActive) RestoreNameJ(lp);
                return;
            }

            jNameWasActive = true;
            CacheOriginalNameJ(lp);

            if (extraRainbowName && Time.unscaledTime >= jNextRainbowAt)
            {
                jRainbowIndex = (jRainbowIndex + 1) % J_RAINBOW.Length;
                jNextRainbowAt = Time.unscaledTime + 0.3f;
            }

            string desired = BuildDesiredNameJ();
            if (string.IsNullOrEmpty(desired)) return;

            // Throttle every name write to at most once per 0.25 seconds.
            if (Time.unscaledTime < jNextNameWriteAt) return;
            jNextNameWriteAt = Time.unscaledTime + 0.25f;
            WriteNameJ(lp, desired);
        }

        private static void CacheOriginalNameJ(PlayerControl lp)
        {
            if (jNameCached) return;
            if (lp == null || lp.Data == null) return;

            string n = ReadStringJ(lp.Data, new string[] { "PlayerName" });
            if (string.IsNullOrEmpty(n))
            {
                object outfit = ReadMemberJ(lp, new string[] { "CurrentOutfit" });
                n = ReadStringJ(outfit, new string[] { "PlayerName" });
            }
            if (string.IsNullOrEmpty(n))
            {
                object def = ReadMemberJ(lp.Data, new string[] { "DefaultOutfit" });
                n = ReadStringJ(def, new string[] { "PlayerName" });
            }
            if (string.IsNullOrEmpty(n)) return;

            jOriginalName = n;
            jNameCached = true;
        }

        private static string BuildDesiredNameJ()
        {
            if (!jNameCached || string.IsNullOrEmpty(jOriginalName)) return null;

            string name = jOriginalName;

            if (extraNameSuffix)
            {
                string suffix = extraNameSuffixText ?? string.Empty;
                if (suffix.Length > 32) suffix = suffix.Substring(0, 32);
                name = name + suffix;
            }

            if (extraRainbowName && J_RAINBOW.Length > 0)
            {
                int idx = ((jRainbowIndex % J_RAINBOW.Length) + J_RAINBOW.Length) % J_RAINBOW.Length;
                name = "<color=#" + J_RAINBOW[idx] + ">" + name + "</color>";
            }

            return name;
        }

        private static void WriteNameJ(PlayerControl lp, string name)
        {
            if (lp == null || lp.Data == null || name == null) return;

            WriteMemberJ(lp.Data, new string[] { "PlayerName" }, name);

            object outfit = ReadMemberJ(lp, new string[] { "CurrentOutfit" });
            if (outfit != null) WriteMemberJ(outfit, new string[] { "PlayerName" }, name);

            object def = ReadMemberJ(lp.Data, new string[] { "DefaultOutfit" });
            if (def != null) WriteMemberJ(def, new string[] { "PlayerName" }, name);

            object cosm = ReadMemberJ(lp, new string[] { "cosmetics", "Cosmetics" });
            TrySetCosmeticsNameJ(cosm, name);
        }

        private static void RestoreNameJ(PlayerControl lp)
        {
            if (jNameCached && !string.IsNullOrEmpty(jOriginalName))
                WriteNameJ(lp, jOriginalName);

            jNameCached = false;
            jOriginalName = null;
            jNameWasActive = false;
            jRainbowIndex = 0;
            jNextNameWriteAt = 0f;
            jNextRainbowAt = 0f;
        }

        // ---------------------------------------------------------------------
        // Feature 3: local player scale
        // ---------------------------------------------------------------------

        private static void TickScaleJ(PlayerControl lp)
        {
            if (lp == null) return;

            if (extraPlayerScale)
            {
                float s = Mathf.Clamp(extraPlayerScaleValue, 0.05f, 5f);
                try
                {
                    lp.transform.localScale = new Vector3(s, s, s);
                    jScaleWasActive = true;
                }
                catch { }
            }
            else if (jScaleWasActive)
            {
                try { lp.transform.localScale = Vector3.one; } catch { }
                jScaleWasActive = false;
            }
        }

        // ---------------------------------------------------------------------
        // Feature 4: cosmetic spin (hat / visor / skin)
        // ---------------------------------------------------------------------

        private static void TickCosmeticSpinJ(PlayerControl lp)
        {
            if (!extraCosmeticSpin)
            {
                if (jCosmeticsWasActive) RestoreCosmeticsJ();
                return;
            }

            jCosmeticsWasActive = true;
            CacheCosmeticsJ();

            // "periodically" - advance roughly once per second.
            if (Time.unscaledTime < jNextCosmeticAt) return;
            jNextCosmeticAt = Time.unscaledTime + 1.0f;

            jCosmeticIndex = (jCosmeticIndex + 1) % J_COSMETIC_IDS.Length;
            uint id = J_COSMETIC_IDS[jCosmeticIndex];
            SetCosmeticIdJ(J_HAT_FIELDS, id);
            SetCosmeticIdJ(J_VISOR_FIELDS, id);
            SetCosmeticIdJ(J_SKIN_FIELDS, id);
            RefreshCosmeticsJ();
        }

        private static void CacheCosmeticsJ()
        {
            if (jCosmeticsCached) return;

            jOriginalHat = GetCosmeticIdJ(J_HAT_FIELDS, out jHatFound);
            jOriginalVisor = GetCosmeticIdJ(J_VISOR_FIELDS, out jVisorFound);
            jOriginalSkin = GetCosmeticIdJ(J_SKIN_FIELDS, out jSkinFound);

            if (!jHatFound && !jVisorFound && !jSkinFound) return; // nothing reflected - silent no-op
            jCosmeticsCached = true;
        }

        private static void RestoreCosmeticsJ()
        {
            if (jHatFound) SetCosmeticIdJ(J_HAT_FIELDS, jOriginalHat);
            if (jVisorFound) SetCosmeticIdJ(J_VISOR_FIELDS, jOriginalVisor);
            if (jSkinFound) SetCosmeticIdJ(J_SKIN_FIELDS, jOriginalSkin);
            RefreshCosmeticsJ();

            jCosmeticsCached = false;
            jHatFound = false;
            jVisorFound = false;
            jSkinFound = false;
            jCosmeticsWasActive = false;
            jCosmeticIndex = 0;
            jNextCosmeticAt = 0f;
        }

        private static object GetCosmeticsObjectJ()
        {
            PlayerControl lp = PlayerControl.LocalPlayer;
            if (lp == null) return null;
            return ReadMemberJ(lp, new string[] { "cosmetics", "Cosmetics" });
        }

        private static uint GetCosmeticIdJ(string[] names, out bool found)
        {
            found = false;
            object cosm = GetCosmeticsObjectJ();
            if (cosm == null) return 0u;

            object value = ReadMemberJ(cosm, names);
            if (value == null)
            {
                object layer = ReadMemberJ(cosm, J_LAYER_MEMBERS);
                if (layer != null) value = ReadMemberJ(layer, names);
            }
            if (value == null) return 0u;

            found = true;
            return ToUInt32J(value);
        }

        private static bool SetCosmeticIdJ(string[] names, uint id)
        {
            object cosm = GetCosmeticsObjectJ();
            if (cosm == null) return false;

            if (WriteMemberJ(cosm, names, id)) return true;

            object layer = ReadMemberJ(cosm, J_LAYER_MEMBERS);
            return layer != null && WriteMemberJ(layer, names, id);
        }

        private static void RefreshCosmeticsJ()
        {
            object cosm = GetCosmeticsObjectJ();
            if (cosm == null) return;

            Type t = cosm.GetType();
            for (int i = 0; i < J_REFRESH_METHODS.Length; i++)
            {
                try
                {
                    MethodInfo mi = t.GetMethod(J_REFRESH_METHODS[i], J_FLAGS, null, Type.EmptyTypes, null);
                    if (mi != null)
                    {
                        mi.Invoke(cosm, null);
                        return;
                    }
                }
                catch { }
            }
        }

        // ---------------------------------------------------------------------
        // Feature 5: local color lock (NetworkedPlayerInfo.ColorId)
        // ---------------------------------------------------------------------

        private static void TickColorLockJ(PlayerControl lp)
        {
            if (!extraLocalColorLock)
            {
                if (jColorWasActive) RestoreColorJ(lp);
                return;
            }

            jColorWasActive = true;
            CacheColorJ(lp);

            // Throttle every color write to at most once per 0.25 seconds.
            if (Time.unscaledTime < jNextColorWriteAt) return;
            jNextColorWriteAt = Time.unscaledTime + 0.25f;

            int target = Mathf.Clamp(extraLocalColorLockId, 0, 17);
            bool found;
            int current = ReadColorIdJ(lp, out found);
            if (found && current == target) return;

            WriteColorIdJ(target);
        }

        private static void CacheColorJ(PlayerControl lp)
        {
            if (jColorCached) return;
            bool found;
            int id = ReadColorIdJ(lp, out found);
            if (!found) return;
            jOriginalColorId = id;
            jColorCached = true;
        }

        private static int ReadColorIdJ(PlayerControl lp, out bool found)
        {
            found = false;
            if (lp == null || lp.Data == null) return 0;

            object value = ReadMemberJ(lp.Data, new string[] { "ColorId" });
            if (value == null)
            {
                object outfit = ReadMemberJ(lp.Data, new string[] { "DefaultOutfit" });
                value = ReadMemberJ(outfit, new string[] { "ColorId" });
            }
            if (value == null) return 0;

            found = true;
            return ToInt32J(value);
        }

        private static bool WriteColorIdJ(int id)
        {
            PlayerControl lp = PlayerControl.LocalPlayer;
            if (lp == null || lp.Data == null) return false;

            if (WriteMemberJ(lp.Data, new string[] { "ColorId" }, id)) return true;

            object outfit = ReadMemberJ(lp.Data, new string[] { "DefaultOutfit" });
            return outfit != null && WriteMemberJ(outfit, new string[] { "ColorId" }, id);
        }

        private static void RestoreColorJ(PlayerControl lp)
        {
            if (jColorCached) WriteColorIdJ(jOriginalColorId);
            jColorCached = false;
            jColorWasActive = false;
            jNextColorWriteAt = 0f;
        }

        // ---------------------------------------------------------------------
        // Generic reflection helpers (all guarded / silent no-op)
        // ---------------------------------------------------------------------

        private static object ReadMemberJ(object target, string[] names)
        {
            if (target == null || names == null) return null;
            Type type = target.GetType();

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    PropertyInfo prop = type.GetProperty(name, J_FLAGS);
                    if (prop != null && prop.CanRead) return prop.GetValue(target, null);
                }
                catch { }
                try
                {
                    FieldInfo field = type.GetField(name, J_FLAGS);
                    if (field != null) return field.GetValue(target);
                }
                catch { }
            }
            return null;
        }

        private static string ReadStringJ(object target, string[] names)
        {
            return ReadMemberJ(target, names) as string;
        }

        private static bool WriteMemberJ(object target, string[] names, object value)
        {
            if (target == null || names == null) return false;
            Type type = target.GetType();

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    PropertyInfo prop = type.GetProperty(name, J_FLAGS);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(target, CoerceValueJ(value, prop.PropertyType), null);
                        return true;
                    }
                }
                catch { }
                try
                {
                    FieldInfo field = type.GetField(name, J_FLAGS);
                    if (field != null)
                    {
                        field.SetValue(target, CoerceValueJ(value, field.FieldType));
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        private static object CoerceValueJ(object value, Type targetType)
        {
            try
            {
                if (value == null || targetType == null) return value;
                if (targetType.IsInstanceOfType(value)) return value;
                if (targetType == typeof(string)) return value.ToString();
                return Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch { return value; }
        }

        private static void TrySetCosmeticsNameJ(object cosmetics, string name)
        {
            if (cosmetics == null || name == null) return;
            try
            {
                MethodInfo mi = cosmetics.GetType().GetMethod(
                    "SetName", J_FLAGS, null, new Type[] { typeof(string) }, null);
                if (mi != null) mi.Invoke(cosmetics, new object[] { name });
            }
            catch { }
        }

        private static uint ToUInt32J(object value)
        {
            try { return value == null ? 0u : Convert.ToUInt32(value); }
            catch { return 0u; }
        }

        private static int ToInt32J(object value)
        {
            try { return value == null ? 0 : Convert.ToInt32(value); }
            catch { return 0; }
        }
    }
}
