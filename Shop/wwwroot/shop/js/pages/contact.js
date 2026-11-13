window.HNUI.mount("pages-contact", document, () => {
  const form = document.getElementById("contactForm");
  if (!form) return;

  const ui = window.HNUI;
  const status = document.getElementById("contactStatus");
  const fields = {
    name: form.querySelector("#contactName"),
    phone: form.querySelector("#contactPhone"),
    subject: form.querySelector("#contactSubject"),
    message: form.querySelector("#contactMessage"),
  };
  const rules = [
    [
      fields.name,
      "نام و نام خانوادگی را وارد کنید.",
      (value) => value.length >= 3 || "نام باید حداقل ۳ حرف داشته باشد.",
    ],
    [
      fields.phone,
      "شماره موبایل را وارد کنید.",
      (value) =>
        /^09\d{9}$/.test(ui.toEnglishDigits(value).replace(/\D/g, "")) ||
        "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.",
    ],
    [
      fields.subject,
      "موضوع درخواست را انتخاب کنید.",
      (value) => Boolean(value),
    ],
    [
      fields.message,
      "متن پیام را وارد کنید.",
      (value) => value.length >= 10 || "متن پیام باید حداقل ۱۰ حرف داشته باشد.",
    ],
  ];

  const errorHost = (input) => document.getElementById(`${input.id}Error`);
  const setError = (input, message) => {
    const field = input.closest(".field");
    const host = errorHost(input);
    const invalid = Boolean(message);
    field.classList.toggle("is-invalid", invalid);
    field.classList.toggle("is-valid", !invalid && Boolean(input.value.trim()));
    input.setAttribute("aria-invalid", String(invalid));
    if (host) {
      host.textContent = message || "";
      host.className = invalid
        ? "field-validation-error"
        : "field-validation-valid";
      window.HNUI.setMessageType(host, invalid ? "error" : "");
    }
    return !invalid;
  };

  const validate = (input, show = true) => {
    const rule = rules.find((item) => item[0] === input);
    if (!rule) return true;
    const value = input.value.trim();
    let message = value ? "" : rule[1];
    if (!message && typeof rule[2] === "function") {
      const result = rule[2](value);
      if (result !== true) message = result;
    }
    return show ? setError(input, message) : !message;
  };

  const showStatus = (message, type) => {
    if (!status) return;
    status.hidden = false;
    status.className = `contact-form-status${type === "error" ? " is-error" : ""}`;
    ui.setMessage(status, message, type);
  };

  fields.phone.addEventListener("input", () => {
    const value = ui
      .toEnglishDigits(fields.phone.value)
      .replace(/\D/g, "")
      .slice(0, 11);
    if (fields.phone.value !== value) fields.phone.value = value;
    if (fields.phone.getAttribute("aria-invalid") === "true")
      validate(fields.phone);
  });
  rules.forEach(([input]) => {
    input.addEventListener(input.tagName === "SELECT" ? "change" : "blur", () =>
      validate(input),
    );
    input.addEventListener("input", () => {
      if (input.getAttribute("aria-invalid") === "true") validate(input);
    });
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const invalid = rules
      .map(([input]) => input)
      .filter((input) => !validate(input));
    if (invalid.length) {
      if (status) {
        showStatus("ارسال انجام نشد. موارد مشخص‌شده را کامل و اصلاح کنید.", "error");
      }
      invalid[0].focus();
      invalid[0]
        .closest(".field")
        ?.scrollIntoView({ behavior: "smooth", block: "center" });
      return;
    }
    const confirmed = await window.confirmFormSubmission({
      title: "بررسی اطلاعات واردشده",
      message: "پیام شما در صندوق پشتیبانی ثبت می‌شود. اطلاعات واردشده را بررسی کنید.",
      confirmText: "ارسال پیام",
    });
    if (!confirmed) return;
    const button = form.querySelector('button[type="submit"]');
    const original = button.innerHTML;
    button.disabled = true;
    button.textContent = "در حال ارسال…";
    try {
      const response = await ui.api.request("store/contact", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: fields.name.value.trim(),
          phone: ui.toEnglishDigits(fields.phone.value).replace(/\D/g, ""),
          subject: fields.subject.value,
          message: fields.message.value.trim(),
        }),
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.message || "ارسال پیام انجام نشد. دوباره تلاش کنید.");
      showStatus(result.message || "پیام شما برای پشتیبانی ثبت شد.", "success");
      form.reset();
      rules.forEach(([input]) => setError(input, ""));
    } catch (error) {
      showStatus(error.message || "ارتباط با سرویس ارسال پیام برقرار نشد.", "error");
    } finally {
      button.innerHTML = original;
      button.disabled = false;
    }
  });
});
