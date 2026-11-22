#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using UnityEngine;

namespace AeroMenu
{
    // Dispatcher for the 50 PRO features. Each group is isolated so a single
    // failing group cannot break the menu or the game.
    public partial class AeroMenuGUI : MonoBehaviour
    {
        internal static void TickPro(bool inputBlocked)
        {
            try { TickProM(inputBlocked); } catch { }
            try { TickProN(inputBlocked); } catch { }
            try { TickProO(inputBlocked); } catch { }
            try { TickProP(inputBlocked); } catch { }
            try { TickProQ(inputBlocked); } catch { }
            try { TickProR(inputBlocked); } catch { }
            try { TickProS(inputBlocked); } catch { }
            try { TickProT(inputBlocked); } catch { }
            try { TickProU(inputBlocked); } catch { }
            try { TickProV(inputBlocked); } catch { }
        }

        internal static void LoadPro()
        {
            try { LoadProM(); } catch { }
            try { LoadProN(); } catch { }
            try { LoadProO(); } catch { }
            try { LoadProP(); } catch { }
            try { LoadProQ(); } catch { }
            try { LoadProR(); } catch { }
            try { LoadProS(); } catch { }
            try { LoadProT(); } catch { }
            try { LoadProU(); } catch { }
            try { LoadProV(); } catch { }
        }

        internal static void SavePro()
        {
            try { SaveProM(); } catch { }
            try { SaveProN(); } catch { }
            try { SaveProO(); } catch { }
            try { SaveProP(); } catch { }
            try { SaveProQ(); } catch { }
            try { SaveProR(); } catch { }
            try { SaveProS(); } catch { }
            try { SaveProT(); } catch { }
            try { SaveProU(); } catch { }
            try { SaveProV(); } catch { }
        }

        internal static void DrawProOverlays()
        {
            if (Screen.width <= 1 || Screen.height <= 1) return;
            try { DrawProMOverlay(); } catch { }
            try { DrawProNOverlay(); } catch { }
            try { DrawProOOverlay(); } catch { }
            try { DrawProPOverlay(); } catch { }
            try { DrawProQOverlay(); } catch { }
            try { DrawProROverlay(); } catch { }
            try { DrawProSOverlay(); } catch { }
            try { DrawProTOverlay(); } catch { }
            try { DrawProUOverlay(); } catch { }
            try { DrawProVOverlay(); } catch { }
        }
    }
}
