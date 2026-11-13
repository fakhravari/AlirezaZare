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
    const account = read("accountData");
    window.HNAccountData = freeze({
        coupons: Array.isArray(account?.coupons) ? account.coupons : [],
    });
})();
