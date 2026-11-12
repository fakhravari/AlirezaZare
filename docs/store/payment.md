# پرداخت

درخواست پرداخت در `Shop/Controllers/PaymentController.cs` و منطق orchestration در `Shop/Services/PaymentService.cs` است. درخواست آغاز پرداخت به نشست cookie کاربر و antiforgery token نیاز دارد؛ callback از مسیر attempt پیگیری می‌شود.

در DI فعلی `Shop/Program.cs`، `IPaymentGateway` با `UnconfiguredPaymentGateway` ثبت شده است. بنابراین endpoint پرداخت وجود دارد، اما این ثبت به معنی اتصال عملیاتی به بانک نیست. فهرست موسسات پرداخت صرفاً محتواست و مرجعش [payment-institutions](payment-institutions.md) است.

## تغییر یا اتصال درگاه

gateway را از قرارداد `Shop/Services/PaymentGateway.cs` دنبال کنید؛ شروع پرداخت، پاسخ callback، verify سمت درگاه، idempotency، تطبیق مبلغ/ارز، وضعیت order و ثبت تاریخچه باید در سرور کنترل شود. callback ورودی قابل اعتماد نیست تا وقتی امضای درگاه و شناسهٔ تلاش معتبر نشده باشد. سفارش را فقط بر اساس redirect یا querystring موفق نکنید.

بررسی قرارداد با fake/test gateway انجام شود؛ آزمون مالی واقعی فقط طبق فرایند محیط sandbox درگاه. پرداخت شبیه‌سازی‌شده را پرداخت بانکی گزارش نکنید.

[سبد و checkout](cart.md) · [مرزهای backend](../architecture/backend-readiness.md) · [فهرست docs](../README.md)

بازبینی DI و endpoint: ۱۲ نوامبر ۲۰۲۶.
