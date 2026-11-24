#nullable disable
using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using InnerNet;
using UnityEngine;

namespace AeroMenu
{
    // "Rejoin Until Impostor" (Self -> Roles -> Role Tools) - lobby hopper.
    //
    // 1. Start it in a lobby: that lobby's settings (max players, impostors,
    //    mode, map) become the target and it leaves to search.
    // 2. It asks the matchmaker for public lobbies with the same settings that
    //    are nearly full (at least max - fill margin players, not full), picks
    //    the fullest one not tried recently and joins it.
    // 3. It waits in the lobby until the match starts and the role screen appears.
    // 4. Match starts: impostor -> stop. Otherwise leave and hop to another lobby.
    public partial class AeroMenuGUI : MonoBehaviour
    {
        public static bool rejoinForImpostor;                 // not persisted on purpose
        public static bool rejoinForImpostorStrict;           // plain Impostor only
        public static bool rejoinForImpostorSameMap;
        public static int rejoinForImpostorFillMargin = 2;    // min players = max - margin
        public static int rejoinForImpostorMaxTries;          // 0 = unlimited lobbies
        public static float rejoinForImpostorLeaveDelay = 1f;
        public static int rejoinForImpostorLobbyWait = 0;     // 0 = wait until the role screen

        private enum RfiPhase { Idle, WaitMatch, Search, Joining }

        private static RfiPhase rfiPhase = RfiPhase.Idle;
        private static string rfiState = "Off";
        private static int rfiHops;
        private static bool rfiPrefsLoaded;
        private static bool rfiCaptureOnLobby;
        private static float rfiNextTickAt;
        private static float rfiRoleSeenAt = -1f;
        private static float rfiMatchEnteredAt = -1f;
        private static float rfiLobbyJoinedAt = -1f;
        private static float rfiNextSearchAt;
        private static float rfiJoinDeadline;
        private static float rfiRequestStartedAt;
        private static float rfiLeaveIssuedAt = -1f;
        private static bool rfiRequestInFlight;
        private static int rfiCurrentGameId;
        private static bool rfiTargetSet;
        private static int rfiTargetMax, rfiTargetImps, rfiTargetMap;
        private static bool rfiTargetHns;
        private static readonly Dictionary<int, float> rfiVisited = new Dictionary<int, float>();

        // Results handed over from the matchmaker callback to the tick.
        private static readonly List<RfiCandidate> rfiResults = new List<RfiCandidate>();
        private static readonly List<RfiCandidate> rfiQueue = new List<RfiCandidate>();
        private static bool rfiResultsReady;
        private static int rfiResultsListed;
        private static int rfiJoinAttempts;

        private struct RfiCandidate
        {
            public int GameId;
            public int Players;
            public int Max;
            public int Imps;
            public int Map;
            public bool Hns;
            public bool ModeKnown;
        }

        // Minimum players accepted in a 15-player room. Fuller rooms are tried
        // first, but a room that started at 10+ can fill before we join it.
        private const int RfiMinActualPlayers = 9;

        private const float RfiRoleSettleSeconds = 0.6f;
        private const float RfiSearchRetrySeconds = 4f;
        private const float RfiJoinTimeoutSeconds = 15f;
        private const float RfiRequestTimeoutSeconds = 15f;
        private const float RfiVisitedForgetSeconds = 600f;

        internal static string RejoinForImpostorStatus
        {
            get
            {
                string hops = rejoinForImpostorMaxTries > 0 ? rfiHops + "/" + rejoinForImpostorMaxTries : rfiHops.ToString();
                return rfiState + "  (lobbies: " + hops + ")";
            }
        }

        private static string RfiTargetText
        {
            get
            {
                if (!rfiTargetSet) return "Target: not set (start it inside a lobby)";
                string map = rejoinForImpostorSameMap ? RfiMapName(rfiTargetMap) : "any map";
                int min = Mathf.Max(1, rfiTargetMax - rejoinForImpostorFillMargin);
                return "Target: " + rfiTargetMax + "p, " + rfiTargetImps + " imp, " + (rfiTargetHns ? "Hide & Seek" : "Classic") +
                       ", " + map + ", " + min + "+ players";
            }
        }

        // ---------------------------------------------------------------- prefs

