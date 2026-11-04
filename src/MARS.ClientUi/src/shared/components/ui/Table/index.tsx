import type { TableProps as AntTableProperties } from "antd";
import { Table as AntTable } from "antd";

interface TableProperties<T> extends AntTableProperties<T> {
  bordered?: boolean;
}

const Table = <T extends object>({
  bordered,
  className,
  ...properties
}: TableProperties<T>) => (
  <AntTable
    bordered={bordered}
    className={className}
    pagination={false}
    {...properties}
  />
);

export default Table;
