import { OVERLAY_EVENT_NAMES, type OverlayEventName } from "@/shared/api/realtime/overlayEvents";

export interface OverlayTemplate {
  event: OverlayEventName;
  title: string;
  description: string;
  /** Маршруты оверлеев, которые слушают это событие. */
  overlayRoutes: string[];
  /** Генерирует JSON payload (null для событий без payload). */
  buildPayload: () => unknown | null;
}

const uid = () => globalThis.crypto?.randomUUID?.() ?? `id-${Math.random().toString(16).slice(2)}`;
const pick = <T,>(arr: T[]) => arr[Math.floor(Math.random() * arr.length)];
const names = ["Pyro", "MikuFan", "WaifuLover", "Streamer", "ChatUser", "TestUser"];
const colors = ["#FF6B6B", "#4ECDC4", "#FFD93D", "#6C5CE7", "#00B894", "#0984E3"];

const sampleTwitchUser = () => ({
  id: uid(),
  twitchId: String(Math.floor(Math.random() * 1_000_000)),
  userLogin: pick(names).toLowerCase(),
  displayName: pick(names),
  chatColor: pick(colors),
  profileImageUrl: "https://static-cdn.jtvnw.net/user-default-pictures-uv/cdd517fe-def4-11e9-948e-784f43822e80-profile_image-70x70.png",
  isOnline: true,
});

const sampleMediaDto = () => ({
  mediaInfo: {
    id: uid(),
    textInfo: {
      keywordsColor: "#00FF00",
      triggerWord: "",
      text: `${pick(names)} активировал тестовый алерт`,
      textColor: "#FFFFFF",
      keywordSymbolDelimiter: 35,
      hasKeywordSymbolDelimiter: true,
    },
    fileInfo: {
      type: "Image",
      filePath: "https://placekitten.com/400/300",
      isLocalFile: false,
      fileName: "alert.png",
      extension: ".png",
      isFileNotConvertable: false,
    },
    positionInfo: {
      isProportion: true,
      isResizeRequires: false,
      height: 300,
      width: 400,
      isRotated: false,
      rotation: 0,
      xCoordinate: 0,
      yCoordinate: 0,
      randomCoordinates: false,
      isVerticallCenter: true,
      isHorizontalCenter: true,
      isUseOriginalWidthAndHeight: false,
    },
    metaInfo: {
      twitchPointsCost: 100,
      twitchGuid: uid(),
      vip: false,
      displayName: pick(names),
      isLooped: false,
      isFreezeRequired: false,
      duration: 5,
      priority: "Normal",
      volume: 100,
      isEnabled: true,
    },
    stylesInfo: { isBorder: true, isShowLetterbox: false },
  },
  uploadStartTime: new Date().toISOString(),
});

const sampleWaifuInfo = () => ({
  id: uid(),
  name: pick(["Hatsune Miku", "Rem", "Zero Two", "Asuna", "Megumin"]),
  imageUrl: "https://placekitten.com/300/400",
  source: "Test",
  shikiId: Math.floor(Math.random() * 10000),
  isMerged: false,
  isAdded: false,
});

const sampleHusbandInfo = () => ({
  id: uid(),
  displayName: pick(names),
  avatarUrl: "https://static-cdn.jtvnw.net/user-default-pictures-uv/cdd517fe-def4-11e9-948e-784f43822e80-profile_image-70x70.png",
});

const sampleFumo = () => ({ id: uid(), name: pick(["Cirno", "Reimu", "Marisa", "Flandre"]), imageUrl: "https://placekitten.com/200/200" });
const sampleFrog = () => ({ id: uid(), name: pick(["Pepe", "Kermit", "Apu"]), imageUrl: "https://placekitten.com/200/200" });
const sampleMikuModule = () => ({ id: uid(), name: pick(["Sakura Miku", "Snow Miku", "Classic Miku"]), imageUrl: "https://placekitten.com/200/200" });

