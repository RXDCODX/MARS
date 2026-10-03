import { Button, Card, Flex, Spin, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";
import { Edit as Pencil, PlayCircle, Trash2 as Trash } from "lucide-react";

import { CinemaMediaItemDto } from "@/shared/api";

import styles from "../CinemaQueuePage.module.scss";

interface MediaItemsTableProperties {
  mediaItems: CinemaMediaItemDto[];
  loading: boolean;
  onMarkAsNext: (id: string) => void;
  onEdit: (item: CinemaMediaItemDto) => void;
  onDelete: (id: string) => void;
}

const statusColorMap: Record<string, string> = {
  Pending: "blue",
  InProgress: "orange",
  Completed: "green",
  Cancelled: "red",
  Postponed: "cyan",
};

const MediaItemsTable: React.FC<MediaItemsTableProperties> = ({
  mediaItems,
  loading,
  onMarkAsNext,
  onEdit,
  onDelete,
}) => {
  const getPriorityTag = (priority: number) => {
    if (priority >= 5) return "red";
    if (priority >= 3) return "orange";
    return "green";
  };

  const columns: ColumnsType<CinemaMediaItemDto> = [
    {
      title: "Next",
      dataIndex: "isNext",
      key: "isNext",
      width: 60,
      render: (isNext: boolean) =>
        isNext ? <Badge status="success" text="NEXT" /> : null,
    },
    {
      title: "Title",
      key: "title",
      render: (_: any, record: CinemaMediaItemDto) => (
        <div className={styles.titleCell}>
          <div className={styles.titleText}>{record.title || "No title"}</div>
          {record.description && (
            <div className={styles.descriptionText}>{record.description}</div>
          )}
        </div>
      ),
    },
    {
      title: "Status",
      dataIndex: "status",
      key: "status",
      width: 120,
      render: (status: string) => (
        <Tag color={statusColorMap[status] || "default"}>{status}</Tag>
      ),
    },
    {
      title: "Priority",
      dataIndex: "priority",
      key: "priority",
      width: 80,
      render: (priority: number) => (
        <Tag color={getPriorityTag(priority)}>{priority}</Tag>
      ),
    },
    {
      title: "Added By",
      key: "addedBy",
      width: 120,
      render: (_: any, record: CinemaMediaItemDto) => (
        <div>
          <div>{record.addedBy}</div>
          {record.twitchUsername && (
            <div className={styles.twitchUsername}>
              @{record.twitchUsername}
            </div>
          )}
        </div>
      ),
    },
    {
      title: "Scheduled For",
      dataIndex: "scheduledFor",
      key: "scheduledFor",
      width: 200,
      render: (value: string) =>
        value ? new Date(value).toLocaleString() : "Not scheduled",
    },
    {
      title: "Created",
      dataIndex: "createdAt",
      key: "createdAt",
      width: 120,
      render: (value: string) => new Date(value).toLocaleDateString(),
    },
    {
      title: "Actions",
      key: "actions",
      width: 400,
      render: (_: any, record: CinemaMediaItemDto) => (
        <Flex gap={4}>
          <Button
            type="primary"
            size="small"
            onClick={() => onMarkAsNext(record.id)}
            disabled={record.isNext}
          >
            <PlayCircle style={{ marginRight: 4 }} />
            Mark Next
          </Button>
          <Button size="small" onClick={() => onEdit(record)}>
            <Pencil style={{ marginRight: 4 }} />
            Edit
          </Button>
          <Button danger size="small" onClick={() => onDelete(record.id)}>
            <Trash style={{ marginRight: 4 }} />
            Delete
          </Button>
        </Flex>
      ),
    },
  ];

  return (
    <Card className={styles.tableCard} title="Queue Items">
      <Spin spinning={loading}>
        <Table
          columns={columns}
          dataSource={mediaItems}
          rowKey="id"
          rowClassName={(record: CinemaMediaItemDto) =>
            record.isNext ? styles.nextRow : ""
          }
          pagination={false}
          scroll={{ x: 1200 }}
        />
      </Spin>
    </Card>
  );
};

export default MediaItemsTable;
