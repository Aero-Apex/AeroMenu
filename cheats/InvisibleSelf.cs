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
        private static bool networkedActive = false;

        internal static void Tick()
        {
            try
            {
                if (!enabled) return;

                PlayerControl lp = PlayerControl.LocalPlayer;
                if (lp == null || lp.Data == null || lp.Data.IsDead) return;
                if (MeetingHud.Instance != null) return;

                // server/vanilla ended the vanish (meeting, timeout, role change) -> re-request
                if (networkedActive && !lp.shouldAppearInvisible)
                    networkedActive = false;

                if (Time.unscaledTime < nextCheck) return;

                Apply(lp);
            }
            catch { }
        }

        internal static void ResetForNewRound()
        {
            networkedActive = false;
            nextCheck = 0f;
        }

        private static void Apply(PlayerControl lp)
        {
            bool amHost = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
            bool isPhantom = lp.Data != null && lp.Data.Role is PhantomRole;

            if (networkedActive)
            {
                // already vanished through the legit path; refresh local visuals only
                try { lp.SetRoleInvisibility(true, false, false); } catch { }
                return;
            }

            if (amHost)
            {
                try { lp.RpcVanish(); networkedActive = true; }
                catch { }
            }
            else if (isPhantom)
            {
                try { lp.CmdCheckVanish(3600f); networkedActive = true; }
                catch { }
            }

            // local visuals always (no network side effects)
            try { lp.SetRoleInvisibility(true, false, false); } catch { }

            // retry the networked path periodically (e.g. after role reveal / game start)
            nextCheck = Time.unscaledTime + (networkedActive ? 8f : 3f);
        }

        private static void Unapply(PlayerControl lp)
        {
            bool amHost = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

            if (networkedActive)
            {
                if (amHost)
                {
                    try { lp.RpcAppear(false); } catch { }
                }
                else
                {
                    try { lp.CmdCheckAppear(false); } catch { }
                }
            }
            networkedActive = false;

            try { lp.SetRoleInvisibility(false, false, false); } catch { }
        }

        internal static void Restore()
        {
            try
            {
                PlayerControl lp = PlayerControl.LocalPlayer;
                if (lp == null || lp.Data == null || lp.Data.IsDead) return;
                if (lp.shouldAppearInvisible || networkedActive) Unapply(lp);
            }
            catch { }
            networkedActive = false;
        }

        internal static string SetEnabled(bool on)
        {
            enabled = on;
            networkedActive = false;
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
