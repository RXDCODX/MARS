using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MARS.Telegram.Api.Rest.Endpoints;

/// <summary>
/// Минимальная страница входа в учётную запись Telegram и консоль отправки сообщений.
/// </summary>
/// <remarks>
/// <para>
/// Нужна на тот случай, когда служба запущена отдельно: полноценный сайт живёт в
/// клиенте Gateway, а без него страницы «WTelegram Клиент» нет вообще, и код
/// подтверждения вводить некуда — вход пользовательского аккаунта не завершается.
/// </para>
/// <para>
/// Страница отдаётся на любой неописанный адрес, а не на одномExact: при
/// опечатке в адресе человек попадает сюда же и видит, куда попал, вместо 404 без
/// подсказки. На mapped-адреса (API, gRPC-эндпоинты, swagger) это не влияет:
/// они описаны, и до запасного маршрута дело не доходит.
/// </para>
/// <para>
/// Ни одного внешнего ресурса: ни CDN, ни шрифтов, ни картинок. Машина владельца
/// за прокси, и страница, которая тянет стили извне, осталась бы голой.
/// </para>
/// </remarks>
public static class WTelegramStandalonePage
{
    /// <summary>Адрес статуса клиента: его опрашивает страница.</summary>
    public const string StatusApiPath = "/api/WTelegram/status";

    /// <summary>Адрес приёма кода подтверждения.</summary>
    public const string CodeApiPath = "/api/WTelegram/verification-code";

    /// <summary>Адрес принудительной переавторизации.</summary>
    public const string ReloginApiPath = "/api/WTelegram/relogin";

    /// <summary>Адрес снимка недавно полученных ботом сообщений.</summary>
    public const string InboxApiPath = "/api/WTelegram/inbox";

    /// <summary>Адрес пассивного статуса MTProto-подключения бота.</summary>
    public const string BotStatusApiPath = "/api/WTelegram/bot-status";

    /// <summary>Адрес активной проверки подключения бота.</summary>
    public const string BotCheckApiPath = "/api/WTelegram/bot-check";

    /// <summary>Адрес отправки сообщения администраторам из страницы.</summary>
    public const string SendMessageApiPath = "/api/WTelegram/send-message";

    private const int PollIntervalMs = 3000;

    /// <summary>
    /// Включает страницу как запасной маршрут: всё описанное раньше отвечает
    /// своим, а неописанное приводит сюда.
    /// </summary>
    public static IEndpointConventionBuilder MapWTelegramStandalonePage(this WebApplication app) =>
        app.MapFallback(
            (HttpContext context) =>
            {
                // Тип содержимого задаётся явно: запись строки сама по себе его не
                // ставит, и браузер показал бы страницу как текст или предложил
                // скачать файл вместо отрисовки формы.
                context.Response.ContentType = "text/html; charset=utf-8";

                // Страница всегда свежая: она менялась вместе с контрактом API, и
                // кэш браузера показывал бы старую форму с мёртвыми кнопками.
                context.Response.Headers.CacheControl = "no-store";

                return context.Response.WriteAsync(Html);
            }
        );

    /// <summary>Разметка страницы целиком: стили и скрипт встроены.</summary>
    public static string Html { get; } = Build();

