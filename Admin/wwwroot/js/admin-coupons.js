(() => {
    "use strict";

    let root = document.getElementById("coupon-page");

    window.AdminCoupons = {
        initialize(container) {
            root = container?.querySelector("#coupon-page") || (container?.id === "coupon-page" ? container : null);
        }
    };

    let activeRequest;
    let requestNumber = 0;

    const showMessage = (message, isError = false) => {
        if (!root) return;
        const feedback = root.querySelector("#coupon-feedback");
        if (!feedback) return;
        feedback.replaceChildren();
        const alert = document.createElement("div");
        alert.className = `alert ${isError ? "alert-danger" : "alert-success"}`;
        alert.setAttribute("role", isError ? "alert" : "status");
        alert.textContent = message;
        feedback.append(alert);
    };

    const showFormError = (form, message) => {
        const errorBox = form.querySelector("[data-coupon-form-error]");
        if (!errorBox) {
            showMessage(message, true);
            return;
        }
        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
        errorBox.scrollIntoView({ block: "nearest" });
    };

    document.addEventListener("invalid", event => {
        const control = event.target;
        const form = control instanceof HTMLElement ? control.closest("form[data-coupon-mutation]") : null;
        if (!root || !form || !root.contains(form)) return;
        event.preventDefault();
        showFormError(form, control.validationMessage || "اطلاعات واردشده را بررسی کنید.");
        control.focus();
    }, true);

    document.addEventListener("input", event => {
        const control = event.target;
        const form = control instanceof HTMLElement ? control.closest("form[data-coupon-mutation]") : null;
        if (!root || !form || !root.contains(form)) return;
        const errorBox = form.querySelector("[data-coupon-form-error]");
        if (errorBox && control.checkValidity()) errorBox.classList.add("d-none");
    });

    const loadContent = async (url, { updateHistory = false, message = "" } = {}) => {
        if (!root) return;
        activeRequest?.abort();
        activeRequest = new AbortController();
        const currentRequest = ++requestNumber;
        root.setAttribute("aria-busy", "true");
        try {
            const response = await fetch(url, {
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "text/html" },
                signal: activeRequest.signal
            });
            if (!response.ok) throw new Error("بارگذاری فهرست کدهای تخفیف انجام نشد.");
            const html = await response.text();
            const documentFragment = new DOMParser().parseFromString(html, "text/html");
            const nextContent = documentFragment.querySelector("#coupon-content");
            const currentContent = root.querySelector("#coupon-content");
            if (!nextContent || !currentContent || currentRequest !== requestNumber) throw new Error("پاسخ فهرست معتبر نیست.");
            currentContent.replaceWith(nextContent);
            if (updateHistory) history.pushState({}, "", url);
            if (message) showMessage(message);
        } catch (error) {
            if (error.name !== "AbortError") showMessage(error.message || "ارتباط با سرور برقرار نشد.", true);
        } finally {
            if (currentRequest === requestNumber) root.removeAttribute("aria-busy");
        }
    };

    const hideModal = async form => {
        const modalElement = form.closest(".modal");
        const modalApi = window.bootstrap?.Modal;
        if (!modalElement || !modalApi || !modalElement.classList.contains("show")) return;
        await new Promise(resolve => {
            modalElement.addEventListener("hidden.bs.modal", resolve, { once: true });
            modalApi.getOrCreateInstance(modalElement).hide();
            window.setTimeout(resolve, 500);
        });
    };

    const loadReport = async url => {
        const modalElement = root.querySelector("#coupon-report-modal");
        const body = modalElement?.querySelector("#coupon-report-body");
        if (!modalElement || !body) return;
        body.textContent = "در حال بارگذاری گزارش…";
        window.bootstrap?.Modal.getOrCreateInstance(modalElement).show();
        try {
            const response = await fetch(url, {
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "text/html" }
            });
            if (!response.ok) throw new Error("بارگذاری گزارش مصرف کد تخفیف انجام نشد.");
            const documentFragment = new DOMParser().parseFromString(await response.text(), "text/html");
            const report = documentFragment.querySelector("#coupon-report-content");
            if (!report) throw new Error("پاسخ گزارش معتبر نیست.");
            body.replaceChildren(report);
            const title = modalElement.querySelector("#coupon-report-title");
            if (title) title.textContent = `گزارش مصرف کد تخفیف ${report.dataset.couponReportCode || ""}`;
        } catch (error) {
            const alert = document.createElement("div");
            alert.className = "alert alert-danger mb-0";
            alert.setAttribute("role", "alert");
            alert.textContent = error.message || "ارتباط با سرور برقرار نشد.";
            body.replaceChildren(alert);
        }
    };

    document.addEventListener("submit", async event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !root || !root.contains(form)) return;

        if (form.matches("[data-coupon-filter]")) {
            event.preventDefault();
            const params = new URLSearchParams(new FormData(form));
            const submitter = event.submitter;
            if (submitter?.name) params.set(submitter.name, submitter.value);
            const url = new URL(form.action, window.location.href);
            url.search = params.toString();
            void loadContent(url, { updateHistory: true });
            return;
        }

        if (!form.matches("[data-coupon-mutation]")) return;
        event.preventDefault();
        const expiryField = form.querySelector('[name="expiresOn"]');
        if (expiryField && !expiryField.value) {
            showFormError(form, "تاریخ انقضا را انتخاب کنید.");
            form.querySelector(`#${CSS.escape(expiryField.id)}-persian`)?.focus();
            return;
        }
        form.querySelector("[data-coupon-form-error]")?.classList.add("d-none");
        root.setAttribute("aria-busy", "true");
        try {
            const response = await fetch(form.action, {
                method: (form.method || "post").toUpperCase(),
                body: new FormData(form),
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json" }
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                showFormError(form, result.message || "ذخیرهٔ تغییر انجام نشد.");
                root.removeAttribute("aria-busy");
                return;
            }
            await hideModal(form);
            await loadContent(window.location.href, { message: result.message });
        } catch {
            showFormError(form, "ارتباط با سرور برقرار نشد؛ تغییر ثبت نشد.");
            root.removeAttribute("aria-busy");
        }
    });

    document.addEventListener("click", event => {
        const reportButton = event.target.closest("[data-coupon-report]");
        if (reportButton && root?.contains(reportButton)) {
            void loadReport(reportButton.dataset.couponReport);
            return;
        }
        const reportPageButton = event.target.closest("[data-coupon-report-page]");
        if (reportPageButton && root?.contains(reportPageButton)) {
            void loadReport(reportPageButton.dataset.couponReportPage);
            return;
        }
        const clearLink = event.target.closest("[data-coupon-clear]");
        if (clearLink && root?.contains(clearLink)) {
            event.preventDefault();
            void loadContent(clearLink.href, { updateHistory: true });
            return;
        }
    });

    document.addEventListener("change", event => {
        const field = event.target;
        if (field instanceof HTMLInputElement && field.name === "expiresOn" && field.value) {
            const form = field.closest("form");
            const errorBox = form?.querySelector("[data-coupon-form-error]");
            if (errorBox?.textContent === "تاریخ انقضا را انتخاب کنید.") errorBox.classList.add("d-none");
        }
    });

    window.addEventListener("popstate", () => void loadContent(window.location.href));
})();
