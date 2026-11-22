#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InnerNet;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Group E state. Field names are fixed by the extras contract.
        // ------------------------------------------------------------------
        public static KeyCode extraTpToCursorKey = KeyCode.T;
        public static KeyCode extraPushPullKey = KeyCode.G;
        public static float extraPushPullStrength = 3f;
        public static bool extraTargetSpeedOverride = false;
        public static float extraTargetSpeedValue = 2.5f;
        public static bool extraAutoRevive = false;
        public static bool extraSpinPlayer = false;
        public static float extraSpinSpeed = 360f;

        // ------------------------------------------------------------------
        // Private helpers state (every name ends with the group suffix E).
        // ------------------------------------------------------------------
        private const BindingFlags BindFlagsE = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static int waitingBindE = 0;                 // 0 = none, 1 = tp key, 2 = push/pull key
        private static float speedApplyTimerE = 0f;          // throttles the speed override

        // Cached reflection lookup for the speed member on PlayerControl / MyPhysics.
        private static readonly Dictionary<Type, MemberInfo> speedMemberCacheE = new Dictionary<Type, MemberInfo>();
        private static readonly string[] speedMemberNamesE = { "Speed", "MoveSpeed", "speed", "WalkSpeed", "moveSpeed", "CurrentSpeed" };

        // ==================================================================
        // Required public surface
        // ==================================================================

        internal void DrawExtrasE(float width)
        {
            try
            {
                HandleBindEventsE();

                float labelWidth = Mathf.Max(150f, width > 0f ? width * 0.42f : 150f);

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    GUILayout.Label(L("PLAYER TOOLS", "ИНСТРУМЕНТЫ ИГРОКА"), headerStyle);
                    GUILayout.Space(4f);

                    // --- 21: teleport to cursor hotkey ---
                    DrawKeyRowE(L("TP To Cursor Key", "Клавиша ТП к курсору"), ref extraTpToCursorKey, 1, labelWidth);

                    // --- 22: push / pull hotkey + strength ---
                    DrawKeyRowE(L("Push / Pull Key", "Клавиша толкать/тянуть"), ref extraPushPullKey, 2, labelWidth);

                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label($"{L("Push/Pull Strength", "Сила толчка")}: {extraPushPullStrength:0.0}", toggleLabelStyle, GUILayout.Width(labelWidth));
                        extraPushPullStrength = ThemedSlider(extraPushPullStrength, 0.1f, 50f, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("R", btnStyle, GUILayout.Width(24f))) extraPushPullStrength = 3f;
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Space(3f);

                    // --- 23: selected target speed override ---
                    extraTargetSpeedOverride = DrawToggle(extraTargetSpeedOverride, L("Target Speed Override", "Скорость цели"), Mathf.RoundToInt(labelWidth));

                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label($"{L("Target Speed", "Скорость цели")}: {extraTargetSpeedValue:0.00}", toggleLabelStyle, GUILayout.Width(labelWidth));
                        extraTargetSpeedValue = ThemedSlider(extraTargetSpeedValue, 0.01f, 20f, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("R", btnStyle, GUILayout.Width(24f))) extraTargetSpeedValue = 2.5f;
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Space(3f);

                    // --- 24: host-only auto revive ---
                    extraAutoRevive = DrawToggle(extraAutoRevive, L("Auto Revive (Host)", "Автовоскрешение (хост)"), Mathf.RoundToInt(labelWidth));

                    // --- 25: spin local player ---
                    extraSpinPlayer = DrawToggle(extraSpinPlayer, L("Spin Player", "Вращение игрока"), Mathf.RoundToInt(labelWidth));

                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label($"{L("Spin Speed", "Скорость вращения")}: {extraSpinSpeed:0}", toggleLabelStyle, GUILayout.Width(labelWidth));
                        extraSpinSpeed = ThemedSlider(extraSpinSpeed, -1440f, 1440f, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("R", btnStyle, GUILayout.Width(24f))) extraSpinSpeed = 360f;
                    }
                    finally { GUILayout.EndHorizontal(); }

                    GUILayout.Space(4f);
                    GUILayout.Label(MenuText(L("Push = hold Left Shift, Pull = no Shift.", "Толчок = зажать Левый Shift, притяжение = без Shift.")), menuDescStyle);
                }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }

        internal static void TickExtrasE(bool inputBlocked)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;

                // --- hotkeys (21 & 22) respect inputBlocked and key-bind wait ---
                if (!inputBlocked && waitingBindE == 0 && local != null && local.transform != null)
                {
                    if (extraTpToCursorKey != KeyCode.None && Input.GetKeyDown(extraTpToCursorKey))
                        TeleportToCursorE(local);

                    if (extraPushPullKey != KeyCode.None && Input.GetKeyDown(extraPushPullKey))
                        ApplyPushPullE(local);
                }

                // --- 23: throttled target speed override ---
                if (extraTargetSpeedOverride)
                {
                    speedApplyTimerE += Time.deltaTime;
                    if (speedApplyTimerE >= 0.1f)
                    {
                        speedApplyTimerE = 0f;
                        ApplyTargetSpeedOverrideE();
                    }
                }
                else
                {
                    speedApplyTimerE = 0f;
                }

                // --- 24: host-only auto revive ---
                if (extraAutoRevive)
                    AutoReviveLocalE();

                // --- 25: visual spin ---
                if (extraSpinPlayer && local != null && local.transform != null)
                {
                    try { local.transform.Rotate(0f, extraSpinSpeed * Time.deltaTime, 0f); }
                    catch { }
                }
            }
            catch { }
        }

        internal static void LoadExtrasE()
        {
            try
            {
                if (PlayerPrefs.HasKey("M_ExtrasE_extraTpToCursorKey"))
                    extraTpToCursorKey = (KeyCode)PlayerPrefs.GetInt("M_ExtrasE_extraTpToCursorKey", (int)extraTpToCursorKey);
                if (PlayerPrefs.HasKey("M_ExtrasE_extraPushPullKey"))
                    extraPushPullKey = (KeyCode)PlayerPrefs.GetInt("M_ExtrasE_extraPushPullKey", (int)extraPushPullKey);
                if (PlayerPrefs.HasKey("M_ExtrasE_extraPushPullStrength"))
                    extraPushPullStrength = Mathf.Clamp(PlayerPrefs.GetFloat("M_ExtrasE_extraPushPullStrength", extraPushPullStrength), 0.1f, 50f);
                extraTargetSpeedOverride = PlayerPrefs.HasKey("M_ExtrasE_extraTargetSpeedOverride") && PlayerPrefs.GetInt("M_ExtrasE_extraTargetSpeedOverride", extraTargetSpeedOverride ? 1 : 0) == 1;
                if (PlayerPrefs.HasKey("M_ExtrasE_extraTargetSpeedValue"))
                    extraTargetSpeedValue = Mathf.Clamp(PlayerPrefs.GetFloat("M_ExtrasE_extraTargetSpeedValue", extraTargetSpeedValue), 0.01f, 20f);
                extraAutoRevive = PlayerPrefs.HasKey("M_ExtrasE_extraAutoRevive") && PlayerPrefs.GetInt("M_ExtrasE_extraAutoRevive", extraAutoRevive ? 1 : 0) == 1;
                extraSpinPlayer = PlayerPrefs.HasKey("M_ExtrasE_extraSpinPlayer") && PlayerPrefs.GetInt("M_ExtrasE_extraSpinPlayer", extraSpinPlayer ? 1 : 0) == 1;
                if (PlayerPrefs.HasKey("M_ExtrasE_extraSpinSpeed"))
                    extraSpinSpeed = Mathf.Clamp(PlayerPrefs.GetFloat("M_ExtrasE_extraSpinSpeed", extraSpinSpeed), -1440f, 1440f);
            }
            catch { }
        }

        internal static void SaveExtrasE()
        {
            try
            {
                PlayerPrefs.SetInt("M_ExtrasE_extraTpToCursorKey", (int)extraTpToCursorKey);
                PlayerPrefs.SetInt("M_ExtrasE_extraPushPullKey", (int)extraPushPullKey);
                PlayerPrefs.SetFloat("M_ExtrasE_extraPushPullStrength", extraPushPullStrength);
                PlayerPrefs.SetInt("M_ExtrasE_extraTargetSpeedOverride", extraTargetSpeedOverride ? 1 : 0);
                PlayerPrefs.SetFloat("M_ExtrasE_extraTargetSpeedValue", extraTargetSpeedValue);
                PlayerPrefs.SetInt("M_ExtrasE_extraAutoRevive", extraAutoRevive ? 1 : 0);
                PlayerPrefs.SetInt("M_ExtrasE_extraSpinPlayer", extraSpinPlayer ? 1 : 0);
                PlayerPrefs.SetFloat("M_ExtrasE_extraSpinSpeed", extraSpinSpeed);
            }
            catch { }
        }

        internal static void DrawExtrasEOverlay()
        {
            // No overlay for group E.
        }

        // ==================================================================
        // Private helpers (all suffixed with E)
        // ==================================================================

        private static void HandleBindEventsE()
        {
            try
            {
                if (waitingBindE == 0) return;

                Event e = Event.current;
                if (e == null || e.type != EventType.KeyDown) return;

                if (e.keyCode == KeyCode.Escape)
                {
                    waitingBindE = 0;
                    e.Use();
                    return;
                }
                if (e.keyCode == KeyCode.None) return;

                if (waitingBindE == 1) extraTpToCursorKey = e.keyCode;
                else if (waitingBindE == 2) extraPushPullKey = e.keyCode;

                waitingBindE = 0;
                e.Use();
            }
            catch { }
        }

        private void DrawKeyRowE(string label, ref KeyCode key, int id, float labelWidth)
        {
            GUILayout.BeginHorizontal();
            try
            {
                GUILayout.Label(label, toggleLabelStyle, GUILayout.Width(labelWidth));

                string bindText = waitingBindE == id
                    ? MenuText("Press any key...")
                    : (key == KeyCode.None ? "NONE" : key.ToString());

                if (GUILayout.Button(bindText, waitingBindE == id ? activeTabStyle : btnStyle, GUILayout.Width(120f)))
                    waitingBindE = waitingBindE == id ? 0 : id;

                if (GUILayout.Button(MenuText("Clear"), btnStyle, GUILayout.Width(50f)))
                {
                    key = KeyCode.None;
                    if (waitingBindE == id) waitingBindE = 0;
                }

                GUILayout.FlexibleSpace();
            }
            finally { GUILayout.EndHorizontal(); }
            GUILayout.Space(3f);
        }

        // Feature 21.
        private static void TeleportToCursorE(PlayerControl local)
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null || local == null || local.NetTransform == null) return;

                Vector3 screen = Input.mousePosition;
                screen.z = 10f;
                Vector3 world = cam.ScreenToWorldPoint(screen);

                Vector2 dest = new Vector2(world.x, world.y);
                TrySnapPlayerE(local, dest);
            }
            catch { }
        }

        // Feature 22.
        private static void ApplyPushPullE(PlayerControl local)
        {
            try
            {
                if (local == null || local.Data == null || local.transform == null) return;

                Vector2 localPos = local.transform.position;
                PlayerControl nearest = null;
                float bestDistance = float.MaxValue;

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == local || pc.Data == null || pc.transform == null) continue;
                    if (pc.Data.IsDead || pc.Data.Disconnected) continue;

                    float d = Vector2.Distance(localPos, pc.transform.position);
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        nearest = pc;
                    }
                }

                if (nearest == null) return;

                Vector2 targetPos = nearest.transform.position;
                Vector2 dir = targetPos - localPos;
                if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 0f);
                dir.Normalize();

                bool push = Input.GetKey(KeyCode.LeftShift);
                Vector2 dest = push
                    ? targetPos + dir * extraPushPullStrength
                    : targetPos - dir * extraPushPullStrength;

                TrySnapPlayerE(nearest, dest);
            }
            catch { }
        }

        // Snap helper shared by features 21 and 22. Reuses the repository's
        // established send-snap RPC helper, with reflection + direct RPC fallbacks.
        private static bool TrySnapPlayerE(PlayerControl player, Vector2 pos)
        {
            if (player == null || player.NetTransform == null) return false;

            // Preferred: existing static helper in network/PlayerRpcActions.cs.
            try
            {
                if (SendPlayerSnapTo(player, pos)) return true;
            }
            catch { }

            // Reflection fallback in case the helper's name/signature changed.
            try
            {
                MethodInfo mi = typeof(AeroMenuGUI).GetMethod("SendPlayerSnapTo", BindFlagsE);
                if (mi != null)
                {
                    object result = mi.Invoke(null, new object[] { player, pos });
                    if (result is bool b && b) return true;
                }
            }
            catch { }

            // Last resort: local network transform RPC / direct snap.
            try
            {
                if (UseSnapToRPC) player.NetTransform.RpcSnapTo(pos);
                else player.NetTransform.SnapTo(pos);
                return true;
            }
            catch { }
            try { player.NetTransform.SnapTo(pos); return true; }
            catch { }

            return false;
        }

        // Feature 23.
        private static void ApplyTargetSpeedOverrideE()
        {
            try
            {
                PlayerControl target = GetSelectedTargetE();
                if (target == null) return;

                // MyPhysics.Speed is the real movement-speed member in Among Us;
                // also probe PlayerControl for good measure.
                object physics = GetMemberE(target, "MyPhysics");
                if (physics != null) SetSpeedE(physics, extraTargetSpeedValue);
                SetSpeedE(target, extraTargetSpeedValue);
            }
            catch { }
        }

        private static PlayerControl GetSelectedTargetE()
        {
            try
            {
                byte selectedId = byte.MaxValue;
                try
                {
                    AeroMenuGUI gui = UnityEngine.Object.FindObjectOfType<AeroMenuGUI>();
                    if (gui != null)
                    {
                        FieldInfo f = typeof(AeroMenuGUI).GetField("selectedAntiCheatPlayerId", BindFlagsE);
                        if (f != null)
                        {
                            object v = f.GetValue(gui);
                            if (v is byte b) selectedId = b;
                            else if (v is int i) selectedId = (byte)i;
                        }
                    }
                }
                catch { }

                if (selectedId != byte.MaxValue && lockedPlayersList != null)
                {
                    foreach (PlayerControl p in lockedPlayersList)
                    {
                        if (p != null && p.PlayerId == selectedId) return p;
                    }
                }

                if (PlayerControl.AllPlayerControls != null)
                {
                    if (selectedId != byte.MaxValue)
                    {
                        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                        {
                            if (p != null && p.PlayerId == selectedId) return p;
                        }
                    }

                    // Fallback: first alive, connected, non-local player.
                    PlayerControl local = PlayerControl.LocalPlayer;
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (p == null || p == local || p.Data == null || p.Data.Disconnected || p.Data.IsDead) continue;
                        return p;
                    }
                }
            }
            catch { }
            return null;
        }

        private static MemberInfo FindSpeedMemberE(Type t)
        {
            if (t == null) return null;

            foreach (string name in speedMemberNamesE)
            {
                try
                {
                    FieldInfo f = t.GetField(name, BindFlagsE);
                    if (f != null && IsSpeedTypeE(f.FieldType)) return f;

                    PropertyInfo p = t.GetProperty(name, BindFlagsE);
                    if (p != null && p.CanWrite && IsSpeedTypeE(p.PropertyType)) return p;
                }
                catch { }
            }
            return null;
        }

        private static bool IsSpeedTypeE(Type t)
        {
            return t == typeof(float) || t == typeof(double) || t == typeof(int) || t == typeof(long);
        }

        private static object CoerceSpeedValueE(Type t, float value)
        {
            try
            {
                if (t == typeof(float)) return value;
                if (t == typeof(double)) return (double)value;
                if (t == typeof(int)) return Mathf.RoundToInt(value);
                if (t == typeof(long)) return (long)value;
            }
            catch { }
            return null;
        }

        private static void SetSpeedE(object obj, float value)
        {
            if (obj == null) return;
            try
            {
                Type t = obj.GetType();
                MemberInfo mi;
                lock (speedMemberCacheE)
                {
                    if (!speedMemberCacheE.TryGetValue(t, out mi))
                    {
                        mi = FindSpeedMemberE(t);
                        speedMemberCacheE[t] = mi;
                    }
                }
                if (mi == null) return;

                if (mi is FieldInfo fi)
                {
                    object coerced = CoerceSpeedValueE(fi.FieldType, value);
                    if (coerced != null) fi.SetValue(obj, coerced);
                }
                else if (mi is PropertyInfo pi && pi.CanWrite)
                {
                    object coerced = CoerceSpeedValueE(pi.PropertyType, value);
                    if (coerced != null) pi.SetValue(obj, coerced, null);
                }
            }
            catch { }
        }

        // Feature 24 (host only). Mirrors cheats/RevivePlayer.cs using reflection only.
        private static void AutoReviveLocalE()
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null) return;

                object data = GetMemberE(local, "Data");
                if (data == null) return;

                object deadValue = GetMemberE(data, "IsDead");
                if (!(deadValue is bool isDead) || !isDead) return;

                // Reset the dead flag.
                SetMemberE(data, "IsDead", false);

                // Re-enable the collider.
                object collider = GetMemberE(local, "Collider");
                if (collider != null) SetMemberE(collider, "enabled", true);

                // Restore the gameplay layer.
                object myPhysics = GetMemberE(local, "MyPhysics");
                if (myPhysics != null)
                {
                    object go = GetMemberE(myPhysics, "gameObject");
                    int playerLayer = LayerMask.NameToLayer("Players");
                    if (go != null && playerLayer >= 0) SetMemberE(go, "layer", playerLayer);
                }
            }
            catch { }
        }

        private static object GetMemberE(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                Type t = obj.GetType();
                PropertyInfo p = t.GetProperty(name, BindFlagsE);
                if (p != null) return p.GetValue(obj, null);
                FieldInfo f = t.GetField(name, BindFlagsE);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            return null;
        }

        private static bool SetMemberE(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            try
            {
                Type t = obj.GetType();
                PropertyInfo p = t.GetProperty(name, BindFlagsE);
                if (p != null && p.CanWrite)
                {
                    p.SetValue(obj, value, null);
                    return true;
                }
                FieldInfo f = t.GetField(name, BindFlagsE);
                if (f != null)
                {
                    f.SetValue(obj, value);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
