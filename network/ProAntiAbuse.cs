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
        // ------------------------------------------------------------------
        // Group S - Anti Abuse / Network (state, exact public names)
        // ------------------------------------------------------------------
        public static bool proSpoofPing = false;
        public static int proSpoofPingValue = 50;
        public static bool proHostMigrationAlert = true;
        public static bool proNetIdSanity = false;
        public static bool proSessionLogger = false;
        public static bool proIdentityLogger = false;

        // Private bookkeeping (suffix S to avoid collisions across partials).
        private static bool proSHostCheckedOnce = false;
        private static bool proSPrevHost = false;
        private static readonly HashSet<int> proSFlaggedNetIds = new HashSet<int>();
        private static float proSLastNetIdWarn = -999f;
        private static string proSLastSessionId = "";
        private static readonly HashSet<string> proSLoggedIdentities = new HashSet<string>();

        private static readonly System.Text.StringBuilder proSSessionBuffer = new System.Text.StringBuilder();
        private static readonly System.Text.StringBuilder proSIdentityBuffer = new System.Text.StringBuilder();
        private static float proSLastLogFlush = -999f;

        private static Type proSAmongUsClientType = null;
        private static Type proSPlayerControlType = null;

        // Overlay cache (static: DrawProSOverlay is static). Never allocated per frame.
        private static GUIStyle proSOverlayStyle = null;
        private static Texture2D proSOverlayBg = null;
        private static string proSOverlayPingText = "";
        private static int proSOverlayPingCached = -1;

        // ------------------------------------------------------------------
        // Persistence
        // ------------------------------------------------------------------
        private static bool LoadBoolS(string key, bool def)
        {
            try { return LoadBool(key, def); }
            catch { return def; }
        }

        private static void SaveBoolS(string key, bool value)
        {
            try { SaveBool(key, value); }
            catch { }
        }

        private static int LoadIntS(string key, int def)
        {
            try { return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : def; }
            catch { return def; }
        }

        private static void SaveIntS(string key, int value)
        {
            try { PlayerPrefs.SetInt(key, value); }
            catch { }
        }

        internal static void LoadProS()
        {
            proSpoofPing = LoadBoolS("M_ProS_proSpoofPing", false);
            proSpoofPingValue = Mathf.Clamp(LoadIntS("M_ProS_proSpoofPingValue", 50), 1, 999);
            proHostMigrationAlert = LoadBoolS("M_ProS_proHostMigrationAlert", true);
            proNetIdSanity = LoadBoolS("M_ProS_proNetIdSanity", false);
            proSessionLogger = LoadBoolS("M_ProS_proSessionLogger", false);
            proIdentityLogger = LoadBoolS("M_ProS_proIdentityLogger", false);
        }

        internal static void SaveProS()
        {
            SaveBoolS("M_ProS_proSpoofPing", proSpoofPing);
            SaveIntS("M_ProS_proSpoofPingValue", proSpoofPingValue);
            SaveBoolS("M_ProS_proHostMigrationAlert", proHostMigrationAlert);
            SaveBoolS("M_ProS_proNetIdSanity", proNetIdSanity);
            SaveBoolS("M_ProS_proSessionLogger", proSessionLogger);
            SaveBoolS("M_ProS_proIdentityLogger", proIdentityLogger);
            ProSFlushLogsS(true);
        }

        // ------------------------------------------------------------------
        // Tick
        // ------------------------------------------------------------------
        internal static void TickProS(bool inputBlocked)
        {
            try
            {
                bool host = ProSIsHostS();

                if (proHostMigrationAlert) ProSCheckHostMigrationS(host);
                if (proNetIdSanity) ProSCheckNetIdsS(host);
                if (proSessionLogger) ProSCheckSessionS();
                if (proIdentityLogger && host) ProSCheckIdentitiesS();
            }
            catch { }
            finally
            {
                ProSFlushLogsS(false);
            }
        }

        private static void ProSCheckHostMigrationS(bool host)
        {
            try
            {
                if (!proSHostCheckedOnce)
                {
                    proSHostCheckedOnce = true;
                    proSPrevHost = host;
                    return;
                }

                if (host && !proSPrevHost)
                {
                    try { ShowNotification(L("[AeroMenu] You are now the host (host migration detected)", "[AeroMenu] Вы теперь хост (обнаружена миграция хоста)")); }
                    catch { }
                }
                proSPrevHost = host;
            }
            catch { }
        }

        private static void ProSCheckNetIdsS(bool host)
        {
            try
            {
                Type pcType = ProSPlayerControlTypeS();
                if (pcType == null) return;

                object all = ProSGetStaticValueAnyS(pcType, "AllPlayerControls", "allPlayerControls");
                System.Collections.IEnumerable en = all as System.Collections.IEnumerable;
                if (en == null) return;

                HashSet<int> seen = new HashSet<int>();
                int localClient = ProSGetLocalClientIdS();

                foreach (object pc in en)
                {
                    if (pc == null) continue;
                    try
                    {
                        object netObj = ProSGetValueAnyS(pc, "NetId", "netId");
                        int netId;
                        try { netId = netObj == null ? 0 : Convert.ToInt32(netObj); }
                        catch { continue; }

                        bool duplicate = !seen.Add(netId);
                        if (netId >= 0 && netId <= 1000000 && !duplicate) continue;

                        if (proSFlaggedNetIds.Count > 512) proSFlaggedNetIds.Clear();
                        if (!proSFlaggedNetIds.Add(netId)) continue;

                        string reason = netId < 0 ? "negative" : (duplicate ? "duplicate" : "out-of-range");
                        ProSWarnS(L("[AeroMenu] Abnormal NetId " + netId + " (" + reason + ") from a client", "[AeroMenu] Подозрительный NetId " + netId + " (" + reason + ") у клиента"));

                        // A negative NetId is never valid; kick the owner if we are host.
                        if (host && netId < 0) ProSKickOwnerS(pc, localClient);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void ProSWarnS(string msg)
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                if (now - proSLastNetIdWarn < 1f) return;
                proSLastNetIdWarn = now;
                ShowNotification(msg);
            }
            catch { }
        }

        private static void ProSKickOwnerS(object pc, int localClient)
        {
            try
            {
                object ownerObj = ProSGetValueAnyS(pc, "OwnerId", "ownerId");
                int owner = ownerObj == null ? -1 : Convert.ToInt32(ownerObj);
                if (owner < 0 || owner == localClient) return;

                object client = ProSGetAmongUsClientS();
                if (client == null) return;

                object result;
                if (ProSTryInvokeS(client, "KickClient", new object[] { owner }, out result)) return;
                ProSTryInvokeS(client, "KickClient", new object[] { owner, "Abnormal NetId", false }, out result);
            }
            catch { }
        }

        private static void ProSCheckSessionS()
        {
            try
            {
                object client = ProSGetAmongUsClientS();
                if (client == null) return;

                string gid = ProSStringOf(ProSGetValueAnyS(client, "GameId", "gameId"));
                string code = ProSStringOf(ProSGetValueAnyS(client, "GameCode", "gameCode"));
                string mode = ProSStringOf(ProSGetValueAnyS(client, "NetworkMode", "networkMode"));
                if (string.IsNullOrEmpty(gid) && string.IsNullOrEmpty(code)) return;

                string key = gid + "|" + code + "|" + mode;
                if (key == proSLastSessionId) return;
                proSLastSessionId = key;

                string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] Session GameId=" +
                              (string.IsNullOrEmpty(gid) ? "-" : gid) + " Code=" +
                              (string.IsNullOrEmpty(code) ? "-" : code) + " Mode=" +
                              (string.IsNullOrEmpty(mode) ? "-" : mode);
                lock (proSSessionBuffer) { proSSessionBuffer.AppendLine(line); }
            }
            catch { }
        }

        private static void ProSCheckIdentitiesS()
        {
            try
            {
                Type pcType = ProSPlayerControlTypeS();
                if (pcType == null) return;

                object all = ProSGetStaticValueAnyS(pcType, "AllPlayerControls", "allPlayerControls");
                System.Collections.IEnumerable en = all as System.Collections.IEnumerable;
                if (en == null) return;

                foreach (object pc in en)
                {
                    if (pc == null) continue;
                    try
                    {
                        object data = ProSGetValueAnyS(pc, "Data", "data");
                        string name = ProSStringOf(ProSGetValueAnyS(data, "PlayerName", "playerName"));
                        if (string.IsNullOrEmpty(name)) name = ProSStringOf(ProSGetValueAnyS(pc, "PlayerName", "playerName"));

                        string puid = ProSStringOf(ProSGetValueAnyS(data, "Puid", "PUID", "puid"));
                        string fc = ProSStringOf(ProSGetValueAnyS(data, "FriendCode", "friendCode"));
                        if (string.IsNullOrEmpty(fc)) fc = ProSFriendCodeS(pc);

                        object ownerObj = ProSGetValueAnyS(pc, "OwnerId", "ownerId");
                        string owner = ownerObj == null ? "-" : Convert.ToString(ownerObj);

                        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(puid) && string.IsNullOrEmpty(fc)) continue;

                        string key = owner + "|" + name + "|" + puid + "|" + fc;
                        lock (proSLoggedIdentities)
                        {
                            if (proSLoggedIdentities.Count > 2048) proSLoggedIdentities.Clear();
                            if (!proSLoggedIdentities.Add(key)) continue;
                        }

                        string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] Client=" + owner +
                                      " Name=" + (string.IsNullOrEmpty(name) ? "-" : name) +
                                      " PUID=" + (string.IsNullOrEmpty(puid) ? "-" : puid) +
                                      " FC=" + (string.IsNullOrEmpty(fc) ? "-" : fc);
                        lock (proSIdentityBuffer) { proSIdentityBuffer.AppendLine(line); }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static string ProSFriendCodeS(object pc)
        {
            try
            {
                object ownerObj = ProSGetValueAnyS(pc, "OwnerId", "ownerId");
                int owner = ownerObj == null ? -1 : Convert.ToInt32(ownerObj);
                if (owner < 0) return "";

                object client = ProSGetAmongUsClientS();
                if (client == null) return "";

                object result;
                object data = null;
                if (!ProSTryInvokeS(client, "GetClientData", new object[] { owner }, out result))
                    ProSTryInvokeS(client, "GetClient", new object[] { owner }, out result);
                data = result;
                if (data == null) return "";

                return ProSStringOf(ProSGetValueAnyS(data, "FriendCode", "friendCode"));
            }
            catch { return ""; }
        }

        // ------------------------------------------------------------------
        // Throttled file logging (AppendAllText, at most once per second)
        // ------------------------------------------------------------------
        private static void ProSFlushLogsS(bool force)
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                if (!force && now - proSLastLogFlush < 1f) return;
                proSLastLogFlush = now;

                ProSFlushOneS(proSSessionBuffer, "ProSessions.txt");
                ProSFlushOneS(proSIdentityBuffer, "ProIdentities.txt");
            }
            catch { }
        }

        private static void ProSFlushOneS(System.Text.StringBuilder buffer, string fileName)
        {
            string content;
            lock (buffer)
            {
                if (buffer.Length == 0) return;
                content = buffer.ToString();
                buffer.Clear();
            }
            try
            {
                if (string.IsNullOrEmpty(Plugin.AeroFolder)) return;
                string path = Path.Combine(Plugin.AeroFolder, fileName);
                File.AppendAllText(path, content);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Reflection utilities (all null-checked and try/catch guarded)
        // ------------------------------------------------------------------
        private static Type ProSFindTypeS(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                Type t = Type.GetType(name, false);
                if (t != null) return t;
            }
            catch { }

            try
            {
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < asms.Length; i++)
                {
                    Assembly asm = asms[i];
                    if (asm == null) continue;
                    try
                    {
                        Type t = asm.GetType(name, false);
                        if (t != null) return t;

                        Type[] types = asm.GetTypes();
                        for (int j = 0; j < types.Length; j++)
                        {
                            if (types[j] != null && types[j].Name == name) return types[j];
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static Type ProSAmongUsClientTypeS()
        {
            try
            {
                if (proSAmongUsClientType == null)
                    proSAmongUsClientType = ProSFindTypeS("AmongUsClient") ?? ProSFindTypeS("InnerNet.AmongUsClient");
            }
            catch { }
            return proSAmongUsClientType;
        }

        private static Type ProSPlayerControlTypeS()
        {
            try
            {
                if (proSPlayerControlType == null)
                    proSPlayerControlType = ProSFindTypeS("PlayerControl") ?? ProSFindTypeS("InnerNet.PlayerControl");
            }
            catch { }
            return proSPlayerControlType;
        }

        private static object ProSGetAmongUsClientS()
        {
            try
            {
                Type t = ProSAmongUsClientTypeS();
                return t == null ? null : ProSGetStaticValueAnyS(t, "Instance", "instance");
            }
            catch { return null; }
        }

        private static bool ProSIsHostS()
        {
            try
            {
                object client = ProSGetAmongUsClientS();
                if (client == null) return false;
                object v = ProSGetValueAnyS(client, "AmHost", "amHost");
                return v is bool b && b;
            }
            catch { return false; }
        }

        private static int ProSGetLocalClientIdS()
        {
            try
            {
                object client = ProSGetAmongUsClientS();
                if (client == null) return -1;
                object v = ProSGetValueAnyS(client, "ClientId", "clientId");
                return v == null ? -1 : Convert.ToInt32(v);
            }
            catch { return -1; }
        }

        private static object ProSGetStaticValueS(Type t, string member)
        {
            if (t == null || string.IsNullOrEmpty(member)) return null;
            Type cur = t;
            while (cur != null)
            {
                try
                {
                    PropertyInfo p = cur.GetProperty(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (p != null) return p.GetValue(null, null);
                    FieldInfo f = cur.GetField(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null) return f.GetValue(null);
                }
                catch { }
                cur = cur.BaseType;
            }
            return null;
        }

        private static object ProSGetStaticValueAnyS(Type t, params string[] names)
        {
            if (names == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                object v = ProSGetStaticValueS(t, names[i]);
                if (v != null) return v;
            }
            return null;
        }

        private static object ProSGetValueS(object obj, string member)
        {
            if (obj == null || string.IsNullOrEmpty(member)) return null;
            Type cur = obj.GetType();
            while (cur != null)
            {
                try
                {
                    PropertyInfo p = cur.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (p != null) return p.GetValue(obj, null);
                    FieldInfo f = cur.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null) return f.GetValue(obj);
                }
                catch { }
                cur = cur.BaseType;
            }
            return null;
        }

        private static object ProSGetValueAnyS(object obj, params string[] names)
        {
            if (names == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                object v = ProSGetValueS(obj, names[i]);
                if (v != null) return v;
            }
            return null;
        }

        private static string ProSStringOf(object o)
        {
            if (o == null) return "";
            try { return Convert.ToString(o) ?? ""; }
            catch { return ""; }
        }

        private static bool ProSTryInvokeS(object target, string name, object[] args, out object result)
        {
            result = null;
            if (target == null || string.IsNullOrEmpty(name) || args == null) return false;
            try
            {
                MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (m.Name != name) continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length != args.Length) continue;
                    try
                    {
                        object[] a = new object[ps.Length];
                        for (int j = 0; j < ps.Length; j++)
                        {
                            if (args[j] == null) { a[j] = null; continue; }
                            try { a[j] = Convert.ChangeType(args[j], ps[j].ParameterType); }
                            catch { a[j] = args[j]; }
                        }
                        result = m.Invoke(target, a);
                        return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        // ------------------------------------------------------------------
        // UI (instance method: uses instance GUIStyles)
        // ------------------------------------------------------------------
        internal void DrawProS(float width)
        {
            try
            {
                float baseW = GetMenuWorkWidth(120f, 760f);
                float contentWidth = width > 60f ? Mathf.Clamp(width, 96f, baseW) : baseW;
                float cardPadding = menuCardStyle != null && menuCardStyle.padding != null
                    ? menuCardStyle.padding.left + menuCardStyle.padding.right
                    : 28f;
                float innerWidth = Mathf.Max(68f, contentWidth - cardPadding);
                int toggleWidth = Mathf.RoundToInt(Mathf.Min(300f, innerWidth));

                GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(contentWidth));
                DrawMenuSectionHeader(L("ANTI-ABUSE / NETWORK", "АНТИ-ЧИТ / СЕТЬ"));

                proSpoofPing = DrawToggle(proSpoofPing, L("Fake Ping Overlay (Cosmetic)", "Фейк пинг (визуально)"), toggleWidth);
                GUILayout.Space(4);
                ProSDrawPingSliderS(innerWidth);
                GUILayout.Space(10);

                proHostMigrationAlert = DrawToggle(proHostMigrationAlert, L("Host Migration Alert", "Уведомление о смене хоста"), toggleWidth);
                GUILayout.Space(4);
                proNetIdSanity = DrawToggle(proNetIdSanity, L("NetId Sanity Check", "Проверка NetId"), toggleWidth);
                GUILayout.Space(4);
                proSessionLogger = DrawToggle(proSessionLogger, L("Session Logger", "Лог сессий"), toggleWidth);
                GUILayout.Space(4);
                proIdentityLogger = DrawToggle(proIdentityLogger, L("Identity Logger (Host Only)", "Лог игроков (только хост)"), toggleWidth);
                GUILayout.Space(8);

                GUILayout.Label(MenuText(L("Logs: AeroMenu/ProSessions.txt, ProIdentities.txt", "Логи: AeroMenu/ProSessions.txt, ProIdentities.txt")), menuDescStyle);
                GUILayout.EndVertical();
            }
            catch { }
        }

        private void ProSDrawPingSliderS(float innerWidth)
        {
            try
            {
                float labelW = Mathf.Min(160f, Mathf.Max(60f, innerWidth * 0.4f));
                float sliderW = Mathf.Max(24f, innerWidth - labelW - 8f);
                string hex = GetMenuAccentHex();

                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
                GUILayout.Label(L("Fake Ping:", "Фейк пинг:") + " <color=#" + hex + ">" + proSpoofPingValue + " ms</color>", toggleLabelStyle, GUILayout.Width(labelW));

                GUIStyle sl = sliderStyle ?? (GUI.skin != null ? GUI.skin.horizontalSlider : null);
                GUIStyle th = sliderThumbStyle ?? (GUI.skin != null ? GUI.skin.horizontalSliderThumb : null);
                float v = sl != null && th != null
                    ? GUILayout.HorizontalSlider((float)proSpoofPingValue, 1f, 999f, sl, th, GUILayout.Width(sliderW))
                    : ThemedSlider((float)proSpoofPingValue, 1f, 999f, GUILayout.Width(sliderW));

                int nv = Mathf.Clamp(Mathf.RoundToInt(v), 1, 999);
                if (nv != proSpoofPingValue) proSpoofPingValue = nv;
                GUILayout.EndHorizontal();
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Overlay (static, lazily cached, no per-frame style/texture allocation)
        // ------------------------------------------------------------------
        internal static void DrawProSOverlay()
        {
            try
            {
                if (!proSpoofPing) return;
                if (Screen.width <= 1 || Screen.height <= 1) return;

                ProSEnsureOverlayS();
                if (proSOverlayStyle == null) return;

                if (proSOverlayPingCached != proSpoofPingValue)
                {
                    proSOverlayPingCached = proSpoofPingValue;
                    proSOverlayPingText = "PING " + proSpoofPingValue + " ms";
                }

                const float w = 130f;
                const float h = 26f;
                Rect r = new Rect(Screen.width - w - 12f, 10f, w, h);
                GUI.Label(r, proSOverlayPingText, proSOverlayStyle);
            }
            catch { }
        }

        private static void ProSEnsureOverlayS()
        {
            try
            {
                if (proSOverlayStyle != null && proSOverlayBg != null) return;

                if (proSOverlayBg == null)
                {
                    proSOverlayBg = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    proSOverlayBg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.62f));
                    proSOverlayBg.Apply();
                }

                if (proSOverlayStyle == null)
                {
                    GUIStyle baseStyle = GUI.skin != null ? GUI.skin.label : null;
                    proSOverlayStyle = baseStyle != null ? new GUIStyle(baseStyle) : new GUIStyle();
                    proSOverlayStyle.alignment = TextAnchor.MiddleCenter;
                    proSOverlayStyle.fontSize = 13;
                    proSOverlayStyle.fontStyle = FontStyle.Bold;
                    proSOverlayStyle.normal.textColor = new Color(0.55f, 1f, 0.62f, 1f);
                    proSOverlayStyle.normal.background = proSOverlayBg;
                    proSOverlayStyle.padding = new RectOffset { left = 6, right = 6, top = 3, bottom = 3 };
                }
            }
            catch { }
        }
    }
}