    private static string Build() =>
        $$"""
            <!doctype html>
            <html lang="ru">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>MARS.Telegram — консоль службы</title>
            <style>
              :root { color-scheme: light dark; }
              body {
                margin: 0; padding: 2rem 1rem; font: 16px/1.5 system-ui, sans-serif;
                display: flex; justify-content: center;
              }
              main { width: 100%; max-width: 64rem; }
              h1 { font-size: 1.4rem; margin: 0 0 1rem; }
              h2 { font-size: 1.05rem; margin: 0 0 .6rem; }
              .card {
                border: 1px solid rgba(128, 128, 128, .4); border-radius: .6rem;
                padding: 1rem 1.2rem; margin-bottom: 1rem;
              }
              .row { display: flex; align-items: center; gap: .6rem; flex-wrap: wrap; }
              .row + .row { margin-top: .8rem; }
              label { min-width: 9rem; }
              input, textarea {
                flex: 1 1 12rem; padding: .45rem .6rem; font: inherit;
                border: 1px solid rgba(128, 128, 128, .5); border-radius: .35rem;
                background: transparent; color: inherit;
              }
              textarea { min-height: 5rem; resize: vertical; }
              button {
                padding: .45rem 1rem; font: inherit; cursor: pointer; border-radius: .35rem;
                border: 1px solid rgba(128, 128, 128, .5); background: transparent;
                color: inherit;
              }
              button:disabled { opacity: .5; cursor: default; }
              .note { margin: .6rem 0 0; font-size: .85rem; opacity: .75; }
              .error { margin: 0 0 1rem; padding: .6rem .8rem; border-radius: .35rem;
                       border: 1px solid #b3261e; color: #b3261e; }
              .ok { margin: 0 0 1rem; padding: .6rem .8rem; border-radius: .35rem;
                    border: 1px solid #1e7e34; color: #1e7e34; }
              code { font-family: ui-monospace, monospace; }
              .status-ok { color: #1e7e34; font-weight: 600; }
              .status-bad { color: #b3261e; font-weight: 600; }
              .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
              @media (max-width: 40rem) { .grid { grid-template-columns: 1fr; } }
              .inbox { max-height: 28rem; overflow: auto; }
              .inbox-item { padding: .5rem 0; border-bottom: 1px solid rgba(128,128,128,.25); }
              .inbox-item:last-child { border-bottom: none; }
              .inbox-meta { font-size: .8rem; opacity: .7; }
              .inbox-text { white-space: pre-wrap; word-break: break-word; }
              .badge { display: inline-block; font-size: .75rem; padding: .1rem .4rem;
                       border-radius: .3rem; border: 1px solid rgba(128,128,128,.4); }
            </style>
            </head>
            <body>
            <main>
              <h1>MARS.Telegram — консоль службы</h1>

              <div id="error" class="error" hidden></div>
              <div id="ok" class="ok" hidden></div>

              <div class="card">
                <h2>Сервис</h2>
                <div class="row">
                  <label>Состояние</label>
                  <span class="status-ok">запущен</span>
                </div>
                <div class="row">
                  <label>Адрес</label>
                  <span id="address"></span>
                </div>
                <div class="row">
                  <label>Время работы</label>
                  <span id="uptime">—</span>
                </div>
              </div>

              <div class="card">
                <h2>Подключение</h2>
                <div class="row">
                  <label for="status">Пользовательский аккаунт</label>
                  <span id="status">загружаю…</span>
                </div>
                <div class="row" id="details" hidden>
                  <label for="detailsText">Подробности</label>
                  <span id="detailsText"></span>
                </div>
                <div class="row">
                  <label>MTProto-бот</label>
                  <span id="botStatus">загружаю…</span>
                  <button id="botCheck" type="button">Проверить подключение</button>
                </div>
                <div class="row" id="botDetails" hidden>
                  <label>Подробности бота</label>
                  <span id="botDetailsText"></span>
                </div>
                <div class="row" id="codeCard" hidden>
                  <label for="code">Код из приложения Telegram</label>
                  <input id="code" autocomplete="one-time-code" inputmode="numeric"
                         placeholder="например, 54321">
                  <button id="send" disabled>Войти</button>
                </div>
                <p class="note">
                  Код приходит в приложение Telegram от аккаунта, указанного в
                  <code>Telegram__PhoneNumber</code>. Он одноразовый и живёт недолго —
                  вводите свежий. При включённой двухфакторной аутентификации пароль
                  уже подставлен из файла окружения.
                </p>
                <div class="row">
                  <label for="relogin">Действия</label>
                  <button id="relogin">Переавторизоваться</button>
                </div>
              </div>

              <div class="grid">
                <div class="card">
                  <h2>Отправка сообщения администраторам</h2>
                  <div class="row">
                    <textarea id="messageText" placeholder="Текст сообщения (разметка HTML поддерживается)"></textarea>
                  </div>
                  <div class="row">
                    <button id="sendMessage" type="button">Отправить</button>
                    <span id="sendResult" class="note"></span>
                  </div>
                  <p class="note">
                    Сообщение уходит всем из <code>Telegram__AdminIds</code>. Входящие
                    сообщения бот уже пересылает администраторам автоматически.
                  </p>
                </div>

                <div class="card">
                  <h2>Полученные сообщения</h2>
                  <div id="inbox" class="inbox">
                    <p class="note">Загружаю…</p>
                  </div>
                </div>
              </div>
            </main>

            <script>
              const statusApi = {{JsonSerializer.Serialize(StatusApiPath)}};
              const codeApi = {{JsonSerializer.Serialize(CodeApiPath)}};
              const reloginApi = {{JsonSerializer.Serialize(ReloginApiPath)}};
              const inboxApi = {{JsonSerializer.Serialize(InboxApiPath)}};
              const botStatusApi = {{JsonSerializer.Serialize(BotStatusApiPath)}};
              const botCheckApi = {{JsonSerializer.Serialize(BotCheckApiPath)}};
              const sendApi = {{JsonSerializer.Serialize(SendMessageApiPath)}};
              const pollMs = {{PollIntervalMs}};

              const statusNode = document.getElementById('status');
              const detailsRow = document.getElementById('details');
              const detailsText = document.getElementById('detailsText');
              const errorNode = document.getElementById('error');
              const okNode = document.getElementById('ok');
              const codeCard = document.getElementById('codeCard');
              const codeInput = document.getElementById('code');
              const sendButton = document.getElementById('send');
              const reloginButton = document.getElementById('relogin');
              const addressNode = document.getElementById('address');
              const uptimeNode = document.getElementById('uptime');
              const botStatusNode = document.getElementById('botStatus');
              const botDetailsRow = document.getElementById('botDetails');
              const botDetailsText = document.getElementById('botDetailsText');
              const botCheckButton = document.getElementById('botCheck');
              const messageText = document.getElementById('messageText');
              const sendMessageButton = document.getElementById('sendMessage');
              const sendResultNode = document.getElementById('sendResult');
              const inboxNode = document.getElementById('inbox');

              const startedAt = Date.now();
              document.getElementById('address').textContent = location.pathname;

              const showError = message => {
                errorNode.textContent = message;
                errorNode.hidden = !message;
                if (message) { okNode.hidden = true; }
              };

              const showOk = message => {
                okNode.textContent = message;
                okNode.hidden = !message;
                if (message) { errorNode.hidden = true; }
              };

              const post = async (path, body) => {
                const response = await fetch(path, body
                  ? { method: 'POST', headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify(body) }
                  : { method: 'POST' });
                const text = await response.text();
                if (!response.ok) {
                  let message = 'Ответ службы: ' + response.status;
                  try {
                    const payload = JSON.parse(text);
                    message = payload.message || message;
                    if (payload.errorDetails) { message += ' (' + payload.errorDetails + ')'; }
                  } catch { /* ответ без разбора JSON — показываем как есть */ }
                  throw new Error(message);
                }
                return text ? JSON.parse(text) : null;
              };

              const render = status => {
                if (!status) {
                  statusNode.textContent = 'служба не ответила';
                  return;
                }
                if (status.isAuthenticated) {
                  statusNode.textContent = 'вход выполнен'
                    + (status.username ? ' (' + status.username + ')' : '');
                  codeCard.hidden = true;
                  detailsRow.hidden = true;
                  return;
                }
                statusNode.textContent = status.isAwaitingCode
                  ? 'ждёт код подтверждения'
                  : 'не авторизован';
                codeCard.hidden = !status.isAwaitingCode;
                const details = status.errorMessage || '';
                detailsRow.hidden = !details;
                detailsText.textContent = details;
              };

              const renderBotStatus = status => {
                if (!status) {
                  botStatusNode.textContent = 'служба не ответила';
                  botStatusNode.className = 'status-bad';
                  return;
                }
                if (status.isConnected) {
                  botStatusNode.textContent = 'подключение успешно'
                    + (status.botUsername ? ' (' + status.botUsername + ')' : '');
                  botStatusNode.className = 'status-ok';
                  botDetailsRow.hidden = true;
                  return;
                }
                botStatusNode.textContent = 'не подключён';
                botStatusNode.className = 'status-bad';
                const details = status.errorMessage || '';
                botDetailsRow.hidden = !details;
                botDetailsText.textContent = details;
              };

              const refresh = async () => {
                try {
                  const response = await fetch(statusApi);
                  render(response.ok ? await response.json() : null);
                } catch {
                  render(null);
                }
                try {
                  const response = await fetch(botStatusApi);
                  renderBotStatus(response.ok ? await response.json() : null);
                } catch {
                  renderBotStatus(null);
                }
                try {
                  const response = await fetch(inboxApi);
                  if (response.ok) {
                    const items = await response.json();
                    renderInbox(items);
                  }
                } catch { /* список не критичен */ }
                uptimeNode.textContent = Math.floor((Date.now() - startedAt) / 1000) + ' с';
              };

              const renderInbox = items => {
                if (!Array.isArray(items) || items.length === 0) {
                  inboxNode.innerHTML = '<p class="note">Пока ничего не получено.</p>';
                  return;
                }
                inboxNode.innerHTML = items.map(item => {
                  const time = new Date(item.receivedAtUtc).toLocaleTimeString();
                  const badge = item.hasMedia ? '<span class="badge">медиа</span> ' : '';
                  const channel = item.isChannelPost ? ' [канал]' : '';
                  const text = (item.text || '').replace(/</g, '&lt;');
                  return '<div class="inbox-item">'
                    + '<div class="inbox-meta">' + time + ' · чат ' + item.chatId + channel + '</div>'
                    + '<div class="inbox-text">' + badge + text + '</div>'
                    + '</div>';
                }).join('');
              };

              const toggleSend = () => { sendButton.disabled = codeInput.value.trim() === ''; };
              codeInput.addEventListener('input', toggleSend);
              codeInput.addEventListener('keydown', event => {
                if (event.key === 'Enter') { sendButton.click(); }
              });

              sendButton.addEventListener('click', async () => {
                const code = codeInput.value.trim();
                if (!code) { return; }
                showError('');
                sendButton.disabled = true;
                reloginButton.disabled = true;
                try {
                  await post(codeApi, { code });
                  codeInput.value = '';
                  codeCard.hidden = true;
                  showOk('Код принят');
                } catch (error) {
                  showError(error.message);
                  codeInput.focus();
                } finally {
                  toggleSend();
                  sendButton.disabled = codeInput.value.trim() === '';
                  reloginButton.disabled = false;
                  await refresh();
                }
              });

              reloginButton.addEventListener('click', async () => {
                showError('');
                reloginButton.disabled = true;
                try {
                  await post(reloginApi);
                  showOk('Переавторизация выполнена');
                } catch (error) {
                  showError(error.message);
                } finally {
                  reloginButton.disabled = false;
                  await refresh();
                }
              });

              botCheckButton.addEventListener('click', async () => {
                botCheckButton.disabled = true;
                try {
                  const status = await post(botCheckApi);
                  renderBotStatus(status);
                  if (status && status.isConnected) { showOk('Подключение бота успешно'); }
                } catch (error) {
                  showError(error.message);
                } finally {
                  botCheckButton.disabled = false;
                }
              });

              sendMessageButton.addEventListener('click', async () => {
                const text = messageText.value.trim();
                if (!text) { return; }
                showError('');
                sendMessageButton.disabled = true;
                try {
                  const result = await post(sendApi, { text });
                  messageText.value = '';
                  const delivered = result && typeof result.result === 'number' ? result.result : null;
                  showOk(delivered !== null ? ('Доставлено ' + delivered + ' администраторам') : 'Сообщение отправлено');
                  await refresh();
                } catch (error) {
                  showError(error.message);
                } finally {
                  sendMessageButton.disabled = false;
                }
              });

              void refresh();
              setInterval(() => void refresh(), pollMs);
            </script>
            </body>
            </html>
            """;
}
