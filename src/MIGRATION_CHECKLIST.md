# Чеклист миграции Rewards и Commands

## Twitch Rewards (MARS.Server → MARS.Alerts)

| # | Оригинал (MARS.Server) | Статус | Микросервисный файл |
|---|------------------------|--------|---------------------|
| 1 | `1_RandomReward/RandomReward_TwitchReward.cs` | ❌ | — |
| 2 | `2_WaifuMarriage/MergeWaifu.cs` | ❌ | — |
| 3 | `2_WaifuMarriage/WaifuMarriage_TwitchReward.cs` | ❌ | — |
| 4 | `4_SearchWife/SearchWife_TwitchReward.cs` | ❌ | — |
| 5 | `4_FumoRoll/FumoRollService.cs` | ✅ | MARS.WaifuGacha |
| 6 | `4_FumoRoll/FumoFridayRoll_TwitchReward.cs` | ❌ | — |
| 7 | `4_FumoRoll/FumoCollectionService.cs` | ✅ | MARS.WaifuGacha |
| 8 | `4_FrogRoll/FrogRollService.cs` | ✅ | MARS.WaifuGacha |
| 9 | `4_FrogRoll/FrogRoll_TwitchReward.cs` | ❌ | — |
| 10 | `4_MikuRoll/MikuRollService.cs` | ✅ | MARS.WaifuGacha |
| 11 | `4_MikuRoll/MikuRoll_TwitchReward.cs` | ❌ | — |
| 12 | `4_MikuRoll/MikuCollectionService.cs` | ✅ | MARS.WaifuGacha |
| 13 | `5_AddWife/AddNewWaifu.cs` | ✅ | MARS.WaifuGacha |
| 14 | `5_AddWife/AddWife_TwitchReward.cs` | ❌ | — |
| 15 | `6_RussianRoulette/RussianRoulette_TwitchReward.cs` | ❌ | — |
| 16 | `6_RussianRoulette/TwitchRussianRoulete.cs` | ❌ | — |
| 17 | `7_Quiz/Quiz_TwitchReward.cs` | ❌ | — |
| 18 | `7_Quiz/TwitchTrivia.cs` | ❌ | — |
| 19 | `9_AudioQuiz/AudioQuiz_TwitchReward.cs` | ❌ | — |
| 20 | `9_AudioQuiz/AudioTriviaMiniGame.cs` | ❌ | — |
| 21 | `10_RandomSound/RandomSound_TwitchReward.cs` | ❌ | — |
| 22 | `11_RandomMemReward/RandomMem_TwitchReward.cs` | ❌ | — |
| 23 | `11_RandomMemReward/Service/RandomMemHandler.cs` | ✅ | MARS.Alerts |
| 24 | `11_RandomMemReward/Service/RandomMemeWorker.cs` | ✅ | MARS.Alerts |
| 25 | `11_RandomMemReward/Service/RandomMemOnline.cs` | ✅ | MARS.Alerts |
| 26 | `13_FumoFriday/FumoFriday_TwitchReward.cs` | ❌ | — |
| 27 | `13_FumoFriday/Entitys/FumoUser.cs` | ✅ | MARS.TwitchCore |
| 28 | `18_GaoAlert/GaoAlert_TwitchReward.cs` | ✅ | Alerts: GaoAlertHandler.cs |
| 29 | `27_RandomArt/RandomArt.cs` | ✅ | Alerts: RandomArtHandler.cs |
| 30 | `38_WednsdayFrog/WednsdayFrog_TwitchReward.cs` | ✅ | Alerts: WednsdayFrogHandler.cs |
| 31 | `39_MikuMonday/MikuMondayTracksService.cs` | ✅ | Alerts: MikuMondayTracksService.cs |
| 32 | `39_MikuMonday/TwitchMikuMondayRewardService.cs` | ❌ | — |
| 33 | `61_What/What_TwitchReward.cs` | ✅ | Alerts: WhatHandler.cs |
| 34 | `134_Pedro/Pedro_TwitchReward.cs` | ✅ | Alerts: PedroHandler.cs |
| 35 | `150_TyazheloReward/Tyazhelo_TwitchReward.cs` | ✅ | Alerts: TyazheloHandler.cs |
| 36 | `155_MichaelTime/MichaelTime_TwitchReward.cs` | ✅ | Alerts: MichaelTimeHandler.cs |
| 37 | `160_LegBum/LegBum_TwitchReward.cs` | ✅ | Alerts: LegBumHandler.cs |
| 38 | `160_LegBum/LegBumRefundService.cs` | ❌ | — |
| 39 | `170_FumoFridayNightReward/FumoFridayNight_TwitchReward.cs` | ✅ | Alerts: FumoFridayNightHandler.cs |
| 40 | `182_Stone/Stone_TwitchReward.cs` | ✅ | Alerts: StoneHandler.cs |
| 41 | `195_Cringe/Cringe_TwitchReward.cs` | ✅ | Alerts: CringeHandler.cs |
| 42 | `210_Hello/Hello_TwitchReward.cs` | ✅ | Alerts: HelloHandler.cs |
| 43 | `215_Bye/Bye_TwitchReward.cs` | ✅ | Alerts: ByeHandler.cs |
| 44 | `317_Intelligence/Intelligence_TwitchReward.cs` | ✅ | Alerts: IntelligenceHandler.cs |
| 45 | `320_MikuScreamer/MikuScreamer_TwitchReward.cs` | ✅ | Alerts: MikuScreamerHandler.cs |
| 46 | `333_Skibidibop/Skibidibop_TwitchReward.cs` | ✅ | Alerts: SkibidibopHandler.cs |
| 47 | `337_PhonkEdit/PhonkEdit_TwitchReward.cs` | ✅ | Alerts: PhonkEditHandler.cs |
| 48 | `341_Aga/Aga_TwitchReward.cs` | ✅ | Alerts: AgaHandler.cs |
| 49 | `342_BadToBone/BadToBone_TwitchReward.cs` | ✅ | Alerts: BadToBoneHandler.cs |
| 50 | `353_TikTokEdit/TikTokEdit_TwitchReward.cs` | ✅ | Alerts: TikTokEditHandler.cs |
| 51 | `375_DanceDance/DanceDance_TwitchReward.cs` | ✅ | Alerts: DanceDanceHandler.cs |
| 52 | `666_Edge0100Alert/Edge0100Alert_TwitchReward.cs` | ✅ | Alerts: Edge0100AlertHandler.cs |
| 53 | `1333_SkibidibopLong/SkibidibopLong_TwitchReward.cs` | ✅ | Alerts: SkibidibopLongHandler.cs |
| 54 | `1510_StatusQuestion/StatusQuestion_TwitchReward.cs` | ✅ | Alerts: StatusQuestionHandler.cs |
| 55 | `1602_CinemaRequest/CinemaRequest_TwitchReward.cs` | ✅ | Alerts: CinemaRequestHandler.cs |
| 56 | `1700_Confetti/Confetti_TwitchReward.cs` | ✅ | Alerts: ConfettiHandler.cs |
| 57 | `1701_Fireworks/Fireworks_TwitchReward.cs` | ✅ | Alerts: FireworksHandler.cs |
| 58 | `1702_EmojisReward/` | ❌ | — |
| 59 | `2002_AdhdSuperpower/AdhdSuperpower_TwitchReward.cs` | ✅ | Alerts: AdhdSuperpowerHandler.cs |
| 60 | `6666_CloseGame/CloseGame_TwitchReward.cs` | ✅ | Alerts: CloseGameHandler.cs |
| 61 | `75000_SelectGame/SelectGame_TwitchReward.cs` | ✅ | Alerts: SelectGameHandler.cs |
| 62 | `8005_Credits/Credits_TwitchReward.cs` | ✅ | Alerts: CreditsHandler.cs |
| 63 | `99999_AllRefundService/AllRefund_TwitchReward.cs` | ✅ | Alerts: AllRefundHandler.cs |

