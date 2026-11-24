#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using InnerNet;
using AmongUs.GameOptions;
using BepInEx;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Group F state (exact field names required by the contract)
        // --------------------------------------------------------------------

        // 26 - Copy room code to clipboard on join.
        public static bool extraAutoCopyCodeOnJoin;

        // 27 - Screenshot hotkey.
        public static KeyCode extraScreenshotKey = KeyCode.F12;

        // 28 - Quick-chat macros + their send keys.
        public static string extraMacro1 = string.Empty;
        public static string extraMacro2 = string.Empty;
        public static string extraMacro3 = string.Empty;
        public static KeyCode extraMacroSendKey1 = KeyCode.None;
        public static KeyCode extraMacroSendKey2 = KeyCode.None;
        public static KeyCode extraMacroSendKey3 = KeyCode.None;

        // 29 - Config backup button.
        public static bool extraBackupConfig;

        // 30 - Auto rejoin after an unexpected disconnect.
        public static bool extraAutoRejoin;

        // --------------------------------------------------------------------
        // Private working state (all suffixed with F)
        // --------------------------------------------------------------------

        private const BindingFlags AllBindingsF =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        // Join / disconnect tracking.
        private static int trackedGameIdF = int.MinValue;
        private static int lastValidGameIdF;
        private static bool rejoinScheduledF;
        private static int rejoinAttemptsF;
        private static int rejoinExhaustedCodeF;
        private static float nextRejoinAttemptAtF;
        private static float rejoinCooldownUntilF;

        // Key-binding capture flags (handled from DrawExtrasF via Event.current).
        private static bool waitScreenshotKeyF;
        private static bool waitMacroKey1F;
        private static bool waitMacroKey2F;
        private static bool waitMacroKey3F;

        // ====================================================================
        // Required public surface
        // ====================================================================

        internal void DrawExtrasF(float width)
        {
            int toggleW = Mathf.Clamp(Mathf.RoundToInt(width > 0f ? width : 260f), 170, 460);
            GUIStyle buttonStyle = btnStyle ?? GUI.skin.button;
            GUIStyle fieldStyle = inputBlockStyle ?? GUI.skin.textField;

            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                DrawMenuSectionHeader(L("UTILITIES", "УТИЛИТЫ"));

                // 26 - Auto copy room code on join -----------------------------
                bool autoCopy = DrawToggle(extraAutoCopyCodeOnJoin,
                    L("Copy Room Code On Join", "Копировать код комнаты при входе"), toggleW, "extrasAutoCopyCodeOnJoin");
                if (autoCopy != extraAutoCopyCodeOnJoin) extraAutoCopyCodeOnJoin = autoCopy;
                GUILayout.Label(L("Copies the new room code to the clipboard whenever you join a game.",
                                  "Копирует код новой комнаты в буфер обмена при каждом входе в игру."), menuDescStyle);
                if (GUILayout.Button(L("Copy Current Code Now", "Скопировать текущий код"), buttonStyle, GUILayout.Height(24)))
                    AutoCopyCodeF();
                GUILayout.Space(8);

                // 27 - Screenshot hotkey -------------------------------------
                DrawMenuSectionHeader(L("SCREENSHOT", "СКРИНШОТ"));
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Screenshot key:", "Клавиша скриншота:"), GUILayout.Width(160));
                string shotText = waitScreenshotKeyF ? MenuText("Press any key...") : KeyLabelF(extraScreenshotKey);
                if (GUILayout.Button(shotText, waitScreenshotKeyF ? activeTabStyle : buttonStyle, GUILayout.Width(120), GUILayout.Height(24)))
                {
                    ClearBindWaitsF();
                    waitScreenshotKeyF = true;
                }
                if (GUILayout.Button(MenuText("Clear"), buttonStyle, GUILayout.Width(56), GUILayout.Height(24)))
                {
                    extraScreenshotKey = KeyCode.None;
                    waitScreenshotKeyF = false;
                    SaveExtrasF();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (GUILayout.Button(L("Take Screenshot Now", "Сделать скриншот"), buttonStyle, GUILayout.Height(24)))
                    TakeScreenshotF();
                GUILayout.Label(L("Saves a PNG into the AeroMenu folder.",
                                  "Сохраняет PNG в папку AeroMenu."), menuDescStyle);
                GUILayout.Space(8);

                // 28 - Quick chat macros -------------------------------------
                DrawMenuSectionHeader(L("QUICK CHAT MACROS", "МАКРОСЫ ЧАТА"));
                DrawMacroRowF("1", ref extraMacro1, ref extraMacroSendKey1, ref waitMacroKey1F, fieldStyle, buttonStyle, toggleW);
                DrawMacroRowF("2", ref extraMacro2, ref extraMacroSendKey2, ref waitMacroKey2F, fieldStyle, buttonStyle, toggleW);
                DrawMacroRowF("3", ref extraMacro3, ref extraMacroSendKey3, ref waitMacroKey3F, fieldStyle, buttonStyle, toggleW);
                GUILayout.Label(L("Type a message, assign a key, then press that key in game to send it to chat.",
                                  "Введите сообщение, назначьте клавишу и нажмите её в игре, чтобы отправить его в чат."), menuDescStyle);
                GUILayout.Space(8);

                // 29 - Config backup -----------------------------------------
                DrawMenuSectionHeader(L("CONFIG BACKUP", "РЕЗЕРВНАЯ КОПИЯ"));
                bool backup = DrawToggle(extraBackupConfig,
                    L("Enable Config Backup Button", "Кнопка резервной копии конфига"), toggleW, "extrasBackupConfig");
                if (backup != extraBackupConfig) extraBackupConfig = backup;
                if (GUILayout.Button(L("Backup AeroMenu.cfg Now", "Скопировать AeroMenu.cfg"), buttonStyle, GUILayout.Height(24)))
                    BackupConfigF();
                GUILayout.Label(L("Creates a timestamped .bak file in the AeroMenu folder.",
                                  "Создаёт .bak файл с меткой времени в папке AeroMenu."), menuDescStyle);
                GUILayout.Space(8);

                // 30 - Auto rejoin -------------------------------------------
                DrawMenuSectionHeader(L("AUTO REJOIN", "АВТО-ПЕРЕХОД"));
                bool autoRejoin = DrawToggle(extraAutoRejoin,
                    L("Auto Rejoin On Disconnect", "Авто-переход при отключении"), toggleW, "extrasAutoRejoin");
                if (autoRejoin != extraAutoRejoin)
                {
                    extraAutoRejoin = autoRejoin;
                    if (!extraAutoRejoin)
                    {
                        rejoinScheduledF = false;
                        rejoinAttemptsF = 0;
                    }
                }
                GUILayout.Label(L("Rejoins the last room after an unexpected disconnect. Limited attempts prevent loops.",
                                  "Повторно подключается к последней комнате после внезапного отключения. Ограничение попыток защищает от циклов."), menuDescStyle);

                // Pending status line.
                if (rejoinScheduledF)
                    GUILayout.Label(MenuText(L("Status: rejoin pending...", "Статус: ожидание перехода...")), menuBadgeStyle);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        internal static void TickExtrasF(bool inputBlocked)
        {
            // Track game id changes for both auto-copy and auto-rejoin. This runs
            // regardless of inputBlocked because it is not player input.
            try
            {
                int gid = GetGameIdF();
                if (gid != trackedGameIdF)
                {
                    int previous = trackedGameIdF;
                    trackedGameIdF = gid;

                    if (gid > 0)
                    {
                        // Joined a (new) room.
                        lastValidGameIdF = gid;
                        rejoinAttemptsF = 0;
                        rejoinScheduledF = false;

                        // A different room clears the anti-loop lock.
                        if (rejoinExhaustedCodeF != 0 && rejoinExhaustedCodeF != gid)
                            rejoinExhaustedCodeF = 0;

                        if (extraAutoCopyCodeOnJoin)
                            AutoCopyCodeF();
                    }
                    else if (gid == 0 && previous > 0)
                    {
                        // Unexpected disconnect from a room we were actually in.
                        lastValidGameIdF = previous;
                        if (extraAutoRejoin && previous != rejoinExhaustedCodeF &&
                            Time.unscaledTime >= rejoinCooldownUntilF)
                        {
                            rejoinScheduledF = true;
                            rejoinAttemptsF = 0;
                            nextRejoinAttemptAtF = Time.unscaledTime + 2.5f;
                            ShowNotification(L("<color=#FFAA00>[AUTO REJOIN]</color> Disconnect detected. Rejoin scheduled.",
                                               "<color=#FFAA00>[AUTO-ПЕРЕХОД]</color> Отключение обнаружено. Переход запланирован."));
                        }
                    }
                }
            }
            catch { }

            // Delayed, heavily guarded rejoin attempt.
            try
            {
                if (extraAutoRejoin && rejoinScheduledF && GetGameIdF() == 0 &&
                    Time.unscaledTime >= nextRejoinAttemptAtF)
                {
                    if (rejoinAttemptsF >= 2 || lastValidGameIdF <= 0 ||
                        lastValidGameIdF == rejoinExhaustedCodeF)
                    {
                        rejoinScheduledF = false;
                        rejoinExhaustedCodeF = lastValidGameIdF;
                        rejoinCooldownUntilF = Time.unscaledTime + 10f;
                    }
                    else
                    {
                        rejoinAttemptsF++;
                        nextRejoinAttemptAtF = Time.unscaledTime + 3f;
                        bool ok = TryRejoinF(lastValidGameIdF);
                        if (ok)
                        {
                            rejoinScheduledF = false;
                            rejoinCooldownUntilF = Time.unscaledTime + 8f;
                            ShowNotification(L("<color=#00FFAA>[AUTO REJOIN]</color> Rejoining room.",
                                               "<color=#00FFAA>[AUTO-ПЕРЕХОД]</color> Повторное подключение."));
                        }
                        else if (rejoinAttemptsF >= 2)
                        {
                            rejoinScheduledF = false;
                            rejoinExhaustedCodeF = lastValidGameIdF;
                            rejoinCooldownUntilF = Time.unscaledTime + 10f;
                            ShowNotification(L("<color=#FF4444>[AUTO REJOIN]</color> Failed after 2 attempts.",
                                               "<color=#FF4444>[AUTO-ПЕРЕХОД]</color> Не удалось после 2 попыток."));
                        }
                    }
                }
            }
            catch { }

            // Keyboard hotkeys are ignored while the menu blocks input or while
            // we are capturing a new binding.
            bool capturing = waitScreenshotKeyF || waitMacroKey1F || waitMacroKey2F || waitMacroKey3F;
            if (inputBlocked || capturing) return;

            try
            {
                if (extraScreenshotKey != KeyCode.None && Input.GetKeyDown(extraScreenshotKey))
                    TakeScreenshotF();

                if (extraMacroSendKey1 != KeyCode.None && Input.GetKeyDown(extraMacroSendKey1))
                    SendMacroChatF(extraMacro1);
                if (extraMacroSendKey2 != KeyCode.None && Input.GetKeyDown(extraMacroSendKey2))
                    SendMacroChatF(extraMacro2);
                if (extraMacroSendKey3 != KeyCode.None && Input.GetKeyDown(extraMacroSendKey3))
                    SendMacroChatF(extraMacro3);
            }
            catch { }
        }

        internal static void LoadExtrasF()
        {
            try
            {
                extraAutoCopyCodeOnJoin = LoadBool("M_ExtrasF_extraAutoCopyCodeOnJoin", false);

                extraScreenshotKey = (KeyCode)PlayerPrefs.GetInt("M_ExtrasF_extraScreenshotKey", (int)KeyCode.F12);

                extraMacro1 = PlayerPrefs.GetString("M_ExtrasF_extraMacro1", string.Empty) ?? string.Empty;
                extraMacro2 = PlayerPrefs.GetString("M_ExtrasF_extraMacro2", string.Empty) ?? string.Empty;
                extraMacro3 = PlayerPrefs.GetString("M_ExtrasF_extraMacro3", string.Empty) ?? string.Empty;

                extraMacroSendKey1 = (KeyCode)PlayerPrefs.GetInt("M_ExtrasF_extraMacroSendKey1", (int)KeyCode.None);
                extraMacroSendKey2 = (KeyCode)PlayerPrefs.GetInt("M_ExtrasF_extraMacroSendKey2", (int)KeyCode.None);
                extraMacroSendKey3 = (KeyCode)PlayerPrefs.GetInt("M_ExtrasF_extraMacroSendKey3", (int)KeyCode.None);

                extraBackupConfig = LoadBool("M_ExtrasF_extraBackupConfig", false);
                extraAutoRejoin = LoadBool("M_ExtrasF_extraAutoRejoin", false);

                // Start tracking from a clean slate.
                trackedGameIdF = int.MinValue;
                lastValidGameIdF = 0;
                rejoinScheduledF = false;
                rejoinAttemptsF = 0;
                rejoinExhaustedCodeF = 0;
                rejoinCooldownUntilF = 0f;
                nextRejoinAttemptAtF = 0f;
                ClearBindWaitsF();
            }
            catch { }
        }

        internal static void SaveExtrasF()
        {
            try
            {
                SaveBool("M_ExtrasF_extraAutoCopyCodeOnJoin", extraAutoCopyCodeOnJoin);
                PlayerPrefs.SetInt("M_ExtrasF_extraScreenshotKey", (int)extraScreenshotKey);

                PlayerPrefs.SetString("M_ExtrasF_extraMacro1", extraMacro1 ?? string.Empty);
                PlayerPrefs.SetString("M_ExtrasF_extraMacro2", extraMacro2 ?? string.Empty);
                PlayerPrefs.SetString("M_ExtrasF_extraMacro3", extraMacro3 ?? string.Empty);

                PlayerPrefs.SetInt("M_ExtrasF_extraMacroSendKey1", (int)extraMacroSendKey1);
                PlayerPrefs.SetInt("M_ExtrasF_extraMacroSendKey2", (int)extraMacroSendKey2);
                PlayerPrefs.SetInt("M_ExtrasF_extraMacroSendKey3", (int)extraMacroSendKey3);

                SaveBool("M_ExtrasF_extraBackupConfig", extraBackupConfig);
                SaveBool("M_ExtrasF_extraAutoRejoin", extraAutoRejoin);
            }
            catch { }
        }

        internal static void DrawExtrasFOverlay()
        {
            // Capture here instead of inside DrawExtrasF: that section is culled
            // when scrolled out of view, which would leave a pending bind stuck
            // (and every hotkey blocked) until the section became visible again.
            if (showMenu) HandleBindCaptureF();
        }

        // ====================================================================
        // UI helpers (suffix F)
        // ====================================================================

        private static void ClearBindWaitsF()
        {
            waitScreenshotKeyF = false;
            waitMacroKey1F = false;
            waitMacroKey2F = false;
            waitMacroKey3F = false;
        }

        private static string KeyLabelF(KeyCode key)
        {
            return key == KeyCode.None ? "NONE" : key.ToString();
        }

        private static void HandleBindCaptureF()
        {
            try
            {
                Event e = Event.current;
                if (e == null || !e.isKey || e.type != EventType.KeyDown) return;

                if (e.keyCode == KeyCode.Escape)
                {
                    if (waitScreenshotKeyF || waitMacroKey1F || waitMacroKey2F || waitMacroKey3F)
                    {
                        ClearBindWaitsF();
                        e.Use();
                    }
                    return;
                }

                if (e.keyCode == KeyCode.None) return;

                if (waitScreenshotKeyF)
                {
                    extraScreenshotKey = e.keyCode;
                    waitScreenshotKeyF = false;
                    SaveExtrasF();
                    e.Use();
                }
                else if (waitMacroKey1F)
                {
                    extraMacroSendKey1 = e.keyCode;
                    waitMacroKey1F = false;
                    SaveExtrasF();
                    e.Use();
                }
                else if (waitMacroKey2F)
                {
                    extraMacroSendKey2 = e.keyCode;
                    waitMacroKey2F = false;
                    SaveExtrasF();
                    e.Use();
                }
                else if (waitMacroKey3F)
                {
                    extraMacroSendKey3 = e.keyCode;
                    waitMacroKey3F = false;
                    SaveExtrasF();
                    e.Use();
                }
            }
            catch { }
        }

        private void DrawMacroRowF(string index, ref string text, ref KeyCode key, ref bool waiting,
                                   GUIStyle fieldStyle, GUIStyle buttonStyle, int toggleW)
        {
            GUILayout.BeginVertical();
            try
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("M" + index + ":", GUILayout.Width(30));
                string typed = AeroTextField(text ?? string.Empty, fieldStyle,
                    GUILayout.Width(Mathf.Max(140f, toggleW - 180f)), GUILayout.Height(22));
                if (!string.Equals(typed, text, StringComparison.Ordinal))
                    text = typed ?? string.Empty;
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                string keyText = waiting ? MenuText("Press any key...") : KeyLabelF(key);
                if (GUILayout.Button(keyText, waiting ? activeTabStyle : buttonStyle, GUILayout.Width(120), GUILayout.Height(22)))
                {
                    ClearBindWaitsF();
                    waiting = true;
                }
                if (GUILayout.Button(MenuText("Clear"), buttonStyle, GUILayout.Width(56), GUILayout.Height(22)))
                {
                    key = KeyCode.None;
                    waiting = false;
                    SaveExtrasF();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (GUILayout.Button(L("Send Macro " + index + " Now", "Отправить макрос " + index),
                                     buttonStyle, GUILayout.Height(22)))
                    SendMacroChatF(text);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        // ====================================================================
        // Feature helpers (suffix F)
        // ====================================================================

        private static int GetGameIdF()
        {
            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                return client == null ? 0 : client.GameId;
            }
            catch { return 0; }
        }

        // 26 - Copy room code using direct Unity API (no reflection needed).
        private static void AutoCopyCodeF()
        {
            try
            {
                string code = GetCurrentRoomCodeForStatus();
                if (string.IsNullOrWhiteSpace(code) || string.Equals(code, "No room", StringComparison.OrdinalIgnoreCase))
                {
                    ShowNotification(L("<color=#FFAA00>[UTIL]</color> No room code available.",
                                       "<color=#FFAA00>[УТИЛИТА]</color> Код комнаты недоступен."));
                    return;
                }

                GUIUtility.systemCopyBuffer = code;
                ShowNotification(L("<color=#00FFAA>[UTIL]</color> Copied room code: ", "<color=#00FFAA>[УТИЛИТА]</color> Скопирован код: ") + "<b>" + code + "</b>");
            }
            catch { }
        }

        // 27 - Screenshot into the AeroMenu folder.
        private static void TakeScreenshotF()
        {
            try
            {
                string folder = Plugin.AeroFolder;
                if (string.IsNullOrWhiteSpace(folder)) folder = Directory.GetCurrentDirectory();
                try { if (!Directory.Exists(folder)) Directory.CreateDirectory(folder); } catch { }

                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                string file = Path.Combine(folder, "AeroMenu_Screenshot_" + stamp + ".png");
                ScreenCapture.CaptureScreenshot(file);
                ShowNotification(L("<color=#00FFAA>[UTIL]</color> Screenshot saved.", "<color=#00FFAA>[УТИЛИТА]</color> Скриншот сохранён."));
            }
            catch { }
        }

        // 28 - Send a macro through ChatController.SendFreeChat via reflection,
        // with a direct RpcSendChat fallback.
        private static void SendMacroChatF(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            string text = message.Trim();
            bool sent = false;

            try
            {
                HudManager hud = HudManager.Instance;
                ChatController chat = hud == null ? null : hud.Chat;
                if (chat != null)
                {
                    Type type = chat.GetType();

                    FieldInfo fieldInfo = type.GetField("freeChatField", AllBindingsF);
                    object freeChat = fieldInfo == null ? null : fieldInfo.GetValue(chat);
                    if (freeChat != null)
                    {
                        Type fieldType = freeChat.GetType();
                        PropertyInfo textProp = fieldType.GetProperty("Text", AllBindingsF);
                        if (textProp != null && textProp.CanWrite)
                        {
                            textProp.SetValue(freeChat, text, null);
                        }
                        else
                        {
                            FieldInfo textAreaField = fieldType.GetField("textArea", AllBindingsF);
                            object textArea = textAreaField == null ? null : textAreaField.GetValue(freeChat);
                            if (textArea != null)
                            {
                                MethodInfo setText = textArea.GetType().GetMethod("SetText", AllBindingsF,
                                    null, new[] { typeof(string), typeof(string) }, null);
                                if (setText != null) setText.Invoke(textArea, new object[] { text, text });
                            }
                        }
                    }

                    MethodInfo send = type.GetMethod("SendFreeChat", AllBindingsF);
                    if (send != null && send.GetParameters().Length == 0)
                    {
                        send.Invoke(chat, null);
                        sent = true;
                    }
                }
            }
            catch { }

            if (!sent)
            {
                try
                {
                    PlayerControl local = PlayerControl.LocalPlayer;
                    if (local != null) local.RpcSendChat(text);
                }
                catch { }
            }
        }

        // 29 - Backup AeroMenu.cfg with a timestamped .bak name.
        private static void BackupConfigF()
        {
            try
            {
                string folder = Plugin.AeroFolder;
                if (string.IsNullOrWhiteSpace(folder)) folder = Directory.GetCurrentDirectory();
                string source = Path.Combine(folder, "AeroMenu.cfg");
                if (!File.Exists(source))
                {
                    ShowNotification(L("<color=#FFAA00>[BACKUP]</color> AeroMenu.cfg not found.",
                                       "<color=#FFAA00>[РЕЗЕРВ]</color> AeroMenu.cfg не найден."));
                    return;
                }

                try { if (!Directory.Exists(folder)) Directory.CreateDirectory(folder); } catch { }

                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string destination = Path.Combine(folder, "AeroMenu_" + stamp + ".cfg.bak");
                File.Copy(source, destination, true);
                ShowNotification(L("<color=#00FFAA>[BACKUP]</color> Saved: ", "<color=#00FFAA>[РЕЗЕРВ]</color> Сохранено: ") +
                                 "<b>" + Path.GetFileName(destination) + "</b>");
            }
            catch { }
        }

        // 30 - Rejoin the remembered room using reflection on the game client.
        private static bool TryRejoinF(int code)
        {
            if (code <= 0) return false;

            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                if (client == null) return false;
                Type type = client.GetType();

                // Restore the remembered game id first.
                PropertyInfo gp = type.GetProperty("GameId", AllBindingsF);
                if (gp != null && gp.CanWrite)
                {
                    gp.SetValue(client, code, null);
                }
                else
                {
                    FieldInfo gf = type.GetField("GameId", AllBindingsF);
                    if (gf != null) gf.SetValue(client, code);
                }

                // Preferred path: JoinGame(int).
                MethodInfo join = type.GetMethod("JoinGame", AllBindingsF, null, new[] { typeof(int) }, null);
                if (join == null)
                {
                    MethodInfo[] methods = type.GetMethods(AllBindingsF);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        if (methods[i].Name == "JoinGame" && methods[i].GetParameters().Length == 1)
                        {
                            join = methods[i];
                            break;
                        }
                    }
                }
                if (join != null)
                {
                    join.Invoke(client, new object[] { code });
                    return true;
                }
            }
            catch { }

            // Fallback: the coroutine helper used elsewhere in this project.
            try
            {
                AmongUsClient client = AmongUsClient.Instance;
                if (client == null) return false;
                client.GameId = code;
                var coroutine = client.CoJoinOnlineGameFromCode(code);
                if (coroutine != null)
                {
                    client.StartCoroutine(coroutine);
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}
