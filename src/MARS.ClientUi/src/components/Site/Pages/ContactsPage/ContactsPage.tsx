import { useState } from "react";

import styles from "./ContactsPage.module.scss";

const ContactsPage: React.FC = () => {
  const [formData, setFormData] = useState({
    name: "",
    email: "",
    subject: "",
    message: "",
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState<
    "idle" | "success" | "error"
  >("idle");

  const handleInputChange = (
    e: React.ChangeEvent<
      HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
    >
  ) => {
    const { name, value } = e.target;
    setFormData(previous => ({
      ...previous,
      [name]: value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    // Имитация отправки формы
    setTimeout(() => {
      setIsSubmitting(false);
      setSubmitStatus("success");
      setFormData({ name: "", email: "", subject: "", message: "" });

      // Сброс статуса через 3 секунды
      setTimeout(() => setSubmitStatus("idle"), 3000);
    }, 2000);
  };

  const contactInfo = [
    {
      icon: "📧",
      title: "Email",
      value: "support@marsclient.com",
      link: "mailto:support@marsclient.com",
    },
    {
      icon: "💬",
      title: "Discord",
      value: "MARS Client Community",
      link: "#",
    },
    {
      icon: "🐙",
      title: "GitHub",
      value: "github.com/marsclient",
      link: "#",
    },
    {
      icon: "📱",
      title: "Telegram",
      value: "@marsclient_support",
      link: "#",
    },
  ];

  return (
    <div className={styles.contactsPage}>
      {/* Hero Section */}
      <section className={styles.hero}>
        <div className={styles.container}>
          <h1 className={styles.heroTitle}>Свяжитесь с нами</h1>
          <p className={styles.heroSubtitle}>
            Есть вопросы или предложения? Мы всегда готовы помочь!
          </p>
        </div>
      </section>

      {/* Contact Section */}
      <section className={styles.contact}>
        <div className={styles.container}>
          <div className={styles.contactContent}>
            {/* Contact Info */}
            <div className={styles.contactInfo}>
              <h2>Наши контакты</h2>
              <p>
                Выберите наиболее удобный для вас способ связи. Мы постараемся
                ответить в кратчайшие сроки.
              </p>

              <div className={styles.contactList}>
                {contactInfo.map((contact, index) => (
                  <a
                    key={index}
                    href={contact.link}
                    className={styles.contactItem}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    <div className={styles.contactIcon}>{contact.icon}</div>
                    <div className={styles.contactDetails}>
                      <h3>{contact.title}</h3>
                      <p>{contact.value}</p>
                    </div>
                  </a>
                ))}
              </div>
            </div>

            {/* Contact Form */}
            <div className={styles.contactForm}>
              <h2>Отправить сообщение</h2>
              <p>
                Заполните форму ниже, и мы свяжемся с вами в ближайшее время.
              </p>

              <form onSubmit={handleSubmit} className={styles.form}>
                <div className={styles.formGroup}>
                  <label htmlFor="name">Имя *</label>
                  <input
                    type="text"
                    id="name"
                    name="name"
                    value={formData.name}
                    onChange={handleInputChange}
                    required
                    placeholder="Ваше имя"
                  />
                </div>

                <div className={styles.formGroup}>
                  <label htmlFor="email">Email *</label>
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={handleInputChange}
                    required
                    placeholder="your@email.com"
                  />
                </div>

                <div className={styles.formGroup}>
                  <label htmlFor="subject">Тема *</label>
                  <select
                    id="subject"
                    name="subject"
                    value={formData.subject}
                    onChange={handleInputChange}
                    required
                  >
                    <option value="">Выберите тему</option>
                    <option value="general">Общий вопрос</option>
                    <option value="technical">Техническая поддержка</option>
                    <option value="feature">Предложение функции</option>
                    <option value="bug">Сообщить об ошибке</option>
                    <option value="partnership">Партнерство</option>
                  </select>
                </div>

                <div className={styles.formGroup}>
                  <label htmlFor="message">Сообщение *</label>
                  <textarea
                    id="message"
                    name="message"
                    value={formData.message}
                    onChange={handleInputChange}
                    required
                    rows={6}
                    placeholder="Опишите ваш вопрос или предложение..."
                  />
                </div>

                <button
                  type="submit"
                  className={styles.submitButton}
                  disabled={isSubmitting}
                >
                  {isSubmitting ? "Отправка..." : "Отправить сообщение"}
                </button>

                {submitStatus === "success" && (
                  <div className={styles.successMessage}>
                    ✅ Сообщение успешно отправлено! Мы свяжемся с вами в
                    ближайшее время.
                  </div>
                )}

                {submitStatus === "error" && (
                  <div className={styles.errorMessage}>
                    ❌ Произошла ошибка при отправке. Попробуйте еще раз.
                  </div>
                )}
              </form>
            </div>
          </div>
        </div>
      </section>

      {/* FAQ Section */}
      <section className={styles.faq}>
        <div className={styles.container}>
          <h2>Часто задаваемые вопросы</h2>
          <div className={styles.faqGrid}>
            <div className={styles.faqItem}>
              <h3>Как начать использовать MARS Client?</h3>
              <p>
                Скачайте приложение, создайте аккаунт и следуйте инструкциям по
                настройке. Наша документация поможет вам быстро освоить все
                функции.
              </p>
            </div>

            <div className={styles.faqItem}>
              <h3>Поддерживаются ли мобильные устройства?</h3>
              <p>
                Да, MARS Client работает на всех современных устройствах,
                включая смартфоны и планшеты.
              </p>
            </div>

            <div className={styles.faqItem}>
              <h3>Есть ли бесплатная версия?</h3>
              <p>
                Да, мы предлагаем бесплатный план с базовыми функциями. Для
                расширенных возможностей доступны платные подписки.
              </p>
            </div>

            <div className={styles.faqItem}>
              <h3>Как получить техническую поддержку?</h3>
              <p>
                Вы можете обратиться к нам через форму выше, Discord сервер или
                email. Мы отвечаем в течение 24 часов.
              </p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};

export default ContactsPage;
