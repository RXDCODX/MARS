const apiRoot = '/api/MediaStorage';

const state = {
    currentDirectory: '',
    currentBrowse: null,
    expanded: new Map(),
    selected: null,
    searching: false,
    searchQuery: '',
    page: 1,
    pageSize: 50,
    treeReloadVersion: 0,
};

const elements = {
    tree: document.querySelector('#folder-tree'),
    list: document.querySelector('#file-list'),
    breadcrumbs: document.querySelector('#breadcrumbs'),
    title: document.querySelector('#directory-title'),
    directorySummary: document.querySelector('#directory-summary'),
    count: document.querySelector('#item-count'),
    status: document.querySelector('#status-line'),
    search: document.querySelector('#search-input'),
    clearSearch: document.querySelector('#clear-search'),
    git: document.querySelector('#git-summary'),
    connection: document.querySelector('#connection-state'),
    refresh: document.querySelector('#refresh-button'),
    treeRefresh: document.querySelector('#tree-refresh'),
    index: document.querySelector('#index-button'),
};

async function requestJson(url, options) {
    const response = await fetch(url, options);
    const body = await response.json().catch(() => null);
    if (!response.ok) {
        throw new Error(body?.errorMessage || body || `HTTP ${response.status}`);
    }
    if (body && Object.hasOwn(body, 'success')) {
        if (!body.success) throw new Error(body.errorMessage || 'Операция не выполнена');
        return body.result;
    }
    return body;
}

function formatBytes(bytes) {
    if (!Number.isFinite(bytes) || bytes < 0) return '—';
    if (bytes === 0) return '0 Б';
    const units = ['Б', 'КБ', 'МБ', 'ГБ', 'ТБ'];
    const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
    return `${(bytes / (1024 ** index)).toFixed(index ? 1 : 0)} ${units[index]}`;
}

function formatDate(value) {
    if (!value) return '—';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '—';
    return date.toLocaleString('ru-RU', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit',
    });
}

function setStatus(message, kind = '') {
    elements.status.textContent = message || '';
    elements.status.className = `status-line ${kind}`.trim();
}

function createElement(tag, className, text) {
    const element = document.createElement(tag);
    if (className) element.className = className;
    if (text !== undefined) element.textContent = text;
    return element;
}

function encodeQuery(value) {
    return encodeURIComponent(value || '');
}

async function browse(directory) {
    return requestJson(`${apiRoot}/browse?directory=${encodeQuery(directory)}`);
}

async function searchFiles(query, page) {
    const params = new URLSearchParams({
        q: query,
        page: String(page),
        pageSize: String(state.pageSize),
    });
    return requestJson(`${apiRoot}/search?${params}`);
}

async function refreshGit() {
    try {
        const status = await requestJson(`${apiRoot}/git/status`);
        elements.git.textContent = status.isRepositoryReady
            ? `Git: ${status.currentBranch || 'branch'} · изменений: ${status.pendingPaths.length}`
            : 'Git: репозиторий не подключён';
        if (status.error) elements.git.title = status.error;
    } catch (error) {
        elements.git.textContent = 'Git: статус недоступен';
        elements.git.title = error.message;
    }
}

function directorySegments(path) {
    return path ? path.split('/') : [];
}

function updateUrl(directory) {
    const url = new URL(window.location.href);
    if (directory) url.searchParams.set('directory', directory);
    else url.searchParams.delete('directory');
    url.searchParams.delete('q');
    url.searchParams.delete('page');
    window.history.pushState({ directory }, '', url);
}

function buildBreadcrumbs(directory) {
    elements.breadcrumbs.replaceChildren();
    const root = createElement('button', '', 'Главная');
    root.type = 'button';
    root.addEventListener('click', () => navigate(''));
    elements.breadcrumbs.append(root);

    let accumulated = '';
    for (const segment of directorySegments(directory)) {
        elements.breadcrumbs.append(createElement('span', 'breadcrumb-separator', '/'));
        accumulated = accumulated ? `${accumulated}/${segment}` : segment;
        const target = accumulated;
        const button = createElement('button', '', segment);
        button.type = 'button';
        button.addEventListener('click', () => navigate(target));
        elements.breadcrumbs.append(button);
    }
}

