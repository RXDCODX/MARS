import { Alert, Button } from "antd";
import { Plus } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import {
  CinemaMediaItemDto,
  CinemaQueue,
  CinemaQueueStatistics,
  CreateMediaItemRequest,
  UpdateMediaItemRequest,
  UpdateMediaItemRequestStatusEnum,
} from "@/shared/api";
import { messageOf } from "@/shared/types/OperationResult";
import { useToastModal } from "@/shared/Utils/ToastModal";
import { useSiteColors } from "@/shared/Utils/useSiteColors";

import styles from "./CinemaQueuePage.module.scss";
import CreateMediaItemModal from "./components/CreateMediaItemModal";
import EditMediaItemModal from "./components/EditMediaItemModal";
import MediaItemsTable from "./components/MediaItemsTable";
import StatisticsCards from "./components/StatisticsCards";

const CinemaQueuePage: React.FC = () => {
  const [mediaItems, setMediaItems] = useState<CinemaMediaItemDto[]>([]);
  const [statistics, setStatistics] = useState<CinemaQueueStatistics | null>(
    null
  );
  const [statisticsError, setStatisticsError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingItem, setEditingItem] = useState<CinemaMediaItemDto | null>(
    null
  );
  const [formData, setFormData] = useState<CreateMediaItemRequest>({
    title: "",
    description: "",
    mediaUrl: "",
    priority: 1,
    scheduledFor: new Date(Date.now() + 60 * 60 * 1000)
      .toISOString()
      .slice(0, 16),
    addedBy: "",
    twitchUsername: "",
    notes: "",
  });
  const [editFormData, setEditFormData] = useState<
    Partial<UpdateMediaItemRequest>
  >({});

  const { showToast } = useToastModal();
  const colors = useSiteColors();
  const cinemaQueueApi = useMemo(() => new CinemaQueue(), []);

  const pageStyles = useMemo(
    () => ({
      backgroundColor: colors.background.primary,
      color: colors.text.primary,
    }),
    [colors]
  );

  const headerStyles = useMemo(
    () => ({
      backgroundColor: colors.background.card,
      borderColor: colors.border.primary,
      boxShadow: colors.shadow.light,
    }),
    [colors]
  );

  const fetchMediaItems = useCallback(async () => {
    try {
      setLoading(true);
      const response = await cinemaQueueApi.cinemaQueueList();
      setMediaItems(response.data.data ?? []);
    } catch (error) {
      showToast({
        success: false,
        message: messageOf(error, "Ошибка при загрузке медиа элементов"),
      });
    } finally {
      setLoading(false);
    }
  }, [cinemaQueueApi, showToast]);

  const fetchStatistics = useCallback(async () => {
    try {
      const response = await cinemaQueueApi.cinemaQueueStatisticsList();
      if (response.status === 200) {
        setStatisticsError(null);
        setStatistics(response.data.data ?? null);
      }
    } catch (error) {
      // Причина показывается, а не только пишется в консоль: карточки статистики
      // рендерятся по `statistics && <StatisticsCards/>`, то есть при отказе они
      // молча исчезали — это то самое «пусто вместо ответа».
      setStatisticsError(messageOf(error, "Не удалось загрузить статистику"));
    }
  }, [cinemaQueueApi]);

  const resetCreateForm = useCallback(() => {
    setFormData({
      title: "",
      description: "",
      mediaUrl: "",
      priority: 1,
      scheduledFor: new Date(Date.now() + 60 * 60 * 1000)
        .toISOString()
        .slice(0, 16),
      addedBy: "",
      twitchUsername: "",
      notes: "",
    });
  }, []);

  useEffect(() => {
    fetchMediaItems();
    fetchStatistics();
  }, [fetchMediaItems, fetchStatistics]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const apiFormData = {
        ...formData,
        scheduledFor: formData.scheduledFor
          ? new Date(formData.scheduledFor).toISOString()
          : undefined,
      };

      const response = await cinemaQueueApi.cinemaQueueCreate(apiFormData);
      showToast(response.data);
      setModalVisible(false);
      resetCreateForm();
      fetchMediaItems();
      fetchStatistics();
    } catch (error) {
      showToast({
        success: false,
        message: messageOf(error, "Ошибка при создании медиа элемента"),
      });
    }
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingItem) return;

    try {
      const apiUpdateData: UpdateMediaItemRequest = {
        ...editFormData,
        status: editFormData.status || UpdateMediaItemRequestStatusEnum.Pending,
        scheduledFor: editFormData.scheduledFor
          ? new Date(editFormData.scheduledFor).toISOString()
          : undefined,
      };

      const response = await cinemaQueueApi.cinemaQueueUpdate(
        editingItem.id,
        apiUpdateData
      );
      showToast(response.data);
      setEditingItem(null);
      setEditFormData({});
      fetchMediaItems();
      fetchStatistics();
    } catch (error) {
      showToast({
        success: false,
        message: messageOf(error, "Ошибка при обновлении медиа элемента"),
      });
    }
  };

  const handleDelete = async (id: string) => {
    try {
      const response = await cinemaQueueApi.cinemaQueueDelete(id);
      showToast(response.data);
      fetchMediaItems();
      fetchStatistics();
    } catch (error) {
      showToast({
        success: false,
        message: messageOf(error, "Ошибка при удалении медиа элемента"),
      });
    }
  };

  const handleMarkAsNext = async (id: string) => {
    try {
      const response = await cinemaQueueApi.cinemaQueueMarkAsNextCreate(id);
      showToast(response.data);
      fetchMediaItems();
    } catch (error) {
      showToast({
        success: false,
        message: messageOf(
          error,
          "Ошибка при отметке медиа элемента как следующего"
        ),
      });
    }
  };

  const handleInputChange = (
    field: keyof CreateMediaItemRequest,
    value: string | number
  ) => {
    setFormData(previous => ({ ...previous, [field]: value }));
  };

  const handleEditInputChange = (
    field: keyof UpdateMediaItemRequest,
    value: string | number | boolean
  ) => {
    setEditFormData(previous => ({ ...previous, [field]: value }));
  };

  const openEditModal = (item: CinemaMediaItemDto) => {
    setEditingItem(item);
    setEditFormData({
      title: item.title,
      description: item.description,
      mediaUrl: item.mediaUrl,
      status: item.status as unknown as UpdateMediaItemRequestStatusEnum,
      priority: item.priority,
      scheduledFor: item.scheduledFor,
      isNext: item.isNext,
      notes: item.notes,
    });
  };

  return (
    <div className={styles.cinemaQueuePage} style={pageStyles}>
      <div className={styles.header} style={headerStyles}>
        <h1 style={{ color: "var(--site-text-accent)" }}>
          Cinema Queue Management
        </h1>
        <div style={{ display: "flex", gap: 8 }}>
          <Button
            type="primary"
            onClick={() => setModalVisible(true)}
            size="large"
          >
            <Plus style={{ marginRight: 8 }} />
            Add New Item
          </Button>
        </div>
      </div>

      {statisticsError ? (
        <Alert message={statisticsError} type="warning" showIcon />
      ) : (
        statistics && <StatisticsCards statistics={statistics} />
      )}

      <MediaItemsTable
        mediaItems={mediaItems}
        loading={loading}
        onMarkAsNext={handleMarkAsNext}
        onEdit={openEditModal}
        onDelete={handleDelete}
      />

      <CreateMediaItemModal
        open={modalVisible}
        onCancel={() => {
          setModalVisible(false);
          resetCreateForm();
        }}
        onSubmit={handleCreate}
        formData={formData}
        onInputChange={handleInputChange}
      />

      <EditMediaItemModal
        open={!!editingItem}
        onCancel={() => {
          setEditingItem(null);
          setEditFormData({});
        }}
        onSubmit={handleUpdate}
        editFormData={editFormData}
        onInputChange={handleEditInputChange}
      />
    </div>
  );
};

export default CinemaQueuePage;
