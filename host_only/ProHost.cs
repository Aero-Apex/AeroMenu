#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using InnerNet;
using AmongUs.GameOptions;
using BepInEx;
using HarmonyLib;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Group P state (exact field names required by the contract)
        // --------------------------------------------------------------------
        public static bool proSkipCountdown = false;
        public static bool proAutobanVoters = false;
        public static bool proLobbyLock = false;
        public static bool proSpectatorMode = false;
        public static bool proGamePreset = false;
        public static int proGamePresetIndex = 0;

        // --------------------------------------------------------------------
        // Internal bookkeeping (every private helper/field ends in P)
        // --------------------------------------------------------------------
        private const BindingFlags AllMembersP = BindingFlags.Public | BindingFlags.NonPublic |
                                                 BindingFlags.Instance | BindingFlags.Static;

        private static float nextSkipCountdownP;
        private static float nextAutobanP;
        private static float nextLockCheckP;

        private static int autobanGameIdP = -1;
        private static object autobanHudP;
        private static readonly HashSet<byte> autobanBannedP = new HashSet<byte>();

        private static int lockGameIdP = -1;
        private static bool lockPrevP;
        private static readonly HashSet<int> lockBaselineP = new HashSet<int>();
        private static readonly HashSet<int> lockKickedP = new HashSet<int>();

        private static int spectatorGameIdP = -1;
        private static bool spectatorAppliedP;

        // Five game-option presets applied reflectively by the button below.
        private static readonly KeyValuePair<string, object>[][] GamePresetsP =
        {
            new[] // 0 - Vanilla
            {
                new KeyValuePair<string, object>("NumImpostors", 1),
                new KeyValuePair<string, object>("KillCooldown", 30f),
                new KeyValuePair<string, object>("PlayerSpeedMod", 1f),
                new KeyValuePair<string, object>("CrewmateVision", 1f),
                new KeyValuePair<string, object>("ImpostorVision", 1.5f),
                new KeyValuePair<string, object>("VotingTime", 120f),
                new KeyValuePair<string, object>("DiscussionTime", 15f),
                new KeyValuePair<string, object>("NumCommonTasks", 1),
                new KeyValuePair<string, object>("NumLongTasks", 1),
                new KeyValuePair<string, object>("NumShortTasks", 2)
            },
            new[] // 1 - Fast
            {
                new KeyValuePair<string, object>("NumImpostors", 2),
                new KeyValuePair<string, object>("KillCooldown", 5f),
                new KeyValuePair<string, object>("PlayerSpeedMod", 2f),
                new KeyValuePair<string, object>("CrewmateVision", 1f),
                new KeyValuePair<string, object>("ImpostorVision", 2f),
                new KeyValuePair<string, object>("VotingTime", 30f),
                new KeyValuePair<string, object>("DiscussionTime", 10f),
                new KeyValuePair<string, object>("NumCommonTasks", 0),
                new KeyValuePair<string, object>("NumLongTasks", 0),
                new KeyValuePair<string, object>("NumShortTasks", 1)
            },
            new[] // 2 - Impostor favoured
            {
                new KeyValuePair<string, object>("NumImpostors", 3),
                new KeyValuePair<string, object>("KillCooldown", 12f),
                new KeyValuePair<string, object>("PlayerSpeedMod", 1.25f),
                new KeyValuePair<string, object>("CrewmateVision", 0.5f),
                new KeyValuePair<string, object>("ImpostorVision", 2f),
                new KeyValuePair<string, object>("VotingTime", 60f),
                new KeyValuePair<string, object>("NumEmergencyMeetings", 0),
                new KeyValuePair<string, object>("NumCommonTasks", 1),
                new KeyValuePair<string, object>("NumLongTasks", 1),
                new KeyValuePair<string, object>("NumShortTasks", 2)
            },
            new[] // 3 - Crewmate favoured
            {
                new KeyValuePair<string, object>("NumImpostors", 1),
                new KeyValuePair<string, object>("KillCooldown", 45f),
                new KeyValuePair<string, object>("PlayerSpeedMod", 1f),
                new KeyValuePair<string, object>("CrewmateVision", 2f),
                new KeyValuePair<string, object>("ImpostorVision", 1f),
                new KeyValuePair<string, object>("VotingTime", 120f),
                new KeyValuePair<string, object>("NumCommonTasks", 2),
                new KeyValuePair<string, object>("NumLongTasks", 2),
                new KeyValuePair<string, object>("NumShortTasks", 3)
            },
            new[] // 4 - Chaos
            {
                new KeyValuePair<string, object>("NumImpostors", 3),
                new KeyValuePair<string, object>("KillCooldown", 2f),
                new KeyValuePair<string, object>("PlayerSpeedMod", 3f),
                new KeyValuePair<string, object>("CrewmateVision", 0.25f),
                new KeyValuePair<string, object>("ImpostorVision", 3f),
                new KeyValuePair<string, object>("VotingTime", 20f),
                new KeyValuePair<string, object>("NumCommonTasks", 0),
                new KeyValuePair<string, object>("NumLongTasks", 0),
                new KeyValuePair<string, object>("NumShortTasks", 4)
            }
        };

        private static readonly string[] GamePresetNamesP =
        {
            "Vanilla", "Fast", "Impostor", "Crewmate", "Chaos"
        };

        // --------------------------------------------------------------------
        // Required public surface
        // --------------------------------------------------------------------

        internal void DrawProP(float width)
        {
            try
            {
                float w = width > 10f ? width : GetMenuWorkWidth(220f, 620f);
                int toggleWidth = Mathf.RoundToInt(Mathf.Clamp(w * 0.6f, 150f, 320f));
                int inputWidth = Mathf.RoundToInt(Mathf.Clamp(w * 0.22f, 60f, 120f));

                GUILayout.BeginVertical(menuCardStyle);
                try
                {
                    GUILayout.Label(MenuText(L("HOST PRO", "ПРО ДЛЯ ХОСТА")), headerStyle);

                    bool host = IsHostP();
                    GUILayout.Label(MenuText(host
                        ? L("Host rights: active", "Права хоста: активны")
                        : L("Host rights: inactive", "Права хоста: неактивны")), menuDescStyle);
                    GUILayout.Space(6);

                    proSkipCountdown = DrawToggle(proSkipCountdown,
                        L("Skip lobby countdown", "Пропустить отсчёт лобби"), toggleWidth);
                    GUILayout.Label(MenuText(L("Forces the lobby countdown to finish immediately.",
                        "Заставляет отсчёт лобби закончиться мгновенно.")), menuDescStyle);
                    GUILayout.Space(6);

                    proAutobanVoters = DrawToggle(proAutobanVoters,
                        L("Autoban team voters", "Автобан за голос за команду"), toggleWidth);
                    GUILayout.Label(MenuText(L("Bans players who vote for a teammate during a meeting.",
                        "Банит игроков, голосующих за союзника на собрании.")), menuDescStyle);
                    GUILayout.Space(6);

                    proLobbyLock = DrawToggle(proLobbyLock,
                        L("Lobby lock", "Блокировка лобби"), toggleWidth);
                    GUILayout.Label(MenuText(L("Kicks anyone who joins after the lock is enabled.",
                        "Кикает всех, кто зашёл после включения блокировки.")), menuDescStyle);
                    GUILayout.Space(6);

                    proSpectatorMode = DrawToggle(proSpectatorMode,
                        L("Spectator mode", "Режим наблюдателя"), toggleWidth);
                    GUILayout.Label(MenuText(L("Forces the local player into a ghost / spectator state.",
                        "Переводит локального игрока в состояние призрака.")), menuDescStyle);
                    GUILayout.Space(6);

                    proGamePreset = DrawToggle(proGamePreset,
                        L("Game preset", "Пресет игры"), toggleWidth);
                    GUILayout.BeginHorizontal();
                    try
                    {
                        GUILayout.Label(MenuText(L("Preset:", "Пресет:")), toggleLabelStyle,
                            GUILayout.Width(inputWidth));
                        int shown = Mathf.Clamp(proGamePresetIndex, 0, GamePresetsP.Length - 1) + 1;
                        string presetText = GUILayout.TextField(shown.ToString(), inputBlockStyle,
                            GUILayout.Width(inputWidth));
                        if (int.TryParse(presetText, out int parsed))
                        {
                            parsed = Mathf.Clamp(parsed, 1, GamePresetsP.Length);
                            proGamePresetIndex = parsed - 1;
                        }
                    }
                    finally { GUILayout.EndHorizontal(); }

                    if (GUILayout.Button(MenuText(L("Apply Preset", "Применить пресет")),
                            btnStyle, GUILayout.Height(24)))
                        ApplyGamePresetP();

                    string name = GamePresetNamesP[Mathf.Clamp(proGamePresetIndex, 0, GamePresetNamesP.Length - 1)];
                    GUILayout.Label(MenuText(L("Selected: ", "Выбран: ") + name +
                        L(" — host applies via reflection.", " — хост применяет через рефлексию.")), menuDescStyle);
                }
                catch { }
                finally { GUILayout.EndVertical(); }
            }
            catch { }
        }

        internal static void TickProP(bool inputBlocked)
        {
            // This group has no hotkeys, so inputBlocked is intentionally unused.
            try
            {
                if (!IsHostP())
                {
                    ResetP();
                    return;
                }

                TickSkipCountdownP();
                TickAutobanVotersP();
                TickLobbyLockP();
                TickSpectatorModeP();
            }
            catch { }
        }

        internal static void LoadProP()
        {
            try
            {
                proSkipCountdown = LoadBool("M_ProP_proSkipCountdown", false);
                proAutobanVoters = LoadBool("M_ProP_proAutobanVoters", false);
                proLobbyLock = LoadBool("M_ProP_proLobbyLock", false);
                proSpectatorMode = LoadBool("M_ProP_proSpectatorMode", false);
                proGamePreset = LoadBool("M_ProP_proGamePreset", false);
                proGamePresetIndex = PlayerPrefs.GetInt("M_ProP_proGamePresetIndex", 0);
                proGamePresetIndex = Mathf.Clamp(proGamePresetIndex, 0, GamePresetsP.Length - 1);
            }
            catch { }
        }

        internal static void SaveProP()
        {
            try
            {
                SaveBool("M_ProP_proSkipCountdown", proSkipCountdown);
                SaveBool("M_ProP_proAutobanVoters", proAutobanVoters);
                SaveBool("M_ProP_proLobbyLock", proLobbyLock);
                SaveBool("M_ProP_proSpectatorMode", proSpectatorMode);
                SaveBool("M_ProP_proGamePreset", proGamePreset);
                PlayerPrefs.SetInt("M_ProP_proGamePresetIndex",
                    Mathf.Clamp(proGamePresetIndex, 0, GamePresetsP.Length - 1));
            }
            catch { }
        }

        internal static void DrawProPOverlay()
        {
            // No overlay elements in this group.
        }

        // --------------------------------------------------------------------
        // Feature ticks (all throttled, all guarded)
        // --------------------------------------------------------------------

        private static void TickSkipCountdownP()
        {
            if (!proSkipCountdown) return;

            float now = Time.unscaledTime;
            if (now < nextSkipCountdownP) return;
            nextSkipCountdownP = now + 0.1f;

            try
            {
                if (GetStaticMemberP(typeof(LobbyBehaviour), "Instance") == null) return;

                object gsm = GetGameStartManagerP();
                if (gsm == null) return;

                // Only shorten a countdown that is already running; never
                // force a lobby from idle into a match on our own.
                object state = GetValueP(gsm, "startState");
                if (state == null ||
                    state.ToString().IndexOf("Countdown", StringComparison.OrdinalIgnoreCase) < 0)
                    return;

                TrySetMemberP(gsm, 0f, "countDownTimer", "countdownTimer", "countDown");
            }
            catch { }
        }

        private static void TickAutobanVotersP()
        {
            if (!proAutobanVoters) return;

            float now = Time.unscaledTime;
            if (now < nextAutobanP) return;
            nextAutobanP = now + 0.5f;

            try
            {
                int gameId = GetGameIdP();
                if (gameId != autobanGameIdP)
                {
                    autobanGameIdP = gameId;
                    autobanHudP = null;
                    autobanBannedP.Clear();
                }

                object hud = GetStaticMemberP(typeof(MeetingHud), "Instance");
                if (hud == null)
                {
                    autobanHudP = null;
                    autobanBannedP.Clear();
                    return;
                }

                if (!ReferenceEquals(hud, autobanHudP))
                {
                    autobanHudP = hud;
                    autobanBannedP.Clear();
                }

                List<KeyValuePair<byte, byte>> votes = new List<KeyValuePair<byte, byte>>();
                ReadVotesP(hud, votes);
                if (votes.Count == 0) return;

                foreach (KeyValuePair<byte, byte> vote in votes)
                {
                    byte voterId = vote.Key;
                    byte targetId = vote.Value;
                    if (voterId == targetId || autobanBannedP.Contains(voterId)) continue;
                    if (targetId >= 250) continue; // HasNotVoted / Missed / Dead / Skipped sentinels.

                    PlayerControl voter = FindPlayerByIdP(voterId);
                    PlayerControl target = FindPlayerByIdP(targetId);
                    if (voter == null || target == null) continue;
                    if (voter == PlayerControl.LocalPlayer) continue;

                    int voterTeam = GetTeamP(voter);
                    int targetTeam = GetTeamP(target);
                    if (voterTeam < 0 || targetTeam < 0 || voterTeam != targetTeam) continue;

                    autobanBannedP.Add(voterId);
                    if (BanPlayerP(voter))
                    {
                        string nm = voter.Data != null && !string.IsNullOrEmpty(voter.Data.PlayerName)
                            ? voter.Data.PlayerName : "?";
                        ShowNotification("<color=#FF5555>[AUTOBAN]</color> " + nm);
                    }
                }
            }
            catch { }
        }

        private static void TickLobbyLockP()
        {
            if (!proLobbyLock) return;

            float now = Time.unscaledTime;
            if (now < nextLockCheckP) return;
            nextLockCheckP = now + 0.25f;

            try
            {
                int gameId = GetGameIdP();
                if (gameId <= 0)
                {
                    lockGameIdP = gameId;
                    lockPrevP = proLobbyLock;
                    lockBaselineP.Clear();
                    lockKickedP.Clear();
                    return;
                }

                bool justEnabled = !lockPrevP && proLobbyLock;
                if (gameId != lockGameIdP || justEnabled)
                {
                    lockGameIdP = gameId;
                    lockPrevP = proLobbyLock;
                    lockBaselineP.Clear();
                    lockKickedP.Clear();

                    if (proLobbyLock) CaptureLockBaselineP();
                }
                lockPrevP = proLobbyLock;

                if (!proLobbyLock) return;

                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;

                PlayerControl local = PlayerControl.LocalPlayer;
                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc == local || pc.Data == null) continue;
                        object bot = GetValueP(pc.Data, "Bot");
                        if (bot is bool isBot && isBot) continue;

                        int owner = Convert.ToInt32(pc.OwnerId);
                        if (owner < 0 || lockBaselineP.Contains(owner) || lockKickedP.Contains(owner)) continue;

                        lockKickedP.Add(owner);
                        AmongUsClient.Instance.KickPlayer(owner, false);
                        string nm = string.IsNullOrEmpty(pc.Data.PlayerName) ? "?" : pc.Data.PlayerName;
                        ShowNotification("<color=#FFAC1C>[LOCK]</color> Kicked " + nm);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TickSpectatorModeP()
        {
            if (!proSpectatorMode)
            {
                spectatorAppliedP = false;
                return;
            }

            try
            {
                int gameId = GetGameIdP();
                if (gameId != spectatorGameIdP)
                {
                    spectatorGameIdP = gameId;
                    spectatorAppliedP = false;
                }
                if (spectatorAppliedP || gameId <= 0) return;

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null) return;

                ForceGhostP(local);
                spectatorAppliedP = true;
                ShowNotification("<color=#8FB7FF>[SPECTATOR]</color> Ghost mode enabled.");
            }
            catch { }
        }

        // --------------------------------------------------------------------
        // Game preset application (reflection only, guarded)
        // --------------------------------------------------------------------

        private static void ApplyGamePresetP()
        {
            if (!IsHostP())
            {
                ShowNotification("<color=#FF4444>[PRESET]</color> Host only.");
                return;
            }

            try
            {
                int index = Mathf.Clamp(proGamePresetIndex, 0, GamePresetsP.Length - 1);
                object mgr = GetStaticMemberP(typeof(GameOptionsManager), "Instance");
                if (mgr == null)
                {
                    ShowNotification("<color=#FF4444>[PRESET]</color> Game options not ready.");
                    return;
                }

                object opts = GetFirstValueP(mgr, "CurrentGameOptions", "currentNormalGameOptions",
                    "currentGameOptions", "GameHostOptions");
                if (opts == null)
                {
                    ShowNotification("<color=#FF4444>[PRESET]</color> Could not read game options.");
                    return;
                }

                KeyValuePair<string, object>[] preset = GamePresetsP[index];
                int applied = 0;
                foreach (KeyValuePair<string, object> option in preset)
                    if (SetGameOptionP(opts, option.Key, option.Value)) applied++;

                try { TrySetMemberP(mgr, opts, "GameHostOptions"); }
                catch { }

                try
                {
                    object gameManager = GetStaticMemberP(typeof(GameManager), "Instance");
                    object logic = GetFirstValueP(gameManager, "LogicOptions");
                    MethodInfo sync = FindMethodP(logic == null ? null : logic.GetType(), "SyncOptions", 0, null);
                    if (sync != null) sync.Invoke(logic, null);
                }
                catch { }

                string name = GamePresetNamesP[Mathf.Clamp(index, 0, GamePresetNamesP.Length - 1)];
                if (applied > 0)
                    ShowNotification("<color=#7CFC98>[PRESET]</color> " + name + " applied (" + applied + " rules).");
                else
                    ShowNotification("<color=#FF4444>[PRESET]</color> No rules could be applied.");
            }
            catch
            {
                ShowNotification("<color=#FF4444>[PRESET]</color> Could not apply preset.");
            }
        }

        private static bool SetGameOptionP(object opts, string name, object value)
        {
            if (opts == null || string.IsNullOrEmpty(name)) return false;

            try
            {
                Type type = opts.GetType();
                string[] setters = { "SetInt", "SetFloat", "SetBool", "SetByte", "SetValue" };
                foreach (string setter in setters)
                {
                    for (Type cur = type; cur != null; cur = cur.BaseType)
                    {
                        MethodInfo[] methods;
                        try { methods = cur.GetMethods(AllMembersP); }
                        catch { continue; }

                        foreach (MethodInfo method in methods)
                        {
                            try
                            {
                                if (!string.Equals(method.Name, setter, StringComparison.Ordinal)) continue;
                                ParameterInfo[] ps = method.GetParameters();
                                if (ps.Length != 2 || !ps[0].ParameterType.IsEnum) continue;

                                object enumValue = FindEnumP(ps[0].ParameterType, name);
                                if (enumValue == null) continue;

                                object arg = CastValueP(value, ps[1].ParameterType);
                                method.Invoke(opts, new object[] { enumValue, arg });
                                return true;
                            }
                            catch { }
                        }
                    }
                }

                // Fallback for builds that expose option values as plain properties.
                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        PropertyInfo prop = cur.GetProperty(name, AllMembersP);
                        if (prop == null || !prop.CanWrite) continue;
                        prop.SetValue(opts, CastValueP(value, prop.PropertyType), null);
                        return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private static object FindEnumP(Type enumType, string name)
        {
            if (enumType == null || !enumType.IsEnum || string.IsNullOrEmpty(name)) return null;
            try
            {
                Array values = Enum.GetValues(enumType);
                foreach (object value in values)
                    if (string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase))
                        return value;
            }
            catch { }
            return null;
        }

        private static object CastValueP(object value, Type target)
        {
            try
            {
                if (value == null) return null;
                if (target == null || target == typeof(object)) return value;
                if (target.IsInstanceOfType(value)) return value;
                if (target == typeof(float)) return Convert.ToSingle(value);
                if (target == typeof(double)) return Convert.ToDouble(value);
                if (target == typeof(int)) return Convert.ToInt32(value);
                if (target == typeof(byte)) return Convert.ToByte(value);
                if (target == typeof(bool)) return Convert.ToBoolean(value);
                if (target.IsEnum) return Enum.ToObject(target, Convert.ToInt32(value));
            }
            catch { }
            return value;
        }

        // --------------------------------------------------------------------
        // Team / vote / ghost helpers (reflection, guarded)
        // --------------------------------------------------------------------

        private static void ReadVotesP(object hud, List<KeyValuePair<byte, byte>> votes)
        {
            if (hud == null || votes == null) return;

            try
            {
                object states = GetValueP(hud, "playerStates", "PlayerStates", "votedFor");
                System.Collections.IEnumerable enumerable = states as System.Collections.IEnumerable;
                if (enumerable != null)
                {
                    foreach (object area in enumerable)
                    {
                        try
                        {
                            if (area == null) continue;
                            byte voter = ToByteP(GetValueP(area, "TargetPlayerId", "VoterId", "voterId"));
                            byte target = ToByteP(GetValueP(area, "VotedFor", "votedFor", "VotedForId"));
                            if (voter != byte.MaxValue) votes.Add(new KeyValuePair<byte, byte>(voter, target));
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (votes.Count > 0) return;

            try
            {
                object dict = GetValueP(hud, "dicVote", "votes", "Votes");
                System.Collections.IDictionary map = dict as System.Collections.IDictionary;
                if (map == null) return;

                foreach (System.Collections.DictionaryEntry entry in map)
                {
                    try
                    {
                        byte voter = ToByteP(entry.Key);
                        byte target = ToByteP(entry.Value);
                        if (voter != byte.MaxValue) votes.Add(new KeyValuePair<byte, byte>(voter, target));
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static byte ToByteP(object value)
        {
            try
            {
                if (value == null) return byte.MaxValue;
                return Convert.ToByte(value);
            }
            catch { return byte.MaxValue; }
        }

        private static int GetTeamP(PlayerControl player)
        {
            if (player == null || player.Data == null) return -1;
            try
            {
                object role = GetValueP(player.Data, "Role");
                object imp = GetValueP(role, "IsImpostor");
                if (imp is bool impostor) return impostor ? 1 : 0;

                imp = GetValueP(player.Data, "IsImpostor");
                if (imp is bool impostor2) return impostor2 ? 1 : 0;

                object team = GetValueP(role, "Team");
                if (team != null)
                {
                    string n = team.ToString();
                    if (n.IndexOf("Impostor", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
                    if (n.IndexOf("Crew", StringComparison.OrdinalIgnoreCase) >= 0) return 0;
                }
            }
            catch { }
            return -1;
        }

        private static PlayerControl FindPlayerByIdP(byte id)
        {
            try
            {
                var all = PlayerControl.AllPlayerControls;
                if (all == null) return null;
                foreach (PlayerControl pc in all)
                    if (pc != null && pc.PlayerId == id) return pc;
            }
            catch { }
            return null;
        }

        private static int GetClientIdP(PlayerControl player)
        {
            if (player == null) return -1;
            try
            {
                object value = GetValueP(player.Data, "ClientId");
                if (value != null) return Convert.ToInt32(value);
            }
            catch { }
            try { return Convert.ToInt32(player.OwnerId); }
            catch { return -1; }
        }

        private static bool BanPlayerP(PlayerControl player)
        {
            try
            {
                if (player == null || player == PlayerControl.LocalPlayer) return false;
                int clientId = GetClientIdP(player);
                if (clientId < 0) return false;
                AmongUsClient.Instance.KickPlayer(clientId, true);
                return true;
            }
            catch { return false; }
        }

        private static void ForceGhostP(PlayerControl local)
        {
            if (local == null || local.Data == null) return;

            if (TrySetBoolMemberP(local.Data, true, "IsDead", "isDead", "Dead")) return;

            // Fallbacks for builds where IsDead is not directly writable.
            try
            {
                MethodInfo setDead = FindMethodP(local.GetType(), "RpcSetDead", 0, null);
                if (setDead != null) { setDead.Invoke(local, null); return; }
                setDead = FindMethodP(local.GetType(), "RpcSetDead", 1, typeof(bool));
                if (setDead != null) { setDead.Invoke(local, new object[] { true }); return; }
            }
            catch { }

            try
            {
                MethodInfo murder = FindMethodP(local.GetType(), "RpcMurderPlayer", 1, typeof(PlayerControl));
                if (murder != null) { murder.Invoke(local, new object[] { local }); return; }
                murder = FindMethodP(local.GetType(), "RpcMurderPlayer", 2, typeof(PlayerControl));
                if (murder != null) murder.Invoke(local, new object[] { local, false });
            }
            catch { }
        }

        private static void CaptureLockBaselineP()
        {
            try
            {
                lockBaselineP.Clear();
                var all = PlayerControl.AllPlayerControls;
                if (all == null) return;
                PlayerControl local = PlayerControl.LocalPlayer;
                foreach (PlayerControl pc in all)
                {
                    try
                    {
                        if (pc == null || pc == local || pc.Data == null) continue;
                        int owner = Convert.ToInt32(pc.OwnerId);
                        if (owner >= 0) lockBaselineP.Add(owner);
                    }
                    catch { }
                }
            }
            catch { }
        }

        // --------------------------------------------------------------------
        // Generic reflection helpers (all guarded)
        // --------------------------------------------------------------------

        private static bool IsHostP()
        {
            try { return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost; }
            catch { return false; }
        }

        private static int GetGameIdP()
        {
            try
            {
                if (AmongUsClient.Instance == null) return -1;
                return AmongUsClient.Instance.GameId;
            }
            catch { return -1; }
        }

        private static void ResetP()
        {
            try
            {
                nextSkipCountdownP = 0f;
                nextAutobanP = 0f;
                nextLockCheckP = 0f;
                autobanGameIdP = -1;
                autobanHudP = null;
                autobanBannedP.Clear();
                lockGameIdP = -1;
                lockPrevP = false;
                lockBaselineP.Clear();
                lockKickedP.Clear();
                spectatorGameIdP = -1;
                spectatorAppliedP = false;
            }
            catch { }
        }

        private static object GetGameStartManagerP()
        {
            try
            {
                object inst = GetStaticMemberP(typeof(GameStartManager), "Instance");
                if (inst != null) return inst;
            }
            catch { }
            try { return UnityEngine.Object.FindObjectOfType<GameStartManager>(); }
            catch { return null; }
        }

        private static object GetStaticMemberP(Type type, params string[] names)
        {
            if (type == null || names == null) return null;
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    for (Type cur = type; cur != null; cur = cur.BaseType)
                    {
                        try
                        {
                            PropertyInfo prop = cur.GetProperty(name, AllMembersP);
                            if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                            {
                                object val = prop.GetValue(null, null);
                                if (val != null) return val;
                            }
                        }
                        catch { }
                        try
                        {
                            FieldInfo field = cur.GetField(name, AllMembersP);
                            if (field != null && field.IsStatic)
                            {
                                object val = field.GetValue(null);
                                if (val != null) return val;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return null;
        }

        private static object GetValueP(object instance, params string[] names)
        {
            if (instance == null || names == null) return null;
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    for (Type cur = instance.GetType(); cur != null; cur = cur.BaseType)
                    {
                        try
                        {
                            PropertyInfo prop = cur.GetProperty(name, AllMembersP);
                            if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                            {
                                object val = prop.GetValue(instance, null);
                                if (val != null) return val;
                            }
                        }
                        catch { }
                        try
                        {
                            FieldInfo field = cur.GetField(name, AllMembersP);
                            if (field != null)
                            {
                                object val = field.GetValue(instance);
                                if (val != null) return val;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return null;
        }

        private static object GetFirstValueP(object instance, params string[] names)
        {
            return GetValueP(instance, names);
        }

        private static FieldInfo FindFieldP(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        FieldInfo field = cur.GetField(name, AllMembersP);
                        if (field != null) return field;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static MethodInfo FindMethodP(Type type, string name, int parameterCount, Type firstParameterType)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                for (Type cur = type; cur != null; cur = cur.BaseType)
                {
                    try
                    {
                        foreach (MethodInfo method in cur.GetMethods(AllMembersP))
                        {
                            if (method.Name != name) continue;
                            ParameterInfo[] ps = method.GetParameters();
                            if (ps.Length != parameterCount) continue;
                            if (firstParameterType != null && ps.Length > 0 && ps[0].ParameterType != firstParameterType)
                                continue;
                            return method;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static bool TrySetMemberP(object target, object value, params string[] names)
        {
            if (target == null || names == null) return false;
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    for (Type cur = target.GetType(); cur != null; cur = cur.BaseType)
                    {
                        try
                        {
                            PropertyInfo prop = cur.GetProperty(name, AllMembersP);
                            if (prop != null && prop.CanWrite && prop.GetIndexParameters().Length == 0)
                            {
                                prop.SetValue(target, CastValueP(value, prop.PropertyType), null);
                                return true;
                            }
                        }
                        catch { }
                        try
                        {
                            FieldInfo field = cur.GetField(name, AllMembersP);
                            if (field != null)
                            {
                                field.SetValue(target, CastValueP(value, field.FieldType));
                                return true;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return false;
        }

        private static bool TrySetBoolMemberP(object target, bool value, params string[] names)
        {
            if (target == null || names == null) return false;
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                try
                {
                    for (Type cur = target.GetType(); cur != null; cur = cur.BaseType)
                    {
                        try
                        {
                            PropertyInfo prop = cur.GetProperty(name, AllMembersP);
                            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
                            {
                                prop.SetValue(target, value, null);
                                return true;
                            }
                        }
                        catch { }
                        try
                        {
                            FieldInfo field = cur.GetField(name, AllMembersP);
                            if (field != null && field.FieldType == typeof(bool))
                            {
                                field.SetValue(target, value);
                                return true;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return false;
        }
    }
}
