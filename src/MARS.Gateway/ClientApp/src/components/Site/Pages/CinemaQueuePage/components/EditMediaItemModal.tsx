import { Button, DatePicker, Flex, Form, Input, Modal, Select } from "antd";

import { UpdateMediaItemRequest } from "@/shared/api";

interface EditMediaItemModalProperties {
  open: boolean;
  onCancel: () => void;
  onSubmit: (e: React.FormEvent) => void;
  editFormData: Partial<UpdateMediaItemRequest>;
  onInputChange: (
    field: keyof UpdateMediaItemRequest,
    value: string | number | boolean
  ) => void;
}

const EditMediaItemModal: React.FC<EditMediaItemModalProperties> = ({
  open,
  onCancel,
  onSubmit,
  editFormData,
  onInputChange,
}) => (
  <Modal
    open={open}
    onCancel={onCancel}
    width={720}
    title="Edit Media Item"
    footer={
      <Flex gap={8}>
        <Button type="primary" onClick={onSubmit as any}>
          Update
        </Button>
        <Button onClick={onCancel}>Cancel</Button>
      </Flex>
    }
  >
    <Form layout="vertical" onFinish={onSubmit}>
      <Form.Item label="Title">
        <Input
          value={editFormData.title || ""}
          onChange={e => onInputChange("title", e.target.value)}
          placeholder="Enter title"
        />
      </Form.Item>

      <Form.Item label="Description">
        <Input.TextArea
          rows={3}
          value={editFormData.description || ""}
          onChange={e => onInputChange("description", e.target.value)}
          placeholder="Enter description"
        />
      </Form.Item>

      <Form.Item label="Media URL">
        <Input
          value={editFormData.mediaUrl || ""}
          onChange={e => onInputChange("mediaUrl", e.target.value)}
          placeholder="Enter media URL"
        />
      </Form.Item>

      <Flex gap={16}>
        <Form.Item label="Status" style={{ flex: 1 }}>
          <Select
            value={editFormData.status || undefined}
            onChange={value => onInputChange("status", value)}
            placeholder="Select status"
            options={[
              { value: "Pending", label: "Pending" },
              { value: "InProgress", label: "In Progress" },
              { value: "Completed", label: "Completed" },
              { value: "Cancelled", label: "Cancelled" },
              { value: "Postponed", label: "Postponed" },
            ]}
          />
        </Form.Item>
        <Form.Item label="Priority" style={{ flex: 1 }}>
          <Select
            value={editFormData.priority || undefined}
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
      </Flex>
      <Flex gap={16}>
        <Form.Item
          label="Scheduled For"
          style={{ flex: 1 }}
          help="Выберите дату и время для показа медиа элемента"
        >
          <DatePicker
            showTime
            style={{ width: "100%" }}
            value={
              editFormData.scheduledFor
                ? (editFormData.scheduledFor as any)
                : undefined
            }
            onChange={(_date, dateString) =>
              onInputChange("scheduledFor", dateString as string)
            }
          />
        </Form.Item>
        <Form.Item label="Mark as Next" style={{ flex: 1 }}>
          <Select
            value={editFormData.isNext?.toString() || undefined}
            onChange={value => onInputChange("isNext", value === "true")}
            placeholder="Select"
            options={[
              { value: "true", label: "Yes" },
              { value: "false", label: "No" },
            ]}
          />
        </Form.Item>
      </Flex>

      <Form.Item label="Notes">
        <Input.TextArea
          rows={2}
          value={editFormData.notes || ""}
          onChange={e => onInputChange("notes", e.target.value)}
          placeholder="Additional notes"
        />
      </Form.Item>
    </Form>
  </Modal>
);

export default EditMediaItemModal;
