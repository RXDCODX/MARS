import { ApiOutlined, AppstoreOutlined, MenuOutlined } from "@ant-design/icons";
import { Button, Dropdown, Flex, Space, Typography } from "antd";
import { Suspense, useState } from "react";
import { Link, useLocation } from "react-router-dom";

import StyleSelector from "@/components/StyleSelector/StyleSelector";
import ThemeToggle from "@/components/ThemeToggle";

interface NavigationItem {
  label: string;
  path: string;
  icon?: string;
}

interface NavigationGroup {
  label: string;
  icon?: string;
  children: NavigationItem[];
}

const Header: React.FC = () => {
  const [mobileOpen, setMobileOpen] = useState(false);
  const location = useLocation();

  const sitePages: NavigationItem[] = [
    { label: "Главная", path: "/", icon: "🏠" },
    { label: "О проекте", path: "/about", icon: "ℹ️" },
    { label: "Документация", path: "/docs", icon: "📖" },
    { label: "Команды", path: "/commands", icon: "⌨️" },
    { label: "Контакты", path: "/contacts", icon: "📧" },
    { label: "Управление алертами", path: "/media-info", icon: "🔔" },
    { label: "Cinema Queue", path: "/cinema-queue", icon: "🎬" },
    { label: "Все маршруты", path: "/routes", icon: "🗺️" },
  ];

  const obsComponents: NavigationGroup[] = [
    {
      label: "Чат",
      icon: "💬",
      children: [
        { label: "Горизонтальный чат", path: "/chath" },
        { label: "Вертикальный чат", path: "/chatv" },
      ],
    },
    {
      label: "Алерты",
      icon: "🔔",
      children: [
        { label: "Pyro Alerts", path: "/pyroalerts" },
        { label: "Waifu Alerts", path: "/waifu" },
        { label: "Highlight Message", path: "/highlite" },
        { label: "Auto Message Billboard", path: "/automessage" },
      ],
    },
    {
      label: "Развлечения",
      icon: "🎮",
      children: [
        { label: "Fumo Friday", path: "/fumofriday" },
        { label: "Random Mem", path: "/randommem" },
        { label: "Экранные частицы", path: "/confetti" },
        { label: "AFK Screen", path: "/afkscreen" },
        { label: "ADHD Controller", path: "/adhd" },
      ],
    },
    {
      label: "Звук",
      icon: "🎵",
      children: [{ label: "Текущий трек", path: "/sr/currenttrack" }],
    },
  ];

  const controlRoomPages: NavigationItem[] = [
    { label: "Панель управления", path: "/admin", icon: "⚙️" },
    { label: "Логи", path: "/logs", icon: "📋" },
    { label: "Дашборд", path: "/dashboard", icon: "📊" },
    { label: "Просмотр серверов", path: "/main", icon: "🖥️" },
    { label: "Сервисы", path: "/services", icon: "🔌" },
    { label: "RootState", path: "/root-state", icon: "💾" },
    { label: "Твич награды", path: "/twitch-rewards", icon: "🎁" },
    { label: "Пользователи Twitch", path: "/twitch-users", icon: "👤" },
    { label: "Автосообщения", path: "/auto-messages", icon: "📨" },
    {
      label: "Telegram ↔ Discord Bridge",
      path: "/telegram-discord-bridge",
      icon: "🔗",
    },
  ];

  const isActive = (path: string) => location.pathname === path;

  const buildDropdownItems = (items: (NavigationItem | NavigationGroup)[]) =>
    items.map(item => {
      if ("children" in item) {
        return {
          key: item.label,
          label: (
            <span>
              {item.icon && <span style={{ marginRight: 6 }}>{item.icon}</span>}
              {item.label}
            </span>
          ),
          children: item.children.map(child => ({
            key: child.path,
            label: (
              <Link to={child.path} data-testid={`nav-link-${child.path}`}>
                {child.icon && (
                  <span style={{ marginRight: 6 }}>{child.icon}</span>
                )}
                {child.label}
              </Link>
            ),
          })),
        };
      }
      return {
        key: item.path,
        label: (
          <Link to={item.path} data-testid={`nav-link-${item.path}`}>
            {item.icon && <span style={{ marginRight: 6 }}>{item.icon}</span>}
            {item.label}
          </Link>
        ),
      };
    });

  return (
    <nav
      style={{
        position: "sticky",
        top: 0,
        zIndex: 1030,
        borderBottom: "3px solid var(--site-border-primary)",
        backgroundColor: "var(--site-bg-secondary)",
        backdropFilter: "blur(12px)",
      }}
      data-testid="navbar"
    >
      <div
        style={{
          maxWidth: 1200,
          margin: "0 auto",
          padding: "8px 24px",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
        }}
      >
        <Link
          to="/"
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "4px 8px",
            borderRadius: 12,
            textDecoration: "none",
          }}
          data-testid="nav-logo"
        >
          <span style={{ fontSize: 20 }}>🚀</span>
          <Typography.Text
            strong
            style={{
              fontSize: 18,
              background:
                "linear-gradient(135deg, var(--site-text-primary), var(--site-text-accent))",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
            }}
          >
            MARS
          </Typography.Text>
        </Link>

        <Button
          type="text"
          icon={<MenuOutlined />}
          onClick={() => setMobileOpen(!mobileOpen)}
          style={{ display: "none" }}
          className="mobile-menu-btn"
          data-testid="nav-mobile-toggle"
        />

        <Flex
          gap={4}
          align="center"
          className="desktop-nav"
          style={{ display: "flex" }}
        >
          <Dropdown
            menu={{ items: buildDropdownItems(sitePages) }}
            trigger={["hover"]}
            placement="bottomLeft"
            data-testid="nav-dropdown-site"
          >
            <Button type="text" size="small">
              Страницы сайта ▾
            </Button>
          </Dropdown>

          <Dropdown
            menu={{ items: buildDropdownItems(obsComponents) }}
            trigger={["hover"]}
            placement="bottomLeft"
            data-testid="nav-dropdown-obs"
          >
            <Button type="text" size="small">
              OBS ▾
            </Button>
          </Dropdown>

          <Dropdown
            menu={{ items: buildDropdownItems(controlRoomPages) }}
            trigger={["hover"]}
            placement="bottomLeft"
            data-testid="nav-dropdown-control"
          >
            <Button type="text" size="small">
              Управление ▾
            </Button>
          </Dropdown>
        </Flex>

        <Space className="desktop-nav" style={{ display: "flex" }}>
          <Suspense fallback={null}>
            <ThemeToggle />
          </Suspense>
          <StyleSelector />
          <Button
            type="text"
            size="small"
            icon={<ApiOutlined />}
            href="/ui"
            target="_blank"
            rel="noopener noreferrer"
            title="Swagger UI"
            data-testid="nav-link-ui"
          />
          <Button
            type="text"
            size="small"
            icon={<AppstoreOutlined />}
            href="/static"
            target="_blank"
            rel="noopener noreferrer"
            title="Static files"
            data-testid="nav-link-static"
          />
        </Space>
      </div>

      {mobileOpen && (
        <div
          style={{
            borderTop: "1px solid var(--site-border-primary)",
            backgroundColor: "var(--site-bg-secondary)",
            padding: "12px 16px",
          }}
          className="mobile-nav-panel"
        >
          <MobileNav
            sitePages={sitePages}
            obsComponents={obsComponents}
            controlRoomPages={controlRoomPages}
            isActive={isActive}
            onLinkClick={() => setMobileOpen(false)}
          />
          <Flex
            justify="center"
            gap={8}
            style={{
              marginTop: 12,
              paddingTop: 12,
              borderTop: "1px solid var(--site-border-primary)",
            }}
          >
            <Suspense fallback={null}>
              <ThemeToggle />
            </Suspense>
            <StyleSelector />
          </Flex>
        </div>
      )}

      <style>{`
        @media (max-width: 1024px) {
          .desktop-nav { display: none !important; }
          .mobile-menu-btn { display: inline-flex !important; }
        }
        @media (min-width: 1025px) {
          .mobile-nav-panel { display: none !important; }
        }
      `}</style>
    </nav>
  );
};

