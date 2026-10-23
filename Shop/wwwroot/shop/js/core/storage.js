(function (window) {
  "use strict";
  if (window.HNStorage) return;
  window.HNStorage = Object.freeze({ create({ document, emit }) {
  const contracts = Object.freeze({
    "hadi-norouzi-cart": "array", "hn-favorites": "array", "hn-compare": "array",
    "hn-delivery-addresses": "array", "hn-profile": "object",
    "hn-product-cache": "object", "hn-header-recent-searches": "array",
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
  const storage = {
    get(key, fallback = null) {
      try { return decode(key, window.localStorage.getItem(key)).value ?? fallback; }
      catch { return fallback; }
    },
    set(key, value) {
      try {
        const raw = window.localStorage.getItem(key);
        const previous = decode(key, raw);
        if (!previous.supported) return false; // A newer application owns this record.
        let encoded = String(value);
        if (contractFor(key)) {
          const normalized = decode(key, encoded);
          if (normalized.value === null || !normalized.supported) return false;
          encoded = JSON.stringify({schemaVersion: 1, value: JSON.parse(normalized.value)});
          let versioned = false;
          try { versioned = Object.hasOwn(JSON.parse(raw || "null") || {}, "schemaVersion"); } catch {}
          if (raw !== null && !versioned) {
            const backupKey = "hn-migration-backup:" + key;
            if (window.localStorage.getItem(backupKey) === null) window.localStorage.setItem(backupKey, raw);
          }
        }
        window.localStorage.setItem(key, encoded);
        if (key === "hn-favorites" || key === "hn-compare") emit("hn:lists-change");
        return true;
      } catch { return false; }
    },
    remove(key) {
      try {
        const previous = decode(key, window.localStorage.getItem(key));
        if (!previous.supported) return false;
        window.localStorage.removeItem(key);
        if (key === "hn-favorites" || key === "hn-compare") emit("hn:lists-change");
        return true;
      } catch { return false; }
    },
    json(key, fallback = null) {
      try { return JSON.parse(this.get(key, "null")) ?? fallback; } catch { return fallback; }
    },
    writable(key) {
      try { return decode(key, window.localStorage.getItem(key)).supported; } catch { return false; }
    },
  };
    return Object.freeze(storage);
  }});
})(window);
