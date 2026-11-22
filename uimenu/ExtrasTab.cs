#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private void DrawExtrasTab()
        {
            float width = Mathf.Max(180f, GetMenuWorkWidth(220f, 610f) - 18f);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(width));
            try
            {
                DrawMenuSectionHeader(L("EXTRAS - 60 FEATURES", "ДОП. ФУНКЦИИ - 60"));
                GUILayout.Label(L("All options are saved automatically. Local features do not affect other players; host features require host permissions.", "Все настройки сохраняются автоматически. Локальные функции не влияют на других игроков; функции хоста требуют прав хоста."), menuDescStyle);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("1-5  VISUAL OVERLAYS", "1-5  ВИЗУАЛЬНЫЕ ОВЕРЛЕИ"));
                DrawExtrasA(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("6-10  INFO PANELS", "6-10  ИНФОРМАЦИОННЫЕ ПАНЕЛИ"));
                DrawExtrasB(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("11-15  LOCAL TWEAKS", "11-15  ЛОКАЛЬНЫЕ НАСТРОЙКИ"));
                DrawExtrasC(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("16-20  HOST TOOLS", "16-20  ИНСТРУМЕНТЫ ХОСТА"));
                DrawExtrasD(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("21-25  PLAYER TOOLS", "21-25  ИНСТРУМЕНТЫ ИГРОКА"));
                DrawExtrasE(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("26-30  UTILITY", "26-30  УТИЛИТЫ"));
                DrawExtrasF(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("31-35  PANELS", "31-35  ПАНЕЛИ"));
                DrawExtrasG(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("36-40  MEETING TOOLS", "36-40  СОБРАНИЯ"));
                DrawExtrasH(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("41-45  LOBBY TOOLS", "41-45  ЛОББИ"));
                DrawExtrasI(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("46-50  COSMETICS (LOCAL)", "46-50  КОСМЕТИКА (ЛОКАЛЬНО)"));
                DrawExtrasJ(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("51-55  DIAGNOSTICS", "51-55  ДИАГНОСТИКА"));
                DrawExtrasK(width);
                GUILayout.Space(10);

                DrawMenuSectionHeader(L("56-60  AUTOMATION", "56-60  АВТОМАТИЗАЦИЯ"));
                DrawExtrasL(width);
                GUILayout.Space(10);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }
    }
}