        private static void LoadRejoinForImpostorPrefs()
        {
            if (rfiPrefsLoaded) return;
            rfiPrefsLoaded = true;
            try
            {
                rejoinForImpostorStrict = PlayerPrefs.GetInt("M_RFI_Strict", 0) == 1;
                rejoinForImpostorSameMap = PlayerPrefs.GetInt("M_RFI_SameMap", 0) == 1;
                rejoinForImpostorFillMargin = Mathf.Clamp(PlayerPrefs.GetInt("M_RFI_FillMargin", 2), 0, 10);
                rejoinForImpostorMaxTries = Mathf.Clamp(PlayerPrefs.GetInt("M_RFI_MaxTries", 0), 0, 99);
                rejoinForImpostorLeaveDelay = Mathf.Clamp(PlayerPrefs.GetFloat("M_RFI_LeaveDelay", 1f), 0f, 5f);
                rejoinForImpostorLobbyWait = Mathf.Clamp(PlayerPrefs.GetInt("M_RFI_LobbyWait", 0), 0, 600);
                rfiTargetSet = PlayerPrefs.GetInt("M_RFI_TargetSet", 0) == 1;
                rfiTargetMax = PlayerPrefs.GetInt("M_RFI_TargetMax", 15);
                rfiTargetImps = PlayerPrefs.GetInt("M_RFI_TargetImps", 2);
                rfiTargetMap = PlayerPrefs.GetInt("M_RFI_TargetMap", 0);
                rfiTargetHns = PlayerPrefs.GetInt("M_RFI_TargetHns", 0) == 1;
            }
            catch { }
        }

        private static void SaveRejoinForImpostorPrefs()
        {
            try
            {
                PlayerPrefs.SetInt("M_RFI_Strict", rejoinForImpostorStrict ? 1 : 0);
                PlayerPrefs.SetInt("M_RFI_SameMap", rejoinForImpostorSameMap ? 1 : 0);
                PlayerPrefs.SetInt("M_RFI_FillMargin", rejoinForImpostorFillMargin);
                PlayerPrefs.SetInt("M_RFI_MaxTries", rejoinForImpostorMaxTries);
                PlayerPrefs.SetFloat("M_RFI_LeaveDelay", rejoinForImpostorLeaveDelay);
                PlayerPrefs.SetInt("M_RFI_LobbyWait", rejoinForImpostorLobbyWait);
                PlayerPrefs.SetInt("M_RFI_TargetSet", rfiTargetSet ? 1 : 0);
                PlayerPrefs.SetInt("M_RFI_TargetMax", rfiTargetMax);
                PlayerPrefs.SetInt("M_RFI_TargetImps", rfiTargetImps);
                PlayerPrefs.SetInt("M_RFI_TargetMap", rfiTargetMap);
                PlayerPrefs.SetInt("M_RFI_TargetHns", rfiTargetHns ? 1 : 0);
            }
            catch { }
        }

        private static bool CaptureLobbyTarget()
        {
            try
            {
                IGameOptions o = GameOptionsManager.Instance != null ? GameOptionsManager.Instance.CurrentGameOptions : null;
                if (o == null) return false;
                rfiTargetMax = o.MaxPlayers;
                rfiTargetImps = o.NumImpostors;
                rfiTargetMap = o.MapId;
                rfiTargetHns = IsHnsMode(o.GameMode);
                rfiTargetSet = rfiTargetMax > 0;
                SaveRejoinForImpostorPrefs();
                return rfiTargetSet;
            }
            catch { return false; }
        }

        // ---------------------------------------------------------------- control

        private static void StartRejoinForImpostor()
        {
            LoadRejoinForImpostorPrefs();
            rfiHops = 0;
            rfiQueue.Clear();
            rfiJoinAttempts = 0;
            rfiRoleSeenAt = -1f;
            rfiMatchEnteredAt = -1f;
            rfiLobbyJoinedAt = -1f;
            rfiRequestInFlight = false;
            rfiResultsReady = false;
            rfiCaptureOnLobby = true;
            rfiPhase = RfiPhase.WaitMatch;
            rfiState = "Starting";
            PruneVisited(true);
        }

        private static void StopRejoinForImpostor(string state, string notification)
        {
            rejoinForImpostor = false;
            rfiPhase = RfiPhase.Idle;
            rfiRequestInFlight = false;
            rfiRoleSeenAt = -1f;
            rfiState = state;
            if (!string.IsNullOrEmpty(notification)) ShowNotification(notification);
        }

        private static void RfiLeaveAndSearch(string why)
        {
            rfiPhase = RfiPhase.Search;
            rfiMatchEnteredAt = -1f;
            rfiRoleSeenAt = -1f;
            rfiLobbyJoinedAt = -1f;
            rfiNextSearchAt = Time.unscaledTime + 2.5f;
            rfiLeaveIssuedAt = Time.unscaledTime;
            rfiState = why;
            try { AmongUsClient.Instance?.ExitGame(DisconnectReasons.ExitGame); } catch { }
        }

        // ---------------------------------------------------------------- tick

