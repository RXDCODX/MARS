import type { MenuProps } from "antd";
import { Menu } from "antd";

interface NavbarProperties {
  bg?: string;
  variant?: string;
  expand?: string;
  className?: string;
  children?: React.ReactNode;
}

const Navbar = ({ className, children }: NavbarProperties) => (
  <nav className={className}>{children}</nav>
);

interface NavProperties {
  className?: string;
  activeKey?: string;
  onSelect?: (key: string | null) => void;
  children?: React.ReactNode;
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
    React.Children.forEach(child, c => {
      if (!(React.isValidElement(c) && c.type === NavItem)) {
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
  eventKey?: string;
  children?: React.ReactNode;
}

const NavItem = (_properties: NavItemProperties): null => null;

interface NavTabsProperties {
  activeKey?: string;
  onSelect?: (key: string) => void;
  className?: string;
  children?: React.ReactNode;
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
    React.Children.forEach(child, c => {
      if (!(React.isValidElement(c) && c.type === NavItem)) {
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
