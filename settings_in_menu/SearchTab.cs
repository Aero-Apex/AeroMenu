using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private static string searchTabQuery = "";
        private static Vector2 searchScroll;
        private static GUIStyle searchFieldStyle = null;
        private static List<SearchableFeature> searchFeatures = null;
        private static int searchMatchCount = 0;
        private static int searchFilterMode = 0; // 0 = All, 1 = On, 2 = Off

        private sealed class SearchableFeature
        {
            public FieldInfo field;
            public string label;
            public string rawName;
            public string category;
        }

        private static readonly string[] searchCategories = new string[]
        {
            "VISUALS", "ESP", "RADAR", "CHAT", "LOBBY", "SABOTAGE", "COMBAT",
            "INFO", "MEETING", "MENU", "OTHER"
        };

        private static string ClassifyFeature(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("radar")) return "RADAR";
            if (n.Contains("esp") || n.Contains("tracer") || n.Contains("box")) return "ESP";
            if (n.Contains("chat")) return "CHAT";
            if (n.Contains("lobby") || n.Contains("level") || (n.Contains("color") && !n.Contains("menu"))) return "LOBBY";
            if (n.Contains("sabotage") || n.Contains("repair")) return "SABOTAGE";
            if (n.Contains("kill") || n.Contains("morph") || n.Contains("shapeshift") || n.Contains("aura") || n.Contains("combat")) return "COMBAT";
            if (n.Contains("ghost") || n.Contains("phantom") || n.Contains("role") || n.Contains("playerinfo")) return "INFO";
            if (n.Contains("vote") || n.Contains("meeting")) return "MEETING";
            if (n.Contains("menu") || n.Contains("theme") || n.Contains("rgb") || n.Contains("accent") || n.Contains("scale") || n.Contains("fps") || n.Contains("background") || n.Contains("character") || n.Contains("watermark") || n.Contains("taskbar")) return "MENU";
            return "OTHER";
        }

        private static void EnsureSearchFeatures()
        {
            if (searchFeatures != null) return;
            searchFeatures = new List<SearchableFeature>();
            try
            {
                FieldInfo[] fields = typeof(AeroMenuGUI).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fields != null)
                {
                    foreach (FieldInfo f in fields)
                    {
                        if (f == null || f.FieldType != typeof(bool)) continue;
                        if (!IsExposedFeature(f.Name)) continue;
                        searchFeatures.Add(new SearchableFeature
                        {
                            field = f,
                            rawName = f.Name,
                            label = HumanizeFeatureName(f.Name),
                            category = ClassifyFeature(f.Name)
                        });
                    }
                }

                searchFeatures.Sort((a, b) =>
                {
                    int c = string.Compare(a.category, b.category, StringComparison.OrdinalIgnoreCase);
                    if (c != 0) return c;
                    return string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch
            {
                if (searchFeatures == null) searchFeatures = new List<SearchableFeature>();
            }
        }

        private static bool IsExposedFeature(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.Equals("showMenu", StringComparison.OrdinalIgnoreCase)) return false;
            if (name.Equals("stylesInited", StringComparison.OrdinalIgnoreCase)) return false;
            if (name.Equals("wasShowMenu", StringComparison.OrdinalIgnoreCase)) return false;
            if (name.Equals("settingsDirty", StringComparison.OrdinalIgnoreCase)) return false;
            if (name.StartsWith("is", StringComparison.OrdinalIgnoreCase)) return false;
            if (name.IndexOf("Wait", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (name.IndexOf("Editing", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (name.IndexOf("Dirty", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }

        private static string HumanizeFeatureName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                    sb.Append(' ');
                else if (i > 0 && char.IsUpper(c) && char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1]))
                    sb.Append(' ');
                sb.Append(i == 0 ? char.ToUpper(c) : c);
            }
            return sb.ToString();
        }

        private void DrawSearchTab()
        {
            EnsureSearchFeatures();

            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                GUILayout.BeginHorizontal();
                DrawMenuSectionHeader(L("SEARCH", "ПОИСК"));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(L("CLEAR", "ОЧИСТИТЬ"), btnStyle, GUILayout.Height(22), GUILayout.Width(90f)))
                    searchTabQuery = "";
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                if (searchFieldStyle == null)
                {
                    searchFieldStyle = new GUIStyle(GUI.skin.textField);
                    if (texInputBg != null)
                    {
                        searchFieldStyle.normal.background = texInputBg;
                        searchFieldStyle.focused.background = texInputBg;
                        searchFieldStyle.border = CreateRectOffset(6, 6, 6, 6);
                    }
                    searchFieldStyle.padding = CreateRectOffset(10, 10, 6, 6);
                    searchFieldStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f, 1f);
                    searchFieldStyle.fontSize = 14;
                }

                GUILayout.BeginHorizontal();
                searchTabQuery = GUILayout.TextField(searchTabQuery, searchFieldStyle, GUILayout.Height(28));
                if (GUILayout.Button(L("RESET", "СБРОС"), btnStyle, GUILayout.Height(28), GUILayout.Width(70f)))
                    searchTabQuery = "";
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                string[] filterLabels = { L("ALL", "ВСЕ"), L("ON", "ВКЛ"), L("OFF", "ВЫКЛ") };
                for (int i = 0; i < 3; i++)
                {
                    if (GUILayout.Button(filterLabels[i], i == searchFilterMode ? activeTabStyle : btnStyle, GUILayout.Height(22)))
                        searchFilterMode = i;
                    if (i < 2) GUILayout.Space(4);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                string q = (searchTabQuery ?? "").Trim().ToLowerInvariant();

                List<SearchableFeature> matches = new List<SearchableFeature>();
                foreach (SearchableFeature f in searchFeatures)
                {
                    bool v = (bool)f.field.GetValue(null);
                    if (searchFilterMode == 1 && !v) continue;
                    if (searchFilterMode == 2 && v) continue;
                    if (q.Length > 0 && !f.label.ToLowerInvariant().Contains(q) && !f.rawName.ToLowerInvariant().Contains(q)) continue;
                    matches.Add(f);
                }
                searchMatchCount = matches.Count;

                GUILayout.Label(L($"Matches: {searchMatchCount} / {searchFeatures.Count}", $"Совпадений: {searchMatchCount} / {searchFeatures.Count}"),
                    menuDescStyle, GUILayout.Height(16));
                GUILayout.Space(4);

                searchScroll = GUILayout.BeginScrollView(searchScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                try
                {
                    string currentCategory = null;
                    foreach (SearchableFeature f in matches)
                    {
                        if (currentCategory != f.category)
                        {
                            currentCategory = f.category;
                            GUILayout.Space(2);
                            GUILayout.Label(f.category, menuSectionTitleStyle, GUILayout.Height(18));
                        }

                        bool current = (bool)f.field.GetValue(null);
                        bool next = DrawToggle(current, f.label, 0);
                        if (next != current)
                        {
                            f.field.SetValue(null, next);
                            settingsDirty = true;
                        }
                    }

                    if (matches.Count == 0)
                    {
                        GUILayout.Label(L("No features match your search.", "Нет функций, соответствующих запросу."), menuDescStyle, GUILayout.Height(22));
                    }
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }
    }
}
