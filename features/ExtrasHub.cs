#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using UnityEngine;

namespace AeroMenu
{
    // Central dispatcher for the 60 optional "EXTRAS" features.
    // Each group lives in its own partial-class file so a single broken group
    // can never take down the whole menu or the game.
    public partial class AeroMenuGUI : MonoBehaviour
    {
        internal static void TickExtras(bool inputBlocked)
        {
            try { TickExtrasA(inputBlocked); } catch { }
            try { TickExtrasB(inputBlocked); } catch { }
            try { TickExtrasC(inputBlocked); } catch { }
            try { TickExtrasD(inputBlocked); } catch { }
            try { TickExtrasE(inputBlocked); } catch { }
            try { TickExtrasF(inputBlocked); } catch { }
            try { TickExtrasG(inputBlocked); } catch { }
            try { TickExtrasH(inputBlocked); } catch { }
            try { TickExtrasI(inputBlocked); } catch { }
            try { TickExtrasJ(inputBlocked); } catch { }
            try { TickExtrasK(inputBlocked); } catch { }
            try { TickExtrasL(inputBlocked); } catch { }
        }

        internal static void LoadExtras()
        {
            try { LoadExtrasA(); } catch { }
            try { LoadExtrasB(); } catch { }
            try { LoadExtrasC(); } catch { }
            try { LoadExtrasD(); } catch { }
            try { LoadExtrasE(); } catch { }
            try { LoadExtrasF(); } catch { }
            try { LoadExtrasG(); } catch { }
            try { LoadExtrasH(); } catch { }
            try { LoadExtrasI(); } catch { }
            try { LoadExtrasJ(); } catch { }
            try { LoadExtrasK(); } catch { }
            try { LoadExtrasL(); } catch { }
        }

        internal static void SaveExtras()
        {
            try { SaveExtrasA(); } catch { }
            try { SaveExtrasB(); } catch { }
            try { SaveExtrasC(); } catch { }
            try { SaveExtrasD(); } catch { }
            try { SaveExtrasE(); } catch { }
            try { SaveExtrasF(); } catch { }
            try { SaveExtrasG(); } catch { }
            try { SaveExtrasH(); } catch { }
            try { SaveExtrasI(); } catch { }
            try { SaveExtrasJ(); } catch { }
            try { SaveExtrasK(); } catch { }
            try { SaveExtrasL(); } catch { }
        }

        internal static void DrawExtrasOverlays()
        {
            if (Screen.width <= 1 || Screen.height <= 1) return;
            try { DrawExtrasAOverlay(); } catch { }
            try { DrawExtrasBOverlay(); } catch { }
            try { DrawExtrasCOverlay(); } catch { }
            try { DrawExtrasDOverlay(); } catch { }
            try { DrawExtrasEOverlay(); } catch { }
            try { DrawExtrasFOverlay(); } catch { }
            try { DrawExtrasGOverlay(); } catch { }
            try { DrawExtrasHOverlay(); } catch { }
            try { DrawExtrasIOverlay(); } catch { }
            try { DrawExtrasJOverlay(); } catch { }
            try { DrawExtrasKOverlay(); } catch { }
            try { DrawExtrasLOverlay(); } catch { }
        }
    }
}