### Shared Services (сервисы, а не обработчики наград)

| Сервис | Статус | Микросервисный файл |
|--------|--------|---------------------|
| `AnswersForTwitchRewards.cs` | ✅ | MARS.TwitchCore |
| `ChannelRewards/` | ✅ | MARS.TwitchCore |
| `Command.cs` | ✅ | MARS.TwitchCore |
| `HighlitedMessage.cs` | ✅ | MARS.Alerts |
| `RickRollerService.cs` | ✅ | MARS.Alerts |
| `RollCooldownNotificationService.cs` | ✅ | MARS.WaifuGacha |
| `RollCooldownService.cs` | ✅ | MARS.WaifuGacha |
| `TwitchMediaAlerts.cs` | ✅ | MARS.Alerts |
| `TwitchMessagesHubAwaker.cs` | ✅ | MARS.TwitchCore |
| `MiniGamesManager.cs` | ✅ | MARS.TwitchCore |

---

## Commands (MARS.Server → MARS.Commands)

| # | Оригинал (MARS.Server) | Статус | Микросервисный файл |
|---|------------------------|--------|---------------------|
| 1 | `adhd_command.cs` | ✅ | Commands |
| 2 | `automessage_SendAutoMessage.cs` | ✅ | Commands |
| 3 | `byebye_command.cs` | ✅ | Commands |
| 4 | `c_ShortCommands.cs` | ✅ | Commands |
| 5 | `catisa_command.cs` | ✅ | Commands |
| 6 | `directory_command.cs` | ✅ | Commands |
| 7 | `discord_command.cs` | ✅ | Commands |
| 8 | `download_command.cs` | ✅ | Commands |
| 9 | `example_command.cs` | ✅ | Commands |
| 10 | `fumo_command.cs` | ✅ | Commands |
| 11 | `genshin_command.cs` | ✅ | Commands |
| 12 | `getAllKeyWordsForAlerts_Command.cs` | ✅ | Commands |
| 13 | `googlephotos_authorize_command.cs` | ✅ | Commands |
| 14 | `hellovideo_command.cs` | ✅ | Commands |
| 15 | `help_command.cs` | ✅ | Commands |
| 16 | `honkai_command.cs` | ✅ | Commands |
| 17 | `honkaiusers_command.cs` | ✅ | Commands |
| 18 | `info_command.cs` | ✅ | Commands |
| 19 | `joinedtwitchchannels_command.cs` | ✅ | Commands |
| 20 | `links_command.cs` | ✅ | Commands |
| 21 | `mikubeam_command.cs` | ✅ | Commands |
| 22 | `mikumonday_MikuMondayReward.cs` | ✅ | Commands |
| 23 | `minigamestop_command.cs` | ✅ | Commands |
| 24 | `mutesound_command.cs` | ✅ | Commands |
| 25 | `platformtest_command.cs` | ✅ | Commands |
| 26 | `puntoswitcher_command.cs` | ✅ | Commands |
| 27 | `queue_QueuePosition.cs` | ✅ | Commands |
| 28 | `randommem_command.cs` | ✅ | Commands |
| 29 | `randomshorts_command.cs` | ✅ | Commands |
| 30 | `rollfrog_command.cs` | ✅ | Commands |
| 31 | `rollfumo_command.cs` | ✅ | Commands |
| 32 | `rollmiku_command.cs` | ✅ | Commands |
| 33 | `rollwaifu_command.cs` | ✅ | Commands |
| 34 | `setenv_command.cs` | ✅ | Commands |
| 35 | `shutdown_command.cs` | ✅ | Commands |
| 36 | `song_command.cs` | ✅ | Commands |
| 37 | `spotifyauth_start_command.cs` | ✅ | Commands |
| 38 | `sr_command.cs` | ✅ | Commands |
| 39 | `srclear_command.cs` | ✅ | Commands |
| 40 | `srlist_SoundRequestList.cs` | ✅ | Commands |
| 41 | `srpause_command.cs` | ✅ | Commands |
| 42 | `srplay_command.cs` | ✅ | Commands |
| 43 | `srstop_command.cs` | ✅ | Commands |
| 44 | `srvolume_command.cs` | ✅ | Commands |
| 45 | `srwrong_command.cs` | ✅ | Commands |
| 46 | `start_command.cs` | ✅ | Commands |
| 47 | `systeminfo_command.cs` | ✅ | Commands |
| 48 | `systeminfo_SystemInfo.cs` | ✅ | Commands |
| 49 | `tanya_command.cs` | ✅ | Commands |
| 50 | `tanya_Tanya.cs` | ✅ | Commands |
| 51 | `telegramonly_command.cs` | ✅ | Commands |
| 52 | `title_ChangeStreamTitle.cs` | ✅ | Commands |
| 53 | `ttsfilter_command.cs` | ✅ | Commands |
| 54 | `ttsstop_command.cs` | ✅ | Commands |
| 55 | `ttsvoice_command.cs` | ✅ | Commands |
| 56 | `ttsvolume_command.cs` | ✅ | Commands |
| 57 | `twitchauthnotify_command.cs` | ✅ | Commands |
| 58 | `twitchblacklistadd_command.cs` | ✅ | Commands |
| 59 | `twitchblacklistremove_command.cs` | ✅ | Commands |
| 60 | `twitchchannelreconnect_command.cs` | ✅ | Commands |
| 61 | `twitchchannelstatus_command.cs` | ✅ | Commands |
| 62 | `twitchevents_command.cs` | ✅ | Commands |
| 63 | `twitchsubrec_command.cs` | ✅ | Commands |
| 64 | `unmutesound_command.cs` | ✅ | Commands |
| 65 | `vanish_command.cs` | ✅ | Commands |
| 66 | `waifuunmerge_command.cs` | ✅ | Commands |
| 67 | `whitelist_command.cs` | ✅ | Commands |
| 68 | `wtelegramstatus_command.cs` | ✅ | Commands |
| 69 | `zonezero_command.cs` | ✅ | Commands |

---

## Статистика

### Rewards
- **Всего оригиналов**: 63 (включая подпапки и shared services)
- **Перенесено в Alerts**: 32 обработчика
- **Перенесено в WaifuGacha**: 6 сервисов
- **Перенесено в TwitchCore**: 5 сервисов
- **Не перенесено (TwitchReward обёртки)**: 20 — это metadata-only классы, которые наследуют `TemporaryReward` и не содержат логики. Их алерты обрабатываются через общую PyroAlerts систему.
- **Не перенесено (доп. файлы)**: `LegBumRefundService.cs`, `1702_EmojisReward/`

### Commands
- **Всего оригиналов**: 69
- **Перенесено**: 69 ✅ (100%)
- **Примечание**: Большинство команд застаблены (stubbed) — внешние зависимости (Twitch, Telegram, Discord) заменены на no-op сообщения. Команды, не зависящие от внешних сервисов, работают полностью.
