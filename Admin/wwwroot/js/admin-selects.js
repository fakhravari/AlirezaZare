(() => {
    "use strict";

    const $ = window.jQuery;
    if (!$?.fn?.select2) return;

    const selector = "select:not([data-native-select])";
    const longOptionLength = 36;
    const defaultMatcher = $.fn.select2.defaults.defaults.matcher;

    function visibleOptions(data) {
        if (data.element?.hidden) return null;
        if (!data.children) return data;
        const children = data.children.map(visibleOptions).filter(Boolean);
        return children.length ? { ...data, children } : null;
    }

    function prepareModalWidth(select) {
        const modal = select.closest(".modal");
        if (!modal) return;

        const hasLongOption = [...select.options].some(option => option.textContent.trim().length > longOptionLength);
        if (!hasLongOption) return;

        select.closest(".admin-modal-field")?.classList.add("admin-modal-field-wide");
        modal.querySelector(".modal-dialog")?.classList.add("admin-modal-wide");
    }

    function refresh(select) {
        if (!(select instanceof HTMLSelectElement)) return;
        prepareModalWidth(select);
        if ($(select).hasClass("select2-hidden-accessible")) $(select).trigger("change.select2");
    }

    function initialize(root) {
        const selects = [];
        if (root instanceof HTMLSelectElement && root.matches(selector)) selects.push(root);
        if (root.querySelectorAll) selects.push(...root.querySelectorAll(selector));

        for (const select of selects) {
            const modal = select.closest(".modal");
            if (modal && !modal.classList.contains("show")) continue;
            prepareModalWidth(select);

            if ($(select).hasClass("select2-hidden-accessible")) {
                refresh(select);
                continue;
            }
            const productFilter = select.matches("[data-product-filter]");
            const checkboxFilter = select.matches("[data-product-filter], [data-checkbox-filter]");
            const productApi = select.dataset.productApi || select.closest("[data-store-variant-prices]")?.dataset.api;
            const productCandidatesUrl = select.dataset.productCandidatesUrl;
            const remoteProductUrl = select.dataset.remoteProductUrl;
            $(select).select2({
                dir: "rtl",
                language: "fa",
                width: "100%",
                minimumResultsForSearch: 0,
                ...(remoteProductUrl ? {
                    placeholder: "انتخاب کالا با نام یا کد",
                    allowClear: true,
                    ajax: {
                        url: remoteProductUrl,
                        delay: 250,
                        data: params => ({ search: params.term || "", page: params.page || 1 }),
                        processResults: data => ({
                            results: data.items.map(item => ({ id: String(item.id), text: `${item.name} (${item.code})` })),
                            pagination: { more: data.more }
                        })
                    }
                } : productFilter && (productCandidatesUrl || productApi) ? {
                    placeholder: "انتخاب کالا با نام یا کد",
                    closeOnSelect: false,
                    ajax: {
                        url: productCandidatesUrl || `${productApi}/products`,
                        delay: 250,
                        data: params => ({ q: params.term || "", page: params.page || 1 }),
                        processResults: data => ({
                            results: data.items.map(item => ({ id: String(item.id), text: `${item.name} (${item.code})` })),
                            pagination: { more: data.more }
                        })
                    },
                    templateResult: item => {
                        if (!item.id) return item.text;
                        const label = document.createElement("span");
                        label.className = "admin-product-filter-option";
                        const box = document.createElement("input");
                        box.type = "checkbox";
                        box.value = String(item.id);
                        box.tabIndex = -1;
                        box.setAttribute("aria-hidden", "true");
                        box.checked = [...select.selectedOptions].some(option => option.value === String(item.id));
                        label.append(box, document.createTextNode(item.text));
                        return $(label);
                    }
                } : {}),
                ...(!productFilter && checkboxFilter ? {
                    closeOnSelect: false,
                    templateResult: item => {
                        if (!item.id) return item.text;
                        const label = document.createElement("span");
                        label.className = "admin-product-filter-option";
                        const box = document.createElement("input");
                        box.type = "checkbox";
                        box.value = String(item.id);
                        box.tabIndex = -1;
                        box.setAttribute("aria-hidden", "true");
                        box.checked = [...select.selectedOptions].some(option => option.value === String(item.id));
                        label.append(box, document.createTextNode(item.text));
                        return $(label);
                    }
                } : {}),
                matcher: (params, data) => {
                    const visible = visibleOptions(data);
                    return visible ? defaultMatcher(params, visible) : null;
                },
                dropdownParent: modal ? $(modal) : $(document.body)
            });
            if (checkboxFilter) $(select).on("select2:select select2:unselect", () => {
                const selected = new Set([...select.selectedOptions].map(option => option.value));
                document.querySelectorAll(".select2-container--open .admin-product-filter-option input").forEach(box => {
                    box.checked = selected.has(box.value);
                });
            });
            const element = $(select).data("select2").$element;
            const trigger = element.trigger;
            element.trigger = function (event, ...args) {
                if (event === "input" || event === "change") {
                    select.dispatchEvent(new Event(event, { bubbles: true }));
                    return this;
                }
                return trigger.call(this, event, ...args);
            };
        }
    }

    initialize(document);
    new MutationObserver(records => {
        for (const record of records) {
            if (record.type === "childList") {
                for (const node of record.addedNodes) if (node instanceof Element) initialize(node);
                const select = record.target instanceof Element ? record.target.closest("select") : null;
                if (select) refresh(select);
            } else {
                const select = record.target instanceof Element ? record.target.closest("select") : null;
                if (select) refresh(select);
            }
        }
    }).observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ["selected", "disabled"] });

    document.addEventListener("shown.bs.modal", event => initialize(event.target));
    document.addEventListener("reset", event => setTimeout(() => event.target.querySelectorAll?.(selector).forEach(refresh), 0), true);
    document.addEventListener("invalid", event => {
        const select = event.target;
        if (!(select instanceof HTMLSelectElement) || !$(select).hasClass("select2-hidden-accessible")) return;
        event.preventDefault();
        $(select).select2("open");
    }, true);
})();