function createTreeNode(directory, depth = 0) {
    const wrapper = createElement('div', 'tree-entry');
    const row = createElement('div', 'tree-node');
    row.style.paddingLeft = `${8 + depth * 3}px`;
    const expanded = state.expanded.get(directory.relativePath);
    const toggle = createElement('button', 'tree-toggle', directory.hasChildren
        ? (expanded ? '▾' : '▸')
        : '');
    toggle.type = 'button';
    toggle.disabled = !directory.hasChildren;
    toggle.setAttribute('aria-label', expanded ? 'Свернуть каталог' : 'Раскрыть каталог');
    toggle.addEventListener('click', async (event) => {
        event.stopPropagation();
        await toggleDirectory(directory);
    });
    row.append(toggle);
    row.append(createElement('span', 'tree-label', `📁 ${directory.name}`));
    if (state.currentDirectory === directory.relativePath) row.setAttribute('aria-current', 'page');
    row.addEventListener('click', () => navigate(directory.relativePath));
    wrapper.append(row);

    if (expanded?.loaded && expanded.items.length) {
        const children = createElement('div', 'tree-children');
        for (const child of expanded.items) children.append(createTreeNode(child, depth + 1));
        wrapper.append(children);
    } else if (expanded?.loading) {
        wrapper.append(createElement('div', 'tree-loading', 'Загрузка…'));
    } else if (expanded?.error) {
        wrapper.append(createElement('div', 'tree-loading', expanded.error));
    }
    return wrapper;
}

function renderTree(rootDirectories) {
    elements.tree.replaceChildren();
    const rootRow = createElement('div', 'tree-node tree-root');
    rootRow.append(createElement('span', 'tree-toggle', '⌂'));
    rootRow.append(createElement('span', 'tree-label', 'wwwroot'));
    if (!state.currentDirectory) rootRow.setAttribute('aria-current', 'page');
    rootRow.addEventListener('click', () => navigate(''));
    elements.tree.append(rootRow);
    for (const directory of rootDirectories) elements.tree.append(createTreeNode(directory));
}

async function loadChildren(directory) {
    const current = state.expanded.get(directory.relativePath) || {};
    state.expanded.set(directory.relativePath, { ...current, loading: true, error: null });
    renderCurrentTree();
    try {
        const result = await browse(directory.relativePath);
        state.expanded.set(directory.relativePath, {
            loaded: true,
            loading: false,
            error: null,
            items: result.directories,
        });
    } catch (error) {
        state.expanded.set(directory.relativePath, {
            loaded: false,
            loading: false,
            error: error.message,
            items: [],
        });
    }
    renderCurrentTree();
}

async function toggleDirectory(directory) {
    const current = state.expanded.get(directory.relativePath);
    if (current?.loading) return;
    if (current?.loaded) {
        state.expanded.set(directory.relativePath, { ...current, collapsed: !current.collapsed });
        if (current.collapsed) state.expanded.set(directory.relativePath, { ...current, collapsed: false });
        else state.expanded.set(directory.relativePath, { ...current, collapsed: true });
        renderCurrentTree();
        return;
    }
    await loadChildren(directory);
}

function renderCurrentTree() {
    if (!state.rootDirectories) return;
    elements.tree.replaceChildren();
    const rootRow = createElement('div', 'tree-node tree-root', '⌂  wwwroot');
    if (!state.currentDirectory) rootRow.setAttribute('aria-current', 'page');
    rootRow.addEventListener('click', () => navigate(''));
    elements.tree.append(rootRow);
    for (const directory of state.rootDirectories) {
        if (state.expanded.get(directory.relativePath)?.collapsed) {
            const copy = { ...directory, hasChildren: false };
            elements.tree.append(createTreeNode(copy));
        } else {
            elements.tree.append(createTreeNode(directory));
        }
    }
}

