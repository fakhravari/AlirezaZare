(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), number = new Intl.NumberFormat("fa-IR");
    if (!ui) return;
    const cell = (value) => { const node = document.createElement("td"); node.textContent = value ?? "—"; return node; };
    const token = (page) => page.querySelector('[name="__RequestVerificationToken"]')?.value || "";
    const status = (page, message, error = false) => {
        const node = page.querySelector("[data-price-status]"); node.textContent = message;
        node.classList.toggle("is-error", error); node.classList.toggle("d-none", !message);
    };
    async function request(page, url, method = "GET", body) {
        return ui.getJson(url, { method, headers: { ...(body ? { "Content-Type": "application/json" } : {}), ...(method !== "GET" ? { RequestVerificationToken: token(page) } : {}) }, ...(body ? { body: JSON.stringify(body) } : {}) });
    }
    function render(page) {
        const query = page.querySelector("[data-price-filter] input").value.trim().toLocaleLowerCase();
        const rows = page._variantPriceRows.filter((item) => !query || [item.productName, item.variantName, item.salesPlanName, item.code].some((value) => String(value || "").toLocaleLowerCase().includes(query)));
        const body = page.querySelector("[data-price-rows]"); body.replaceChildren();
        for (const item of rows) {
            const row = document.createElement("tr");
            row.append(cell(item.productName), cell(item.variantName), cell(item.salesPlanName));
            const amountCell = document.createElement("td"), amount = document.createElement("input");
            amount.type = "number"; amount.min = "0"; amount.step = "1"; amount.className = "form-control form-control-sm"; amount.inputMode = "numeric";
            amount.setAttribute("aria-label", `قیمت ${item.productName}، ${item.variantName}، ${item.salesPlanName}`);
            amount.value = item.priceAmount == null ? "" : String(item.priceAmount); amount.disabled = page.dataset.canEdit !== "true"; amountCell.append(amount); row.append(amountCell);
            const discountCell = document.createElement("td"), discount = document.createElement("input");
            discount.type = "number"; discount.min = "0"; discount.max = "99.99"; discount.step = "0.01"; discount.className = "form-control form-control-sm";
            discount.setAttribute("aria-label", `درصد تخفیف ${item.productName}، ${item.variantName}، ${item.salesPlanName}`);
            discount.value = item.discountPercent == null ? "0" : String(item.discountPercent); discount.disabled = page.dataset.canEdit !== "true"; discountCell.append(discount); row.append(discountCell);
            const activeCell = document.createElement("td"), activeLabel = document.createElement("label"), active = document.createElement("input"), activeText = document.createElement("span");
            active.type = "checkbox"; active.checked = item.isEnabled !== false; active.disabled = page.dataset.canEdit !== "true"; activeText.textContent = " فعال"; activeLabel.append(active, activeText); activeCell.append(activeLabel); row.append(activeCell);
            const actions = document.createElement("td"), save = document.createElement("button"); save.type = "button"; save.className = "btn btn-sm btn-primary"; save.textContent = "ذخیره"; save.disabled = page.dataset.canEdit !== "true";
            save.addEventListener("click", async () => {
                try {
                    if (!amount.value.trim()) {
                        await request(page, `${page.dataset.api}/${item.variantId}/${encodeURIComponent(item.salesPlanId)}`, "DELETE");
                    } else {
                        const priceAmount = Number(amount.value), discountPercent = Number(discount.value || 0);
                        if (!Number.isSafeInteger(priceAmount) || priceAmount < 0 || !Number.isFinite(discountPercent) || discountPercent < 0 || discountPercent >= 100) throw new Error("قیمت صحیح و تخفیف کمتر از ۱۰۰ وارد کنید.");
                        await request(page, `${page.dataset.api}/${item.variantId}/${encodeURIComponent(item.salesPlanId)}`, "PUT", { priceAmount, discountPercent, isEnabled: active.checked });
                    }
                    ui.notify("قیمت طرح ذخیره شد.", "success"); await load(page);
                } catch (error) { ui.notify(error.message || "ذخیرهٔ قیمت ناموفق بود.", "danger"); }
            });
            actions.append(save);
            if (item.priceAmount != null) {
                const clear = document.createElement("button"); clear.type = "button"; clear.className = "btn btn-sm btn-default ms-1"; clear.textContent = "قیمت پایه"; clear.disabled = page.dataset.canEdit !== "true";
                clear.addEventListener("click", async () => { try { await request(page, `${page.dataset.api}/${item.variantId}/${encodeURIComponent(item.salesPlanId)}`, "DELETE"); ui.notify("قیمت اختصاصی حذف شد.", "success"); await load(page); } catch (error) { ui.notify(error.message || "حذف قیمت ناموفق بود.", "danger"); } }); actions.append(clear);
            }
            row.append(actions); body.append(row);
        }
        page.querySelector("[data-price-count]").textContent = `${number.format(rows.length)} ردیف`;
        if (!rows.length) status(page, "موردی با این جست‌وجو پیدا نشد."); else status(page, "");
    }
    async function load(page) {
        status(page, "در حال دریافت قیمت‌ها…");
        try { const result = await request(page, page.dataset.api); page._variantPriceRows = Array.isArray(result) ? result : []; render(page); }
        catch (error) { status(page, error.message || "دریافت قیمت‌ها ناموفق بود.", true); }
    }
    function initialize(root) {
        const page = root.querySelector?.("[data-store-variant-prices]"); if (!page || initialized.has(page)) return; initialized.add(page);
        page.querySelector("[data-price-filter]").addEventListener("input", () => render(page));
        page.querySelector("[data-price-refresh]").addEventListener("click", () => void load(page));
        void load(page);
    }
    window.AdminVariantPrices = Object.freeze({ initialize });
})();
