(() => {
  const imageMap = new Map([
    ["watch-blue.webp", "tool-hammer-drill.png"],
    ["watch-cream.webp", "tool-multimeter-kit.png"],
    ["watch-silver.webp", "tool-soldering-station.png"],
    ["watch-story.webp", "tool-led-worklight.png"],
    ["watch-hero.webp", "tool-hammer-drill.png"],
    ["slider-navy-steel.webp", "tool-hammer-drill.png"],
    ["slider-navy-chrono.webp", "tool-multimeter-kit.png"],
    ["slider-cream-classic.webp", "tool-led-worklight.png"]
  ]);

  const phraseMap = new Map([
    ["مجله ساعت", "مجله فنی"],
    ["مجله تخصصی ساعت", "مجله تخصصی برق و ابزار"],
    ["گالری ساعت و استایل", "گالری ابزار و پروژه"],
    ["گالری ساعت", "گالری ابزار"],
    ["فروشگاه ساعت", "فروشگاه برق و ابزار"],
    ["ساعت‌فروشی علی‌رضا زارع", "مرکز برق و ابزار علی‌رضا زارع"],
    ["گالری ساعت علی‌رضا زارع", "گالری ابزار علی‌رضا زارع"],
    ["کاتالوگ هوشمند ساعت", "کاتالوگ هوشمند برق و ابزار"],
    ["جستجو و انتخاب دقیق‌تر", "جستجو و انتخاب ابزار دقیق‌تر"],
    ["نوع ساعت", "گروه محصول"],
    ["نوع موتور", "نوع تغذیه"],
    ["جنس بند", "محیط کاربرد"],
    ["جزئیات ساعت", "جزئیات محصول"],
    ["مشاهده همه ساعت‌ها", "مشاهده همه ابزارها"],
    ["ساعت‌های منتخب", "ابزارهای منتخب"],
    ["تمام ساعت‌ها", "تمام محصولات"],
    ["ساعت‌های اورجینال", "ابزار و قطعات اصل"],
    ["ساعت‌های اورجینال مردانه و زنانه", "ابزار برقی، قطعات الکترونیکی و تجهیزات روشنایی"],
    ["واردات، تأمین و عرضه ساعت‌های اورجینال مردانه و زنانه است.", "تأمین و عرضه ابزار برقی، قطعات الکترونیکی و تجهیزات تخصصی موردنیاز صنعتگران است."],
    ["هر ساعت", "هر ابزار"],
    ["انتخاب ساعت", "انتخاب ابزار"],
    ["خرید ساعت", "خرید ابزار"],
    ["نگهداری ساعت", "نگهداری ابزار"],
    ["تنظیم بند ساعت", "راه‌اندازی و استفاده از ابزار"],
    ["ساعتی انتخاب کردم", "ابزاری انتخاب کردم"],
    ["ساعت را بهتر ببینید", "محصول را بهتر بررسی کنید"],
    ["ساعت موردعلاقه‌تان را پیدا کردید؟", "ابزار موردنیازتان را پیدا کردید؟"],
    ["ساعت‌های ذخیره‌شده", "محصولات ذخیره‌شده"],
    ["ساعت موردعلاقه", "محصول موردعلاقه"],
    ["نوا کرونو، نسخه شب", "دریل چکشی شارژی حرفه‌ای"],
    ["نوا کرونو شب", "دریل چکشی شارژی"],
    ["نوا کرونو", "دریل چکشی شارژی"],
    ["آرمان رزگلد", "کیت مولتی‌متر دیجیتال"],
    ["آرمان کلاسیک", "مولتی‌متر دیجیتال حرفه‌ای"],
    ["مهتاب مینیمال", "ایستگاه هویه دیجیتال"],
    ["هاله رز", "چراغ کار LED"],
    ["میراث کلاسیک", "ست ابزار برق‌کار"],
    ["قاب استیل ضدخش، صفحه سرمه‌ای و مقاومت تا عمق ۵۰ متر.", "موتور قدرتمند، سه‌نظام مقاوم و باتری بادوام برای کارهای نصب و کارگاهی."],
    ["سادگی کلاسیک با جزئیات گرم و بند چرم طبیعی.", "اندازه‌گیری دقیق ولتاژ، جریان و مقاومت همراه با پراب‌های ایمن."],
    ["راهنمای خرید و نگهداری ساعت", "راهنمای خرید، تعمیر و نگهداری ابزار"],
    ["راهنمای انتخاب ساعت مچی مناسب", "راهنمای انتخاب ابزار مناسب هر پروژه"],
    ["ضمانت اصالت تمام ساعت‌ها", "تضمین اصالت تمام ابزارها و قطعات"],
    ["خریدار نوا کرونو", "خریدار دریل شارژی"],
    ["خریدار آرمان رزگلد", "خریدار مولتی‌متر دیجیتال"],
    ["خریدار مهتاب مینیمال", "خریدار ایستگاه هویه"],
    ["ساعت مردانه", "ابزار برقی"],
    ["ساعت زنانه", "ابزار دستی"],
    ["ساعت کلاسیک", "قطعات الکترونیکی"],
    ["ساعت اسپرت", "تجهیزات روشنایی"]
  ]);

  const servicePaths = new Set(["/Contact", "/Faq", "/Warranty", "/Tracking", "/Shipping"]);
  const editorialPaths = new Set(["/About", "/Brands", "/Gallery", "/Magazine"]);

  const replaceText = root => {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    const nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    nodes.forEach(node => {
      let value = node.nodeValue;
      phraseMap.forEach((replacement, phrase) => { value = value.split(phrase).join(replacement); });
      value = value.replace(/ساعت/g, "ابزار");
      node.nodeValue = value;
    });
  };

  const replaceImages = root => {
    root.querySelectorAll?.("img").forEach(image => {
      for (const [oldName, newName] of imageMap) {
        if (image.src.includes(oldName)) {
          image.src = `ClockTemplate/images/products/${newName}`;
          image.alt = "ابزار و تجهیزات تخصصی برق";
          break;
        }
      }
      if (servicePaths.has(location.pathname) && image.closest(".page-hero")) image.src = "ClockTemplate/images/heroes/hero-electrical-service.png";
      if (editorialPaths.has(location.pathname) && image.closest(".page-hero")) image.src = "ClockTemplate/images/heroes/hero-electrical-editorial.png";
      if (image.closest(".page-hero")) image.alt = "تجهیزات برق، ابزار حرفه‌ای و خدمات فنی";
    });
    root.querySelectorAll?.("[data-image]").forEach(element => {
      for (const [oldName, newName] of imageMap) {
        if ((element.dataset.image || "").includes(oldName)) element.dataset.image = `ClockTemplate/images/products/${newName}`;
      }
    });
  };

  const apply = root => {
    replaceText(root);
    replaceImages(root);
  };

  document.addEventListener("DOMContentLoaded", () => {
    apply(document.body);
    new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => {
      if (node.nodeType === Node.ELEMENT_NODE) apply(node);
    }))).observe(document.body, { childList: true, subtree: true });
  });
})();