function renderRows(items) {
    elements.list.replaceChildren();
    if (!items.length) {
        elements.list.append(createElement('div', 'empty-state', 'Каталог пуст.'));
        return;
    }

    const table = createElement('table', 'file-table');
    const thead = document.createElement('thead');
    const header = document.createElement('tr');
    for (const label of ['', 'Тип', 'Имя', 'Размер', 'Загружен', 'Последний просмотр']) {
        header.append(createElement('th', '', label));
    }
    thead.append(header);
    table.append(thead);
    const tbody = document.createElement('tbody');

    const dirs = items.directories || [];
    const files = items.files || [];
    for (const directory of dirs) {
        const row = document.createElement('tr');
        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.disabled = true;
        const cellSelect = document.createElement('td');
        cellSelect.append(checkbox);
        row.append(cellSelect);
        row.append(createElement('td', 'folder-icon', '📁'));
        const nameCell = createElement('td', 'file-name-cell');
        const button = createElement('button', 'file-name-button', directory.name);
        button.type = 'button';
        button.addEventListener('click', () => navigate(directory.relativePath));
        nameCell.append(button);
        row.append(nameCell);
        row.append(createElement('td', 'size-cell', '—'));
        row.append(createElement('td', 'date-cell', '—'));
        row.append(createElement('td', 'date-cell', '—'));
        tbody.append(row);
    }
    for (const file of files) {
        const row = document.createElement('tr');
        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.disabled = !file.isIndexed;
        const cellSelect = document.createElement('td');
        cellSelect.append(checkbox);
        row.append(cellSelect);
        row.append(createElement('td', 'file-icon', '📄'));
        const nameCell = createElement('td', 'file-name-cell');
        const button = createElement('button', 'file-name-button', file.name);
        button.type = 'button';
        button.addEventListener('click', () => {
            state.selected = file;
            setStatus(file.isIndexed ? 'Файл выбран.' : 'Файл ещё не индексирован: сначала выполните индексацию.', file.isIndexed ? 'success' : '');
        });
        nameCell.append(button);
        if (!file.isIndexed) nameCell.append(createElement('span', 'unindexed', 'Не индексирован'));
        row.append(nameCell);
        row.append(createElement('td', 'size-cell', formatBytes(file.sizeBytes)));
        row.append(createElement('td', 'date-cell', formatDate(file.uploadedAt || file.lastWriteTimeUtc)));
        row.append(createElement('td', 'date-cell', formatDate(file.lastDownloadedAt)));
        tbody.append(row);
    }
    table.append(tbody);
    elements.list.append(table);
}

function renderSearchResults(result) {
    elements.list.replaceChildren();
    const entries = result.entries || [];
    if (!entries.length) {
        elements.list.append(createElement('div', 'empty-state', 'Совпадений не найдено.'));
        return;
    }
    const table = createElement('table', 'file-table');
    const thead = document.createElement('thead');
    const header = document.createElement('tr');
    for (const label of ['', 'Тип', 'Имя', 'Размер', 'Загружен', 'Последний просмотр']) {
        header.append(createElement('th', '', label));
    }
    thead.append(header);
    table.append(thead);
    const tbody = document.createElement('tbody');
    for (const entry of entries) {
        const row = document.createElement('tr');
        row.append(createElement('td', '', ''));
        row.append(createElement('td', 'file-icon', '📄'));
        const nameCell = createElement('td', 'file-name-cell');
        const button = createElement('button', 'file-name-button', entry.fileName);
        button.type = 'button';
        button.addEventListener('click', () => navigate(entry.path.split('/').slice(0, -1).join('/')));
        nameCell.append(button);
        row.append(nameCell);
        row.append(createElement('td', 'size-cell', formatBytes(entry.sizeBytes)));
        row.append(createElement('td', 'date-cell', formatDate(entry.uploadedAt)));
        row.append(createElement('td', 'date-cell', formatDate(entry.lastDownloadedAt)));
        tbody.append(row);
    }
    table.append(tbody);
    elements.list.append(table);
    elements.directorySummary.textContent = `Найдено: ${result.total}`;
}

async function loadDirectory(directory, { updateHistory = true } = {}) {
    state.searching = false;
    state.searchQuery = '';
    state.currentDirectory = directory;
    state.selected = null;
    elements.search.value = '';
    elements.clearSearch.hidden = true;
    buildBreadcrumbs(directory);
    elements.title.textContent = directory ? directory.split('/').at(-1) : 'wwwroot';
    elements.list.replaceChildren(createElement('div', 'empty-state', 'Загрузка…'));
    try {
        const result = await browse(directory);
        state.currentBrowse = result;
        state.rootDirectories = directory ? state.rootDirectories : result.directories;
        if (!directory) renderTree(result.directories);
        else renderCurrentTree();
        renderRows(result);
        const count = result.directories.length + result.files.length;
        elements.directorySummary.textContent = `${result.directories.length} папок · ${result.files.length} файлов`;
        elements.count.textContent = `${count} элементов`;
        if (updateHistory) updateUrl(directory);
        setStatus('');
    } catch (error) {
        elements.list.replaceChildren(createElement('div', 'error-state', error.message));
        setStatus(error.message, 'error');
    }
}

