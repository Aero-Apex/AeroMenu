#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using UnityEngine;

namespace AeroMenu
{
    internal static class InvisibleSelf
    {
        internal static bool enabled = false;
        private static float nextCheck = 0f;

        internal static void Tick()
        {
            try
            {
                if (!enabled) return;

                PlayerControl lp = PlayerControl.LocalPlayer;
                if (lp == null || lp.Data == null || lp.Data.IsDead) return;
                if (MeetingHud.Instance != null) return;

                if (Time.unscaledTime < nextCheck) return;
                nextCheck = Time.unscaledTime + 1.5f;

                if (!lp.shouldAppearInvisible)
                    lp.SetRoleInvisibility(true, false, false);
            }
            catch { }
        }

        internal static void Restore()
        {
            try
            {
                PlayerControl lp = PlayerControl.LocalPlayer;
                if (lp == null || lp.Data == null || lp.Data.IsDead) return;
                if (lp.shouldAppearInvisible)
                    lp.SetRoleInvisibility(false, false, false);
            }
            catch { }
        }

        internal static string SetEnabled(bool on)
        {
            enabled = on;
            if (!on)
            {
                Restore();
                return "You are visible again.";
            }
            nextCheck = 0f;
            Tick();
            return "You are now invisible.";
        }
    }
}
