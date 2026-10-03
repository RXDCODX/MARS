import { Avatar, Button, Card, Checkbox, Input, Typography } from "antd";
import { ArrowLeft, Save, User } from "lucide-react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";

import { defaultApiConfig } from "@/shared/api/api-config";
import { TwitchUsers } from "@/shared/api/http-clients/TwitchUsers";
import type { CreateTwitchUserRequest } from "@/shared/api/types/data-contracts";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./TwitchUsersPage.module.scss";

const { Title, Text } = Typography;

const TwitchUserCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { showToast } = useToastModal();

  const [twitchId, setTwitchId] = useState("");
  const [userLogin, setUserLogin] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [profileImageUrl, setProfileImageUrl] = useState("");
  const [chatColor, setChatColor] = useState("");
  const [aliasNickname, setAliasNickname] = useState("");
  const [isModerator, setIsModerator] = useState(false);
  const [isVip, setIsVip] = useState(false);
  const [isInBlockList, setIsInBlockList] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCreate = async () => {
    if (!twitchId.trim()) {
      setError("Twitch ID обязателен");
      return;
    }
    if (!userLogin.trim()) {
      setError("User Login обязателен");
      return;
    }
    if (!displayName.trim()) {
      setError("Display Name обязателен");
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const payload: CreateTwitchUserRequest = {
        twitchId: twitchId.trim(),
        userLogin: userLogin.trim(),
        displayName: displayName.trim(),
        profileImageUrl: profileImageUrl.trim() || undefined,
        chatColor: chatColor.trim() || undefined,
        isModerator: isModerator,
        isVip: isVip,
        isInBlockList: isInBlockList,
        aliasNickname: aliasNickname.trim() || undefined,
      };

      const client = new TwitchUsers(defaultApiConfig);
      const response = await client.twitchUsersCreate(payload);
      const operation = response.data;

      if (operation.success) {
        showToast({ success: true, message: "Пользователь создан" });
        navigate("/twitch-users");
      } else {
        setError(operation.message ?? "Ошибка при создании");
        showToast({
          success: false,
          message: operation.message ?? "Ошибка при создании",
        });
      }
    } catch {
      setError("Ошибка при создании пользователя");
      showToast({
        success: false,
        message: "Ошибка при создании пользователя",
      });
    } finally {
      setSaving(false);
    }
  };

  const hasAvatar = profileImageUrl.trim().length > 0;

  return (
    <div className={styles.pageContainer} data-testid="twitch-user-create-page">
      <div className={styles.header}>
        <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
          <Button
            icon={<ArrowLeft size={16} />}
            onClick={() => navigate("/twitch-users")}
            data-testid="button-back"
          />
          <Title level={4} style={{ margin: 0 }}>
            Создание пользователя
          </Title>
        </div>
        <Button
          type="primary"
          onClick={handleCreate}
          loading={saving}
          icon={<Save size={16} />}
          data-testid="button-create"
        >
          Создать
        </Button>
      </div>

      <Card>
        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "1rem",
              marginBottom: "0.5rem",
            }}
          >
            <Avatar
              src={hasAvatar ? profileImageUrl : undefined}
              size={80}
              icon={hasAvatar ? undefined : <User size={40} />}
              style={{
                backgroundColor: hasAvatar ? undefined : "#8c8c8c",
              }}
              data-testid="avatar-preview"
            >
              {!hasAvatar && displayName
                ? displayName.charAt(0).toUpperCase()
                : undefined}
            </Avatar>
            <div>
              <Title level={5} style={{ margin: 0 }}>
                {displayName || "Новый пользователь"}
              </Title>
              <Text type="secondary">@{userLogin || "login"}</Text>
            </div>
          </div>

          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: "0.75rem",
            }}
          >
            <div>
              <Text strong>Twitch ID *</Text>
              <Input
                value={twitchId}
                onChange={e => setTwitchId(e.target.value)}
                placeholder="Числовой ID Twitch"
                data-testid="input-twitch-id"
              />
            </div>
            <div>
              <Text strong>User Login *</Text>
              <Input
                value={userLogin}
                onChange={e => setUserLogin(e.target.value)}
                placeholder="Логин"
                data-testid="input-user-login"
              />
            </div>
            <div>
              <Text strong>Display Name *</Text>
              <Input
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                placeholder="Отображаемое имя"
                data-testid="input-display-name"
              />
            </div>
            <div>
              <Text strong>Profile Image URL</Text>
              <Input
                value={profileImageUrl}
                onChange={e => setProfileImageUrl(e.target.value)}
                placeholder="https://..."
                data-testid="input-profile-image-url"
              />
            </div>
            <div>
              <Text strong>Chat Color</Text>
              <Input
                value={chatColor}
                onChange={e => setChatColor(e.target.value)}
                placeholder="#FF0000"
                data-testid="input-chat-color"
              />
            </div>
            <div>
              <Text strong>Alias Nickname</Text>
              <Input
                value={aliasNickname}
                onChange={e => setAliasNickname(e.target.value)}
                placeholder="Прозвище для TTS"
                data-testid="input-alias-nickname"
              />
            </div>
          </div>

          <div style={{ display: "flex", gap: "1.5rem", flexWrap: "wrap" }}>
            <Checkbox
              checked={isModerator}
              onChange={e => setIsModerator(e.target.checked)}
              data-testid="checkbox-is-moderator"
            >
              Moderator
            </Checkbox>
            <Checkbox
              checked={isVip}
              onChange={e => setIsVip(e.target.checked)}
              data-testid="checkbox-is-vip"
            >
              VIP
            </Checkbox>
            <Checkbox
              checked={isInBlockList}
              onChange={e => setIsInBlockList(e.target.checked)}
              data-testid="checkbox-is-blocked"
            >
              В блок-листе
            </Checkbox>
          </div>

          {error && (
            <Text type="danger" data-testid="error-alert">
              {error}
            </Text>
          )}
        </div>
      </Card>
    </div>
  );
};

export default TwitchUserCreatePage;
