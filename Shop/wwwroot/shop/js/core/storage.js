(function (window) {
  "use strict";
  if (window.HNStorage) return;

  window.HNStorage = Object.freeze({ create({ emit }) {
    const contracts = Object.freeze({
      "hadi-norouzi-cart": "array", "hn-favorites": "array", "hn-compare": "array",
      "hn-delivery-addresses": "array", "hn-profile": "object",
      "hn-header-recent-searches": "array",
    });
    const contractFor = (key) => contracts[key] || (/^hn-product-(?:reviews|questions)-[1-9]\d*$/.test(key) ? "array" : null);
    const decode = (key, raw) => {
      if (raw === null || !contractFor(key)) return { value: raw, supported: true };
      try {
        let value = JSON.parse(raw);
        if (value && !Array.isArray(value) && Object.hasOwn(value, "schemaVersion")) {
          if (value.schemaVersion !== 1) return { value: null, supported: false };
          value = value.value;
        }
        const valid = contractFor(key) === "array" ? Array.isArray(value)
          : value && typeof value === "object" && !Array.isArray(value);
        return { value: valid ? JSON.stringify(value) : null, supported: true };
      } catch { return { value: null, supported: true }; }
    };

    // Keep old Shop data in memory just long enough for account bootstrap to submit
    // it to the API, then remove only Shop-owned keys from browser storage.
    const values = new Map();
    const legacy = new Map();
    try {
      for (let index = window.localStorage.length - 1; index >= 0; index -= 1) {
        const key = window.localStorage.key(index);
        if (key === null || !(key.startsWith("hn-") || key.startsWith("hadi-norouzi-"))) continue;
        const raw = window.localStorage.getItem(key);
        if (key === "hn-product-cache") {
          window.localStorage.removeItem(key);
          continue;
        }
        values.set(key, raw);
        legacy.set(key, raw);
        window.localStorage.removeItem(key);
      }
    } catch { /* Storage may be disabled; API-backed state remains available. */ }
    values.delete("hn-demo-auth");
    legacy.delete("hn-demo-auth");

    const storage = {
      get(key, fallback = null) {
        const result = decode(key, values.get(key) ?? null);
        return result.value ?? fallback;
      },
      set(key, value) {
        const raw = String(value);
        const normalized = decode(key, raw);
        if (!normalized.supported || (contractFor(key) && normalized.value === null)) return false;
        values.set(key, contractFor(key) ? normalized.value : raw);
        if (key === "hn-favorites" || key === "hn-compare") emit("hn:lists-change");
        if (key === "hn-price-watches-demo") emit("hn:account-data-change", { key });
        return true;
      },
      remove(key) {
        values.delete(key);
        return true;
      },
      json(key, fallback = null) {
        try { return JSON.parse(this.get(key, "null")) ?? fallback; } catch { return fallback; }
      },
      writable() { return true; },
      legacy(key) { return legacy.get(key) ?? null; },
      legacyKeys() { return [...legacy.keys()]; },
      clearLegacy(key) { legacy.delete(key); },
      clear() { values.clear(); legacy.clear(); },
    };
    return Object.freeze(storage);
  }});
})(window);
