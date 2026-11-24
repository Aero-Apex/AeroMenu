#nullable disable
using UnityEngine;

namespace AeroMenu
{
    // GUILayout.TextField / TextArea are unusable in this Il2Cpp build: they throw
    // "NotSupportedException: Method unstripping failed" in TextEditor.set_text,
    // which aborts the rest of the tab and leaves layout groups open. This is a
    // self-contained replacement that never touches TextEditor.
    public partial class AeroMenuGUI : MonoBehaviour
    {
        internal static int aeroFocusedField = -1;

        internal static bool AeroTextFieldFocused { get { return aeroFocusedField != -1; } }

        internal static string AeroTextField(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            return AeroTextInput(text, style, false, 256, options);
        }

        internal static string AeroTextArea(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            return AeroTextInput(text, style, true, 4000, options);
        }

        private static string AeroTextInput(string text, GUIStyle style, bool multiline, int maxLength, GUILayoutOption[] options)
        {
            if (text == null) text = string.Empty;
            if (style == null) style = GUI.skin.box;

            int id = GUIUtility.GetControlID(FocusType.Keyboard);
            bool focused = aeroFocusedField == id;
            bool caretOn = focused && ((int)(Time.unscaledTime * 2f) % 2 == 0);
            string shown = text + (caretOn ? "|" : (focused ? " " : string.Empty));

            Rect r = GUILayoutUtility.GetRect(GUIContent.Temp(shown.Length == 0 ? " " : shown), style, options);
            Event e = Event.current;
            if (e == null) return text;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (r.Contains(e.mousePosition))
                    {
                        // Clear the legacy "isEditingX" flags too, otherwise typing
                        // here also feeds whichever pseudo-input was still active.
                        ClearAllEditingFlags();
                        aeroFocusedField = id;
                        e.Use();
                    }
                    else if (focused)
                    {
                        aeroFocusedField = -1;
                    }
                    break;

                case EventType.KeyDown:
                    if (!focused) break;
                    bool mod = e.control || e.command;
                    if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Tab ||
                        (!multiline && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)))
                    {
                        aeroFocusedField = -1;
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (mod) text = string.Empty;
                        else if (text.Length > 0) text = text.Substring(0, text.Length - 1);
                        GUI.changed = true;
                        e.Use();
                    }
                    else if (mod && e.keyCode == KeyCode.V)
                    {
                        string clip = string.Empty;
                        try { clip = GUIUtility.systemCopyBuffer ?? string.Empty; } catch { }
                        if (!multiline) clip = clip.Replace("\r", string.Empty).Replace("\n", " ");
                        text += clip;
                        GUI.changed = true;
                        e.Use();
                    }
                    else if (mod && e.keyCode == KeyCode.C)
                    {
                        try { GUIUtility.systemCopyBuffer = text; } catch { }
                        e.Use();
                    }
                    else if (mod && (e.keyCode == KeyCode.X))
                    {
                        try { GUIUtility.systemCopyBuffer = text; } catch { }
                        text = string.Empty;
                        GUI.changed = true;
                        e.Use();
                    }
                    else if (multiline && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
                    {
                        text += "\n";
                        GUI.changed = true;
                        e.Use();
                    }
                    else if (!mod && e.character != '\0' && !char.IsControl(e.character))
                    {
                        text += e.character;
                        GUI.changed = true;
                        e.Use();
                    }
                    else if (e.keyCode != KeyCode.None)
                    {
                        e.Use();
                    }
                    break;

                case EventType.Repaint:
                    string visible = shown;
                    if (!multiline)
                    {
                        // Keep the end of long text (and the caret) visible.
                        float avail = Mathf.Max(10f, r.width - style.padding.horizontal - 4f);
                        int guard = 0;
                        while (visible.Length > 1 && style.CalcSize(GUIContent.Temp(visible)).x > avail && guard++ < 4000)
                            visible = visible.Substring(1);
                    }
                    Color old = GUI.color;
                    if (focused) GUI.color = new Color(old.r, old.g, old.b, Mathf.Min(1f, old.a));
                    GUI.Label(r, visible, style);
                    GUI.color = old;
                    break;
            }

            if (text.Length > maxLength) text = text.Substring(0, maxLength);
            return text;
        }
    }
}
