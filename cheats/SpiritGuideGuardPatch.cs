#nullable disable
using HarmonyLib;
using UnityEngine;

namespace AeroMenu
{
    // SpiritGuideRole.FixedUpdate reads SpriteRenderer.flipX from renderers that
    // can be destroyed (target left / respawned, camera parts torn down after a
    // meeting). The resulting NullReferenceException fires every physics tick and
    // each one is logged with a stack trace and sent through Sentry. When a frame
    // gets slower, more FixedUpdates run per frame, which throws more exceptions:
    // a frame-rate death spiral. Skip the tick while any of those renderers is gone.
    [HarmonyPatch(typeof(SpiritGuideRole), nameof(SpiritGuideRole.FixedUpdate))]
    public static class SpiritGuideGuardPatch
    {
        public static bool Prefix(SpiritGuideRole __instance)
        {
            try
            {
                if (__instance == null) return false;
                if (__instance.cameraHandSpriteRenderer == null) return false;

                var parts = __instance.cameraPartsRenderer;
                if (parts != null)
                {
                    for (int i = 0; i < parts.Length; i++)
                        if (parts[i] == null) return false;
                }

                if (!BodyOk(__instance.Player)) return false;
                if (__instance.currentTarget != null && !BodyOk(__instance.currentTarget)) return false;
                if (__instance.hauntTarget != null && !BodyOk(__instance.hauntTarget)) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool BodyOk(PlayerControl pc)
        {
            if (pc == null) return false;
            CosmeticsLayer c = pc.cosmetics;
            if (c == null) return false;
            var body = c.currentBodySprite;
            return body != null && body.BodySprite != null;
        }
    }
}
