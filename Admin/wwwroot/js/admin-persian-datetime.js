(() => {
    "use strict";

    const dateSelector = 'input[type="date"], input[type="datetime-local"]';
    const instances = new WeakMap();
    let observer;
    let activePopover;

    document.addEventListener("pointerdown", event => {
        const state = activePopover;
        if (!state) return;
        const popover = document.getElementById(state.textInput.getAttribute("aria-describedby"));
        if (state.textInput.contains(event.target) || popover?.contains(event.target)) return;
        if (state.shown) state.picker.hide();
        else state.dismissWhenShown = true;
    }, true);

    function parseValue(value, kind) {
        if (!value) return null;
        const parts = value.match(/^(\d{4})-(\d{2})-(\d{2})(?:T| )(\d{2}):(\d{2})(?::(\d{2}))?$/);
        if (kind === "datetime-local") {
            if (!parts) return null;
            return new Date(Number(parts[1]), Number(parts[2]) - 1, Number(parts[3]), Number(parts[4]), Number(parts[5]), Number(parts[6] || 0));
        }

        const dateParts = value.match(/^(\d{4})-(\d{2})-(\d{2})$/);
        return dateParts
            ? new Date(Number(dateParts[1]), Number(dateParts[2]) - 1, Number(dateParts[3]))
            : null;
    }

    function initializeInput(input) {
        if (!(input instanceof HTMLInputElement) || instances.has(input) || !window.mds?.MdsPersianDateTimePicker) return;

        const kind = input.type;
        const defaultValue = input.defaultValue;
        const originalId = input.id || `admin-date-${crypto.randomUUID()}`;
        const textId = `${originalId}-persian`;
        input.id = originalId;

        const textInput = document.createElement("input");
        textInput.type = "text";
        textInput.id = textId;
        textInput.className = input.className || "form-control";
        textInput.classList.add("admin-persian-date-input");
        textInput.autocomplete = "off";
        textInput.readOnly = true;
        textInput.dir = "ltr";
        textInput.setAttribute("aria-haspopup", "dialog");
        textInput.setAttribute("aria-label", input.getAttribute("aria-label") || input.labels?.[0]?.textContent?.trim() || "انتخاب تاریخ شمسی");
        textInput.required = input.required;
        textInput.disabled = input.disabled;
        textInput.placeholder = kind === "datetime-local" ? "انتخاب تاریخ و ساعت شمسی" : "انتخاب تاریخ شمسی";
        textInput.dataset.adminPersianDateInput = "true";

        for (const label of input.labels || []) {
            if (label.htmlFor === originalId) label.htmlFor = textId;
        }

        input.required = false;
        input.type = "hidden";
        input.dataset.adminPersianDateValue = "true";
        input.insertAdjacentElement("afterend", textInput);

        const selectedDate = parseValue(input.value, kind);
        const picker = new window.mds.MdsPersianDateTimePicker(textInput, {
            targetTextSelector: `#${CSS.escape(textId)}`,
            targetDateSelector: `#${CSS.escape(originalId)}`,
            isGregorian: false,
            persianNumber: true,
            enableTimePicker: kind === "datetime-local",
            textFormat: kind === "datetime-local" ? "yyyy/MM/dd HH:mm:ss" : "yyyy/MM/dd",
            dateFormat: kind === "datetime-local" ? "yyyy-MM-ddTHH:mm:ss" : "yyyy-MM-dd",
            selectedDate: selectedDate || undefined,
            selectedDateToShow: selectedDate || new Date(),
            onDayClick: () => input.dispatchEvent(new Event("change", { bubbles: true })),
            modalMode: false
        });

        const popoverState = { picker, textInput, shown: false, dismissWhenShown: false };
        textInput.addEventListener("show.bs.popover", () => {
            popoverState.shown = false;
            popoverState.dismissWhenShown = false;
            activePopover = popoverState;
        });
        textInput.addEventListener("shown.bs.popover", () => {
            popoverState.shown = true;
            if (popoverState.dismissWhenShown) picker.hide();
        });
        textInput.addEventListener("hidden.bs.popover", () => {
            if (activePopover === popoverState) activePopover = undefined;
        });

        // The library attaches its click handler after creating the popover.
        textInput.addEventListener("click", () => {
            if (!picker.getBsPopoverInstance()) window.setTimeout(() => {
                if (!textInput.disabled) picker.show();
            }, 200);
        });

        const state = { picker, textInput, kind, revision: 0 };
        instances.set(input, state);
        if (selectedDate) picker.setDate(selectedDate);
        input.form?.addEventListener("reset", () => {
            const revision = state.revision;
            window.setTimeout(() => {
                if (state.revision === revision) setValue(input, defaultValue);
            });
        });

        new MutationObserver(() => { textInput.disabled = input.disabled; }).observe(input, {
            attributes: true,
            attributeFilter: ["disabled"]
        });
    }

    function initialize(root = document) {
        if (root.matches?.(dateSelector)) initializeInput(root);
        root.querySelectorAll?.(dateSelector).forEach(initializeInput);
    }

    function setValue(input, value) {
        const state = instances.get(input);
        if (!state) return false;
        state.revision++;
        input.value = value || "";
        const date = parseValue(input.value, state.kind);
        if (date) state.picker.setDate(date);
        else {
            state.picker.clearDate();
            input.value = "";
        }
        return true;
    }

    window.AdminPersianDatePicker = Object.freeze({ initialize, setValue });
    initialize();
    observer = new MutationObserver(records => {
        for (const record of records) {
            for (const node of record.addedNodes) {
                if (node.nodeType === Node.ELEMENT_NODE) initialize(node);
            }
        }
    });
    observer.observe(document.documentElement, { childList: true, subtree: true });
})();
