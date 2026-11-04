import { getAudioPath } from "./mediaAssets";
import {
  FirstPartProps as FirstPartProperties,
  Props as Properties,
} from "./template/AlertTemplate";

interface AlertVersion {
  id: number;
  Variant: FirstPartProperties;
}

const versions: AlertVersion[] = [
  {
    id: 1,
    Variant: {
      audioSrc: getAudioPath("track1"),
      durationMs: 7215,
    },
  },
  {
    id: 2,
    Variant: {
      audioSrc: getAudioPath("track2"),
      durationMs: 9986,
    },
  },
  {
    id: 3,
    Variant: {
      audioSrc: getAudioPath("track3"),
      durationMs: 16_023,
    },
  },
  {
    id: 4,
    Variant: {
      audioSrc: getAudioPath("track4"),
      durationMs: 8725,
    },
  },
  {
    id: 5,
    Variant: {
      audioSrc: getAudioPath("track5"),
      durationMs: 8850,
    },
  },
  {
    id: 6,
    Variant: {
      audioSrc: getAudioPath("track6"),
      durationMs: 7829,
    },
  },
  {
    id: 7,
    Variant: {
      audioSrc: getAudioPath("track7"),
      durationMs: 15_478,
    },
  },
  {
    id: 8,
    Variant: {
      audioSrc: getAudioPath("track8"),
      durationMs: 13_414,
    },
  },
];

export const getRandomVersion = (
  text: string,
  callback: () => void
): { id: number; Variant: Properties } => {
  const baza = versions[Math.floor(Math.random() * versions.length)];
  return {
    id: baza.id,
    Variant: {
      firstPart: baza.Variant,
      secondPart: {
        message: text,
      },
      callback,
    },
  };
};

export default getRandomVersion;
