import {
  Avatar,
  Button,
  Card,
  Checkbox,
  Input,
  Select,
  Tag,
  Typography,
} from "antd";
import {
  ArrowUpDown,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  User,
} from "lucide-react";
import { useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useShallow } from "zustand/react/shallow";

import { useToastModal } from "@/shared/Utils/ToastModal";

import TwitchUserDeleteModal from "./components/TwitchUserDeleteModal";
import {
  selectProcessedUsers,
  type SortField,
  useTwitchUsersStore,
} from "./store/useTwitchUsersStore";
import styles from "./TwitchUsersPage.module.scss";
import type { TwitchUserViewModel } from "./TwitchUsersPage.types";

const { Title, Text } = Typography;
const { Meta } = Card;

const roleClass = (user: TwitchUserViewModel): string => {
  if (user.isInBlockList) return styles.cardBlocked;
  if (user.isBroadcaster) return styles.cardBroadcaster;
  if (user.isModerator) return styles.cardModerator;
  if (user.isVip) return styles.cardVip;
  return styles.cardFollower;
};

const UserCard: React.FC<{
  user: TwitchUserViewModel;
  onEdit: (id: string) => void;
  onDelete: (id: string, name: string) => void;
}> = ({ user, onEdit, onDelete }) => (
  <div
    className={`${styles.userCard} ${roleClass(user)}`}
    onClick={() => onEdit(user.twitchId)}
    data-testid={`card-user-${user.twitchId}`}
  >
    <Card bordered={false} className={styles.cardInner}>
      <div className={styles.cardBody}>
        <Avatar
          src={user.profileImageUrl}
          size={64}
          icon={user.profileImageUrl ? undefined : <User size={32} />}
          style={{
            backgroundColor: user.profileImageUrl ? undefined : "#8c8c8c",
          }}
          data-testid={`avatar-user-${user.twitchId}`}
        />

        <Meta
          title={
            <span
              style={user.chatColor ? { color: user.chatColor } : undefined}
            >
              {user.displayName}
            </span>
          }
          description={
            <div>
              <Text type="secondary" style={{ fontSize: 12 }}>
                @{user.userLogin}
              </Text>
              {user.aliasNickname && (
                <div style={{ marginTop: 4 }}>
                  <Text
                    type="secondary"
                    style={{ fontSize: 11, fontStyle: "italic" }}
                  >
                    TTS: {user.aliasNickname}
                  </Text>
                </div>
              )}
            </div>
          }
        />

        <div className={styles.roleTags}>
          {user.isBroadcaster && <Tag color="red">Broadcaster</Tag>}
          {user.isModerator && <Tag color="blue">Mod</Tag>}
          {user.isVip && <Tag color="purple">VIP</Tag>}
          {user.isInBlockList && <Tag color="red">Blocked</Tag>}
          {!user.isBroadcaster && !user.isModerator && !user.isVip && (
            <Tag>Follower</Tag>
          )}
        </div>
      </div>
    </Card>
    <Button
      danger
      type="text"
      className={styles.deleteButton}
      icon={<Trash2 size={14} />}
      onClick={e => {
        e.stopPropagation();
        onDelete(user.twitchId, user.displayName);
      }}
      data-testid={`button-delete-${user.twitchId}`}
    >
      Удалить
    </Button>
  </div>
);

const SortIcon: React.FC<{
  field: SortField;
  currentField: SortField;
  direction: "asc" | "desc";
  onClick: () => void;
}> = ({ field, currentField, direction, onClick }) => {
  if (field !== currentField) return null;
  return (
    <ArrowUpDown
      size={14}
      className={`${styles.sortIcon} ${direction === "desc" ? styles.sortDesc : ""}`}
      onClick={onClick}
    />
  );
};

