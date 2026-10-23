window.HNUI.mount("core-form-validation", document, () => {
  "use strict";

  const customForms = new Set([
    "contactForm",
    "profileEditModalForm",
    "loginEmailForm",
    "registerEmailForm",
    "forgotPasswordForm",
    "emailOtpForm",
  ]);
  const prepareForm = (form) => {
    if (form instanceof HTMLFormElement) form.noValidate = true;
  };

  document.querySelectorAll("form").forEach(prepareForm);
  new MutationObserver((records) => {
    records.forEach((record) =>
      record.addedNodes.forEach((node) => {
        if (!(node instanceof Element)) return;
        if (node.matches("form")) prepareForm(node);
        node.querySelectorAll?.("form").forEach(prepareForm);
      }),
    );
  }).observe(document.documentElement, { childList: true, subtree: true });

  const messageFor = (control, value) => {
    if (control.required && !value) {
      if (control.type === "checkbox" || control.type === "radio")
        return "این گزینه را انتخاب کنید.";
      if (control.tagName === "SELECT") return "یک مورد را انتخاب کنید.";
      return "تکمیل این فیلد الزامی است.";
    }
    if (!value) return "";

    if (
      control.dataset.validation === "full-name" &&
      value.split(/\s+/).filter(Boolean).length < 2
    )
      return "نام و نام خانوادگی را کامل وارد کنید.";

    if (control.type === "email" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value))
      return "ایمیل را با قالب صحیح وارد کنید.";
    if (control.type === "url") {
      try {
        const url = new URL(value);
        if (!url.protocol || !url.hostname) throw new Error("invalid");
      } catch {
        return "نشانی وب را با قالب صحیح وارد کنید.";
      }
    }
    const normalized =
      /^(tel|number)$/.test(control.type) || control.inputMode === "numeric"
        ? window.HNUI.toEnglishDigits(value)
        : value;
    if (control.pattern) {
      try {
        if (!new RegExp(`^(?:${control.pattern})$`).test(normalized)) {
          if (control.type === "tel")
            return "شماره همراه را با قالب صحیح وارد کنید.";
          if (control.name === "postal") return "کد پستی باید ۱۰ رقم باشد.";
          return "مقدار واردشده با قالب موردنیاز هم‌خوانی ندارد.";
        }
      } catch {
        return "قالب این فیلد معتبر نیست.";
      }
    }
    if (control.minLength >= 0 && value.length < control.minLength)
      return `حداقل ${control.minLength} نویسه وارد کنید.`;
    if (control.maxLength >= 0 && value.length > control.maxLength)
      return `حداکثر ${control.maxLength} نویسه مجاز است.`;
    if (
      control.type === "number" &&
      control.min !== "" &&
      Number(value) < Number(control.min)
    )
      return `مقدار باید حداقل ${control.min} باشد.`;
    if (
      control.type === "number" &&
      control.max !== "" &&
      Number(value) > Number(control.max)
    )
      return `مقدار باید حداکثر ${control.max} باشد.`;
    return "";
  };

  const errorIdFor = (control) =>
    `form-error-${(control.id || control.name || "field").replace(/[^a-zA-Z0-9_-]/g, "-")}`;

  const renderError = (control, message) => {
    const id = errorIdFor(control);
    let host = document.getElementById(id);
    if (!host) {
      host = document.createElement("small");
      host.id = id;
      host.className = "form-field-error";
      host.setAttribute("role", "alert");
      const visualField =
        control.closest(".login-input, .tracking-input") || control;
      visualField.insertAdjacentElement("afterend", host);
    }

    host.textContent = message;
    host.hidden = !message;
    window.HNUI?.setMessageType(host, message ? "error" : "");
    control.classList.toggle("is-invalid", Boolean(message));
    control.setAttribute("aria-invalid", String(Boolean(message)));
    const describedBy = new Set(
      (control.getAttribute("aria-describedby") || "")
        .split(/\s+/)
        .filter(Boolean),
    );
    if (message) describedBy.add(id);
    else describedBy.delete(id);
    if (describedBy.size)
      control.setAttribute("aria-describedby", [...describedBy].join(" "));
    else control.removeAttribute("aria-describedby");
  };

  const validateForm = (form) => {
    const invalid = [];
    const handledGroups = new Set();
    [...form.elements].forEach((control) => {
      if (
        !(control instanceof HTMLElement) ||
        control.disabled ||
        control.type === "submit" ||
        control.type === "button" ||
        control.type === "reset" ||
        control.type === "hidden"
      )
        return;
      if (control.matches(":radio, :checkbox") && control.name) {
        if (handledGroups.has(control.name)) return;
        handledGroups.add(control.name);
        const group = [
          ...form.querySelectorAll(`[name="${CSS.escape(control.name)}"]`),
        ];
        const required = group.some((item) => item.required);
        const message =
          required && !group.some((item) => item.checked)
            ? "یک گزینه را انتخاب کنید."
            : "";
        group.forEach((item) => {
          item.classList.toggle("is-invalid", Boolean(message));
          item.setAttribute("aria-invalid", String(Boolean(message)));
        });
        renderError(group[0], message);
        if (message) invalid.push(control);
        return;
      }
      const message = messageFor(control, String(control.value ?? "").trim());
      renderError(control, message);
      if (message) invalid.push(control);
    });
    return invalid;
  };

  document.addEventListener(
    "submit",
    (event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) return;
      prepareForm(form);
      if (customForms.has(form.id)) return;
      const invalid = validateForm(form);
      if (!invalid.length) return;
      event.preventDefault();
      event.stopImmediatePropagation();
      form.dispatchEvent(
        new CustomEvent("hn:form-invalid", {
          bubbles: true,
          detail: { invalid },
        }),
      );
      invalid[0].focus({ preventScroll: true });
      invalid[0].scrollIntoView({ behavior: "smooth", block: "center" });
    },
    true,
  );

  document.addEventListener("input", (event) => {
    const control = event.target;
    if (
      !(
        control instanceof HTMLInputElement ||
        control instanceof HTMLTextAreaElement
      )
    )
      return;
    if (control.getAttribute("aria-invalid") !== "true") return;
    const message = messageFor(control, String(control.value ?? "").trim());
    renderError(control, message);
  });
  document.addEventListener("change", (event) => {
    const control = event.target;
    if (
      !(
        control instanceof HTMLSelectElement ||
        control instanceof HTMLInputElement
      )
    )
      return;
    if (control.getAttribute("aria-invalid") !== "true") return;
    if (control.matches(":radio, :checkbox") && control.name) {
      const group = [
        ...control.form.querySelectorAll(
          `[name="${CSS.escape(control.name)}"]`,
        ),
      ];
      const message = group.some((item) => item.checked)
        ? ""
        : "یک گزینه را انتخاب کنید.";
      group.forEach((item) => {
        item.classList.toggle("is-invalid", Boolean(message));
        item.setAttribute("aria-invalid", String(Boolean(message)));
      });
      renderError(group[0], message);
      return;
    }
    renderError(
      control,
      messageFor(control, String(control.value ?? "").trim()),
    );
  });
  document.addEventListener("reset", (event) => {
    window.setTimeout(() => {
      event.target
        .querySelectorAll("[aria-invalid], .is-invalid")
        .forEach((control) => {
          renderError(control, "");
        });
    });
  });
});
