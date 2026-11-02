window.HNUI.mount("pages-page-content", document, () => {
document.addEventListener("DOMContentLoaded", () => {
  const share = document.querySelector(".article-share");
  if (share) {
    const url = encodeURIComponent(window.location.href);
    const title = encodeURIComponent(document.title);
    share
      .querySelector('[data-share="telegram"]')
      ?.setAttribute("href", `https://t.me/share/url?url=${url}&text=${title}`);
    share
      .querySelector('[data-share="whatsapp"]')
      ?.setAttribute("href", `https://wa.me/?text=${title}%20${url}`);
    const destinations = {
      linkedin: `https://www.linkedin.com/sharing/share-offsite/?url=${url}`,
      x: `https://twitter.com/intent/tweet?url=${url}&text=${title}`,
      facebook: `https://www.facebook.com/sharer/sharer.php?u=${url}`,
      email: `mailto:?subject=${title}&body=${title}%0A${url}`,
    };
    Object.entries(destinations).forEach(([name, href]) =>
      share.querySelector(`[data-share="${name}"]`)?.setAttribute("href", href),
    );
    const nativeShare = share.querySelector('[data-share="native"]');
    if (nativeShare && typeof navigator.share === "function") {
      nativeShare.hidden = false;
      nativeShare.addEventListener("click", async () => {
        try {
          await navigator.share({
            title: document.title,
            url: window.location.href,
          });
        } catch (error) {
          if (error.name !== "AbortError")
            window.HNUI.setMessage(
              share.querySelector(".article-share-status"),
              "اشتراک‌گذاری انجام نشد؛ از کپی لینک استفاده کنید.",
              "error",
            );
        }
      });
    }
    share
      .querySelector('[data-share="copy"]')
      ?.addEventListener("click", async (event) => {
        const button = event.currentTarget;
        const status = share.querySelector(".article-share-status");
        try {
          await navigator.clipboard.writeText(window.location.href);
        } catch {
          window.HNUI.setMessage(
            status,
            "کپی خودکار ممکن نشد؛ لینک را از نوار آدرس کپی کنید.",
            "warning",
          );
          return;
        }
        window.HNUI.setMessage(status, "");
        window.HNUI.showToast("پیوند مقاله کپی شد.", "success");
        button.classList.add("copied");
        setTimeout(() => {
          window.HNUI.setMessage(status, "");
          button.classList.remove("copied");
        }, 2200);
      });
  }

  const form = document.querySelector("#contactForm");
  // Contact validation and submit feedback live in contact.js.
  if (form && !form.matches("#contactForm")) {
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      const button = form.querySelector('button[type="submit"]');
      const original = button.innerHTML;
      button.innerHTML = '<i class="bi bi-check2"></i> پیام شما ثبت شد';
      button.disabled = true;
      setTimeout(() => {
        button.innerHTML = original;
        button.disabled = false;
        form.reset();
      }, 2200);
    });
  }
});

});
