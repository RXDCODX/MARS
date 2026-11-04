import { useState } from "react";

import Announce from "@/shared/Utils/Announce/Announce";

import Message from "./Message";

export default function HighliteMessage() {
  const [announced, setAnnounced] = useState(false);

  return (
    <>
      {!announced && (
        <Announce
          title={"HighliteMessage"}
          callback={() => setAnnounced(true)}
        />
      )}
      <div data-testid="obs-highlite-message">
        <Message />
      </div>
    </>
  );
}
