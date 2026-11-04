import { JSX } from "react";
import { Textfit } from "react-textfit";

import { MediaDto } from "@/shared/api";
import useTwitchStore from "@/shared/twitchStore/twitchStore";

import { replaceEmotes } from "..";
import styles from "./BigTextStyles.module.scss";

interface Properties {
  content: MediaDto | string;
}

export function BigTextBlockForAudio({ content: mediaInfo }: Properties) {
  const parser = useTwitchStore(state => state.parser);
  const parserToLink = useTwitchStore(state => state.parseToLink);
  const text =
    typeof mediaInfo === "string"
      ? mediaInfo
      : mediaInfo.mediaInfo.textInfo.text;

  if (text === null && text === "") {
    return;
  }

  if (!parser || !parserToLink) {
    return;
  }

  const splits = text?.split("=");

  const length = splits?.length;

  if (!length) {
    return null;
  }
  if (length && length > 2) {
    console.error("Дохуя разделителей в тексте алерта");
    return null;
  }

  const emotesSplits: Array<string | JSX.Element | JSX.Element[]> = [];
  for (let index = 0; index < splits.length; index++) {
    splits[index] = splits[index].trim();
    if (splits[index]) {
      const result = replaceEmotes({
        text: splits[index],
        parser,
        newParser: parserToLink,
      });
      if (result) {
        emotesSplits[index] = result;
      }
    }
  }

  const is2Exists = splits[1] != undefined && emotesSplits[1] != undefined;

  return (
    <div className={styles.grid}>
      <div className={styles.grid_cell}>
        <Textfit
          forceSingleModeWidth
          style={{
            width: "100%",
            height: "100%",
            marginLeft: "auto",
            marginRight: "auto",
            textAlign: "center",
            display: "flex",
            justifyContent: "center",
            alignContent: "center",
            flexWrap: "nowrap",
            alignItems: "stretch",
          }}
          max={2000}
        >
          {emotesSplits[0]}
        </Textfit>
      </div>
      <div className={styles.grid_cell}></div>
      <div className={styles.grid_cell}></div>
      <div className={styles.grid_cell}></div>
      <div
        className={
          styles.grid_cell + ` down-cell ${is2Exists ? " bg-black" : ""}`
        }
      >
        <Textfit
          forceSingleModeWidth
          style={{
            width: "100%",
            height: "100%",
            marginLeft: "auto",
            marginRight: "auto",
            textAlign: "center",
            display: "flex",
            justifyContent: "center",
            alignContent: "center",
            flexWrap: "nowrap",
            alignItems: "stretch",
          }}
          max={2000}
        >
          {is2Exists ? emotesSplits[1] : ""}
        </Textfit>
      </div>
    </div>
  );
}
