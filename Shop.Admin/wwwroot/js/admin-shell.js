(() => {
    "use strict";

    if (window.App && typeof window.App.settings === "function") {
        window.App.settings({ disableLocalStorage: true, disableSidebarScrollMemory: true });
    }

    const content = document.getElementById("content");
    const sidebar = document.getElementById("sidebar");
    if (!content) return;

    let navigationRequest;

    function setActiveNavigation(target) {
        if (!sidebar) return;
        sidebar.querySelectorAll(".menu-item.active").forEach(item => item.classList.remove("active", "expand"));
        sidebar.querySelectorAll(".menu-link[aria-expanded='true']").forEach(link => link.setAttribute("aria-expanded", "false"));
        const targetUrl = new URL(target.href);
        const link = [...sidebar.querySelectorAll("a[data-admin-nav]")].find(item => {
            const itemUrl = new URL(item.href);
            return itemUrl.pathname.toLowerCase() === targetUrl.pathname.toLowerCase() && itemUrl.search === targetUrl.search;
        });
        const item = link?.closest(".menu-item");
        item?.classList.add("active");
        const group = item?.parentElement?.closest(".menu-item.has-sub");
        if (group) {
            group.classList.add("active", "expand");
            group.querySelector(":scope > .menu-link")?.setAttribute("aria-expanded", "true");
        }
    }

    async function navigate(href, pushState = true) {
        const target = new URL(href, window.location.href);
        if (target.origin !== window.location.origin) return;
        navigationRequest?.abort();
        navigationRequest = new AbortController();
        const controller = navigationRequest;
        content.setAttribute("aria-busy", "true");
        try {
            const response = await fetch(target, {
                method: "GET", cache: "no-store", signal: controller.signal,
                headers: { Accept: "text/html", "X-Requested-With": "XMLHttpRequest" }
            });
            const html = await response.text();
            if (!response.ok) throw new Error(`بارگذاری صفحه با خطای ${response.status} روبه‌رو شد.`);
            const nextDocument = new DOMParser().parseFromString(html, "text/html");
            const nextContent = nextDocument.getElementById("content");
            if (!nextContent) throw new Error("ساختار صفحهٔ دریافت‌شده معتبر نیست.");
            content.innerHTML = nextContent.innerHTML;
            document.title = nextDocument.title;
            if (pushState) history.pushState({ admin: true }, "", target.href);
            setActiveNavigation(target);
            window.AdminPages?.initialize(content);
            const heading = content.querySelector("h1");
            if (heading) {
                heading.tabIndex = -1;
                heading.focus({ preventScroll: true });
            } else content.focus({ preventScroll: true });
        } catch (error) {
            if (error.name === "AbortError") return;
            const alert = document.createElement("div");
            alert.className = "alert alert-danger";
            alert.setAttribute("role", "alert");
            alert.textContent = error.message || "بارگذاری صفحه ناموفق بود.";
            content.replaceChildren(alert);
            window.AdminUI?.notify(alert.textContent, "danger");
        } finally {
            if (navigationRequest === controller) {
                content.removeAttribute("aria-busy");
                navigationRequest = null;
            }
        }
    }

    document.addEventListener("click", event => {
        const link = event.target.closest("a[data-admin-nav]");
        if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || link.target === "_blank") return;
        event.preventDefault();
        void navigate(link.href);
    });

    window.addEventListener("popstate", () => void navigate(location.href, false));
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => window.AdminPages?.initialize(content), { once: true });
    } else window.AdminPages?.initialize(content);
})();
