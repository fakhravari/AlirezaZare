(() => {
    "use strict";
    const render = (item, ui) => {
        const references = item.priceReferences || [];
        if (!references.length) return null;
        const amount = value => `${ui.formatNumber(Number(value) || 0)} تومان`;
        const components = document.createElement("ul");
        components.className = "order-price-components";
        components.setAttribute("aria-label", "اجزای مبلغ واحد");
        const labels = { product: "قیمت کالا", accessory: "متعلقه", variant: "قیمت تنوع" };
        for (const reference of references) {
            const component = document.createElement("li");
            const heading = document.createElement("div"); heading.className = "order-price-components__heading";
            const label = document.createElement("span"); label.textContent = `${labels[reference.component] || "قیمت"}${reference.component === "accessory" && reference.name ? `: ${reference.name}` : ""}`;
            const price = document.createElement("strong"); price.textContent = amount(reference.calculatedUnitAmount);
            heading.append(label, price); component.append(heading);
            const line = document.createElement("div"); line.className = "order-price-components__line";
            line.textContent = `مبلغ واحد: ${amount(reference.calculatedUnitAmount)} × ${ui.formatNumber(item.quantity)} = ${amount(Number(reference.calculatedUnitAmount) * Number(item.quantity))}`;
            component.append(line);
            if (reference.priceBeforeDiscountAmount != null && reference.discountPercent != null) {
                const before = Number(reference.priceBeforeDiscountAmount), final = Number(reference.calculatedUnitAmount);
                if (Number.isFinite(before) && Number.isFinite(final)) {
                    const breakdown = document.createElement("div"); breakdown.className = "order-price-components__breakdown";
                    const original = document.createElement("span"); original.textContent = `پیش از تخفیف: ${amount(before)}`;
                    const discount = document.createElement("span"); discount.textContent = `تخفیف ${ui.formatNumber(reference.discountPercent)}٪: ${amount(before - final)}`;
                    breakdown.append(original, discount); component.append(breakdown);
                }
            }
            components.append(component);
        }
        return components;
    };
    window.HNOrderPriceComponents = Object.freeze({ render });
})();