const TwitchUsersPage: React.FC = () => {
  const { showToast } = useToastModal();
  const navigate = useNavigate();

  const loadUsers = useTwitchUsersStore(state => state.loadUsers);
  const isLoading = useTwitchUsersStore(state => state.isLoading);
  const searchQuery = useTwitchUsersStore(state => state.searchQuery);
  const setSearchQuery = useTwitchUsersStore(state => state.setSearchQuery);
  const sortField = useTwitchUsersStore(state => state.sortField);
  const sortDirection = useTwitchUsersStore(state => state.sortDirection);
  const setSortField = useTwitchUsersStore(state => state.setSortField);
  const toggleSortDirection = useTwitchUsersStore(
    state => state.toggleSortDirection
  );
  const hideFollowers = useTwitchUsersStore(state => state.hideFollowers);
  const setHideFollowers = useTwitchUsersStore(state => state.setHideFollowers);
  const processedUsers = useTwitchUsersStore(useShallow(selectProcessedUsers));
  const openDeleteConfirm = useTwitchUsersStore(
    state => state.openDeleteConfirm
  );

  useEffect(() => {
    void loadUsers().then(result => {
      if (result && !result.success) {
        showToast(result);
      }
    });
  }, [loadUsers, showToast]);

  const handleRefresh = () => {
    void loadUsers().then(result => {
      if (result && !result.success) {
        showToast(result);
      }
    });
  };

  const handleSortFieldChange = (value: SortField) => {
    setSortField(value);
  };

  const handleSortDirectionToggle = () => {
    toggleSortDirection();
  };

  return (
    <div className={styles.pageContainer} data-testid="twitch-users-page">
      <div className={styles.header}>
        <Title level={3} style={{ margin: 0 }}>
          Пользователи Twitch
        </Title>
        <div className={styles.toolbar}>
          <Input
            placeholder="Поиск..."
            prefix={<Search size={16} />}
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            style={{ width: 200 }}
            allowClear
            data-testid="input-search"
          />
          <Select
            value={sortField}
            onChange={handleSortFieldChange}
            size="small"
            style={{ width: 140 }}
            data-testid="select-sort"
            options={[
              { value: "displayName", label: "По имени" },
              { value: "userLogin", label: "По логину" },
              { value: "role", label: "По роли" },
            ]}
          />
          <Button
            size="small"
            icon={
              <ArrowUpDown
                size={14}
                className={
                  sortDirection === "desc" ? styles.sortIconFlipped : ""
                }
              />
            }
            onClick={handleSortDirectionToggle}
            data-testid="button-sort-direction"
          />
          <Checkbox
            checked={hideFollowers}
            onChange={e => setHideFollowers(e.target.checked)}
            data-testid="checkbox-hide-followers"
          >
            Без фоловеров
          </Checkbox>
          <Button
            size="small"
            icon={<RefreshCw size={14} />}
            onClick={handleRefresh}
            loading={isLoading}
            data-testid="button-refresh"
          />
          <Link to="/twitch-users/create">
            <Button
              type="primary"
              size="small"
              icon={<Plus size={14} />}
              data-testid="button-create"
            >
              Создать
            </Button>
          </Link>
        </div>
      </div>

      {isLoading && processedUsers.length === 0 ? (
        <div className={styles.loadingContainer} data-testid="loading-spinner">
          <RefreshCw size={32} className={styles.spinner} />
          <Text type="secondary">Загрузка пользователей...</Text>
        </div>
      ) : processedUsers.length === 0 ? (
        <div className={styles.emptyState} data-testid="empty-state">
          <User size={48} className={styles.emptyIcon} />
          <Title level={4}>Пользователи не найдены</Title>
          <Text type="secondary">
            {searchQuery || hideFollowers
              ? "Попробуйте изменить параметры фильтрации"
              : "Пользователи Twitch пока не загружены"}
          </Text>
        </div>
      ) : (
        <div className={styles.cardGrid}>
          {processedUsers.map(user => (
            <UserCard
              key={user.twitchId}
              user={user}
              onEdit={id => navigate(`/twitch-users/${id}/edit`)}
              onDelete={openDeleteConfirm}
            />
          ))}
        </div>
      )}

      <TwitchUserDeleteModal />
    </div>
  );
};

export default TwitchUsersPage;
