(() => {
    "use strict";

    const ui = window.AdminUI;
    const initialized = new WeakSet();
    const number = new Intl.NumberFormat("fa-IR");
    const pageSize = 50;

    function initialize(root) {
        const page = root.querySelector?.("[data-admin-newsletter-subscribers]");
        if (!page || initialized.has(page) || !ui) return;
        initialized.add(page);

        let pageNumber = 1;
        let requestId = 0;

        const token = () => page.querySelector('[name="__RequestVerificationToken"]')?.value || "";
        const setStatus = (message, isError = false) => {
            const target = page.querySelector("[data-newsletter-load-status]");
            target.textContent = message;
            target.classList.toggle("text-danger", isError);
            target.classList.toggle("text-muted", !isError);
        };

        async function load() {
            const currentRequest = ++requestId;
            const query = new URLSearchParams(new FormData(page.querySelector("[data-newsletter-filter]")));
            query.set("page", String(pageNumber));
            query.set("pageSize", String(pageSize));
            setStatus("در حال دریافت اعضا…");

            try {
                const result = await ui.getJson(`${page.dataset.api}?${query}`);
                if (currentRequest !== requestId) return;

                const body = page.querySelector("[data-newsletter-rows]");
                body.replaceChildren();
                for (const item of result.items || []) {
                    const row = document.createElement("tr");
                    const cells = [
                        item.email,
                        window.AdminUI.formatDateTime(item.subscribedAtUtc),
                        item.unsubscribedAtUtc ? "لغوشده" : "فعال"
                    ];
                    for (const value of cells) {
                        const cell = document.createElement("td");
                        cell.textContent = value ?? "";
                        row.append(cell);
                    }

                    const actionCell = document.createElement("td");
                    const button = document.createElement("button");
                    const isSubscribed = !item.unsubscribedAtUtc;
                    button.type = "button";
                    button.className = "btn btn-sm btn-default";
                    button.textContent = isSubscribed ? "لغو عضویت" : "فعال‌سازی";
                    button.dataset.adminConfirm = isSubscribed
                        ? "عضویت این نشانی لغو شود؟"
                        : "عضویت این نشانی دوباره فعال شود؟";
                    button.addEventListener("click", async () => {
                        button.disabled = true;
                        try {
                            await ui.getJson(`${page.dataset.api}/${item.id}/status`, {
                                method: "PUT",
                                headers: { "Content-Type": "application/json", RequestVerificationToken: token() },
                                body: JSON.stringify({ subscribed: !isSubscribed })
                            });
                            await load();
                        } catch (error) {
                            ui.notify(error.message || "تغییر وضعیت عضویت انجام نشد.", "danger");
                            button.disabled = false;
                        }
                    });
                    actionCell.append(button);
                    row.append(actionCell);
                    body.append(row);
                }

                page.querySelector("[data-newsletter-status]").textContent = `${number.format(result.total || 0)} عضو`;
                const hasNext = result.hasNext === true;
                const totalPages = Number.isFinite(result.total) ? Math.ceil(result.total / (result.pageSize || pageSize)) : pageNumber + (hasNext ? 1 : 0);
                ui.renderNumberedPagination(page.querySelector("[data-newsletter-pagination]"), pageNumber, totalPages,
                    target => { pageNumber = target; void load(); }, "صفحه‌بندی اعضای خبرنامه");
                setStatus(result.items?.length ? "" : "عضوی با این فیلتر پیدا نشد.");

                const exportQuery = new URLSearchParams(new FormData(page.querySelector("[data-newsletter-filter]")));
                if (exportQuery.get("includeUnsubscribed") === "true") exportQuery.set("includeUnsubscribed", "true");
                page.querySelector("[data-newsletter-export]").href = `${page.dataset.api}/export?${exportQuery}`;
            } catch (error) {
                if (currentRequest === requestId)
                    setStatus(error.message || "دریافت اعضای خبرنامه ناموفق بود.", true);
            }
        }

        page.querySelector("[data-newsletter-filter]").addEventListener("submit", event => {
            event.preventDefault();
            pageNumber = 1;
            void load();
        });
        void load();
    }

    window.AdminNewsletterSubscribers = Object.freeze({ initialize });
})();
