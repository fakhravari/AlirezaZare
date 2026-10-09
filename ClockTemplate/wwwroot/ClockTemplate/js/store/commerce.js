document.addEventListener('DOMContentLoaded', () => {
  const ui=window.HNUI, catalog=window.HNProducts || [];
  const fa=n=>ui.formatNumber(n);
  const esc=s=>ui.escapeHtml(s);
  const homeEnglishNames = {'هاله رز':'Haleh Rose','نوا کرونو شب':'Nova Chrono Night','نوا کرونو، نسخه شب':'Nova Chrono Night Edition','نوا نسخه شب':'Nova Night Edition','نوا استیل سرمه‌ای':'Nova Navy Steel','آرمان بند چرمی':'Arman Leather','آبان مینیمال':'Aban Minimal','میراث کلاسیک':'Miras Classic','آبان بند مش':'Aban Mesh','آرمان رزگلد':'Arman Rose Gold','دماوند اسپرت':'Damavand Sport','مهتاب سیلور':'Mahtab Silver','میراث چرمی':'Miras Leather','هاله کلاسیک':'Haleh Classic'};
  const englishProductName = item => catalog.find(product => product.name === item.name)?.englishName || homeEnglishNames[item.name] || '';
  const money=n=>`${fa(n)} تومان`;
  const empty=(text)=>`<div class="flow-card flow-empty"><i class="bi bi-bag"></i><h2>${text}</h2><a class="flow-button" href="Search">مشاهده ساعت‌ها</a></div>`;
  const emptyCart=()=>`<section class="cart-empty-state" aria-labelledby="emptyCartTitle"><div class="cart-empty-ornament" aria-hidden="true"><span><i class="bi bi-bag"></i></span></div><div class="cart-empty-copy"><span class="cart-empty-kicker">سبد خرید</span><h2 id="emptyCartTitle">سبد خرید شما خالی است</h2><p>هنوز ساعتی برای بررسی و خرید انتخاب نکرده‌اید. از میان مدل‌های منتخب، انتخابتان را شروع کنید.</p><div class="cart-empty-actions"><a class="flow-button" href="Search"><i class="bi bi-watch" aria-hidden="true"></i><span>مشاهده ساعت‌ها</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a><a class="ui-inline-action" href="Magazine"><i class="bi bi-journal-text" aria-hidden="true"></i><span>راهنمای انتخاب</span></a></div><div class="cart-empty-benefits"><span><i class="bi bi-patch-check" aria-hidden="true"></i>ضمانت اصالت</span><span><i class="bi bi-truck" aria-hidden="true"></i>ارسال به سراسر ایران</span></div></div></section>`;
  const favorites=()=>{try{const v=JSON.parse(ui.storage.get('hn-favorites','[]'));return Array.isArray(v)?[...new Set(v.map(String))]:[]}catch{return []}};
  const routes={'پیگیری سفارش':'Tracking','شرایط ارسال':'Shipping','ضمانت اصالت':'Warranty','پرسش‌های متداول':'Faq','پروفایل من':'Profile','سفارش‌های من':'Profile/Orders','آدرس‌های من':'Profile/Addresses','لیست علاقه‌مندی‌ها':'Profile/Favorites','دیدگاه‌ها و پرسش‌ها':'Profile/Questions','مشاهده سبد و ادامه خرید':'Cart','مشاهده همه برندها':'Brands','مشاهده همه مطالب':'Magazine','درباره مجموعه':'About','تماس با ما':'Contact'};
  function connect(){
    document.querySelectorAll('a').forEach(a=>{
      const t=a.textContent.trim().replace(/\s+/g,' ');
      for(const [label,url] of Object.entries(routes)) if(t===label || (t.startsWith(label)&&t.length<label.length+5)) a.setAttribute('href',url);
    });
    document.querySelectorAll('#cartFooter a,.page-cart-footer>a,#searchCartDrawer .search-cart-footer a').forEach(a=>a.href='Cart');
    document.querySelectorAll('.product-card').forEach(card=>{
      if(card.querySelector('a[href^="Product"]'))return;
      const name=card.querySelector('h3');
      if(!name)return;
      const p=catalog.find(p=>name.textContent.trim().includes(p.name));
      if(p){const a=document.createElement('a');a.href=`Product?id=${p.id}`;a.textContent=name.textContent;name.replaceChildren(a);}
    });
  }
  connect();
  window.addEventListener('load',connect);
  document.querySelector('#searchCartFooter button')?.addEventListener('click',()=>{location.href='Cart';});
  const productView=document.querySelector('#productView');
  if(productView){
    const p=catalog.find(p=>p.id===Number(new URLSearchParams(location.search).get('id')));
    if(!p)productView.innerHTML=empty('محصول پیدا نشد');
    else{
      document.title=`${p.name} | علی‌رضا زارع`;
      const isFavorite=favorites().includes(String(p.id));
      const images=Array.isArray(p.images)&&p.images.length?p.images.filter(Boolean):[p.image];
      const videos=(Array.isArray(p.videos)?p.videos:[]).map(video=>typeof video==='string'?{src:video}:video).filter(video=>video?.src);
      const media=[...images.map(src=>({type:'image',src})),...videos.map(video=>({type:'video',src:video.src,poster:video.poster||p.image,title:video.title||`ویدئوی ${p.name}`}))];
      const mainMedia=media[0]||{type:'image',src:p.image};
      const renderMedia=item=>item.type==='video'?`<video class="product-detail-image" controls preload="metadata" poster="${esc(item.poster||p.image)}"><source src="${esc(item.src)}"></video>`:`<img class="product-detail-image" src="${esc(item.src)}" alt="${esc(p.name)}" fetchpriority="high">`;
      const thumbMarkup=media.length>1?`<div class="product-detail-thumbnails" role="group" aria-label="انتخاب تصویر یا ویدئو">${media.map((item,index)=>`<button class="product-detail-thumb${index===0?' is-active':''}" type="button" data-detail-media-index="${index}" aria-label="${item.type==='video'?esc(item.title):`تصویر ${fa(index+1)}`}" aria-pressed="${index===0}">${item.type==='video'?`<span><i class="bi bi-play-fill" aria-hidden="true"></i></span>`:`<img src="${esc(item.src)}" alt="" loading="lazy">`}</button>`).join('')}</div>`:'';
      const specs=[['برند',p.brand],['دسته‌بندی',p.type],['نوع تغذیه',p.movement],['کاربری/ساختار',p.strap],['شناسه محصول',`HN-${String(p.id).padStart(3,'0')}`],...(p.detail?.specs||[])];
      const related=catalog.filter(item=>item.id!==p.id&&(item.type===p.type||item.brand===p.brand)).slice(0,4);
      const suggestions=related.length?related:catalog.filter(item=>item.id!==p.id).slice(0,4);
      const feedbackPages={reviews:1,questions:1},feedbackPageSize=4;
      const readFeedback=type=>{const seeded=(p.detail?.[type]||[]).map(([name,text,rating,reply],index)=>({name:'کاربر مهمان',text,rating:rating||0,reply:type==='questions'?(p.id===1&&[0,3,8].includes(index)?reply||'':''):reply||'',sample:true}));try{const raw=ui.storage.get(`hn-product-${type}-${p.id}`,'__HN_EMPTY__');if(raw==='__HN_EMPTY__')return seeded;const value=JSON.parse(raw);return [...(Array.isArray(value)?value.filter(item=>item&&typeof item.text==='string').map(item=>({...item,name:item.name?.trim()||'کاربر مهمان',sample:false})):[]),...seeded]}catch{return seeded}};
      const feedbackMarkup=(type,items)=>items.length?items.map(item=>`<article class="product-feedback-card"><div class="product-feedback-meta"><img class="product-feedback-avatar" src="ClockTemplate/images/guest-avatar.svg" alt="" loading="lazy"><strong>${esc(item.name?.trim()||'کاربر مهمان')}</strong>${type==='reviews'?`<span class="product-feedback-stars" aria-label="امتیاز ${fa(item.rating)} از ۵">${'★'.repeat(Math.max(1,Math.min(5,Number(item.rating)||5)))}</span>`:''}</div><p>${esc(item.text)}</p>${item.reply?`<aside class="product-feedback-admin-reply"><div class="product-feedback-admin-meta"><span><i class="bi bi-patch-check-fill" aria-hidden="true"></i>پاسخ مدیر فروشگاه</span><small>پاسخ رسمی</small></div><p>${esc(item.reply)}</p></aside>`:''}</article>`).join(''):`<div class="product-feedback-empty"><i class="bi ${type==='reviews'?'bi-chat-square-heart':'bi-question-circle'}" aria-hidden="true"></i><strong>${type==='reviews'?'هنوز دیدگاهی برای این محصول ثبت نشده است.':'هنوز پرسشی دربارهٔ این محصول ثبت نشده است.'}</strong><span>${type==='reviews'?'تجربهٔ استفاده‌تان را با خریداران دیگر به اشتراک بگذارید.':'سؤال خود را ثبت کنید تا اطلاعات این کالا کامل‌تر شود.'}</span></div>`;
      productView.innerHTML=`<div class="product-detail-layout"><section class="product-detail-gallery" aria-label="تصاویر و ویدئوهای محصول"><div class="product-detail-image-frame"><span class="product-detail-image-code"><i class="bi bi-upc-scan" aria-hidden="true"></i> HN-${String(p.id).padStart(3,'0')}</span><div class="product-detail-media-frame" id="productDetailMedia">${renderMedia(mainMedia)}</div></div>${thumbMarkup}<div class="product-detail-gallery-caption"><i class="bi bi-images" aria-hidden="true"></i><span>تصاویر و ویدئوهای محصول</span><small>${media.length>1?`${fa(media.length)} فایل رسانه‌ای`:'نمای کامل محصول'}</small></div></section><section class="product-detail-summary" aria-labelledby="productDetailTitle"><div class="product-detail-meta"><span><i class="bi bi-patch-check" aria-hidden="true"></i>${esc(p.brand)}</span><span>کد کالا: HN-${String(p.id).padStart(3,'0')}</span></div><h2 id="productDetailTitle">${esc(p.name)}</h2><p class="product-detail-description">${esc(p.name)} از گروه ${esc(p.type)}، با ویژگی ${esc(p.movement)} و ساختار ${esc(p.strap)}.</p><div class="product-main-feature-chips" aria-label="ویژگی‌های اصلی"><span><i class="bi bi-grid" aria-hidden="true"></i>${esc(p.type)}</span><span><i class="bi bi-lightning-charge" aria-hidden="true"></i>${esc(p.movement)}</span><span><i class="bi bi-building" aria-hidden="true"></i>${esc(p.brand)}</span></div><div class="product-detail-price"><div><span>قیمت محصول</span><small>قیمت کاتالوگ نمایشی</small></div><strong>${money(p.price*1000000)}</strong><p><i class="bi bi-info-circle" aria-hidden="true"></i> برای تأیید قیمت روز و موجودی با فروشگاه تماس بگیرید.</p></div><div class="product-detail-actions"><button class="product-detail-add flow-button" id="addProduct" type="button"><i class="bi bi-bag-plus" aria-hidden="true"></i> افزودن به سبد خرید</button><div class="product-detail-secondary-actions"><a href="Cart"><i class="bi bi-basket3" aria-hidden="true"></i> مشاهده سبد خرید</a><button class="product-detail-favorite" id="saveProduct" type="button" aria-pressed="${isFavorite}" aria-label="${isFavorite?'حذف از علاقه‌مندی‌ها':'ذخیره در علاقه‌مندی‌ها'}"><i class="bi ${isFavorite?'bi-heart-fill':'bi-heart'}" aria-hidden="true"></i><span>${isFavorite?'ذخیره‌شده':'علاقه‌مندی'}</span></button></div></div><p class="product-detail-status" id="productStatus" role="status" aria-live="polite"></p></section></div><nav class="product-detail-tabs" aria-label="بخش‌های اطلاعات محصول"><a href="#product-intro"><i class="bi bi-info-circle" aria-hidden="true"></i>معرفی</a><a href="#product-expert"><i class="bi bi-journal-richtext" aria-hidden="true"></i>بررسی تخصصی</a><a href="#product-specifications"><i class="bi bi-list-check" aria-hidden="true"></i>مشخصات</a><a href="#product-reviews"><i class="bi bi-chat-heart" aria-hidden="true"></i>دیدگاه‌ها <b>${fa(readFeedback('reviews').length)}</b></a><a href="#product-questions"><i class="bi bi-question-circle" aria-hidden="true"></i>پرسش‌ها <b>${fa(readFeedback('questions').length)}</b></a></nav><section class="product-detail-content-panel" id="product-intro"><header><span>معرفی محصول</span><h2>${esc(p.name)}؛ انتخابی متناسب با نیاز شما</h2></header><p>این محصول از دستهٔ ${esc(p.type)} است. هنگام انتخاب، نوع ${esc(p.movement)} و ساختار ${esc(p.strap)} را با نیاز و شرایط کاری خود تطبیق دهید. اطلاعات این صفحه بر پایهٔ مشخصات ثبت‌شدهٔ کالا تهیه شده است.</p></section><section class="product-detail-content-panel product-detail-expert" id="product-expert"><header><span>راهنمای انتخاب</span><h2>بررسی تخصصی</h2></header><div class="product-expert-grid"><div><i class="bi bi-tools" aria-hidden="true"></i><strong>دسته و کاربرد</strong><p>${esc(p.type)}؛ برای انتخاب دقیق، نوع کار و میزان استفادهٔ روزانه را در نظر بگیرید.</p></div><div><i class="bi bi-lightning-charge" aria-hidden="true"></i><strong>نوع عملکرد</strong><p>${esc(p.movement)}؛ سازگاری منبع تغذیه یا شیوهٔ عملکرد را پیش از خرید بررسی کنید.</p></div><div><i class="bi bi-rulers" aria-hidden="true"></i><strong>ساختار محصول</strong><p>${esc(p.strap)}؛ ابعاد، لوازم همراه و شرایط نصب را با فضای کارتان تطبیق دهید.</p></div></div></section><section class="product-detail-content-panel" id="product-specifications"><header><span>جزئیات فنی</span><h2>ویژگی‌های اصلی و دیگر ویژگی‌ها</h2></header><div class="product-spec-groups"><section class="product-spec-group"><h3><i class="bi bi-stars" aria-hidden="true"></i>ویژگی‌های اصلی</h3><dl><div><dt>دسته‌بندی</dt><dd>${esc(p.type)}</dd></div><div><dt>نوع عملکرد</dt><dd>${esc(p.movement)}</dd></div><div><dt>برند</dt><dd>${esc(p.brand)}</dd></div></dl></section><section class="product-spec-group"><h3><i class="bi bi-list-check" aria-hidden="true"></i>دیگر ویژگی‌ها</h3><dl>${specs.map(([label,value])=>`<div><dt>${esc(label)}</dt><dd>${esc(value)}</dd></div>`).join('')}</dl></section></div></section><section class="product-detail-content-panel" id="product-reviews"><header><span>تجربهٔ خریداران</span><h2>دیدگاه‌ها</h2></header><div class="product-feedback-list" id="productReviewList">${feedbackMarkup('reviews',readFeedback('reviews'))}</div><form class="product-feedback-form" data-feedback-type="reviews"><label>نام نمایشی<input name="name" maxlength="60" required placeholder="نام شما"></label><label>امتیاز<select name="rating" required><option value="5">۵ — عالی</option><option value="4">۴ — خوب</option><option value="3">۳ — متوسط</option><option value="2">۲ — ضعیف</option><option value="1">۱ — خیلی ضعیف</option></select></label><label class="product-feedback-wide">دیدگاه شما<textarea name="text" rows="3" maxlength="800" required placeholder="تجربه یا نکتهٔ خود را بنویسید"></textarea></label><button class="product-feedback-submit" type="submit"><i class="bi bi-send" aria-hidden="true"></i>ثبت دیدگاه</button><small class="product-feedback-note">دیدگاه در این نسخه به‌صورت محلی در همین مرورگر ذخیره می‌شود.</small></form></section><section class="product-detail-content-panel" id="product-questions"><header><span>پاسخ به ابهام‌ها</span><h2>پرسش‌ها</h2></header><div class="product-feedback-list" id="productQuestionList">${feedbackMarkup('questions',readFeedback('questions'))}</div><form class="product-feedback-form product-question-form" data-feedback-type="questions"><label>نام نمایشی<input name="name" maxlength="60" required placeholder="نام شما"></label><label class="product-feedback-wide">پرسش شما<textarea name="text" rows="3" maxlength="600" required placeholder="سؤال خود دربارهٔ محصول را بنویسید"></textarea></label><button class="product-feedback-submit" type="submit"><i class="bi bi-question-lg" aria-hidden="true"></i>ثبت پرسش</button><small class="product-feedback-note">پرسش در این نسخه به‌صورت محلی در همین مرورگر ذخیره می‌شود.</small></form></section><section class="product-detail-related" aria-labelledby="relatedProductsTitle"><header><span>برای انتخاب بهتر</span><h2 id="relatedProductsTitle">کالاهای مشابه</h2></header><div class="product-detail-related-grid">${suggestions.map(item=>`<article class="product-card product-detail-related-card"><a class="product-image" href="Product?id=${item.id}"><img src="${esc(item.image)}" alt="${esc(item.name)}" loading="lazy"></a><div class="product-info"><div class="product-related-meta"><span class="product-related-brand">${esc(item.brand)}</span><span class="product-related-category"><i class="bi bi-grid-3x3-gap" aria-hidden="true"></i>${esc(item.type)}</span></div><h3><a href="Product?id=${item.id}">${esc(item.name)}</a></h3><p>${money(item.price*1000000)}</p></div><button class="add-cart" type="button" data-id="${item.id}" data-name="${esc(item.name)}" data-price="${item.price*1000000}" data-image="${esc(item.image)}"><i class="bi bi-bag-plus" aria-hidden="true"></i>افزودن به سبد خرید</button></article>`).join('')}</div></section>`;
      const detail=p.detail||{};
      const questionsPanel=productView.querySelector('#product-questions');
      const questionList=questionsPanel.querySelector('#productQuestionList');
      const questionForm=questionsPanel.querySelector('.product-question-form');
      const questionLayout=document.createElement('div');
      questionLayout.className='product-question-layout';
      const questionFormColumn=document.createElement('div');
      questionFormColumn.className='product-question-form-column';
      questionFormColumn.innerHTML='<div class="product-question-form-heading"><span><i class="bi bi-chat-square-text" aria-hidden="true"></i></span><strong>پرسش خود را ثبت کنید</strong><small>سؤال‌تان را دربارهٔ این کالا بنویسید.</small></div>';
      questionFormColumn.append(questionForm);
      const questionListColumn=document.createElement('div');
      questionListColumn.className='product-question-list-column';
      questionListColumn.append(questionList);
      const questionPagination=document.createElement('nav');
      questionPagination.className='product-feedback-pagination';
      questionPagination.dataset.feedbackPagination='questions';
      questionPagination.setAttribute('aria-label','صفحه‌بندی پرسش‌ها');
      questionListColumn.append(questionPagination);
      questionLayout.append(questionFormColumn,questionListColumn);
      questionsPanel.querySelector('header').after(questionLayout);
      const decorateQuestionCards=(items=readFeedback('questions'),start=0)=>{productView.querySelectorAll('#productQuestionList .product-feedback-card').forEach((card,index)=>card.classList.toggle('product-question-faq',Boolean(items[start+index]?.sample)));};
      decorateQuestionCards();
      questionForm.addEventListener('submit',()=>setTimeout(decorateQuestionCards,0));
      const reviewsPanel=productView.querySelector('#product-reviews');
      const reviewForm=reviewsPanel.querySelector('.product-feedback-form');
      const reviewList=reviewsPanel.querySelector('#productReviewList');
      const reviewLayout=document.createElement('div');
      reviewLayout.className='product-review-layout';
      const reviewFormColumn=document.createElement('div');
      reviewFormColumn.className='product-review-form-column product-question-form-column';
      reviewFormColumn.innerHTML='<div class="product-question-form-heading"><span><i class="bi bi-chat-heart" aria-hidden="true"></i></span><strong>تجربه‌تان را بنویسید</strong><small>ثبت دیدگاه با نام دلخواه یا به‌صورت کاربر مهمان.</small></div>';
      reviewFormColumn.append(reviewForm);
      const reviewListColumn=document.createElement('div');
      reviewListColumn.className='product-review-list-column product-question-list-column';
      reviewListColumn.append(reviewList);
      const reviewPagination=document.createElement('nav');
      reviewPagination.className='product-feedback-pagination';
      reviewPagination.dataset.feedbackPagination='reviews';
      reviewPagination.setAttribute('aria-label','صفحه‌بندی دیدگاه‌ها');
      reviewListColumn.append(reviewPagination);
      reviewLayout.append(reviewFormColumn,reviewListColumn);
      reviewsPanel.querySelector('header').after(reviewLayout);
      productView.querySelectorAll('.product-feedback-form input[name="name"]').forEach(input=>{input.required=false;input.placeholder='اختیاری؛ در صورت خالی‌بودن کاربر مهمان';const label=input.closest('label');if(label?.firstChild?.nodeType===Node.TEXT_NODE)label.firstChild.textContent='نام نمایشی (اختیاری)';});
      const productTitle=productView.querySelector('#productDetailTitle');
      if(p.englishName)productTitle.insertAdjacentHTML('afterend','<p class="product-detail-english-name" lang="en" dir="ltr">'+esc(p.englishName)+'</p>');
      const productMeta=productView.querySelector('.product-detail-meta');
      const categoryPath=Array.isArray(p.categoryPath)&&p.categoryPath.length?p.categoryPath:['ابزار و تجهیزات',p.type,p.type];
      productMeta.innerHTML='<span class="product-meta-pill product-meta-brand"><i class="bi bi-patch-check" aria-hidden="true"></i><span><small>برند</small><strong>'+esc(p.brand)+'</strong></span></span><span class="product-meta-pill product-meta-code"><i class="bi bi-upc-scan" aria-hidden="true"></i><span><small>کد محصول</small><strong dir="ltr">HN-'+String(p.id).padStart(3,'0')+'</strong></span></span><span class="product-meta-pill product-meta-category"><i class="bi bi-grid" aria-hidden="true"></i><span class="product-meta-category-content"><small>مسیر دسته‌بندی</small><ol class="product-meta-path" aria-label="'+esc(categoryPath.join('، '))+'">'+categoryPath.map(part=>'<li>'+esc(part)+'</li>').join('')+'</ol></span></span>';
      const decorateSalePrice=(node,item)=>{const discount=Number(item?.discountPercent)||0;if(!node||discount<=0||discount>=100)return;const current=Math.round(item.price*1000000),previous=Math.round(current/(1-discount/100));node.innerHTML='<span class="product-price-deal"><del class="price-old">'+fa(previous)+'</del><b class="product-price-discount">٪'+fa(discount)+'</b></span><strong class="product-price-current">'+money(current)+'</strong>';};
      const salePercent=Number(p.discountPercent)||0;
      const priceSubtitle=productView.querySelector('.product-detail-price > div small');
      if(priceSubtitle)priceSubtitle.textContent=salePercent>0?'قیمت پس از تخفیف':'قیمت قابل پرداخت';
      productView.querySelector('.product-detail-price > p')?.remove();
      const detailPrice=productView.querySelector('.product-detail-price > strong');
      if(salePercent>0&&salePercent<100&&detailPrice){const current=Math.round(p.price*1000000),previous=Math.round(current/(1-salePercent/100));detailPrice.insertAdjacentHTML('beforebegin','<div class="product-detail-sale-meta"><del class="price-old">'+money(previous)+'</del><span class="product-price-discount">٪'+fa(salePercent)+' تخفیف</span></div>');}
      productView.querySelectorAll('.product-detail-related-card').forEach((card,index)=>{const item=suggestions[index];decorateSalePrice(card.querySelector('.product-info > p'),item);if(item&&Number(item.discountPercent)>0)card.querySelector('.product-image')?.insertAdjacentHTML('beforeend','<span class="product-discount-badge">٪'+fa(item.discountPercent)+' تخفیف</span>');});
      productView.querySelectorAll('.product-feedback-note').forEach(note=>note.remove());
      productView.querySelector('.product-detail-description').textContent=detail.intro||`محصول ${p.name} برای کاربرد ${p.type}؛ جزئیات نهایی به مدل انتخابی وابسته است.`;
      productView.querySelector('#product-intro > p').textContent=detail.intro||'اطلاعات تکمیلی این محصول به‌زودی ثبت می‌شود.';
      productView.querySelector('#product-intro > p').insertAdjacentHTML('afterend',`<ul class="product-detail-intro-points">${(detail.highlights||[]).map(item=>`<li><i class="bi bi-check2-circle" aria-hidden="true"></i>${esc(item)}</li>`).join('')}</ul>`);
      const expertPoints=(detail.highlights||[]).filter(Boolean);
      const expertFallback=(detail.specs||[]).slice(0,3).map(([label,value])=>`${label}: ${value}`);
      const expertGuide=expertPoints.length?expertPoints:expertFallback;
      productView.querySelector('.product-expert-grid').innerHTML=expertGuide.map((text,index)=>`<div><i class="bi ${['bi-tools','bi-lightbulb','bi-shield-check'][index%3]}" aria-hidden="true"></i><strong>${['کاربرد پیشنهادی','نکتهٔ مهم پیش از خرید','راهنمای استفاده و ایمنی'][index%3]}</strong><p>${esc(text)}</p></div>`).join('');
      productView.querySelectorAll('.product-detail-tabs a[href="#product-reviews"],.product-detail-tabs a[href="#product-questions"]').forEach(tab=>{const count=tab.querySelector('b');if(count)count.textContent=fa(Number(count.textContent)||0);});
      productView.querySelectorAll('[data-detail-media-index]').forEach(button=>button.addEventListener('click',()=>{const item=media[Number(button.dataset.detailMediaIndex)];if(!item)return;productView.querySelector('#productDetailMedia').innerHTML=renderMedia(item);productView.querySelectorAll('.product-detail-thumb').forEach(thumb=>{const active=thumb===button;thumb.classList.toggle('is-active',active);thumb.setAttribute('aria-pressed',String(active));});}));
      const renderProductFeedback=type=>{const items=readFeedback(type),list=productView.querySelector(type==='reviews'?'#productReviewList':'#productQuestionList'),tab=productView.querySelector(`.product-detail-tabs a[href="#product-${type==='reviews'?'reviews':'questions'}"] b`),pagination=productView.querySelector(`[data-feedback-pagination="${type}"]`),pages=Math.max(1,Math.ceil(items.length/feedbackPageSize));feedbackPages[type]=Math.min(feedbackPages[type],pages);const start=(feedbackPages[type]-1)*feedbackPageSize;if(list)list.innerHTML=feedbackMarkup(type,items.slice(start,start+feedbackPageSize));if(type==='questions')decorateQuestionCards(items,start);if(tab)tab.textContent=fa(items.length);if(pagination)pagination.innerHTML=pages>1?`<button type="button" data-feedback-page="${Math.max(1,feedbackPages[type]-1)}" aria-label="صفحهٔ قبل" ${feedbackPages[type]===1?'disabled':''}><i class="bi bi-chevron-right" aria-hidden="true"></i></button>${Array.from({length:pages},(_,index)=>index+1).map(page=>`<button type="button" data-feedback-page="${page}" aria-label="صفحهٔ ${fa(page)}" ${page===feedbackPages[type]?'aria-current="page"':''}>${fa(page)}</button>`).join('')}<button type="button" data-feedback-page="${Math.min(pages,feedbackPages[type]+1)}" aria-label="صفحهٔ بعد" ${feedbackPages[type]===pages?'disabled':''}><i class="bi bi-chevron-left" aria-hidden="true"></i></button>`:'';};
      productView.addEventListener('click',event=>{const button=event.target.closest('[data-feedback-pagination] button[data-feedback-page]');if(!button)return;const type=button.closest('[data-feedback-pagination]').dataset.feedbackPagination;feedbackPages[type]=Number(button.dataset.feedbackPage)||1;renderProductFeedback(type);});
      productView.querySelectorAll('.product-feedback-form').forEach(form=>form.addEventListener('submit',event=>{event.preventDefault();const type=form.dataset.feedbackType,data=new FormData(form),name=String(data.get('name')||'').trim()||'کاربر مهمان',text=String(data.get('text')||'').trim();if(!text)return;const key=`hn-product-${type}-${p.id}`,items=readFeedback(type);items.unshift({name,text,rating:Number(data.get('rating'))||5,sample:false});ui.storage.set(key,JSON.stringify(items.filter(item=>!item.sample).slice(0,50)));feedbackPages[type]=1;renderProductFeedback(type);form.reset();const status=document.querySelector('#productStatus');if(status)status.textContent=type==='reviews'?'دیدگاه شما ثبت شد.':'پرسش شما ثبت شد.';}));
      renderProductFeedback('reviews');
      renderProductFeedback('questions');
      document.querySelector('#addProduct').onclick=event=>{ui.cart.add({...p,price:p.price*1000000},1,event.currentTarget);document.querySelector('#productStatus').textContent='محصول به سبد خرید اضافه شد.';};
      document.querySelector('#saveProduct').onclick=e=>{let ids=favorites();ids=ids.includes(String(p.id))?ids.filter(i=>i!==String(p.id)):[...ids,String(p.id)];ui.storage.set('hn-favorites',JSON.stringify(ids));const active=ids.includes(String(p.id));e.currentTarget.setAttribute('aria-pressed',String(active));e.currentTarget.setAttribute('aria-label',active?'حذف از علاقه‌مندی‌ها':'ذخیره در علاقه‌مندی‌ها');e.currentTarget.innerHTML=`<i class="bi ${active?'bi-heart-fill':'bi-heart'}" aria-hidden="true"></i><span>${active?'ذخیره‌شده':'علاقه‌مندی'}</span>`;};
    }
  }
  const couponKey = 'hn-demo-coupon';
  let couponCode = ui.storage.get(couponKey, '') === 'HADI10' ? 'HADI10' : '';
  let couponMessage = '';
  let couponError = false;
  const normalizeCoupon = value => value.trim().replace(/[۰-۹]/g, digit => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(digit))).replace(/[٠-٩]/g, digit => String('٠١٢٣٤٥٦٧٨٩'.indexOf(digit))).toUpperCase();
  const quantityFeedback = new Map();
  function renderCart(){
    const items=ui.cart.read(),total=items.reduce((s,i)=>s+i.price*i.quantity,0);
    if (!items.length) { couponCode = ''; couponMessage = ''; ui.storage.remove(couponKey); }
    const discount = couponCode ? Math.floor(total * 0.1) : 0;
    const finalTotal = total - discount;
    const couponMarkup = `<form id="couponForm" class="flow-coupon"><label for="couponCode">کد تخفیف دارید؟</label><div class="flow-coupon-entry"><input id="couponCode" name="coupon" dir="ltr" placeholder="کد تخفیف" maxlength="32" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-describedby="couponHint couponStatus" aria-invalid="${couponError}" value="${couponCode}"><button class="flow-button" type="submit">اعمال کد</button></div><small id="couponHint">کد آزمایشی <bdi>HADI10</bdi> برای ۱۰٪ تخفیف در نسخه نمایشی</small><p id="couponStatus" class="flow-coupon-status${couponError ? ' is-error' : ''}" role="status">${esc(couponMessage || (couponCode ? 'کد HADI10 اعمال شده است.' : ''))}</p>${couponCode ? '<button id="removeCoupon" class="flow-coupon-remove" type="button">حذف کد تخفیف</button>' : ''}</form>`;
    const totalsMarkup = `<div class="flow-total flow-discount"><span class="cart-line-label"><i class="bi bi-tags" aria-hidden="true"></i><span>مجموع تخفیف${couponCode ? ' (۱۰٪)' : ''}</span></span><span>${money(discount)}</span></div><div class="flow-total flow-payable"><span class="cart-line-label"><i class="bi bi-wallet2" aria-hidden="true"></i><span>مبلغ نهایی محصولات<small>بدون هزینه ارسال</small></span></span><strong>${money(finalTotal)}</strong></div>`;
    const view=document.querySelector('#cartView');
    if(view){
      const addressSection=document.querySelector('.cart-address-section');
      const paymentSection=document.querySelector('.cart-payment-section');
      if(addressSection)addressSection.remove();
      if(paymentSection)paymentSection.remove();
      view.innerHTML=items.length?`<div class="flow-grid"><div class="cart-primary-column"><div class="cart-address-slot"></div><section class="flow-card cart-products-panel"><div class="cart-items-list">${items.map((i,n)=>`<article class="flow-row cart-product-row"><div class="cart-product-identity"><img src="${esc(i.image)}" alt="${esc(i.name)}"><div class="cart-product-copy">${englishProductName(i) ? `<span class="cart-product-english" lang="en" dir="ltr">${esc(englishProductName(i))}</span>` : ''}<strong>${esc(i.name)}</strong><p>${money(i.price)}</p></div></div><div class="cart-item-actions"><div class="cart-quantity" role="group" aria-label="تعداد ${esc(i.name)}"><button type="button" data-minus="${n}" aria-label="کاهش تعداد ${esc(i.name)}" ${i.quantity===1?'disabled':''}><i class="bi bi-dash" aria-hidden="true"></i></button><span class="cart-quantity-value">${fa(i.quantity)}</span><button type="button" data-plus="${n}" aria-label="افزایش تعداد ${esc(i.name)}" ${i.quantity>=99?'disabled':''}><i class="bi bi-plus" aria-hidden="true"></i></button></div><button type="button" class="cart-item-remove" data-remove="${n}" aria-label="حذف ${esc(i.name)} از سبد"><i class="bi bi-trash3" aria-hidden="true"></i><span>حذف</span></button></div></article>`).join('')}</div></section></div><div class="cart-summary-column"><div class="cart-payment-slot"></div><aside class="flow-card cart-summary"><div class="cart-summary-heading"><span class="cart-summary-icon"><i class="bi bi-receipt" aria-hidden="true"></i></span><h2>خلاصه سبد خرید</h2></div><div class="cart-summary-lines"><div class="flow-total"><span class="cart-line-label"><i class="bi bi-box-seam" aria-hidden="true"></i><span>تعداد آیتم‌ها</span></span><strong>${fa(items.reduce((sum,item)=>sum+item.quantity,0))} عدد <small>(${fa(items.length)} مدل)</small></strong></div><div class="flow-total"><span class="cart-line-label"><i class="bi bi-receipt" aria-hidden="true"></i><span>جمع قیمت محصولات</span></span><strong>${money(total)}</strong></div>${totalsMarkup}</div>${couponMarkup}<p class="cart-shipping-note"><i class="bi bi-truck" aria-hidden="true"></i> هزینه ارسال هنگام تأیید سفارش مشخص می‌شود.</p><a class="flow-button cart-checkout-link" href="Checkout">مشاهده پیش‌فاکتور و پرداخت <i class="bi bi-arrow-left" aria-hidden="true"></i></a><a class="ui-inline-action cart-continue-link" href="Search"><i class="bi bi-watch" aria-hidden="true"></i><span>بازگشت به فروشگاه</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></aside></div></div>`:emptyCart();
      if(items.length && addressSection){
        const addressSlot=view.querySelector('.cart-address-slot');
        if(addressSlot)addressSlot.append(addressSection);
        else view.before(addressSection);
      }
      if(items.length && paymentSection){
        const paymentSlot=view.querySelector('.cart-payment-slot');
        if(paymentSlot)paymentSlot.append(paymentSection);
        else view.before(paymentSection);
      }
      const returnToStore = view.querySelector('.cart-continue-link');
      if (returnToStore) {
        const actions = document.createElement('div');
        actions.className = 'cart-summary-actions';
        returnToStore.before(actions);
        actions.append(returnToStore);

        const clearCart = document.createElement('button');
        clearCart.className = 'ui-inline-action cart-clear-action';
        clearCart.type = 'button';
        clearCart.innerHTML = '<i class="bi bi-trash3" aria-hidden="true"></i><span>حذف همه اقلام</span>';
        clearCart.onclick = async () => {
          const confirmed = await window.confirmFormSubmission?.({
            title: 'حذف همه اقلام سبد؟',
            message: 'همهٔ محصولات و تعداد انتخاب‌شده از سبد خرید پاک می‌شوند.',
            confirmText: 'بله، حذف شود',
          });
          if (confirmed) ui.cart.write([]);
        };
        actions.append(clearCart);
      }
      view.querySelectorAll('[data-remove],[data-plus],[data-minus]').forEach(button => {
        const action = button.dataset.plus !== undefined ? 'plus' : 'minus';
        const index = Number(button.dataset[action]);
        const key = button.dataset.remove === undefined ? JSON.stringify([items[index].id, action]) : '';
        if (key) {
          button.dataset.quantityFeedback = key;
          button.classList.toggle('is-clicked', quantityFeedback.has(key));
        }
        button.onclick = () => {
          if (button.dataset.remove !== undefined) items.splice(Number(button.dataset.remove), 1);
          else {
            items[index].quantity += action === 'plus' ? 1 : -1;
            clearTimeout(quantityFeedback.get(key));
            quantityFeedback.set(key, setTimeout(() => {
              quantityFeedback.delete(key);
              view.querySelectorAll('[data-quantity-feedback]').forEach(current => {
                if (current.dataset.quantityFeedback === key) current.classList.remove('is-clicked');
              });
            }, 1000));
          }
          ui.cart.write(items);
        };
      });
    }
    document.querySelector('#couponForm')?.addEventListener('submit', event => {
      event.preventDefault();
      const input = document.querySelector('#couponCode');
      const value = normalizeCoupon(input.value);
      if (value !== 'HADI10') {
        couponError = true;
        couponMessage = value ? 'کد تخفیف معتبر نیست. کد واردشده را بررسی کنید.' : 'لطفاً کد تخفیف را وارد کنید.';
        input.setAttribute('aria-invalid', 'true');
        const status = document.querySelector('#couponStatus');
        status.textContent = couponMessage;
        status.classList.add('is-error');
        input.focus();
        return;
      }
      couponMessage = couponCode ? 'این کد قبلاً اعمال شده است.' : 'کد تخفیف اعمال شد؛ ۱۰٪ از جمع محصولات کم شد.';
      couponCode = value;
      couponError = false;
      ui.storage.set(couponKey, couponCode);
      renderCart();
      document.querySelector('#couponCode')?.focus();
    });
    document.querySelector('#removeCoupon')?.addEventListener('click', () => {
      couponCode = ''; couponError = false; couponMessage = 'کد تخفیف حذف شد.';
      ui.storage.remove(couponKey);
      renderCart();
      document.querySelector('#couponCode')?.focus();
    });
    const summary=document.querySelector('#orderSummary');
    if(summary){
      summary.innerHTML=`<div class="cart-summary-heading"><span class="cart-summary-icon"><i class="bi bi-receipt" aria-hidden="true"></i></span><h2>خلاصه پیش‌فاکتور</h2></div><div class="cart-summary-lines"><div class="flow-total"><span>تعداد کالاها</span><strong>${fa(items.reduce((sum,item)=>sum+item.quantity,0))} عدد</strong></div><div class="flow-total"><span>جمع محصولات</span><strong>${money(total)}</strong></div>${totalsMarkup}</div>`;
      const invoice=document.querySelector('#invoiceItems');
      if(invoice) invoice.innerHTML=items.length ? items.map(item=>`<article class="invoice-item"><img src="${esc(item.image)}" alt="${esc(item.name)}"><div><strong>${esc(item.name)}</strong><small>${fa(item.quantity)} عدد × ${money(item.price)}</small></div><b>${money(item.price*item.quantity)}</b></article>`).join('') : '<p>سبد خرید شما خالی است.</p>';
    }
    document.querySelectorAll('.page-cart-toggle b,#cartCount,#searchCartCount').forEach(el=>el.textContent=fa(items.reduce((s,i)=>s+i.quantity,0)));
  }
  renderCart();document.addEventListener('hn:cart-change',renderCart);window.addEventListener('storage',renderCart);
  document.querySelector('#trackingForm')?.addEventListener('submit',e=>{e.preventDefault();document.querySelector('#trackingStatus').textContent='استعلام آنلاین فعال نیست. برای دریافت وضعیت واقعی سفارش با پشتیبانی ۰۹۱۷۷۷۶۴۳۲۴ تماس بگیرید.';});
  function renderFavorites(){const view=document.querySelector('#favoritesView');if(!view)return;const cache=ui.storage.json('hn-product-cache',{});const list=favorites().map(id=>catalog.find(p=>String(p.id)===id)||cache[id]).filter(Boolean);view.innerHTML=list.length?`<div class="flow-cards">${list.map(p=>`<article class="flow-card favorite-item-card"><a href="Product?id=${encodeURIComponent(p.id)}"><img class="flow-photo" src="${esc(p.image)}" alt="${esc(p.name)}"><h2>${esc(p.name)}</h2></a><p>${money(Number(p.price)*1000000)}</p><button class="list-remove-button" type="button" data-unlike="${esc(p.id)}" aria-label="حذف ${esc(p.name)} از علاقه‌مندی‌ها"><i class="bi bi-trash3" aria-hidden="true"></i><span>حذف از علاقه‌مندی‌ها</span></button></article>`).join('')}</div>`:empty('هنوز ساعتی ذخیره نکرده‌اید');view.querySelectorAll('[data-unlike]').forEach(b=>b.onclick=()=>{ui.storage.set('hn-favorites',JSON.stringify(favorites().filter(id=>id!==String(b.dataset.unlike))));renderFavorites();});}
  renderFavorites();
  const renderCompare=()=>{
    const view=document.querySelector('#compareView');
    if(!view)return;
    const savedCompare=ui.storage.json('hn-compare',[]);
    if((!Array.isArray(savedCompare)||savedCompare.length===0)&&ui.storage.get('hn-compare-demo-seeded','__HN_EMPTY__')==='__HN_EMPTY__'){
      ui.storage.set('hn-compare',JSON.stringify(catalog.slice(0,3).map(product=>String(product.id))));
      ui.storage.set('hn-compare-demo-seeded','1');
    }
    const ids=ui.storage.json('hn-compare',[]).map(String);
    const cache=ui.storage.json('hn-product-cache',{});
    const list=ids.map(id=>catalog.find(product=>String(product.id)===id)||cache[id]).filter(Boolean);
    view.innerHTML=list.length?`<header class="compare-overview"><span class="compare-overview-icon"><i class="bi bi-columns-gap" aria-hidden="true"></i></span><span class="compare-overview-copy"><strong>محصول‌ها کنار هم، انتخاب آسان‌تر</strong><small>مشخصات و کاربرد هر گزینه را با هم بسنجید.</small></span><b>${fa(list.length)} محصول</b><a href="Search"><i class="bi bi-plus-lg" aria-hidden="true"></i>افزودن محصول</a></header><div class="compare-product-grid">${list.map((product,index)=>{const specs=[['دسته‌بندی',product.type||'ابزار و تجهیزات'],['نوع تغذیه',product.movement||'مطابق مشخصات کالا'],['کاربری',product.strap||'کارگاهی'],...(product.detail?.specs||[]).slice(0,3)];return `<article class="compare-product-card"><div class="compare-card-top"><span>گزینهٔ ${fa(index+1)}</span><button class="compare-remove-icon" type="button" data-compare-remove-page="${esc(product.id)}" aria-label="حذف ${esc(product.name)} از مقایسه"><i class="bi bi-trash3" aria-hidden="true"></i></button></div><a class="compare-product-link" href="Product?id=${encodeURIComponent(product.id)}"><div class="compare-product-image"><img src="${esc(product.image)}" alt="${esc(product.name)}" loading="lazy"></div><div class="compare-product-meta"><span>${esc(product.brand||'برند منتخب')}</span><small><i class="bi bi-grid-3x3-gap" aria-hidden="true"></i>${esc(product.type||'ابزار و تجهیزات')}</small></div><h2>${esc(product.name)}</h2></a><div class="compare-price"><small>قیمت محصول</small><strong>${money(Number(product.price)*1000000)}</strong></div><h3 class="compare-spec-title"><i class="bi bi-list-check" aria-hidden="true"></i>مشخصات قابل بررسی</h3><dl>${specs.map(([label,value])=>`<div><dt>${esc(label)}</dt><dd>${esc(value)}</dd></div>`).join('')}</dl><a class="compare-detail-link" href="Product?id=${encodeURIComponent(product.id)}">جزئیات محصول<i class="bi bi-arrow-left" aria-hidden="true"></i></a></article>`}).join('')}</div>`:`<div class="compare-empty"><i class="bi bi-columns-gap" aria-hidden="true"></i><h2>هنوز محصولی برای مقایسه انتخاب نکرده‌اید</h2><p>از آیکن مقایسه روی کارت محصولات استفاده کنید تا مشخصاتشان اینجا کنار هم نمایش داده شود.</p><a class="flow-button" href="Search">انتخاب محصولات <i class="bi bi-arrow-left" aria-hidden="true"></i></a></div>`;
    view.querySelectorAll('[data-compare-remove-page]').forEach(button=>button.onclick=()=>{const next=ui.storage.json('hn-compare',[]).map(String).filter(id=>id!==button.dataset.compareRemovePage);ui.storage.set('hn-compare',JSON.stringify(next));renderCompare();});
  };
  renderCompare();
  document.addEventListener('hn:lists-change',()=>{renderFavorites();renderCompare();});
});

