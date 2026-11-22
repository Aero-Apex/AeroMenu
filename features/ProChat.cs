#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
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
    // ------------------------------------------------------------------------
    // Group Q - Chat (PRO). Inserted into the existing SELF tab.
    // All state is public static with the exact names required by the contract.
    // No Harmony patches: everything is driven from Tick + guarded reflection.
    // ------------------------------------------------------------------------
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // ===== Public state (exact contract names) ==========================
        public static bool proChatRoleColors;
        public static bool proChatBypassFilter;
        public static bool proChatClipboard;
        public static bool proChatHighlighter;
        public static string proHighlightWords = string.Empty;
        public static bool proChatFontSize;
        public static int proChatFontSizeValue = 24;

        // ===== Private working state (all suffixed with Q) ==================
        private const int ProChatFontMinQ = 8;
        private const int ProChatFontMaxQ = 72;
        private const int ProHighlightMaxWordsQ = 32;
        private const BindingFlags ProChatFlagsQ =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.FlattenHierarchy;

        // All string/color/font writes are gated to at most once per 0.25s.
        private static float proChatNextWriteAtQ;
        private static readonly List<ProChatTargetStateQ> proChatStatesQ = new List<ProChatTargetStateQ>();

        // TextBoxTMP filter bypass state (originals captured once per box).
        private static object proChatBypassBoxQ;
        private static bool proChatBypassStoredQ;
        private static bool proChatBypassOrigAllowAllQ;
        private static bool proChatBypassOrigSymbolsQ;
        private static bool proChatBypassOrigEmailQ;

        private class ProChatTargetStateQ
        {
            public object target;
            public bool seen;
            public string originalText;
            public string appliedText;
            public string lastCopiedText;
            public Color originalColor = Color.white;
            public float originalFontSize;
            public float appliedFontSize;
            public bool hasColor;
            public bool hasFont;
            public bool textModified;
            public bool colorModified;
            public bool fontModified;
        }

        // ====================================================================
        // Required public surface
        // ====================================================================

        internal void DrawProQ(float width)
        {
            try
            {
                float workWidth = width > 0f ? width : GetMenuWorkWidth(180f, 420f);
                int toggleW = Mathf.Clamp(Mathf.RoundToInt(workWidth), 170, 520);
                GUIStyle buttonStyle = btnStyle ?? GUI.skin.button;
                GUIStyle fieldStyle = inputBlockStyle ?? GUI.skin.textField;

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    DrawMenuSectionHeader(L("CHAT PRO", "CHAT PRO"));

                    // Role colors ---------------------------------------------
                    proChatRoleColors = DrawToggle(proChatRoleColors,
                        L("Role-Colored Own Messages", "Role-Colored Own Messages"), toggleW, "proChatRoleColorsQ");
                    GUILayout.Label(L("Colors your own chat messages with your current role color.",
                                      "Colors your own chat messages with your current role color."), menuDescStyle);
                    GUILayout.Space(6);

                    // Bypass filter -------------------------------------------
                    proChatBypassFilter = DrawToggle(proChatBypassFilter,
                        L("Bypass Chat Filter", "Bypass Chat Filter"), toggleW, "proChatBypassFilterQ");
                    GUILayout.Label(L("Allows all characters in the free-chat input box.",
                                      "Allows all characters in the free-chat input box."), menuDescStyle);
                    GUILayout.Space(6);

                    // Clipboard -----------------------------------------------
                    proChatClipboard = DrawToggle(proChatClipboard,
                        L("Copy Chat To Clipboard", "Copy Chat To Clipboard"), toggleW, "proChatClipboardQ");
                    GUILayout.Label(L("Automatically copies each new chat message to the clipboard.",
                                      "Automatically copies each new chat message to the clipboard."), menuDescStyle);
                    GUILayout.Space(6);

                    // Highlighter ---------------------------------------------
                    proChatHighlighter = DrawToggle(proChatHighlighter,
                        L("Highlight Keywords", "Highlight Keywords"), toggleW, "proChatHighlighterQ");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(L("Keywords:", "Keywords:"), GUILayout.Width(96));
                    string nextWords = GUILayout.TextField(proHighlightWords ?? string.Empty, fieldStyle);
                    if (!string.Equals(nextWords, proHighlightWords, StringComparison.Ordinal))
                        proHighlightWords = nextWords ?? string.Empty;
                    GUILayout.EndHorizontal();
                    GUILayout.Label(L("Comma-separated. Matching words are highlighted in yellow.",
                                      "Comma-separated. Matching words are highlighted in yellow."), menuDescStyle);
                    GUILayout.Space(6);

                    // Font size ------------------------------------------------
                    proChatFontSize = DrawToggle(proChatFontSize,
                        L("Chat Font Size", "Chat Font Size"), toggleW, "proChatFontSizeQ");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(L("Size:", "Size:"), GUILayout.Width(96));
                    float slider = GUILayout.HorizontalSlider(proChatFontSizeValue, ProChatFontMinQ, ProChatFontMaxQ);
                    proChatFontSizeValue = Mathf.Clamp(Mathf.RoundToInt(slider), ProChatFontMinQ, ProChatFontMaxQ);
                    GUILayout.Label(proChatFontSizeValue.ToString(), GUILayout.Width(32));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(8);
                    if (GUILayout.Button(L("Save Chat Pro Settings", "Save Chat Pro Settings"), buttonStyle, GUILayout.Height(24)))
                    {
                        SaveProQ();
                        ShowNotification(L("<color=#00FFAA>[CHAT PRO]</color> Settings saved.",
                                           "<color=#00FFAA>[CHAT PRO]</color> Settings saved."));
                    }
                }
                finally
                {
                    GUILayout.EndVertical();
                }
            }
            catch { }
        }

        internal static void TickProQ(bool inputBlocked)
        {
            // This group has no hotkeys, so inputBlocked is intentionally not
            // consumed. All work below is non-input and runs regardless.
            try
            {
                float now = Time.unscaledTime;
                if (now < proChatNextWriteAtQ) return;
                proChatNextWriteAtQ = now + 0.25f;

                object chat = GetChatControllerQ();
                if (chat == null) return;

                bool anyActive = proChatRoleColors || proChatBypassFilter || proChatClipboard ||
                                 proChatHighlighter || proChatFontSize;
                if (!anyActive)
                {
                    RestoreAllQ();
                    return;
                }

                ApplyBypassQ(chat);
                ScanChatQ(chat);
                PruneStatesQ();
            }
            catch { }
        }

        internal static void LoadProQ()
        {
            try
            {
                proChatRoleColors = LoadBool("M_ProQ_proChatRoleColors", false);
                proChatBypassFilter = LoadBool("M_ProQ_proChatBypassFilter", false);
                proChatClipboard = LoadBool("M_ProQ_proChatClipboard", false);
                proChatHighlighter = LoadBool("M_ProQ_proChatHighlighter", false);
                proHighlightWords = PlayerPrefs.GetString("M_ProQ_proHighlightWords", string.Empty) ?? string.Empty;
                proChatFontSize = LoadBool("M_ProQ_proChatFontSize", false);
                proChatFontSizeValue = PlayerPrefs.GetInt("M_ProQ_proChatFontSizeValue", 24);
                proChatFontSizeValue = Mathf.Clamp(proChatFontSizeValue, ProChatFontMinQ, ProChatFontMaxQ);

                proChatNextWriteAtQ = 0f;
                proChatStatesQ.Clear();
                proChatBypassStoredQ = false;
                proChatBypassBoxQ = null;
            }
            catch { }
        }

        internal static void SaveProQ()
        {
            try
            {
                SaveBool("M_ProQ_proChatRoleColors", proChatRoleColors);
                SaveBool("M_ProQ_proChatBypassFilter", proChatBypassFilter);
                SaveBool("M_ProQ_proChatClipboard", proChatClipboard);
                SaveBool("M_ProQ_proChatHighlighter", proChatHighlighter);
                PlayerPrefs.SetString("M_ProQ_proHighlightWords", proHighlightWords ?? string.Empty);
                SaveBool("M_ProQ_proChatFontSize", proChatFontSize);
                PlayerPrefs.SetInt("M_ProQ_proChatFontSizeValue",
                    Mathf.Clamp(proChatFontSizeValue, ProChatFontMinQ, ProChatFontMaxQ));
            }
            catch { }
        }

        internal static void DrawProQOverlay()
        {
            // No persistent HUD element belongs to this group. The screen guard
            // is kept so any future overlay stays allocation-free.
            if (Screen.width <= 1 || Screen.height <= 1) return;
        }

        // ====================================================================
        // Reflection primitives (all guarded; missing members => no-op)
        // ====================================================================

        private static object GetMemberQ(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                Type t = target.GetType();
                FieldInfo f = t.GetField(name, ProChatFlagsQ);
                if (f != null) return f.GetValue(target);
                PropertyInfo p = t.GetProperty(name, ProChatFlagsQ);
                if (p != null && p.CanRead) return p.GetValue(target, null);
            }
            catch { }
            return null;
        }

        private static object GetStaticMemberQ(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                FieldInfo f = type.GetField(name, ProChatFlagsQ);
                if (f != null) return f.GetValue(null);
                PropertyInfo p = type.GetProperty(name, ProChatFlagsQ);
                if (p != null && p.CanRead) return p.GetValue(null, null);
            }
            catch { }
            return null;
        }

        private static bool SetMemberQ(object target, string name, object value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return false;
            try
            {
                Type t = target.GetType();
                FieldInfo f = t.GetField(name, ProChatFlagsQ);
                if (f != null) { f.SetValue(target, value); return true; }
                PropertyInfo p = t.GetProperty(name, ProChatFlagsQ);
                if (p != null && p.CanWrite) { p.SetValue(target, value, null); return true; }
            }
            catch { }
            return false;
        }

        private static bool GetBoolQ(object target, string name, out bool value)
        {
            value = false;
            object o = GetMemberQ(target, name);
            if (o is bool b) { value = b; return true; }
            return false;
        }

        private static bool GetFloatQ(object target, string name, out float value)
        {
            value = 0f;
            object o = GetMemberQ(target, name);
            if (o is float f) { value = f; return true; }
            if (o is double d) { value = (float)d; return true; }
            if (o is int i) { value = (float)i; return true; }
            return false;
        }

        private static bool GetColorQ(object target, out Color value)
        {
            value = Color.white;
            object o = GetMemberQ(target, "color");
            if (o is Color c) { value = c; return true; }
            return false;
        }

        private static string ReadTextQ(object target)
        {
            object o = GetMemberQ(target, "text");
            return o is string s ? s : string.Empty;
        }

        // ====================================================================
        // Game object discovery
        // ====================================================================

        private static object GetChatControllerQ()
        {
            try
            {
                object hud = GetStaticMemberQ(typeof(HudManager), "Instance");
                if (hud == null) return null;
                return GetMemberQ(hud, "Chat");
            }
            catch { return null; }
        }

        private static List<object> GetChatBubblesQ(object chat)
        {
            var result = new List<object>();
            try
            {
                Component comp = chat as Component;
                if (comp == null) return result;
                Component[] all = comp.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    Component c = all[i];
                    if (c == null) continue;
                    string typeName = c.GetType().Name;
                    if (typeName.IndexOf("ChatBubble", StringComparison.Ordinal) >= 0)
                        result.Add(c);
                }
            }
            catch { }
            return result;
        }

        private static object GetLocalPlayerDataQ()
        {
            try
            {
                object local = GetStaticMemberQ(typeof(PlayerControl), "LocalPlayer");
                if (local == null) return null;
                return GetMemberQ(local, "Data");
            }
            catch { return null; }
        }

        private static bool IsOwnBubbleQ(object bubble, object localData)
        {
            try
            {
                object info = GetMemberQ(bubble, "playerInfo");
                return ReferenceEquals(info, localData);
            }
            catch { return false; }
        }

        private static Color GetLocalRoleColorQ()
        {
            try
            {
                object local = GetStaticMemberQ(typeof(PlayerControl), "LocalPlayer");
                if (local == null) return Color.white;
                object data = GetMemberQ(local, "Data");
                object role = data != null ? GetMemberQ(data, "Role") : null;
                if (role == null) return Color.white;

                object teamObj = GetMemberQ(role, "TeamColor");
                Color fallback = teamObj is Color tc ? tc : Color.white;

                object roleObj = GetMemberQ(role, "Role");
                int roleId = -1;
                if (roleObj != null)
                {
                    try { roleId = Convert.ToInt32(roleObj); }
                    catch { roleId = -1; }
                }

                try { return GetRoleColor(roleId, fallback); }
                catch { return fallback; }
            }
            catch { return Color.white; }
        }

        // ====================================================================
        // Feature application
        // ====================================================================

        private static void ApplyBypassQ(object chat)
        {
            try
            {
                object free = GetMemberQ(chat, "freeChatField");
                object box = free != null ? GetMemberQ(free, "textArea") : null;

                if (proChatBypassFilter)
                {
                    if (box == null) return;

                    if (proChatBypassStoredQ && !ReferenceEquals(box, proChatBypassBoxQ))
                    {
                        // Chat controller was recreated - drop the stale snapshot.
                        proChatBypassStoredQ = false;
                        proChatBypassBoxQ = null;
                    }

                    if (!proChatBypassStoredQ)
                    {
                        proChatBypassBoxQ = box;
                        bool a, s, e;
                        GetBoolQ(box, "allowAllCharacters", out a);
                        GetBoolQ(box, "AllowSymbols", out s);
                        GetBoolQ(box, "AllowEmail", out e);
                        proChatBypassOrigAllowAllQ = a;
                        proChatBypassOrigSymbolsQ = s;
                        proChatBypassOrigEmailQ = e;
                        proChatBypassStoredQ = true;
                    }

                    SetMemberQ(box, "allowAllCharacters", true);
                    SetMemberQ(box, "AllowSymbols", true);
                    SetMemberQ(box, "AllowEmail", true);
                }
                else if (proChatBypassStoredQ)
                {
                    object restoreBox = proChatBypassBoxQ;
                    if (restoreBox != null)
                    {
                        SetMemberQ(restoreBox, "allowAllCharacters", proChatBypassOrigAllowAllQ);
                        SetMemberQ(restoreBox, "AllowSymbols", proChatBypassOrigSymbolsQ);
                        SetMemberQ(restoreBox, "AllowEmail", proChatBypassOrigEmailQ);
                    }
                    proChatBypassStoredQ = false;
                    proChatBypassBoxQ = null;
                }
            }
            catch { }
        }

        private static void ScanChatQ(object chat)
        {
            List<object> bubbles = GetChatBubblesQ(chat);
            object localData = GetLocalPlayerDataQ();
            Color roleColor = proChatRoleColors ? GetLocalRoleColorQ() : Color.white;
            string[] words = proChatHighlighter ? ParseHighlightWordsQ(proHighlightWords) : null;

            for (int i = 0; i < bubbles.Count; i++)
            {
                try
                {
                    object bubble = bubbles[i];
                    object textArea = GetMemberQ(bubble, "TextArea");
                    if (textArea == null) continue;

                    ProChatTargetStateQ st = GetStateQ(textArea);
                    st.seen = true;

                    string current = ReadTextQ(textArea);
                    string source = ResolveSourceQ(st, current);

                    // Clipboard: copy each newly observed message (clean text).
                    if (proChatClipboard && !string.IsNullOrEmpty(source) &&
                        !string.Equals(source, st.lastCopiedText, StringComparison.Ordinal))
                    {
                        st.lastCopiedText = source;
                        string clean = CleanChatTextQ(source);
                        if (!string.IsNullOrEmpty(clean))
                            GUIUtility.systemCopyBuffer = clean;
                    }

                    // Highlighter: write only when the rendered text must change.
                    if (words != null && words.Length > 0)
                    {
                        string baseText = !string.IsNullOrEmpty(st.originalText) ? st.originalText : source;
                        string highlighted = HighlightQ(baseText, words);
                        if (!string.IsNullOrEmpty(highlighted) &&
                            !string.Equals(highlighted, st.appliedText, StringComparison.Ordinal))
                        {
                            SetMemberQ(textArea, "text", highlighted);
                            st.appliedText = highlighted;
                            st.textModified = true;
                        }
                    }
                    else if (st.textModified)
                    {
                        SetMemberQ(textArea, "text", st.originalText ?? string.Empty);
                        st.appliedText = null;
                        st.textModified = false;
                    }

                    // Role colors: own messages only.
                    if (proChatRoleColors && localData != null && IsOwnBubbleQ(bubble, localData))
                    {
                        SetMemberQ(textArea, "color", roleColor);
                        st.colorModified = true;
                    }
                    else if (st.colorModified)
                    {
                        SetMemberQ(textArea, "color", st.originalColor);
                        st.colorModified = false;
                    }

                    ApplyFontSizeQ(st, textArea);
                }
                catch { }
            }

            // The free-chat output text also follows the font-size setting.
            try
            {
                object free = GetMemberQ(chat, "freeChatField");
                object box = free != null ? GetMemberQ(free, "textArea") : null;
                object output = box != null ? GetMemberQ(box, "outputText") : null;
                if (output != null)
                {
                    ProChatTargetStateQ st = GetStateQ(output);
                    st.seen = true;
                    ApplyFontSizeQ(st, output);
                }
            }
            catch { }
        }

        private static string ResolveSourceQ(ProChatTargetStateQ st, string current)
        {
            string source;
            if (st.textModified)
            {
                if (string.Equals(current, st.appliedText, StringComparison.Ordinal))
                {
                    // Still our highlighted text - use the cached original.
                    source = st.originalText;
                }
                else
                {
                    // The game replaced it (pooled bubble reused for a new line).
                    st.originalText = current;
                    st.appliedText = null;
                    st.textModified = false;
                    source = current;
                }
            }
            else
            {
                if (!string.Equals(current, st.originalText, StringComparison.Ordinal))
                    st.originalText = current;
                source = current;
            }
            return source ?? string.Empty;
        }

        private static ProChatTargetStateQ GetStateQ(object target)
        {
            for (int i = 0; i < proChatStatesQ.Count; i++)
            {
                if (ReferenceEquals(proChatStatesQ[i].target, target))
                    return proChatStatesQ[i];
            }

            var st = new ProChatTargetStateQ();
            st.target = target;
            st.originalText = ReadTextQ(target);
            st.hasColor = GetColorQ(target, out st.originalColor);
            st.hasFont = GetFloatQ(target, "fontSize", out st.originalFontSize);
            proChatStatesQ.Add(st);
            return st;
        }

        private static void ApplyFontSizeQ(ProChatTargetStateQ st, object target)
        {
            if (!st.hasFont) return;
            float desired = Mathf.Clamp(proChatFontSizeValue, ProChatFontMinQ, ProChatFontMaxQ);
            if (proChatFontSize)
            {
                if (st.fontModified && Mathf.Approximately(st.appliedFontSize, desired)) return;
                SetMemberQ(target, "fontSize", desired);
                st.appliedFontSize = desired;
                st.fontModified = true;
            }
            else if (st.fontModified)
            {
                SetMemberQ(target, "fontSize", st.originalFontSize);
                st.fontModified = false;
            }
        }

        private static void PruneStatesQ()
        {
            for (int i = proChatStatesQ.Count - 1; i >= 0; i--)
            {
                if (!proChatStatesQ[i].seen)
                    proChatStatesQ.RemoveAt(i);
            }
        }

        private static void RestoreAllQ()
        {
            try
            {
                for (int i = 0; i < proChatStatesQ.Count; i++)
                {
                    ProChatTargetStateQ st = proChatStatesQ[i];
                    try
                    {
                        if (st.textModified)
                        {
                            SetMemberQ(st.target, "text", st.originalText ?? string.Empty);
                            st.textModified = false;
                            st.appliedText = null;
                        }
                        if (st.colorModified)
                        {
                            SetMemberQ(st.target, "color", st.originalColor);
                            st.colorModified = false;
                        }
                        if (st.fontModified)
                        {
                            SetMemberQ(st.target, "fontSize", st.originalFontSize);
                            st.fontModified = false;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            proChatStatesQ.Clear();

            try
            {
                if (proChatBypassStoredQ && proChatBypassBoxQ != null)
                {
                    SetMemberQ(proChatBypassBoxQ, "allowAllCharacters", proChatBypassOrigAllowAllQ);
                    SetMemberQ(proChatBypassBoxQ, "AllowSymbols", proChatBypassOrigSymbolsQ);
                    SetMemberQ(proChatBypassBoxQ, "AllowEmail", proChatBypassOrigEmailQ);
                }
            }
            catch { }
            proChatBypassStoredQ = false;
            proChatBypassBoxQ = null;
        }

        // ====================================================================
        // Text helpers
        // ====================================================================

        private static string[] ParseHighlightWordsQ(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new string[0];
            string[] parts = raw.Split(new char[] { ',', ';', '|', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            for (int i = 0; i < parts.Length && result.Count < ProHighlightMaxWordsQ; i++)
            {
                string word = parts[i].Trim();
                if (word.Length > 0) result.Add(word);
            }
            return result.ToArray();
        }

        private static string HighlightQ(string text, string[] words)
        {
            if (string.IsNullOrEmpty(text) || words == null || words.Length == 0) return text;

            string lower = text.ToLowerInvariant();
            bool[] marked = new bool[text.Length];
            bool[] inTag = new bool[text.Length];
            bool tagOpen = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '<') { tagOpen = true; inTag[i] = true; continue; }
                if (ch == '>' && tagOpen) { inTag[i] = true; tagOpen = false; continue; }
                if (tagOpen) inTag[i] = true;
            }

            bool any = false;
            for (int w = 0; w < words.Length; w++)
            {
                string word = words[w].ToLowerInvariant();
                if (word.Length == 0) continue;
                int from = 0;
                while (from <= lower.Length - word.Length)
                {
                    int found = lower.IndexOf(word, from, StringComparison.Ordinal);
                    if (found < 0) break;
                    bool plain = true;
                    for (int j = found; j < found + word.Length; j++)
                    {
                        if (inTag[j]) { plain = false; break; }
                    }
                    if (plain)
                    {
                        for (int j = found; j < found + word.Length; j++) marked[j] = true;
                        any = true;
                    }
                    from = found + word.Length;
                }
            }

            if (!any) return text;

            var sb = new System.Text.StringBuilder(text.Length + words.Length * 24);
            bool open = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (marked[i] && !open) { sb.Append("<color=#FFE600>"); open = true; }
                if (!marked[i] && open) { sb.Append("</color>"); open = false; }
                sb.Append(text[i]);
            }
            if (open) sb.Append("</color>");
            return sb.ToString();
        }

        private static string CleanChatTextQ(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new System.Text.StringBuilder(text.Length);
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '<') { inTag = true; continue; }
                if (ch == '>' && inTag) { inTag = false; continue; }
                if (!inTag) sb.Append(ch);
            }
            return sb.ToString().Trim();
        }
    }
}
