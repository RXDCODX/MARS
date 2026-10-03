import { Button, DatePicker, Flex, Form, Input, Modal, Select } from "antd";

import { CreateMediaItemRequest } from "@/shared/api";

interface CreateMediaItemModalProperties {
  open: boolean;
  onCancel: () => void;
  onSubmit: (e: React.FormEvent) => void;
  formData: CreateMediaItemRequest;
  onInputChange: (
    field: keyof CreateMediaItemRequest,
    value: string | number
  ) => void;
}

const CreateMediaItemModal: React.FC<CreateMediaItemModalProperties> = ({
  open,
  onCancel,
  onSubmit,
  formData,
  onInputChange,
}) => (
  <Modal
    open={open}
    onCancel={onCancel}
    width={720}
    title="Add New Media Item"
    footer={
      <Flex gap={8}>
        <Button type="primary" onClick={onSubmit as any}>
          Create
        </Button>
        <Button onClick={onCancel}>Cancel</Button>
      </Flex>
    }
  >
    <Form layout="vertical" onFinish={onSubmit}>
      <Form.Item label="Title">
        <Input
          value={formData.title || ""}
          onChange={e => onInputChange("title", e.target.value)}
          placeholder="Enter title"
        />
      </Form.Item>

      <Form.Item label="Description">
        <Input.TextArea
          rows={3}
          value={formData.description || ""}
          onChange={e => onInputChange("description", e.target.value)}
          placeholder="Enter description"
        />
      </Form.Item>

      <Form.Item label="Media URL" required>
        <Input
          value={formData.mediaUrl}
          onChange={e => onInputChange("mediaUrl", e.target.value)}
          placeholder="Enter media URL"
          required
        />
      </Form.Item>

      <Flex gap={16}>
        <Form.Item label="Priority" style={{ flex: 1 }}>
          <Select
            value={formData.priority}
            onChange={value => onInputChange("priority", value)}
            placeholder="Select priority"
            options={[
              { value: 1, label: "1 - Low" },
              { value: 2, label: "2 - Normal" },
              { value: 3, label: "3 - Medium" },
              { value: 4, label: "4 - High" },
              { value: 5, label: "5 - Critical" },
            ]}
          />
        </Form.Item>
        <Form.Item
          label="Scheduled For"
          style={{ flex: 1 }}
          help="Выберите дату и время для показа медиа элемента (опционально)"
        >
          <DatePicker
            showTime
            style={{ width: "100%" }}
            value={
              formData.scheduledFor ? (formData.scheduledFor as any) : undefined
            }
            onChange={(_date, dateString) =>
              onInputChange("scheduledFor", dateString as string)
            }
          />
        </Form.Item>
      </Flex>
      <Flex gap={16}>
        <Form.Item label="Added By" style={{ flex: 1 }}>
          <Input
            value={formData.addedBy || ""}
            onChange={e => onInputChange("addedBy", e.target.value)}
            placeholder="Enter your name"
          />
        </Form.Item>
        <Form.Item label="Twitch Username" style={{ flex: 1 }}>
          <Input
            value={formData.twitchUsername || ""}
            onChange={e => onInputChange("twitchUsername", e.target.value)}
            placeholder="Enter Twitch username"
          />
        </Form.Item>
      </Flex>

      <Form.Item label="Notes">
        <Input.TextArea
          rows={2}
          value={formData.notes || ""}
          onChange={e => onInputChange("notes", e.target.value)}
          placeholder="Additional notes"
        />
      </Form.Item>
    </Form>
  </Modal>
);

export default CreateMediaItemModal;
