namespace Shop.Models;

public static partial class JournalArticles
{
    private sealed record Article(string Slug, string Category, string Title, string Summary, string Image, string[] Headings, string[] Content, MagazineSourceModel[]? Sources = null);

    private static readonly Article[] Articles =
    [
        new("choosing-work-drill", "راهنمای خرید", "راهنمای انتخاب دریل برای خانه و کارگاه", "از نوع کار و منبع تغذیه تا سه‌نظام و متعلقات؛ معیارهای انتخاب یک دریل متناسب با نیازتان.", "tool-hammer-drill.png", ["کاربری را مشخص کنید", "شارژی یا برق شهری؟", "سه‌نظام و کنترل سرعت", "متعلقات و نکات ایمنی"], ["برای نصب‌های سبک، مونتاژ یا سوراخ‌کاری مصالح، ابتدا جنس سطح و میزان استفاده را مشخص کنید. انتخاب ابزار بر مبنای کاربرد واقعی، از پرداخت هزینه برای قابلیت‌های غیرضروری جلوگیری می‌کند.", "مدل شارژی آزادی حرکت بیشتری می‌دهد و برای محیط‌های بدون پریز مناسب است؛ مدل برق شهری برای کار پیوسته نیاز به شارژ ندارد. وزن دستگاه و ظرفیت باتری را با مدت و محل استفاده بسنجید.", "قطر سه‌نظام، سازگاری با مته‌های موجود و امکان تنظیم سرعت را بررسی کنید. برای سوراخ‌کاری مصالح، وجود حالت چکشی و مته مناسب اهمیت دارد.", "پیش از سفارش، محتویات جعبه، باتری و شارژر را بررسی کنید. هنگام کار از عینک محافظ استفاده کنید و مته را متناسب با سطح انتخاب کنید."]),
        new("multimeter-buying-guide", "راهنمای خرید", "چطور مولتی‌متر مناسب انتخاب کنیم؟", "نوع اندازه‌گیری، محدوده و دقت، رده ایمنی دستگاه و کیفیت پراب‌ها را با کاربرد خود بسنجید؛ این راهنما نکته‌های انتخاب و نگهداری مولتی‌متر را مرور می‌کند.", "tool-multimeter-kit.png", ["اندازه‌گیری‌های موردنیاز", "محدوده و دقت", "رده ایمنی و پراب‌ها", "نگهداری درست"], ["ابتدا مشخص کنید به اندازه‌گیری ولتاژ، جریان، مقاومت، پیوستگی یا قابلیت‌های دیگری نیاز دارید. امکانات دستگاه را با کار روزانه خود تطبیق دهید.", "محدوده اندازه‌گیری و دقت اعلام‌شده برای همان مدل را بررسی کنید. قابلیت‌هایی مانند نمایشگر خوانا، نور پس‌زمینه یا انتخاب خودکار محدوده می‌توانند کاربری را ساده‌تر کنند.", "برای کار برقی، رده ایمنی دستگاه و پراب‌ها را متناسب با محیط کاری انتخاب کنید. ظاهر سالم یا عدد درج‌شده به‌تنهایی جایگزین بررسی استاندارد و دستورالعمل سازنده نیست.", "پراب‌ها را پس از استفاده جداگانه نگهداری کنید و باتری دستگاه را طبق راهنمای سازنده تعویض کنید. برای اندازه‌گیری‌های پرخطر از فرد متخصص کمک بگیرید."]),
        new("soldering-station-basics", "آموزش تخصصی", "راهنمای انتخاب ایستگاه لحیم‌کاری", "برای تعمیر برد و مونتاژ قطعات، توان، کنترل دما و نوع نوک هویه را با کارتان هماهنگ کنید.", "tool-soldering-station.png", ["نوع کار را بشناسید", "کنترل دما و توان", "نوک مناسب قطعه", "تهویه و ایمنی"], ["تعمیر بردهای ظریف با لحیم‌کاری قطعات بزرگ تفاوت دارد. اندازه قطعه و مدت استفاده روزانه را مبنای انتخاب دستگاه قرار دهید.", "توان و پایداری دمایی را از مشخصات مدل بررسی کنید. کنترل دقیق دما برای کارهای تکراری و قطعات حساس مفید است، اما تنظیم مناسب به نوع آلیاژ و قطعه نیز بستگی دارد.", "نوک هویه باید با محل اتصال و قطعه سازگار باشد. از نوک تمیز و سالم استفاده کنید و برای جلوگیری از آسیب، گرما را بیش از زمان لازم روی پد نگه ندارید.", "دود لحیم را با تهویه یا مکنده مناسب از محل تنفس دور کنید. دستگاه داغ را روی پایه نسوز بگذارید و پیش از تعویض نوک، دستور سازنده را رعایت کنید."]),
        new("worklight-selection", "راهنمای خرید", "چراغ کار مناسب تعمیرگاه و محیط نصب", "شدت و پخش نور، زاویه تابش، پایه و منبع تغذیه را با فضای تعمیرگاه یا محل نصب هماهنگ کنید؛ راهنما به انتخاب چراغ کار و بررسی شرایط محیطی می‌پردازد.", "tool-led-worklight.png", ["نور متناسب با کار", "پایه و زاویه تابش", "باتری یا برق شهری", "شرایط محیطی"], ["برای کار دقیق نزدیک، نور یکنواخت و امکان تنظیم شدت روشنایی اهمیت دارد. مساحت محل و فاصله چراغ تا سطح کار را در نظر بگیرید.", "پایه قابل تنظیم یا آهنربایی می‌تواند در نصب و تعمیر کمک کند. پایداری چراغ و جهت تابش را متناسب با فضای میز یا محل کار بررسی کنید.", "مدل شارژی جابه‌جایی را آسان می‌کند؛ مدل برق شهری برای استفاده طولانی مناسب است. زمان کارکرد باتری و روش شارژ را از مشخصات همان محصول بخوانید.", "در محیط‌های دارای گردوغبار یا رطوبت، درجه حفاظت درج‌شده برای مدل را بررسی کنید. از استفاده خارج از محدوده مجاز سازنده خودداری کنید."]),
        new("drill-bit-matching", "آموزش تخصصی", "انتخاب مته بر اساس جنس سطح", "مته چوب، فلز و مصالح را بر اساس جنس سطح، شکل نوک، قطر و سازگاری با سه‌نظام انتخاب کنید؛ این راهنما نکات شروع ایمن سوراخ‌کاری را نیز توضیح می‌دهد.", "tool-hammer-drill.png", ["سطح را شناسایی کنید", "مته مناسب جنس کار", "سازگاری با سه‌نظام", "شروع ایمن سوراخ‌کاری"], ["پیش از انتخاب مته، جنس سطح و وجود پوشش یا لایه‌های پنهان را بررسی کنید. برای دیوار، محل عبور کابل و لوله را نیز در نظر بگیرید.", "مته چوب، فلز و مصالح از نظر هندسه و جنس متفاوت‌اند. استفاده از مته نامناسب می‌تواند به سطح، ابزار یا قطعه کار آسیب بزند.", "قطر و نوع دنباله مته باید با سه‌نظام دستگاه سازگار باشد. مته را محکم و مستقیم نصب کنید و پیش از شروع، گردش آزاد آن را بررسی کنید.", "قطعه را ثابت کنید، عینک محافظ بزنید و با فشار کنترل‌شده شروع کنید. هنگام تعویض مته، دستگاه را از برق جدا یا باتری را خارج کنید."]),
        new("electrical-tool-safety", "ایمنی و نگهداری", "چک‌لیست ایمنی هنگام کار با ابزار برقی", "پیش از کار با ابزار برقی، بدنه و کابل را بررسی کنید، ابزار حفاظتی مناسب بپوشید و متعلقات سازگار به‌کار ببرید؛ نکات قطع برق و نگهداری را هم بخوانید.", "tool-multimeter-kit.png", ["بازرسی اولیه", "تجهیزات حفاظت فردی", "انتخاب ابزار درست", "پایان کار و نگهداری"], ["بدنه، کابل، دوشاخه، باتری و متعلقات را پیش از استفاده بررسی کنید. ابزار آسیب‌دیده یا دارای صدای غیرعادی را به کار نگیرید.", "عینک ایمنی و تجهیزات حفاظت متناسب با نوع کار استفاده کنید. لباس گشاد و زیورآلات را از قطعات متحرک دور نگه دارید.", "ابزار و متعلقات را مطابق جنس سطح و دستورالعمل سازنده انتخاب کنید. برای تعمیر یا تنظیم، منبع تغذیه را قطع کنید.", "پس از کار ابزار را خاموش و تمیز کنید و در محل خشک نگه دارید. سرویس‌های تخصصی را به تعمیرکار واجد صلاحیت بسپارید."]),
        new("battery-tool-care", "ایمنی و نگهداری", "نگهداری باتری ابزار شارژی", "برای نگهداری باتری ابزار شارژی، شارژر سازگار انتخاب کنید و دما، محل نگهداری و نشانه‌های آسیب را بررسی کنید؛ راهنما توصیه‌های آماده‌سازی و شارژ را مرور می‌کند.", "tool-hammer-drill.png", ["شارژر سازگار", "دما و محل نگهداری", "نشانه‌های آسیب", "آماده‌سازی برای کار"], ["از شارژر سازگار با پلتفرم و مدل باتری استفاده کنید. باتری ناسازگار یا شارژر غیراستاندارد می‌تواند خطرآفرین باشد.", "باتری را مطابق دستور سازنده شارژ و در محیط خشک و دارای دمای مناسب نگهداری کنید. از رهاکردن آن در گرمای شدید یا مجاورت شعله خودداری کنید.", "تورم، نشتی، بوی غیرعادی یا داغ‌شدن بیش از حد از نشانه‌های توقف استفاده است. باتری آسیب‌دیده را باز نکنید و طبق دستورالعمل دفع کنید.", "پیش از کار، شارژ کافی و اتصال درست باتری را بررسی کنید. برای نگهداری طولانی‌مدت، توصیه سازنده درباره سطح شارژ را دنبال کنید."]),
        new("soldering-tip-care", "ایمنی و نگهداری", "چطور از نوک هویه مراقبت کنیم؟", "با تمیزکاری درست، دمای متناسب و قلع‌اندودکردن نوک هویه به حفظ کیفیت لحیم کمک کنید؛ این راهنما زمان مناسب تعویض نوک و نکات نگهداری را توضیح می‌دهد.", "tool-soldering-station.png", ["تمیزکاری در زمان مناسب", "دمای متناسب", "قلع‌اندودکردن نوک", "تعویض اصولی"], ["نوک را با ابزار تمیزکننده توصیه‌شده برای دستگاه پاک کنید. سایش شدید یا سوهان‌کاری می‌تواند پوشش نوک را از بین ببرد.", "دمای لازم را با نوع لحیم و قطعه هماهنگ کنید. دمای بالاتر از نیاز، عمر نوک را کم می‌کند و احتمال آسیب به برد را افزایش می‌دهد.", "در زمان توقف، طبق راهنمای سازنده نوک را با لایه‌ای نازک از لحیم محافظت کنید. هویه را برای مدت طولانی در دمای بالا و بدون استفاده رها نکنید.", "پیش از تعویض، دستگاه را خاموش و خنک کنید. نوک جایگزین باید با مدل و دسته هویه سازگار باشد."]),
        new("power-tool-buying-checklist", "راهنمای خرید", "پیش از خرید ابزار برقی چه چیزهایی را بررسی کنیم؟", "پیش از خرید ابزار برقی، کاربرد و مشخصات مدل، محتویات بسته، سازگاری متعلقات و شرایط ضمانت را بررسی کنید؛ این چک‌لیست موارد لازم برای مقایسه را جمع‌بندی می‌کند.", "tool-led-worklight.png", ["کاربرد و مشخصات", "اقلام داخل بسته", "ضمانت و خدمات", "مقایسه نهایی"], ["نیاز کاری و مدت استفاده را مشخص کنید و مشخصات کلیدی را با آن بسنجید. نام مشابه دو محصول لزوماً به معنی توانایی یا محتویات یکسان نیست.", "باتری، شارژر، کیف و متعلقات ممکن است در بسته‌های مختلف متفاوت باشند. فهرست اقلام همراه همان کالا را پیش از خرید بخوانید.", "مدت و شرایط ضمانت، ارائه‌دهنده خدمات و شیوه پیگیری را بررسی کنید. شرایط ضمانت را با نوع استفاده خود تطبیق دهید.", "قیمت، مشخصات، اقلام همراه و خدمات را کنار هم مقایسه کنید. در صورت باقی‌ماندن ابهام، پیش از ثبت سفارش از فروشگاه سؤال کنید."]),
        new("multimeter-measurement-safety", "ایمنی و نگهداری", "نکات ایمنی اندازه‌گیری با مولتی‌متر", "انتخاب حالت و ورودی درست، محدوده مناسب و پراب سالم را پیش از اندازه‌گیری بررسی کنید.", "tool-multimeter-kit.png", ["بازرسی دستگاه و پراب", "حالت و ورودی درست", "محدوده اندازه‌گیری", "چه زمانی کار را متوقف کنیم؟"], ["بدنه، عایق پراب و محل اتصال را بررسی کنید. کابل ترک‌خورده یا پراب آسیب‌دیده نباید برای اندازه‌گیری استفاده شود.", "حالت اندازه‌گیری و ورودی پراب را مطابق دستورالعمل تنظیم کنید. اتصال اشتباه می‌تواند به دستگاه یا کاربر آسیب بزند.", "محدوده و رده ایمنی ابزار باید با مدار مورد اندازه‌گیری سازگار باشد. پیش از اندازه‌گیری جریان، روش اتصال و محدودیت‌های دستگاه را از راهنما بخوانید.", "اگر از رده ایمنی، روش اندازه‌گیری یا وضعیت مدار مطمئن نیستید، کار را متوقف کنید و از متخصص کمک بگیرید. مولتی‌متر جایگزین آموزش ایمنی نیست."]),
        new("workshop-lighting-guide", "راهنمای خرید", "روشنایی کارگاه را چطور بهتر برنامه‌ریزی کنیم؟", "ترکیب نور عمومی و موضعی، خیرگی و سایه را کاهش می‌دهد و دید محل کار را بهتر می‌کند.", "tool-led-worklight.png", ["نور عمومی و موضعی", "کاهش سایه و خیرگی", "انتخاب محل نصب", "نگهداری چراغ‌ها"], ["روشنایی عمومی را با چراغ موضعی روی میز یا محل تعمیر تکمیل کنید. انتخاب چراغ باید با نوع فعالیت و اندازه فضا متناسب باشد.", "زاویه تابش را طوری تنظیم کنید که دست یا ابزار سایه مزاحم ایجاد نکند. منبع نور را مستقیم در میدان دید قرار ندهید.", "محل نصب باید دسترسی برای تنظیم و سرویس داشته باشد و کابل‌ها مسیر رفت‌وآمد را مسدود نکنند. برای هر محیط، شرایط نصب مجاز را بررسی کنید.", "چراغ را طبق دستور سازنده خاموش و تمیز کنید. پیش از تعویض یا سرویس، برق را قطع کنید و از قطعات سازگار استفاده کنید."]),
        new("soldering-station-setup", "آموزش تخصصی", "آماده‌سازی میز تعمیر برد", "چیدمان ابزار، نور و تهویه مناسب، فرایند تعمیرات الکترونیک را ایمن‌تر و دقیق‌تر می‌کند.", "tool-soldering-station.png", ["سطح کار و نظم ابزار", "نور کافی", "تهویه مناسب", "ایمنی قطعات حساس"], ["سطح نسوز و مرتب برای کار انتخاب کنید و ابزارهای داغ را روی پایه مخصوص قرار دهید. کابل‌ها را از محل حرکت دست دور کنید.", "چراغ موضعی با زاویه قابل تنظیم، دید اتصالات کوچک را بهتر می‌کند. نور را طوری قرار دهید که بازتاب آزاردهنده روی برد ایجاد نشود.", "دود لحیم را با تهویه مناسب از محدوده تنفس دور کنید. تهویه عمومی را متناسب با حجم و نوع کار در نظر بگیرید.", "برای قطعات حساس، روش حفاظت در برابر تخلیه الکترواستاتیک را رعایت کنید. پیش از دست‌کاری برد، منبع تغذیه آن را قطع کنید."]),
    ];

