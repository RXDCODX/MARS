import { Button, Input, Spin } from "antd";
import { ArrowUpDown, Edit3, Trash2 } from "lucide-react";
import { useMemo } from "react";

import { useWaifuRollStore } from "../store/useWaifuRollStore";
import styles from "../WaifuRollPage.module.scss";

const WaifuList: React.FC = () => {
  const waifus = useWaifuRollStore(s => s.waifus);
  const husbands = useWaifuRollStore(s => s.husbands);
  const isLoading = useWaifuRollStore(s => s.isLoading);
  const searchQuery = useWaifuRollStore(s => s.searchQuery);
  const sortDirection = useWaifuRollStore(s => s.sortDirection);
  const setSearchQuery = useWaifuRollStore(s => s.setSearchQuery);
  const toggleSortDirection = useWaifuRollStore(s => s.toggleSortDirection);
  const startEditWaifu = useWaifuRollStore(s => s.startEditWaifu);
  const confirmDelete = useWaifuRollStore(s => s.confirmDelete);
  const switchToHusbandAndEdit = useWaifuRollStore(
    s => s.switchToHusbandAndEdit
  );

  const filteredWaifus = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();
    const sorted = [...waifus].toSorted((a, b) =>
      sortDirection === "asc"
        ? a.name.localeCompare(b.name)
        : b.name.localeCompare(a.name)
    );

    if (!normalizedQuery) {
      return sorted;
    }

    return sorted.filter(
      w =>
        w.name.toLowerCase().includes(normalizedQuery) ||
        (w.anime && w.anime.toLowerCase().includes(normalizedQuery)) ||
        (w.manga && w.manga.toLowerCase().includes(normalizedQuery))
    );
  }, [waifus, searchQuery, sortDirection]);

  const findHusbandForWaifu = (shikiId: string) =>
    husbands.find(h => h.waifuBrideId === shikiId);

  if (isLoading) {
    return (
      <div className={styles.loadingBox}>
        <Spin data-testid="loading-spinner" />
      </div>
    );
  }

  return (
    <div data-testid="waifu-list">
      <div className={styles.toolbar}>
        <Input.Search
          placeholder="Поиск по имени, аниме, манге..."
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

      {filteredWaifus.length === 0 ? (
        <div className={styles.emptyState} data-testid="empty-state">
          Вайфу не найдены
        </div>
      ) : (
        <div className={styles.cardGrid}>
          {filteredWaifus.map(waifu => {
            const husband = waifu.isPrivated
              ? findHusbandForWaifu(waifu.shikiId)
              : undefined;

            return (
              <div
                key={waifu.shikiId}
                className={styles.card}
                data-testid={`card-waifu-${waifu.shikiId}`}
              >
                <div className={styles.cardImage}>
                  <img
                    src={waifu.imageUrl}
                    alt={waifu.name}
                    loading="lazy"
                    onError={e => {
                      (e.target as HTMLImageElement).style.display = "none";
                    }}
                  />
                </div>
                <div className={styles.cardBody}>
                  <div className={styles.cardName}>{waifu.name}</div>
                  {waifu.anime && (
                    <div className={styles.cardMeta}>{waifu.anime}</div>
                  )}
                  {!waifu.anime && waifu.manga && (
                    <div className={styles.cardMeta}>{waifu.manga}</div>
                  )}
                  <div className={styles.cardStats}>
                    <span>Роулов: {waifu.orderCount}</span>
                    {waifu.audioName && (
                      <span className={styles.audioTag}>{waifu.audioName}</span>
                    )}
                  </div>
                  <div className={styles.cardStatus}>
                    {waifu.isPrivated && husband ? (
                      <span
                        className={styles.statusLink}
                        onClick={() => switchToHusbandAndEdit(husband.twitchId)}
                        data-testid={`link-husband-${waifu.shikiId}`}
                      >
                        Занята: {husband.displayName ?? husband.twitchId}
                      </span>
                    ) : (
                      <span className={styles.badgeAvailable}>Свободна</span>
                    )}
                  </div>
                </div>
                <div className={styles.cardActions}>
                  <Button
                    type="text"
                    size="small"
                    icon={<Edit3 size={14} />}
                    onClick={() => startEditWaifu(waifu)}
                    data-testid={`button-edit-${waifu.shikiId}`}
                  >
                    Изменить
                  </Button>
                  <Button
                    type="text"
                    size="small"
                    danger
                    icon={<Trash2 size={14} />}
                    onClick={() => confirmDelete(waifu.shikiId)}
                    data-testid={`button-delete-${waifu.shikiId}`}
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

export default WaifuList;
