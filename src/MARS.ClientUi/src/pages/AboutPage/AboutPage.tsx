import { Link } from "react-router-dom";

import styles from "./AboutPage.module.scss";

const AboutPage: React.FC = () => {
  const teamMembers = [
    {
      name: "Команда разработки",
      role: "Backend & Frontend",
      description: "Создание мощной архитектуры и современного интерфейса",
    },
    {
      name: "Дизайн команда",
      role: "UI/UX Design",
      description: "Создание красивых и удобных интерфейсов",
    },
    {
      name: "QA команда",
      role: "Тестирование",
      description: "Обеспечение качества и стабильности работы",
    },
  ];

  const milestones = [
    {
      year: "2024",
      title: "Запуск проекта",
      description: "Начало разработки MARS Client",
    },
    {
      year: "2024",
      title: "Первая версия",
      description: "Базовые OBS компоненты и панель управления",
    },
    {
      year: "2024",
      title: "Расширение функционала",
      description: "Добавление новых компонентов и улучшение UI",
    },
  ];

  return (
    <div className={styles.aboutPage}>
      {/* Hero Section */}
      <section className={styles.hero}>
        <div className={styles.container}>
          <h1 className={styles.heroTitle}>О проекте MARS Client</h1>
          <p className={styles.heroSubtitle}>
            Мощная платформа для создания профессиональных стримов с
            интерактивными компонентами
          </p>
        </div>
      </section>

      {/* Mission Section */}
      <section className={styles.mission}>
        <div className={styles.container}>
          <div className={styles.missionContent}>
            <div className={styles.missionText}>
              <h2>Наша миссия</h2>
              <p>
                MARS Client создан для того, чтобы помочь стримерам создавать
                качественный контент с минимальными усилиями. Мы предоставляем
                готовые решения для всех аспектов стриминга: от красивых алертов
                до мощной панели управления.
              </p>
              <p>
                Наша цель - сделать стриминг доступным и профессиональным для
                всех, кто хочет делиться своим контентом с миром.
              </p>
            </div>
            <div className={styles.missionVisual}>
              <div className={styles.statsCard}>
                <div className={styles.stat}>
                  <span className={styles.statNumber}>100+</span>
                  <span className={styles.statLabel}>Компонентов</span>
                </div>
                <div className={styles.stat}>
                  <span className={styles.statNumber}>1000+</span>
                  <span className={styles.statLabel}>Пользователей</span>
                </div>
                <div className={styles.stat}>
                  <span className={styles.statNumber}>24/7</span>
                  <span className={styles.statLabel}>Поддержка</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Team Section */}
      <section className={styles.team}>
        <div className={styles.container}>
          <h2 className={styles.sectionTitle}>Наша команда</h2>
          <div className={styles.teamGrid}>
            {teamMembers.map((member, index) => (
              <div key={index} className={styles.teamCard}>
                <div className={styles.memberAvatar}>
                  <span className={styles.avatarText}>
                    {member.name.charAt(0)}
                  </span>
                </div>
                <h3 className={styles.memberName}>{member.name}</h3>
                <p className={styles.memberRole}>{member.role}</p>
                <p className={styles.memberDescription}>{member.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Timeline Section */}
      <section className={styles.timeline}>
        <div className={styles.container}>
          <h2 className={styles.sectionTitle}>История развития</h2>
          <div className={styles.timelineContainer}>
            {milestones.map((milestone, index) => (
              <div key={index} className={styles.timelineItem}>
                <div className={styles.timelineYear}>{milestone.year}</div>
                <div className={styles.timelineContent}>
                  <h3 className={styles.timelineTitle}>{milestone.title}</h3>
                  <p className={styles.timelineDescription}>
                    {milestone.description}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className={styles.cta}>
        <div className={styles.container}>
          <h2>Готовы присоединиться?</h2>
          <p>Начните использовать MARS Client уже сегодня</p>
          <div className={styles.ctaButtons}>
            <Link to="/pyroalerts" className={styles.primaryButton}>
              Попробовать бесплатно
            </Link>
            <Link to="/contacts" className={styles.secondaryButton}>
              Связаться с нами
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
};

export default AboutPage;