        internal static void TickRejoinForImpostor()
        {
            LoadRejoinForImpostorPrefs();
            if (!rejoinForImpostor)
            {
                if (rfiPhase != RfiPhase.Idle) { rfiPhase = RfiPhase.Idle; rfiState = "Off"; }
                TickRfiFilterRestore();
                return;
            }

            float now = Time.unscaledTime;
            if (now < rfiNextTickAt) return;
            rfiNextTickAt = now + 0.2f;

            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                bool inLobby = LobbyBehaviour.Instance != null;
                bool inMatch = !inLobby && ShipStatus.Instance != null;

                if ((inMatch || inLobby) && rfiPhase == RfiPhase.Search)
                {
                    // A leave is in progress; the client has not left the room yet.
                    if (rfiLeaveIssuedAt < 0f || now - rfiLeaveIssuedAt > 5f)
                    {
                        rfiLeaveIssuedAt = now;
                        try { client?.ExitGame(DisconnectReasons.ExitGame); } catch { }
                    }
                    rfiNextSearchAt = Mathf.Max(rfiNextSearchAt, now + 1.5f);
                    if (!rfiState.StartsWith("Leaving", StringComparison.Ordinal) && !rfiState.StartsWith("Settings", StringComparison.Ordinal) &&
                        !rfiState.StartsWith("No start", StringComparison.Ordinal) && !rfiState.StartsWith("Host left", StringComparison.Ordinal))
                        rfiState = "Leaving...";
                    return;
                }

                if (inMatch) TickRfiMatch(client, now);
                else if (inLobby) TickRfiLobby(client, now);
                else TickRfiMenu(client, now);
            }
            catch { }
        }

        private static void TickRfiMatch(AmongUsClient client, float now)
        {
            rfiPhase = RfiPhase.WaitMatch;
            if (rfiCaptureOnLobby)
            {
                // Started during a match: this match's settings become the target.
                rfiCaptureOnLobby = false;
                CaptureLobbyTarget();
            }
            rfiLobbyJoinedAt = -1f;
            if (rfiMatchEnteredAt < 0f) rfiMatchEnteredAt = now;
            if (client != null && client.GameId != 0) rfiCurrentGameId = client.GameId;

            // The role is only final once the game has shown the role screen.
            bool roleScreenShown = false;
            try { roleScreenShown = IntroCutscene.Instance != null; } catch { }
            // Late joins may miss the cutscene; after 15s of role data, proceed.
            bool lateJoinFallback = now - rfiMatchEnteredAt > 15f;
            if (!roleScreenShown && !lateJoinFallback) { rfiState = "Match starting, waiting for role screen"; return; }

            PlayerControl lp = PlayerControl.LocalPlayer;
            if (lp == null || lp.Data == null || lp.Data.Role == null) { rfiState = "Role screen shown, waiting for role data"; return; }

            if (rfiRoleSeenAt < 0f) { rfiRoleSeenAt = now; rfiState = "Role screen shown, checking role"; return; }
            if (now - rfiRoleSeenAt < RfiRoleSettleSeconds) return;

            RoleTypes role = lp.Data.RoleType;
            string roleName = RoleNameForRfi(role);
            if (IsWantedImpostorRole(lp, role))
            {
                StopRejoinForImpostor("Got " + roleName + " after " + rfiHops + " lobbies",
                    "<color=#FF3B3B>[IMPOSTOR]</color> You are " + roleName + "! (" + rfiHops + " lobbies)");
                return;
            }

            if (client != null && client.AmHost)
            {
                StopRejoinForImpostor("Stopped - you are host", "<color=#FFAA00>[IMPOSTOR]</color> You became host, staying.");
                return;
            }

            if (rejoinForImpostorMaxTries > 0 && rfiHops >= rejoinForImpostorMaxTries)
            {
                StopRejoinForImpostor("Stopped - lobby limit reached", "<color=#FFAA00>[IMPOSTOR]</color> Lobby limit reached, staying in this match.");
                return;
            }

            float leaveAt = rfiRoleSeenAt + RfiRoleSettleSeconds + rejoinForImpostorLeaveDelay;
            if (now < leaveAt) { rfiState = roleName + " - leaving in " + Mathf.CeilToInt(leaveAt - now) + "s"; return; }

            if (rfiCurrentGameId != 0) rfiVisited[rfiCurrentGameId] = now;
            RfiLeaveAndSearch("Leaving (" + roleName + "), finding next lobby");
        }