interface MobileNavProperties {
  sitePages: NavigationItem[];
  obsComponents: NavigationGroup[];
  controlRoomPages: NavigationItem[];
  isActive: (path: string) => boolean;
  onLinkClick: () => void;
}

const MobileNav: React.FC<MobileNavProperties> = ({
  sitePages,
  obsComponents,
  controlRoomPages,
  isActive,
  onLinkClick,
}) => {
  const [openSection, setOpenSection] = useState<string | null>(null);

  const sections = [
    { key: "site", label: "Страницы сайта", items: sitePages },
    {
      key: "obs",
      label: "OBS Компоненты",
      items: obsComponents.flatMap(g => g.children),
    },
    { key: "control", label: "Управление", items: controlRoomPages },
  ];

  return (
    <Flex vertical gap={4}>
      {sections.map(section => (
        <div key={section.key}>
          <Button
            type="text"
            block
            onClick={() =>
              setOpenSection(openSection === section.key ? null : section.key)
            }
            style={{ justifyContent: "space-between", textAlign: "left" }}
          >
            {section.label}
            <span
              style={{
                transform:
                  openSection === section.key ? "rotate(180deg)" : "none",
                transition: "transform 0.2s",
              }}
            >
              ▾
            </span>
          </Button>
          {openSection === section.key && (
            <div
              style={{
                marginLeft: 16,
                borderLeft: "2px solid var(--site-border-primary)",
                paddingLeft: 12,
              }}
            >
              {section.items.map((item, index) => (
                <Link
                  key={index}
                  to={item.path}
                  onClick={onLinkClick}
                  style={{
                    display: "block",
                    padding: "6px 12px",
                    fontSize: 14,
                    color: isActive(item.path)
                      ? "var(--site-text-accent)"
                      : "var(--site-text-primary)",
                    textDecoration: "none",
                    borderRadius: 8,
                    backgroundColor: isActive(item.path)
                      ? "var(--site-hover-bg)"
                      : "transparent",
                  }}
                >
                  {item.icon && (
                    <span style={{ marginRight: 8 }}>{item.icon}</span>
                  )}
                  {item.label}
                </Link>
              ))}
            </div>
          )}
        </div>
      ))}
    </Flex>
  );
};

export default Header;
