import { BreakingNews } from "./components/BreakingNews";
import { Catisa } from "./components/Catisa";
import { DVDLogos } from "./components/DVDLogos";
import { FitnessVideo } from "./components/FitnessVideo";
import { GTAVideo } from "./components/GTAVideo";
import { HydraulicMobileVideo } from "./components/HydraulicMobileVideo";
import { LOFIGirl } from "./components/LOFI-Girl";
import { MukbangVideo } from "./components/MukbangVideo";
import { Notifications } from "./components/Notifications";
import { Quiz } from "./components/Quiz";
import { RainEffect } from "./components/RainEffect";
import { SlimeVideo } from "./components/SlimeVideo";
import { StreamerVideo } from "./components/StreamerVideo";
import { Surfer } from "./components/Surfer";
import { useAdhdConfig } from "./store/adhdLayoutStore";

export function ADHDPage() {
  const config = useAdhdConfig();

  return (
    <>
      {config.showRainEffect && <RainEffect />}
      {config.showDVDLogos && <DVDLogos count={config.dvdLogosCount} />}
      {config.showBreakingNews && <BreakingNews />}
      {config.showStreamerVideo && <StreamerVideo />}
      {config.showFitnessVideo && <FitnessVideo />}
      {config.showGTAVideo && <GTAVideo />}
      {config.showHydraulicMobileVideo && <HydraulicMobileVideo />}
      {config.showSlimeVideo && <SlimeVideo />}
      {config.showMukbangVideo && <MukbangVideo />}
      {config.showQuiz && <Quiz />}
      {config.showSurfer && <Surfer />}
      {config.showLOFIGirl && <LOFIGirl />}
      {config.showCatisa && <Catisa />}
      {config.showNotifications && <Notifications />}
    </>
  );
}