export const OVERLAY_TEMPLATES: OverlayTemplate[] = [
  {
    event: "Alert",
    title: "Alert (медиа-алерт)",
    description: "Один медиа-алерт (PyroAlerts).",
    overlayRoutes: ["/pyroalerts"],
    buildPayload: () => ({ media: sampleMediaDto() }),
  },
  {
    event: "Alerts",
    title: "Alerts (пакет)",
    description: "Пакет медиа-алертов.",
    overlayRoutes: ["/pyroalerts"],
    buildPayload: () => ({ media: [sampleMediaDto(), sampleMediaDto()] }),
  },
  {
    event: "WaifuRoll",
    title: "Waifu Roll",
    description: "Ролл вайфу.",
    overlayRoutes: ["/waifu"],
    buildPayload: () => ({
      waifu: sampleWaifuInfo(),
      displayName: pick(names),
      color: pick(colors),
    }),
  },
  {
    event: "AddNewWaifu",
    title: "Add New Waifu",
    description: "Добавление новой вайфу.",
    overlayRoutes: ["/waifu"],
    buildPayload: () => ({ waifu: { ...sampleWaifuInfo(), isAdded: true }, displayName: pick(names), color: pick(colors) }),
  },
  {
    event: "ShowCurrentWife",
    title: "Show Current Wife",
    description: "Показать текущую жену.",
    overlayRoutes: ["/waifu"],
    buildPayload: () => ({ waifu: sampleWaifuInfo(), husband: sampleHusbandInfo(), color: pick(colors) }),
  },
  {
    event: "MergeWaifu",
    title: "Merge Waifu",
    description: "Слияние вайфу.",
    overlayRoutes: ["/waifu"],
    buildPayload: () => ({ waifu: { ...sampleWaifuInfo(), isMerged: true }, husband: sampleHusbandInfo(), color: pick(colors) }),
  },
  {
    event: "FumoFriday",
    title: "Fumo Friday",
    description: "Фумо-пятница алерт.",
    overlayRoutes: ["/fumofriday"],
    buildPayload: () => ({ displayName: pick(names), color: pick(colors) }),
  },
  {
    event: "NewMessage",
    title: "New Chat Message",
    description: "Новое сообщение чата.",
    overlayRoutes: ["/chath", "/chatv"],
    buildPayload: () => {
      const user = sampleTwitchUser();
      return {
        id: uid(),
        messageJson: {
          id: uid(),
          userId: user.twitchId,
          name: user.displayName,
          login: user.userLogin,
          text: pick(["Привет стрим!", "Тестовое сообщение", "PogChamp", "Работает!"]),
          color: user.chatColor,
          profileImageUrl: user.profileImageUrl,
          roles: [],
          badges: [],
          fragments: [],
          isVip: false,
          isSubscriber: false,
          isModerator: false,
          isBroadcaster: false,
        },
      };
    },
  },
  {
    event: "DeleteMessage",
    title: "Delete Chat Message",
    description: "Удаление сообщения чата (нужен id существующего сообщения).",
    overlayRoutes: ["/chath", "/chatv"],
    buildPayload: () => ({ id: "message-id-to-delete" }),
  },
  {
    event: "MakeScreenParticles",
    title: "Screen Particles",
    description: "Частицы на экране (конфетти/фейерверк).",
    overlayRoutes: ["/confetti"],
    buildPayload: () => ({ particlesJson: pick(["Confetty", "Fireworks"]) }),
  },
  {
    event: "MakeScreenEmojisParticles",
    title: "Emoji Particles",
    description: "Эмодзи-частицы на экране.",
    overlayRoutes: ["/confetti"],
    buildPayload: () => ({
      messageJson: {
        id: uid(),
        userId: "1",
        name: pick(names),
        login: "test",
        text: "🎉🎊✨",
        color: pick(colors),
        roles: [],
        badges: [],
        fragments: [],
      },
    }),
  },
  {
    event: "RandomMem",
    title: "Random Mem",
    description: "Случайный мем (использует AlertEvent payload).",
    overlayRoutes: ["/randommem"],
    buildPayload: () => ({ media: sampleMediaDto() }),
  },
  {
    event: "Adhd",
    title: "ADHD",
    description: "Режим ADHD на N секунд.",
    overlayRoutes: ["/adhd"],
    buildPayload: () => ({ seconds: 30 }),
  },
  {
    event: "GaoAlert",
    title: "Gao Alert",
    description: "Гао-алерт.",
    overlayRoutes: ["/gaoalert"],
    buildPayload: () => ({
      gaoAlertJson: {
        id: uid(),
        twitchUser: sampleTwitchUser(),
        isJustText: true,
        justText: "GAO GAO!",
      },
    }),
  },
  {
    event: "Credits",
    title: "Credits",
    description: "Титры (пустое событие).",
    overlayRoutes: ["/credits"],
    buildPayload: () => null,
  },
  {
    event: "MichaelJackson",
    title: "Michael Jackson",
    description: "Michael Jackson алерт (пустое событие).",
    overlayRoutes: ["/MichaelJackson"],
    buildPayload: () => null,
  },
  {
    event: "MikuMonday",
    title: "Miku Monday",
    description: "Miku Monday событие.",
    overlayRoutes: ["/MikuMonday"],
    buildPayload: () => ({
      id: uid(),
      twitchUser: sampleTwitchUser(),
      selectedTrack: { id: uid(), name: "Miku Miku Beam", artist: "Hatsune Miku", url: "" },
      availableTracks: [],
      skipAvailableTracksUpdate: false,
    }),
  },
  {
    event: "MikuMikuBeam",
    title: "Miku Miku Beam",
    description: "Луч Мику Мику (список пользователей).",
    overlayRoutes: ["/mikumikubeam"],
    buildPayload: () => ({
      usersJson: [sampleTwitchUser(), sampleTwitchUser()].map(u => ({
        twitchId: u.twitchId,
        displayName: u.displayName,
        profileImageUrl: u.profileImageUrl,
        color: u.chatColor,
      })),
    }),
  },
  {
    event: "PhonkEdit",
    title: "Phonk Edit",
    description: "Фонк-правка (пустое событие).",
    overlayRoutes: ["/phonk-manager"],
    buildPayload: () => null,
  },
  {
    event: "TikTokEdit",
    title: "TikTok Edit",
    description: "TikTok edit событие.",
    overlayRoutes: ["/tik-tok-manager"],
    buildPayload: () => ({ guid: uid(), text: "Тестовый TikTok текст" }),
  },
  {
    event: "AllRefund",
    title: "All Refund",
    description: "Полный возврат.",
    overlayRoutes: ["/all-refund"],
    buildPayload: () => ({ userJson: sampleTwitchUser() }),
  },
  {
    event: "FumoRoll",
    title: "Fumo Roll",
    description: "Ролл фумо.",
    overlayRoutes: ["/fumo"],
    buildPayload: () => ({
      fumoJson: sampleFumo(),
      twitchUserJson: sampleTwitchUser(),
      collectedCount: 3,
      totalCount: 20,
    }),
  },
  {
    event: "FrogRoll",
    title: "Frog Roll",
    description: "Ролл жабы.",
    overlayRoutes: ["/frogs"],
    buildPayload: () => ({ frogJson: sampleFrog(), twitchUserJson: sampleTwitchUser() }),
  },
  {
    event: "MikuRoll",
    title: "Miku Roll",
    description: "Ролл мику.",
    overlayRoutes: ["/miku-roll"],
    buildPayload: () => ({
      mikuModuleJson: sampleMikuModule(),
      twitchUserJson: sampleTwitchUser(),
      collectedCount: 2,
      totalCount: 10,
    }),
  },
  {
    event: "AdhdConfig",
    title: "ADHD Config",
    description: "Обновление конфигурации ADHD-раскладки.",
    overlayRoutes: ["/adhd"],
    buildPayload: () => ({
      config: {
        showRainEffect: true,
        dvdLogosCount: 3,
        showQuiz: true,
      },
    }),
  },
];

export const TEMPLATE_BY_EVENT = new Map(OVERLAY_TEMPLATES.map(t => [t.event, t]));

// Санити: покрыты все события манифеста
const missing = OVERLAY_EVENT_NAMES.filter(n => !TEMPLATE_BY_EVENT.has(n));
if (missing.length > 0) {
  // eslint-disable-next-line no-console
  console.warn("Overlay templates missing events:", missing);
}