        private static void TickRfiLobby(AmongUsClient client, float now)
        {
            rfiRoleSeenAt = -1f;
            int gameId = client != null ? client.GameId : 0;

            if (rfiCaptureOnLobby)
            {
                // Started inside a lobby: its settings become the target, then hop.
                rfiCaptureOnLobby = false;
                if (!CaptureLobbyTarget())
                {
                    StopRejoinForImpostor("Stopped - could not read lobby settings", null);
                    return;
                }
                if (gameId != 0) rfiVisited[gameId] = now;
                ShowNotification("<color=#FF6B6B>[IMPOSTOR]</color> " + RfiTargetText);
                RfiLeaveAndSearch("Settings saved, finding lobby");
                return;
            }

            if (rfiLobbyJoinedAt < 0f || gameId != rfiCurrentGameId)
            {
                rfiLobbyJoinedAt = now;
                rfiCurrentGameId = gameId;
                if (rfiQueue.Count > 0 && rfiQueue[0].GameId == gameId) rfiQueue.RemoveAt(0);
                rfiJoinAttempts = 0;
                rfiHops++;
                if (gameId != 0) rfiVisited[gameId] = now;
            }
            rfiPhase = RfiPhase.WaitMatch;

            if (client != null && client.AmHost)
            {
                RfiLeaveAndSearch("Host left (you are host now), next lobby");
                return;
            }

            float waited = now - rfiLobbyJoinedAt;
            // Wait for the match / role screen. The timer is only an optional
            // safety valve (0 disables it and waits indefinitely).
            if (rejoinForImpostorLobbyWait > 0 && waited >= rejoinForImpostorLobbyWait)
            {
                RfiLeaveAndSearch("No start in " + rejoinForImpostorLobbyWait + "s, next lobby");
                return;
            }

            int players = 0;
            try { players = PlayerControl.AllPlayerControls != null ? PlayerControl.AllPlayerControls.Count : 0; } catch { }
            rfiState = "In " + SafeRoomName(gameId) + " (" + players + "p), waiting for role screen" +
                       (rejoinForImpostorLobbyWait > 0
                           ? " " + Mathf.CeilToInt(rejoinForImpostorLobbyWait - waited) + "s"
                           : string.Empty);
        }

        private static void TickRfiMenu(AmongUsClient client, float now)
        {
            rfiLeaveIssuedAt = -1f;
            rfiLobbyJoinedAt = -1f;
            rfiRoleSeenAt = -1f;
            try { AeroBugroomScoutService.TryCloseDisconnectPopup(); } catch { }

            if (rfiCaptureOnLobby)
            {
                if (!rfiTargetSet)
                {
                    StopRejoinForImpostor("Stopped - join a lobby first", "<color=#FFAA00>[IMPOSTOR]</color> Start it inside a lobby so it can copy the settings.");
                    return;
                }
                rfiCaptureOnLobby = false;
                rfiPhase = RfiPhase.Search;
                rfiNextSearchAt = now;
            }

            if (rfiPhase == RfiPhase.WaitMatch) { rfiPhase = RfiPhase.Search; rfiNextSearchAt = now + 1f; }

            if (rfiPhase == RfiPhase.Joining)
            {
                if (now < rfiJoinDeadline) { rfiState = "Joining " + SafeRoomName(rfiCurrentGameId) + "..."; return; }
                if (rfiJoinAttempts < 2)
                {
                    rfiState = "Join failed, retrying " + SafeRoomName(rfiCurrentGameId);
                    rfiNextSearchAt = now + 1.5f;
                }
                else if (rfiQueue.Count > 0)
                {
                    rfiVisited[rfiQueue[0].GameId] = now;
                    rfiQueue.RemoveAt(0);
                    rfiJoinAttempts = 0;
                    rfiState = rfiQueue.Count > 0 ? "Join failed, trying next lobby" : "Join failed, refreshing list";
                    rfiNextSearchAt = now + 1.5f;
                }
                else rfiNextSearchAt = now + 1.5f;
                rfiPhase = RfiPhase.Search;
                return;
            }

            if (rfiResultsReady)
            {
                rfiResultsReady = false;
                BuildRfiQueue(now);
                if (rfiQueue.Count == 0)
                {
                    rfiNextSearchAt = now + RfiSearchRetrySeconds;
                    rfiState = "No matching lobby (" + rfiResultsListed + " listed), retrying";
                    return;
                }
            }

            // Respect the post-leave settle delay here too: after a failed role
            // the client can reach the menu before rfiNextSearchAt, and joining
            // immediately races the pending disconnect.
            if (rfiQueue.Count > 0 && rfiPhase == RfiPhase.Search && client != null && now >= rfiNextSearchAt)
            {
                RfiCandidate c = rfiQueue[0];
                rfiCurrentGameId = c.GameId;
                rfiJoinAttempts++;
                rfiPhase = RfiPhase.Joining;
                rfiJoinDeadline = now + RfiJoinTimeoutSeconds;
                rfiState = "Joining " + SafeRoomName(c.GameId) + " (" + c.Players + "/" + c.Max + ", try " + rfiJoinAttempts + ")";
                client.GameId = c.GameId;
                var co = client.CoJoinOnlineGameFromCode(c.GameId);
                if (co != null) client.StartCoroutine(co);
                return;
            }

            if (rfiRequestInFlight)
            {
                if (now - rfiRequestStartedAt > RfiRequestTimeoutSeconds) { rfiRequestInFlight = false; rfiNextSearchAt = now + 1f; }
                else rfiState = "Searching lobbies...";
                return;
            }

            if (now < rfiNextSearchAt) return;
            RequestRfiGameList(client, now);
        }

        // ---------------------------------------------------------------- search

