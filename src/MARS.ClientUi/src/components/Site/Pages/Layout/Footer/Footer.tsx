import { Flex, Typography } from "antd";
import { Link } from "react-router-dom";

import styles from "./Footer.module.scss";

const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();

  const linkSections = [
    {
      title: "Быстрые ссылки",
      links: [
        { to: "/", label: "Главная" },
        { to: "/about", label: "О проекте" },
        { to: "/docs", label: "Документация" },
        { to: "/contacts", label: "Контакты" },
      ],
    },
    {
      title: "OBS Компоненты",
      links: [
        { to: "/pyroalerts", label: "Pyro Alerts" },
        { to: "/waifu", label: "Waifu Alerts" },
        { to: "/chath", label: "Горизонтальный чат" },
        { to: "/chatv", label: "Вертикальный чат" },
      ],
    },
    {
      title: "Панель управления",
      links: [
        { to: "/admin", label: "Админ панель" },
        { to: "/dashboard", label: "Дашборд" },
        { to: "/main", label: "Просмотр серверов" },
        { to: "/services", label: "Сервисы" },
      ],
    },
  ];

  return (
    <footer className={styles.footer} data-testid="footer">
      <div className={styles.container}>
        <div className={styles.content}>
          <div>
            <Typography.Title level={4} style={{ marginBottom: 12 }}>
              🚀 MARS Client
            </Typography.Title>
            <Typography.Paragraph type="secondary" style={{ fontSize: 14 }}>
              Мощная платформа для управления стримингом и создания
              интерактивных компонентов для OBS.
            </Typography.Paragraph>
          </div>

          {linkSections.map(section => (
            <div key={section.title}>
              <Typography.Text
                strong
                style={{
                  display: "block",
                  marginBottom: 12,
                  fontSize: 13,
                  textTransform: "uppercase",
                  letterSpacing: 1,
                }}
              >
                {section.title}
              </Typography.Text>
              <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                {section.links.map(link => (
                  <li key={link.to} style={{ marginBottom: 8 }}>
                    <Link
                      to={link.to}
                      style={{
                        color: "var(--site-text-secondary)",
                        textDecoration: "none",
                        fontSize: 14,
                        transition: "color 0.2s",
                      }}
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <hr
          style={{
            border: "none",
            borderTop: "1px solid var(--site-border-primary)",
            margin: "24px 0",
          }}
        />

        <div className={styles.bottom}>
          <Typography.Text type="secondary" style={{ fontSize: 14 }}>
            &copy; {currentYear} MARS Client. Все права защищены.
          </Typography.Text>
          <Flex gap={12}>
            {[
              { href: "#", label: "GitHub", icon: "🐙" },
              { href: "#", label: "Discord", icon: "💬" },
              { href: "#", label: "Twitter", icon: "🐦" },
            ].map(social => (
              <a
                key={social.label}
                href={social.href}
                aria-label={social.label}
                style={{
                  fontSize: 18,
                  color: "var(--site-text-secondary)",
                  textDecoration: "none",
                  transition: "transform 0.2s",
                }}
              >
                {social.icon}
              </a>
            ))}
          </Flex>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
