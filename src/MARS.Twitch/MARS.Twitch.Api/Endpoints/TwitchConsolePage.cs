using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MARS.Twitch.Api.Endpoints;

/// <summary>
/// Страница-консоль администратора MARS.Twitch: статус, отправка и входящие.
/// </summary>
public static class TwitchConsolePage
{
    public const string StatusApiPath = "/api/TwitchAuth/status";

    public const string IdentityApiPath = "/api/TwitchConsole/identity";

    public const string InboxApiPath = "/api/TwitchConsole/inbox";

    public const string JoinApiPath = "/api/TwitchConsole/channel/join";

    public const string SendApiPath = "/api/TwitchConsole/chat/send";

    public const string ReconnectApiPath = "/api/TwitchAuth/reconnect";

    private const int PollIntervalMs = 3000;

    public static IEndpointConventionBuilder MapTwitchConsolePage(this WebApplication app) =>
        app.MapGet(
            "/",
            (HttpContext context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers.CacheControl = "no-store";
                return context.Response.WriteAsync(Html);
            }
        );

    public static string Html { get; } = Build();

    private static string Build() =>
        $$"""
            <!doctype html>
            <html lang="ru">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>MARS.Twitch — консоль службы</title>
            <style>
              :root { color-scheme: light dark; }
              body { margin: 0; padding: 2rem 1rem; font: 16px/1.5 system-ui, sans-serif; display: flex; justify-content: center; }
              main { width: 100%; max-width: 64rem; }
              h1 { font-size: 1.4rem; margin: 0 0 1rem; }
              h2 { font-size: 1.05rem; margin: 0 0 .6rem; }
              .card { border: 1px solid rgba(128,128,128,.4); border-radius: .6rem; padding: 1rem 1.2rem; margin-bottom: 1rem; }
              .row { display: flex; align-items: center; gap: .6rem; flex-wrap: wrap; }
              .row + .row { margin-top: .8rem; }
              label { min-width: 9rem; }
              input, textarea { flex: 1 1 12rem; padding: .45rem .6rem; font: inherit; border: 1px solid rgba(128,128,128,.5); border-radius: .35rem; background: transparent; color: inherit; }
              textarea { min-height: 5rem; resize: vertical; }
              button { padding: .45rem 1rem; font: inherit; cursor: pointer; border-radius: .35rem; border: 1px solid rgba(128,128,128,.5); background: transparent; color: inherit; }
              button:disabled { opacity: .5; cursor: default; }
              .note { margin: .6rem 0 0; font-size: .85rem; opacity: .75; }
              .error { margin: 0 0 1rem; padding: .6rem .8rem; border-radius: .35rem; border: 1px solid #b3261e; color: #b3261e; }
              .ok { margin: 0 0 1rem; padding: .6rem .8rem; border-radius: .35rem; border: 1px solid #1e7e34; color: #1e7e34; }
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
              .badge { display: inline-block; font-size: .75rem; padding: .1rem .4rem; border-radius: .3rem; border: 1px solid rgba(128,128,128,.4); }
            </style>
            </head>
            <body>
            <main>
              <h1>MARS.Twitch — консоль службы</h1>
              <div id="error" class="error" hidden></div>
              <div id="ok" class="ok" hidden></div>

              <div class="card">
                <h2>Сервис</h2>
                <div class="row"><label>Состояние</label><span class="status-ok">запущен</span></div>
                <div class="row"><label>Адрес</label><span id="address"></span></div>
                <div class="row"><label>Время работы</label><span id="uptime">—</span></div>
              </div>

              <div class="card">
                <h2>Под кем вошёл</h2>
                <div class="row"><label>Владелец токена</label><span id="identity">загружаю…</span></div>
                <div class="row"><label>IRC-логин бота</label><span id="botName">—</span></div>
                <button id="refreshIdentity" type="button">Обновить</button>
              </div>

              <div class="card">
                <h2>Подключение</h2>
                <div class="row"><label>Чат Twitch</label><span id="chatStatus">загружаю…</span></div>
                <div class="row"><label>EventSub</label><span id="eventSubStatus">—</span></div>
                <div class="row"><label>Каналы</label><span id="joinedChannels">—</span></div>
                <div class="row"><label>Последний дисконнект</label><span id="disconnect">—</span></div>
                <div class="row">
                  <button id="refreshStatus" type="button">Обновить статус</button>
                  <button id="reconnect" type="button">Переподключиться</button>
                </div>
              </div>

              <div class="grid">
                <div class="card">
                  <h2>Отправка в чат</h2>
                  <div class="row"><label for="channel">Канал</label><input id="channel" placeholder="rxdcodx"></div>
                  <div class="row"><button id="joinChannel" type="button">Подключиться к каналу</button></div>
                  <div class="row"><textarea id="messageText" placeholder="Текст сообщения"></textarea></div>
                  <div class="row">
                    <button id="sendMessage" type="button">Отправить</button>
                    <span id="sendResult" class="note"></span>
                  </div>
                  <p class="note">Сообщение уходит через текущее подключение Twitch. Длинный текст разбивается автоматически.</p>
                </div>
                <div class="card">
                  <h2>Полученные сообщения</h2>
                  <div class="row"><label for="filterChannel">Фильтр</label><input id="filterChannel" placeholder="оставьте пустым для всех"></div>
                  <div id="inbox" class="inbox"><p class="note">Загружаю…</p></div>
                </div>
              </div>
            </main>

            <script>
              const statusApi = {{JsonSerializer.Serialize(StatusApiPath)}};
              const identityApi = {{JsonSerializer.Serialize(IdentityApiPath)}};
              const inboxApi = {{JsonSerializer.Serialize(InboxApiPath)}};
              const joinApi = {{JsonSerializer.Serialize(JoinApiPath)}};
              const sendApi = {{JsonSerializer.Serialize(SendApiPath)}};
              const reconnectApi = {{JsonSerializer.Serialize(ReconnectApiPath)}};
              const pollMs = {{PollIntervalMs}};

              const errorNode = document.getElementById('error');
              const okNode = document.getElementById('ok');
              const addressNode = document.getElementById('address');
              const uptimeNode = document.getElementById('uptime');
              const identityNode = document.getElementById('identity');
              const botNameNode = document.getElementById('botName');
              const chatStatusNode = document.getElementById('chatStatus');
              const eventSubNode = document.getElementById('eventSubStatus');
              const joinedNode = document.getElementById('joinedChannels');
              const disconnectNode = document.getElementById('disconnect');
              const channelInput = document.getElementById('channel');
              const joinButton = document.getElementById('joinChannel');
              const messageText = document.getElementById('messageText');
              const sendButton = document.getElementById('sendMessage');
              const sendResultNode = document.getElementById('sendResult');
              const filterInput = document.getElementById('filterChannel');
              const inboxNode = document.getElementById('inbox');
              const refreshStatusButton = document.getElementById('refreshStatus');
              const refreshIdentityButton = document.getElementById('refreshIdentity');
              const reconnectButton = document.getElementById('reconnect');

              const startedAt = Date.now();
              addressNode.textContent = location.pathname;

              const showError = message => { errorNode.textContent = message; errorNode.hidden = !message; if (message) okNode.hidden = true; };
              const showOk = message => { okNode.textContent = message; okNode.hidden = !message; if (message) errorNode.hidden = true; };

              const post = async (path, body) => {
                const response = await fetch(path, body ? { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) } : { method: 'POST' });
                const text = await response.text();
                if (!response.ok) {
                  let message = 'Ответ службы: ' + response.status;
                  try { const payload = JSON.parse(text); message = payload.errorMessage || payload.message || message; } catch {}
                  throw new Error(message);
                }
                return text ? JSON.parse(text) : null;
              };

              const renderStatus = status => {
                if (!status) { chatStatusNode.textContent = 'служба не ответила'; chatStatusNode.className = 'status-bad'; return; }
                const result = status.result || status;
                chatStatusNode.textContent = result.isChatConnected ? 'подключение успешно' : 'не подключён';
                chatStatusNode.className = result.isChatConnected ? 'status-ok' : 'status-bad';
                eventSubNode.textContent = result.isEventSubConnected ? 'подключён' : 'не подключён';
                eventSubNode.className = result.isEventSubConnected ? 'status-ok' : 'status-bad';
                joinedNode.textContent = result.joinedChannels || '—';
                disconnectNode.textContent = result.lastDisconnectReason || (result.lastDisconnectedAt ? new Date(result.lastDisconnectedAt).toLocaleString() : '—');
              };

              const loadIdentity = async () => {
                try {
                  const response = await fetch(identityApi);
                  if (!response.ok) { identityNode.textContent = 'не удалось получить'; return; }
                  const payload = await response.json();
                  const result = payload.result || payload;
                  if (result && result.login) {
                    identityNode.textContent = result.displayName ? result.displayName + ' (' + result.login + ', ' + result.userId + ')' : result.login + ' (' + result.userId + ')';
                  } else {
                    identityNode.textContent = payload.errorMessage || 'токен недействителен';
                  }
                } catch { identityNode.textContent = 'служба не ответила'; }
              };

              const loadInbox = async () => {
                try {
                  const channel = filterInput.value.trim();
                  const url = channel ? inboxApi + '?channel=' + encodeURIComponent(channel) : inboxApi;
                  const response = await fetch(url);
                  if (!response.ok) return;
                  const items = await response.json();
                  renderInbox(Array.isArray(items) ? items : (items.result || []));
                } catch {}
              };

              const renderInbox = items => {
                if (!Array.isArray(items) || items.length === 0) { inboxNode.innerHTML = '<p class="note">Пока ничего не получено.</p>'; return; }
                inboxNode.innerHTML = items.map(item => {
                  const time = new Date(item.receivedAtUtc).toLocaleTimeString();
                  const badges = [item.isModerator ? 'mod' : '', item.isVip ? 'VIP' : '', item.isSubscriber ? 'sub' : '', item.isBroadcaster ? 'broadcaster' : ''].filter(Boolean).map(b => '<span class="badge">' + b + '</span>').join(' ');
                  const text = (item.message || '').replace(/</g, '&lt;');
                  return '<div class="inbox-item"><div class="inbox-meta">' + time + ' · ' + item.channel + ' · ' + (item.displayName || item.username) + '</div><div class="inbox-text">' + badges + ' ' + text + '</div></div>';
                }).join('');
              };

              const refreshStatus = async () => {
                try {
                  const response = await fetch(statusApi);
                  if (response.ok) renderStatus(await response.json());
                } catch { renderStatus(null); }
                await loadInbox();
                uptimeNode.textContent = Math.floor((Date.now() - startedAt) / 1000) + ' с';
              };

              refreshStatusButton.addEventListener('click', () => void refreshStatus());
              refreshIdentityButton.addEventListener('click', () => void loadIdentity());
              filterInput.addEventListener('input', () => void loadInbox());

              joinButton.addEventListener('click', async () => {
                const channel = channelInput.value.trim();
                if (!channel) return;
                joinButton.disabled = true;
                try {
                  const result = await post(joinApi, { channel });
                  showOk('Подключено к каналу ' + (result && result.result ? result.result : channel));
                } catch (error) { showError(error.message); }
                finally { joinButton.disabled = false; await refreshStatus(); }
              });

              sendButton.addEventListener('click', async () => {
                const text = messageText.value.trim();
                const channel = channelInput.value.trim();
                if (!text || !channel) return;
                sendButton.disabled = true;
                try {
                  const result = await post(sendApi, { channel, text });
                  messageText.value = '';
                  showOk('Сообщение отправлено в ' + (result && result.result ? result.result : channel));
                  await loadInbox();
                } catch (error) { showError(error.message); }
                finally { sendButton.disabled = false; }
              });

              reconnectButton.addEventListener('click', async () => {
                reconnectButton.disabled = true;
                try {
                  await post(reconnectApi);
                  showOk('Переподключение запущено');
                } catch (error) { showError(error.message); }
                finally { reconnectButton.disabled = false; await refreshStatus(); }
              });

              void loadIdentity();
              void refreshStatus();
              setInterval(() => void refreshStatus(), pollMs);
            </script>
            </body>
            </html>
            """;
}
