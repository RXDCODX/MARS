import { Button, Card, Typography } from "antd";
import { Trash2, Upload, Volume2 } from "lucide-react";
import { useRef } from "react";

import { getApiBaseUrl } from "@/shared/api/api-config";
import { useToastModal } from "@/shared/Utils/ToastModal";

import { useWaifuRollStore } from "../store/useWaifuRollStore";

const AudioManager: React.FC = () => {
  const { showToast } = useToastModal();
  const audios = useWaifuRollStore(s => s.audios);
  const deleteAudio = useWaifuRollStore(s => s.deleteAudio);
  const uploadAudio = useWaifuRollStore(s => s.uploadAudio);
  const fileInputReference = useRef<HTMLInputElement>(null);

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const name = file.name.replace(/\.[^/.]+$/, "");
    const result = await uploadAudio(file, name);
    if (result) {
      showToast(result);
    }

    if (fileInputReference.current) {
      fileInputReference.current.value = "";
    }
  };

  const handleDelete = async (id: string) => {
    const result = await deleteAudio(id);
    if (result) {
      showToast(result);
    }
  };

  const getStreamUrl = (id: string) =>
    `${getApiBaseUrl()}/api/WaifuRoll/audios/${id}/stream`;

  return (
    <Card
      title="Аудио треки"
      style={{ marginTop: "1rem" }}
      data-testid="audio-manager"
      extra={
        <>
          <input
            ref={fileInputReference}
            type="file"
            accept=".mp3,.wav,.ogg,.m4a,.flac"
            onChange={handleUpload}
            style={{ display: "none" }}
            data-testid="input-audio-file"
          />
          <Button
            icon={<Upload size={14} />}
            onClick={() => fileInputReference.current?.click()}
            data-testid="button-upload-audio"
          >
            Загрузить
          </Button>
        </>
      }
    >
      {audios.length === 0 ? (
        <Typography.Text type="secondary" data-testid="empty-audios">
          Нет загруженных аудио
        </Typography.Text>
      ) : (
        <div
          style={{ display: "flex", flexDirection: "column", gap: "0.5rem" }}
        >
          {audios.map(audio => (
            <div
              key={audio.id}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "0.5rem",
                padding: "0.5rem",
                borderRadius: "4px",
                background: "var(--ant-color-bg-layout)",
              }}
              data-testid={`audio-item-${audio.id}`}
            >
              <Volume2 size={14} />
              <Typography.Text style={{ flex: 1 }}>
                {audio.name}
                <Typography.Text
                  type="secondary"
                  style={{ marginLeft: "0.5rem" }}
                >
                  {audio.fileExtension}
                </Typography.Text>
              </Typography.Text>
              <audio
                controls
                src={getStreamUrl(audio.id)}
                style={{ height: "32px", maxWidth: "200px" }}
                data-testid={`audio-player-${audio.id}`}
              />
              <Button
                type="text"
                size="small"
                danger
                icon={<Trash2 size={14} />}
                onClick={() => void handleDelete(audio.id)}
                data-testid={`button-delete-audio-${audio.id}`}
              />
            </div>
          ))}
        </div>
      )}
    </Card>
  );
};

export default AudioManager;
