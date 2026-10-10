document.addEventListener("DOMContentLoaded", () => {
  const megaSets = [
    [
      ["بر اساس سبک", "کلاسیک", "مینیمال", "کرنوگراف", "اسپرت"],
      ["جنس بند", "چرم طبیعی", "استیل ضدزنگ", "بند مش", "سیلیکونی"],
      ["رنگ صفحه", "سرمه‌ای", "مشکی", "سفید", "سبز"],
      ["مجموعه مردانه", "روزمره", "رسمی", "پرفروش‌ها", "نسخه محدود"],
    ],
    [
      ["بر اساس سبک", "ظریف", "مینیمال", "کلاسیک", "مجلسی"],
      ["جنس بند", "استیل", "چرم", "سرامیکی", "بند مش"],
      ["رنگ محبوب", "رزگلد", "نقره‌ای", "سفید", "مشکی"],
      ["مجموعه زنانه", "جدیدترین‌ها", "پرفروش‌ها", "هدیه", "نسخه محدود"],
    ],
    [
      ["نوع استفاده", "روزمره", "باشگاهی", "طبیعت‌گردی", "مسافرت"],
      ["ویژگی فنی", "ضد آب", "کرنوگراف", "نمایش تاریخ", "شب‌تاب"],
      ["جنس بند", "سیلیکونی", "استیل", "برزنتی", "رابر"],
      ["مجموعه اسپرت", "پرفروش‌ها", "تازه‌رسیده‌ها", "مقاوم", "پیشنهاد هفته"],
    ],
    [
      ["فرم طراحی", "کلاسیک رسمی", "وینتیج", "مینیمال", "Dress Watch"],
      ["نوع موتور", "کوارتز", "اتوماتیک", "مکانیکی", "سولار"],
      ["جنس بند", "چرم طبیعی", "استیل", "بند مش", "دو رنگ"],
      ["مجموعه کلاسیک", "اداری", "مجلسی", "هدیه مدیریتی", "نسخه خاص"],
    ],
    [
      [
        "پیشنهاد ویژه",
        "تخفیف امروز",
        "پیشنهاد هفته",
        "قیمت ویژه",
        "ارسال رایگان",
      ],
      [
        "بازه قیمت",
        "زیر ۲۰ میلیون",
        "۲۰ تا ۳۰ میلیون",
        "۳۰ تا ۵۰ میلیون",
        "لوکس",
      ],
      [
        "منتخب مشتریان",
        "پرفروش‌ترین‌ها",
        "بالاترین امتیاز",
        "محبوب‌ترین‌ها",
        "تازه‌رسیده‌ها",
      ],
      ["فرصت محدود", "آخرین موجودی", "نسخه محدود", "پکیج هدیه", "خرید سازمانی"],
    ],
  ];
  const megaColumns = document.querySelector(".page-mega-columns");
  const renderMega = (index) => {
    if (!megaColumns) return;
    megaColumns.classList.remove("changing");
    void megaColumns.offsetWidth;
    megaColumns.innerHTML = megaSets[index]
      .map(
        (group) =>
          `<div><strong>${group[0]}</strong>${group
            .slice(1)
            .map((item) => `<a href="Home/Index#collection">${item}</a>`)
            .join("")}</div>`,
      )
      .join("");
    megaColumns.classList.add("changing");
  };
  document.querySelectorAll(".page-mega-sidebar>a").forEach((link, index) =>
    link.addEventListener("mouseenter", () => {
      document
        .querySelectorAll(".page-mega-sidebar>a")
        .forEach((item) => item.classList.remove("active"));
      link.classList.add("active");
      renderMega(index);
    }),
  );
  document.querySelectorAll(".page-header-dropdown>button").forEach((button) =>
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      const item = button.parentElement;
      document
        .querySelectorAll(".page-header-dropdown.open")
        .forEach((open) => {
          if (open !== item) open.classList.remove("open");
        });
      item.classList.toggle("open");
    }),
  );
  document.addEventListener("click", () =>
    document
      .querySelectorAll(".page-header-dropdown.open")
      .forEach((item) => item.classList.remove("open")),
  );
  document.addEventListener("click", (event) => {
    const link = event.target.closest(
      ".page-mega-sidebar a,.page-mega-columns a,.page-brand-menu>a",
    );
    if (!link) return;
    event.preventDefault();
    const label =
      link.querySelector("span")?.childNodes[0]?.textContent.trim() ||
      link.textContent.trim();
    const params = new URLSearchParams();
    const type = ["مردانه", "زنانه", "اسپرت", "کلاسیک"].find((item) =>
      label.includes(item),
    );
    if (link.closest(".page-brand-menu")) params.set("brand", label);
    else if (type) params.set("type", type);
    location.href = `Search${params.toString() ? `?${params}` : ""}`;
  });
});

