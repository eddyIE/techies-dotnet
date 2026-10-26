// Magento-style admin grids. The server renders the grid (Views/Shared/_Grid.cshtml); this
// script only swaps it in place: sorting, paging, page size and filters are fetched with the
// X-Grid header (server returns just the partial) and the URL is updated with pushState so the
// back button and bookmarks work. Without JavaScript every link and form still works as a full page.
(() => {
    const grids = document.querySelectorAll("[data-grid]");
    if (grids.length === 0) return;

    async function load(grid, url, push = true) {
        grid.classList.add("opacity-50");
        try {
            const response = await fetch(url, { headers: { "X-Grid": "1" } });
            if (!response.ok || response.redirected) { location.href = url; return; }
            grid.innerHTML = await response.text();
            if (push) history.pushState({ grid: grid.id }, "", url);
        } catch {
            location.href = url;
        } finally {
            grid.classList.remove("opacity-50");
        }
    }

    function selected(grid) { return [...grid.querySelectorAll("[data-grid-row]:checked")]; }

    function refreshSelection(grid) {
        const rows = [...grid.querySelectorAll("[data-grid-row]")];
        const count = selected(grid).length;
        const all = grid.querySelector("[data-grid-select-all]");
        if (all) { all.checked = rows.length > 0 && count === rows.length; all.indeterminate = count > 0 && count < rows.length; }
        const submit = grid.querySelector("[data-grid-mass-submit]");
        const action = grid.querySelector("[data-grid-mass-select]");
        if (submit) submit.disabled = count === 0 || !action.value;
        const label = grid.querySelector("[data-grid-selected-count]");
        if (label) label.textContent = count > 0 ? `(${count})` : "";
    }

    grids.forEach((grid) => {
        grid.addEventListener("click", (e) => {
            const link = e.target.closest("a[data-grid-link]");
            if (!link || e.ctrlKey || e.metaKey) return;
            e.preventDefault();
            load(grid, link.href);
        });

        grid.addEventListener("change", (e) => {
            if (e.target.matches("[data-grid-page-size]")) load(grid, e.target.value);
            if (e.target.matches("[data-grid-select-all]"))
                grid.querySelectorAll("[data-grid-row]").forEach((box) => (box.checked = e.target.checked));
            refreshSelection(grid);
        });

        grid.addEventListener("submit", (e) => {
            const form = e.target;
            if (form.matches("[data-grid-filter]")) {
                // Empty fields are left out so the URL only carries the filters in use.
                e.preventDefault();
                const params = new URLSearchParams();
                for (const [key, value] of new FormData(form)) if (String(value).trim() !== "") params.append(key, value);
                load(grid, form.action + (params.size ? "?" + params : ""));
            } else if (form.matches("[data-grid-mass]")) {
                const option = grid.querySelector("[data-grid-mass-select]").selectedOptions[0];
                const count = selected(grid).length;
                if (!option.value || count === 0) { e.preventDefault(); return; }
                if (!confirm(`${option.dataset.confirm || option.textContent} — ${count} dòng đã chọn?`)) { e.preventDefault(); return; }
                form.action = option.value;
            } else if (form.dataset.confirm && !confirm(form.dataset.confirm)) {
                e.preventDefault();
            }
        });

        // "Hủy" in the filter panel throws away unapplied edits.
        grid.addEventListener("click", (e) => {
            if (e.target.closest("[data-grid-filter-cancel]")) e.target.closest("form").reset();
        });
    });

    window.addEventListener("popstate", () => grids.forEach((grid) => load(grid, location.href, false)));
})();
