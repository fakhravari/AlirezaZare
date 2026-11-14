# پرداخت

درخواست و پاسخ HTTP پرداخت در `Shop/Controllers/PaymentController.cs` است. orchestration تراکنش، attempt و سفارش در `Data/Infrastructure/Payments/PaymentService.cs` پیاده‌سازی و از قرارداد `Data/Application/Store/Contracts/IPaymentGateway.cs` به درگاه متصل می‌شود. درخواست آغاز پرداخت به نشست cookie کاربر و antiforgery token نیاز دارد؛ callback از مسیر attempt پیگیری می‌شود و query آن پیش از عبور به قرارداد به دادهٔ مستقل از ASP.NET تبدیل می‌شود.

در DI فعلی `Shop/Program.cs`، `IPaymentGateway` با `UnconfiguredPaymentGateway` ثبت شده است. بنابراین endpoint پرداخت وجود دارد، اما این ثبت به معنی اتصال عملیاتی به بانک نیست. فهرست موسسات پرداخت صرفاً محتواست و مرجعش [payment-institutions](payment-institutions.md) است.

Checkout فعلاً فقط فاکتور/سفارش را ثبت می‌کند و کاربر را به endpoint پرداخت یا شبیه‌ساز هدایت نمی‌کند. سفارش با وضعیت `pending-payment` می‌ماند، فاکتور و جمع تخفیف در صفحهٔ تأیید نمایش داده می‌شود و وجهی دریافت نمی‌شود.

## تغییر یا اتصال درگاه

gateway را از قرارداد `Data/Application/Store/Contracts/IPaymentGateway.cs` دنبال کنید؛ شروع پرداخت، پاسخ callback، verify سمت درگاه، idempotency، تطبیق مبلغ/ارز، وضعیت order و ثبت تاریخچه باید در سرور کنترل شود. callback ورودی قابل اعتماد نیست تا وقتی امضای درگاه و شناسهٔ تلاش معتبر نشده باشد. سفارش را فقط بر اساس redirect یا querystring موفق نکنید.

بررسی قرارداد با fake/test gateway انجام شود؛ آزمون مالی واقعی فقط طبق فرایند محیط sandbox درگاه. پرداخت شبیه‌سازی‌شده را پرداخت بانکی گزارش نکنید.

[سبد و checkout](cart.md) · [مرزهای backend](../architecture/backend-readiness.md) · [فهرست docs](../README.md)

بازبینی DI، endpoint و جریان ثبت فاکتور: ۱۳ نوامبر ۲۰۲۶.
