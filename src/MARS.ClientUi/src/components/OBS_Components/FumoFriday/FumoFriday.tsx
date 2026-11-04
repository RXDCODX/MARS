import { useState } from "react";

import Announce from "@/shared/Utils/Announce/Announce";

import { FumoFridayController } from "./FumoFridayController";

export function FumoFriday() {
  const [announced, setAnnounced] = useState<boolean>(false);
  document.title = "FumoFriday";

  return (
    <>
      {!announced && (
        <Announce callback={() => setAnnounced(true)} title={"FumoFriday"} />
      )}
      <FumoFridayController />
    </>
  );
}
