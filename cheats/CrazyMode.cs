#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
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
            if (!on) RepairAllChaos();
            return on ? "CRAZY MODE ENGAGED!" : "Crazy Mode off — everything repaired.";
        }

        internal static void ResetForNewRound()
        {
            nextAt = 0f;
            lastIdx = -1;
        }

        internal static void RepairAllChaos()
        {
            try
            {
                ShipStatus ss = ShipStatus.Instance;
                if (ss == null) return;

                try { ss.RpcUpdateSystem(SystemTypes.Reactor, 16); } catch { }
                try { ss.RpcUpdateSystem(SystemTypes.Laboratory, 16); } catch { }
                try { ss.RpcUpdateSystem(SystemTypes.HeliSabotage, 16); } catch { }
                try { ss.RpcUpdateSystem(SystemTypes.HeliSabotage, 17); } catch { }
                try { ss.RpcUpdateSystem(SystemTypes.LifeSupp, 16); } catch { }
                try { ss.RpcUpdateSystem(SystemTypes.Comms, 16); } catch { }
                try { ss.RpcUpdateSystem(SystemTypes.Comms, 17); } catch { }

                // lights: flip every mismatched switch back
                try
                {
                    if (ss.Systems != null && ss.Systems.ContainsKey(SystemTypes.Electrical))
                    {
                        SwitchSystem sys = ss.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                        if (sys != null)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                bool expected = (sys.ExpectedSwitches & (1 << i)) != 0;
                                bool actual = (sys.ActualSwitches & (1 << i)) != 0;
                                if (expected != actual) ss.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                            }
                        }
                    }
                }
                catch { }

                try { ss.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 0); } catch { }

                // open every door again
                var doors = ss.AllDoors;
                if (doors != null)
                {
                    foreach (var d in doors)
                    {
                        try { ss.RpcUpdateSystem(SystemTypes.Doors, (byte)(d.Id | 64)); } catch { }
                    }
                }
            }
            catch { }
        }

        internal static string ScatterPlayers()
        {
            try
            {
                AmongUsClient net = AmongUsClient.Instance;
                if (net == null || !net.AmHost)
                    return "Scattering others needs host. Sabotage chaos still works!";
                if (net.GameState != InnerNetClient.GameStates.Started || ShipStatus.Instance == null)
                    return "Only during a match.";
                if (MeetingHud.Instance != null) return "Not during meetings.";

                List<PlayerControl> alive = new List<PlayerControl>();
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (p == null || p.Data == null || p.Data.IsDead || p.Data.Role == null) continue;
                    if (NetworkedClones.IsClone(p)) continue;
                    alive.Add(p);
                }
                if (alive.Count < 2) return "Not enough players.";

                foreach (PlayerControl p in alive)
                {
                    PlayerControl anchor = alive[UnityEngine.Random.Range(0, alive.Count)];
                    Vector2 pos = (Vector2)anchor.transform.position + new Vector2(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-4f, 4f));
                    try { if (p.NetTransform != null) p.NetTransform.RpcSnapTo(pos); } catch { }
                }
                return "Scattered " + alive.Count + " players!";
            }
            catch { return "Scatter failed."; }
        }
    }
}
