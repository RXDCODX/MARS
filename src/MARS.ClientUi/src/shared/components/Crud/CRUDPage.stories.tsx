import type { Meta, StoryObj } from "@storybook/react-vite";

import { CRUDPage } from ".";
import { ColumnDef, DataSource, FormSchema } from "./types";

type User = {
  id: number;
  name: string;
  age: number;
  active: boolean;
  role: "admin" | "user";
};

const inMemory: User[] = Array.from({ length: 57 }).map((_, index) => ({
  id: index + 1,
  name: `User ${index + 1}`,
  age: 18 + ((index * 7) % 40),
  active: index % 3 === 0,
  role: index % 5 === 0 ? "admin" : "user",
}));

const sleep = (ms: number) => new Promise(res => setTimeout(res, ms));

const dataSource: DataSource<User, Partial<User>, Partial<User>> = {
  async list({ page = 1, pageSize = 10, sortBy, sortDir, search }) {
    await sleep(120);
    let data = [...inMemory];
    if (search) {
      data = data.filter(x =>
        x.name.toLowerCase().includes(search.toLowerCase())
      );
    }
    if (sortBy) {
      data.sort((a: User, b: User) => {
        const av = a[sortBy! as keyof User];
        const bv = b[sortBy! as keyof User];
        if (av === bv) return 0;
        return (av > bv ? 1 : -1) * (sortDir === "desc" ? -1 : 1);
      });
    }
    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    return { items: data.slice(start, end), total: data.length };
  },
  async getById(id: string | number) {
    await sleep(80);
    const numberId = typeof id === "string" ? parseInt(id, 10) : id;
    const found = inMemory.find(x => x.id === numberId)!;
    return { ...found };
  },
  async create(payload) {
    await sleep(150);
    const id = Math.max(...inMemory.map(x => x.id)) + 1;
    const created = {
      id,
      name: payload.name ?? "New",
      age: Number(payload.age ?? 18),
      active: Boolean(payload.active),
      role: (payload.role as User["role"]) ?? "user",
    } as User;
    inMemory.push(created);
    return created;
  },
  async update(id, payload) {
    await sleep(150);
    const index = inMemory.findIndex(x => x.id === id);
    inMemory[index] = { ...inMemory[index], ...(payload as Partial<User>) };
    return { ...inMemory[index] };
  },
  async remove(id) {
    await sleep(120);
    const index = inMemory.findIndex(x => x.id === id);
    if (index !== -1) inMemory.splice(index, 1);
  },
};

const columns: Array<ColumnDef<User>> = [
  { id: "name", header: "Имя", accessor: "name", sortable: true },
  { id: "age", header: "Возраст", accessor: "age", sortable: true, width: 120 },
  {
    id: "active",
    header: "Активен",
    accessor: r => (r.active ? "Да" : "Нет"),
    width: 120,
  },
  { id: "role", header: "Роль", accessor: "role", width: 120 },
];

const formSchema: FormSchema<User> = {
  layout: "grid",
  fields: [
    { name: "name", label: "Имя", type: "text", required: true },
    { name: "age", label: "Возраст", type: "number", min: 1, max: 120 },
    { name: "active", label: "Активен", type: "boolean" },
    {
      name: "role",
      label: "Роль",
      type: "select",
      options: [
        { label: "Пользователь", value: "user" },
        { label: "Админ", value: "admin" },
      ],
    },
  ],
};

const meta: Meta<typeof CRUDPage<User>> = {
  title: "Shared/CRUD/CRUDPage",
  component: CRUDPage<User>,
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "Generic CRUD страница, конфигурируемая через generic-типы и пропсы.",
      },
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <CRUDPage<User>
      title="Пользователи"
      dataSource={dataSource}
      columns={columns}
      formSchema={formSchema}
      defaultSort={{ field: "name", dir: "asc" }}
    />
  ),
};