async function navigate(directory) {
    const path = directory || '';
    await loadDirectory(path);
}

async function performSearch(query, page = 1) {
    const normalized = query.trim();
    if (!normalized) return loadDirectory(state.currentDirectory);
    state.searching = true;
    state.searchQuery = normalized;
    elements.clearSearch.hidden = false;
    elements.title.textContent = 'Поиск';
    elements.breadcrumbs.replaceChildren(createElement('span', '', 'Результаты глобального поиска'));
    elements.list.replaceChildren(createElement('div', 'empty-state', 'Поиск…'));
    try {
        const result = await searchFiles(normalized, page);
        renderSearchResults(result);
        elements.count.textContent = `${result.total} результатов`;
        elements.directorySummary.textContent = `Страница ${result.page} из ${Math.max(1, Math.ceil(result.total / result.pageSize))}`;
        const url = new URL(window.location.href);
        url.searchParams.set('q', normalized);
        url.searchParams.set('page', String(page));
        url.searchParams.delete('directory');
        window.history.pushState({ query: normalized, page }, '', url);
    } catch (error) {
        setStatus(error.message, 'error');
    }
}

async function refreshCurrent() {
    if (state.searching) return performSearch(state.searchQuery, state.page);
    await loadDirectory(state.currentDirectory, { updateHistory: false });
}

async function refreshTree() {
    state.expanded.clear();
    const root = await browse('');
    state.rootDirectories = root.directories;
    renderTree(root.directories);
    if (state.currentDirectory) {
        const segments = directorySegments(state.currentDirectory);
        let path = '';
        for (const segment of segments) {
            path = path ? `${path}/${segment}` : segment;
            const directory = { name: segment, relativePath: path, hasChildren: true };
            await loadChildren(directory);
        }
    }
}

async function runIndex() {
    elements.index.disabled = true;
    setStatus('Индексация файлов…');
    try {
        const result = await requestJson(`${apiRoot}/index`, { method: 'POST' });
        setStatus(`Индексация завершена: добавлено ${result.added}, обновлено ${result.updated}.`, 'success');
        await refreshTree();
        await refreshCurrent();
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        elements.index.disabled = false;
    }
}

elements.refresh.addEventListener('click', async () => {
    await refreshTree();
    await refreshCurrent();
});
elements.treeRefresh.addEventListener('click', () => void refreshTree());
elements.index.addEventListener('click', () => void runIndex());
elements.search.addEventListener('keydown', (event) => {
    if (event.key === 'Enter') void performSearch(elements.search.value);
    if (event.key === 'Escape') {
        elements.search.value = '';
        void loadDirectory(state.currentDirectory);
    }
});
elements.clearSearch.addEventListener('click', () => {
    elements.search.value = '';
    void loadDirectory(state.currentDirectory);
});
window.addEventListener('popstate', () => {
    const url = new URL(window.location.href);
    if (url.searchParams.has('q')) {
        elements.search.value = url.searchParams.get('q') || '';
        void performSearch(elements.search.value, Number(url.searchParams.get('page') || 1));
    } else {
        void loadDirectory(url.searchParams.get('directory') || '', { updateHistory: false });
    }
});

async function connectHub() {
    try {
        const signalR = await import('https://cdn.jsdelivr.net/npm/@microsoft/signalr@10.0.0/+esm');
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/media-storage')
            .withAutomaticReconnect()
            .build();
        const refresh = () => {
            void refreshTree();
            void refreshCurrent();
            void refreshGit();
        };
        connection.on('StorageChanged', refresh);
        connection.on('StorageStructureChanged', refresh);
        connection.onreconnected(refresh);
        connection.onclose(() => { elements.connection.textContent = 'SignalR: отключено'; });
        await connection.start();
        elements.connection.textContent = 'SignalR: подключено';
    } catch (error) {
        elements.connection.textContent = 'SignalR: недоступен (ручное обновление работает)';
    }
}

async function initialize() {
    const url = new URL(window.location.href);
    const directory = url.searchParams.get('directory') || '';
    if (url.searchParams.has('q')) {
        elements.search.value = url.searchParams.get('q') || '';
        await performSearch(elements.search.value, Number(url.searchParams.get('page') || 1));
    } else {
        await loadDirectory(directory, { updateHistory: false });
    }
    await refreshTree();
    await refreshGit();
    void connectHub();
}

initialize();
