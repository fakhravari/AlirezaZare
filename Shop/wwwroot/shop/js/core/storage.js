(function (window) {
  "use strict";
  if (window.HNStorage) return;
  window.HNStorage = Object.freeze({ create({ document, emit }) {
  const PROFILE_ACTIVITY_KEY = "hn-profile-activity";

  const recordProfileActivity = (key, previous, next) => {
    if (previous === next) return;

    const eventMap = {
      "hn-favorites": {
        title: "فهرست علاقه‌مندی‌ها به‌روز شد",
        icon: "bi-heart",
        label: "محصول ذخیره‌شده",
      },
      "hn-compare": {
        title: "فهرست مقایسه به‌روز شد",
        icon: "bi-arrow-left-right",
        label: "محصول در مقایسه",
      },
      "hn-delivery-addresses": {
        title: "آدرس‌های حساب به‌روز شد",
        icon: "bi-geo-alt",
        label: "آدرس ذخیره‌شده",
      },
    };
    let event = eventMap[key];
    let detail = "";

    if (event) {
      const parseList = (value) => {
        try {
          const parsed = JSON.parse(value || "[]");
          return Array.isArray(parsed) ? parsed : [];
        } catch {
          return [];
        }
      };
      const currentItems = parseList(next);

      // The comparison page seeds a sample list on first visit. Keep that setup
      // out of the user's activity history.
      if (key === "hn-compare" && previous === null && currentItems.length > 1)
        return;

      detail = `${currentItems.length} ${event.label}`;
    } else if (key === "hn-demo-coupon" && next) {
      event = { title: "کد تخفیف در سبد خرید اعمال شد", icon: "bi-ticket-perforated" };
      detail = `کد ${String(next)}`;
    } else if (key === "hn-demo-coupon" && previous) {
      event = { title: "کد تخفیف از سبد خرید حذف شد", icon: "bi-ticket-perforated" };
      detail = `کد ${String(previous)}`;
    }

    if (!event) return;

    try {
      const saved = storage.json(PROFILE_ACTIVITY_KEY, []);
      const history = Array.isArray(saved) ? saved : [];
      history.unshift({
        title: event.title,
        detail,
        icon: event.icon,
        at: new Date().toISOString(),
      });
      if (storage.set(PROFILE_ACTIVITY_KEY, JSON.stringify(history.slice(0, 20))))
        emit("hn:profile-activity-change");
    } catch {
      // Activity history is optional; the underlying account action must still work.
    }
  };

  const contracts = Object.freeze({
    "hadi-norouzi-cart": "array", "hn-favorites": "array", "hn-compare": "array",
    "hn-delivery-addresses": "array", "hn-profile": "object", "hn-profile-activity": "array",
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
        recordProfileActivity(key, previous.value, String(value));
        if (key === "hn-favorites" || key === "hn-compare") emit("hn:lists-change");
        return true;
      } catch { return false; }
    },
    remove(key) {
      try {
        const previous = decode(key, window.localStorage.getItem(key));
        if (!previous.supported) return false;
        window.localStorage.removeItem(key);
        recordProfileActivity(key, previous.value, null);
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
