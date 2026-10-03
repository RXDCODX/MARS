import {
  Avatar,
  Button,
  Card,
  Checkbox,
  Input,
  Spin,
  Tag,
  Typography,
} from "antd";
import { ArrowLeft, Save, Trash2, User } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { defaultApiConfig } from "@/shared/api/api-config";
import { TwitchUsers } from "@/shared/api/http-clients/TwitchUsers";
import type {
  TwitchUserDto,
  UpdateTwitchUserRequest,
} from "@/shared/api/types/data-contracts";
import { useToastModal } from "@/shared/Utils/ToastModal";

import styles from "./TwitchUsersPage.module.scss";

const { Title, Text } = Typography;

const TwitchUserEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showToast } = useToastModal();

  const [user, setUser] = useState<TwitchUserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [userLogin, setUserLogin] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [profileImageUrl, setProfileImageUrl] = useState("");
  const [chatColor, setChatColor] = useState("");
  const [aliasNickname, setAliasNickname] = useState("");
  const [isModerator, setIsModerator] = useState(false);
  const [isVip, setIsVip] = useState(false);
  const [isInBlockList, setIsInBlockList] = useState(false);

  const loadUser = useCallback(async () => {
    if (!id) {
      setError("Не указан идентификатор пользователя");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const client = new TwitchUsers(defaultApiConfig);
      const response = await client.twitchUsersDetail(id);
      const operation = response.data;

      if (operation.success && operation.data) {
        const u = operation.data;
        setUser(u);
        setUserLogin(u.userLogin);
        setDisplayName(u.displayName);
        setProfileImageUrl(u.profileImageUrl ?? "");
        setChatColor(u.chatColor ?? "");
        setAliasNickname(u.aliasNickname ?? "");
        setIsModerator(u.isModerator);
        setIsVip(u.isVip);
        setIsInBlockList(u.isInBlockList);
      } else {
        setError(operation.message ?? "Пользователь не найден");
      }
    } catch {
      setError("Ошибка при загрузке пользователя");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void loadUser();
  }, [loadUser]);

  const handleSave = async () => {
    if (!id) return;

    setSaving(true);
    setError(null);

    try {
      const payload: UpdateTwitchUserRequest = {
        userLogin: userLogin.trim() || undefined,
        displayName: displayName.trim() || undefined,
        profileImageUrl: profileImageUrl.trim() || undefined,
        chatColor: chatColor.trim() || undefined,
        isModerator: isModerator,
        isVip: isVip,
        isInBlockList: isInBlockList,
        aliasNickname: aliasNickname.trim() || undefined,
      };

      const client = new TwitchUsers(defaultApiConfig);
      const response = await client.twitchUsersUpdate(id, payload);
      const operation = response.data;

      if (operation.success) {
        showToast({ success: true, message: "Пользователь обновлён" });
        navigate("/twitch-users");
      } else {
        setError(operation.message ?? "Ошибка при сохранении");
        showToast({
          success: false,
          message: operation.message ?? "Ошибка при сохранении",
        });
      }
    } catch {
      setError("Ошибка при сохранении пользователя");
      showToast({
        success: false,
        message: "Ошибка при сохранении пользователя",
      });
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!id) return;

    setSaving(true);

    try {
      const client = new TwitchUsers(defaultApiConfig);
      const response = await client.twitchUsersDelete(id);
      const operation = response.data;

      if (operation.success) {
        showToast({ success: true, message: "Пользователь удалён" });
        navigate("/twitch-users");
      } else {
        setError(operation.message ?? "Ошибка при удалении");
        showToast({
          success: false,
          message: operation.message ?? "Ошибка при удалении",
        });
      }
    } catch {
      setError("Ошибка при удалении пользователя");
      showToast({
        success: false,
        message: "Ошибка при удалении пользователя",
      });
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.pageContainer} data-testid="loading-spinner">
        <div className={styles.loadingContainer}>
          <Spin size="large" />
          <Text type="secondary">Загрузка пользователя...</Text>
        </div>
      </div>
    );
  }

  if (error && !user) {
    return (
      <div className={styles.pageContainer} data-testid="error-message">
        <div className={styles.emptyState}>
          <Text type="danger">{error}</Text>
          <Button onClick={() => navigate("/twitch-users")}>
            Вернуться к списку
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.pageContainer} data-testid="twitch-user-edit-page">
      <div className={styles.header}>
        <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
          <Button
            icon={<ArrowLeft size={16} />}
            onClick={() => navigate("/twitch-users")}
            data-testid="button-back"
          />
          <Title level={4} style={{ margin: 0 }}>
            Редактирование пользователя
          </Title>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button
            danger
            onClick={handleDelete}
            loading={saving}
            icon={<Trash2 size={16} />}
            data-testid="button-delete"
          >
            Удалить
          </Button>
          <Button
            type="primary"
            onClick={handleSave}
            loading={saving}
            icon={<Save size={16} />}
            data-testid="button-save"
          >
            Сохранить
          </Button>
        </div>
      </div>

      {user && (
        <Card>
          <div
            style={{ display: "flex", flexDirection: "column", gap: "1rem" }}
          >
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: "1rem",
                marginBottom: "0.5rem",
              }}
            >
              <Avatar
                src={profileImageUrl || user.profileImageUrl}
                size={80}
                icon={
                  !profileImageUrl && !user.profileImageUrl ? (
                    <User size={40} />
                  ) : undefined
                }
                style={{
                  backgroundColor:
                    !profileImageUrl && !user.profileImageUrl
                      ? "#8c8c8c"
                      : undefined,
                }}
                data-testid="avatar-preview"
              />
              <div>
                <Title level={5} style={{ margin: 0 }}>
                  {displayName || user.displayName}
                </Title>
                <Text type="secondary">@{userLogin || user.userLogin}</Text>
                <div style={{ marginTop: 4 }}>
                  {user.isBroadcaster && <Tag color="red">Broadcaster</Tag>}
                  {(isModerator || user.isModerator) && (
                    <Tag color="blue">Mod</Tag>
                  )}
                  {(isVip || user.isVip) && <Tag color="purple">VIP</Tag>}
                </div>
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
                <Text strong>Twitch ID</Text>
                <Input
                  value={user.twitchId}
                  disabled
                  data-testid="input-twitch-id"
                />
              </div>
              <div>
                <Text strong>User Login</Text>
                <Input
                  value={userLogin}
                  onChange={e => setUserLogin(e.target.value)}
                  data-testid="input-user-login"
                />
              </div>
              <div>
                <Text strong>Display Name</Text>
                <Input
                  value={displayName}
                  onChange={e => setDisplayName(e.target.value)}
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
      )}
    </div>
  );
};

export default TwitchUserEditPage;
