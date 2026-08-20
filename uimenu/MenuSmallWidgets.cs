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
private bool DrawColoredActionButton(string text, Color color, float width, float height = 24f, bool exactWidth = false)
        {
            text = MenuText(text);
            GUIStyle style = coloredActionButtonStyle;
            Color themedColor = RgbMenuTextActive() ? GetMenuAccentColor() : (whiteMenuTheme ? GetThemeAccentColor(color) : color);
            Color hoverColor = whiteMenuTheme
                ? Color.Lerp(themedColor, Color.black, 0.18f)
                : Color.Lerp(themedColor, Color.white, 0.22f);

            style.normal.textColor = themedColor;
            style.hover.textColor = hoverColor;
            style.focused.textColor = themedColor;
            style.active.textColor = whiteMenuTheme ? Color.white : Color.black;
            style.clipping = exactWidth ? TextClipping.Clip : TextClipping.Overflow;
            style.wordWrap = false;

            float minContentWidth = Mathf.Ceil(style.CalcSize(GUIContent.Temp(text)).x) + 32f;
            float finalButtonWidth = exactWidth ? width : Mathf.Max(width, minContentWidth);
            return GUILayout.Button(text, style, GUILayout.Width(finalButtonWidth), GUILayout.Height(height));
        }

private GUIStyle CreateClippedButtonStyle(GUIStyle sourceStyle)
        {
            return sourceStyle == activeTabStyle ? clippedActiveButtonStyle : clippedButtonStyle;
        }

private GUIStyle CreateCompactMenuCardStyle()
        {
            return compactMenuCardStyle;
        }

private bool DrawCompactToggle(bool value, string text, int width = 0)
        {
            text = MenuText(text);
            int finalWidth = width > 0 ? Mathf.Max(width, 44) : 168;
            GUILayout.BeginHorizontal(GUILayout.Width(finalWidth), GUILayout.Height(17));

            Rect animSwitchRect = GUILayoutUtility.GetRect(28f, 14f, GUILayout.Width(28f), GUILayout.Height(14f));
            bool clickedBox = GUI.Button(animSwitchRect, "", value ? trackOnStyle : trackOffStyle);
            DrawAnimatedSwitch(animSwitchRect, value, text);

            GUILayout.Space(4);

            float textWidth = Mathf.Max(42f, finalWidth - 36f);
            Rect textRect = GUILayoutUtility.GetRect(textWidth, 16f, GUILayout.Width(textWidth), GUILayout.Height(16f));
            GUI.Label(textRect, text, compactToggleTextStyle);

            bool clickedText = Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition);
            if (clickedText) Event.current.Use();

            GUILayout.EndHorizontal();

            if (clickedBox || clickedText) settingsDirty = true;
            return (clickedBox || clickedText) ? !value : value;
        }

private bool DrawFixedMenuButton(string text, GUIStyle sourceStyle, float width, float height)
        {
            return GUILayout.Button(MenuText(text), CreateClippedButtonStyle(sourceStyle), GUILayout.Width(width), GUILayout.Height(height));
        }

private bool DrawPseudoInputButton(string value, bool editing, float height = 28f, int maxChars = 52)
        {
            GUIStyle style = editing ? activePseudoInputStyle : pseudoInputStyle;

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            return GUI.Button(rect, FormatInputPreview(value, editing, maxChars), style);
        }

private void DrawClippedHint(string text, float height = 13f)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, clippedHintStyle, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            GUI.Label(rect, text, clippedHintStyle);
        }

private void OpenExternalLink(string url, string label)
        {
            try
            {
                Application.OpenURL(url);
                ShowNotification($"<color=#00FFAA>[LINK]</color> Opening <b>{label}</b>");
            }
            catch
            {
                ShowNotification("<color=#FF4444>[LINK]</color> Failed to open link.");
            }
        }


    }
}
