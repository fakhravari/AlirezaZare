(() => {
    "use strict";

    const $ = window.jQuery;
    if (!$?.fn?.select2) return;

    const selector = "select:not([data-native-select])";

    function refresh(select) {
        if (select instanceof HTMLSelectElement && $(select).hasClass("select2-hidden-accessible"))
            $(select).trigger("change.select2");
    }

    function initialize(root) {
        const selects = [];
        if (root instanceof HTMLSelectElement && root.matches(selector)) selects.push(root);
        if (root.querySelectorAll) selects.push(...root.querySelectorAll(selector));

        for (const select of selects) {
            if ($(select).hasClass("select2-hidden-accessible")) {
                refresh(select);
                continue;
            }
            const modal = select.closest(".modal");
            $(select).select2({
                dir: "rtl",
                language: "fa",
                width: "100%",
                minimumResultsForSearch: 0,
                dropdownParent: modal ? $(modal) : $(document.body)
            });
        }
    }

    initialize(document);
    new MutationObserver(records => {
        for (const record of records) {
            if (record.type === "childList") {
                for (const node of record.addedNodes) if (node instanceof Element) initialize(node);
                if (record.target instanceof HTMLSelectElement) refresh(record.target);
            } else if (record.target instanceof HTMLSelectElement) refresh(record.target);
        }
    }).observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ["selected", "disabled"] });

    document.addEventListener("show.bs.modal", event => initialize(event.target));
    document.addEventListener("shown.bs.modal", event => event.target.querySelectorAll(selector).forEach(refresh));
    document.addEventListener("reset", event => setTimeout(() => event.target.querySelectorAll?.(selector).forEach(refresh), 0), true);
    document.addEventListener("invalid", event => {
        const select = event.target;
        if (!(select instanceof HTMLSelectElement) || !$(select).hasClass("select2-hidden-accessible")) return;
        event.preventDefault();
        $(select).select2("open");
    }, true);
})();
