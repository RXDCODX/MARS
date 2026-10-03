import type { MenuProps } from "antd";
import type { ReactNode } from "react";
import { Children, isValidElement } from "react";
import { Menu } from "antd";

interface NavbarProperties {
  bg?: string;
  variant?: string;
  expand?: string;
  className?: string;
  children?: ReactNode;
}

const Navbar = ({ className, children }: NavbarProperties) => (
  <nav className={className}>{children}</nav>
);

interface NavProperties {
  className?: string;
  activeKey?: string;
  onSelect?: (key: string | null) => void;
  children?: ReactNode;
}

const NavLinks = ({
  className,
  activeKey,
  onSelect,
  children,
}: NavProperties) => {
  const items: MenuProps["items"] = [];

  const processChildren = (child: React.ReactNode) => {
    if (!child) return;
    Children.forEach(child, c => {
      if (!(isValidElement(c) && c.type === NavItem)) {
        return;
      }

      const { eventKey, children: label } = c.props as NavItemProperties;
      items?.push({ key: eventKey, label });
    });
  };

  processChildren(children);

  return (
    <Menu
      mode="horizontal"
      selectedKeys={activeKey ? [activeKey] : []}
      onClick={({ key }) => onSelect?.(key)}
      items={items}
      className={className}
    />
  );
};

interface NavItemProperties {
  /**
   * Ключ пункта меню.
   *
   *
   * Обязателен, а не необязателен: antd требует key у каждого пункта,
   * потому что по нему выбирается активный и на нём держится состояние
   * открытой вкладки. Пункт без ключа нельзя ни отметить, ни найти, поэтому
   * отсутствие ключа — ошибка в разметке, а не повод пропустить пункт.
   */
  eventKey: string;
  children?: ReactNode;
}

const NavItem = (_properties: NavItemProperties): null => null;

interface NavTabsProperties {
  activeKey?: string;
  onSelect?: (key: string) => void;
  className?: string;
  children?: ReactNode;
}

const NavTabs = ({
  activeKey,
  onSelect,
  className,
  children,
}: NavTabsProperties) => {
  const items: MenuProps["items"] = [];

  const processChildren = (child: React.ReactNode) => {
    if (!child) return;
    Children.forEach(child, c => {
      if (!(isValidElement(c) && c.type === NavItem)) {
        return;
      }

      const { eventKey, children: label } = c.props as NavItemProperties;
      items?.push({ key: eventKey, label });
    });
  };

  processChildren(children);

  return (
    <Menu
      mode="horizontal"
      selectedKeys={activeKey ? [activeKey] : []}
      onClick={({ key }) => onSelect?.(key)}
      items={items}
      className={className}
    />
  );
};

export { Navbar, NavLinks, NavTabs, NavItem };