        // Uses the game's own Find Game screen: it fetches the public lobby list
        // and hands it to FindAGameManager.HandleList, which RfiHandleListPatch
        // forwards here. (Calling the matchmaker directly from the main menu fails
        // because state the Find Game screen sets up is missing.)
        private static void RequestRfiGameList(AmongUsClient client, float now)
        {
            try
            {
                FindAGameManager fgm = UnityEngine.Object.FindObjectOfType<FindAGameManager>();
                rfiRequestInFlight = true;
                rfiRequestStartedAt = now;
                ApplyRfiFilters(fgm);
                if (fgm == null)
                {
                    EnsureRfiSearchOptions();
                    rfiState = "Opening Find Game...";
                    UnityEngine.SceneManagement.SceneManager.LoadScene("FindAGame");
                    return; // the screen refreshes its list by itself when it opens
                }
                rfiState = "Searching lobbies...";
                fgm.RefreshList();
            }
            catch (Exception ex)
            {
                rfiRequestInFlight = false;
                rfiNextSearchAt = now + RfiSearchRetrySeconds;
                rfiState = "Search failed, retrying";
                Plugin.Instance?.Log?.LogWarning("[IMPOSTOR] lobby search failed: " + ex.Message);
            }
        }

        // The Find Game screen reads GameOptionsManager.GameSearchOptions in Start.
        // The online menu normally sets them; when opening the screen directly we
        // rebuild them from the search settings the game saved.
        private static void EnsureRfiSearchOptions()
        {
            try
            {
                GameOptionsManager gom = GameOptionsManager.Instance;
                if (gom == null) return;
                IGameOptions current = null;
                try { current = gom.GameSearchOptions; } catch { }
                if (current != null) return;

                var mp = AmongUs.Data.DataManager.Settings.Multiplayer;
                var raw = rfiTargetHns ? mp.RawHideNSeekSearchOptions : mp.RawNormalSearchOptions;
                IGameOptions opts = null;
                if (raw != null && raw.Length > 0 && gom.gameOptionsFactory != null)
                    opts = gom.gameOptionsFactory.FromBytes(raw);
                if (opts == null && !rfiTargetHns)
                    opts = new NormalGameOptionsV12(gom.logger).Cast<IGameOptions>();
                if (opts == null) return;

                if (rfiTargetHns) gom.hideNSeekGameSearchOptions = opts.Cast<HideNSeekGameOptionsV12>();
                else gom.normalGameSearchOptions = opts.Cast<NormalGameOptionsV12>();
                gom.currentSearchOptions = opts;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.Log?.LogWarning("[IMPOSTOR] search options setup failed: " + ex.Message);
            }
        }

        // ---- Find Game filters --------------------------------------------------
        // The Find Game screen reloads its filters from the saved settings
        // (DataManager.Settings.Multiplayer.ClassicFilterSet / HnSFilterSet) on every
        // refresh, so the hopper edits that saved set: impostor count = target, map =
        // target or any. Chat / language filters are left alone. The user's original
        // values are kept in PlayerPrefs and written back when hopping stops, or on
        // the next launch if the game was closed mid-hop.
        private static float rfiFilterRestoreCheckAt;

        private static AmongUs.Matchmaking.GameFilterSet GetSavedFilterSet(bool hns)
        {
            var mp = AmongUs.Data.DataManager.Settings.Multiplayer;
            return hns ? mp.HnSFilterSet : mp.ClassicFilterSet;
        }

        private static void WriteSavedFilterSet(bool hns, AmongUs.Matchmaking.GameFilterSet set)
        {
            var mp = AmongUs.Data.DataManager.Settings.Multiplayer;
            if (hns) mp.HnSFilterSet = set; else mp.ClassicFilterSet = set;
        }

        private static AmongUs.Matchmaking.GameFilter FindRfiFilter(AmongUs.Matchmaking.GameFilterSet set, string key, out int index)
        {
            index = -1;
            for (int i = 0; i < set.Filters.Count; i++)
            {
                var f = set.Filters[i];
                if (f != null && f.Key == key) { index = i; return f; }
            }
            return null;
        }

        private static AmongUs.Matchmaking.GameFilter MakeRfiFilter(string type, string key, string json)
        {
            var gf = new AmongUs.Matchmaking.GameFilter(type, key, json);
            try { gf.ModifySubFilter(gf.ResolveSubFilter(type, json)); } catch { }
            return gf;
        }

        // Returns null when there is no impostor filter.
        private static List<int> ReadRfiImps(AmongUs.Matchmaking.GameFilterSet set)
        {
            int idx;
            var f = FindRfiFilter(set, "ImpostorNumber", out idx);
            if (f == null) return null;
            var list = new List<int>();
            var sub = f.SubFilter != null ? f.SubFilter.TryCast<AmongUs.Matchmaking.IntGameFilter>() : null;
            if (sub != null && sub.AcceptedValues != null)
            {
                for (int i = 0; i < sub.AcceptedValues.Count; i++) list.Add(sub.AcceptedValues[i]);
                return list;
            }
            var m = System.Text.RegularExpressions.Regex.Match(f.SubFilterString ?? "", "\"AcceptedValues\":\\[([0-9, ]*)\\]");
            if (m.Success)
                foreach (string part in m.Groups[1].Value.Split(','))
                    if (int.TryParse(part.Trim(), out int v)) list.Add(v);
            return list;
        }

