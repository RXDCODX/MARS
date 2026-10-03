import "./AFKScreenDemo.scss";

import React, { useState } from "react";

import AFKScreen from "./AFKScreen";

const AFKScreenDemo: React.FC = () => {
  const [config, setConfig] = useState({
    videoId: "6oMsWcCDGnw",
    playlistId: "PLB6r_8YDnipME_VANRWiiBgI9-rabmmL1",
    autoplay: true,
    controls: true,
    loop: true,
    muted: true,
  });

  const [isPlaying, setIsPlaying] = useState(false);

  const handleConfigChange = (key: string, value: any) => {
    setConfig(previous => ({ ...previous, [key]: value }));
  };

  const togglePlay = () => {
    setIsPlaying(!isPlaying);
  };

  return (
    <div className="afk-demo">
      <div className="demo-controls">
        <h2>AFKScreen Demo</h2>

        <div className="control-group">
          <label>
            Video ID:
            <input
              type="text"
              value={config.videoId}
              onChange={e => handleConfigChange("videoId", e.target.value)}
            />
          </label>
        </div>

        <div className="control-group">
          <label>
            Playlist ID:
            <input
              type="text"
              value={config.playlistId}
              onChange={e => handleConfigChange("playlistId", e.target.value)}
            />
          </label>
        </div>

        <div className="control-group">
          <label>
            <input
              type="checkbox"
              checked={config.autoplay}
              onChange={e => handleConfigChange("autoplay", e.target.checked)}
            />
            Autoplay
          </label>
        </div>

        <div className="control-group">
          <label>
            <input
              type="checkbox"
              checked={config.controls}
              onChange={e => handleConfigChange("controls", e.target.checked)}
            />
            Show Controls
          </label>
        </div>

        <div className="control-group">
          <label>
            <input
              type="checkbox"
              checked={config.loop}
              onChange={e => handleConfigChange("loop", e.target.checked)}
            />
            Loop
          </label>
        </div>

        <div className="control-group">
          <label>
            <input
              type="checkbox"
              checked={config.muted}
              onChange={e => handleConfigChange("muted", e.target.checked)}
            />
            Muted
          </label>
        </div>

        <button onClick={togglePlay} className="demo-button">
          {isPlaying ? "Stop Demo" : "Start Demo"}
        </button>
      </div>

      {isPlaying && (
        <div className="demo-player">
          <AFKScreen />
        </div>
      )}
    </div>
  );
};

export default AFKScreenDemo;
