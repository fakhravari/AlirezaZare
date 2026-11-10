# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: header-guest-panels.spec.mjs >> mobile account label hides only below its compact-header breakpoint
- Location: tests\browser\header-guest-panels.spec.mjs:32:1

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: page.goto: Test timeout of 30000ms exceeded.
Call log:
  - navigating to "http://127.0.0.1:5198/", waiting until "load"

```

# Page snapshot

```yaml
- generic [active] [ref=f5e1]:
  - link "رفتن به محتوای اصلی" [ref=f5e2] [cursor=pointer]:
    - /url: "#main-content"
  - banner [ref=f5e3]:
    - navigation [ref=f5e4]:
      - text:  
      - generic [ref=f5e5]:
        - button "باز کردن منو" [ref=f5e6] [cursor=pointer]:
          - generic [ref=f5e7]: 
        - generic [ref=f5e8]:
          - text:                                 
          - generic [ref=f5e10]:
            - button "ورود / ثبت‌نام در حساب کاربری" [ref=f5e12] [cursor=pointer]:
              - generic [aria-hidden] [ref=f5e13]: 
              - generic [ref=f5e14]: ورود / ثبت‌نام
              - generic [aria-hidden] [ref=f5e15]: 
            - text: 
            - generic "خرید، مقایسه و علاقه‌مندی‌ها" [ref=f5e16]:
              - link "علاقه‌مندی‌ها" [ref=f5e18] [cursor=pointer]:
                - /url: Profile/Favorites
                - generic [aria-hidden] [ref=f5e19]: 
                - generic [aria-hidden] [ref=f5e20]: ۰
              - link "مقایسه محصولات ۰" [ref=f5e22] [cursor=pointer]:
                - /url: Compare
                - generic [aria-hidden] [ref=f5e23]: 
                - generic [aria-hidden] [ref=f5e24]: ۰
              - button "مشاهده سبد خرید" [ref=f5e26] [cursor=pointer]:
                - generic [ref=f5e27]: 
                - generic [ref=f5e28]: ۰
    - region "دسته‌بندی و پیشنهادهای فروشگاه" [ref=f5e29]:
      - generic [ref=f5e30]:
        - text:                 
        - generic [ref=f5e31]:
          - generic [ref=f5e32]:
            - img "ابزار دقیق برای کار حرفه‌ای؛ برای هر کار، ابزار درست. همراه پروژه‌های خانه و کارگاه." [ref=f5e34]
            - img "قطعات برای ساخت و تعمیر؛ ایده‌هایت را روشن کن. ابزار و قطعات برای پروژه‌های دقیق." [ref=f5e36]
            - img "روشنایی و تجهیزات کارگاهی؛ نور بهتر، کار دقیق‌تر. برای خانه و محیط کار." [ref=f5e38]
            - img "ابزار برقی برای کار حرفه‌ای؛ قدرت در دستان تو. انتخابی برای خانه و کارگاه." [ref=f5e40]
            - img "قطعات برای ساخت و تعمیر؛ همراه ایده‌های تازه. ابزار و قطعات ساخت و تعمیر." [ref=f5e42]
            - img "ابزار اندازه‌گیری دقیق؛ با اطمینان اندازه بگیر. ابزار تست برای بررسی مدارها." [ref=f5e44]
          - button "اسلاید قبلی" [ref=f5e45] [cursor=pointer]:
            - generic [ref=f5e46]: 
          - button "اسلاید بعدی" [ref=f5e47] [cursor=pointer]:
            - generic [ref=f5e48]: 
          - group "انتخاب اسلاید" [ref=f5e49]
  - main [ref=f5e51]:
    - generic [ref=f5e53]:
      - heading "فروشگاه تخصصی برق و ابزار برای انتخاب دقیق‌تر" [level=1] [ref=f5e54]
      - generic [ref=f5e56]:
        - generic [ref=f5e57]: محصولات منتخب
        - heading [level=2] [ref=f5e58]:
          - text: فروشگاه تخصصی برق و ابزار
          - emphasis [ref=f5e59]: برای انتخاب دقیق‌تر
      - list [ref=f5e60]:
        - listitem [ref=f5e61]:
          - button "همه" [ref=f5e62] [cursor=pointer]:
            - generic [aria-hidden] [ref=f5e63]: 
            - text: همه
        - listitem [ref=f5e64]:
          - button "ابزار برقی" [ref=f5e65] [cursor=pointer]:
            - generic [aria-hidden] [ref=f5e66]: 
            - text: ابزار برقی
        - listitem [ref=f5e67]:
          - button "اندازه‌گیری" [ref=f5e68] [cursor=pointer]:
            - generic [aria-hidden] [ref=f5e69]: 
            - text: اندازه‌گیری
        - listitem [ref=f5e70]:
          - button "لحیم‌کاری" [ref=f5e71] [cursor=pointer]:
            - generic [aria-hidden] [ref=f5e72]: 
            - text: لحیم‌کاری
        - listitem [ref=f5e73]:
          - button "روشنایی" [ref=f5e74] [cursor=pointer]:
            - generic [aria-hidden] [ref=f5e75]: 
            - text: روشنایی
      - link "مشاهده همه ابزارها " [ref=f5e76] [cursor=pointer]:
        - /url: Search
    - region [ref=f5e77]:
      - generic [ref=f5e78]:
        - generic [ref=f5e80]:
          - generic [ref=f5e81]: پیشنهادهای منتخب
          - heading [level=2] [ref=f5e82]:
            - text: فروشگاه تخصصی برق و ابزار
            - emphasis [ref=f5e83]: برای انتخاب دقیق‌تر
        - generic [ref=f5e84]:
          - article [ref=f5e86]:
            - generic [ref=f5e87]:
              - heading "ابزار برقی و شارژی" [level=3] [ref=f5e88]
              - link "مرور محصولات" [ref=f5e89] [cursor=pointer]:
                - /url: /product/list
                - text: مرور محصولات
                - generic [aria-hidden] [ref=f5e90]: 
          - article [ref=f5e92]:
            - generic [ref=f5e93]:
              - heading "اندازه‌گیری و آزمون" [level=3] [ref=f5e94]
              - link "مرور محصولات" [ref=f5e95] [cursor=pointer]:
                - /url: /product/list
                - text: مرور محصولات
                - generic [aria-hidden] [ref=f5e96]: 
          - article [ref=f5e98]:
            - generic [ref=f5e99]:
              - heading "تجهیزات لحیم‌کاری" [level=3] [ref=f5e100]
              - link "مرور محصولات" [ref=f5e101] [cursor=pointer]:
                - /url: /product/list
                - text: مرور محصولات
                - generic [aria-hidden] [ref=f5e102]: 
    - region [ref=f5e103]:
      - generic [ref=f5e104]:
        - generic [ref=f5e105]:
          - generic [ref=f5e106]:
            - generic [ref=f5e107]: محصولات پرفروش
            - heading [level=2] [ref=f5e108]:
              - text: پرفروش‌ترین
              - emphasis [ref=f5e109]: ابزارها
            - paragraph [ref=f5e110]: قیمت و مشخصات هر کالا در صفحهٔ محصول است.
          - link "مشاهدهٔ همهٔ محصولات" [ref=f5e111] [cursor=pointer]:
            - /url: /Search
            - text: مشاهدهٔ همهٔ محصولات
            - generic [aria-hidden] [ref=f5e112]: 
        - region "محصولات پرفروش فروشگاه"
    - region [ref=f5e113]:
      - generic [ref=f5e114]:
        - generic [ref=f5e116]:
          - generic [ref=f5e117]: تازه‌واردهای فروشگاه
          - heading [level=2] [ref=f5e118]:
            - text: جدیدترین
            - emphasis [ref=f5e119]: کالاها
        - region "جدیدترین محصولات فروشگاه"
    - region [ref=f5e120]:
      - generic [ref=f5e121]:
        - heading "برندهای موجود" [level=2] [ref=f5e124]
        - generic [ref=f5e126]:
          - generic [ref=f5e127]:
            - link "مشاهده محصولات بوش" [ref=f5e128] [cursor=pointer]:
              - /url: /search?brand=bosch
              - img "لوگوی BOSCH" [ref=f5e130]
              - strong [ref=f5e131]: بوش
            - link "جزئیات برند بوش":
              - /url: /brands/bosch
              - generic [aria-hidden]: 
          - generic [ref=f5e132]:
            - link "مشاهده محصولات ماکیتا" [ref=f5e133] [cursor=pointer]:
              - /url: /search?brand=makita
              - img "لوگوی MAKITA" [ref=f5e135]
              - strong [ref=f5e136]: ماکیتا
            - link "جزئیات برند ماکیتا":
              - /url: /brands/makita
              - generic [aria-hidden]: 
          - generic [ref=f5e137]:
            - link "مشاهده محصولات دیوالت" [ref=f5e138] [cursor=pointer]:
              - /url: /search?brand=dewalt
              - img "لوگوی DEWALT" [ref=f5e140]
              - strong [ref=f5e141]: دیوالت
            - link "جزئیات برند دیوالت":
              - /url: /brands/dewalt
              - generic [aria-hidden]: 
          - generic [ref=f5e142]:
            - link "مشاهده محصولات رونیکس" [ref=f5e143] [cursor=pointer]:
              - /url: /search?brand=ronix
              - img "لوگوی RONIX" [ref=f5e145]
              - strong [ref=f5e146]: رونیکس
            - link "جزئیات برند رونیکس":
              - /url: /brands/ronix
              - generic [aria-hidden]: 
          - generic [ref=f5e147]:
            - link "مشاهده محصولات توسن" [ref=f5e148] [cursor=pointer]:
              - /url: /search?brand=tosan
              - img "لوگوی TOSAN" [ref=f5e150]
              - strong [ref=f5e151]: توسن
            - link "جزئیات برند توسن":
              - /url: /brands/tosan
              - generic [aria-hidden]: 
          - generic [ref=f5e152]:
            - link "مشاهده محصولات یونی‌تی" [ref=f5e153] [cursor=pointer]:
              - /url: /search?brand=uni-t
              - img "لوگوی UNI-T" [ref=f5e155]
              - strong [ref=f5e156]: یونی‌تی
            - link "جزئیات برند یونی‌تی":
              - /url: /brands/uni-t
              - generic [aria-hidden]: 
          - generic [ref=f5e157]:
            - link "مشاهده محصولات فلوک" [ref=f5e158] [cursor=pointer]:
              - /url: /search?brand=fluke
              - img "لوگوی FLUKE" [ref=f5e160]
              - strong [ref=f5e161]: فلوک
            - link "جزئیات برند فلوک":
              - /url: /brands/fluke
              - generic [aria-hidden]: 
          - generic [ref=f5e162]:
            - link "مشاهده محصولات مستک" [ref=f5e163] [cursor=pointer]:
              - /url: /search?brand=mastech
              - img "لوگوی MASTECH" [ref=f5e165]
              - strong [ref=f5e166]: مستک
            - link "جزئیات برند مستک":
              - /url: /brands/mastech
              - generic [aria-hidden]: 
          - generic [ref=f5e167]:
            - link "مشاهده محصولات کیوریتسو" [ref=f5e168] [cursor=pointer]:
              - /url: /search?brand=kyoritsu
              - img "لوگوی KYORITSU" [ref=f5e170]
              - strong [ref=f5e171]: کیوریتسو
            - link "جزئیات برند کیوریتسو":
              - /url: /brands/kyoritsu
              - generic [aria-hidden]: 
          - generic [ref=f5e172]:
            - link "مشاهده محصولات یوهوا" [ref=f5e173] [cursor=pointer]:
              - /url: /search?brand=yihua
              - img "لوگوی YIHUA" [ref=f5e175]
              - strong [ref=f5e176]: یوهوا
            - link "جزئیات برند یوهوا":
              - /url: /brands/yihua
              - generic [aria-hidden]: 
          - generic [ref=f5e177]:
            - link "مشاهده محصولات هاکو" [ref=f5e178] [cursor=pointer]:
              - /url: /search?brand=hakko
              - img "لوگوی HAKKO" [ref=f5e180]
              - strong [ref=f5e181]: هاکو
            - link "جزئیات برند هاکو":
              - /url: /brands/hakko
              - generic [aria-hidden]: 
          - generic [ref=f5e182]:
            - link "مشاهده محصولات ولر" [ref=f5e183] [cursor=pointer]:
              - /url: /search?brand=weller
              - img "لوگوی WELLER" [ref=f5e185]
              - strong [ref=f5e186]: ولر
            - link "جزئیات برند ولر":
              - /url: /brands/weller
              - generic [aria-hidden]: 
          - generic [ref=f5e187]:
            - link "مشاهده محصولات فیلیپس" [ref=f5e188] [cursor=pointer]:
              - /url: /search?brand=philips
              - img "لوگوی PHILIPS" [ref=f5e190]
              - strong [ref=f5e191]: فیلیپس
            - link "جزئیات برند فیلیپس":
              - /url: /brands/philips
              - generic [aria-hidden]: 
          - generic [ref=f5e192]:
            - link "مشاهده محصولات وایها" [ref=f5e193] [cursor=pointer]:
              - /url: /search?brand=wiha
              - img "لوگوی WIHA" [ref=f5e195]
              - strong [ref=f5e196]: وایها
            - link "جزئیات برند وایها":
              - /url: /brands/wiha
              - generic [aria-hidden]: 
          - generic [ref=f5e197]:
            - link "مشاهده محصولات آروا" [ref=f5e198] [cursor=pointer]:
              - /url: /search?brand=arva
              - generic [ref=f5e199]: ARVA
              - strong [ref=f5e201]: آروا
            - link "جزئیات برند آروا":
              - /url: /brands/arva
              - generic [aria-hidden]: 
          - generic [ref=f5e202]:
            - link "مشاهده محصولات ویکتور" [ref=f5e203] [cursor=pointer]:
              - /url: /search?brand=victor
              - generic [ref=f5e204]: VICTOR
              - strong [ref=f5e206]: ویکتور
            - link "جزئیات برند ویکتور":
              - /url: /brands/victor
              - generic [aria-hidden]: 
          - generic [ref=f5e207]:
            - link "مشاهده محصولات کوئیک" [ref=f5e208] [cursor=pointer]:
              - /url: /search?brand=quick
              - generic [ref=f5e209]: QUICK
              - strong [ref=f5e211]: کوئیک
            - link "جزئیات برند کوئیک":
              - /url: /brands/quick
              - generic [aria-hidden]: 
          - generic [ref=f5e212]:
            - link "مشاهده محصولات گوت" [ref=f5e213] [cursor=pointer]:
              - /url: /search?brand=goot
              - generic [ref=f5e214]: GOOT
              - strong [ref=f5e216]: گوت
            - link "جزئیات برند گوت":
              - /url: /brands/goot
              - generic [aria-hidden]: 
          - generic [ref=f5e217]:
            - link "مشاهده محصولات کنیپکس" [ref=f5e218] [cursor=pointer]:
              - /url: /search?brand=knipex
              - generic [ref=f5e219]: KNIPEX
              - strong [ref=f5e221]: کنیپکس
            - link "جزئیات برند کنیپکس":
              - /url: /brands/knipex
              - generic [aria-hidden]: 
          - generic [ref=f5e222]:
            - link "مشاهده محصولات استنلی" [ref=f5e223] [cursor=pointer]:
              - /url: /search?brand=stanley
              - generic [ref=f5e224]: STANLEY
              - strong [ref=f5e226]: استنلی
            - link "جزئیات برند استنلی":
              - /url: /brands/stanley
              - generic [aria-hidden]: 
          - generic [ref=f5e227]:
            - link "مشاهده محصولات بننینگ" [ref=f5e228] [cursor=pointer]:
              - /url: /search?brand=benning
              - generic [ref=f5e229]: BENNING
              - strong [ref=f5e231]: بننینگ
            - link "جزئیات برند بننینگ":
              - /url: /brands/benning
              - generic [aria-hidden]: 
    - generic [ref=f5e233]:
      - generic [ref=f5e234]:
        - generic [ref=f5e235]:
          - generic [ref=f5e236]: تجربه خرید از ندا اولیائی
          - heading [level=2] [ref=f5e237]:
            - text: آنچه مشتریان ما
            - emphasis [ref=f5e238]: می‌گویند
        - generic [ref=f5e240]:
          - generic [ref=f5e241]: ۴٫۹
          - generic [ref=f5e242]:
            - img "امتیاز ۴٫۹ از ۵" [ref=f5e243]:
              - generic [ref=f5e244]: 
              - generic [ref=f5e245]: 
              - generic [ref=f5e246]: 
              - generic [ref=f5e247]: 
              - generic [ref=f5e248]: 
            - strong [ref=f5e249]: بر پایه بیش از ۱,۲۰۰ خرید موفق
            - generic [ref=f5e250]: کیفیت، اصالت و همراهی تا انتخاب نهایی
      - generic [ref=f5e252]:
        - article [ref=f5e253]:
          - generic [aria-hidden] [ref=f5e254]: “ 
          - img "۵ ستاره" [ref=f5e255]:
            - generic [aria-hidden] [ref=f5e256]: 
            - generic [aria-hidden] [ref=f5e257]: 
            - generic [aria-hidden] [ref=f5e258]: 
            - generic [aria-hidden] [ref=f5e259]: 
            - generic [aria-hidden] [ref=f5e260]: 
          - paragraph [ref=f5e261]: برای خرید ابزار نصب، مشخصات و اقلام داخل بسته را مقایسه کردم. سفارش مرتب و به‌موقع به دستم رسید.
          - generic [ref=f5e265]:
            - strong [ref=f5e266]: سارا محمدی
            - generic [ref=f5e267]: خریدار ابزار نصب
            - time [ref=f5e268]:
              - generic [aria-hidden] [ref=f5e269]: 
              - generic [ref=f5e270]: ۱۴۰۵/۰۷/۱۵
        - article [ref=f5e271]:
          - generic [aria-hidden] [ref=f5e272]: “ 
          - img "۴ ستاره" [ref=f5e273]:
            - generic [aria-hidden] [ref=f5e274]: 
            - generic [aria-hidden] [ref=f5e275]: 
            - generic [aria-hidden] [ref=f5e276]: 
            - generic [aria-hidden] [ref=f5e277]: 
          - paragraph [ref=f5e278]: مولتی‌متر را بر اساس محدوده اندازه‌گیری انتخاب کردم. مشخصات کالا و توضیحات فروشگاه با هم تطبیق داشت.
          - generic [ref=f5e282]:
            - strong [ref=f5e283]: میلاد خلیلی
            - generic [ref=f5e284]: خریدار مولتی‌متر
            - time [ref=f5e285]:
              - generic [aria-hidden] [ref=f5e286]: 
              - generic [ref=f5e287]: ۱۴۰۵/۰۷/۰۸
        - article [ref=f5e288]:
          - generic [aria-hidden] [ref=f5e289]: “ 
          - img "۵ ستاره" [ref=f5e290]:
            - generic [aria-hidden] [ref=f5e291]: 
            - generic [aria-hidden] [ref=f5e292]: 
            - generic [aria-hidden] [ref=f5e293]: 
            - generic [aria-hidden] [ref=f5e294]: 
            - generic [aria-hidden] [ref=f5e295]: 
          - paragraph [ref=f5e296]: برای انتخاب نوک هویه و دمای مناسب سؤال داشتم؛ راهنمایی فروشگاه کمک کرد گزینه سازگار را پیدا کنم.
          - generic [ref=f5e300]:
            - strong [ref=f5e301]: نیلوفر رستمی
            - generic [ref=f5e302]: خریدار ایستگاه هویه
            - time [ref=f5e303]:
              - generic [aria-hidden] [ref=f5e304]: 
              - generic [ref=f5e305]: ۱۴۰۵/۰۷/۰۲
        - article [ref=f5e306]:
          - generic [aria-hidden] [ref=f5e307]: “ 
          - img "۴ ستاره" [ref=f5e308]:
            - generic [aria-hidden] [ref=f5e309]: 
            - generic [aria-hidden] [ref=f5e310]: 
            - generic [aria-hidden] [ref=f5e311]: 
            - generic [aria-hidden] [ref=f5e312]: 
          - paragraph [ref=f5e313]: پیش از خرید چراغ کار درباره نوع پایه و شارژدهی پرسیدم و پاسخ روشنی گرفتم. ارسال هم به‌موقع بود.
          - generic [ref=f5e317]:
            - strong [ref=f5e318]: امیرعلی کیانی
            - generic [ref=f5e319]: خریدار چراغ کار
            - time [ref=f5e320]:
              - generic [aria-hidden] [ref=f5e321]: 
              - generic [ref=f5e322]: ۱۴۰۵/۰۶/۲۸
        - article [ref=f5e323]:
          - generic [aria-hidden] [ref=f5e324]: “ 
          - img "۴ ستاره" [ref=f5e325]:
            - generic [aria-hidden] [ref=f5e326]: 
            - generic [aria-hidden] [ref=f5e327]: 
            - generic [aria-hidden] [ref=f5e328]: 
            - generic [aria-hidden] [ref=f5e329]: 
          - paragraph [ref=f5e330]: تنوع ابزارها خوب بود و با کمک مشاور، چراغی متناسب با میز تعمیر و فضای کارگاه انتخاب کردم.
          - generic [ref=f5e334]:
            - strong [ref=f5e335]: لیلا مرادی
            - generic [ref=f5e336]: خریدار چراغ کارگاهی
            - time [ref=f5e337]:
              - generic [aria-hidden] [ref=f5e338]: 
              - generic [ref=f5e339]: ۱۴۰۵/۰۶/۲۰
        - article [ref=f5e340]:
          - generic [aria-hidden] [ref=f5e341]: “ 
          - img "۵ ستاره" [ref=f5e342]:
            - generic [aria-hidden] [ref=f5e343]: 
            - generic [aria-hidden] [ref=f5e344]: 
            - generic [aria-hidden] [ref=f5e345]: 
            - generic [aria-hidden] [ref=f5e346]: 
            - generic [aria-hidden] [ref=f5e347]: 
          - paragraph [ref=f5e348]: قبل از سفارش ابزار برقی درباره ضمانت و اقلام همراه پرسیدم؛ پاسخ‌گویی پشتیبانی انتخاب را آسان کرد.
          - generic [ref=f5e352]:
            - strong [ref=f5e353]: رضا شریفی
            - generic [ref=f5e354]: خریدار ابزار برقی
            - time [ref=f5e355]:
              - generic [aria-hidden] [ref=f5e356]: 
              - generic [ref=f5e357]: ۱۴۰۵/۰۶/۱۴
    - region "مزیت‌های خرید از ندا اولیائی" [ref=f5e358]:
      - generic [ref=f5e360]:
        - generic [ref=f5e361]:
          - generic [ref=f5e362]:
            - generic [ref=f5e363]:
              - generic [aria-hidden] [ref=f5e364]: 
              - text: اطلاعات روشن برای انتخاب
            - heading [level=2] [ref=f5e365]:
              - text: تجربه‌ای راحت‌تر،
              - emphasis [ref=f5e366]: با خیال آسوده
          - paragraph [ref=f5e367]: پیش از خرید، جزئیات هر کالا و شرایط مرتبط با آن را جداگانه بررسی کنید.
        - article [ref=f5e368]:
          - generic [aria-hidden] [ref=f5e369]: 
          - generic [ref=f5e370]:
            - strong [ref=f5e371]: بررسی شرایط ضمانت
            - generic [ref=f5e372]: مدت و ضوابط ضمانت در اطلاعات همان محصول مشخص می‌شود.
        - article [ref=f5e373]:
          - generic [aria-hidden] [ref=f5e374]: 
          - generic [ref=f5e375]:
            - strong [ref=f5e376]: اطلاع از شرایط ارسال
            - generic [ref=f5e377]: هزینه و زمان ارسال با توجه به مقصد و وضعیت سفارش اعلام می‌شود.
        - article [ref=f5e378]:
          - generic [aria-hidden] [ref=f5e379]: 
          - generic [ref=f5e380]:
            - strong [ref=f5e381]: پرسش از پشتیبانی
            - generic [ref=f5e382]: برای ابهام دربارهٔ مشخصات یا سفارش از راه‌های تماس استفاده کنید.
        - article [ref=f5e383]:
          - generic [aria-hidden] [ref=f5e384]: 
          - generic [ref=f5e385]:
            - strong [ref=f5e386]: وضعیت پرداخت
            - generic [ref=f5e387]: پرداخت آنلاین هنوز فعال نیست و وجهی دریافت نمی‌شود.
        - article [ref=f5e388]:
          - generic [aria-hidden] [ref=f5e389]: 
          - generic [ref=f5e390]:
            - strong [ref=f5e391]: انتخاب آگاهانه
            - generic [ref=f5e392]: مشخصات، قیمت و اقلام همراه را پیش از تصمیم‌گیری مقایسه کنید تا انتخاب دقیق‌تری داشته باشید.
    - generic [ref=f5e394]:
      - generic [ref=f5e395]:
        - generic [ref=f5e396]:
          - generic [ref=f5e397]: مجله تخصصی برق و ابزار
          - heading [level=2] [ref=f5e398]:
            - text: راهنمای خرید و
            - emphasis [ref=f5e399]: کار با ابزار
        - link "مشاهده همه مطالب " [ref=f5e400] [cursor=pointer]:
          - /url: /magazine
          - text: مشاهده همه مطالب
          - generic [ref=f5e401]: 
      - generic [ref=f5e402]:
        - article [ref=f5e404]:
          - link [ref=f5e405] [cursor=pointer]:
            - /url: /magazine/choosing-work-drill
            - heading "راهنمای انتخاب دریل برای خانه و کارگاه" [level=3] [ref=f5e406]
          - paragraph [ref=f5e407]: از نوع کار و منبع تغذیه تا سه‌نظام و متعلقات؛ معیارهای انتخاب یک دریل متناسب با نیازتان.
          - text: 4 دقیقه مطالعه
        - article [ref=f5e409]:
          - link [ref=f5e410] [cursor=pointer]:
            - /url: /magazine/multimeter-buying-guide
            - heading "چطور مولتی‌متر مناسب انتخاب کنیم؟" [level=3] [ref=f5e411]
          - paragraph [ref=f5e412]: نوع اندازه‌گیری، محدوده و دقت، رده ایمنی دستگاه و کیفیت پراب‌ها را با کاربرد خود بسنجید؛ این راهنما نکته‌های انتخاب و نگهداری مولتی‌متر را مرور می‌کند.
          - text: 5 دقیقه مطالعه
        - article [ref=f5e414]:
          - link [ref=f5e415] [cursor=pointer]:
            - /url: /magazine/soldering-station-basics
            - heading "راهنمای انتخاب ایستگاه لحیم‌کاری" [level=3] [ref=f5e416]
          - paragraph [ref=f5e417]: برای تعمیر برد و مونتاژ قطعات، توان، کنترل دما و نوع نوک هویه را با کارتان هماهنگ کنید.
          - text: 6 دقیقه مطالعه
        - article [ref=f5e419]:
          - link [ref=f5e420] [cursor=pointer]:
            - /url: /magazine/worklight-selection
            - heading "چراغ کار مناسب تعمیرگاه و محیط نصب" [level=3] [ref=f5e421]
          - paragraph [ref=f5e422]: شدت و پخش نور، زاویه تابش، پایه و منبع تغذیه را با فضای تعمیرگاه یا محل نصب هماهنگ کنید؛ راهنما به انتخاب چراغ کار و بررسی شرایط محیطی می‌پردازد.
          - text: 7 دقیقه مطالعه
        - article [ref=f5e424]:
          - link [ref=f5e425] [cursor=pointer]:
            - /url: /magazine/drill-bit-matching
            - heading "انتخاب مته بر اساس جنس سطح" [level=3] [ref=f5e426]
          - paragraph [ref=f5e427]: مته چوب، فلز و مصالح را بر اساس جنس سطح، شکل نوک، قطر و سازگاری با سه‌نظام انتخاب کنید؛ این راهنما نکات شروع ایمن سوراخ‌کاری را نیز توضیح می‌دهد.
          - text: 4 دقیقه مطالعه
        - article [ref=f5e429]:
          - link [ref=f5e430] [cursor=pointer]:
            - /url: /magazine/electrical-tool-safety
            - heading "چک‌لیست ایمنی هنگام کار با ابزار برقی" [level=3] [ref=f5e431]
          - paragraph [ref=f5e432]: پیش از کار با ابزار برقی، بدنه و کابل را بررسی کنید، ابزار حفاظتی مناسب بپوشید و متعلقات سازگار به‌کار ببرید؛ نکات قطع برق و نگهداری را هم بخوانید.
          - text: 5 دقیقه مطالعه
    - generic [ref=f5e435]:
      - generic [ref=f5e436]:
        - generic [ref=f5e437]: به‌روزرسانی‌های فروشگاه
        - heading "از تازه‌های محصولات باخبر شوید" [level=2] [ref=f5e438]
        - paragraph [ref=f5e439]: در حال حاضر ثبت‌نام خبرنامه فعال نیست. برای مشاهدهٔ محصولات و راهنماهای فنی، محصولات و مجله را مرور کنید.
      - generic [ref=f5e441]:
        - textbox "نشانی ایمیل شما" [ref=f5e442]
        - button "بررسی وضعیت خبرنامه" [ref=f5e443] [cursor=pointer]:
          - generic [aria-hidden] [ref=f5e444]: 
  - contentinfo [ref=f5e445]:
    - generic [ref=f5e446]:
      - generic [ref=f5e447]:
        - generic [ref=f5e448]:
          - link "برق شیراز؛ مرجع تخصصی ابزار و تجهیزات برقی؛ رفتن به صفحهٔ اصلی" [ref=f5e449] [cursor=pointer]:
            - /url: Home/Index#home
            - img "برق شیراز؛ مرجع تخصصی ابزار و تجهیزات برقی" [ref=f5e450]
          - paragraph [ref=f5e451]: محصولاتی از ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی برای بررسی و مقایسه. مشخصات، قیمت و شرایط هر کالا را در صفحهٔ محصول ببینید و برای پرسش‌های بیشتر با مجموعه تماس بگیرید.
          - generic "شبکه‌های اجتماعی و راه‌های ارتباطی" [ref=f5e452]:
            - img "اینستاگرام" [ref=f5e453]:
              - generic [ref=f5e454]: 
            - link "واتساپ فروشگاه ندا اولیائی" [ref=f5e455] [cursor=pointer]:
              - /url: https://wa.me/989369455457
              - generic [ref=f5e456]: 
            - img "تلگرام" [ref=f5e457]:
              - generic [ref=f5e458]: 
            - img "لینکدین" [ref=f5e459]:
              - generic [ref=f5e460]: 
            - link "تماس تلفنی با فروشگاه ندا اولیائی" [ref=f5e461] [cursor=pointer]:
              - /url: tel:+989369455457
              - generic [ref=f5e462]: 
          - generic "نمادهای اعتماد" [ref=f5e463]:
            - link "مشاهده نماد اعتماد الکترونیکی فروشگاه (پنجره جدید)" [ref=f5e464] [cursor=pointer]:
              - /url: https://trustseal.enamad.ir/?id=8270056&Code=magw24ZAYJwl3ZjkoHJrQoG8tZ4dv7MC
              - img "نماد اعتماد الکترونیکی فروشگاه" [ref=f5e465]
            - img "مجوز کسب‌وکارهای مجازی" [ref=f5e466]
            - img "نماد ساماندهی" [ref=f5e467]
        - generic [ref=f5e468]:
          - heading "دسته‌بندی‌ها" [level=2] [ref=f5e469]
          - link "ابزار برقی" [ref=f5e470] [cursor=pointer]:
            - /url: /search?product-group=power-tools
          - link "ابزار اندازه‌گیری" [ref=f5e471] [cursor=pointer]:
            - /url: /search?product-group=measurement-tools
          - link "لحیم‌کاری و تعمیر برد" [ref=f5e472] [cursor=pointer]:
            - /url: /search?product-group=soldering-and-board-repair
          - link "روشنایی و لامپ" [ref=f5e473] [cursor=pointer]:
            - /url: /search?product-group=lighting-and-lamps
        - generic [ref=f5e474]:
          - heading "خدمات مشتریان" [level=2] [ref=f5e475]
          - link "پیگیری سفارش" [ref=f5e476] [cursor=pointer]:
            - /url: /Tracking
          - link "شرایط ارسال" [ref=f5e477] [cursor=pointer]:
            - /url: /Shipping
          - link "ضمانت اصالت" [ref=f5e478] [cursor=pointer]:
            - /url: /Warranty
          - link "پرسش‌های متداول" [ref=f5e479] [cursor=pointer]:
            - /url: /Faq
          - link "رویه بازگرداندن کالا" [ref=f5e480] [cursor=pointer]:
            - /url: /Legal/Returns
        - generic [ref=f5e481]:
          - heading "کمپانی و قوانین" [level=2] [ref=f5e482]
          - link "درباره مجموعه" [ref=f5e483] [cursor=pointer]:
            - /url: Home/Index#story
          - link "مجله برق و ابزار" [ref=f5e484] [cursor=pointer]:
            - /url: Magazine
          - link "تماس با ما" [ref=f5e485] [cursor=pointer]:
            - /url: Contact
          - link "حریم خصوصی" [ref=f5e486] [cursor=pointer]:
            - /url: /Legal/Privacy
          - link "شرایط استفاده" [ref=f5e487] [cursor=pointer]:
            - /url: /Legal/Terms
        - generic [ref=f5e488]:
          - heading "راه‌های ارتباطی" [level=2] [ref=f5e489]
          - 'link "مدیریت: 09369455457" [ref=f5e490] [cursor=pointer]':
            - /url: tel:+989369455457
          - 'link "پشتیبانی: 09369455457" [ref=f5e491] [cursor=pointer]':
            - /url: tel:+989369455457
      - generic [ref=f5e492]:
        - generic [ref=f5e493]: © ۱۴۰۵ ندا اولیائی؛ تمامی حقوق محفوظ است.
        - generic [ref=f5e494]: تخصص ما تأمین ابزار و تجهیزات فنی معتبر است.
  - text:                  
