window.HNUI.mount("pages-order-tracking", document, () => {
  const form = document.getElementById("trackingForm");
  if (!form) return;

  const status = document.getElementById("trackingStatus");
  const resultHost = document.getElementById("trackingResult");
  const button = form.querySelector('button[type="submit"]') || form.querySelector("button");
  const labels = {
    "pending-payment": "در انتظار پرداخت",
    "warehouse-confirmed": "تأیید و آماده‌سازی سفارش",
    "in-transit": "در حال ارسال",
    "shipped-to-post": "تحویل به پست",
    delivered: "تحویل‌شده",
    cancelled: "لغوشده",
  };

  const setStatus = (message, type = "") => {
    if (status) window.HNUI.setMessage(status, message, type);
  };

  const addField = (parent, label, value, direction = "") => {
    if (!value) return;
    const row = document.createElement("div");
    row.className = "tracking-result-field";
    const name = document.createElement("span");
    name.textContent = label;
    const content = document.createElement("strong");
    content.textContent = value;
    if (direction) content.dir = direction;
    row.append(name, content);
    parent.append(row);
  };

  const formatDate = (value) => {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? ""
      : new Intl.DateTimeFormat("fa-IR", { dateStyle: "long", timeStyle: "short" }).format(date);
  };

  const renderResult = (data) => {
    resultHost.replaceChildren();
    const heading = document.createElement("h3");
    heading.textContent = "وضعیت سفارش";
    const fields = document.createElement("div");
    fields.className = "tracking-result-fields";
    addField(fields, "کد سفارش", data.orderNumber, "ltr");
    addField(fields, "وضعیت", labels[data.status] || data.status || "ثبت‌شده");
    addField(fields, "زمان ثبت", formatDate(data.orderedAtUtc));
    addField(fields, "شرکت حمل", data.shippingCarrier);
    addField(fields, "کد رهگیری", data.shippingTrackingCode, "ltr");
    resultHost.append(heading, fields);

    const events = Array.isArray(data.statusHistory) ? data.statusHistory : [];
    if (events.length) {
      const historyHeading = document.createElement("h4");
      historyHeading.textContent = "تاریخچهٔ وضعیت ارسال";
      const list = document.createElement("ol");
      list.className = "tracking-history-list";
      events.forEach((item) => {
        const entry = document.createElement("li");
        const state = document.createElement("strong");
        state.textContent = labels[item.newStatus] || item.newStatus || "به‌روزرسانی سفارش";
        const time = document.createElement("time");
        time.textContent = formatDate(item.changedAtUtc);
        entry.append(state, time);
        list.append(entry);
      });
      resultHost.append(historyHeading, list);
    }

    const trackingEvents = Array.isArray(data.trackingHistory) ? data.trackingHistory : [];
    if (trackingEvents.length) {
      const historyHeading = document.createElement("h4");
      historyHeading.textContent = "تغییرهای کد رهگیری";
      const list = document.createElement("ol");
      list.className = "tracking-history-list";
      trackingEvents.forEach((item) => {
        const entry = document.createElement("li");
        const details = document.createElement("strong");
        details.textContent = `${item.carrier}: ${item.trackingCode}`;
        details.dir = "auto";
        const time = document.createElement("time");
        time.textContent = formatDate(item.changedAtUtc);
        entry.append(details, time);
        list.append(entry);
      });
      resultHost.append(historyHeading, list);
    }
    resultHost.hidden = false;
  };

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    resultHost.hidden = true;
    setStatus("در حال بررسی سفارش…", "info");
    button.disabled = true;
    try {
      const response = await window.HNUI.api.request("store/orders/tracking", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          orderNumber: form.elements.order.value.trim(),
          phone: window.HNUI.toEnglishDigits(form.elements.phone.value).replace(/\D/g, ""),
        }),
      });
      const data = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(data.message || "سفارش پیدا نشد یا امکان بررسی آن وجود ندارد.");
      renderResult(data);
      setStatus("اطلاعات سفارش پیدا شد.", "success");
    } catch (error) {
      setStatus(error.message || "ارتباط با سرویس پیگیری برقرار نشد.", "error");
    } finally {
      button.disabled = false;
    }
  });
});
