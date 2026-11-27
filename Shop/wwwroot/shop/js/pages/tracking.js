window.HNUI.mount("pages-order-tracking", document, () => {
  const form = document.getElementById("trackingForm");
  if (!form) return;

  const status = document.getElementById("trackingStatus");
  const resultHost = document.getElementById("trackingResult");
  const button = form.querySelector('button[type="submit"]') || form.querySelector("button");

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
    return window.HNUI.formatDateTime(value);
  };

  const renderResult = (data) => {
    resultHost.replaceChildren();
    const heading = document.createElement("h3");
    heading.textContent = "وضعیت سفارش";
    const fields = document.createElement("div");
    fields.className = "tracking-result-fields";
    addField(fields, "شماره سفارش", data.orderNumber, "ltr");
    addField(fields, "وضعیت", data.statusName || data.status || "ثبت‌شده");
    addField(fields, "زمان ثبت", formatDate(data.createdAtUtc));
    resultHost.append(heading, fields);

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
