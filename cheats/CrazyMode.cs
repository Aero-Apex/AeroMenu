#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using InnerNet;
using UnityEngine;

namespace AeroMenu
{
    internal static class CrazyMode
    {
        internal static bool enabled = false;
        private static float nextAt = 0f;
        private static int lastIdx = -1;

        private static readonly SystemTypes[] chaosSabos =
        {
            SystemTypes.Reactor,
            SystemTypes.Laboratory,
            SystemTypes.HeliSabotage,
            SystemTypes.LifeSupp,
            SystemTypes.Comms
        };

        internal static void Tick()
        {
            try
            {
                if (!enabled) return;
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
                if (ShipStatus.Instance == null) return;
                if (MeetingHud.Instance != null || ExileController.Instance != null) return;
                if (Time.unscaledTime < nextAt) return;
                nextAt = Time.unscaledTime + 2.5f;

                int pool = chaosSabos.Length + 2; // extra slots: lights + mushroom mixup
                int idx = UnityEngine.Random.Range(0, pool);
                if (idx == lastIdx) idx = (idx + 1) % pool;
                lastIdx = idx;

                if (idx == chaosSabos.Length)
                {
                    // lights: random broken switches
                    byte b = 4;
                    for (int i = 0; i < 5; i++) if (UnityEngine.Random.value > 0.5f) b |= (byte)(1 << i);
                    try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(b | 128)); } catch { }
                }
                else if (idx == chaosSabos.Length + 1)
                {
                    try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 0); } catch { }
                }
                else
                {
                    try { ShipStatus.Instance.RpcUpdateSystem(chaosSabos[idx], 128); } catch { }
                }

                // slam every airlock for maximum chaos
                var doors = ShipStatus.Instance.AllDoors;
                if (doors != null)
                {
                    foreach (var door in doors)
                    {
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id); } catch { }
                    }
                }
            }
            catch { }
        }

        internal static string SetEnabled(bool on)
        {
            enabled = on;
            nextAt = 0f;
            lastIdx = -1;
            return on ? "CRAZY MODE ENGAGED!" : "Crazy Mode off.";
        }

        internal static void ResetForNewRound()
        {
            nextAt = 0f;
            lastIdx = -1;
        }
    }
}
