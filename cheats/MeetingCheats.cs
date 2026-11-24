#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Reflection;
using AmongUs.Data.Player;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private static bool TryOpenModdedMeeting(PlayerControl caller, NetworkedPlayerInfo reportedBody, string successMessage = null)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Modded meeting call is host only.");
                return false;
            }

            if (PlayerControl.LocalPlayer == null || ShipStatus.Instance == null || LobbyBehaviour.Instance != null || !AmongUsClient.Instance.IsGameStarted)
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Match must be started.");
                return false;
            }

            if (IsMeetingOrExileActive() || IntroCutscene.Instance != null)
            {
                ShowNotification("<color=#FFAA00>[MEETING]</color> Meeting/exile/intro is already active.");
                return false;
            }

            PlayerControl meetingCaller = caller ?? PlayerControl.LocalPlayer;
            if (meetingCaller == null || meetingCaller.Data == null || meetingCaller.Data.Disconnected)
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Meeting caller was not found.");
                return false;
            }

            if (meetingCaller.Data.IsDead)
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Meeting caller is dead.");
                return false;
            }

            try
            {
                meetingCaller.CmdReportDeadBody(reportedBody);

                if (!string.IsNullOrWhiteSpace(successMessage))
                    ShowNotification(successMessage);

                return true;
            }
            catch
            {
                ShowNotification("<color=#FF0000>[MEETING]</color> Failed to open modded meeting.");
                return false;
            }
        }

        // --------------------------------------------------------------------
        // Extra meeting tools - drawn inside the existing "MEETING TOOLS"
        // section (EXTRAS / H). No new top-level tab is introduced.
        //
        //   * Live Vote Tracker: local, read-only overlay that shows who each
        //     player is voting for while a meeting is in progress.
        //   * Meeting Quick Message: type a short line and send it straight to
        //     meeting chat without leaving the menu (which blocks chat input).
        // --------------------------------------------------------------------

        public static bool meetingLiveVoteTracker = false;

        private const int MeetingVoteTrackMaxRows = 15;
        private const int MeetingQuickMessageMaxChars = 200;

        private static readonly BindingFlags MeetingVoteFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static string meetingQuickMessage = string.Empty;

        private static Texture2D meetingVoteBgTex;
        private static GUIStyle meetingVotePanelStyle;
        private static GUIStyle meetingVoteRowStyle;

        // Called from DrawExtrasH, inside the existing MEETING TOOLS card.
        internal void DrawMeetingQuickTools(float width)
        {
            int toggleW = Mathf.Clamp(Mathf.RoundToInt(width > 0f ? width : 260f), 170, 460);
            GUIStyle fieldStyle = inputBlockStyle ?? GUI.skin.textField;

            bool tracker = DrawToggle(meetingLiveVoteTracker,
                L("Live Vote Tracker", "Живой учёт голосов"), toggleW, "meetingLiveVoteTracker");
            if (tracker != meetingLiveVoteTracker)
            {
                meetingLiveVoteTracker = tracker;
                settingsDirty = true;
            }
            GUILayout.Label(L("Shows each player's current vote while a meeting is open. Local overlay only.",
                              "Показывает текущий голос каждого игрока во время собрания. Только локальный оверлей."), menuDescStyle);
            GUILayout.Space(6);

            GUILayout.Label(L("Meeting quick message", "Быстрое сообщение на собрании"), menuDescStyle);
            string typed = AeroTextField(meetingQuickMessage ?? string.Empty, fieldStyle, GUILayout.Height(24));
            if (!string.Equals(typed, meetingQuickMessage, StringComparison.Ordinal))
                meetingQuickMessage = typed ?? string.Empty;
            GUILayout.Space(4);

            if (DrawFixedMenuButton(L("SEND TO MEETING CHAT", "ОТПРАВИТЬ В ЧАТ СОБРАНИЯ"),
                                    btnStyle, Mathf.Min(toggleW, 240f), 24f))
            {
                if (TrySendMeetingQuickMessage(meetingQuickMessage))
                    meetingQuickMessage = string.Empty;
            }
            GUILayout.Label(L("Requires an open meeting. Uses the same sanitised chat path as the portable chat.",
                              "Требует открытого собрания. Использует тот же очищенный путь чата, что и портативный чат."), menuDescStyle);
        }

        private bool TrySendMeetingQuickMessage(string raw)
        {
            if (MeetingHud.Instance == null)
            {
                ShowNotification("<color=#FFAA00>[MEETING]</color> Open a meeting first.");
                return false;
            }

            string text = (raw ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text))
            {
                ShowNotification("<color=#FFAA00>[MEETING]</color> Type a message first.");
                return false;
            }
            if (text.Length > MeetingQuickMessageMaxChars)
                text = text.Substring(0, MeetingQuickMessageMaxChars);

            // Reuse the portable chat sender so commands, whispers and the chat
            // history behave exactly as they do in the Chat tab.
            return TrySendCustomChatMessage(text);
        }

        // Called from DrawExtrasHOverlay (OnGUI pass).
        internal static void DrawMeetingQuickToolsOverlay()
        {
            if (!meetingLiveVoteTracker || MeetingHud.Instance == null) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            try
            {
                if (meetingVotePanelStyle == null || meetingVoteRowStyle == null)
                    CreateMeetingVoteStyles();

                FieldInfo statesField = MeetingHud.Instance.GetType().GetField("playerStates", MeetingVoteFlags);
                object statesValue = statesField == null ? null : statesField.GetValue(MeetingHud.Instance);
                System.Collections.IEnumerable states = statesValue as System.Collections.IEnumerable;
                if (states == null) return;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("<color=#00FFAA>LIVE VOTES</color>\n");

                int rows = 0;
                foreach (object state in states)
                {
                    if (state == null) continue;
                    if (rows >= MeetingVoteTrackMaxRows) break;

                    Type stateType = state.GetType();
                    object idValue = GetMeetingVoteValue(state, stateType, new[] { "PlayerId", "playerId" });
                    if (idValue == null) continue;

                    byte voterId;
                    try { voterId = Convert.ToByte(idValue); } catch { continue; }

                    string voterName = GetMeetingPlayerName(voterId);
                    if (string.IsNullOrEmpty(voterName)) voterName = "P" + voterId;

                    string target;
                    object voteFor = GetMeetingVoteValue(state, stateType,
                        new[] { "VotedForId", "votedForId", "VotedFor", "votedFor" });
                    if (voteFor == null)
                    {
                        object didVote = GetMeetingVoteValue(state, stateType,
                            new[] { "DidVote", "didVote", "HasVoted", "hasVoted" });
                        target = (didVote is bool voted && voted) ? "?" : "waiting";
                    }
                    else
                    {
                        byte votedFor;
                        try { votedFor = Convert.ToByte(voteFor); } catch { votedFor = byte.MaxValue; }

                        if (votedFor == PlayerVoteArea.SkippedVote) target = "SKIP";
                        else if (votedFor == byte.MaxValue) target = "waiting";
                        else
                        {
                            string name = GetMeetingPlayerName(votedFor);
                            target = string.IsNullOrEmpty(name) ? ("P" + votedFor) : name;
                        }
                    }

                    sb.Append(voterName).Append("  ->  ").Append(target);
                    if (rows + 1 < MeetingVoteTrackMaxRows) sb.Append('\n');
                    rows++;
                }

                if (rows == 0) return;

                const float w = 252f;
                float h = 26f + rows * 16f + 8f;
                Rect r = new Rect(Screen.width - w - 10f, 10f, w, h);

                GUI.Box(r, GUIContent.none, meetingVotePanelStyle);
                GUI.Label(r, sb.ToString(), meetingVoteRowStyle);
            }
            catch { }
        }

        private static object GetMeetingVoteValue(object target, Type type, string[] names)
        {
            try
            {
                if (target == null || type == null || names == null) return null;
                for (int i = 0; i < names.Length; i++)
                {
                    FieldInfo field = type.GetField(names[i], MeetingVoteFlags);
                    if (field != null) return field.GetValue(target);

                    PropertyInfo property = type.GetProperty(names[i], MeetingVoteFlags);
                    if (property != null) return property.GetValue(target, null);
                }
            }
            catch { }
            return null;
        }

        private static string GetMeetingPlayerName(byte id)
        {
            try
            {
                if (GameData.Instance == null) return null;
                NetworkedPlayerInfo info = GameData.Instance.GetPlayerById(id);
                return info == null ? null : info.PlayerName;
            }
            catch { return null; }
        }

        private static void CreateMeetingVoteStyles()
        {
            try
            {
                if (meetingVoteBgTex == null)
                {
                    meetingVoteBgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    meetingVoteBgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
                    meetingVoteBgTex.Apply();
                    meetingVoteBgTex.hideFlags = HideFlags.HideAndDontSave;
                }

                if (meetingVotePanelStyle == null)
                {
                    meetingVotePanelStyle = new GUIStyle(GUIStyle.none);
                    meetingVotePanelStyle.normal.background = meetingVoteBgTex;
                    RectOffset panelBorder = new RectOffset();
                    panelBorder.left = panelBorder.right = panelBorder.top = panelBorder.bottom = 4;
                    meetingVotePanelStyle.border = panelBorder;
                }

                if (meetingVoteRowStyle == null)
                {
                    meetingVoteRowStyle = new GUIStyle(GUI.skin.label);
                    meetingVoteRowStyle.fontSize = 12;
                    meetingVoteRowStyle.wordWrap = false;
                    meetingVoteRowStyle.clipping = TextClipping.Clip;
                    meetingVoteRowStyle.normal.textColor = new Color(0.92f, 0.94f, 1f, 1f);
                    RectOffset rowPadding = new RectOffset();
                    rowPadding.left = 10; rowPadding.right = 8; rowPadding.top = 8; rowPadding.bottom = 6;
                    meetingVoteRowStyle.padding = rowPadding;
                    meetingVoteRowStyle.richText = true;
                }
            }
            catch { }
        }

        // Called from LoadExtrasH / SaveExtrasH so the settings persist like the
        // other meeting tools.
        internal static void LoadMeetingQuickTools()
        {
            try
            {
                meetingLiveVoteTracker = LoadBool("M_MeetingLiveVoteTracker", false);
                meetingQuickMessage = PlayerPrefs.GetString("M_MeetingQuickMessage", string.Empty) ?? string.Empty;
                if (meetingQuickMessage.Length > MeetingQuickMessageMaxChars)
                    meetingQuickMessage = meetingQuickMessage.Substring(0, MeetingQuickMessageMaxChars);
            }
            catch { }
        }

        internal static void SaveMeetingQuickTools()
        {
            try
            {
                SaveBool("M_MeetingLiveVoteTracker", meetingLiveVoteTracker);
                PlayerPrefs.SetString("M_MeetingQuickMessage", meetingQuickMessage ?? string.Empty);
            }
            catch { }
        }
    }
}
