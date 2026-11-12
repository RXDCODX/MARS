(() => {
    "use strict";

    const widthStorageKey = "mars-media-storage-column-widths-v1";
    const minColumnWidth = 64;
    const maxColumnWidth = 1000;

    function readSavedWidths() {
        try {
            const value = JSON.parse(localStorage.getItem(widthStorageKey) || "{}");
            return value && typeof value === "object" ? value : {};
        } catch {
            return {};
        }
    }

    function saveWidths(widths) {
        try {
            localStorage.setItem(widthStorageKey, JSON.stringify(widths));
        } catch {
            // The browser may disable storage. Resizing still works for this page.
        }
    }

    function initResizableTable(table) {
        const columns = new Map(
            [...table.querySelectorAll("col[data-column]")].map(column => [column.dataset.column, column])
        );
        const savedWidths = readSavedWidths();

        for (const [name, column] of columns) {
            const savedWidth = Number(savedWidths[name]);
            if (Number.isFinite(savedWidth) && savedWidth >= minColumnWidth && savedWidth <= maxColumnWidth) {
                column.style.width = `${savedWidth}px`;
            }
        }

        for (const header of table.querySelectorAll("th[data-resize-column]")) {
            const name = header.dataset.resizeColumn;
            const column = columns.get(name);
            if (!column) continue;

            const handle = document.createElement("span");
            handle.className = "column-resizer";
            handle.setAttribute("role", "separator");
            handle.setAttribute("aria-orientation", "vertical");
            handle.setAttribute("aria-label", `Изменить ширину столбца ${name}`);
            handle.tabIndex = 0;
            header.append(handle);

            handle.addEventListener("pointerdown", event => {
                if (event.button !== 0) return;
                event.preventDefault();

                const startX = event.clientX;
                const startWidth = column.getBoundingClientRect().width;
                handle.setPointerCapture(event.pointerId);
                table.classList.add("is-resizing");

                const move = moveEvent => {
                    const width = Math.min(maxColumnWidth, Math.max(minColumnWidth, startWidth + moveEvent.clientX - startX));
                    column.style.width = `${Math.round(width)}px`;
                };

                const finish = finishEvent => {
                    if (finishEvent.pointerId !== event.pointerId) return;
                    handle.removeEventListener("pointermove", move);
                    handle.removeEventListener("pointerup", finish);
                    handle.removeEventListener("pointercancel", finish);
                    table.classList.remove("is-resizing");
                    const width = Math.round(column.getBoundingClientRect().width);
                    saveWidths({ ...readSavedWidths(), [name]: width });
                };

                handle.addEventListener("pointermove", move);
                handle.addEventListener("pointerup", finish);
                handle.addEventListener("pointercancel", finish);
            });

            handle.addEventListener("keydown", event => {
                if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return;
                event.preventDefault();
                const delta = event.key === "ArrowRight" ? 10 : -10;
                const width = Math.min(maxColumnWidth, Math.max(minColumnWidth, column.getBoundingClientRect().width + delta));
                column.style.width = `${width}px`;
                saveWidths({ ...readSavedWidths(), [name]: width });
            });
        }
    }

    function getDialog(id) {
        const dialog = document.getElementById(id);
        return dialog instanceof HTMLDialogElement ? dialog : null;
    }

    document.addEventListener("click", event => {
        const opener = event.target.closest("[data-open-dialog]");
        if (opener) {
            const dialog = getDialog(opener.dataset.openDialog);
            if (!dialog) return;

            if (dialog.id === "replace-dialog") {
                const path = opener.dataset.filePath || "";
                const name = opener.dataset.fileName || path.split("/").pop() || path;
                const pathInput = dialog.querySelector("#replacement-path");
                const fileInput = dialog.querySelector("#replacement-file");
                const description = dialog.querySelector("#replace-dialog-description");
                const validation = dialog.querySelector("#replacement-validation");
                const submit = dialog.querySelector("#replacement-submit");
                pathInput.value = path;
                fileInput.value = "";
                fileInput.dataset.expectedExtension = getExtension(name);
                fileInput.accept = fileInput.dataset.expectedExtension || "";
                description.textContent = `Новый файл заменит «${name}». Расширение должно совпадать с оригиналом.`;
                validation.textContent = "";
                submit.disabled = true;
            }

            if (dialog.id === "move-dialog") {
                dialog.querySelector("#move-entry-id").value = opener.dataset.entryId || "";
                dialog.querySelector("#move-dialog-description").textContent =
                    `Переместить «${opener.dataset.fileName || "файл"}» в другой каталог.`;
            }

            if (!dialog.open) dialog.showModal();
            return;
        }

        const closer = event.target.closest("[data-close-dialog]");
        if (closer) {
            closer.closest("dialog")?.close();
        }
    });

    function getExtension(fileName) {
        const name = String(fileName || "").split(/[\\/]/).pop();
        const index = name.lastIndexOf(".");
        return index > 0 ? name.slice(index).toLowerCase() : "";
    }

    const replaceForm = document.getElementById("replace-form");
    const replacementInput = document.getElementById("replacement-file");
    const replacementValidation = document.getElementById("replacement-validation");
    const replacementSubmit = document.getElementById("replacement-submit");

    function validateReplacement() {
        if (!replacementInput || !replacementValidation || !replacementSubmit) return false;
        const file = replacementInput.files?.[0];
        if (!file) {
            replacementValidation.textContent = "";
            replacementSubmit.disabled = true;
            return false;
        }

        const expected = replacementInput.dataset.expectedExtension || "";
        const actual = getExtension(file.name);
        const matches = actual === expected;
        replacementValidation.textContent = matches
            ? `Формат совпадает (${actual || "без расширения"}).`
            : `Формат не совпадает: ожидается ${expected || "без расширения"}, выбран ${actual || "без расширения"}.`;
        replacementValidation.classList.toggle("is-error", !matches);
        replacementSubmit.disabled = !matches;
        return matches;
    }

    replacementInput?.addEventListener("change", validateReplacement);
    replaceForm?.addEventListener("submit", event => {
        if (!validateReplacement()) event.preventDefault();
    });

    document.querySelectorAll("table[data-resizable-columns]").forEach(initResizableTable);

    document.addEventListener("click", event => {
        const interactive = event.target.closest("a, button, form, input, select, textarea, label, summary");
        if (interactive) return;
        const row = event.target.closest("tr[data-preview-url]");
        if (row?.dataset.previewUrl) window.location.assign(row.dataset.previewUrl);
    });

    document.addEventListener("keydown", event => {
        if (event.key !== "Enter" && event.key !== " ") return;
        const row = event.target.closest("tr[data-preview-url]");
        if (!row || event.target !== row) return;
        event.preventDefault();
        window.location.assign(row.dataset.previewUrl);
    });
})();
