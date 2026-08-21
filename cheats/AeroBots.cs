#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AeroMenu
{
    internal static class AeroBots
    {
        internal sealed class Bot
        {
            public NetworkedPlayerInfo Info;
            public string Name;
            public int ColorId;
        }

        private static readonly List<Bot> bots = new List<Bot>();
        private static readonly HashSet<uint> botNetIds = new HashSet<uint>();
        internal static int Count => bots.Count;

        private static readonly string[] botNames = new string[]
        {
            "Aero", "Vortex", "Nova", "Blitz", "Ghosty", "Shadow", "Pixel", "Rogue",
            "Echo", "Frost", "Titan", "Zephyr", "Onyx", "Comet", "Drift", "Havoc",
            "Jinx", "Krypto", "Lynx", "Mystic", "Nimbus", "Orbit", "Phantom", "Quasar",
            "Raven", "Storm", "Turbo", "Umbra", "Viper", "Wraith"
        };

        internal static bool IsBot(PlayerControl pc)
        {
            if (pc == null) return false;
            try { if (botNetIds.Contains(pc.NetId)) return true; } catch { }
            return false;
        }

        private static byte NextFreeId()
        {
            HashSet<int> used = new HashSet<int>();
            try { foreach (NetworkedPlayerInfo pi in GameData.Instance.AllPlayers) if (pi != null) used.Add(pi.PlayerId); } catch { }
            for (byte i = 0; i < 100; i++) if (!used.Contains(i)) return i;
            return 255;
        }

        private static int NextFreeColor()
        {
            try
            {
                List<int> free = AeroMenuGUI.GetFreeColorIds();
                if (free != null && free.Count > 0) return free[UnityEngine.Random.Range(0, free.Count)];
            }
            catch { }
            return UnityEngine.Random.Range(0, Palette.PlayerColors.Length);
        }

        internal static string AddBots(int n)
        {
            if (!NetworkedClones.Ready()) return "Host only.";
            AmongUsClient net = AmongUsClient.Instance;
            if (net == null || net.PlayerPrefab == null) return "No prefab.";
            int added = 0;
            for (int k = 0; k < n; k++)
            {
                try { if (MakeBot(net)) added++; } catch { }
            }
            return added > 0 ? "Bots added: " + added : "Failed to add bots.";
        }

        private static bool MakeBot(AmongUsClient net)
        {
            byte id = NextFreeId();
            if (id >= 100) return false;

            PlayerControl prefab = net.PlayerPrefab;
            Vector3 basePos = PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.transform.position : Vector3.zero;
            Vector3 pos = basePos + new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), UnityEngine.Random.Range(-1.5f, 1.5f), 0f);

            bool wasActive = prefab.gameObject.activeSelf;
            prefab.gameObject.SetActive(false);
            PlayerControl bot = Object.Instantiate(prefab);
            try
            {
                bot.PlayerId = id;
                bot.transform.position = pos;
                bot.gameObject.SetActive(true);
                prefab.gameObject.SetActive(wasActive);

                net.Spawn(bot.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                bot.transform.position = pos;
                try { if (bot.NetTransform != null) bot.NetTransform.SnapTo(new Vector2(pos.x, pos.y)); } catch { }
                if (bot.Collider != null) bot.Collider.enabled = false;
                if (bot.MyPhysics != null) bot.MyPhysics.enabled = false;

                NetworkedPlayerInfo info = TryAddPlayer(bot);

                Bot b = new Bot { Name = botNames[UnityEngine.Random.Range(0, botNames.Length)], ColorId = NextFreeColor(), Info = info };
                ApplyProfile(b);                bots.Add(b);
                try { botNetIds.Add(bot.NetId); } catch { }
                return true;
            }
            catch
            {
                try { prefab.gameObject.SetActive(wasActive); } catch { }
                try { Object.Destroy(bot.gameObject); } catch { }
                return false;
            }
        }

        private static NetworkedPlayerInfo TryAddPlayer(PlayerControl pc)
        {
            try
            {
                ClientData cd = PlayerControl.LocalPlayer != null && AmongUsClient.Instance != null
                    ? AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer)
                    : null;
                return GameData.Instance.AddPlayer(pc, cd);
            }
            catch { }
            try
            {
                MethodInfo m = typeof(GameData).GetMethod("AddPlayer", new[] { typeof(PlayerControl) });
                if (m != null) return m.Invoke(GameData.Instance, new object[] { pc }) as NetworkedPlayerInfo;
            }
            catch { }
            return null;
        }

        private static void SetInfoObject(NetworkedPlayerInfo info, PlayerControl pc)
        {
            try
            {
                PropertyInfo p = typeof(NetworkedPlayerInfo).GetProperty("Object");
                if (p != null && p.CanWrite) { p.SetValue(info, pc); return; }
                FieldInfo f = typeof(NetworkedPlayerInfo).GetField("Object", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) { f.SetValue(info, pc); return; }
                MethodInfo m = typeof(NetworkedPlayerInfo).GetMethod("set_Object", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null) m.Invoke(info, new object[] { pc });
            }
            catch { }
        }

        private static void ApplyProfile(Bot b)
        {
            try
            {
                if (b.Info == null) return;
                b.Info.PlayerName = b.Name;
                if (b.Info.DefaultOutfit != null) b.Info.DefaultOutfit.ColorId = b.ColorId;
                if (b.Info.Object != null) b.Info.Object.SetOutfit(b.Info.DefaultOutfit, PlayerOutfitType.Default);
            }
            catch { }
        }

        internal static void ClearAllBots()
        {
            int n = bots.Count;
            foreach (Bot b in bots)
            {
                try
                {
                    if (b.Info != null)
                    {
                        try
                        {
                            PlayerControl obj = b.Info.Object;
                            if (obj != null)
                            {
                                InnerNetObject ino = obj.Cast<InnerNetObject>();
                                AmongUsClient net = AmongUsClient.Instance;
                                if (net != null && ino != null) net.Despawn(ino);
                                if (obj.gameObject != null) Object.Destroy(obj.gameObject);
                            }
                        }
                        catch { }
                        try { GameData.Instance.RemovePlayer(b.Info.PlayerId); } catch { }
                    }
                }
                catch { }
            }
            bots.Clear();
            botNetIds.Clear();
            if (n > 0) AeroMenuGUI.ShowNotification("<color=#FFAA55>[BOTS]</color> Removed " + n + " bots.");
        }

        [HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
        internal static class AeroBotsGameStartPatch
        {
            internal static void Prefix() => EnsureInGame();
        }

        internal static void EnsureInGame()
        {
            try
            {
                if (bots.Count == 0) return;
                AmongUsClient net = AmongUsClient.Instance;
                if (net == null || !net.AmHost || net.PlayerPrefab == null || GameData.Instance == null) return;

                foreach (Bot b in bots)
                {
                    try
                    {
                        bool hasEntry = false;
                        try { hasEntry = b.Info != null && GameData.Instance.AllPlayers.Contains(b.Info); } catch { }

                        if (!hasEntry)
                        {
                            byte id = NextFreeId();
                            if (id >= 100) continue;
                            PlayerControl prefab = net.PlayerPrefab;
                            Vector3 basePos = PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.transform.position : Vector3.zero;
                            Vector3 pos = basePos + new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-1f, 1f), 0f);
                            bool wasActive = prefab.gameObject.activeSelf;
                            prefab.gameObject.SetActive(false);
                            PlayerControl bot = Object.Instantiate(prefab);
                            try
                            {
                                bot.PlayerId = id;
                                bot.transform.position = pos;
                                bot.gameObject.SetActive(true);
                                prefab.gameObject.SetActive(wasActive);
                                net.Spawn(bot.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                                bot.transform.position = pos;
                                if (bot.Collider != null) bot.Collider.enabled = false;
                                if (bot.MyPhysics != null) bot.MyPhysics.enabled = false;
                                b.Info = TryAddPlayer(bot);
                                ApplyProfile(b);
                                try { botNetIds.Add(bot.NetId); } catch { }
                            }
                            catch
                            {
                                try { prefab.gameObject.SetActive(wasActive); } catch { }
                                try { Object.Destroy(bot.gameObject); } catch { }
                            }
                            continue;
                        }

                        NetworkedPlayerInfo info = b.Info;
                        bool needsBody = false;
                        try { needsBody = info.Object == null || !info.Object.gameObject.activeInHierarchy; } catch { needsBody = true; }

                        if (needsBody)
                        {
                            PlayerControl prefab = net.PlayerPrefab;
                            Vector3 basePos = PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.transform.position : Vector3.zero;
                            Vector3 pos = basePos + new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-1f, 1f), 0f);
                            bool wasActive = prefab.gameObject.activeSelf;
                            prefab.gameObject.SetActive(false);
                            PlayerControl bot = Object.Instantiate(prefab);
                            try
                            {
                                bot.PlayerId = info.PlayerId;
                                bot.transform.position = pos;
                                bot.gameObject.SetActive(true);
                                prefab.gameObject.SetActive(wasActive);
                                net.Spawn(bot.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                                bot.transform.position = pos;
                                if (bot.Collider != null) bot.Collider.enabled = false;
                                if (bot.MyPhysics != null) bot.MyPhysics.enabled = false;
                                try { SetInfoObject(info, bot); } catch { }
                                ApplyProfile(b);
                                try { botNetIds.Add(bot.NetId); } catch { }
                            }
                            catch
                            {
                                try { prefab.gameObject.SetActive(wasActive); } catch { }
                                try { Object.Destroy(bot.gameObject); } catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
