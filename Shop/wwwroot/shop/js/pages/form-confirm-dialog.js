window.HNUI.mount("pages-form-confirm-dialog", document, () => {
  let activeResolve = null;
  let lastFocused = null;
  const overlay = document.createElement("div");
  overlay.className = "form-confirm-overlay";
  overlay.hidden = true;
  overlay.innerHTML = `<div class="form-confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="formConfirmTitle" aria-describedby="formConfirmText"><span class="form-confirm-icon"><i class="bi bi-shield-check"></i></span><h2 id="formConfirmTitle"></h2><p id="formConfirmText"></p><div class="form-confirm-actions"><button class="form-confirm-accept" type="button">بله، ثبت شود</button><button class="form-confirm-cancel" type="button">بازگشت و بررسی</button></div></div>`;
  document.body.appendChild(overlay);
  const accept = overlay.querySelector(".form-confirm-accept");
  const cancel = overlay.querySelector(".form-confirm-cancel");
  const close = (result) => {
    if (!activeResolve) return;
    overlay.classList.remove("is-open");
    document.body.classList.remove("has-form-confirm");
    const resolve = activeResolve;
    activeResolve = null;
    resolve(result);
    window.setTimeout(() => {
      overlay.hidden = true;
      lastFocused?.focus();
    }, 220);
  };
  accept.addEventListener("click", () => close(true));
  cancel.addEventListener("click", () => close(false));
  overlay.addEventListener("click", (event) => {
    if (event.target === overlay) close(false);
  });
  document.addEventListener("keydown", (event) => {
    if (!activeResolve) return;
    if (event.key === "Escape") close(false);
    if (event.key === "Tab") {
      const index = [accept, cancel].indexOf(document.activeElement);
      if (event.shiftKey && index <= 0) {
        event.preventDefault();
        cancel.focus();
      } else if (!event.shiftKey && index === 1) {
        event.preventDefault();
        accept.focus();
      }
    }
  });
  window.confirmFormSubmission = (options) =>
    new Promise((resolve) => {
      if (activeResolve) return resolve(false);
      lastFocused = document.activeElement;
      overlay.querySelector("h2").textContent =
        options?.title || "از ثبت درخواست مطمئن هستید؟";
      overlay.querySelector("p").textContent =
        options?.message ||
        "لطفاً پیش از ثبت، از درستی اطلاعات واردشده مطمئن شوید.";
      accept.textContent = options?.confirmText || "بله، ثبت شود";
      activeResolve = resolve;
      overlay.hidden = false;
      document.body.classList.add("has-form-confirm");
      requestAnimationFrame(() => {
        overlay.classList.add("is-open");
        accept.focus();
      });
    });
});
