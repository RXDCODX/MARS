const apiRoot = '/api/MediaStorage';

const state = {
    currentDirectory: '',
    currentBrowse: null,
    expanded: new Map(),
    selected: null,
    selectedEntries: new Map(),
    searching: false,
    searchQuery: '',
    page: 1,
    pageSize: 50,
    treeReloadVersion: 0,
    showDeleted: false,
    dryRun: true,
    pendingFiles: [],
    uploadTarget: 'Uploads',
    busy: false,
    lastView: 'browser',
    browseCache: new Map(),
    sortKey: 'name',
    sortDirection: 'asc',
    columnWidths: {},
    currentSearchResult: null,
    previewRequestId: 0,
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
    previewContent: document.querySelector('#preview-content'),
    previewDetails: document.querySelector('#preview-details'),
    previewClose: document.querySelector('#preview-close'),
    uploadButton: document.querySelector('#upload-button'),
    fileInput: document.querySelector('#file-input'),
    replaceFileInput: document.querySelector('#replace-file-input'),
    trashToggle: document.querySelector('#trash-toggle'),
    bulkToolbar: document.querySelector('#bulk-toolbar'),
    operationResult: document.querySelector('#operation-result'),
    dialogRoot: document.querySelector('#dialog-root'),
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

const BROWSE_CACHE_TTL_MS = 20_000;
const COLUMN_WIDTH_STORAGE_KEY = 'mars.mediaStorage.columnWidths.v1';

async function browse(directory, { force = false } = {}) {
    const key = directory || '';
    const cached = state.browseCache.get(key);
    const now = Date.now();

    if (!force && cached?.value && now - cached.cachedAt < BROWSE_CACHE_TTL_MS) {
        return cached.value;
    }
    if (!force && cached?.promise) {
        return cached.promise;
    }

    const promise = requestJson(`${apiRoot}/browse?directory=${encodeQuery(key)}`)
        .then(value => {
            state.browseCache.set(key, { value, cachedAt: Date.now(), promise: null });
            return value;
        })
        .catch(error => {
            if (cached?.value) {
                state.browseCache.set(key, cached);
                return cached.value;
            }
            state.browseCache.delete(key);
            throw error;
        });

    state.browseCache.set(key, {
        value: cached?.value || null,
        cachedAt: cached?.cachedAt || 0,
        promise,
    });
    return promise;
}

function invalidateBrowseCache() {
    state.browseCache.clear();
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
        state.gitStatus = status;
        elements.git.textContent = status.isRepositoryReady
            ? `Git: ${status.currentBranch || 'branch'} · изменений: ${status.pendingPaths.length}`
            : 'Git: репозиторий не подключён';
        if (status.error) elements.git.title = status.error;
    } catch (error) {
        elements.git.textContent = 'Git: статус недоступен';
        elements.git.title = error.message;
        state.gitStatus = null;
    }
    renderBulkToolbar();
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
    const isExpanded = expanded?.loaded && !expanded.collapsed;
    const toggle = createElement('button', 'tree-toggle', directory.hasChildren
        ? (isExpanded ? '▾' : '▸')
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

    if (isExpanded && expanded.items.length) {
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
        state.expanded.set(directory.relativePath, {
            ...current,
            collapsed: !current.collapsed,
        });
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
        elements.tree.append(createTreeNode(directory));
    }
}


function previewUrl(path) {
    return `${apiRoot}/preview?path=${encodeURIComponent(path)}&v=${Date.now()}`;
}

function downloadUrl(path) {
    return `${apiRoot}/file?path=${encodeURIComponent(path)}`;
}

function clearPreview() {
    state.previewRequestId++;
    state.selected = null;
    elements.previewContent.replaceChildren(
        createElement('div', 'empty-state', 'Выберите файл в списке.')
    );
    elements.previewDetails.replaceChildren();
}

async function openPreview(file) {
    const requestId = ++state.previewRequestId;
    state.selected = file;
    elements.previewContent.replaceChildren(
        createElement('div', 'empty-state', 'Загрузка предпросмотра…')
    );
    elements.previewDetails.replaceChildren();

    const extension = getExtension(file.name);
    const source = previewUrl(file.relativePath);
    const content = elements.previewContent;
    const details = elements.previewDetails;

    try {
        if (['.png', '.jpg', '.jpeg', '.gif', '.webp', '.avif'].includes(extension)) {
            const image = createElement('img', 'preview-image');
            image.alt = file.name;
            image.loading = 'lazy';
            image.src = source;
            image.addEventListener('error', () => {
                if (requestId === state.previewRequestId) {
                    content.replaceChildren(createElement('div', 'error-state', 'Не удалось загрузить изображение.'));
                }
            }, { once: true });
            content.replaceChildren(image);
        } else if (['.mp4', '.webm', '.ogv', '.mov', '.avi', '.wmv'].includes(extension)) {
            const video = createElement('video', 'preview-video');
            video.controls = true;
            video.preload = 'metadata';
            video.playsInline = true;
            video.src = source;
            content.replaceChildren(video);
        } else if (['.mp3', '.wav', '.ogg', '.opus', '.flac'].includes(extension)) {
            const audio = createElement('audio', 'preview-audio');
            audio.controls = true;
            audio.preload = 'metadata';
            audio.src = source;
            content.replaceChildren(audio);
        } else if (extension === '.pdf') {
            const frame = createElement('iframe', 'preview-pdf');
            frame.title = `Предпросмотр ${file.name}`;
            frame.src = source;
            content.replaceChildren(frame);
        } else if (['.txt', '.log', '.json', '.xml', '.csv', '.md', '.markdown', '.yaml', '.yml'].includes(extension)) {
            const result = await requestJson(
                `${apiRoot}/text-preview?path=${encodeURIComponent(file.relativePath)}`
            );
            if (requestId !== state.previewRequestId) return;
            const pre = createElement('pre', 'preview-text');
            pre.textContent = result.content;
            content.replaceChildren(pre);
            if (result.truncated) {
                content.append(createElement('p', 'preview-warning', 'Показано начало файла: содержимое ограничено 512 КиБ.'));
            }
        } else {
            content.replaceChildren(
                createElement('div', 'preview-message',
                    'Для этого формата встроенный предпросмотр отключён. Файл можно скачать.')
            );
        }

        if (requestId !== state.previewRequestId) return;

        details.append(createElement('span', 'label', 'Имя'));
        details.append(createElement('span', 'value', file.name));
        details.append(createElement('span', 'label', 'Расширение'));
        details.append(createElement('span', 'value', extension || '—'));
        details.append(createElement('span', 'label', 'Размер'));
        details.append(createElement('span', 'value', formatBytes(file.sizeBytes)));
        details.append(createElement('span', 'label', 'Изменён'));
        details.append(createElement('span', 'value', formatDate(file.lastWriteTimeUtc)));
        details.append(createElement('span', 'label', 'Индекс'));
        details.append(createElement('span', 'value', file.isIndexed ? 'Проиндексирован' : 'Не индексирован'));

        const actions = createElement('div', 'preview-actions');
        const download = createElement('a', 'button button-primary', 'Скачать');
        download.href = downloadUrl(file.relativePath);
        download.download = file.name;
        actions.append(download);

        const replace = createElement('button', 'button button-quiet', 'Заменить');
        replace.type = 'button';
        replace.addEventListener('click', () => {
            elements.replaceFileInput.value = '';
            elements.replaceFileInput.click();
        });
        actions.append(replace);

        const remove = createElement('button', 'button button-danger', 'Удалить');
        remove.type = 'button';
        remove.addEventListener('click', () => void confirmDeleteSelected());
        actions.append(remove);

        details.append(actions);
        setStatus(file.isIndexed ? 'Файл выбран.' : 'Файл ещё не индексирован; доступен просмотр, скачивание и удаление.', file.isIndexed ? 'success' : '');
    } catch (error) {
        if (requestId !== state.previewRequestId) return;
        content.replaceChildren(createElement('div', 'error-state', error.message));
        setStatus(error.message, 'error');
    }
}

function getExtension(fileName) {
    const name = String(fileName || '');
    const dot = name.lastIndexOf('.');
    return dot > 0 ? name.slice(dot).toLowerCase() : '';
}

function selectedIds() {
    return [...state.selectedEntries.keys()];
}

function clearSelection() {
    state.selectedEntries.clear();
    document.querySelectorAll('input.bulk-checkbox').forEach(checkbox => {
        checkbox.checked = false;
    });
    renderBulkToolbar();
}

function selectEntry(entry) {
    if (!entry?.id) return;
    state.selectedEntries.set(entry.id, entry);
    renderBulkToolbar();
}

function toggleSelection(entry, checked) {
    if (!entry?.id) return;
    if (checked) state.selectedEntries.set(entry.id, entry);
    else state.selectedEntries.delete(entry.id);
    renderBulkToolbar();
}

function createSelectionCheckbox(entry, disabled = false) {
    const checkbox = createElement('input', 'bulk-checkbox');
    checkbox.type = 'checkbox';
    checkbox.checked = Boolean(entry.id && state.selectedEntries.has(entry.id));
    checkbox.dataset.entryId = entry.id || '';
    checkbox.disabled = disabled || !entry.id;
    checkbox.setAttribute('aria-label', `Выбрать ${entry.fileName || entry.name || 'файл'}`);
    checkbox.addEventListener('change', () => toggleSelection(entry, checkbox.checked));
    return checkbox;
}

function renderBulkToolbar() {
    const toolbar = elements.bulkToolbar;
    toolbar.replaceChildren();
    const count = state.selectedEntries.size;
    toolbar.hidden = count === 0;
    if (!count) return;

    toolbar.append(createElement('span', 'bulk-count', `Выбрано: ${count}`));

    const dryRunLabel = createElement('label', 'dry-run-label');
    const dryRun = createElement('input');
    dryRun.type = 'checkbox';
    dryRun.checked = state.dryRun;
    dryRun.addEventListener('change', () => {
        state.dryRun = dryRun.checked;
        renderBulkToolbar();
    });
    dryRunLabel.append(dryRun);
    dryRunLabel.append(document.createTextNode('Пробный прогон (dry-run)'));
    toolbar.append(dryRunLabel);

    if (state.showDeleted) {
        const restore = createElement('button', 'button button-primary', 'Восстановить');
        restore.type = 'button';
        restore.disabled = state.busy;
        restore.addEventListener('click', () => void runRestore());
        toolbar.append(restore);
    } else {
        const sync = createElement('button', 'button button-quiet', 'Синхронизировать в Git');
        sync.type = 'button';
        sync.disabled = state.busy || !state.gitStatus?.isRepositoryReady;
        sync.addEventListener('click', () => void runGitSync());
        toolbar.append(sync);

        const move = createElement('button', 'button button-quiet', 'Перенести');
        move.type = 'button';
        move.disabled = state.busy;
        move.addEventListener('click', () => void chooseMoveTarget());
        toolbar.append(move);

        const remove = createElement('button', 'button', 'Удалить в корзину');
        remove.type = 'button';
        remove.disabled = state.busy;
        remove.addEventListener('click', () => void confirmDelete());
        toolbar.append(remove);
    }

    const clear = createElement('button', 'button button-quiet', 'Снять выбор');
    clear.type = 'button';
    clear.disabled = state.busy;
    clear.addEventListener('click', clearSelection);
    toolbar.append(clear);
}

function showOutcome(title, result) {
    const container = elements.operationResult;
    container.replaceChildren();
    container.hidden = false;

    const heading = createElement('div', 'operation-result-title');
    heading.append(createElement(
        'strong',
        '',
        `${title}: успешно ${result.succeeded} из ${result.requested}, ошибок/пропусков ${result.failed}`
    ));
    const close = createElement('button', 'button button-quiet', 'Скрыть');
    close.type = 'button';
    close.addEventListener('click', () => {
        container.hidden = true;
        container.replaceChildren();
    });
    heading.append(close);
    container.append(heading);

    if (Array.isArray(result.errors) && result.errors.length) {
        const list = createElement('ul', 'operation-result-errors');
        for (const message of result.errors) list.append(createElement('li', '', message));
        container.append(list);
    }
}

async function postJson(endpoint, payload) {
    return requestJson(`${apiRoot}/${endpoint}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
    });
}

async function confirmDeleteSelected() {
    const file = state.selected;
    if (!file) return;
    const dialog = createDialog(
        `Удалить «${file.name}»?`,
        'Файл будет перемещён в корзину. Его можно будет восстановить до автоматической очистки.'
    );
    addDialogActions(dialog, [
        { label: 'Отмена', onClick: closeDialog },
        {
            label: 'Удалить в корзину',
            className: 'button button-danger',
            onClick: () => void deleteSelectedFile(),
        },
    ]);
}

async function deleteSelectedFile() {
    const file = state.selected;
    if (!file) return;
    state.busy = true;
    try {
        await requestJson(`${apiRoot}/file?path=${encodeQuery(file.relativePath)}`, {
            method: 'DELETE',
        });
        closeDialog();
        clearPreview();
        invalidateBrowseCache();
        clearSelection();
        await refreshTree();
        await refreshCurrent();
        await refreshGit();
        setStatus(`Файл «${file.name}» перемещён в корзину.`, 'success');
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        state.busy = false;
    }
}

async function replaceSelectedFile(file) {
    const selected = state.selected;
    if (!selected || !file) return;
    if (file.size === 0) {
        setStatus('Нельзя заменить файл пустым файлом.', 'error');
        return;
    }

    const form = new FormData();
    form.append('path', selected.relativePath);
    form.append('file', file, file.name);
    state.busy = true;
    setStatus(`Замена «${selected.name}»…`);

    try {
        const result = await requestJson(`${apiRoot}/replace`, { method: 'POST', body: form });
        showOutcome('Замена файла', result);
        invalidateBrowseCache();
        await refreshTree();
        await loadDirectory(state.currentDirectory, { updateHistory: false, force: true });
        const refreshed = state.currentBrowse?.files?.find(item => item.relativePath === selected.relativePath);
        if (refreshed) await openPreview(refreshed);
        await refreshGit();
        setStatus(`Файл «${selected.name}» заменён.`, 'success');
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        state.busy = false;
    }
}

function closeDialog() {
    elements.dialogRoot.replaceChildren();
}

function createDialog(title, description) {
    const backdrop = createElement('div', 'dialog-backdrop');
    backdrop.addEventListener('click', event => {
        if (event.target === backdrop && !state.busy) closeDialog();
    });
    const dialog = createElement('section', 'dialog');
    dialog.setAttribute('role', 'dialog');
    dialog.setAttribute('aria-modal', 'true');
    dialog.append(createElement('h2', '', title));
    if (description) dialog.append(createElement('p', 'dialog-description', description));
    backdrop.append(dialog);
    elements.dialogRoot.replaceChildren(backdrop);
    return dialog;
}

function addDialogActions(dialog, actions) {
    const row = createElement('div', 'dialog-actions');
    for (const action of actions) {
        const button = createElement('button', action.className || 'button button-quiet', action.label);
        button.type = 'button';
        button.disabled = Boolean(action.disabled);
        button.addEventListener('click', action.onClick);
        row.append(button);
    }
    dialog.append(row);
}

async function openDirectoryPicker({ title, initialPath = '', allowRoot = false, onSelect }) {
    const dialog = createDialog(title, 'Выберите каталог назначения. Внутренние каталоги хранилища здесь не отображаются.');
    const tree = createElement('div', 'dialog-tree');
    const expanded = new Map();
    const selectedLabel = createElement('p', 'dialog-description', initialPath || 'wwwroot');
    let selectedPath = initialPath;

    const finish = () => {
        if (!selectedPath && !allowRoot) return;
        closeDialog();
        onSelect(selectedPath);
    };

    async function loadChildren(path) {
        const item = expanded.get(path) || {};
        expanded.set(path, { ...item, loading: true, error: null });
        await render();
        try {
            const result = await browse(path);
            expanded.set(path, {
                loading: false,
                loaded: true,
                error: null,
                directories: result.directories,
            });
        } catch (error) {
            expanded.set(path, { loading: false, loaded: false, error: error.message, directories: [] });
        }
        await render();
    }

    async function toggle(path) {
        const item = expanded.get(path);
        if (item?.loading) return;
        if (item?.loaded) {
            expanded.set(path, { ...item, collapsed: !item.collapsed });
            await render();
        } else {
            await loadChildren(path);
        }
    }

    async function renderNode(parent, directory, depth) {
        const row = createElement('div', 'dialog-tree-node');
        row.style.paddingLeft = `${depth * 15}px`;
        const item = expanded.get(directory.relativePath);
        const caret = createElement('button', 'tree-toggle', directory.hasChildren ? (item?.loaded && !item.collapsed ? '▾' : '▸') : '');
        caret.type = 'button';
        caret.disabled = !directory.hasChildren;
        caret.addEventListener('click', () => void toggle(directory.relativePath));
        row.append(caret);
        row.append(createElement('span', 'tree-label', `📁 ${directory.name}`));
        const choose = createElement('button', 'button button-quiet', 'Выбрать');
        choose.type = 'button';
        choose.addEventListener('click', () => {
            selectedPath = directory.relativePath;
            selectedLabel.textContent = selectedPath;
            finish();
        });
        row.append(choose);
        parent.append(row);

        if (item?.loading) {
            parent.append(createElement('div', 'dialog-tree-loading', 'Загрузка…'));
        } else if (item?.error) {
            parent.append(createElement('div', 'dialog-tree-loading', item.error));
        } else if (item?.loaded && !item.collapsed) {
            for (const child of item.directories) await renderNode(parent, child, depth + 1);
        }
    }

    async function render() {
        tree.replaceChildren();
        const rootRow = createElement('div', 'dialog-tree-node');
        const rootItem = expanded.get('');
        const rootCaret = createElement('button', 'tree-toggle', rootItem?.loaded && !rootItem.collapsed ? '▾' : '▸');
        rootCaret.type = 'button';
        rootCaret.addEventListener('click', () => void toggle(''));
        rootRow.append(rootCaret);
        rootRow.append(createElement('span', 'tree-label', '🏠 wwwroot'));
        if (allowRoot) {
            const rootChoose = createElement('button', 'button button-quiet', 'Выбрать');
            rootChoose.type = 'button';
            rootChoose.addEventListener('click', () => {
                selectedPath = '';
                selectedLabel.textContent = 'wwwroot';
                finish();
            });
            rootRow.append(rootChoose);
        } else {
            rootRow.append(createElement('span', 'muted', 'Корень недоступен'));
        }
        tree.append(rootRow);
        if (rootItem?.loading) {
            tree.append(createElement('div', 'dialog-tree-loading', 'Загрузка…'));
        } else if (rootItem?.error) {
            tree.append(createElement('div', 'dialog-tree-loading', rootItem.error));
        } else if (rootItem?.loaded && !rootItem.collapsed) {
            for (const directory of rootItem.directories) await renderNode(tree, directory, 0);
        }
    }

    dialog.append(selectedLabel);
    dialog.append(tree);
    addDialogActions(dialog, [
        { label: 'Отмена', onClick: closeDialog },
        { label: 'Выбрать текущий каталог', className: 'button button-primary', disabled: !allowRoot && !selectedPath, onClick: finish },
    ]);
    await loadChildren('');
    if (initialPath) {
        let current = '';
        for (const segment of directorySegments(initialPath)) {
            current = current ? `${current}/${segment}` : segment;
            const directory = { name: segment, relativePath: current, hasChildren: true };
            await loadChildren(current);
        }
    }
}

async function runGitSync() {
    const ids = selectedIds();
    if (!ids.length || state.showDeleted) return;
    state.busy = true;
    renderBulkToolbar();
    try {
        const result = await postJson('sync', { ids });
        setStatus(result.committed
            ? `Git-коммит создан для ${result.filesSelected} файлов.`
            : `Среди ${result.filesSelected} выбранных файлов нет изменений для коммита.`,
        'success');
        clearSelection();
        await refreshGit();
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        state.busy = false;
        renderBulkToolbar();
    }
}

async function runRestore() {
    const ids = selectedIds();
    if (!ids.length || !state.showDeleted) return;
    state.busy = true;
    renderBulkToolbar();
    try {
        const restored = await postJson('restore', { ids, dryRun: state.dryRun });
        showOutcome(state.dryRun ? 'Пробное восстановление' : 'Восстановление', {
            requested: ids.length,
            succeeded: restored,
            failed: ids.length - restored,
            errors: [],
        });
        if (!state.dryRun) {
            clearSelection();
            invalidateBrowseCache();
            await loadTrash();
            await refreshTree();
            await refreshGit();
        }
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        state.busy = false;
        renderBulkToolbar();
    }
}

async function confirmDelete() {
    const ids = selectedIds();
    if (!ids.length || state.showDeleted) return;
    const dialog = createDialog(
        `Удалить в корзину ${ids.length} файлов?`,
        state.dryRun
            ? 'Включён пробный прогон: файлы не будут перемещены.'
            : 'Файлы переместятся в корзину. Их можно будет восстановить до автоматической очистки.'
    );
    addDialogActions(dialog, [
        { label: 'Отмена', onClick: closeDialog },
        {
            label: state.dryRun ? 'Проверить' : 'Удалить в корзину',
            className: 'button button-primary',
            onClick: () => void runDelete(),
        },
    ]);
}

async function runDelete() {
    const ids = selectedIds();
    if (!ids.length) return;
    state.busy = true;
    renderBulkToolbar();
    try {
        const result = await postJson('delete', { ids, dryRun: state.dryRun });
        showOutcome(state.dryRun ? 'Пробное удаление' : 'Удаление', result);
        closeDialog();
        if (!state.dryRun) {
            clearSelection();
            invalidateBrowseCache();
            await refreshTree();
            await refreshCurrent();
            await refreshGit();
        }
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        state.busy = false;
        renderBulkToolbar();
    }
}

async function chooseMoveTarget() {
    if (!selectedIds().length || state.showDeleted) return;
    await openDirectoryPicker({
        title: `Перенос ${selectedIds().length} файлов`,
        initialPath: state.currentDirectory,
        allowRoot: false,
        onSelect: async targetDirectory => {
            await runMove(targetDirectory);
        },
    });
}

async function runMove(targetDirectory) {
    const ids = selectedIds();
    if (!ids.length) return;
    state.busy = true;
    renderBulkToolbar();
    try {
        const result = await postJson('move', { ids, targetDirectory, dryRun: state.dryRun });
        showOutcome(state.dryRun ? 'Пробный перенос' : 'Перенос', result);
        if (!state.dryRun) {
            clearSelection();
            invalidateBrowseCache();
            await refreshTree();
            await refreshCurrent();
            await refreshGit();
        }
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        state.busy = false;
        renderBulkToolbar();
    }
}

function showUploadDialog() {
    const dialog = createDialog('Загрузка файлов', 'Добавьте несколько файлов и выберите каталог назначения.');
    const target = createElement('div', 'dialog-field');
    target.append(createElement('span', '', 'Каталог назначения'));
    const targetValue = createElement('strong', '', state.uploadTarget);
    target.append(targetValue);
    const chooseTarget = createElement('button', 'button button-quiet', 'Выбрать каталог…');
    chooseTarget.type = 'button';
    chooseTarget.addEventListener('click', () => {
        void openDirectoryPicker({
            title: 'Каталог для загрузки',
            initialPath: state.uploadTarget,
            allowRoot: false,
            onSelect: path => showUploadDialogWithTarget(path),
        });
    });
    target.append(chooseTarget);
    dialog.append(target);

    const filePicker = createElement('button', 'button button-quiet', 'Добавить файлы…');
    filePicker.type = 'button';
    filePicker.addEventListener('click', () => elements.fileInput.click());
    dialog.append(filePicker);

    const fileList = createElement('ul', 'dialog-files');
    if (!state.pendingFiles.length) {
        fileList.append(createElement('li', '', 'Файлы не выбраны.'));
    } else {
        for (const file of state.pendingFiles) {
            const item = createElement('li');
            item.append(createElement('span', 'file-name', file.name));
            item.append(createElement('span', 'muted', formatBytes(file.size)));
            const remove = createElement('button', 'icon-button', '×');
            remove.type = 'button';
            remove.setAttribute('aria-label', `Убрать ${file.name}`);
            remove.addEventListener('click', () => {
                state.pendingFiles = state.pendingFiles.filter(candidate => candidate !== file);
                showUploadDialog();
            });
            item.append(remove);
            fileList.append(item);
        }
    }
    dialog.append(fileList);

    const progress = createElement('div', 'dialog-description', '');
    progress.id = 'upload-progress-label';
    const progressBar = createElement('div', 'dialog-progress');
    progressBar.append(createElement('span'));
    progressBar.id = 'upload-progress';
    dialog.append(progress);
    dialog.append(progressBar);

    addDialogActions(dialog, [
        { label: 'Отмена', onClick: closeDialog },
        { label: `Загрузить (${state.pendingFiles.length})`, className: 'button button-primary', disabled: !state.pendingFiles.length, onClick: () => void runUpload() },
    ]);
}

function showUploadDialogWithTarget(path) {
    state.uploadTarget = path;
    showUploadDialog();
}

function addPendingFiles(fileList) {
    if (!fileList || !fileList.length) return;
    const merged = new Map(state.pendingFiles.map(file => [`${file.name}:${file.size}:${file.lastModified}`, file]));
    for (const file of Array.from(fileList)) {
        merged.set(`${file.name}:${file.size}:${file.lastModified}`, file);
    }
    state.pendingFiles = [...merged.values()];
    showUploadDialog();
}

function runUpload() {
    if (!state.pendingFiles.length) return;
    const form = new FormData();
    form.append('targetDirectory', state.uploadTarget);
    for (const file of state.pendingFiles) form.append('files', file, file.name);

    state.busy = true;
    renderBulkToolbar();
    setStatus('Загрузка: 0%');
    const request = new XMLHttpRequest();
    request.open('POST', `${apiRoot}/upload`);
    request.upload.onprogress = event => {
        if (!event.lengthComputable) return;
        const percent = Math.round((event.loaded / event.total) * 100);
        setStatus(`Загрузка: ${percent}% (${formatBytes(event.loaded)} из ${formatBytes(event.total)})`);
        const label = document.querySelector('#upload-progress-label');
        const bar = document.querySelector('#upload-progress > span');
        if (label) label.textContent = `${percent}% · ${formatBytes(event.loaded)} из ${formatBytes(event.total)}`;
        if (bar) bar.style.width = `${percent}%`;
    };
    request.onload = async () => {
        try {
            const envelope = JSON.parse(request.responseText);
            if (request.status < 200 || request.status >= 300 || !envelope.success) {
                throw new Error(envelope.errorMessage || `HTTP ${request.status}`);
            }
            const result = envelope.result;
            showOutcome('Загрузка', result);
            state.pendingFiles = [];
            closeDialog();
            invalidateBrowseCache();
            await refreshTree();
            if (state.showDeleted) await loadTrash();
            else await refreshCurrent();
            await refreshGit();
            setStatus('Загрузка завершена.', 'success');
        } catch (error) {
            setStatus(error.message, 'error');
        } finally {
            state.busy = false;
            renderBulkToolbar();
        }
    };
    request.onerror = () => {
        state.busy = false;
        renderBulkToolbar();
        setStatus('Сеть недоступна во время загрузки.', 'error');
    };
    request.send(form);
}

async function loadTrash({ preserveSelection = false } = {}) {
    state.showDeleted = true;
    state.searching = false;
    state.selected = null;
    state.searchQuery = '';
    if (!preserveSelection) clearSelection();
    clearPreview();
    elements.search.value = '';
    elements.search.disabled = true;
    elements.uploadButton.disabled = true;
    elements.clearSearch.hidden = true;
    elements.title.textContent = 'Корзина';
    elements.breadcrumbs.replaceChildren(createElement('span', '', 'Корзина'));
    elements.list.replaceChildren(createElement('div', 'empty-state', 'Загрузка корзины…'));
    try {
        const entries = await requestJson(`${apiRoot}/entries?includeDeleted=true`);
        const deleted = entries.filter(entry => entry.isDeleted);
        if (preserveSelection) {
            const visibleIds = new Set(deleted.map(entry => entry.id));
            for (const id of state.selectedEntries.keys()) {
                if (!visibleIds.has(id)) state.selectedEntries.delete(id);
            }
        }
        renderTrashRows(deleted);
        renderBulkToolbar();
        elements.count.textContent = `${deleted.length} удалённых файлов`;
        elements.directorySummary.textContent = 'Восстановление удалённых файлов';
        elements.trashToggle.textContent = 'Вернуться к файлам';
    } catch (error) {
        elements.list.replaceChildren(createElement('div', 'error-state', error.message));
        setStatus(error.message, 'error');
    }
}

function renderTrashRows(entries) {
    elements.list.replaceChildren();
    if (!entries.length) {
        elements.list.append(createElement('div', 'empty-state', 'Корзина пуста.'));
        return;
    }
    const table = createElement('table', 'file-table');
    const head = document.createElement('thead');
    const header = document.createElement('tr');
    const selectAllCell = createElement('th');
    const selectAll = createElement('input', 'bulk-checkbox');
    selectAll.type = 'checkbox';
    selectAll.checked = entries.length > 0 && entries.every(entry => state.selectedEntries.has(entry.id));
    selectAll.addEventListener('change', () => {
        for (const entry of entries) {
            if (selectAll.checked) state.selectedEntries.set(entry.id, entry);
            else state.selectedEntries.delete(entry.id);
        }
        renderBulkToolbar();
        renderTrashRows(entries);
    });
    selectAllCell.append(selectAll);
    header.append(selectAllCell);
    for (const label of ['Тип', 'Имя', 'Размер', 'Удалён']) header.append(createElement('th', '', label));
    head.append(header);
    table.append(head);
    const body = document.createElement('tbody');
    for (const entry of entries) {
        const row = document.createElement('tr');
        const checkboxCell = createElement('td');
        checkboxCell.append(createSelectionCheckbox(entry));
        row.append(checkboxCell);
        row.append(createElement('td', 'file-icon', '📄'));
        row.append(createElement('td', 'file-name-cell', entry.fileName));
        row.append(createElement('td', 'size-cell', formatBytes(entry.sizeBytes)));
        row.append(createElement('td', 'date-cell', formatDate(entry.deletedAt)));
        body.append(row);
    }
    table.append(body);
    elements.list.append(table);
}

const FILE_TABLE_COLUMNS = [
    { key: 'selection', label: '', width: 34, sortable: false },
    { key: 'type', label: 'Тип', width: 42, sortable: true },
    { key: 'name', label: 'Имя', width: 280, sortable: true },
    { key: 'extension', label: 'Расширение', width: 86, sortable: true },
    { key: 'size', label: 'Размер', width: 92, sortable: true },
    { key: 'modified', label: 'Изменён', width: 132, sortable: true },
    { key: 'uploaded', label: 'Загружен', width: 132, sortable: true },
    { key: 'lastViewed', label: 'Просмотрен', width: 132, sortable: true },
];

function getColumnWidths() {
    if (Object.keys(state.columnWidths).length) return state.columnWidths;
    const defaults = Object.fromEntries(FILE_TABLE_COLUMNS.map(column => [column.key, column.width]));
    try {
        const saved = JSON.parse(localStorage.getItem(COLUMN_WIDTH_STORAGE_KEY) || '{}');
        for (const column of FILE_TABLE_COLUMNS) {
            const width = Number(saved[column.key]);
            if (Number.isFinite(width) && width >= 42 && width <= 900) defaults[column.key] = width;
        }
    } catch {
        // Storage may be unavailable in private browsing; defaults remain usable.
    }
    state.columnWidths = defaults;
    return defaults;
}

function persistColumnWidths() {
    try {
        localStorage.setItem(COLUMN_WIDTH_STORAGE_KEY, JSON.stringify(getColumnWidths()));
    } catch {
        // The current session still keeps the widths in state.
    }
}

function getFileTypeInfo(fileName, isDirectory = false) {
    if (isDirectory) return { label: 'Каталог', icon: '📁' };
    const extension = getExtension(fileName);
    if (['.mp4', '.webm', '.ogv', '.mov', '.avi', '.wmv', '.mkv'].includes(extension)) {
        return { label: 'Видео', icon: '▶' };
    }
    if (['.mp3', '.wav', '.ogg', '.opus', '.flac'].includes(extension)) {
        return { label: 'Аудио', icon: '♫' };
    }
    if (['.png', '.jpg', '.jpeg', '.gif', '.webp', '.avif', '.bmp'].includes(extension)) {
        return { label: 'Изображение', icon: '▧' };
    }
    if (extension === '.pdf') return { label: 'PDF', icon: 'PDF' };
    if (['.txt', '.log', '.json', '.xml', '.csv', '.md', '.markdown', '.yaml', '.yml'].includes(extension)) {
        return { label: 'Текст', icon: '≡' };
    }
    return { label: 'Файл', icon: '📄' };
}

function getSortValue(item, key, isDirectory = false) {
    switch (key) {
        case 'type': return getFileTypeInfo(item.name || item.fileName, isDirectory).label;
        case 'name': return item.name || item.fileName || '';
        case 'extension': return item.extension || getExtension(item.name || item.fileName);
        case 'size': return Number(item.sizeBytes || 0);
        case 'modified': return item.lastWriteTimeUtc || item.modifiedAt || '';
        case 'uploaded': return item.uploadedAt || '';
        case 'lastViewed': return item.lastDownloadedAt || '';
        default: return item.name || item.fileName || '';
    }
}

function compareSortValues(left, right, key, direction) {
    const a = getSortValue(left.item, key, left.isDirectory);
    const b = getSortValue(right.item, key, right.isDirectory);
    const multiplier = direction === 'desc' ? -1 : 1;
    if (a === '' || a === null || a === undefined) return b === '' || b == null ? 0 : 1;
    if (b === '' || b === null || b === undefined) return -1;
    if (typeof a === 'number' && typeof b === 'number') return (a - b) * multiplier;
    if (key === 'modified' || key === 'uploaded' || key === 'lastViewed') {
        return (Date.parse(a) - Date.parse(b)) * multiplier;
    }
    return String(a).localeCompare(String(b), 'ru', { numeric: true, sensitivity: 'base' }) * multiplier;
}

function sortFileItems(items, key = state.sortKey, direction = state.sortDirection, isDirectory = false) {
    return [...items].sort((a, b) => compareSortValues(
        { item: a, isDirectory },
        { item: b, isDirectory },
        key,
        direction
    ));
}

function changeSort(key) {
    if (state.sortKey === key) {
        state.sortDirection = state.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
        state.sortKey = key;
        state.sortDirection = 'asc';
    }
    if (state.searching && state.currentSearchResult) {
        renderSearchResults(state.currentSearchResult);
    } else if (state.currentBrowse) {
        renderRows(state.currentBrowse);
    }
}

function createFileTable() {
    const table = createElement('table', 'file-table');
    const widths = getColumnWidths();
    const colgroup = document.createElement('colgroup');
    let totalWidth = 0;
    for (const column of FILE_TABLE_COLUMNS) {
        const col = document.createElement('col');
        col.dataset.column = column.key;
        const width = widths[column.key] || column.width;
        col.style.width = `${width}px`;
        totalWidth += width;
        colgroup.append(col);
    }
    table.append(colgroup);
    table.style.minWidth = `${totalWidth}px`;

    const thead = document.createElement('thead');
    const header = document.createElement('tr');
    for (const column of FILE_TABLE_COLUMNS) {
        const th = document.createElement('th');
        th.dataset.column = column.key;
        if (column.sortable) {
            const sortButton = createElement('button', 'sort-button');
            sortButton.type = 'button';
            const active = state.sortKey === column.key;
            sortButton.textContent = active
                ? `${column.label} ${state.sortDirection === 'asc' ? '↑' : '↓'}`
                : column.label;
            sortButton.setAttribute('aria-label', `Сортировать по колонке «${column.label}»`);
            sortButton.setAttribute('aria-sort', active
                ? (state.sortDirection === 'asc' ? 'ascending' : 'descending')
                : 'none');
            sortButton.addEventListener('click', () => changeSort(column.key));
            th.append(sortButton);
        } else if (column.label) {
            th.textContent = column.label;
        }

        if (column.key !== 'selection') {
            const resizer = createElement('span', 'column-resizer');
            resizer.setAttribute('role', 'separator');
            resizer.setAttribute('aria-orientation', 'vertical');
            resizer.setAttribute('aria-label', `Изменить ширину колонки «${column.label}»`);
            resizer.addEventListener('pointerdown', event => beginColumnResize(event, table, column.key));
            th.append(resizer);
        }
        header.append(th);
    }
    thead.append(header);
    table.append(thead);
    table.append(document.createElement('tbody'));
    return table;
}

function beginColumnResize(event, table, columnKey) {
    event.preventDefault();
    event.stopPropagation();
    const col = table.querySelector(`col[data-column="${columnKey}"]`);
    if (!col) return;
    const widths = getColumnWidths();
    const startX = event.clientX;
    const startWidth = widths[columnKey] || Number.parseFloat(col.style.width) || 100;
    const onMove = moveEvent => {
        const width = Math.max(42, Math.min(900, startWidth + moveEvent.clientX - startX));
        widths[columnKey] = Math.round(width);
        col.style.width = `${widths[columnKey]}px`;
        table.style.minWidth = `${FILE_TABLE_COLUMNS.reduce((sum, item) => sum + (widths[item.key] || item.width), 0)}px`;
    };
    const onUp = () => {
        window.removeEventListener('pointermove', onMove);
        window.removeEventListener('pointerup', onUp);
        persistColumnWidths();
    };
    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp, { once: true });
}

function appendFileRowCells(row, { entry, name, sizeBytes, modifiedAt, uploadedAt, lastViewedAt, isDirectory = false, onOpen }) {
    const fileType = getFileTypeInfo(name, isDirectory);
    const typeCell = createElement('td', 'type-cell', fileType.icon);
    typeCell.title = fileType.label;
    row.append(typeCell);

    const nameCell = createElement('td', 'file-name-cell');
    const button = createElement('button', 'file-name-button', name);
    button.type = 'button';
    if (onOpen) button.addEventListener('click', onOpen);
    nameCell.append(button);
    row.append(nameCell);

    row.append(createElement('td', 'extension-cell', isDirectory ? '—' : (entry?.extension || getExtension(name) || '—')));
    row.append(createElement('td', 'size-cell', isDirectory ? '—' : formatBytes(sizeBytes)));
    row.append(createElement('td', 'date-cell', isDirectory ? '—' : formatDate(modifiedAt)));
    row.append(createElement('td', 'date-cell', isDirectory ? '—' : formatDate(uploadedAt)));
    row.append(createElement('td', 'date-cell', isDirectory ? '—' : formatDate(lastViewedAt)));
}

function renderRows(items) {
    elements.list.replaceChildren();
    const dirs = sortFileItems(items.directories || [], state.sortKey, state.sortDirection, true);
    const files = sortFileItems(items.files || [], state.sortKey, state.sortDirection, false);
    if (!dirs.length && !files.length) {
        elements.list.append(createElement('div', 'empty-state', 'Каталог пуст.'));
        return;
    }

    const table = createFileTable();
    const tbody = table.querySelector('tbody');
    const selectableFiles = files.filter(file => file.isIndexed && file.storageEntryId);
    const selectionHeader = table.querySelector('th[data-column="selection"]');
    const selectAll = createElement('input', 'bulk-checkbox');
    selectAll.type = 'checkbox';
    selectAll.checked = selectableFiles.length > 0
        && selectableFiles.every(file => state.selectedEntries.has(file.storageEntryId));
    selectAll.disabled = selectableFiles.length === 0;
    selectAll.setAttribute('aria-label', 'Выбрать все индексированные файлы');
    selectAll.addEventListener('change', () => {
        for (const file of selectableFiles) {
            if (selectAll.checked) {
                state.selectedEntries.set(file.storageEntryId, {
                    id: file.storageEntryId,
                    fileName: file.name,
                    path: file.relativePath,
                    sizeBytes: file.sizeBytes,
                    uploadedAt: file.uploadedAt,
                    lastDownloadedAt: file.lastDownloadedAt,
                    isDeleted: false,
                });
            } else {
                state.selectedEntries.delete(file.storageEntryId);
            }
        }
        renderBulkToolbar();
        renderRows(items);
    });
    selectionHeader.append(selectAll);

    for (const directory of dirs) {
        const row = document.createElement('tr');
        row.className = 'directory-row';
        row.append(document.createElement('td'));
        appendFileRowCells(row, {
            name: directory.name,
            isDirectory: true,
            onOpen: () => navigate(directory.relativePath),
        });
        tbody.append(row);
    }

    for (const file of files) {
        const row = document.createElement('tr');
        const entry = file.storageEntryId ? {
            id: file.storageEntryId,
            fileName: file.name,
            path: file.relativePath,
            sizeBytes: file.sizeBytes,
            uploadedAt: file.uploadedAt,
            lastDownloadedAt: file.lastDownloadedAt,
            isDeleted: false,
        } : null;
        const checkboxCell = document.createElement('td');
        if (entry) checkboxCell.append(createSelectionCheckbox(entry, !file.isIndexed));
        else checkboxCell.append(createElement('span', 'muted', '—'));
        row.append(checkboxCell);
        appendFileRowCells(row, {
            entry: { extension: getExtension(file.name) },
            name: file.name,
            sizeBytes: file.sizeBytes,
            modifiedAt: file.lastWriteTimeUtc,
            uploadedAt: file.uploadedAt,
            lastViewedAt: file.lastDownloadedAt,
            onOpen: () => void openPreview(file),
        });
        row.classList.toggle('is-selected', state.selected?.relativePath === file.relativePath);
        row.addEventListener('click', event => {
            if (event.target.closest('button, input, a, .column-resizer')) return;
            void openPreview(file);
        });
        if (!file.isIndexed) {
            const nameCell = row.querySelector('.file-name-cell');
            nameCell.append(createElement('span', 'unindexed', 'Не индексирован'));
        }
        tbody.append(row);
    }

    elements.list.append(table);
}

function renderSearchResults(result) {
    state.currentSearchResult = result;
    elements.list.replaceChildren();
    const entries = sortFileItems(result.entries || [], state.sortKey, state.sortDirection, false);
    if (!entries.length) {
        elements.list.append(createElement('div', 'empty-state', 'Совпадений не найдено.'));
        return;
    }

    const table = createFileTable();
    const tbody = table.querySelector('tbody');
    const selectionHeader = table.querySelector('th[data-column="selection"]');
    const selectAll = createElement('input', 'bulk-checkbox');
    selectAll.type = 'checkbox';
    selectAll.checked = entries.length > 0 && entries.every(entry => state.selectedEntries.has(entry.id));
    selectAll.addEventListener('change', () => {
        for (const entry of entries) {
            if (selectAll.checked) state.selectedEntries.set(entry.id, entry);
            else state.selectedEntries.delete(entry.id);
        }
        renderBulkToolbar();
        renderSearchResults(result);
    });
    selectionHeader.append(selectAll);

    for (const entry of entries) {
        const row = document.createElement('tr');
        const checkboxCell = document.createElement('td');
        checkboxCell.append(createSelectionCheckbox(entry));
        row.append(checkboxCell);
        appendFileRowCells(row, {
            entry,
            name: entry.fileName,
            sizeBytes: entry.sizeBytes,
            modifiedAt: null,
            uploadedAt: entry.uploadedAt,
            lastViewedAt: entry.lastDownloadedAt,
            onOpen: () => void openPreview({
                name: entry.fileName,
                relativePath: entry.path,
                sizeBytes: entry.sizeBytes,
                lastWriteTimeUtc: null,
                uploadedAt: entry.uploadedAt,
                lastDownloadedAt: entry.lastDownloadedAt,
                storageEntryId: entry.id,
                isIndexed: true,
            }),
        });
        row.addEventListener('click', event => {
            if (event.target.closest('button, input, a, .column-resizer')) return;
            void openPreview({
                name: entry.fileName,
                relativePath: entry.path,
                sizeBytes: entry.sizeBytes,
                lastWriteTimeUtc: entry.uploadedAt,
                uploadedAt: entry.uploadedAt,
                lastDownloadedAt: entry.lastDownloadedAt,
                storageEntryId: entry.id,
                isIndexed: true,
            });
        });
        tbody.append(row);
    }
    elements.list.append(table);

    const pages = Math.max(1, Math.ceil(result.total / result.pageSize));
    const pagination = createElement('div', 'pagination');
    const previous = createElement('button', 'button button-quiet', '← Назад');
    previous.type = 'button';
    previous.disabled = result.page <= 1;
    previous.addEventListener('click', () => void performSearch(state.searchQuery, result.page - 1));
    const next = createElement('button', 'button button-quiet', 'Вперёд →');
    next.type = 'button';
    next.disabled = result.page >= pages;
    next.addEventListener('click', () => void performSearch(state.searchQuery, result.page + 1));
    pagination.append(previous);
    pagination.append(createElement('span', 'muted', `Страница ${result.page} из ${pages} · всего ${result.total}`));
    pagination.append(next);
    elements.list.append(pagination);
    elements.directorySummary.textContent = `Найдено: ${result.total}`;
}async function loadDirectory(directory, { updateHistory = true, preserveSelection = false, force = false } = {}) {
    state.searching = false;
    state.searchQuery = '';
    state.currentDirectory = directory;
    state.showDeleted = false;
    elements.search.disabled = false;
    elements.trashToggle.textContent = 'Корзина';
    elements.uploadButton.disabled = false;
    if (!preserveSelection) clearSelection();
    clearPreview();
    elements.search.value = '';
    elements.clearSearch.hidden = true;
    buildBreadcrumbs(directory);
    elements.title.textContent = directory ? directory.split('/').at(-1) : 'wwwroot';
    elements.list.replaceChildren(createElement('div', 'empty-state', 'Загрузка…'));
    try {
        const result = await browse(directory, { force });
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

async function performSearch(query, page = 1, { updateHistory = true, preserveSelection = false } = {}) {
    const normalized = query.trim();
    if (!normalized) return loadDirectory(state.currentDirectory);
    state.showDeleted = false;
    elements.search.disabled = false;
    elements.trashToggle.textContent = 'Корзина';
    if (!preserveSelection) clearSelection();
    state.searching = true;
    state.searchQuery = normalized;
    elements.clearSearch.hidden = false;
    elements.title.textContent = 'Поиск';
    elements.breadcrumbs.replaceChildren(createElement('span', '', 'Результаты глобального поиска'));
    elements.list.replaceChildren(createElement('div', 'empty-state', 'Поиск…'));
    try {
        const result = await searchFiles(normalized, page);
        state.currentSearchResult = result;
        state.page = result.page;
        renderSearchResults(result);
        elements.count.textContent = `${result.total} результатов`;
        elements.directorySummary.textContent = `Страница ${result.page} из ${Math.max(1, Math.ceil(result.total / result.pageSize))}`;
        if (updateHistory) {
            const url = new URL(window.location.href);
            url.searchParams.set('q', normalized);
            url.searchParams.set('page', String(page));
            url.searchParams.delete('directory');
            window.history.pushState({ query: normalized, page }, '', url);
        }
    } catch (error) {
        setStatus(error.message, 'error');
    }
}

async function refreshCurrent() {
    if (state.showDeleted) return loadTrash({ preserveSelection: true });
    if (state.searching) return performSearch(state.searchQuery, state.page, { updateHistory: false, preserveSelection: true });
    const selectedPath = state.selected?.relativePath;
    const selected = new Map(state.selectedEntries);
    await loadDirectory(state.currentDirectory, { updateHistory: false, preserveSelection: true, force: true });
    for (const [id, entry] of selected) {
        const found = state.currentBrowse?.files?.find(file => file.storageEntryId === id);
        if (!found) state.selectedEntries.delete(id);
    }
    renderBulkToolbar();
    if (selectedPath) {
        const refreshed = state.currentBrowse?.files?.find(file => file.relativePath === selectedPath);
        if (refreshed) await openPreview(refreshed);
        else clearPreview();
    }
}

async function refreshTree({ force = true } = {}) {
    try {
        if (force) invalidateBrowseCache();
        state.expanded.clear();
        const root = await browse('', { force });
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
    } catch (error) {
        elements.tree.replaceChildren(createElement('div', 'error-state', error.message));
    }
}

async function runIndex() {
    elements.index.disabled = true;
    setStatus('Индексация файлов…');
    try {
        const result = await requestJson(`${apiRoot}/index`, { method: 'POST' });
        setStatus(`Индексация завершена: добавлено ${result.added}, обновлено ${result.updated}.`, 'success');
        invalidateBrowseCache();
        await refreshTree({ force: true });
        await refreshCurrent();
    } catch (error) {
        setStatus(error.message, 'error');
    } finally {
        elements.index.disabled = false;
    }
}

elements.refresh.addEventListener('click', async () => {
    invalidateBrowseCache();
    await refreshTree({ force: true });
    await refreshCurrent();
});
elements.treeRefresh.addEventListener('click', () => void refreshTree());
elements.previewClose.addEventListener('click', clearPreview);
elements.index.addEventListener('click', () => void runIndex());
elements.uploadButton.addEventListener('click', () => {
    state.pendingFiles = [];
    state.uploadTarget = state.currentDirectory || 'Uploads';
    showUploadDialog();
});
elements.fileInput.addEventListener('change', () => {
    addPendingFiles(elements.fileInput.files);
    elements.fileInput.value = '';
});
elements.replaceFileInput.addEventListener('change', () => {
    const file = elements.replaceFileInput.files?.[0];
    if (file) void replaceSelectedFile(file);
    elements.replaceFileInput.value = '';
});
elements.trashToggle.addEventListener('click', () => {
    if (state.showDeleted) {
        state.showDeleted = false;
        void loadDirectory(state.currentDirectory);
    } else {
        void loadTrash();
    }
});
elements.list.addEventListener('dragover', event => {
    if (state.showDeleted || state.searching) return;
    event.preventDefault();
    elements.list.classList.add('drag-over');
});
elements.list.addEventListener('dragleave', event => {
    if (!elements.list.contains(event.relatedTarget)) elements.list.classList.remove('drag-over');
});
elements.list.addEventListener('drop', event => {
    if (state.showDeleted || state.searching) return;
    event.preventDefault();
    elements.list.classList.remove('drag-over');
    state.pendingFiles = [];
    state.uploadTarget = state.currentDirectory || 'Uploads';
    addPendingFiles(event.dataTransfer.files);
});
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
    // MediaStorage broadcasts only server-to-client notifications. A small
    // native WebSocket client is sufficient for the SignalR JSON protocol and
    // avoids shipping a runtime dependency or fetching code from a CDN.
    let reconnectTimer = null;
    let socket = null;
    let stopped = false;

    const scheduleReconnect = () => {
        if (stopped || reconnectTimer !== null) return;
        elements.connection.textContent = 'SignalR: переподключение…';
        reconnectTimer = window.setTimeout(() => {
            reconnectTimer = null;
            void connect();
        }, 5000);
    };

    async function connect() {
        try {
            const negotiateUrl = new URL('/hubs/media-storage/negotiate', window.location.origin);
            negotiateUrl.searchParams.set('negotiateVersion', '1');
            const negotiateResponse = await fetch(negotiateUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                cache: 'no-store',
            });
            if (!negotiateResponse.ok) throw new Error(`SignalR negotiate HTTP ${negotiateResponse.status}`);
            const negotiation = await negotiateResponse.json();
            if (!negotiation.connectionToken) throw new Error('SignalR не вернул connectionToken');

            const websocketUrl = new URL('/hubs/media-storage', window.location.href);
            websocketUrl.protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
            websocketUrl.searchParams.set('id', negotiation.connectionToken);
            socket = new WebSocket(websocketUrl);
            let buffer = '';
            let heartbeat = null;

            socket.addEventListener('open', () => {
                socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + '\u001e');
                heartbeat = window.setInterval(() => {
                    if (socket?.readyState === WebSocket.OPEN) {
                        socket.send(JSON.stringify({ type: 6 }) + '\u001e');
                    }
                }, 15000);
                elements.connection.textContent = 'SignalR: подключено';
            });

            socket.addEventListener('message', event => {
                buffer += String(event.data);
                const frames = buffer.split('\u001e');
                buffer = frames.pop() || '';
                for (const frame of frames) {
                    if (!frame) continue;
                    let message;
                    try { message = JSON.parse(frame); } catch { continue; }
                    if (message.type === 7) {
                        socket?.close();
                        return;
                    }
                    if (message.type === 1 &&
                        (message.target === 'StorageChanged' || message.target === 'StorageStructureChanged')) {
                        invalidateBrowseCache();
                        void refreshTree({ force: true });
                        void refreshCurrent();
                        void refreshGit();
                    }
                }
            });

            socket.addEventListener('close', () => {
                if (heartbeat !== null) window.clearInterval(heartbeat);
                elements.connection.textContent = 'SignalR: отключено';
                scheduleReconnect();
            });
            socket.addEventListener('error', () => socket?.close());
        } catch {
            elements.connection.textContent = 'SignalR: недоступен (ручное обновление работает)';
            scheduleReconnect();
        }
    }

    await connect();
    window.addEventListener('pagehide', () => {
        stopped = true;
        if (reconnectTimer !== null) window.clearTimeout(reconnectTimer);
        socket?.close();
    }, { once: true });
}

async function initialize() {
    const url = new URL(window.location.href);
    const directory = url.searchParams.get('directory') || '';
    if (url.searchParams.has('q')) {
        elements.search.value = url.searchParams.get('q') || '';
        await performSearch(elements.search.value, Number(url.searchParams.get('page') || 1), { updateHistory: false });
    } else {
        await loadDirectory(directory, { updateHistory: false });
    }
    await refreshTree();
    await refreshGit();
    void connectHub();
}

initialize();
