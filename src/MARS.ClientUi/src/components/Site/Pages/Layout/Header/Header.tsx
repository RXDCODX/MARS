import { ApiOutlined, AppstoreOutlined, DatabaseOutlined, MenuOutlined } from "@ant-design/icons";
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
          >
            <Button type="text" size="small" data-testid="nav-dropdown-site">
              Страницы сайта ▾
            </Button>
          </Dropdown>

          <Dropdown
            menu={{ items: buildDropdownItems(obsComponents) }}
            trigger={["hover"]}
            placement="bottomLeft"
          >
            <Button type="text" size="small" data-testid="nav-dropdown-obs">
              OBS ▾
            </Button>
          </Dropdown>

          <Dropdown
            menu={{ items: buildDropdownItems(controlRoomPages) }}
            trigger={["hover"]}
            placement="bottomLeft"
          >
            <Button type="text" size="small" data-testid="nav-dropdown-control">
              Управление ▾
            </Button>
          </Dropdown>
        </Flex>

        <Space className="desktop-nav" style={{ display: "flex" }}>
          <Suspense fallback={null}>
            <ThemeToggle />
          </Suspense>
          <StyleSelector />
          <Dropdown
            menu={{
              items: [
                {
                  key: "gateway-swagger",
                  label: (
                    <a
                      href="/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-gateway"
                    >
                      Gateway (Swagger)
                    </a>
                  ),
                },
                { type: "divider" },
                {
                  key: "twitch-core",
                  label: (
                    <a
                      href="http://localhost:6002/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-twitch"
                    >
                      TwitchCore
                    </a>
                  ),
                },
                {
                  key: "waifu-gacha",
                  label: (
                    <a
                      href="http://localhost:6003/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-waifu"
                    >
                      WaifuGacha
                    </a>
                  ),
                },
                {
                  key: "shikimori",
                  label: (
                    <a
                      href="http://localhost:6027/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-shikimori"
                    >
                      Shikimori
                    </a>
                  ),
                },
                {
                  key: "telegram",
                  label: (
                    <a
                      href="http://localhost:6004/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-telegram"
                    >
                      Telegram
                    </a>
                  ),
                },
                {
                  key: "discord",
                  label: (
                    <a
                      href="http://localhost:6005/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-discord"
                    >
                      Discord
                    </a>
                  ),
                },
                {
                  key: "commands",
                  label: (
                    <a
                      href="http://localhost:6006/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-commands"
                    >
                      Commands
                    </a>
                  ),
                },
                {
                  key: "sound-request",
                  label: (
                    <a
                      href="http://localhost:6007/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-sound"
                    >
                      SoundRequest
                    </a>
                  ),
                },
                {
                  key: "tts",
                  label: (
                    <a
                      href="http://localhost:6008/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-tts"
                    >
                      TTS
                    </a>
                  ),
                },
                {
                  key: "obs",
                  label: (
                    <a
                      href="http://localhost:6009/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-obs"
                    >
                      OBS
                    </a>
                  ),
                },
                {
                  key: "alerts",
                  label: (
                    <a
                      href="http://localhost:6010/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-alerts"
                    >
                      Alerts
                    </a>
                  ),
                },
                {
                  key: "scoreboard",
                  label: (
                    <a
                      href="http://localhost:6011/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-scoreboard"
                    >
                      Scoreboard
                    </a>
                  ),
                },
                {
                  key: "cinema-queue",
                  label: (
                    <a
                      href="http://localhost:6012/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-cinema"
                    >
                      CinemaQueue
                    </a>
                  ),
                },
                {
                  key: "media-storage",
                  label: (
                    <a
                      href="http://localhost:6013/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-mediastorage"
                    >
                      MediaStorage
                    </a>
                  ),
                },
                {
                  key: "admin",
                  label: (
                    <a
                      href="http://localhost:6014/swagger"
                      target="_blank"
                      rel="noopener noreferrer"
                      data-testid="nav-swagger-admin"
                    >
                      Admin
                    </a>
                  ),
                },
              ],
            }}
            trigger={["hover"]}
            placement="bottomLeft"
          >
            <Button
              type="text"
              size="small"
              icon={<ApiOutlined />}
              title="Swagger UI (список всех сервисов)"
              data-testid="nav-dropdown-swagger"
            >
              Swagger ▾
            </Button>
          </Dropdown>
          <Button
            type="text"
            size="small"
            icon={<DatabaseOutlined />}
            href="/storage-ui"
            target="_blank"
            rel="noopener noreferrer"
            title="MediaStorage UI"
            data-testid="nav-link-mediastorage"
          >
            MediaStorage
          </Button>
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