        // Returns -1 when there is no map filter.
        private static int ReadRfiMap(AmongUs.Matchmaking.GameFilterSet set)
        {
            int idx;
            var f = FindRfiFilter(set, "Map", out idx);
            if (f == null) return -1;
            var sub = f.SubFilter != null ? f.SubFilter.TryCast<AmongUs.Matchmaking.MapGameFilter>() : null;
            if (sub != null) return sub.AcceptedValues;
            var m = System.Text.RegularExpressions.Regex.Match(f.SubFilterString ?? "", "\"AcceptedValues\":([0-9]+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : -1;
        }

        private static void SetRfiImps(AmongUs.Matchmaking.GameFilterSet set, List<int> values)
        {
            int idx;
            var f = FindRfiFilter(set, "ImpostorNumber", out idx);
            if (values == null) { if (f != null) set.Filters.RemoveAt(idx); return; }
            var sub = f != null && f.SubFilter != null ? f.SubFilter.TryCast<AmongUs.Matchmaking.IntGameFilter>() : null;
            if (sub != null && sub.AcceptedValues != null)
            {
                sub.AcceptedValues.Clear();
                foreach (int v in values) sub.AcceptedValues.Add(v);
                try { f.ModifySubFilter(sub.Cast<AmongUs.Matchmaking.ISubFilter>()); } catch { }
                return;
            }
            if (f != null) set.Filters.RemoveAt(idx);
            set.Filters.Add(MakeRfiFilter("int", "ImpostorNumber",
                "{\"AcceptedValues\":[" + string.Join(",", values) + "],\"OptionEnum\":1,\"FilterType\":\"int\"}"));
        }

        private static void SetRfiMap(AmongUs.Matchmaking.GameFilterSet set, int mask)
        {
            int idx;
            var f = FindRfiFilter(set, "Map", out idx);
            if (mask < 0) { if (f != null) set.Filters.RemoveAt(idx); return; }
            var sub = f != null && f.SubFilter != null ? f.SubFilter.TryCast<AmongUs.Matchmaking.MapGameFilter>() : null;
            if (sub != null)
            {
                sub.AcceptedValues = (byte)mask;
                try { f.ModifySubFilter(sub.Cast<AmongUs.Matchmaking.ISubFilter>()); } catch { }
                return;
            }
            if (f != null) set.Filters.RemoveAt(idx);
            set.Filters.Add(MakeRfiFilter("map", "Map", "{\"AcceptedValues\":" + mask + ",\"FilterType\":\"map\"}"));
        }

        private static void ApplyRfiFilters(FindAGameManager fgm)
        {
            try
            {
                bool hns = rfiTargetHns;
                var set = GetSavedFilterSet(hns);
                if (set == null || set.Filters == null) return;

                if (PlayerPrefs.GetInt("M_RFI_FilterDirty", 0) == 0)
                {
                    List<int> imps = ReadRfiImps(set);
                    PlayerPrefs.SetString("M_RFI_OrigImps", imps == null ? "none" : string.Join(",", imps));
                    PlayerPrefs.SetInt("M_RFI_OrigMap", ReadRfiMap(set));
                    PlayerPrefs.SetInt("M_RFI_OrigHns", hns ? 1 : 0);
                    PlayerPrefs.SetInt("M_RFI_FilterDirty", 1);
                    PlayerPrefs.Save();
                }

                SetRfiImps(set, new List<int> { rfiTargetImps });
                bool sameMap = rejoinForImpostorSameMap && rfiTargetMap >= 0 && rfiTargetMap < 8;
                SetRfiMap(set, sameMap ? (1 << rfiTargetMap) : -1);
                WriteSavedFilterSet(hns, set);
            }
            catch (Exception ex)
            {
                Plugin.Instance?.Log?.LogWarning("[IMPOSTOR] could not set search filters: " + ex.Message);
            }
        }

        // Writes the user's original impostor/map filters back to the saved set.
        private static bool RestoreRfiFilters()
        {
            if (PlayerPrefs.GetInt("M_RFI_FilterDirty", 0) == 0) return true;
            try
            {
                bool hns = PlayerPrefs.GetInt("M_RFI_OrigHns", 0) == 1;
                var set = GetSavedFilterSet(hns);
                if (set == null || set.Filters == null) return false;

                string imps = PlayerPrefs.GetString("M_RFI_OrigImps", "none");
                List<int> values = null;
                if (imps != "none")
                {
                    values = new List<int>();
                    foreach (string part in imps.Split(','))
                        if (int.TryParse(part.Trim(), out int v)) values.Add(v);
                }
                SetRfiImps(set, values);
                SetRfiMap(set, PlayerPrefs.GetInt("M_RFI_OrigMap", -1));
                WriteSavedFilterSet(hns, set);

                PlayerPrefs.SetInt("M_RFI_FilterDirty", 0);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.Log?.LogWarning("[IMPOSTOR] could not restore search filters: " + ex.Message);
                return false;
            }
        }

        // Runs while the feature is off until the user's filters are back.
        private static void TickRfiFilterRestore()
        {
            if (rejoinForImpostor) return;
            float now = Time.unscaledTime;
            if (now < rfiFilterRestoreCheckAt) return;
            rfiFilterRestoreCheckAt = now + 2f;
            if (PlayerPrefs.GetInt("M_RFI_FilterDirty", 0) == 0) return;
            if (AmongUs.Data.DataManager.Settings == null) return;
            RestoreRfiFilters();
        }

        internal static bool RfiWantsGameList
        {
            get { return rejoinForImpostor && (rfiPhase == RfiPhase.Search || rfiRequestInFlight); }
        }

        internal static void ReceiveRfiGameList(HttpMatchmakerManager.FindGamesListFilteredResponse response)
        {
            rfiRequestInFlight = false;
            rfiResults.Clear();
            rfiResultsListed = 0;
            try
            {
                var games = response != null ? response.Games : null;
                if (games != null)
                {
                    int n = games.Count;
                    rfiResultsListed = n;
                    for (int i = 0; i < n; i++)
                    {
                        GameListing g = games[i];
                        RfiCandidate c = new RfiCandidate
                        {
                            GameId = g.GameId,
                            Players = g.PlayerCount,
                            Max = g.MaxPlayers,
                            Imps = g.NumImpostors,
                            Map = g.MapId
                        };
                        try
                        {
                            if (g.Options != null) { c.Hns = IsHnsMode(g.Options.GameMode); c.ModeKnown = true; }
                        }
                        catch { }
                        rfiResults.Add(c);
                    }
                }
            }
            catch { }
            rfiResultsReady = true;
        }

        private static void BuildRfiQueue(float now)
        {
            PruneVisited(false);
            rfiQueue.Clear();
            var temp = new List<RfiCandidate>();
            for (int i = 0; i < rfiResults.Count; i++)
            {
                RfiCandidate c = rfiResults[i];
                if (c.GameId == 0 || rfiVisited.ContainsKey(c.GameId)) continue;
                if (c.Max != rfiTargetMax || c.Imps != rfiTargetImps) continue;
                if (c.ModeKnown && c.Hns != rfiTargetHns) continue;
                if (rejoinForImpostorSameMap && c.Map != rfiTargetMap) continue;
                int min = Mathf.Max(1, Mathf.Min(rfiTargetMax - rejoinForImpostorFillMargin, RfiMinActualPlayers));
                if (c.Players < min || c.Players >= c.Max) continue;
                temp.Add(c);
            }
            temp.Sort((a, b) => b.Players.CompareTo(a.Players)); // fullest first
            for (int i = 0; i < temp.Count && i < 8; i++) rfiQueue.Add(temp[i]);
        }

        private static void PruneVisited(bool all)
        {
            if (all) { rfiVisited.Clear(); return; }
            float now = Time.unscaledTime;
            List<int> old = null;
            foreach (var kv in rfiVisited)
                if (now - kv.Value > RfiVisitedForgetSeconds) (old ??= new List<int>()).Add(kv.Key);
            if (old != null) foreach (int k in old) rfiVisited.Remove(k);
        }

        // ---------------------------------------------------------------- helpers

        private static bool IsHnsMode(GameModes mode)
        {
            return mode == GameModes.HideNSeek || mode == GameModes.SeekFools;
        }

        private static bool IsWantedImpostorRole(PlayerControl lp, RoleTypes role)
        {
            if (rejoinForImpostorStrict) return role == RoleTypes.Impostor;
            try
            {
                RoleBehaviour rb = lp.Data.Role;
                if (rb != null && (rb.IsImpostor || rb.TeamType == RoleTeamTypes.Impostor)) return true;
            }
            catch { }
            return IsImpostorTeamRole(role);
        }

        private static string RoleNameForRfi(RoleTypes role)
        {
            try { return GetLocalRoleDisplayName(role); } catch { return role.ToString(); }
        }

        private static string SafeRoomName(int code)
        {
            if (code == 0) return "lobby";
            try { return GameCode.IntToGameName(code); } catch { return code.ToString(); }
        }

        private static string RfiMapName(int map)
        {
            switch (map)
            {
                case 0: return "Skeld";
                case 1: return "Mira";
                case 2: return "Polus";
                case 3: return "Dleks";
                case 4: return "Airship";
                case 5: return "Fungle";
                default: return "map " + map;
            }
        }

        // ---------------------------------------------------------------- UI

        private void DrawRfiStepper(string label, ref int value, int min, int max, int step, string display)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, toggleLabelStyle, GUILayout.Width(110f), GUILayout.Height(20f));
            if (GUILayout.Button("-", btnStyle, GUILayout.Width(22f), GUILayout.Height(20f)))
            { value = Mathf.Max(min, value - step); SaveRejoinForImpostorPrefs(); }
            GUILayout.Label(display, accentValueStyle, GUILayout.Width(64f), GUILayout.Height(20f));
            if (GUILayout.Button("+", btnStyle, GUILayout.Width(22f), GUILayout.Height(20f)))
            { value = Mathf.Min(max, value + step); SaveRejoinForImpostorPrefs(); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawRejoinForImpostor(float width)
        {
            LoadRejoinForImpostorPrefs();
            int toggleW = Mathf.RoundToInt(Mathf.Clamp(width, 170f, 280f));
            GUILayout.Space(4);

            bool was = rejoinForImpostor;
            rejoinForImpostor = DrawToggle(rejoinForImpostor, L("Rejoin Until Impostor", "Перезаходить до импостера"), toggleW, "rejoinForImpostor");
            if (rejoinForImpostor && !was)
            {
                StartRejoinForImpostor();
                ShowNotification("<color=#FF6B6B>[IMPOSTOR]</color> Lobby hopping ON.");
            }
            else if (!rejoinForImpostor && was)
            {
                rfiPhase = RfiPhase.Idle;
                rfiState = "Off";
            }

            bool strict = DrawToggle(rejoinForImpostorStrict, L("Plain Impostor only", "Только обычный импостер"), toggleW, "rejoinForImpostorStrict");
            if (strict != rejoinForImpostorStrict) { rejoinForImpostorStrict = strict; SaveRejoinForImpostorPrefs(); }
            bool sameMap = DrawToggle(rejoinForImpostorSameMap, L("Same map only", "Только та же карта"), toggleW, "rejoinForImpostorSameMap");
            if (sameMap != rejoinForImpostorSameMap) { rejoinForImpostorSameMap = sameMap; SaveRejoinForImpostorPrefs(); }

            int minPlayers = Mathf.Max(1, (rfiTargetSet ? rfiTargetMax : 15) - rejoinForImpostorFillMargin);
            DrawRfiStepper(L("Min players", "Мин. игроков"), ref rejoinForImpostorFillMargin, 0, 10, 1,
                "max-" + rejoinForImpostorFillMargin + " (" + minPlayers + ")");
            DrawRfiStepper(L("Lobby wait", "Ожидание лобби"), ref rejoinForImpostorLobbyWait, 0, 600, 30,
                rejoinForImpostorLobbyWait == 0 ? "Wait for role screen" : rejoinForImpostorLobbyWait + "s");
            DrawRfiStepper(L("Max lobbies", "Макс. лобби"), ref rejoinForImpostorMaxTries, 0, 99, 1,
                rejoinForImpostorMaxTries == 0 ? L("No limit", "Без лимита") : rejoinForImpostorMaxTries.ToString());

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Leave delay", "Задержка выхода") + " " + rejoinForImpostorLeaveDelay.ToString("0.0") + "s",
                toggleLabelStyle, GUILayout.Width(110f), GUILayout.Height(20f));
            float d = ThemedSlider(rejoinForImpostorLeaveDelay, 0f, 5f, GUILayout.Width(Mathf.Max(60f, toggleW - 120f)));
            d = Mathf.Round(d * 10f) / 10f;
            if (Mathf.Abs(d - rejoinForImpostorLeaveDelay) > 0.001f) { rejoinForImpostorLeaveDelay = d; SaveRejoinForImpostorPrefs(); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(RfiTargetText, menuDescStyle);
            GUILayout.Label(L("Status: ", "Статус: ") + RejoinForImpostorStatus, menuDescStyle);
            GUILayout.Label(L("Start it inside a lobby: it copies that lobby's settings, then hops between nearly full public lobbies with the same settings until you get impostor.",
                              "Запустите в лобби: настройки копируются, затем перебираются почти полные публичные лобби с теми же настройками, пока не выпадет импостер."),
                            menuDescStyle);
            GUILayout.Label(L("While hopping, your Find Game impostor/map filters are set to the target; your own filters come back when it stops.",
                              "Во время поиска фильтры Find Game (импостеры/карта) меняются на цель; ваши фильтры вернутся после остановки."),
                            menuDescStyle);
        }
    }

    // Forwards the Find Game screen's lobby list to the hopper while it is searching.
    [HarmonyLib.HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.HandleList))]
    public static class RfiHandleListPatch
    {
        public static void Postfix(HttpMatchmakerManager.FindGamesListFilteredResponse response)
        {
            try
            {
                if (AeroMenuGUI.RfiWantsGameList) AeroMenuGUI.ReceiveRfiGameList(response);
            }
            catch { }
        }
    }
}
