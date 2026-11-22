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
        // ====================================================================
        // Group R state (public static, exact contract names)
        // ====================================================================

        // Clipboard tool.
        public static bool proClipboardTool;
        public static string proClipboardText = string.Empty;

        // Real clock overlay.
        public static bool proRealClock;

        // Safe arithmetic calculator.
        public static bool proCalculator;
        public static string proCalcExpression = string.Empty;

        // Persistent notes pad.
        public static bool proNotesPad;
        public static string proNotesText = string.Empty;

        // Room code generator.
        public static bool proRoomCodeGen;

        // ====================================================================
        // Private working state (all suffixed with R)
        // ====================================================================

        private static readonly System.Random proRngR = new System.Random();

        private static string proRCalcResultR = string.Empty;
        private static string proRCalcErrorR = string.Empty;
        private static string proRLastCodeR = string.Empty;

        private static string proRClockTextR = string.Empty;
        private static float proRClockNextR;

        // Lazily cached overlay resources (created once, never per-frame).
        private static bool proRStylesReadyR;
        private static GUIStyle proRClockTitleStyleR;
        private static GUIStyle proRClockStyleR;
        private static Texture2D proRPanelTexR;

        // ====================================================================
        // Required public surface
        // ====================================================================

        internal void DrawProR(float width)
        {
            try
            {
                int toggleW = Mathf.Clamp(
                    Mathf.RoundToInt(width > 0f ? width : GetMenuWorkWidth(180f, 460f)), 170, 460);

                GUIStyle buttonStyle = btnStyle ?? GUI.skin.button;
                GUIStyle fieldStyle = inputBlockStyle ?? GUI.skin.textField;
                GUIStyle areaStyle = inputBlockStyle ?? GUI.skin.textArea;

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    // --- Clipboard tool --------------------------------------
                    DrawMenuSectionHeader(L("CLIPBOARD TOOL", "БУФЕР ОБМЕНА"));
                    proClipboardTool = DrawToggle(proClipboardTool,
                        L("Enable Clipboard Tool", "Включить инструмент буфера обмена"),
                        toggleW, "proClipboardTool");
                    if (proClipboardTool)
                    {
                        GUILayout.Label(MenuText(L("Text:", "Текст:")), menuDescStyle);
                        string typedClip = GUILayout.TextField(proClipboardText ?? string.Empty,
                            fieldStyle, GUILayout.Height(24));
                        if (!string.Equals(typedClip, proClipboardText, StringComparison.Ordinal))
                        {
                            proClipboardText = typedClip ?? string.Empty;
                            SaveProR();
                        }

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(MenuText(L("Copy", "Копировать")), buttonStyle,
                                             GUILayout.Width(96), GUILayout.Height(24)))
                            CopyClipboardTextR();
                        if (GUILayout.Button(MenuText(L("Paste", "Вставить")), buttonStyle,
                                             GUILayout.Width(96), GUILayout.Height(24)))
                            PasteClipboardTextR();
                        if (GUILayout.Button(MenuText(L("Clear", "Очистить")), buttonStyle,
                                             GUILayout.Width(80), GUILayout.Height(24)))
                        {
                            proClipboardText = string.Empty;
                            SaveProR();
                            ShowNotification(L("<color=#00FFAA>[UTIL]</color> Clipboard field cleared.",
                                               "<color=#00FFAA>[УТИЛИТА]</color> Поле буфера очищено."));
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        GUILayout.Label(MenuText(L("Copies/pastes text using the system clipboard.",
                                                   "Копирует и вставляет текст через системный буфер обмена.")),
                                        menuDescStyle);
                    }
                    GUILayout.Space(8);

                    // --- Real clock ------------------------------------------
                    DrawMenuSectionHeader(L("REAL CLOCK", "РЕАЛЬНЫЕ ЧАСЫ"));
                    proRealClock = DrawToggle(proRealClock,
                        L("Show Real Date & Time Overlay", "Показывать реальные дату и время"),
                        toggleW, "proRealClock");
                    GUILayout.Label(MenuText(L("Draws the current system date and time in the top-right corner.",
                                               "Отображает текущие системные дату и время в правом верхнем углу.")),
                                    menuDescStyle);
                    GUILayout.Space(8);

                    // --- Calculator ------------------------------------------
                    DrawMenuSectionHeader(L("CALCULATOR", "КАЛЬКУЛЯТОР"));
                    proCalculator = DrawToggle(proCalculator,
                        L("Enable Calculator", "Включить калькулятор"), toggleW, "proCalculator");
                    if (proCalculator)
                    {
                        GUILayout.Label(MenuText(L("Expression (+ - * / and parentheses):",
                                                   "Выражение (+ - * / и скобки):")), menuDescStyle);
                        string typedExpr = GUILayout.TextField(proCalcExpression ?? string.Empty,
                            fieldStyle, GUILayout.Height(24));
                        if (!string.Equals(typedExpr, proCalcExpression, StringComparison.Ordinal))
                        {
                            proCalcExpression = typedExpr ?? string.Empty;
                            SaveProR();
                        }

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(MenuText(L("Calculate", "Вычислить")), buttonStyle,
                                             GUILayout.Width(110), GUILayout.Height(24)))
                            EvaluateCalcR();
                        if (GUILayout.Button(MenuText(L("Copy Result", "Копировать результат")), buttonStyle,
                                             GUILayout.Width(150), GUILayout.Height(24)))
                            CopyCalcResultR();
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        if (!string.IsNullOrEmpty(proRCalcErrorR))
                            GUILayout.Label(MenuText(proRCalcErrorR), menuBadgeStyle);
                        else if (!string.IsNullOrEmpty(proRCalcResultR))
                            GUILayout.Label(MenuText(proRCalcResultR), activeTabStyle);

                        GUILayout.Label(MenuText(L("Example: (12 + 3) * 4 / 2", "Пример: (12 + 3) * 4 / 2")),
                                        menuDescStyle);
                    }
                    GUILayout.Space(8);

                    // --- Notes pad -------------------------------------------
                    DrawMenuSectionHeader(L("NOTES PAD", "ЗАМЕТКИ"));
                    proNotesPad = DrawToggle(proNotesPad,
                        L("Enable Notes Pad", "Включить блок заметок"), toggleW, "proNotesPad");
                    if (proNotesPad)
                    {
                        string typedNotes = GUILayout.TextArea(proNotesText ?? string.Empty,
                            areaStyle, GUILayout.Height(130));
                        if (!string.Equals(typedNotes, proNotesText, StringComparison.Ordinal))
                        {
                            proNotesText = typedNotes ?? string.Empty;
                            SaveProR();
                        }
                        GUILayout.Label(MenuText(L("Notes are saved automatically as you type.",
                                                   "Заметки сохраняются автоматически при вводе.")),
                                        menuDescStyle);
                    }
                    GUILayout.Space(8);

                    // --- Room code generator ---------------------------------
                    DrawMenuSectionHeader(L("ROOM CODE GENERATOR", "ГЕНЕРАТОР КОДА КОМНАТЫ"));
                    proRoomCodeGen = DrawToggle(proRoomCodeGen,
                        L("Enable Room Code Generator", "Включить генератор кода комнаты"),
                        toggleW, "proRoomCodeGen");
                    if (proRoomCodeGen)
                    {
                        if (GUILayout.Button(MenuText(L("Generate & Copy Room Code", "Создать и скопировать код")),
                                             buttonStyle, GUILayout.Height(26)))
                            GenerateRoomCodeR();
                        if (!string.IsNullOrEmpty(proRLastCodeR))
                            GUILayout.Label(MenuText(L("Last generated:", "Последний сгенерированный:") + " " + proRLastCodeR),
                                            activeTabStyle);
                        GUILayout.Label(MenuText(L("Produces a random 6-character room-code-like string.",
                                                   "Создаёт случайную строку из 6 символов, похожую на код комнаты.")),
                                        menuDescStyle);
                    }
                }
                finally
                {
                    GUILayout.EndVertical();
                }
            }
            catch { }
        }

        internal static void TickProR(bool inputBlocked)
        {
            try
            {
                // Refresh the clock text a few times per second, only when shown.
                if (proRealClock)
                {
                    float nowTime = Time.realtimeSinceStartup;
                    if (nowTime >= proRClockNextR)
                    {
                        proRClockNextR = nowTime + 0.25f;
                        DateTime now = DateTime.Now;
                        proRClockTextR = now.ToString("yyyy-MM-dd") + "\n" + now.ToString("HH:mm:ss");
                    }
                }
            }
            catch { }

            // This group has no hotkeys; honour the input guard explicitly.
            if (inputBlocked) return;
        }

        internal static void LoadProR()
        {
            try
            {
                proClipboardTool = LoadBool("M_ProR_proClipboardTool", false);
                proClipboardText = PlayerPrefs.GetString("M_ProR_proClipboardText", string.Empty) ?? string.Empty;

                proRealClock = LoadBool("M_ProR_proRealClock", false);

                proCalculator = LoadBool("M_ProR_proCalculator", false);
                proCalcExpression = PlayerPrefs.GetString("M_ProR_proCalcExpression", string.Empty) ?? string.Empty;

                proNotesPad = LoadBool("M_ProR_proNotesPad", false);
                proNotesText = PlayerPrefs.GetString("M_ProR_proNotesText", string.Empty) ?? string.Empty;

                proRoomCodeGen = LoadBool("M_ProR_proRoomCodeGen", false);

                proRCalcResultR = string.Empty;
                proRCalcErrorR = string.Empty;
                proRLastCodeR = string.Empty;
                proRClockTextR = string.Empty;
                proRClockNextR = 0f;
            }
            catch { }
        }

        internal static void SaveProR()
        {
            try
            {
                SaveBool("M_ProR_proClipboardTool", proClipboardTool);
                PlayerPrefs.SetString("M_ProR_proClipboardText", proClipboardText ?? string.Empty);

                SaveBool("M_ProR_proRealClock", proRealClock);

                SaveBool("M_ProR_proCalculator", proCalculator);
                PlayerPrefs.SetString("M_ProR_proCalcExpression", proCalcExpression ?? string.Empty);

                SaveBool("M_ProR_proNotesPad", proNotesPad);
                PlayerPrefs.SetString("M_ProR_proNotesText", proNotesText ?? string.Empty);

                SaveBool("M_ProR_proRoomCodeGen", proRoomCodeGen);
            }
            catch { }
        }

        internal static void DrawProROverlay()
        {
            if (!proRealClock) return;
            if (Screen.width <= 1 || Screen.height <= 1) return;

            Event e = Event.current;
            if (e == null || e.type != EventType.Repaint) return;

            Color previousColor = GUI.color;
            try
            {
                EnsureStylesR();
                if (!proRStylesReadyR) return;

                float panelW = Mathf.Min(260f, Screen.width - 24f);
                if (panelW < 120f) return;
                float panelH = 76f;
                float x = Screen.width - panelW - 14f;
                float y = 14f;

                GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.82f);
                GUI.DrawTexture(new Rect(x, y, panelW, panelH), proRPanelTexR, ScaleMode.StretchToFill, true);
                GUI.color = previousColor;

                GUI.Label(new Rect(x + 10f, y + 8f, panelW - 20f, 18f),
                          MenuText(L("REAL TIME", "РЕАЛЬНОЕ ВРЕМЯ")), proRClockTitleStyleR);
                GUI.Label(new Rect(x + 10f, y + 28f, panelW - 20f, 42f),
                          proRClockTextR, proRClockStyleR);
            }
            catch { }
            finally
            {
                GUI.color = previousColor;
            }
        }

        // ====================================================================
        // Private helpers (all suffixed with R)
        // ====================================================================

        private static void EnsureStylesR()
        {
            if (proRStylesReadyR && proRPanelTexR != null &&
                proRClockStyleR != null && proRClockTitleStyleR != null)
                return;

            try
            {
                if (proRPanelTexR == null)
                {
                    proRPanelTexR = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    proRPanelTexR.SetPixel(0, 0, Color.white);
                    proRPanelTexR.Apply();
                }

                if (proRClockTitleStyleR == null)
                {
                    proRClockTitleStyleR = new GUIStyle
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.UpperLeft,
                        clipping = TextClipping.Clip,
                        wordWrap = false,
                        richText = true
                    };
                    proRClockTitleStyleR.normal.textColor = new Color(0.35f, 0.90f, 0.95f, 1f);
                }

                if (proRClockStyleR == null)
                {
                    proRClockStyleR = new GUIStyle
                    {
                        fontSize = 17,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.UpperLeft,
                        clipping = TextClipping.Clip,
                        wordWrap = true,
                        richText = true
                    };
                    proRClockStyleR.normal.textColor = Color.white;
                }

                proRStylesReadyR = proRPanelTexR != null &&
                                   proRClockStyleR != null && proRClockTitleStyleR != null;
            }
            catch { }
        }

        private static void CopyClipboardTextR()
        {
            try
            {
                GUIUtility.systemCopyBuffer = proClipboardText ?? string.Empty;
                ShowNotification(L("<color=#00FFAA>[UTIL]</color> Text copied to clipboard.",
                                   "<color=#00FFAA>[УТИЛИТА]</color> Текст скопирован в буфер обмена."));
            }
            catch { }
        }

        private static void PasteClipboardTextR()
        {
            try
            {
                proClipboardText = GUIUtility.systemCopyBuffer ?? string.Empty;
                SaveProR();
                ShowNotification(L("<color=#00FFAA>[UTIL]</color> Text pasted from clipboard.",
                                   "<color=#00FFAA>[УТИЛИТА]</color> Текст вставлен из буфера обмена."));
            }
            catch { }
        }

        private static void EvaluateCalcR()
        {
            double value;
            string error;
            if (!TryEvalCalcR(proCalcExpression, out value, out error))
            {
                proRCalcResultR = string.Empty;
                proRCalcErrorR = MenuText(L("Error: ", "Ошибка: ") + (error ?? "invalid expression"));
                return;
            }

            proRCalcErrorR = string.Empty;
            proRCalcResultR = "= " + FormatCalcValueR(value);
        }

        private static void CopyCalcResultR()
        {
            if (string.IsNullOrEmpty(proRCalcResultR))
            {
                ShowNotification(L("<color=#FFAA00>[CALC]</color> Nothing to copy.",
                                   "<color=#FFAA00>[КАЛЬКУЛЯТОР]</color> Нечего копировать."));
                return;
            }
            try
            {
                GUIUtility.systemCopyBuffer = proRCalcResultR.Substring(2);
                ShowNotification(L("<color=#00FFAA>[CALC]</color> Result copied.",
                                   "<color=#00FFAA>[КАЛЬКУЛЯТОР]</color> Результат скопирован."));
            }
            catch { }
        }

        private static string FormatCalcValueR(double value)
        {
            try
            {
                return value.ToString("0.##########", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return value.ToString();
            }
        }

        private static bool TryEvalCalcR(string expression, out double result, out string error)
        {
            result = 0d;
            error = null;
            if (string.IsNullOrEmpty(expression) || expression.Length > 256)
            {
                error = "empty or too long";
                return false;
            }

            CalcParserR parser = new CalcParserR(expression);
            return parser.TryParseR(out result, out error);
        }

        private static void GenerateRoomCodeR()
        {
            const string alphabetR = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            try
            {
                char[] charsR = new char[6];
                for (int i = 0; i < charsR.Length; i++)
                    charsR[i] = alphabetR[proRngR.Next(alphabetR.Length)];

                string codeR = new string(charsR);
                proRLastCodeR = codeR;
                GUIUtility.systemCopyBuffer = codeR;
                ShowNotification(L("<color=#00FFAA>[UTIL]</color> Copied room code: ",
                                   "<color=#00FFAA>[УТИЛИТА]</color> Скопирован код: ") + "<b>" + codeR + "</b>");
            }
            catch { }
        }

        // --------------------------------------------------------------------
        // Safe arithmetic parser (recursive descent, no eval / no Reflection.Emit)
        //   Expression := Term (('+' | '-') Term)*
        //   Term       := Factor (('*' | '/') Factor)*
        //   Factor     := ('+' | '-') Factor | Primary
        //   Primary    := Number | '(' Expression ')'
        // --------------------------------------------------------------------
        private sealed class CalcParserR
        {
            private readonly string textR;
            private int posR;
            private bool failedR;
            private string errorR;

            internal CalcParserR(string text)
            {
                textR = text ?? string.Empty;
            }

            internal bool TryParseR(out double value, out string error)
            {
                value = 0d;
                error = null;
                if (string.IsNullOrEmpty(textR.Trim()))
                {
                    error = "empty expression";
                    return false;
                }

                try
                {
                    value = ParseExpressionR();
                    SkipWsR();
                    if (failedR)
                    {
                        error = errorR;
                        return false;
                    }
                    if (posR < textR.Length)
                    {
                        error = "unexpected character '" + textR[posR] + "'";
                        return false;
                    }
                    if (double.IsNaN(value) || double.IsInfinity(value))
                    {
                        error = "invalid result";
                        return false;
                    }
                    return true;
                }
                catch
                {
                    error = "invalid expression";
                    return false;
                }
            }

            private double ParseExpressionR()
            {
                double left = ParseTermR();
                while (!failedR)
                {
                    SkipWsR();
                    if (posR >= textR.Length) break;
                    char opR = textR[posR];
                    if (opR != '+' && opR != '-') break;
                    posR++;
                    double right = ParseTermR();
                    if (failedR) break;
                    left = opR == '+' ? left + right : left - right;
                }
                return left;
            }

            private double ParseTermR()
            {
                double left = ParseFactorR();
                while (!failedR)
                {
                    SkipWsR();
                    if (posR >= textR.Length) break;
                    char opR = textR[posR];
                    if (opR != '*' && opR != '/') break;
                    posR++;
                    double right = ParseFactorR();
                    if (failedR) break;
                    if (opR == '/')
                    {
                        if (right == 0d)
                        {
                            FailR("division by zero");
                            break;
                        }
                        left /= right;
                    }
                    else
                    {
                        left *= right;
                    }
                }
                return left;
            }

            private double ParseFactorR()
            {
                SkipWsR();
                if (posR >= textR.Length)
                {
                    FailR("unexpected end");
                    return 0d;
                }
                char cR = textR[posR];
                if (cR == '+')
                {
                    posR++;
                    return ParseFactorR();
                }
                if (cR == '-')
                {
                    posR++;
                    return -ParseFactorR();
                }
                return ParsePrimaryR();
            }

            private double ParsePrimaryR()
            {
                SkipWsR();
                if (posR >= textR.Length)
                {
                    FailR("unexpected end");
                    return 0d;
                }
                char cR = textR[posR];
                if (cR == '(')
                {
                    posR++;
                    double innerR = ParseExpressionR();
                    SkipWsR();
                    if (posR >= textR.Length || textR[posR] != ')')
                    {
                        FailR("missing ')'");
                        return innerR;
                    }
                    posR++;
                    return innerR;
                }
                if (cR == '.' || (cR >= '0' && cR <= '9'))
                    return ParseNumberR();

                FailR("unexpected character '" + cR + "'");
                return 0d;
            }

            private double ParseNumberR()
            {
                SkipWsR();
                double integerPartR = 0d;
                bool anyDigitR = false;
                while (posR < textR.Length && textR[posR] >= '0' && textR[posR] <= '9')
                {
                    integerPartR = integerPartR * 10d + (textR[posR] - '0');
                    posR++;
                    anyDigitR = true;
                }

                double fractionR = 0d;
                double scaleR = 1d;
                if (posR < textR.Length && textR[posR] == '.')
                {
                    posR++;
                    while (posR < textR.Length && textR[posR] >= '0' && textR[posR] <= '9')
                    {
                        scaleR *= 0.1d;
                        fractionR += (textR[posR] - '0') * scaleR;
                        posR++;
                        anyDigitR = true;
                    }
                }

                if (!anyDigitR)
                {
                    FailR("expected number");
                    return 0d;
                }
                return integerPartR + fractionR;
            }

            private void SkipWsR()
            {
                while (posR < textR.Length && char.IsWhiteSpace(textR[posR])) posR++;
            }

            private void FailR(string message)
            {
                if (!failedR)
                {
                    failedR = true;
                    errorR = message;
                }
            }
        }
    }
}
