document.addEventListener('DOMContentLoaded', () => {
  const ui=window.HNUI, catalog=window.HNProducts || [];
  const fa=n=>ui.formatNumber(n);
  const esc=s=>ui.escapeHtml(s);
  const homeEnglishNames = {'هاله رز':'Haleh Rose','نوا کرونو شب':'Nova Chrono Night','نوا کرونو، نسخه شب':'Nova Chrono Night Edition','نوا نسخه شب':'Nova Night Edition','نوا استیل سرمه‌ای':'Nova Navy Steel','آرمان بند چرمی':'Arman Leather','آبان مینیمال':'Aban Minimal','میراث کلاسیک':'Miras Classic','آبان بند مش':'Aban Mesh','آرمان رزگلد':'Arman Rose Gold','دماوند اسپرت':'Damavand Sport','مهتاب سیلور':'Mahtab Silver','میراث چرمی':'Miras Leather','هاله کلاسیک':'Haleh Classic'};
  const englishProductName = item => catalog.find(product => product.name === item.name)?.englishName || homeEnglishNames[item.name] || '';
  const money=n=>`${fa(n)} تومان`;
  const empty=(text)=>`<div class="flow-card flow-empty"><i class="bi bi-bag"></i><h2>${text}</h2><a class="flow-button" href="Search">مشاهده ساعت‌ها</a></div>`;
  const emptyCart=()=>`<section class="cart-empty-state" aria-labelledby="emptyCartTitle"><div class="cart-empty-ornament" aria-hidden="true"><span><i class="bi bi-bag"></i></span></div><div class="cart-empty-copy"><span class="cart-empty-kicker">سبد خرید</span><h2 id="emptyCartTitle">سبد خرید شما خالی است</h2><p>هنوز ساعتی برای بررسی و خرید انتخاب نکرده‌اید. از میان مدل‌های منتخب، انتخابتان را شروع کنید.</p><div class="cart-empty-actions"><a class="flow-button" href="Search"><i class="bi bi-watch" aria-hidden="true"></i><span>مشاهده ساعت‌ها</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a><a class="ui-inline-action" href="Magazine"><i class="bi bi-journal-text" aria-hidden="true"></i><span>راهنمای انتخاب</span></a></div><div class="cart-empty-benefits"><span><i class="bi bi-patch-check" aria-hidden="true"></i>ضمانت اصالت</span><span><i class="bi bi-truck" aria-hidden="true"></i>ارسال به سراسر ایران</span></div></div></section>`;
  const favorites=()=>{try{const v=JSON.parse(ui.storage.get('hn-favorites','[]'));return Array.isArray(v)?v:[]}catch{return []}};
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
  document.addEventListener('click',e=>{
    const button=e.target.closest('.favorite');
    if(button){
      const card=button.closest('.product-card');
      const id=card?.querySelector('[data-id]')?.dataset.id;
      const p=catalog.find(p=>String(p.id)===id)||catalog.find(p=>card?.querySelector('h3')?.textContent.trim()===p.name);
      if(p){let ids=favorites();ids=ids.includes(p.id)?ids.filter(i=>i!==p.id):[...ids,p.id];ui.storage.set('hn-favorites',JSON.stringify(ids));}
    }
  },true);
  const productView=document.querySelector('#productView');
  if(productView){
    const p=catalog.find(p=>p.id===Number(new URLSearchParams(location.search).get('id')));
    if(!p)productView.innerHTML=empty('محصول پیدا نشد');
    else{
      document.title=`${p.name} | هادی نوروزی`;
      productView.innerHTML=`<div class="flow-grid"><img class="flow-photo" src="${p.image}" alt="${p.name}"><section class="flow-card"><span>${p.brand} · HN-${String(p.id).padStart(3,'0')}</span><h2>${p.name}</h2><p>طراحی ${p.type} با موتور ${p.movement} و بند ${p.strap}.</p><dl><dt>سبک</dt><dd>${p.type}</dd><dt>موتور</dt><dd>${p.movement}</dd><dt>جنس بند</dt><dd>${p.strap}</dd></dl><h2>${money(p.price*1000000)}</h2><p>اطلاعات و قیمت این کاتالوگ نمایشی است؛ برای تأیید موجودی با فروشگاه تماس بگیرید.</p><button class="flow-button" id="addProduct">افزودن به سبد خرید</button> <a href="Cart">مشاهده سبد</a><br><button class="flow-button" id="saveProduct" aria-pressed="${favorites().includes(p.id)}">${favorites().includes(p.id)?'حذف از علاقه‌مندی‌ها':'ذخیره در علاقه‌مندی‌ها'}</button><p id="productStatus" role="status"></p></section></div>`;
      document.querySelector('#addProduct').onclick=event=>{ui.cart.add({...p,price:p.price*1000000},1,event.currentTarget);document.querySelector('#productStatus').textContent='محصول به سبد خرید اضافه شد.';};
      document.querySelector('#saveProduct').onclick=e=>{let ids=favorites();ids=ids.includes(p.id)?ids.filter(i=>i!==p.id):[...ids,p.id];ui.storage.set('hn-favorites',JSON.stringify(ids));e.target.setAttribute('aria-pressed',ids.includes(p.id));e.target.textContent=ids.includes(p.id)?'حذف از علاقه‌مندی‌ها':'ذخیره در علاقه‌مندی‌ها';};
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
  function renderFavorites(){const view=document.querySelector('#favoritesView');if(!view)return;const list=catalog.filter(p=>favorites().includes(p.id));view.innerHTML=list.length?`<div class="flow-cards">${list.map(p=>`<article class="flow-card"><a href="Product?id=${p.id}"><img class="flow-photo" src="${p.image}" alt="${p.name}"><h2>${p.name}</h2></a><p>${money(p.price*1000000)}</p><button class="flow-button" data-unlike="${p.id}">حذف از فهرست</button></article>`).join('')}</div>`:empty('هنوز ساعتی ذخیره نکرده‌اید');view.querySelectorAll('[data-unlike]').forEach(b=>b.onclick=()=>{ui.storage.set('hn-favorites',JSON.stringify(favorites().filter(id=>id!==Number(b.dataset.unlike))));renderFavorites();});}
  renderFavorites();
});

