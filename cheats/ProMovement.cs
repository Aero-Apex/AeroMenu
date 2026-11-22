#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using InnerNet;
using AmongUs.GameOptions;
using BepInEx;
using HarmonyLib;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Group T (Movement) state. Names are fixed by the pro contract.
        // ------------------------------------------------------------------
        public static bool proTpByNumber = false;
        public static bool proSpeedHotkeys = false;
        public static bool proCameraFollow = false;
        public static bool proSmoothCamera = false;
        public static bool proZoomHotkeys = false;

        // ------------------------------------------------------------------
        // Private helper state (every private name ends with the group suffix T).
        // ------------------------------------------------------------------
        private const BindingFlags BindFlagsT = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private const float SpeedStepT = 0.5f;
        private const float SpeedMinT = 1f;
        private const float SpeedMaxT = 30f;

        private const float CamZoomMinT = 1f;
        private const float CamZoomMaxT = 20f;
        private const float CamZoomStepT = 0.5f;
        private const float CamSmoothSpeedT = 8f;

        private static readonly string[] SnapMethodNamesT = { "RpcSnapTo", "SnapTo" };

        // Original camera baseline (captured on enable, restored on disable).
        private static bool camBaselineCapturedT = false;
        private static Camera camBaselineCameraT = null;
        private static Vector3 camOriginalPosT = Vector3.zero;
        private static float camOriginalSizeT = 3f;

        // Follow-camera bookkeeping.
        private static bool camFollowDisabledByMeT = false;
        private static bool prevCameraFollowT = false;

        // Zoom bookkeeping.
        private static bool prevZoomHotkeysT = false;
        private static float camZoomTargetT = -1f;

        // Lazily cached overlay style.
        private static GUIStyle overlayStyleT = null;

        // ==================================================================
        // Required public surface
        // ==================================================================

        internal void DrawProT(float width)
        {
            try
            {
                float work = width > 0f ? Mathf.Clamp(width, 120f, 900f) : GetMenuWorkWidth(160f, 620f);
                int toggleW = Mathf.RoundToInt(work);

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    GUILayout.Label(L("MOVEMENT (PRO)", "ДВИЖЕНИЕ (PRO)"), headerStyle);
                    GUILayout.Space(4f);

                    proTpByNumber = DrawToggle(proTpByNumber, L("TP By Number (1-9)", "ТП по номеру (1-9)"), toggleW);
                    proSpeedHotkeys = DrawToggle(proSpeedHotkeys, L("Speed Hotkeys ([ / ])", "Скорость клавишами ([ / ])"), toggleW);
                    proCameraFollow = DrawToggle(proCameraFollow, L("Camera Follow Target", "Камера за целью"), toggleW);
                    proSmoothCamera = DrawToggle(proSmoothCamera, L("Smooth Camera", "Плавная камера"), toggleW);
                    proZoomHotkeys = DrawToggle(proZoomHotkeys, L("Zoom Hotkeys (+ / -)", "Зум клавишами (+ / -)"), toggleW);

                    GUILayout.Space(4f);
                    GUILayout.Label(MenuText(L("1-9: teleport to player N. [ ]: walk speed. +/-: camera zoom. Follow uses the selected player.", "1-9: ТП к игроку N. [ ]: скорость. +/-: зум камеры. Следование использует выбранного игрока.")), menuDescStyle);
                }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }

        internal static void TickProT(bool inputBlocked)
        {
            try
            {
                // Track feature enable/disable edges (baseline capture + restore).
                UpdateFeatureEdgesT();

                // Hotkeys only when gameplay input is not blocked.
                if (!inputBlocked)
                {
                    if (proTpByNumber) HandleTpHotkeysT();
                    if (proSpeedHotkeys) HandleSpeedHotkeysT();
                    if (proZoomHotkeys) HandleZoomHotkeysT();
                }

                // Continuous camera effects run regardless of inputBlocked.
                if (proCameraFollow) ApplyCameraFollowT();
                if (proZoomHotkeys) ApplyCameraZoomT();
            }
            catch { }
        }

        internal static void LoadProT()
        {
            try
            {
                proTpByNumber = LoadBool("M_ProT_proTpByNumber", false);
                proSpeedHotkeys = LoadBool("M_ProT_proSpeedHotkeys", false);
                proCameraFollow = LoadBool("M_ProT_proCameraFollow", false);
                proSmoothCamera = LoadBool("M_ProT_proSmoothCamera", false);
                proZoomHotkeys = LoadBool("M_ProT_proZoomHotkeys", false);
            }
            catch { }
        }

        internal static void SaveProT()
        {
            try
            {
                SaveBool("M_ProT_proTpByNumber", proTpByNumber);
                SaveBool("M_ProT_proSpeedHotkeys", proSpeedHotkeys);
                SaveBool("M_ProT_proCameraFollow", proCameraFollow);
                SaveBool("M_ProT_proSmoothCamera", proSmoothCamera);
                SaveBool("M_ProT_proZoomHotkeys", proZoomHotkeys);
            }
            catch { }
        }

        internal static void DrawProTOverlay()
        {
            try
            {
                if (Screen.width <= 1) return;
                if (!proTpByNumber && !proSpeedHotkeys && !proCameraFollow && !proSmoothCamera && !proZoomHotkeys) return;

                if (overlayStyleT == null)
                {
                    try
                    {
                        overlayStyleT = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            normal = { textColor = new Color(0.75f, 0.90f, 1f, 0.85f) }
                        };
                        overlayStyleT.normal.background = null;
                    }
                    catch { return; }
                }

                string text = "";
                if (proTpByNumber) text += "TP 1-9  ";
                if (proSpeedHotkeys) text += "SPEED [ ]  ";
                if (proCameraFollow) text += proSmoothCamera ? "SMOOTH FOLLOW  " : "FOLLOW  ";
                if (proZoomHotkeys) text += "ZOOM +/-";
                if (string.IsNullOrEmpty(text)) return;

                GUI.Label(new Rect(10f, Screen.height - 28f, Mathf.Min(520f, Screen.width - 20f), 22f), text, overlayStyleT);
            }
            catch { }
        }

        // ==================================================================
        // Private helpers (all suffixed with T)
        // ==================================================================

        private static void UpdateFeatureEdgesT()
        {
            try
            {
                // --- camera follow edge ---
                if (proCameraFollow && !prevCameraFollowT)
                {
                    Camera cam = Camera.main;
                    if (cam != null) CaptureCameraBaselineT(cam);
                    DisableFollowCameraT();
                }
                else if (!proCameraFollow && prevCameraFollowT)
                {
                    RestoreCameraPositionT();
                    RestoreFollowCameraT();
                }
                prevCameraFollowT = proCameraFollow;

                // --- zoom edge ---
                if (proZoomHotkeys && !prevZoomHotkeysT)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        CaptureCameraBaselineT(cam);
                        camZoomTargetT = cam.orthographicSize;
                    }
                }
                else if (!proZoomHotkeys && prevZoomHotkeysT)
                {
                    RestoreCameraZoomT();
                }
                prevZoomHotkeysT = proZoomHotkeys;

                // Baseline is no longer needed once both camera features are off.
                if (!proCameraFollow && !proZoomHotkeys)
                {
                    camBaselineCapturedT = false;
                    camBaselineCameraT = null;
                }
            }
            catch { }
        }

        private static void CaptureCameraBaselineT(Camera cam)
        {
            if (cam == null) return;
            if (camBaselineCapturedT && camBaselineCameraT == cam) return;
            try
            {
                if (cam.transform != null) camOriginalPosT = cam.transform.position;
                camOriginalSizeT = cam.orthographicSize;
                camBaselineCameraT = cam;
                camBaselineCapturedT = true;
            }
            catch { }
        }

        private static void RestoreCameraPositionT()
        {
            try
            {
                if (!camBaselineCapturedT) return;
                Camera cam = Camera.main;
                if (cam == null || camBaselineCameraT != cam || cam.transform == null) return;
                cam.transform.position = camOriginalPosT;
            }
            catch { }
        }

        private static void RestoreCameraZoomT()
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                if (camBaselineCapturedT && camBaselineCameraT == cam) cam.orthographicSize = camOriginalSizeT;
                camZoomTargetT = -1f;
            }
            catch { }
        }

        // Find the game's FollowerCamera by component name (no direct type dependency).
        private static object FindFollowCameraT()
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null || cam.gameObject == null) return null;
                Component[] comps = cam.gameObject.GetComponents<Component>();
                if (comps == null) return null;
                foreach (Component c in comps)
                {
                    if (c == null) continue;
                    if (c.GetType().Name == "FollowerCamera") return c;
                }
            }
            catch { }
            return null;
        }

        private static void DisableFollowCameraT()
        {
            try
            {
                if (camFollowDisabledByMeT) return;
                object follow = FindFollowCameraT();
                if (follow == null) return;
                if (SetMemberT(follow, "enabled", false))
                {
                    camFollowDisabledByMeT = true;
                    SetMemberT(follow, "Target", null);
                }
            }
            catch { }
        }

        private static void RestoreFollowCameraT()
        {
            try
            {
                if (!camFollowDisabledByMeT) return;
                object follow = FindFollowCameraT();
                if (follow != null)
                {
                    SetMemberT(follow, "enabled", true);
                    PlayerControl local = PlayerControl.LocalPlayer;
                    if (local != null)
                    {
                        try
                        {
                            MethodInfo m = follow.GetType().GetMethod("SetTarget", BindFlagsT);
                            if (m != null) m.Invoke(follow, new object[] { local });
                        }
                        catch { }
                    }
                }
                camFollowDisabledByMeT = false;
            }
            catch { }
        }

        private static void ApplyCameraFollowT()
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null || cam.transform == null) return;
                if (!camBaselineCapturedT || camBaselineCameraT != cam) CaptureCameraBaselineT(cam);

                PlayerControl target = GetTargetPlayerT();
                if (target == null || target.transform == null) return;

                Vector3 tp = target.transform.position;
                Vector3 desired = new Vector3(tp.x, tp.y, camOriginalPosT.z);

                if (proSmoothCamera)
                {
                    float t = 1f - Mathf.Exp(-CamSmoothSpeedT * Time.deltaTime);
                    cam.transform.position = Vector3.Lerp(cam.transform.position, desired, Mathf.Clamp01(t));
                }
                else
                {
                    cam.transform.position = desired;
                }
            }
            catch { }
        }

        private static void ApplyCameraZoomT()
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                if (!camBaselineCapturedT || camBaselineCameraT != cam) CaptureCameraBaselineT(cam);
                if (camZoomTargetT < 0f) camZoomTargetT = cam.orthographicSize;

                float target = Mathf.Clamp(camZoomTargetT, CamZoomMinT, CamZoomMaxT);
                if (proSmoothCamera)
                {
                    float t = 1f - Mathf.Exp(-CamSmoothSpeedT * Time.deltaTime);
                    cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, target, Mathf.Clamp01(t));
                }
                else
                {
                    cam.orthographicSize = target;
                }
            }
            catch { }
        }

        private static void HandleTpHotkeysT()
        {
            try
            {
                for (int i = 1; i <= 9; i++)
                {
                    KeyCode mainKey = (KeyCode)((int)KeyCode.Alpha0 + i);
                    KeyCode padKey = (KeyCode)((int)KeyCode.Keypad0 + i);

                    bool pressed = false;
                    try { pressed = Input.GetKeyDown(mainKey) || Input.GetKeyDown(padKey); }
                    catch { try { pressed = Input.GetKeyDown(mainKey); } catch { } }
                    if (!pressed) continue;

                    PlayerControl target = GetPlayerAtIndexT(i);
                    if (target == null || target.transform == null) return;
                    TrySnapLocalT(target.transform.position);
                    return;
                }
            }
            catch { }
        }

        private static void HandleSpeedHotkeysT()
        {
            try
            {
                bool down = false;
                bool up = false;
                try { down = Input.GetKeyDown(KeyCode.LeftBracket); } catch { }
                try { up = Input.GetKeyDown(KeyCode.RightBracket); } catch { }
                if (!down && !up) return;

                walkSpeed = Mathf.Clamp(walkSpeed + (up ? SpeedStepT : -SpeedStepT), SpeedMinT, SpeedMaxT);
            }
            catch { }
        }

        private static void HandleZoomHotkeysT()
        {
            try
            {
                bool zoomIn = false;
                bool zoomOut = false;
                try
                {
                    zoomIn = Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus);
                    zoomOut = Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);
                }
                catch { }
                if (!zoomIn && !zoomOut) return;

                if (camZoomTargetT < 0f)
                {
                    Camera cam = Camera.main;
                    camZoomTargetT = cam != null ? cam.orthographicSize : camOriginalSizeT;
                }

                // '+' / '=' zoom in => smaller orthographic size; '-' => larger.
                camZoomTargetT = Mathf.Clamp(camZoomTargetT + (zoomIn ? -CamZoomStepT : CamZoomStepT), CamZoomMinT, CamZoomMaxT);
            }
            catch { }
        }

        private static PlayerControl GetPlayerAtIndexT(int oneBased)
        {
            if (oneBased < 1 || oneBased > 9) return null;
            try
            {
                int idx = oneBased - 1;
                int seen = 0;

                if (lockedPlayersList != null && lockedPlayersList.Count > 0)
                {
                    foreach (PlayerControl p in lockedPlayersList)
                    {
                        if (p == null || p.Data == null || p.Data.Disconnected) continue;
                        if (seen == idx) return p;
                        seen++;
                    }
                }

                seen = 0;
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (p == null || p.Data == null || p.Data.Disconnected) continue;
                        if (seen == idx) return p;
                        seen++;
                    }
                }
            }
            catch { }
            return null;
        }

        private static PlayerControl GetTargetPlayerT()
        {
            try
            {
                byte selectedId = byte.MaxValue;
                try
                {
                    AeroMenuGUI gui = UnityEngine.Object.FindObjectOfType<AeroMenuGUI>();
                    if (gui != null)
                    {
                        FieldInfo f = typeof(AeroMenuGUI).GetField("selectedAntiCheatPlayerId", BindFlagsT);
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

                if (selectedId != byte.MaxValue && PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (p != null && p.PlayerId == selectedId) return p;
                    }
                }

                PlayerControl local = PlayerControl.LocalPlayer;
                if (PlayerControl.AllPlayerControls != null)
                {
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

        // Teleport the local player via the snap RPC, resolved through reflection.
        private static bool TrySnapLocalT(Vector2 pos)
        {
            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null) return false;

            // Preferred: the repository's own snap helper, invoked reflectively.
            try
            {
                MethodInfo send = typeof(AeroMenuGUI).GetMethod("SendPlayerSnapTo", BindFlagsT);
                if (send != null)
                {
                    object res = send.Invoke(null, new object[] { local, pos });
                    if (res is bool b && b) return true;
                }
            }
            catch { }

            // Fallback: reflect the snap methods on the local player's NetTransform.
            object net = null;
            try { net = GetMemberT(local, "NetTransform"); }
            catch { }
            if (net == null) return false;

            foreach (string name in SnapMethodNamesT)
            {
                try
                {
                    MethodInfo mi = net.GetType().GetMethod(name, BindFlagsT, null, new[] { typeof(Vector2) }, null);
                    if (mi == null)
                    {
                        foreach (MethodInfo cand in net.GetType().GetMethods(BindFlagsT))
                        {
                            if (cand.Name != name) continue;
                            ParameterInfo[] ps = cand.GetParameters();
                            if (ps.Length != 1) continue;
                            Type pt = ps[0].ParameterType;
                            if (pt == typeof(Vector2) || pt == typeof(Vector3) || pt == typeof(object))
                            {
                                mi = cand;
                                break;
                            }
                        }
                    }
                    if (mi == null) continue;

                    ParameterInfo[] pars = mi.GetParameters();
                    object arg = pars.Length > 0 && pars[0].ParameterType == typeof(Vector3)
                        ? (object)(Vector3)pos
                        : (object)pos;
                    mi.Invoke(net, new object[] { arg });
                    return true;
                }
                catch { }
            }
            return false;
        }

        private static object GetMemberT(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                Type t = obj.GetType();
                PropertyInfo p = t.GetProperty(name, BindFlagsT);
                if (p != null) return p.GetValue(obj, null);
                FieldInfo f = t.GetField(name, BindFlagsT);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            return null;
        }

        private static bool SetMemberT(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            try
            {
                Type t = obj.GetType();
                PropertyInfo p = t.GetProperty(name, BindFlagsT);
                if (p != null && p.CanWrite)
                {
                    p.SetValue(obj, value, null);
                    return true;
                }
                FieldInfo f = t.GetField(name, BindFlagsT);
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