    public static IReadOnlyList<string> Slugs => Articles.Concat(ExpandedArticles).Select(article => article.Slug).ToArray();

    private static readonly IReadOnlyDictionary<string, JournalArticleMetrics> Metrics =
        new Dictionary<string, JournalArticleMetrics>(StringComparer.OrdinalIgnoreCase)
        {
            ["choosing-work-drill"] = new(15400, 386, 4.9, 48),
            ["multimeter-buying-guide"] = new(12800, 342, 4.8, 42),
            ["electrical-tool-safety"] = new(9600, 218, 4.9, 27),
            ["soldering-station-basics"] = new(8200, 196, 4.7, 31),
            ["worklight-selection"] = new(7400, 152, 4.8, 19),
            ["battery-tool-care"] = new(6100, 124, 4.6, 15)
        };

    public static JournalArticleMetrics GetMetrics(string slug) =>
        Metrics.GetValueOrDefault(slug) ?? new JournalArticleMetrics(0, 0, 0, 0);

    public static MagazineDetailsModel? Find(string slug)
    {
        var allArticles = Articles.Concat(ExpandedArticles).ToArray();
        var article = allArticles.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (article is null) return null;

        var sections = article.Headings.Select((heading, index) => new MagazineSectionModel($"section-{index + 1}", heading, article.Content[index])).ToArray();
        var related = allArticles
            .Where(candidate => !string.Equals(candidate.Slug, article.Slug, StringComparison.OrdinalIgnoreCase))
            .Select(candidate => new { Article = candidate, Score = RelatedScore(article, candidate) })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Article.Title, StringComparer.Ordinal)
            .Take(8)
            .Select(candidate => new MagazineRelatedArticleModel(candidate.Article.Slug, candidate.Article.Category, candidate.Article.Title, ProductMediaCatalog.GetContentPath(candidate.Article.Image)))
            .ToArray();
        return new MagazineDetailsModel(article.Slug, article.Category, "مجله تخصصی برق و ابزار", article.Title, article.Summary, "تحریریه ندا اولیائی", "", "۶ دقیقه مطالعه", ProductMediaCatalog.GetContentPath(article.Image), ProductMediaCatalog.GetContentPath(article.Image), article.Summary, sections, "مشخصات و دستورالعمل سازندهٔ همان مدل را بررسی کنید و ابزار را متناسب با کاربرد و شرایط ایمنی انتخاب کنید.", ResolveSources(article), BuildTags(article), related, GetMetrics(article.Slug));
    }

    private static int RelatedScore(Article current, Article candidate)
    {
        var score = string.Equals(current.Category, candidate.Category, StringComparison.Ordinal) ? 2 : 0;
        var currentTopic = TopicFor(current.Slug);
        var candidateTopic = TopicFor(candidate.Slug);
        if (currentTopic == candidateTopic) score += 5;
        if (current.Slug.Contains("safety", StringComparison.Ordinal) == candidate.Slug.Contains("safety", StringComparison.Ordinal)) score++;
        return score;
    }

    private static string TopicFor(string slug) => slug switch
    {
        var value when value.Contains("solder", StringComparison.Ordinal) => "soldering",
        var value when value.Contains("multimeter", StringComparison.Ordinal) || value.Contains("measurement", StringComparison.Ordinal) => "measurement",
        var value when value.Contains("light", StringComparison.Ordinal) || value.Contains("lumen", StringComparison.Ordinal) => "lighting",
        var value when value.Contains("battery", StringComparison.Ordinal) || value.Contains("cordless", StringComparison.Ordinal) => "battery",
        var value when value.Contains("cord", StringComparison.Ordinal) || value.Contains("electrical", StringComparison.Ordinal) => "electrical",
        _ => "tools"
    };

    private static IReadOnlyList<MagazineTagModel> BuildTags(Article article)
    {
        var topic = TopicFor(article.Slug);
        var topicLabel = topic switch
        {
            "soldering" => "لحیم‌کاری و تعمیر برد",
            "measurement" => "مولتی‌متر و اندازه‌گیری",
            "lighting" => "روشنایی و چراغ کارگاهی",
            "battery" => "باتری و ابزار شارژی",
            "electrical" => "برق، کابل و سیم رابط",
            _ => "ابزار و تجهیزات کارگاهی"
        };
        var filter = topic switch
        {
            "soldering" => "soldering",
            "measurement" => "measurement",
            "lighting" => "lighting",
            "battery" => "battery",
            "electrical" => "electrical",
            _ => "tools"
        };
        var branch = article.Category switch
        {
            "راهنمای خرید" => "buying",
            "آموزش تخصصی" => "education",
            _ => "safety"
        };
        var additional = topic switch
        {
            "soldering" => new MagazineTagModel("ایمنی میز کار", "area-tools"),
            "measurement" => new MagazineTagModel("ایمنی اندازه‌گیری", "area-electrical"),
            "lighting" => new MagazineTagModel("نور محیط کار", "area-workshop"),
            "battery" => new MagazineTagModel("نگهداری ابزار", "area-tools"),
            "electrical" => new MagazineTagModel("ایمنی برق", "area-electrical"),
            _ => new MagazineTagModel("انتخاب ابزار", "area-tools")
        };
        return [new(article.Category, branch), new(topicLabel, filter), additional];
    }

    private static IReadOnlyList<MagazineSourceModel> ResolveSources(Article article)
    {
        var sources = (article.Sources ?? []).ToList();
        var topicalSources = TopicFor(article.Slug) switch
        {
            "soldering" => new[] { new MagazineSourceModel("راهنمای HAKKO برای نگهداری نوک هویه", "https://www.hakko.com/english/support/maintenance/detail.php?seq=183"), new MagazineSourceModel("راهنمای OSHA دربارهٔ تهویهٔ موضعی در محیط کار", "https://www.osha.gov/otm/section-5-construction-operations/chapter-3") },
            "measurement" => new[] { new MagazineSourceModel("راهنمای Fluke برای آزمون پیوستگی مولتی‌متر", "https://www.fluke.com/en-gb/learn/blog/digital-multimeters/how-to-test-for-continuity"), new MagazineSourceModel("OSHA 1910.334: ایمنی تجهیزات و ابزار آزمون", "https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.334") },
            "lighting" => new[] { new MagazineSourceModel("راهنمای ENERGY STAR دربارهٔ روشنایی و لومن", "https://www.energystar.gov/products/learn-about-brightness"), new MagazineSourceModel("راهنمای ENERGY STAR دربارهٔ دمای رنگ نور", "https://www.energystar.gov/products/light_fixtures/downlights-key-product-criteria") },
            "battery" => new[] { new MagazineSourceModel("راهنمای ایمنی باتری در دفترچهٔ سازندهٔ Makita؛ برای مدل‌های سازگار", "https://cdn.makitatools.com/apps/cms/doc/prod/XWT/41303269-e06f-4eb8-8eef-14ae86c69d2e_XWT05Z_IM.pdf") },
            "electrical" => new[] { new MagazineSourceModel("راهنمای OSHA برای ایمنی سیم‌های رابط", "https://www.osha.gov/electrical/hazards/flexible-cords"), new MagazineSourceModel("OSHA 1910.334: ایمنی تجهیزات و ابزار آزمون", "https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.334") },
            _ => new[] { new MagazineSourceModel("راهنمای OSHA دربارهٔ ابزارهای برقی", "https://www.osha.gov/etools/construction/electrical-incidents/power-tools"), new MagazineSourceModel("OSHA 1910.243: الزامات ابزارهای دستی و برقی", "https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.243") }
        };
        sources.AddRange(topicalSources);
        return sources.DistinctBy(source => source.Url, StringComparer.OrdinalIgnoreCase).Take(4).ToArray();
    }
}

public sealed record JournalArticleMetrics(int Views, int Likes, double AverageRating, int Comments);
