document.addEventListener("DOMContentLoaded", () => {
    const ui = window.HNUI,
        catalog = window.HNProducts || [];
    const fa = (n) => ui.formatNumber(n);
    const esc = (s) => ui.escapeHtml(s);
    const money = (n) => `${fa(n)} تومان`;
    const cartOptionsMarkup = (item) => ui.cart.renderOptions(item);
    const salePriceMarkup = (product) => {
        const discount = Number(product?.discountPercent) || 0;
        const current = Math.round(Number(product?.price) * 1000000);
        if (!discount || discount >= 100 || !current) return `<strong>${money(current)}</strong>`;
        const previous = Math.round(current / (1 - discount / 100));
        return `<strong class="product-price-current">${money(current)}</strong><span class="product-price-deal"><del class="price-old">${money(previous)}</del><b class="product-price-discount">٪${fa(discount)}</b></span>`;
    };
    const empty = (text) =>
        `<div class="flow-card flow-empty empty-state-card"><i class="bi bi-bag"></i><h2>${text}</h2><a class="flow-button" href="Search">مشاهده محصولات</a></div>`;
    const emptyCart = () =>
        `<section class="cart-empty-state" aria-labelledby="emptyCartTitle"><div class="cart-empty-ornament" aria-hidden="true"><span><i class="bi bi-bag"></i></span></div><div class="cart-empty-copy"><span class="cart-empty-kicker">سبد خرید</span><h2 id="emptyCartTitle">سبد خرید شما خالی است</h2><p>هنوز محصولی انتخاب نکرده‌اید. کاتالوگ را مرور کنید یا برای آشنایی با معیارهای انتخاب، راهنماهای فنی را بخوانید.</p><div class="cart-empty-actions"><a class="flow-button" href="Search"><i class="bi bi-tools" aria-hidden="true"></i><span>مرور محصولات</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a><a class="ui-inline-action" href="Magazine"><i class="bi bi-journal-text" aria-hidden="true"></i><span>راهنمای انتخاب</span></a></div></div></section>`;
    const favorites = () => {
        try {
            const v = JSON.parse(ui.storage.get("hn-favorites", "[]"));
            return Array.isArray(v) ? [...new Set(v.map(String))] : [];
        } catch {
            return [];
        }
    };
    const routes = {
        "پیگیری سفارش": "Tracking",
        "شرایط ارسال": "Shipping",
        "ضمانت اصالت": "Warranty",
        "پرسش‌های متداول": "Faq",
        "پروفایل من": "Profile",
        "سفارش‌های من": "Profile/Orders",
        "آدرس‌های من": "Profile/Addresses",
        "لیست علاقه‌مندی‌ها": "Profile/Favorites",
        "دیدگاه‌ها و پرسش‌ها": "Profile/Questions",
        "مشاهده سبد و ادامه خرید": "Cart",
        "مشاهده همه برندها": "Brands",
        "مشاهده همه مطالب": "Magazine",
        "درباره مجموعه": "About",
        "تماس با ما": "Contact",
    };
    function connect() {
        document.querySelectorAll("a").forEach((a) => {
            const t = a.textContent.trim().replace(/\s+/g, " ");
            for (const [label, url] of Object.entries(routes))
                if (t === label || (t.startsWith(label) && t.length < label.length + 5))
                    a.setAttribute("href", url);
        });
        document
            .querySelectorAll(
                "#cartFooter a,.page-cart-footer>a,#searchCartDrawer .search-cart-footer a",
            )
            .forEach((a) => (a.href = "Cart"));
        document.querySelectorAll(".product-card").forEach((card) => {
            if (card.querySelector('a[href^="Product"]')) return;
            const name = card.querySelector("h3");
            if (!name) return;
            const p = catalog.find((p) => name.textContent.trim().includes(p.name));
            if (p) {
                const a = document.createElement("a");
                a.href = window.HNProductUrl(p);
                a.textContent = name.textContent;
                name.replaceChildren(a);
            }
        });
    }
    connect();
    window.addEventListener("load", connect);
    document
        .querySelector("#searchCartFooter button")
        ?.addEventListener("click", () => {
            location.href = "Cart";
        });
    const productView = document.querySelector("#productView");
    if (productView) {
        const productId = Number(location.pathname.match(/(?:^|\/)(?:[a-z0-9-]+-)?(\d+)\/?$/i)?.[1]);
        const p = catalog.find((p) => p.id === productId);
        if (!p) productView.innerHTML = empty("محصول پیدا نشد");
        else {
            const productSectionHash =
                /^#product-(?:intro|expert|specifications|reviews|questions)$/.test(
                    location.hash,
                );
            if (
                productSectionHash &&
                window.matchMedia("(max-width: 767px)").matches
            ) {
                history.replaceState(
                    history.state,
                    "",
                    `${location.pathname}${location.search}`,
                );
                window.scrollTo({ top: 0, left: 0, behavior: "auto" });
            }
            const isFavorite = favorites().includes(String(p.id));
            const selectedVariants = p.variants
                ? Object.fromEntries(
                    Object.entries(p.variants)
                        .map(([key, feature]) => [key, feature.options[0]?.value])
                        .filter(([, value]) => value !== undefined),
                )
                : {};
            const images =
                Array.isArray(p.images) && p.images.length
                    ? p.images.filter(Boolean)
                    : [p.image];
            const videos = (Array.isArray(p.videos) ? p.videos : [])
                .map((video) => (typeof video === "string" ? { src: video } : video))
                .filter((video) => video?.src);
            const media = [
                ...images.map((src) => ({ type: "image", src })),
                ...videos.map((video) => ({
                    type: "video",
                    src: video.src,
                    poster: video.poster || p.image,
                    title: video.title || `ویدئوی ${p.name}`,
                })),
            ];
            const mainMedia = media[0] || { type: "image", src: p.image };
            const renderMedia = (item) =>
                item.type === "video"
                    ? `<video class="product-detail-image" controls preload="metadata" poster="${esc(item.poster || p.image)}"><source src="${esc(item.src)}"></video>`
                    : `<img class="product-detail-image" src="${esc(item.src)}" alt="${esc(p.name)}" fetchpriority="high">`;
            const thumbMarkup =
                media.length > 1
                    ? `<div class="product-detail-thumbnails" role="group" aria-label="انتخاب تصویر یا ویدئو">${media.map((item, index) => `<button class="product-detail-thumb${index === 0 ? " is-active" : ""}" type="button" data-detail-media-index="${index}" aria-label="${item.type === "video" ? esc(item.title) : `تصویر ${fa(index + 1)}`}" aria-pressed="${index === 0}">${item.type === "video" ? `<span><img src="${esc(item.poster || p.image)}" alt="" loading="lazy"><i class="bi bi-play-fill" aria-hidden="true"></i></span>` : `<img src="${esc(item.src)}" alt="" loading="lazy">`}</button>`).join("")}</div>`
                    : "";
            const variantMarkup = p.variants
                ? `<section class="product-variant-options" aria-labelledby="productVariantTitle"><header class="product-variant-heading"><span class="product-variant-heading-icon"><i class="bi bi-sliders" aria-hidden="true"></i></span><span><strong id="productVariantTitle">انتخاب ویژگی‌های محصول</strong><small>گزینه‌های موردنظر را انتخاب کنید.</small></span></header>${Object.entries(
                    p.variants,
                )
                    .map(([key, feature]) => {
                        const isPriceRequired = feature.options.some(
                            (option) => Number(option.priceDelta) !== 0,
                        ),
                            selectedValue = selectedVariants[key];
                        return `<fieldset class="product-variant-field product-variant-field--${esc(key)}" data-variant-key="${esc(key)}" data-required="${isPriceRequired}"><legend><span>${esc(feature.label)}</span></legend><div class="product-variant-choices">${feature.options
                            .map((option) => {
                                const selected = option.value === selectedValue;
                                return `<button class="product-variant-choice${option.swatch ? " product-variant-choice--swatch" : ""}${selected ? " is-selected" : ""}" type="button" data-value="${esc(option.value)}" data-delta="${Number(option.priceDelta) || 0}" aria-label="${esc(option.label)}" title="${esc(option.label)}" aria-pressed="${selected}">${option.swatch ? `<span class="product-variant-swatch" style="--variant-swatch:${esc(option.swatch)}"></span>` : ""}<span class="product-variant-choice-label">${esc(option.label)}</span>${Number(option.priceDelta) ? `<small>+ ${money(Number(option.priceDelta) * 1000000)}</small>` : ""}</button>`;
                            })
                            .join("")}</div></fieldset>`;
                    })
                    .join("")}</section>`
                : "";
            const selectedAccessories = new Set(
                (p.accessories || [])
                    .filter((item) => item.required)
                    .map((item) => item.id),
            );
            const accessoryMarkup = p.accessories?.length
                ? `<section class="product-accessories" aria-labelledby="productAccessoriesTitle"><header><span class="product-accessories-icon"><i class="bi bi-box-seam" aria-hidden="true"></i></span><span><strong id="productAccessoriesTitle">متعلقات و خدمات کالا</strong><small>موارد الزامی مشخص‌اند؛ بقیه را به‌دلخواه انتخاب کنید.</small></span></header><div class="product-accessory-list">${p.accessories.map((item) => `<label class="product-accessory${item.required ? " is-required" : ""}"><input type="checkbox" data-accessory-id="${esc(item.id)}" ${item.required ? "checked disabled" : ""}><span class="product-accessory-check" aria-hidden="true"><i class="bi bi-check2"></i></span><span class="product-accessory-copy"><strong>${esc(item.name)}<em class="product-accessory-status ${item.required ? "is-required" : "is-optional"}">${item.required ? '<i class="bi bi-lock-fill" aria-hidden="true"></i> الزامی' : "اختیاری"}</em></strong><small>${esc(item.description)}</small></span><span class="product-accessory-price"><small>افزوده به فاکتور</small><strong class="product-price-current">${item.price ? `+ ${money(Number(item.price) * 1000000)}` : "رایگان"}</strong></span></label>`).join("")}</div></section>`
                : "";
            const specs = [
                ["برند", p.brand],
                ["دسته‌بندی", p.type],
                ["منبع تغذیه", p.powerSource || p.movement],
                ["مورد استفاده", p.useCase || p.strap],
                ["شناسه محصول", `HN-${String(p.id).padStart(3, "0")}`],
                ...(p.detail?.specs || []),
            ];
            const related = catalog
                .filter(
                    (item) =>
                        item.id !== p.id &&
                        (item.type === p.type || item.brand === p.brand),
                )
                .slice(0, 4);
            const suggestions = related.length
                ? related
                : catalog.filter((item) => item.id !== p.id).slice(0, 4);
            const feedbackPages = { reviews: 1, questions: 1 },
                feedbackPageSize = 4;
            const questionGuideItems = [
                [
                    "اقلام همراه کالا را از کجا ببینم؟",
                    "فهرست محتویات و متعلقات در مشخصات همین کالا درج شده است؛ بسته‌بندی مدل‌های مختلف ممکن است تفاوت داشته باشد.",
                ],
                [
                    "چطور سازگاری لوازم جانبی را بررسی کنم؟",
                    "مدل، ابعاد، نوع اتصال و مشخصات فنی را با کالای اصلی تطبیق دهید؛ هم‌برند بودن به‌تنهایی سازگاری را تضمین نمی‌کند.",
                ],
                [
                    "شرایط ضمانت و خدمات کالا چیست؟",
                    "مدت و موارد پوشش بسته به کالا متفاوت است؛ شرایط ضمانت ثبت‌شده برای همین محصول را پیش از خرید بررسی کنید.",
                ],
                [
                    "برای استفادهٔ طولانی چه نکته‌ای رعایت کنم؟",
                    "محدودهٔ کاری و شیوهٔ نگهداری هر مدل متفاوت است؛ مشخصات و دستورالعمل همان کالا را مبنا قرار دهید.",
                ],
            ].map(([text, reply]) => ({
                name: "راهنمای خرید",
                text,
                rating: 0,
                reply,
                sample: true,
            }));
            const readFeedback = (type) => {
                const seeded = (p.detail?.[type] || []).map(
                    ([name, text, rating, reply]) => ({
                        name: name?.trim() || "کاربر مهمان",
                        text,
                        rating: rating || 0,
                        reply: reply || "",
                        sample: true,
                    }),
                );
                if (type === "questions") seeded.push(...questionGuideItems);
                try {
                    const raw = ui.storage.get(
                        `hn-product-${type}-${p.id}`,
                        "__HN_EMPTY__",
                    );
                    if (raw === "__HN_EMPTY__") return seeded;
                    const value = JSON.parse(raw);
                    return [
                        ...(Array.isArray(value)
                            ? value
                                .filter((item) => item && typeof item.text === "string")
                                .map((item) => ({
                                    ...item,
                                    name: item.name?.trim() || "کاربر مهمان",
                                    sample: false,
                                }))
                            : []),
                        ...seeded,
                    ];
                } catch {
                    return seeded;
                }
            };
            const ratingStars = (rating) => {
                const value = Math.max(1, Math.min(5, Number(rating) || 5));
                return `<span class="product-feedback-stars" aria-label="امتیاز ${fa(value)} از ۵"><span aria-hidden="true">${"★".repeat(value)}</span><span class="product-feedback-stars-empty" aria-hidden="true">${"☆".repeat(5 - value)}</span></span>`;
            };
            const feedbackMarkup = (type, items) =>
                items.length
                    ? items
                        .map(
                            (item) =>
                                `<article class="product-feedback-card"><div class="product-feedback-meta"><img class="product-feedback-avatar" src="shop/images/guest-avatar.svg" alt="" loading="lazy"><strong>${esc(item.name?.trim() || "کاربر مهمان")}</strong>${type === "reviews" ? `${ratingStars(item.rating)}` : ""}</div><p class="${type === "questions" ? "product-question-user-text" : "product-feedback-user-text"}">${esc(item.text)}</p>${item.reply ? `<aside class="product-feedback-admin-reply"><div class="product-feedback-admin-meta"><span><i class="bi bi-patch-check-fill" aria-hidden="true"></i>پاسخ مدیر فروشگاه</span><small>پاسخ رسمی</small></div><p>${esc(item.reply)}</p></aside>` : ""}</article>`,
                        )
                        .join("")
                    : `<div class="product-feedback-empty empty-state-card"><i class="bi ${type === "reviews" ? "bi-chat-square-heart" : "bi-question-circle"}" aria-hidden="true"></i><strong>${type === "reviews" ? "هنوز دیدگاهی برای این محصول ثبت نشده است." : "هنوز پرسشی دربارهٔ این محصول ثبت نشده است."}</strong><span>${type === "reviews" ? "تجربهٔ استفاده‌تان را با خریداران دیگر به اشتراک بگذارید." : "سؤال خود را ثبت کنید تا اطلاعات این کالا کامل‌تر شود."}</span></div>`;
            productView.innerHTML = `<div class="product-detail-layout"><section class="product-detail-gallery" aria-label="تصاویر و ویدئوهای محصول"><div class="product-detail-image-frame"><span class="product-detail-image-code"><i class="bi bi-upc-scan" aria-hidden="true"></i> HN-${String(p.id).padStart(3, "0")}</span><div class="product-detail-media-frame" id="productDetailMedia">${renderMedia(mainMedia)}</div><button class="product-detail-gallery-open" id="openProductGallery" type="button"><i class="bi bi-grid-3x3-gap" aria-hidden="true"></i><span>مشاهدهٔ همه</span><b>${fa(media.length)}</b></button></div>${thumbMarkup}<div class="product-detail-gallery-caption"><i class="bi bi-images" aria-hidden="true"></i><span>تصاویر و ویدئوهای محصول</span><small>${media.length > 1 ? `${fa(media.length)} فایل رسانه‌ای` : "نمای کامل محصول"}</small></div></section><section class="product-detail-summary" aria-labelledby="productDetailTitle"><div class="product-detail-meta"><span><i class="bi bi-patch-check" aria-hidden="true"></i>${esc(p.brand)}</span><span>کد کالا: HN-${String(p.id).padStart(3, "0")}</span></div><h2 id="productDetailTitle">${esc(p.name)}</h2><p class="product-detail-description">${esc(p.name)} از گروه ${esc(p.type)}، با ویژگی ${esc(p.movement)} و ساختار ${esc(p.strap)}.</p><div class="product-main-feature-chips" aria-label="ویژگی‌های اصلی"><span><i class="bi bi-grid" aria-hidden="true"></i>${esc(p.type)}</span><span><i class="bi bi-lightning-charge" aria-hidden="true"></i>${esc(p.movement)}</span><span><i class="bi bi-building" aria-hidden="true"></i>${esc(p.brand)}</span></div>${variantMarkup}<div class="product-detail-price"><div class="product-price-heading"><span>تفکیک قیمت</span><small>قیمت کالا و هزینه‌های انتخابی</small></div><div class="product-price-context"><i class="bi bi-shield-check" aria-hidden="true"></i><span><strong>خرید مطمئن</strong><small>قیمت نهایی همراه با ضمانت اصالت کالا</small></span></div><div class="product-detail-base-price"><div class="product-detail-base-row"><span>قیمت کالا</span><strong id="productBasePrice">${money(p.price * 1000000)}</strong></div><div class="product-detail-sale-meta"></div></div><div class="product-detail-extras-price"><span>هزینهٔ گزینه‌ها و متعلقات</span><strong id="productAddonsPrice"></strong></div><div class="product-detail-total-price"><span>مبلغ قابل پرداخت</span><strong id="productFinalPrice">${money(p.price * 1000000)}</strong></div></div><div class="product-detail-actions"><button class="product-detail-add flow-button" id="addProduct" type="button"><i class="bi bi-bag-plus" aria-hidden="true"></i> افزودن به سبد خرید</button><div class="product-detail-secondary-actions"><a href="Cart"><i class="bi bi-basket3" aria-hidden="true"></i> مشاهده سبد</a><button class="product-detail-favorite" id="saveProduct" type="button" aria-pressed="${isFavorite}" aria-label="${isFavorite ? "حذف از علاقه‌مندی‌ها" : "ذخیره در علاقه‌مندی‌ها"}"><i class="bi ${isFavorite ? "bi-heart-fill" : "bi-heart"}" aria-hidden="true"></i><span>${isFavorite ? "ذخیره‌شده" : "علاقه‌مندی"}</span></button></div></div><p class="product-detail-status" id="productStatus" role="status" aria-live="polite"></p></section></div><nav class="product-detail-tabs" aria-label="بخش‌های اطلاعات محصول"><a href="#product-intro"><i class="bi bi-file-earmark-text" aria-hidden="true"></i>معرفی</a><a href="#product-expert"><i class="bi bi-clipboard2-pulse" aria-hidden="true"></i>بررسی تخصصی</a><a href="#product-specifications"><i class="bi bi-sliders2" aria-hidden="true"></i>مشخصات</a><a href="#product-reviews"><i class="bi bi-chat-square-text" aria-hidden="true"></i>دیدگاه‌ها <b>${fa(readFeedback("reviews").length)}</b></a><a href="#product-questions"><i class="bi bi-patch-question" aria-hidden="true"></i>پرسش‌ها <b>${fa(readFeedback("questions").length)}</b></a></nav><section class="product-detail-content-panel" id="product-intro"><header><span>معرفی محصول</span><h2>${esc(p.name)}؛ انتخابی متناسب با نیاز شما</h2></header><p>این محصول از دستهٔ ${esc(p.type)} است. هنگام انتخاب، نوع ${esc(p.movement)} و ساختار ${esc(p.strap)} را با نیاز و شرایط کاری خود تطبیق دهید. اطلاعات این صفحه بر پایهٔ مشخصات ثبت‌شدهٔ کالا تهیه شده است.</p></section><section class="product-detail-content-panel product-detail-expert" id="product-expert"><header><span>راهنمای انتخاب</span><h2>بررسی تخصصی</h2></header><div class="product-expert-grid"><div><i class="bi bi-tools" aria-hidden="true"></i><strong>دسته و کاربرد</strong><p>${esc(p.type)}؛ برای انتخاب دقیق، نوع کار و میزان استفادهٔ روزانه را در نظر بگیرید.</p></div><div><i class="bi bi-lightning-charge" aria-hidden="true"></i><strong>نوع عملکرد</strong><p>${esc(p.movement)}؛ سازگاری منبع تغذیه یا شیوهٔ عملکرد را پیش از خرید بررسی کنید.</p></div><div><i class="bi bi-rulers" aria-hidden="true"></i><strong>ساختار محصول</strong><p>${esc(p.strap)}؛ ابعاد، لوازم همراه و شرایط نصب را با فضای کارتان تطبیق دهید.</p></div></div></section><section class="product-detail-content-panel" id="product-specifications"><header><span>جزئیات فنی</span><h2>ویژگی‌های اصلی و دیگر ویژگی‌ها</h2></header><div class="product-spec-groups"><section class="product-spec-group"><h3><i class="bi bi-stars" aria-hidden="true"></i>ویژگی‌های اصلی</h3><dl><div><dt>دسته‌بندی</dt><dd>${esc(p.type)}</dd></div><div><dt>نوع عملکرد</dt><dd>${esc(p.movement)}</dd></div><div><dt>برند</dt><dd>${esc(p.brand)}</dd></div></dl></section><section class="product-spec-group"><h3><i class="bi bi-list-check" aria-hidden="true"></i>دیگر ویژگی‌ها</h3><dl>${specs.map(([label, value]) => `<div><dt>${esc(label)}</dt><dd>${esc(value)}</dd></div>`).join("")}</dl></section></div></section><section class="product-detail-content-panel" id="product-reviews"><header><span>تجربهٔ خریداران</span><h2>دیدگاه‌ها</h2></header><div class="product-feedback-list" id="productReviewList">${feedbackMarkup("reviews", readFeedback("reviews"))}</div><form class="product-feedback-form" data-feedback-type="reviews"><label>نام<input name="name" maxlength="60" required placeholder="نام شما"></label><label>امتیاز<select name="rating" required><option value="5">۵ — عالی</option><option value="4">۴ — خوب</option><option value="3">۳ — متوسط</option><option value="2">۲ — ضعیف</option><option value="1">۱ — خیلی ضعیف</option></select></label><label class="product-feedback-wide">دیدگاه شما<textarea name="text" rows="3" maxlength="800" required placeholder="تجربه یا نکتهٔ خود را بنویسید"></textarea></label><button class="product-feedback-submit" type="submit"><i class="bi bi-send" aria-hidden="true"></i>ثبت دیدگاه</button><small class="product-feedback-note">دیدگاه در این نسخه به‌صورت محلی در همین مرورگر ذخیره می‌شود.</small></form></section><section class="product-detail-content-panel" id="product-questions"><header><span>پاسخ به ابهام‌ها</span><h2>پرسش‌ها</h2></header><div class="product-feedback-list" id="productQuestionList">${feedbackMarkup("questions", readFeedback("questions"))}</div><form class="product-feedback-form product-question-form" data-feedback-type="questions"><label>نام<input name="name" maxlength="60" required placeholder="نام شما"></label><label class="product-feedback-wide">پرسش شما<textarea name="text" rows="3" maxlength="600" required placeholder="سؤال خود دربارهٔ محصول را بنویسید"></textarea></label><button class="product-feedback-submit" type="submit"><i class="bi bi-question-lg" aria-hidden="true"></i>ثبت پرسش</button><small class="product-feedback-note">پرسش در این نسخه به‌صورت محلی در همین مرورگر ذخیره می‌شود.</small></form></section><section class="product-detail-related" aria-labelledby="relatedProductsTitle"><header><span>برای انتخاب بهتر</span><h2 id="relatedProductsTitle">کالاهای مشابه</h2></header><div class="product-detail-related-grid">${suggestions.map((item) => `<article class="product-card product-detail-related-card"><a class="product-image" href="Product?id=${item.id}"><img src="${esc(item.image)}" alt="${esc(item.name)}" loading="lazy"></a><div class="product-info"><h3><a href="Product?id=${item.id}">${esc(item.name)}</a></h3><p>${money(item.price * 1000000)}</p></div><button class="add-cart" type="button" data-id="${item.id}" data-name="${esc(item.name)}" data-price="${item.price * 1000000}" data-image="${esc(item.image)}"><i class="bi bi-bag-plus" aria-hidden="true"></i>افزودن به سبد خرید</button></article>`).join("")}</div></section>`;
            const shareActions = document.createElement("div");
            shareActions.className = "product-share-control";
            shareActions.innerHTML = `<button class="product-detail-share" type="button" aria-haspopup="true" aria-expanded="false" aria-controls="productShareMenu"><i class="bi bi-share" aria-hidden="true"></i><span>اشتراک‌گذاری</span></button><div class="product-share-menu" id="productShareMenu" role="group" aria-label="اشتراک‌گذاری این محصول" hidden><strong>اشتراک‌گذاری محصول</strong><button class="product-share-native" type="button" data-product-share-native><i class="bi bi-phone" aria-hidden="true"></i>اشتراک‌گذاری دستگاه</button><a data-share-network="whatsapp" target="_blank" rel="noopener noreferrer"><i class="bi bi-whatsapp" aria-hidden="true"></i>واتساپ</a><a data-share-network="telegram" target="_blank" rel="noopener noreferrer"><i class="bi bi-telegram" aria-hidden="true"></i>تلگرام</a><a data-share-network="facebook" target="_blank" rel="noopener noreferrer"><i class="bi bi-facebook" aria-hidden="true"></i>فیس‌بوک</a><a data-share-network="x" target="_blank" rel="noopener noreferrer"><i class="bi bi-twitter-x" aria-hidden="true"></i>ایکس</a><a data-share-network="linkedin" target="_blank" rel="noopener noreferrer"><i class="bi bi-linkedin" aria-hidden="true"></i>لینکدین</a><a data-share-network="email"><i class="bi bi-envelope" aria-hidden="true"></i>ایمیل</a><button type="button" data-copy-product-link><i class="bi bi-link-45deg" aria-hidden="true"></i>کپی پیوند</button></div>`;
            productView
                .querySelector(".product-detail-secondary-actions")
                ?.append(shareActions);

            const shareTitle = p.name || document.title;
            const shareUrl = window.location.href;
            const shareText = `${shareTitle} | ${shareUrl}`;
            const shareUrlEncoded = encodeURIComponent(shareUrl);
            const shareTextEncoded = encodeURIComponent(shareTitle);
            const socialShareUrls = {
                whatsapp: `https://wa.me/?text=${encodeURIComponent(shareText)}`,
                telegram: `https://t.me/share/url?url=${shareUrlEncoded}&text=${shareTextEncoded}`,
                facebook: `https://www.facebook.com/sharer/sharer.php?u=${shareUrlEncoded}`,
                x: `https://twitter.com/intent/tweet?url=${shareUrlEncoded}&text=${shareTextEncoded}`,
                linkedin: `https://www.linkedin.com/sharing/share-offsite/?url=${shareUrlEncoded}`,
                email: `mailto:?subject=${shareTextEncoded}&body=${encodeURIComponent(shareUrl)}`,
            };
            const shareMenu = shareActions.querySelector(".product-share-menu");
            const shareTrigger = shareActions.querySelector(".product-detail-share");
            const nativeShareButton = shareActions.querySelector(
                "[data-product-share-native]",
            );
            nativeShareButton.hidden = typeof navigator.share !== "function";
            shareActions.querySelectorAll("[data-share-network]").forEach((link) => {
                link.href = socialShareUrls[link.dataset.shareNetwork];
            });
            shareTrigger.addEventListener("click", () => {
                const isOpen = shareTrigger.getAttribute("aria-expanded") === "true";
                shareTrigger.setAttribute("aria-expanded", String(!isOpen));
                shareMenu.hidden = isOpen;
            });
            nativeShareButton.addEventListener("click", async () => {
                try {
                    await navigator.share({ title: shareTitle, text: shareTitle, url: shareUrl });
                } catch (error) {
                    if (error?.name !== "AbortError") {
                        const status = productView.querySelector("#productStatus");
                        if (status) status.textContent = "اشتراک‌گذاری انجام نشد؛ یک روش دیگر را انتخاب کنید.";
                    }
                }
            });
            shareActions
                .querySelector("[data-copy-product-link]")
                .addEventListener("click", async () => {
                    let copied = false;
                    try {
                        if (navigator.clipboard?.writeText) {
                            await navigator.clipboard.writeText(shareUrl);
                            copied = true;
                        }
                    } catch {
                        copied = false;
                    }
                    if (!copied) {
                        const input = document.createElement("textarea");
                        input.value = shareUrl;
                        input.setAttribute("readonly", "");
                        input.style.position = "fixed";
                        input.style.opacity = "0";
                        document.body.append(input);
                        input.select();
                        copied = document.execCommand("copy");
                        input.remove();
                    }
                    const status = productView.querySelector("#productStatus");
                    if (status)
                        status.textContent = copied
                            ? "پیوند محصول کپی شد."
                            : "کپی خودکار در این مرورگر در دسترس نیست.";
                    ui.showToast(
                        copied
                            ? "پیوند محصول کپی شد."
                            : "کپی پیوند در این مرورگر انجام نشد.",
                        copied ? "success" : "error",
                    );
                    if (copied) {
                        shareMenu.hidden = true;
                        shareTrigger.setAttribute("aria-expanded", "false");
                        shareTrigger.focus();
                    }
                });
            document.addEventListener("click", (event) => {
                if (!shareActions.contains(event.target)) {
                    shareMenu.hidden = true;
                    shareTrigger.setAttribute("aria-expanded", "false");
                }
            });
            shareActions.addEventListener("keydown", (event) => {
                if (event.key === "Escape") {
                    shareMenu.hidden = true;
                    shareTrigger.setAttribute("aria-expanded", "false");
                    shareTrigger.focus();
                }
            });

            const inventoryValue =
                p.stockQuantity ??
                p.inventoryCount ??
                p.quantityInStock ??
                p.availableQuantity ??
                p.stock;
            const inventoryQuantity =
                typeof inventoryValue !== "number" &&
                    !(typeof inventoryValue === "string" && inventoryValue.trim())
                    ? Number.NaN
                    : Number(inventoryValue);
            const hasInventoryQuantity =
                Number.isFinite(inventoryQuantity) && inventoryQuantity >= 0;
            const inventoryAvailable = hasInventoryQuantity
                ? inventoryQuantity > 0
                : p.inStock === true;
            const inventoryUnavailable = hasInventoryQuantity
                ? inventoryQuantity === 0
                : p.inStock === false;
            const inventoryMessage = hasInventoryQuantity
                ? inventoryUnavailable
                    ? "۰ عدد · ناموجود"
                    : `${fa(inventoryQuantity)} عدد در انبار`
                : inventoryUnavailable
                    ? "در حال حاضر ناموجود"
                    : inventoryAvailable
                        ? "موجود در انبار"
                        : "برای اطلاع از موجودی تماس بگیرید";
            const inventoryState = inventoryAvailable
                ? "is-available"
                : inventoryUnavailable
                    ? "is-unavailable"
                    : "is-pending";
            const inventoryIcon = inventoryAvailable
                ? "bi-check2-circle"
                : inventoryUnavailable
                    ? "bi-x-circle"
                    : "bi-clock-history";
            const inventoryStatus = document.createElement("div");
            inventoryStatus.className = `product-stock-status ${inventoryState}`;
            inventoryStatus.setAttribute("role", "status");
            inventoryStatus.innerHTML = `<span class="product-stock-status-icon"><i class="bi bi-box-seam" aria-hidden="true"></i></span><span class="product-stock-status-copy"><strong>موجودی انبار</strong><small>${inventoryMessage}</small></span><i class="bi ${inventoryIcon} product-stock-status-mark" aria-hidden="true"></i>`;
            const updateInventoryStatus = () => {
                let quantity = inventoryQuantity;
                const variantKeys = Object.keys(p.variants || {});
                const allVariantsSelected =
                    variantKeys.length > 0 &&
                    variantKeys.every((key) => selectedVariants[key] !== undefined);
                if (allVariantsSelected && p.variantStock) {
                    const combinationKey = variantKeys
                        .map((key) => `${key}=${selectedVariants[key]}`)
                        .join("|");
                    if (Object.prototype.hasOwnProperty.call(p.variantStock, combinationKey))
                        quantity = Number(p.variantStock[combinationKey]);
                }
                const quantityKnown = Number.isFinite(quantity) && quantity >= 0;
                const available = quantityKnown ? quantity > 0 : p.inStock === true;
                const unavailable = quantityKnown ? quantity === 0 : p.inStock === false;
                const message = quantityKnown
                    ? unavailable
                        ? "۰ عدد · ناموجود"
                        : `${fa(quantity)} عدد در انبار`
                    : unavailable
                        ? "در حال حاضر ناموجود"
                        : available
                            ? "موجود در انبار"
                            : "برای اطلاع از موجودی تماس بگیرید";
                const state = available
                    ? "is-available"
                    : unavailable
                        ? "is-unavailable"
                        : "is-pending";
                const icon = available
                    ? "bi-check2-circle"
                    : unavailable
                        ? "bi-x-circle"
                        : "bi-clock-history";
                inventoryStatus.className = `product-stock-status ${state}`;
                inventoryStatus.innerHTML = `<span class="product-stock-status-icon"><i class="bi bi-box-seam" aria-hidden="true"></i></span><span class="product-stock-status-copy"><strong>موجودی انبار</strong><small>${message}</small></span><i class="bi ${icon} product-stock-status-mark" aria-hidden="true"></i>`;
                productView
                    .querySelectorAll("#addProduct")
                    .forEach((button) => {
                        button.disabled = unavailable;
                        button.setAttribute("aria-disabled", String(unavailable));
                    });
            };
            productView.querySelectorAll('a[href^="Product?id="]').forEach((link) => {
                const productId = link.getAttribute("href")?.match(/^Product\?id=(\d+)/i)?.[1];
                if (productId) link.href = window.HNProductUrl(productId);
            });
            const detail = p.detail || {};
            if (media.length === 1)
                productView.querySelector(
                    ".product-detail-gallery-caption > span",
                ).textContent = "تصویر محصول";
            const questionsPanel = productView.querySelector("#product-questions");
            const questionList = questionsPanel.querySelector("#productQuestionList");
            const questionForm = questionsPanel.querySelector(
                ".product-question-form",
            );
            const questionLayout = document.createElement("div");
            questionLayout.className = "product-question-layout";
            const questionFormColumn = document.createElement("div");
            questionFormColumn.className = "product-question-form-column";
            questionFormColumn.innerHTML =
                '<div class="product-question-form-heading"><span><i class="bi bi-chat-square-text" aria-hidden="true"></i></span><strong>پرسش خود را ثبت کنید</strong><small>سؤال‌تان را دربارهٔ این کالا بنویسید.</small></div>';
            questionFormColumn.append(questionForm);
            const questionListColumn = document.createElement("div");
            questionListColumn.className = "product-question-list-column";
            questionListColumn.append(questionList);
            const questionPagination = document.createElement("nav");
            questionPagination.className = "product-feedback-pagination";
            questionPagination.dataset.feedbackPagination = "questions";
            questionPagination.setAttribute("aria-label", "صفحه‌بندی پرسش‌ها");
            questionListColumn.append(questionPagination);
            questionLayout.append(questionFormColumn, questionListColumn);
            questionsPanel.querySelector("header").after(questionLayout);
            const decorateQuestionCards = (
                items = readFeedback("questions"),
                start = 0,
            ) => {
                productView
                    .querySelectorAll("#productQuestionList .product-feedback-card")
                    .forEach((card, index) =>
                        card.classList.toggle(
                            "product-question-faq",
                            Boolean(items[start + index]?.sample),
                        ),
                    );
            };
            decorateQuestionCards();
            questionForm.addEventListener("submit", () =>
                setTimeout(decorateQuestionCards, 0),
            );
            const reviewsPanel = productView.querySelector("#product-reviews");
            const reviewForm = reviewsPanel.querySelector(".product-feedback-form");
            const reviewList = reviewsPanel.querySelector("#productReviewList");
            const reviewLayout = document.createElement("div");
            reviewLayout.className = "product-review-layout";
            const reviewFormColumn = document.createElement("div");
            reviewFormColumn.className =
                "product-review-form-column product-question-form-column";
            reviewFormColumn.innerHTML =
                '<div class="product-question-form-heading"><span><i class="bi bi-chat-heart" aria-hidden="true"></i></span><strong>تجربه‌تان را بنویسید</strong><small>ثبت دیدگاه با نام دلخواه یا به‌صورت کاربر مهمان.</small></div>';
            reviewFormColumn.append(reviewForm);
            const reviewListColumn = document.createElement("div");
            reviewListColumn.className =
                "product-review-list-column product-question-list-column";
            reviewListColumn.append(reviewList);
            const reviewPagination = document.createElement("nav");
            reviewPagination.className = "product-feedback-pagination";
            reviewPagination.dataset.feedbackPagination = "reviews";
            reviewPagination.setAttribute("aria-label", "صفحه‌بندی دیدگاه‌ها");
            reviewListColumn.append(reviewPagination);
            reviewLayout.append(reviewFormColumn, reviewListColumn);
            reviewsPanel.querySelector("header").after(reviewLayout);
            productView
                .querySelectorAll('.product-feedback-form input[name="name"]')
                .forEach((input) => {
                    input.required = false;
                    input.placeholder = "اختیاری";
                    const label = input.closest("label");
                    if (label?.firstChild?.nodeType === Node.TEXT_NODE)
                        label.firstChild.textContent = "نام (اختیاری)";
                });
            if (accessoryMarkup) {
                const accessoryAnchor =
                    productView.querySelector(".product-variant-options") ||
                    productView.querySelector(".product-main-feature-chips");
                accessoryAnchor?.insertAdjacentHTML("afterend", accessoryMarkup);
            }
            const accessoriesSection = productView.querySelector(".product-accessories");
            if (accessoriesSection) accessoriesSection.before(inventoryStatus);
            else productView.querySelector(".product-detail-price")?.before(inventoryStatus);
            const purchaseTrustPanel = productView.querySelector(".product-price-context"),
                productGallery = productView.querySelector(".product-detail-gallery");
            if (purchaseTrustPanel && productGallery) {
                purchaseTrustPanel.classList.add("product-purchase-trust");
                purchaseTrustPanel.classList.remove("product-price-context");
                purchaseTrustPanel.setAttribute("aria-label", "مزیت‌های خرید مطمئن");
                purchaseTrustPanel.innerHTML =
                    '<div class="product-purchase-trust-heading"><span class="product-purchase-trust-mark"><i class="bi bi-shield-check" aria-hidden="true"></i></span><span><small>پیش از تصمیم‌گیری</small><strong>اطلاعات محصول را بررسی کنید</strong><p>مشخصات و شرایط ارائه‌شده برای همین کالا را مبنا قرار دهید.</p></span></div><ul class="product-purchase-trust-list"><li><i class="bi bi-list-check" aria-hidden="true"></i><span><strong>مشخصات فنی</strong><small>سازگاری فنی و اقلام همراه را بسنجید.</small></span></li><li><i class="bi bi-chat-dots" aria-hidden="true"></i><span><strong>پرسش پیش از خرید</strong><small>ابهام‌ها را پیش از خرید بپرسید.</small></span></li><li><i class="bi bi-box-seam" aria-hidden="true"></i><span><strong>شرایط سفارش</strong><small>تحویل و ضمانت سفارش را بررسی کنید.</small></span></li></ul>';
                const galleryColumn = document.createElement("div");
                galleryColumn.className = "product-detail-gallery-column";
                productGallery.replaceWith(galleryColumn);
                galleryColumn.append(productGallery, purchaseTrustPanel);
            }
            const productTitle = productView.querySelector("#productDetailTitle");
            if (p.englishName)
                productTitle.insertAdjacentHTML(
                    "afterend",
                    '<p class="product-detail-english-name" lang="en" dir="ltr">' +
                    esc(p.englishName) +
                    "</p>",
                );
            const productMeta = productView.querySelector(".product-detail-meta");
            const productBrandLogos = {
                Bosch: "bosch.svg",
                Makita: "makita.svg",
                DeWalt: "dewalt.svg",
                Ronix: "ronix.png",
                Tosan: "tosan.webp",
                "UNI-T": "uni-t.png",
                Fluke: "fluke.svg",
                Mastech: "mastech.png",
                Kyoritsu: "kyoritsu.png",
                Yihua: "yihua.png",
                Hakko: "hakko.png",
                Weller: "weller.svg",
                Philips: "philips.svg",
                Wiha: "wiha.svg",
            };
            const productBrandPersianNames = {
                Arva: "آروا",
                Benning: "بننینگ",
                Bosch: "بوش",
                DeWalt: "دیوالت",
                Fluke: "فلوک",
                GOOT: "گوت",
                Hakko: "هاکو",
                Knipex: "کنیپکس",
                Kyoritsu: "کیوریتسو",
                Makita: "ماکیتا",
                Mastech: "مستک",
                Philips: "فیلیپس",
                Quick: "کوئیک",
                Ronix: "رونیکس",
                Stanley: "استنلی",
                Tosan: "توسن",
                "UNI-T": "یونی‌تی",
                Victor: "ویکتور",
                Weller: "ولر",
                Wiha: "وایها",
                YIHUA: "یوهوا",
            };
            const brandPersianName = productBrandPersianNames[p.brand] || p.brand;
            const brandLogoFile = productBrandLogos[p.brand];
            const brandMark = brandLogoFile
                ? `<img class="product-meta-brand-logo" src="/content/brands/${brandLogoFile}" alt="" aria-hidden="true" decoding="async">`
                : '<i class="bi bi-patch-check" aria-hidden="true"></i>';
            const categoryPath =
                Array.isArray(p.categoryPath) && p.categoryPath.length
                    ? p.categoryPath
                    : ["ابزار و تجهیزات", p.type, p.type];
            const brandCountry = window.HNCountries?.[
                String(p.brandCountryCode || "").toUpperCase()
            ];
            const brandCountryMark = brandCountry
                ? `<img class="product-meta-country-flag" src="/content/countries/flags/${brandCountry.code.toLowerCase()}.svg" alt="" aria-hidden="true" loading="lazy" decoding="async">`
                : '<i class="bi bi-globe2" aria-hidden="true"></i>';
            const brandCountryContent = brandCountry
                ? `<span><small>کشور</small><strong>${esc(brandCountry.fa)}</strong><small class="product-meta-country-en" lang="en" dir="ltr">${esc(brandCountry.en)}</small></span>`
                : '<span><small>کشور</small><strong>ثبت نشده</strong></span>';
            const productBreadcrumb = document.querySelector(
                ".taxonomy-breadcrumb ol",
            );
            if (productBreadcrumb)
                productBreadcrumb.innerHTML = `<li><a href="/"><i class="bi bi-house-door" aria-hidden="true"></i><span>خانه</span></a></li>${categoryPath.map((part) => `<li><i class="bi bi-chevron-left" aria-hidden="true"></i><span>${esc(part)}</span></li>`).join("")}<li class="cart-breadcrumb-current"><i class="bi bi-chevron-left" aria-hidden="true"></i><span aria-current="page"><i class="bi bi-tools" aria-hidden="true"></i>${esc(p.name)}</span></li>`;
            productMeta.innerHTML =
                '<div class="product-meta-identity" role="group" aria-label="اطلاعات برند، کشور و کد محصول"><div class="product-meta-identity-top"><span class="product-meta-pill product-meta-brand">' +
                brandMark +
                '<span><small>برند</small><strong>' +
                esc(brandPersianName) +
                '</strong><small class="product-meta-brand-en" lang="en" dir="ltr">' +
                esc(p.brand) +
                '</small></span></span><span class="product-meta-pill product-meta-country">' +
                brandCountryMark +
                brandCountryContent +
                '</span></div><span class="product-meta-pill product-meta-code"><i class="bi bi-upc-scan" aria-hidden="true"></i><span><small>کد محصول</small><strong dir="ltr">HN-' +
                String(p.id).padStart(3, "0") +
                '</strong></span></span></div><span class="product-meta-pill product-meta-category"><i class="bi bi-grid" aria-hidden="true"></i><span class="product-meta-category-content"><small>مسیر دسته‌بندی</small><ol class="product-meta-path" aria-label="' +
                esc(categoryPath.join("، ")) +
                '">' +
                categoryPath.map((part) => "<li>" + esc(part) + "</li>").join("") +
                "</ol></span></span>";
            const decorateSalePrice = (node, item) => {
                const discount = Number(item?.discountPercent) || 0;
                const current = Math.round(Number(item?.price) * 1000000);
                if (!node || !current) return;
                node.innerHTML =
                    '<strong class="product-price-current">' + money(current) + "</strong>";
                node.classList.remove("has-discount");
                if (discount <= 0 || discount >= 100) return;
                node.classList.add("has-discount");
                const previous = Math.round(current / (1 - discount / 100));
                node.insertAdjacentHTML(
                    "beforeend",
                    '<span class="product-price-deal"><del class="price-old">' +
                    money(previous) +
                    '</del><b class="product-price-discount">٪' +
                    fa(discount) +
                    "</b></span>",
                );
            };
            const salePercent = Number(p.discountPercent) || 0;
            const priceHeading = productView.querySelector(
                ".product-detail-price > .product-price-heading",
            );
            const priceLabel = priceHeading?.querySelector("span");
            if (priceLabel) priceLabel.textContent = "تفکیک قیمت";
            priceHeading?.querySelector("small")?.remove();
            const featureChips = productView.querySelector(
                ".product-main-feature-chips",
            );
            if (featureChips) {
                featureChips.insertAdjacentHTML(
                    "beforebegin",
                    '<div class="product-main-feature-heading"><i class="bi bi-stars" aria-hidden="true"></i><span>ویژگی‌های کلیدی محصول</span></div>',
                );
                const featureHeading = featureChips.previousElementSibling;
                const featurePanel = document.createElement("section");
                featurePanel.className = "product-main-features";
                featurePanel.setAttribute("aria-labelledby", "productMainFeaturesTitle");
                const featureTitle = featureHeading?.querySelector("span");
                if (featureTitle) featureTitle.id = "productMainFeaturesTitle";
                featureHeading?.before(featurePanel);
                if (featureHeading) featurePanel.append(featureHeading);
                featurePanel.append(featureChips);
            }
            productView.querySelector(".product-detail-price > p")?.remove();
            const detailPrice = productView.querySelector("#productBasePrice");
            const saleMeta = productView.querySelector(".product-detail-sale-meta");
            if (salePercent > 0 && salePercent < 100 && detailPrice && saleMeta) {
                const current = Math.round(p.price * 1000000),
                    previous = Math.round(current / (1 - salePercent / 100));
                saleMeta.innerHTML =
                    '<del class="price-old">' +
                    money(previous) +
                    '</del><span class="product-price-discount">٪' +
                    fa(salePercent) +
                    " تخفیف کالا</span>";
            }
            productView
                .querySelectorAll(".product-detail-related-card")
                .forEach((card, index) => {
                    const item = suggestions[index];
                    decorateSalePrice(card.querySelector(".product-info > p"), item);
                });
            productView
                .querySelectorAll(".product-feedback-note")
                .forEach((note) => note.remove());
            productView.querySelector(".product-detail-description").textContent =
                detail.intro ||
                `محصول ${p.name} برای کاربرد ${p.type}؛ جزئیات نهایی به مدل انتخابی وابسته است.`;
            productView.querySelector("#product-intro > p").textContent =
                detail.intro || "اطلاعات تکمیلی این محصول به‌زودی ثبت می‌شود.";
            productView
                .querySelector("#product-intro > p")
                .insertAdjacentHTML(
                    "afterend",
                    `<ul class="product-detail-intro-points">${(detail.highlights || []).map((item) => `<li><i class="bi bi-check2-circle" aria-hidden="true"></i>${esc(item)}</li>`).join("")}</ul>`,
                );
            const introParagraph = productView.querySelector("#product-intro > p");
            const introHighlights = productView.querySelector(
                "#product-intro > .product-detail-intro-points",
            );
            if (introParagraph) {
                const introCopy = document.createElement("div");
                introCopy.className = "product-intro-copy";
                introCopy.id = "productIntroCopy";
                introParagraph.before(introCopy);
                introCopy.append(introParagraph);
                if (introHighlights) introCopy.append(introHighlights);

                const introToggle = document.createElement("button");
                introToggle.className = "product-intro-toggle";
                introToggle.type = "button";
                introToggle.setAttribute("aria-controls", introCopy.id);
                introToggle.setAttribute("aria-expanded", "false");
                introToggle.innerHTML = '<span>مشاهدهٔ همه</span><i class="bi bi-chevron-down" aria-hidden="true"></i>';
                introCopy.after(introToggle);

                const syncIntroToggle = () => {
                    const wasExpanded = introCopy.classList.contains("is-expanded");
                    introCopy.classList.remove("is-expanded");
                    const isOverflowing =
                        introCopy.scrollHeight > introCopy.clientHeight + 2;
                    if (wasExpanded && isOverflowing)
                        introCopy.classList.add("is-expanded");
                    else if (wasExpanded) {
                        introToggle.setAttribute("aria-expanded", "false");
                        introToggle.innerHTML = '<span>مشاهدهٔ همه</span><i class="bi bi-chevron-down" aria-hidden="true"></i>';
                    }
                    introCopy.classList.toggle(
                        "is-truncated",
                        isOverflowing && !introCopy.classList.contains("is-expanded"),
                    );
                    introToggle.hidden = !isOverflowing && !wasExpanded;
                };

                introToggle.addEventListener("click", () => {
                    const isExpanded = introToggle.getAttribute("aria-expanded") === "true";
                    introCopy.classList.toggle("is-expanded", !isExpanded);
                    introCopy.classList.toggle("is-truncated", isExpanded);
                    introToggle.setAttribute("aria-expanded", String(!isExpanded));
                    introToggle.innerHTML = isExpanded
                        ? '<span>مشاهدهٔ همه</span><i class="bi bi-chevron-down" aria-hidden="true"></i>'
                        : '<span>نمایش کمتر</span><i class="bi bi-chevron-up" aria-hidden="true"></i>';
                });

                let introResizeFrame = 0;
                window.addEventListener(
                    "resize",
                    () => {
                        window.cancelAnimationFrame(introResizeFrame);
                        introResizeFrame = window.requestAnimationFrame(syncIntroToggle);
                    },
                    { passive: true },
                );
                window.requestAnimationFrame(syncIntroToggle);
                if (document.fonts?.ready) document.fonts.ready.then(syncIntroToggle);
            }
            const expertPoints = (detail.highlights || []).filter(Boolean);
            const expertFallback = (detail.specs || [])
                .slice(0, 3)
                .map(([label, value]) => `${label}: ${value}`);
            const expertGuide = expertPoints.length ? expertPoints : expertFallback;
            productView.querySelector(".product-expert-grid").innerHTML = expertGuide
                .map(
                    (text, index) =>
                        `<div><i class="bi ${["bi-tools", "bi-lightbulb", "bi-shield-check"][index % 3]}" aria-hidden="true"></i><strong>${["کاربرد پیشنهادی", "نکتهٔ مهم پیش از خرید", "راهنمای استفاده و ایمنی"][index % 3]}</strong><p>${esc(text)}</p></div>`,
                )
                .join("");
            const expertPanel = productView.querySelector("#product-expert");
            const expertIntro = document.createElement("p");
            expertIntro.className = "product-expert-intro";
            expertIntro.textContent = `در این بخش، مشخصات ثبت‌شدهٔ ${p.name} را به زبان کاربردی مرور می‌کنیم تا روشن شود این کالا برای چه نوع کاری مناسب است و پیش از خرید چه نکاتی را باید بسنجید. این راهنما بر پایهٔ اطلاعات محصول تهیه شده و به‌معنای آزمون آزمایشگاهی یا بررسی حضوری کالا نیست.`;
            expertPanel?.querySelector(".product-expert-grid")?.before(expertIntro);
            const expertNote = document.createElement("aside");
            expertNote.className = "product-expert-note";
            expertNote.innerHTML = '<strong><i class="bi bi-clipboard-check" aria-hidden="true"></i>چطور از این بررسی استفاده کنید؟</strong><p>کاربری و حجم استفادهٔ خود را با پیشنهادها تطبیق دهید، سازگاری و مشخصات فنی را در بخش جزئیات فنی بررسی کنید و پیش از سفارش، اقلام همراه، شرایط ضمانت و موجودی همین مدل را از فروشگاه بپرسید.</p>';
            expertPanel?.querySelector(".product-expert-grid")?.after(expertNote);
            productView
                .querySelectorAll(
                    '.product-detail-tabs a[href="#product-reviews"],.product-detail-tabs a[href="#product-questions"]',
                )
                .forEach((tab) => {
                    const count = tab.querySelector("b");
                    if (count) count.textContent = fa(Number(count.textContent) || 0);
                });
            let activeMediaIndex = 0;
            const galleryDialog = document.createElement("dialog");
            galleryDialog.className = "product-gallery-dialog";
            galleryDialog.setAttribute("aria-labelledby", "productGalleryTitle");
            galleryDialog.innerHTML = `<div class="product-gallery-dialog-shell"><header class="product-gallery-dialog-head"><div><span>گالری محصول</span><h2 id="productGalleryTitle">${esc(p.name)}</h2></div><div class="product-gallery-dialog-tools"><span class="product-gallery-dialog-count" data-gallery-count>${fa(1)} از ${fa(media.length)}</span><button type="button" data-gallery-close aria-label="بستن گالری"><i class="bi bi-x-lg" aria-hidden="true"></i></button></div></header><div class="product-gallery-dialog-body"><div class="product-gallery-full-stage" id="productGalleryFullStage">${renderMedia(mainMedia)}</div><aside class="product-gallery-dialog-rail"><div class="product-gallery-rail-title"><strong>همهٔ تصاویر و ویدئوها</strong><span>${fa(media.length)} فایل</span></div><div class="product-gallery-full-thumbs">${media.map((item, index) => `<button class="product-gallery-full-thumb${index === 0 ? " is-active" : ""}" type="button" data-gallery-media-index="${index}" aria-label="${item.type === "video" ? `پخش ویدئوی ${fa(index + 1)}` : `نمایش تصویر ${fa(index + 1)}`}" aria-pressed="${index === 0}">${item.type === "video" ? `<span><img src="${esc(item.poster || p.image)}" alt="" loading="lazy"><i class="bi bi-play-fill" aria-hidden="true"></i></span>` : `<img src="${esc(item.src)}" alt="" loading="lazy">`}<b>${fa(index + 1)}</b></button>`).join("")}</div></aside></div></div>`;
            document.body.append(galleryDialog);
            const setActiveMedia = (index) => {
                const next = Number(index);
                if (!Number.isInteger(next) || !media[next]) return;
                activeMediaIndex = next;
                const item = media[next];
                productView.querySelector("#productDetailMedia").innerHTML =
                    renderMedia(item);
                galleryDialog.querySelector("#productGalleryFullStage").innerHTML =
                    renderMedia(item);
                productView
                    .querySelectorAll("[data-detail-media-index]")
                    .forEach((button) => {
                        const active = Number(button.dataset.detailMediaIndex) === next;
                        button.classList.toggle("is-active", active);
                        button.setAttribute("aria-pressed", String(active));
                    });
                galleryDialog
                    .querySelectorAll("[data-gallery-media-index]")
                    .forEach((button) => {
                        const active = Number(button.dataset.galleryMediaIndex) === next;
                        button.classList.toggle("is-active", active);
                        button.setAttribute("aria-pressed", String(active));
                    });
                galleryDialog.querySelector("[data-gallery-count]").textContent =
                    `${fa(next + 1)} از ${fa(media.length)}`;
            };
            const detailTabLinks = [
                ...productView.querySelectorAll(
                    '.product-detail-tabs a[href^="#product-"]',
                ),
            ];
            const detailTabSections = detailTabLinks
                .map((link) => ({
                    link,
                    section: productView.querySelector(link.getAttribute("href")),
                }))
                .filter((item) => item.section);
            let currentActiveDetailTab = null;
            const setActiveDetailTab = (activeLink) => {
                if (!activeLink) return;
                const changed = currentActiveDetailTab !== activeLink;
                currentActiveDetailTab = activeLink;
                detailTabLinks.forEach((link) => {
                    const active = link === activeLink;
                    link.classList.toggle("is-active", active);
                    if (active) link.setAttribute("aria-current", "location");
                    else link.removeAttribute("aria-current");
                });
                if (changed && window.matchMedia("(max-width: 575px)").matches) {
                    activeLink.scrollIntoView({
                        behavior: window.matchMedia("(prefers-reduced-motion: reduce)")
                            .matches
                            ? "auto"
                            : "smooth",
                        block: "nearest",
                        inline: "center",
                    });
                }
            };
            detailTabLinks.forEach((link) =>
                link.addEventListener("click", (event) => {
                    const target = productView.querySelector(link.getAttribute("href"));
                    if (!target) return;
                    event.preventDefault();
                    setActiveDetailTab(link);
                    history.replaceState(null, "", link.getAttribute("href"));
                    target.scrollIntoView({ behavior: "smooth", block: "start" });
                }),
            );
            let detailTabScrollFrame = 0;
            const syncActiveDetailTab = () => {
                detailTabScrollFrame = 0;
                if (!detailTabSections.length) return;
                const marker = Math.min(window.innerHeight * 0.34, 220);
                let current = null,
                    nearest = null,
                    nearestDistance = Infinity;
                detailTabSections.forEach((item) => {
                    const rect = item.section.getBoundingClientRect();
                    if (rect.top <= marker && rect.bottom > marker) current = item;
                    const distance =
                        rect.top > marker ? rect.top - marker : marker - rect.bottom;
                    if (distance < nearestDistance) {
                        nearest = item;
                        nearestDistance = distance;
                    }
                });
                setActiveDetailTab((current || nearest)?.link);
            };
            window.addEventListener(
                "scroll",
                () => {
                    if (!detailTabScrollFrame)
                        detailTabScrollFrame =
                            window.requestAnimationFrame(syncActiveDetailTab);
                },
                { passive: true },
            );
            window.addEventListener("resize", syncActiveDetailTab, { passive: true });
            syncActiveDetailTab();
            productView
                .querySelector("#openProductGallery")
                .addEventListener("click", () => {
                    productView.querySelector("#productDetailMedia video")?.pause();
                    galleryDialog.showModal();
                    setActiveMedia(activeMediaIndex);
                    galleryDialog.querySelector("[data-gallery-close]").focus();
                });
            galleryDialog
                .querySelector("[data-gallery-close]")
                .addEventListener("click", () => galleryDialog.close());
            galleryDialog.addEventListener("close", () => {
                galleryDialog.querySelector("#productGalleryFullStage video")?.pause();
            });
            productView
                .querySelectorAll("[data-detail-media-index]")
                .forEach((button) =>
                    button.addEventListener("click", () =>
                        setActiveMedia(button.dataset.detailMediaIndex),
                    ),
                );
            galleryDialog
                .querySelectorAll("[data-gallery-media-index]")
                .forEach((button) =>
                    button.addEventListener("click", () =>
                        setActiveMedia(button.dataset.galleryMediaIndex),
                    ),
                );
            const renderProductFeedback = (type) => {
                const items = readFeedback(type),
                    list = productView.querySelector(
                        type === "reviews" ? "#productReviewList" : "#productQuestionList",
                    ),
                    tab = productView.querySelector(
                        `.product-detail-tabs a[href="#product-${type === "reviews" ? "reviews" : "questions"}"] b`,
                    ),
                    pagination = productView.querySelector(
                        `[data-feedback-pagination="${type}"]`,
                    ),
                    pages = Math.max(1, Math.ceil(items.length / feedbackPageSize));
                feedbackPages[type] = Math.min(feedbackPages[type], pages);
                const start = (feedbackPages[type] - 1) * feedbackPageSize;
                if (list)
                    list.innerHTML = feedbackMarkup(
                        type,
                        items.slice(start, start + feedbackPageSize),
                    );
                if (type === "questions") decorateQuestionCards(items, start);
                if (tab) tab.textContent = fa(items.length);
                if (pagination)
                    pagination.innerHTML =
                        pages > 1
                            ? `<button type="button" data-feedback-page="${Math.max(1, feedbackPages[type] - 1)}" aria-label="صفحهٔ قبل" ${feedbackPages[type] === 1 ? "disabled" : ""}><i class="bi bi-chevron-right" aria-hidden="true"></i></button>${Array.from(
                                { length: pages },
                                (_, index) => index + 1,
                            )
                                .map(
                                    (page) =>
                                        `<button type="button" data-feedback-page="${page}" aria-label="صفحهٔ ${fa(page)}" ${page === feedbackPages[type] ? 'aria-current="page"' : ""}>${fa(page)}</button>`,
                                )
                                .join(
                                    "",
                                )}<button type="button" data-feedback-page="${Math.min(pages, feedbackPages[type] + 1)}" aria-label="صفحهٔ بعد" ${feedbackPages[type] === pages ? "disabled" : ""}><i class="bi bi-chevron-left" aria-hidden="true"></i></button>`
                            : "";
            };
            productView.addEventListener("click", (event) => {
                const button = event.target.closest(
                    "[data-feedback-pagination] button[data-feedback-page]",
                );
                if (!button) return;
                const type = button.closest("[data-feedback-pagination]").dataset
                    .feedbackPagination;
                feedbackPages[type] = Number(button.dataset.feedbackPage) || 1;
                renderProductFeedback(type);
                const feedbackList = productView.querySelector(
                    type === "reviews" ? "#productReviewList" : "#productQuestionList",
                );
                const feedbackSection = feedbackList?.closest(
                    ".product-detail-content-panel",
                );
                if (feedbackSection)
                    window.requestAnimationFrame(() =>
                        feedbackSection.scrollIntoView({
                            behavior: window.matchMedia("(prefers-reduced-motion: reduce)")
                                .matches
                                ? "auto"
                                : "smooth",
                            block: "start",
                        }),
                    );
            });
            productView.querySelectorAll(".product-feedback-form").forEach((form) =>
                form.addEventListener("submit", async (event) => {
                    event.preventDefault();
                    const type = form.dataset.feedbackType,
                        data = new FormData(form),
                        name = String(data.get("name") || "").trim() || "کاربر مهمان",
                        text = String(data.get("text") || "").trim();
                    if (!text) return;
                    const isReview = type === "reviews",
                        confirmed = await window.confirmFormSubmission({
                            title: isReview ? "ثبت دیدگاه برای این محصول؟" : "ثبت پرسش دربارهٔ این محصول؟",
                            message: isReview
                                ? "پس از تأیید، دیدگاه شما در همین مرورگر ثبت و نمایش داده می‌شود. از آماده بودن متن مطمئن شوید."
                                : "پس از تأیید، پرسش شما در همین مرورگر ثبت و نمایش داده می‌شود. از آماده بودن متن مطمئن شوید.",
                            confirmText: isReview ? "بله، دیدگاه ثبت شود" : "بله، پرسش ثبت شود",
                        });
                    if (!confirmed) return;
                    const key = `hn-product-${type}-${p.id}`,
                        items = readFeedback(type);
                    items.unshift({
                        name,
                        text,
                        rating: Number(data.get("rating")) || 5,
                        sample: false,
                    });
                    ui.storage.set(
                        key,
                        JSON.stringify(items.filter((item) => !item.sample).slice(0, 50)),
                    );
                    feedbackPages[type] = 1;
                    renderProductFeedback(type);
                    form.reset();
                    const status = document.querySelector("#productStatus");
                    if (status)
                        ui.setMessage(
                            status,
                            type === "reviews" ? "دیدگاه شما ثبت شد." : "پرسش شما ثبت شد.",
                            "success",
                        );
                }),
            );
            renderProductFeedback("reviews");
            renderProductFeedback("questions");
            const requiredVariantFields = [
                ...productView.querySelectorAll(
                    '.product-variant-field[data-required="true"]',
                ),
            ];
            const calculateVariantPrice = () =>
                Math.round(
                    (Number(p.price) +
                        Object.entries(selectedVariants).reduce((sum, [key, value]) => {
                            const option = p.variants?.[key]?.options.find(
                                (item) => item.value === value,
                            );
                            return sum + (Number(option?.priceDelta) || 0);
                        }, 0) +
                        (p.accessories || [])
                            .filter((item) => selectedAccessories.has(item.id))
                            .reduce((sum, item) => sum + (Number(item.price) || 0), 0)) *
                    1000000,
                );
            const updateVariantPrice = () => {
                const value = calculateVariantPrice();
                const formatted = money(value);
                const desktopPrice = productView.querySelector("#productFinalPrice");
                if (desktopPrice) desktopPrice.textContent = formatted;
                const addonsPrice = productView.querySelector("#productAddonsPrice");
                const basePrice = Math.round(Number(p.price) * 1000000);
                const adjustment = value - basePrice;
                if (addonsPrice)
                    addonsPrice.textContent =
                        adjustment > 0
                            ? money(adjustment)
                            : adjustment < 0
                                ? "کسر قیمت گزینه‌ها: " + money(Math.abs(adjustment))
                                : "بدون هزینهٔ افزوده";
                updateInventoryStatus();
            };
            updateVariantPrice();
            productView.querySelectorAll(".product-variant-field").forEach((field) =>
                field.addEventListener("click", (event) => {
                    const choice = event.target.closest(".product-variant-choice");
                    if (!choice) return;
                    const key = field.dataset.variantKey;
                    selectedVariants[key] = choice.dataset.value;
                    field
                        .querySelectorAll(".product-variant-choice")
                        .forEach((button) => {
                            button.setAttribute("aria-pressed", String(button === choice));
                            button.classList.toggle("is-selected", button === choice);
                        });
                    field.classList.remove("has-error");
                    updateVariantPrice();
                }),
            );
            productView
                .querySelectorAll("[data-accessory-id]:not(:disabled)")
                .forEach((input) =>
                    input.addEventListener("change", () => {
                        if (input.checked)
                            selectedAccessories.add(input.dataset.accessoryId);
                        else selectedAccessories.delete(input.dataset.accessoryId);
                        updateVariantPrice();
                    }),
                );
            const addProductToCart = (button) => {
                const missing = requiredVariantFields.find(
                    (field) => !selectedVariants[field.dataset.variantKey],
                );
                if (missing) {
                    missing.classList.add("has-error");
                    const featureLabel =
                        missing
                            .querySelector("legend")
                            ?.childNodes[0]?.textContent?.trim() || "ویژگی محصول";
                    const status = productView.querySelector("#productStatus");
                    ui.setMessage(
                        status,
                        `لطفاً ${featureLabel} را انتخاب کنید؛ این ویژگی روی قیمت تأثیر دارد.`,
                        "warning",
                    );
                    missing.scrollIntoView({ behavior: "smooth", block: "center" });
                    missing.querySelector(".product-variant-choice")?.focus();
                    return;
                }
                const chosen = Object.entries(selectedVariants).map(([key, value]) => {
                    const feature = p.variants[key];
                    const option = feature.options.find((item) => item.value === value);
                    return option
                        ? {
                            key,
                            label: feature.label,
                            value: option.label,
                            productValue: option.value,
                            priceDelta: Number(option.priceDelta) || 0,
                        }
                        : null;
                });
                const selectedAccessoryItems = (p.accessories || []).filter((item) =>
                    selectedAccessories.has(item.id),
                );
                ui.cart.add(
                    {
                        ...p,
                        id: String(p.id),
                        name: p.name,
                        price: calculateVariantPrice(),
                        options: {
                            variants: chosen.filter(Boolean),
                            accessories: selectedAccessoryItems.map((item) => ({
                                name: item.name,
                                price: item.price,
                                required: Boolean(item.required),
                            })),
                        },
                    },
                    1,
                    button,
                );
            };
            document.querySelector("#addProduct").onclick = (event) =>
                addProductToCart(event.currentTarget);
            document.querySelector("#saveProduct").onclick = (e) => {
                const button = e.currentTarget;
                if (!ui.auth.requireLogin(() => button.click())) return;
                let ids = favorites();
                ids = ids.includes(String(p.id))
                    ? ids.filter((i) => i !== String(p.id))
                    : [...ids, String(p.id)];
                ui.storage.set("hn-favorites", JSON.stringify(ids));
                const active = ids.includes(String(p.id));
                e.currentTarget.setAttribute("aria-pressed", String(active));
                e.currentTarget.setAttribute(
                    "aria-label",
                    active ? "حذف از علاقه‌مندی‌ها" : "ذخیره در علاقه‌مندی‌ها",
                );
                e.currentTarget.innerHTML = `<i class="bi ${active ? "bi-heart-fill" : "bi-heart"}" aria-hidden="true"></i><span>${active ? "ذخیره‌شده" : "علاقه‌مندی"}</span>`;
            };
        }
    }
    const couponKey = "hn-demo-coupon";
    let couponCode = ui.storage.get(couponKey, "") === "HADI10" ? "HADI10" : "";
    let couponMessage = "";
    let couponError = false;
    const normalizeCoupon = (value) =>
        ui.toEnglishDigits(value.trim()).toUpperCase();
    const quantityFeedback = new Map();
    function renderCart() {
        const items = ui.cart.read(),
            total = items.reduce((s, i) => s + i.price * i.quantity, 0);
        if (!items.length) {
            couponCode = "";
            couponMessage = "";
            ui.storage.remove(couponKey);
        }
        const discount = couponCode ? Math.floor(total * 0.1) : 0;
        const finalTotal = total - discount;
        const couponMarkup = `<form id="couponForm" class="flow-coupon"><label for="couponCode">کد تخفیف دارید؟</label><div class="flow-coupon-entry"><input id="couponCode" name="coupon" dir="ltr" placeholder="کد تخفیف" maxlength="32" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-describedby="couponHint couponStatus" aria-invalid="${couponError}" value="${couponCode}"><button class="flow-button" type="submit">اعمال کد</button></div><small id="couponHint">کد تخفیف <bdi>HADI10</bdi>؛ ۱۰٪ تخفیف با وارد کردن این کد</small><p id="couponStatus" class="flow-coupon-status${couponError ? " is-error" : ""}" role="status">${esc(couponMessage || (couponCode ? "کد HADI10 اعمال شده است." : ""))}</p>${couponCode ? '<button id="removeCoupon" class="flow-coupon-remove" type="button">حذف کد تخفیف</button>' : ""}</form>`;
        const totalsMarkup = `<div class="flow-total flow-discount"><span class="cart-line-label"><i class="bi bi-tags" aria-hidden="true"></i><span>مجموع تخفیف${couponCode ? " (۱۰٪)" : ""}</span></span><span>${money(discount)}</span></div><div class="flow-total flow-payable"><span class="cart-line-label"><i class="bi bi-wallet2" aria-hidden="true"></i><span>مبلغ نهایی محصولات<small>بدون هزینه ارسال</small></span></span><strong>${money(finalTotal)}</strong></div>`;
        const view = document.querySelector("#cartView");
        if (view) {
            const addressSection = document.querySelector(".cart-address-section");
            const paymentSection = document.querySelector(".cart-payment-section");
            if (addressSection) addressSection.remove();
            if (paymentSection) paymentSection.remove();
            view.innerHTML = items.length
                ? `<div class="flow-grid"><div class="cart-primary-column"><div class="cart-address-slot"></div><section class="flow-card cart-products-panel"><div class="cart-items-list">${items.map((i, n) => `<article class="flow-row cart-product-row"><div class="cart-product-identity"><img src="${esc(i.image)}" alt="${esc(i.name)}"><div class="cart-product-copy"><strong>${esc(i.name)}</strong>${cartOptionsMarkup(i)}<p class="cart-product-unit-price"><span>قیمت هر عدد</span><strong>${money(i.price)}</strong></p></div></div><div class="cart-item-actions"><div class="cart-quantity" role="group" aria-label="تعداد ${esc(i.name)}"><button type="button" data-minus="${n}" aria-label="کاهش تعداد ${esc(i.name)}" ${i.quantity === 1 ? "disabled" : ""}><i class="bi bi-dash" aria-hidden="true"></i></button><span class="cart-quantity-value">${fa(i.quantity)}</span><button type="button" data-plus="${n}" aria-label="افزایش تعداد ${esc(i.name)}" ${i.quantity >= 99 ? "disabled" : ""}><i class="bi bi-plus" aria-hidden="true"></i></button></div><button type="button" class="cart-item-remove" data-remove="${n}" aria-label="حذف ${esc(i.name)} از سبد"><i class="bi bi-trash3" aria-hidden="true"></i></button></div></article>`).join("")}</div></section></div><div class="cart-summary-column"><div class="cart-payment-slot"></div><aside class="flow-card cart-summary"><div class="cart-summary-heading"><span class="cart-summary-icon"><i class="bi bi-receipt" aria-hidden="true"></i></span><h2>خلاصه سبد خرید</h2></div><div class="cart-summary-lines"><div class="flow-total"><span class="cart-line-label"><i class="bi bi-box-seam" aria-hidden="true"></i><span>تعداد آیتم‌ها</span></span><strong>${fa(items.reduce((sum, item) => sum + item.quantity, 0))} عدد <small>(${fa(items.length)} مدل)</small></strong></div><div class="flow-total"><span class="cart-line-label"><i class="bi bi-receipt" aria-hidden="true"></i><span>جمع قیمت محصولات</span></span><strong>${money(total)}</strong></div>${totalsMarkup}</div>${couponMarkup}<p class="cart-shipping-note"><i class="bi bi-truck" aria-hidden="true"></i> هزینه ارسال هنگام تأیید سفارش مشخص می‌شود.</p><a class="flow-button cart-checkout-link" href="Checkout">مشاهده پیش‌فاکتور و پرداخت <i class="bi bi-arrow-left" aria-hidden="true"></i></a><a class="ui-inline-action cart-continue-link" href="Search"><i class="bi bi-tools" aria-hidden="true"></i><span>بازگشت به فروشگاه</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></aside></div></div>`
                : emptyCart();
            ui.setMessageType(
                view.querySelector("#couponStatus"),
                couponError ? "error" : couponCode ? "success" : "",
            );
            view
                .querySelectorAll(".cart-product-unit-price > span")
                .forEach((label) => {
                    label.textContent = "قیمت واحد";
                });
            view.querySelectorAll(".cart-product-row").forEach((row, index) => {
                const title = row.querySelector(".cart-product-copy > strong");
                const item = items[index];
                if (!title || !item) return;
                const link = document.createElement("a");
                link.className = "cart-product-title-link";
                link.href = `Product?id=${encodeURIComponent(item.id)}`;
                link.target = "_blank";
                link.rel = "noopener noreferrer";
                link.textContent = title.textContent;
                title.replaceWith(link);
            });
            if (items.length && addressSection) {
                const addressSlot = view.querySelector(".cart-address-slot");
                if (addressSlot) addressSlot.append(addressSection);
                else view.before(addressSection);
            }
            if (items.length && paymentSection) {
                const paymentSlot = view.querySelector(".cart-payment-slot");
                if (paymentSlot) paymentSlot.append(paymentSection);
                else view.before(paymentSection);
            }
            const returnToStore = view.querySelector(".cart-continue-link");
            if (returnToStore) {
                const actions = document.createElement("div");
                actions.className = "cart-summary-actions";
                returnToStore.before(actions);
                actions.append(returnToStore);

                const clearCart = document.createElement("button");
                clearCart.className = "ui-inline-action cart-clear-action";
                clearCart.type = "button";
                clearCart.innerHTML =
                    '<i class="bi bi-trash3" aria-hidden="true"></i><span>حذف همه اقلام</span>';
                clearCart.onclick = async () => {
                    const confirmed = await window.confirmFormSubmission?.({
                        title: "حذف همه اقلام سبد؟",
                        message: "همهٔ محصولات و تعداد انتخاب‌شده از سبد خرید پاک می‌شوند.",
                        confirmText: "بله، حذف شود",
                    });
                    if (confirmed) ui.cart.write([]);
                };
                actions.append(clearCart);
            }
            view
                .querySelectorAll("[data-plus]")
                .forEach((button) => {
                    button.disabled = false;
                });
            view
                .querySelectorAll("[data-remove],[data-plus],[data-minus]")
                .forEach((button) => {
                    const action = button.dataset.plus !== undefined ? "plus" : "minus";
                    const index = Number(button.dataset[action]);
                    const key =
                        button.dataset.remove === undefined
                            ? JSON.stringify([items[index].id, action])
                            : "";
                    if (key) {
                        button.dataset.quantityFeedback = key;
                        button.classList.toggle("is-clicked", quantityFeedback.has(key));
                    }
                    button.onclick = () => {
                        if (button.dataset.remove !== undefined) {
                            items.splice(Number(button.dataset.remove), 1);
                            ui.cart.write(items);
                        } else {
                            const item = items[index];
                            const nextQuantity =
                                item.quantity + (action === "plus" ? 1 : -1);
                            clearTimeout(quantityFeedback.get(key));
                            quantityFeedback.set(
                                key,
                                setTimeout(() => {
                                    quantityFeedback.delete(key);
                                    view
                                        .querySelectorAll("[data-quantity-feedback]")
                                        .forEach((current) => {
                                            if (current.dataset.quantityFeedback === key)
                                                current.classList.remove("is-clicked");
                                        });
                                }, 1000),
                            );
                            ui.cart.update(item.id, nextQuantity);
                        }
                    };
                });
        }
        document
            .querySelector("#couponForm")
            ?.addEventListener("submit", (event) => {
                event.preventDefault();
                const input = document.querySelector("#couponCode");
                const value = normalizeCoupon(input.value);
                if (value !== "HADI10") {
                    couponError = true;
                    couponMessage = value
                        ? "کد تخفیف معتبر نیست. کد واردشده را بررسی کنید."
                        : "لطفاً کد تخفیف را وارد کنید.";
                    input.setAttribute("aria-invalid", "true");
                    const status = document.querySelector("#couponStatus");
                    status.textContent = couponMessage;
                    status.classList.add("is-error");
                    ui.setMessageType(status, "error");
                    input.focus();
                    return;
                }
                couponMessage = couponCode
                    ? "این کد قبلاً اعمال شده است."
                    : "کد تخفیف اعمال شد؛ ۱۰٪ از جمع محصولات کم شد.";
                couponCode = value;
                couponError = false;
                ui.storage.set(couponKey, couponCode);
                renderCart();
                ui.setMessageType(
                    document.querySelector("#couponStatus"),
                    couponMessage.startsWith("این کد قبلاً") ? "info" : "success",
                );
                document.querySelector("#couponCode")?.focus();
            });
        document.querySelector("#removeCoupon")?.addEventListener("click", () => {
            couponCode = "";
            couponError = false;
            couponMessage = "کد تخفیف حذف شد.";
            ui.storage.remove(couponKey);
            renderCart();
            ui.setMessageType(document.querySelector("#couponStatus"), "success");
            document.querySelector("#couponCode")?.focus();
        });
        const summary = document.querySelector("#orderSummary");
        if (summary) {
            summary.innerHTML = `<div class="cart-summary-heading"><span class="cart-summary-icon"><i class="bi bi-receipt" aria-hidden="true"></i></span><h2>خلاصه پیش‌فاکتور</h2></div><div class="cart-summary-lines"><div class="flow-total"><span>تعداد کالاها</span><strong>${fa(items.reduce((sum, item) => sum + item.quantity, 0))} عدد</strong></div><div class="flow-total"><span>جمع محصولات</span><strong>${money(total)}</strong></div>${totalsMarkup}</div>`;
            const invoice = document.querySelector("#invoiceItems");
            if (invoice)
                invoice.innerHTML = items.length
                    ? items
                        .map(
                            (item) =>
                                `<article class="invoice-item"><img src="${esc(item.image)}" alt="${esc(item.name)}"><div class="invoice-item-details"><strong><a class="invoice-product-title" href="${window.HNProductUrl(item.id)}" target="_blank" rel="noopener noreferrer">${esc(item.name)}</a></strong>${cartOptionsMarkup(item)}<div class="invoice-item-pricing"><small class="invoice-item-quantity-price">${fa(item.quantity)} عدد × ${money(item.price)}</small><strong class="invoice-item-line-total"><span>جمع این کالا</span><b>${money(item.price * item.quantity)}</b></strong></div></div></article>`,
                        )
                        .join("")
                    : "<p>سبد خرید شما خالی است.</p>";
        }
        document
            .querySelectorAll(".page-cart-toggle b,#cartCount,#searchCartCount")
            .forEach(
                (el) =>
                    (el.textContent = fa(items.reduce((s, i) => s + i.quantity, 0))),
            );
    }
    renderCart();
    document.addEventListener("hn:cart-change", renderCart);
    window.addEventListener("storage", renderCart);
    document.querySelector("#trackingForm")?.addEventListener("submit", (e) => {
        e.preventDefault();
        ui.setMessage(
            document.querySelector("#trackingStatus"),
            "استعلام آنلاین فعال نیست. برای دریافت وضعیت واقعی سفارش با پشتیبانی 03969455457 تماس بگیرید.",
            "warning",
        );
    });
    function renderFavorites() {
        const view = document.querySelector("#favoritesView");
        if (!view) return;
        const cache = ui.storage.json("hn-product-cache", {});
        const list = favorites()
            .map((id) => catalog.find((p) => String(p.id) === id) || cache[id])
            .filter(Boolean);
        view.innerHTML = list.length
            ? `<div class="flow-cards">${list.map((p) => `<article class="flow-card favorite-item-card"><a href="${window.HNProductUrl(p)}"><img class="flow-photo" src="${esc(p.image)}" alt="${esc(p.name)}"><h2>${esc(p.name)}</h2></a><p class="favorite-item-price${Number(p.discountPercent) > 0 ? " has-discount" : ""}">${salePriceMarkup(p)}</p><button class="list-remove-button" type="button" data-unlike="${esc(p.id)}" aria-label="حذف ${esc(p.name)} از علاقه‌مندی‌ها"><i class="bi bi-trash3" aria-hidden="true"></i><span>حذف از علاقه‌مندی‌ها</span></button></article>`).join("")}</div>`
            : empty("هنوز محصولی به علاقه‌مندی‌ها اضافه نکرده‌اید");
        view.querySelectorAll("[data-unlike]").forEach(
            (b) =>
            (b.onclick = () => {
                ui.storage.set(
                    "hn-favorites",
                    JSON.stringify(
                        favorites().filter((id) => id !== String(b.dataset.unlike)),
                    ),
                );
                renderFavorites();
            }),
        );
    }
    renderFavorites();
    const renderCompare = () => {
        const view = document.querySelector("#compareView");
        if (!view) return;
        const savedCompare = ui.storage.json("hn-compare", []);
        const demoIds = ["1", "2", "6", "7", "11", "13", "16", "18", "21", "22"];
        const legacyDemoIds = catalog
            .slice(0, 3)
            .map((product) => String(product.id));
        if (
            Array.isArray(savedCompare) &&
            savedCompare.map(String).join(",") === legacyDemoIds.join(",") &&
            ui.storage.get("hn-compare-demo-seeded", "") === "1"
        ) {
            ui.storage.set(
                "hn-compare",
                JSON.stringify(
                    demoIds.filter((id) =>
                        catalog.some((product) => String(product.id) === id),
                    ),
                ),
            );
        }
        if (
            (!Array.isArray(savedCompare) || savedCompare.length === 0) &&
            ui.storage.get("hn-compare-demo-seeded", "__HN_EMPTY__") ===
            "__HN_EMPTY__"
        ) {
            ui.storage.set(
                "hn-compare",
                JSON.stringify(
                    demoIds.filter((id) =>
                        catalog.some((product) => String(product.id) === id),
                    ),
                ),
            );
            ui.storage.set("hn-compare-demo-seeded", "1");
        }
        const ids = ui.storage.json("hn-compare", []).map(String);
        const cache = ui.storage.json("hn-product-cache", {});
        const list = ids
            .map(
                (id) =>
                    catalog.find((product) => String(product.id) === id) || cache[id],
            )
            .filter(Boolean);
        const groups = new Map();
        list.forEach((product) => {
            const category =
                product.categoryPath?.[1] || product.type || "ابزار و تجهیزات";
            if (!groups.has(category)) groups.set(category, []);
            groups.get(category).push(product);
        });
        const groupIcons = {
            "ابزار برقی": "bi-lightning-charge",
            "ابزار اندازه‌گیری": "bi-speedometer2",
            "لحیم‌کاری و تعمیر برد": "bi-cpu",
            "روشنایی و لامپ": "bi-lightbulb",
            "ابزار برق‌کار": "bi-tools",
            "تستر و ایمنی برق": "bi-shield-check",
        };
        const renderCard = (product, index) => {
            const specs = [
                ["دسته‌بندی", product.type || "ابزار و تجهیزات"],
                ["منبع تغذیه", product.powerSource || product.movement || "مطابق مشخصات کالا"],
                ["مورد استفاده", product.useCase || product.strap || "کارگاهی"],
                ...(product.detail?.specs || []).slice(0, 3),
            ];
            return `<article class="compare-product-card"><div class="compare-card-top"><span>گزینهٔ ${fa(index + 1)}</span><button class="compare-remove-icon" type="button" data-compare-remove-page="${esc(product.id)}" aria-label="حذف ${esc(product.name)} از مقایسه"><i class="bi bi-trash3" aria-hidden="true"></i></button></div><div class="compare-product-image"><img src="${esc(product.image)}" alt="${esc(product.name)}" loading="lazy"></div><div class="compare-product-meta"><span>${esc(product.brand || "برند منتخب")}</span><small><i class="bi bi-grid-3x3-gap" aria-hidden="true"></i>${esc(product.type || "ابزار و تجهیزات")}</small></div><h2><a class="compare-product-link" href="${window.HNProductUrl(product)}">${esc(product.name)}</a></h2><div class="compare-price"><small>قیمت محصول</small><div class="compare-price-values${Number(product.discountPercent) > 0 ? " has-discount" : ""}">${salePriceMarkup(product)}</div></div><h3 class="compare-spec-title"><i class="bi bi-list-check" aria-hidden="true"></i>مشخصات قابل بررسی</h3><dl>${specs.map(([label, value]) => `<div><dt>${esc(label)}</dt><dd>${esc(value)}</dd></div>`).join("")}</dl><a class="compare-detail-link" href="${window.HNProductUrl(product)}">جزئیات محصول<i class="bi bi-arrow-left" aria-hidden="true"></i></a></article>`;
        };
        view.innerHTML = list.length
            ? `<header class="compare-overview"><span class="compare-overview-icon"><i class="bi bi-columns-gap" aria-hidden="true"></i></span><span class="compare-overview-copy"><strong>مقایسهٔ دسته‌بندی‌شدهٔ محصولات</strong><small>هر گروه را جداگانه بررسی کنید تا انتخاب دقیق‌تری داشته باشید.</small></span><span class="compare-overview-metrics" aria-label="${fa(list.length)} محصول در ${fa(groups.size)} گروه"><span class="compare-overview-metric"><i class="bi bi-box-seam" aria-hidden="true"></i><b>${fa(list.length)}</b><small>محصول</small></span><span class="compare-overview-metric"><i class="bi bi-grid-3x3-gap" aria-hidden="true"></i><b>${fa(groups.size)}</b><small>گروه</small></span></span><button class="compare-clear-all" type="button" data-compare-clear-all><i class="bi bi-trash3" aria-hidden="true"></i>حذف همه</button></header><div class="compare-category-groups">${[...groups.entries()].map(([category, products]) => `<section class="compare-category-group${products.length === 1 ? " has-single-product" : ""}" aria-label="مقایسهٔ ${esc(category)}"><header class="compare-category-heading"><span class="compare-category-icon"><i class="bi ${groupIcons[category] || "bi-grid"}" aria-hidden="true"></i></span><span><strong>${esc(category)}</strong><small>مقایسهٔ محصولات هم‌دسته</small></span><b>${fa(products.length)} محصول</b></header><div class="compare-product-grid">${products.map(renderCard).join("")}</div></section>`).join("")}</div>`
            : `<div class="compare-empty empty-state-card"><div class="compare-empty-art" aria-hidden="true"><span><i class="bi bi-columns-gap"></i></span><i class="bi bi-plus-lg"></i></div><span class="compare-empty-kicker">مقایسهٔ هوشمند محصولات</span><h2>هنوز محصولی برای مقایسه ندارید</h2><p>محصول‌های موردنظرتان را از صفحهٔ فروشگاه انتخاب کنید؛ مشخصاتشان را اینجا کنار هم و یک‌جا بررسی می‌کنید.</p><a class="flow-button" href="Search"><i class="bi bi-search" aria-hidden="true"></i> جست‌وجوی محصولات <i class="bi bi-arrow-left" aria-hidden="true"></i></a><small class="compare-empty-hint"><i class="bi bi-lightbulb" aria-hidden="true"></i> برای افزودن، آیکن مقایسه را روی کارت هر محصول بزنید.</small></div>`;
        view.querySelectorAll("[data-compare-remove-page]").forEach(
            (button) =>
            (button.onclick = () => {
                const next = ui.storage
                    .json("hn-compare", [])
                    .map(String)
                    .filter((id) => id !== button.dataset.compareRemovePage);
                ui.storage.set("hn-compare", JSON.stringify(next));
                renderCompare();
            }),
        );
        view.querySelector("[data-compare-clear-all]")?.addEventListener(
            "click",
            async () => {
                const confirmed = await window.confirmFormSubmission?.({
                    title: "حذف همهٔ محصولات مقایسه؟",
                    message: "فهرست مقایسه پاک می‌شود و این محصولات دیگر در این صفحه نمایش داده نمی‌شوند.",
                    confirmText: "بله، حذف همه",
                });
                if (!confirmed) return;
                ui.storage.set("hn-compare", "[]");
                document.dispatchEvent(new CustomEvent("hn:lists-change"));
                window.setTimeout(() => {
                    const toast = document.querySelector("#liveToast");
                    if (!toast) return;
                    ui.showToast("فهرست مقایسه با موفقیت پاک شد.", "success");
                }, 240);
            },
        );
    };
    renderCompare();
    document.addEventListener("hn:lists-change", () => {
        renderFavorites();
        renderCompare();
    });
});