```

# Test source

```ts
  1   | import {test, expect} from '@playwright/test';
  2   | import axe from 'axe-core';
  3   | import {useBrowserAccount} from '../helpers/browser-account.mjs';
  4   | 
  5   | async function expectNoA11yViolations(page, state) {
  6   |   const results = await page.evaluate(async () => window.axe.run(document, {
  7   |     runOnly: {type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice']},
  8   |   }));
  9   |   expect(results.violations.map(issue => ({id: issue.id, nodes: issue.nodes.map(node => node.target)})), state).toEqual([]);
  10  | }
  11  | 
  12  | const panels = [
  13  |   ['#favoritesToggle', '#favoritesPanel'],
  14  |   ['#compareToggle', '#comparePanel'],
  15  |   ['#cartToggle', '#cartPanel'],
  16  | ];
  17  | 
  18  | test('empty guest header panels stay closed on hover', async ({page, isMobile}) => {
  19  |   test.skip(isMobile, 'Hover panels are a desktop interaction.');
  20  |   await page.goto('/');
  21  |   for (const [toggle, panel] of panels) {
  22  |     await page.locator(toggle).hover();
  23  |     await expect(page.locator(panel)).not.toHaveClass(/\bopen\b/);
  24  |     await expect(page.locator(panel)).toBeHidden();
  25  |     await expect(page.locator(toggle)).toHaveAttribute('aria-expanded', 'false');
  26  |     await expect(page.locator('#loginModal')).not.toBeVisible();
  27  |   }
  28  |   await page.locator('#cartToggle').click();
  29  |   await expect(page.locator('#loginModal')).toBeVisible();
  30  | });
  31  | 
  32  | test('mobile account label hides only below its compact-header breakpoint', async ({page}) => {
  33  |   for (const width of [320, 390, 575, 576, 767, 768]) {
  34  |     await page.setViewportSize({width, height: 900});
> 35  |     await page.goto('/');
      |                ^ Error: page.goto: Test timeout of 30000ms exceeded.
  36  |     const label = page.locator('#mainNav .account-btn > .account-label');
  37  |     const chevron = page.locator('#mainNav .account-btn > .bi-chevron-down');
  38  |     const display = await label.evaluate(element => getComputedStyle(element).display);
  39  |     const chevronDisplay = await chevron.evaluate(element => getComputedStyle(element).display);
  40  |     expect(display === 'none').toBe(width <= 575);
  41  |     expect(chevronDisplay === 'none').toBe(width <= 575);
  42  |     const documentWidth = await page.evaluate(() => document.documentElement.scrollWidth);
  43  |     expect(documentWidth).toBeLessThanOrEqual(width);
  44  |   }
  45  | });
  46  | 
  47  | test('desktop home brand caret is hidden by its header owner without important', async ({page, isMobile}) => {
  48  |   if (isMobile) test.skip();
  49  |   for (const width of [1200, 1359, 1360, 1440]) {
  50  |     await page.setViewportSize({width, height: 900});
  51  |     await page.goto('/');
  52  |     const caretDisplay = await page.locator('#mainNav .brand-dropdown > .nav-link').evaluate(element =>
  53  |       getComputedStyle(element, '::after').display,
  54  |     );
  55  |     expect(caretDisplay, `brand caret at ${width}px`).toBe('none');
  56  |   }
  57  | });
  58  | 
  59  | test('desktop help menu presents customer and company links in two columns', async ({page, isMobile}) => {
  60  |   test.skip(isMobile, 'The two-column menu is desktop-only; mobile keeps its accordion.');
  61  |   await page.goto('/');
  62  |   const dropdown = page.locator('#mainNav .quick-links-dropdown:not(.sales-plans-dropdown)');
  63  |   await dropdown.locator(':scope > button').click();
  64  |   const menu = dropdown.locator('.quick-links-menu');
  65  |   await expect(menu).toBeVisible();
  66  |   await expect(menu.locator('.quick-links-group')).toHaveCount(2);
  67  |   await expect(menu.locator('#quickCustomerLinks')).toBeVisible();
  68  |   await expect(menu.locator('#quickCompanyLinks')).toBeVisible();
  69  |   await expect(menu.locator('.quick-links-group a')).toHaveCount(10);
  70  | });
  71  | 
  72  | test('mobile help links remain in the existing accordion', async ({page, isMobile}) => {
  73  |   test.skip(!isMobile, 'The accordion is the mobile navigation pattern.');
  74  |   await page.goto('/');
  75  |   const drawer = page.locator('#mobileMenu');
  76  |   await expect(drawer.locator('.mobile-quick-links-accordion')).toBeAttached();
  77  |   await expect(drawer.locator('.quick-links-mobile-group')).toHaveCount(2);
  78  |   await expect(page.locator('#mainNav .quick-links-dropdown:not(.sales-plans-dropdown) .quick-links-menu')).toBeHidden();
  79  | });
  80  | 
  81  | test('mobile navigation drawer passes WCAG checks while open', async ({page, isMobile}) => {
  82  |   test.skip(!isMobile, 'The navigation drawer is a mobile interaction.');
  83  |   await page.addInitScript(axe.source);
  84  |   await page.goto('/');
  85  |   await page.locator('#mainNav .navbar-toggler').click();
  86  |   await expect(page.locator('#mobileMenu')).toBeVisible();
  87  |   await expectNoA11yViolations(page, 'mobile navigation drawer');
  88  | });
  89  | 
  90  | test('mobile drawer category icons keep their 31px layout in their component owner', async ({page, isMobile}) => {
  91  |   test.skip(!isMobile, 'Category icon sizing is specific to the mobile drawer.');
  92  |   await page.goto('/');
  93  |   await page.locator('#mainNav .navbar-toggler').click();
  94  |   const drawer = page.locator('#mobileMenu');
  95  |   await expect(drawer).toBeVisible();
  96  |   const icon = drawer.locator('.mobile-category-toggle > span > i').first();
  97  |   await expect(icon).toBeVisible();
  98  |   const layout = await icon.evaluate(element => {
  99  |     const style = getComputedStyle(element);
  100 |     return {
  101 |       display: style.display,
  102 |       width: style.width,
  103 |       height: style.height,
  104 |       placeItems: style.placeItems,
  105 |       borderRadius: style.borderRadius,
  106 |       fontSize: style.fontSize,
  107 |       boxShadow: style.boxShadow,
  108 |       marginLeft: style.marginLeft,
  109 |     };
  110 |   });
  111 |   expect(layout).toEqual({
  112 |     display: 'grid',
  113 |     width: '31px',
  114 |     height: '31px',
  115 |     placeItems: 'center',
  116 |     borderRadius: '9px',
  117 |     fontSize: '15px',
  118 |     boxShadow: 'none',
  119 |     marginLeft: '0px',
  120 |   });
  121 | });
  122 | 
  123 | test('authenticated user can preview nonempty header panels on hover', async ({page, isMobile}) => {
  124 |   test.skip(isMobile, 'Hover panels are a desktop interaction.');
  125 |   await useBrowserAccount(page, {favorites: [1], compare: [1], cart: [{id: 1, quantity: 1, salesPlanId: 'regular'}]});
  126 |   await page.goto('/');
  127 |   await expect(page.locator('#cartPanel .cart-item')).toHaveCount(1);
  128 |   for (const [toggle, panel] of panels) {
  129 |     await page.locator(toggle).hover();
  130 |     await expect(page.locator(panel)).toHaveClass(/\bopen\b/);
  131 |     await expect(page.locator(toggle)).toHaveAttribute('aria-expanded', 'true');
  132 |     await expect(page.locator('#loginModal')).not.toBeVisible();
  133 |   }
  134 | });
  135 | 
```