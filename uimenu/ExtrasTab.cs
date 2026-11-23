#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private static readonly System.Collections.Generic.HashSet<string> extrasSectionErrorsLogged = new System.Collections.Generic.HashSet<string>();

        // Each EXTRAS group is isolated: if one throws, its layout group is still
        // closed and the remaining groups keep drawing.
        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        private void DrawExtrasSection(string id, System.Action<float> draw, float width)
        {
            DrawCulled("extras" + id, () =>
            {
                try { draw(width); }
                catch (System.Exception ex)
                {
                    if (extrasSectionErrorsLogged.Add(id + ex.GetType().Name))
                        Plugin.Instance?.Log?.LogError("[EXTRAS] section " + id + " failed: " + ex);
                }
            });
        }

        private void DrawExtrasTab()
        {
            float width = Mathf.Max(180f, GetMenuWorkWidth(220f, 610f) - 18f);

            // Outer card padding (32) + nested section card padding (32).
            float innerWidth = Mathf.Max(150f, width - 72f);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(width));
            try
            {
                DrawMenuSectionHeader(L("EXTRAS - 60 FEATURES", "ДОП. ФУНКЦИИ - 60"));
                GUILayout.Label(L("All options are saved automatically. Local features do not affect other players; host features require host permissions.", "Все настройки сохраняются автоматически. Локальные функции не влияют на других игроков; функции хоста требуют прав хоста."), menuDescStyle);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("1-5  VISUAL OVERLAYS", "1-5  ВИЗУАЛЬНЫЕ ОВЕРЛЕИ"));
                DrawExtrasSection("A", DrawExtrasA, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("6-10  INFO PANELS", "6-10  ИНФОРМАЦИОННЫЕ ПАНЕЛИ"));
                DrawExtrasSection("B", DrawExtrasB, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("11-15  LOCAL TWEAKS", "11-15  ЛОКАЛЬНЫЕ НАСТРОЙКИ"));
                DrawExtrasSection("C", DrawExtrasC, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("16-20  HOST TOOLS", "16-20  ИНСТРУМЕНТЫ ХОСТА"));
                DrawExtrasSection("D", DrawExtrasD, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("21-25  PLAYER TOOLS", "21-25  ИНСТРУМЕНТЫ ИГРОКА"));
                DrawExtrasSection("E", DrawExtrasE, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("26-30  UTILITY", "26-30  УТИЛИТЫ"));
                DrawExtrasSection("F", DrawExtrasF, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("31-35  PANELS", "31-35  ПАНЕЛИ"));
                DrawExtrasSection("G", DrawExtrasG, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("36-40  MEETING TOOLS", "36-40  СОБРАНИЯ"));
                DrawExtrasSection("H", DrawExtrasH, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("41-45  LOBBY TOOLS", "41-45  ЛОББИ"));
                DrawExtrasSection("I", DrawExtrasI, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("46-50  COSMETICS (LOCAL)", "46-50  КОСМЕТИКА (ЛОКАЛЬНО)"));
                DrawExtrasSection("J", DrawExtrasJ, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("51-55  DIAGNOSTICS", "51-55  ДИАГНОСТИКА"));
                DrawExtrasSection("K", DrawExtrasK, innerWidth);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("56-60  AUTOMATION", "56-60  АВТОМАТИЗАЦИЯ"));
                DrawExtrasSection("L", DrawExtrasL, innerWidth);
                GUILayout.Space(10);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }
    }
}
