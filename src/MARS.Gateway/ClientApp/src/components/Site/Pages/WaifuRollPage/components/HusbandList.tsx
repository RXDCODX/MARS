import { Button, Input, Spin } from "antd";
import { ArrowUpDown, Edit3, HeartOff, Trash2, User } from "lucide-react";
import { useMemo } from "react";

import { useWaifuRollStore } from "../store/useWaifuRollStore";
import styles from "../WaifuRollPage.module.scss";

const HusbandList: React.FC = () => {
  const husbands = useWaifuRollStore(s => s.husbands);
  const waifus = useWaifuRollStore(s => s.waifus);
  const isLoading = useWaifuRollStore(s => s.isLoading);
  const searchQuery = useWaifuRollStore(s => s.searchQuery);
  const sortDirection = useWaifuRollStore(s => s.sortDirection);
  const setSearchQuery = useWaifuRollStore(s => s.setSearchQuery);
  const toggleSortDirection = useWaifuRollStore(s => s.toggleSortDirection);
  const startEditHusband = useWaifuRollStore(s => s.startEditHusband);
  const confirmDelete = useWaifuRollStore(s => s.confirmDelete);
  const confirmUnmerge = useWaifuRollStore(s => s.confirmUnmerge);

  const filteredHusbands = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();
    const sorted = [...husbands].toSorted((a, b) =>
      sortDirection === "asc"
        ? (a.displayName ?? a.twitchId).localeCompare(
            b.displayName ?? b.twitchId
          )
        : (b.displayName ?? b.twitchId).localeCompare(
            a.displayName ?? a.twitchId
          )
    );

    if (!normalizedQuery) {
      return sorted;
    }

    return sorted.filter(
      h =>
        h.displayName && h.displayName.toLowerCase().includes(normalizedQuery)
    );
  }, [husbands, searchQuery, sortDirection]);

  const findWaifuForHusband = (waifuBrideId?: string | null) => {
    if (!waifuBrideId) {
      return;
    }
    return waifus.find(w => w.shikiId === waifuBrideId);
  };

  if (isLoading) {
    return (
      <div className={styles.loadingBox}>
        <Spin data-testid="loading-spinner" />
      </div>
    );
  }

  return (
    <div data-testid="husband-list">
      <div className={styles.toolbar}>
        <Input.Search
          placeholder="Поиск по имени..."
          value={searchQuery}
          onChange={e => setSearchQuery(e.target.value)}
          style={{ width: 300 }}
          data-testid="input-search"
          allowClear
        />
        <Button
          icon={<ArrowUpDown size={14} />}
          onClick={toggleSortDirection}
          data-testid="button-sort"
        >
          {sortDirection === "asc" ? "А-Я" : "Я-А"}
        </Button>
      </div>

      {filteredHusbands.length === 0 ? (
        <div className={styles.emptyState} data-testid="empty-state">
          Мужи не найдены
        </div>
      ) : (
        <div className={styles.cardGrid}>
          {filteredHusbands.map(husband => {
            const waifu = findWaifuForHusband(husband.waifuBrideId);

            return (
              <div
                key={husband.twitchId}
                className={styles.card}
                data-testid={`card-husband-${husband.twitchId}`}
              >
                <div className={styles.cardImage}>
                  {husband.profileImageUrl ? (
                    <img
                      src={husband.profileImageUrl}
                      alt={husband.displayName ?? ""}
                      loading="lazy"
                      onError={e => {
                        (e.target as HTMLImageElement).style.display = "none";
                      }}
                    />
                  ) : (
                    <div className={styles.avatarPlaceholder}>
                      <User size={48} />
                    </div>
                  )}
                </div>
                <div className={styles.cardBody}>
                  <div className={styles.cardName}>
                    {husband.displayName ?? "Без имени"}
                  </div>
                  <div className={styles.cardStats}>
                    <span>Роулов: {husband.orderCount}</span>
                  </div>
                  <div className={styles.cardStatus}>
                    {husband.isPrivated && waifu ? (
                      <span className={styles.badgePrivated}>
                        В браке с {waifu.name}
                      </span>
                    ) : (
                      <span className={styles.badgeAvailable}>Свободен</span>
                    )}
                  </div>
                  {husband.whenOrdered && (
                    <div className={styles.cardMeta}>
                      Последний роул:{" "}
                      {new Date(husband.whenOrdered).toLocaleDateString(
                        "ru-RU"
                      )}
                    </div>
                  )}
                </div>
                <div className={styles.cardActions}>
                  <Button
                    type="text"
                    size="small"
                    icon={<Edit3 size={14} />}
                    onClick={() => startEditHusband(husband)}
                    data-testid={`button-edit-${husband.twitchId}`}
                  >
                    Изменить
                  </Button>
                  {husband.isPrivated && (
                    <Button
                      type="text"
                      size="small"
                      icon={<HeartOff size={14} />}
                      onClick={() => confirmUnmerge(husband.twitchId)}
                      data-testid={`button-unmerge-${husband.twitchId}`}
                    >
                      Развод
                    </Button>
                  )}
                  <Button
                    type="text"
                    size="small"
                    danger
                    icon={<Trash2 size={14} />}
                    onClick={() => confirmDelete(husband.twitchId)}
                    data-testid={`button-delete-${husband.twitchId}`}
                  >
                    Удалить
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default HusbandList;
