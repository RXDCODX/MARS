using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MARS.Telegram.Endpoints;

/// <summary>
/// Минимальная страница входа в учётную запись Telegram.
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

    private const int PollIntervalMs = 3000;

    /// <summary>
    /// Включает страницу как запасной маршрут: всё описанное раньше отвечает
    /// своим, а неописанное приводит сюда.
    /// </summary>
    public static IEndpointConventionBuilder MapWTelegramStandalonePage(this WebApplication app) =>
        app.MapFallback((HttpContext context) =>
        {
            // Тип содержимого задаётся явно: запись строки сама по себе его не
            // ставит, и браузер показал бы страницу как текст или предложил
            // скачать файл вместо отрисовки формы.
            context.Response.ContentType = "text/html; charset=utf-8";

            // Страница всегда свежая: она менялась вместе с контрактом API, и
            // кэш браузера показывал бы старую форму с мёртвыми кнопками.
            context.Response.Headers.CacheControl = "no-store";

            return context.Response.WriteAsync(Html);
        });

    /// <summary>Разметка страницы целиком: стили и скрипт встроены.</summary>
    public static string Html { get; } = Build();

    private static string Build() =>
        $$"""
        <!doctype html>
        <html lang="ru">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>MARS.Telegram — вход в аккаунт</title>
        <style>
          :root { color-scheme: light dark; }
          body {
            margin: 0; padding: 2rem 1rem; font: 16px/1.5 system-ui, sans-serif;
            display: flex; justify-content: center;
          }
          main { width: 100%; max-width: 34rem; }
          h1 { font-size: 1.4rem; margin: 0 0 1rem; }
          .card {
            border: 1px solid rgba(128, 128, 128, .4); border-radius: .6rem;
            padding: 1rem 1.2rem; margin-bottom: 1rem;
          }
          .row { display: flex; align-items: center; gap: .6rem; flex-wrap: wrap; }
          .row + .row { margin-top: .8rem; }
          label { min-width: 9rem; }
          input {
            flex: 1 1 12rem; padding: .45rem .6rem; font: inherit;
            border: 1px solid rgba(128, 128, 128, .5); border-radius: .35rem;
            background: transparent; color: inherit;
          }
          button {
            padding: .45rem 1rem; font: inherit; cursor: pointer; border-radius: .35rem;
            border: 1px solid rgba(128, 128, 128, .5); background: transparent;
            color: inherit;
          }
          button:disabled { opacity: .5; cursor: default; }
          .note { margin: .6rem 0 0; font-size: .85rem; opacity: .75; }
          .error { margin: 0 0 1rem; padding: .6rem .8rem; border-radius: .35rem;
                   border: 1px solid #b3261e; color: #b3261e; }
          code { font-family: ui-monospace, monospace; }
        </style>
        </head>
        <body>
        <main>
          <h1>MARS.Telegram — вход в учётную запись</h1>

          <div id="error" class="error" hidden></div>

          <div class="card">
            <div class="row">
              <label for="status">Состояние входа</label>
              <span id="status">загружаю…</span>
            </div>
            <div class="row" id="details" hidden>
              <label for="detailsText">Подробности</label>
              <span id="detailsText"></span>
            </div>
            <p class="note" id="address"></p>
          </div>

          <div class="card" id="codeCard" hidden>
            <div class="row">
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
          </div>

          <div class="card">
            <div class="row">
              <label for="relogin">Действия</label>
              <button id="relogin">Переавторизоваться</button>
            </div>
            <p class="note">
              После входа сессия сохраняется в базе, и страница больше не нужна:
              клиент входит сам.
            </p>
          </div>
        </main>

        <script>
          const statusApi = {{JsonSerializer.Serialize(StatusApiPath)}};
          const codeApi = {{JsonSerializer.Serialize(CodeApiPath)}};
          const reloginApi = {{JsonSerializer.Serialize(ReloginApiPath)}};
          const pollMs = {{PollIntervalMs}};

          const statusNode = document.getElementById('status');
          const detailsRow = document.getElementById('details');
          const detailsText = document.getElementById('detailsText');
          const errorNode = document.getElementById('error');
          const codeCard = document.getElementById('codeCard');
          const codeInput = document.getElementById('code');
          const sendButton = document.getElementById('send');
          const reloginButton = document.getElementById('relogin');

          document.getElementById('address').textContent =
            'Запрошенный адрес: ' + location.pathname
            + ' — страница отдаётся на любой неописанный адрес этой службы.';

          const showError = message => {
            errorNode.textContent = message;
            errorNode.hidden = !message;
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

          const refresh = async () => {
            try {
              const response = await fetch(statusApi);
              render(response.ok ? await response.json() : null);
            } catch {
              render(null);
            }
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
            } catch (error) {
              showError(error.message);
              // Форма остаётся на месте: код может быть неверным или просроченным,
              // и вводить надо следующий, а не уходить со страницы.
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
            } catch (error) {
              showError(error.message);
            } finally {
              reloginButton.disabled = false;
              await refresh();
            }
          });

          void refresh();
          setInterval(() => void refresh(), pollMs);
        </script>
        </body>
        </html>
        """;
}
