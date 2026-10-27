(() => {
    "use strict";
    const read = (id) => {
        const element = document.getElementById(id);
        if (!element) return null;
        try {
            const data = JSON.parse(element.textContent);
            return data?.schemaVersion === 1 ? data : null;
        } catch { return null; }
    };
    const freeze = (value) => {
        if (value && typeof value === "object") {
            Object.values(value).forEach(freeze);
            Object.freeze(value);
        }
        return value;
    };
    const commerce = read("commerceData");
    const account = read("accountData");
    const offers = commerce?.currency === "IRT" && commerce.couponOffers &&
        typeof commerce.couponOffers === "object" && !Array.isArray(commerce.couponOffers)
        ? Object.fromEntries(Object.entries(commerce.couponOffers).filter(([code, percent]) =>
            /^[A-Z0-9]+$/.test(code) && Number.isInteger(percent) && percent > 0 && percent < 100))
        : {};
    window.HNAccountData = freeze({
        couponOffers: offers,
        coupons: Array.isArray(account?.coupons) ? account.coupons : [],
    });
})();
