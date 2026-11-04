import { Button } from "antd";

import { MediaDto, MediaFileInfoTypeEnum } from "@/shared/api";

interface RandomMemDetailsProperties {
  item: MediaDto;
  onClose: () => void;
}

export default function RandomMemDetails({
  item,
  onClose,
}: RandomMemDetailsProperties) {
  const { fileInfo, metaInfo, textInfo } = item.mediaInfo;
  const source = fileInfo.filePath;

  const baseStyle: React.CSSProperties = {
    position: "absolute",
    right: 12,
    top: 12,
    width: 420,
    maxHeight: "80vh",
    padding: 12,
    borderRadius: 12,
    background: "var(--site-bg-card)",
    boxShadow: "var(--site-shadow-medium)",
    color: "var(--site-text-primary)",
    overflow: "auto",
    zIndex: 1000,
  };

  const renderMediaPreview = () => {
    switch (fileInfo.type) {
      case MediaFileInfoTypeEnum.Image:
      case MediaFileInfoTypeEnum.Gif: {
        return (
          <img
            src={source}
            alt={fileInfo.fileName}
            style={{ width: "100%", height: "auto", borderRadius: 6 }}
          />
        );
      }

      case MediaFileInfoTypeEnum.Video: {
        return (
          <video
            src={source}
            controls
            style={{ width: "100%", borderRadius: 6 }}
          />
        );
      }

      case MediaFileInfoTypeEnum.Audio:
      case MediaFileInfoTypeEnum.Voice: {
        return <audio src={source} controls style={{ width: "100%" }} />;
      }

      default: {
        return (
          <div>
            <div>Тип: {fileInfo.type}</div>
            <a href={source} target="_blank" rel="noreferrer">
              Открыть файл
            </a>
          </div>
        );
      }
    }
  };

  return (
    <div style={baseStyle} onClick={e => e.stopPropagation()}>
      <div style={{ fontWeight: 700, marginBottom: 8 }}>
        {metaInfo.displayName || fileInfo.fileName}
      </div>

      <div
        style={{
          border: "1px solid var(--site-border-secondary)",
          borderRadius: 8,
          padding: 8,
          marginBottom: 12,
        }}
      >
        {renderMediaPreview()}
      </div>

      <div style={{ fontSize: 12, opacity: 0.8, marginBottom: 8 }}>
        Путь: {fileInfo.filePath}
      </div>

      {textInfo?.text && (
        <div style={{ whiteSpace: "pre-wrap" }}>{textInfo.text}</div>
      )}

      <div style={{ display: "flex", gap: 8, marginTop: 12 }}>
        <Button size="small" onClick={onClose}>
          Закрыть
        </Button>
      </div>
    </div>
  );
}
