SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'007-store-runtime-datasets')
    BEGIN COMMIT TRANSACTION; RETURN; END;
    CREATE TABLE [Store].[RuntimeDataset]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [DatasetKey] NVARCHAR(100) NOT NULL,
        [PayloadJson] NVARCHAR(MAX) NOT NULL,
        [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_RuntimeDataset_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Store_RuntimeDataset] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_RuntimeDataset_DatasetKey] UNIQUE ([DatasetKey]),
        CONSTRAINT [CK_Store_RuntimeDataset_PayloadJson] CHECK (ISJSON([PayloadJson]) = 1)
    );
    INSERT INTO [Store].[RuntimeDataset] ([DatasetKey], [PayloadJson]) VALUES
        (N'catalog',CONVERT(NVARCHAR(MAX),CAST(N'' AS NVARCHAR(MAX))+N'{
  "schemaVersion": 2,
  "specificationGroups": {"identity":{"name":"شناسه و کاربرد","order":10},"operation":{"name":"ویژگی‌های عملکردی","order":20},"safety":{"name":"ایمنی و انتخاب","order":30},"accessories":{"name":"لوازم و متعلقات","order":40}},
  "specificationCategories": {"tool-type":{"groupId":"identity","name":"نوع ابزار","order":10},"application":{"groupId":"identity","name":"کاربری","order":20},"display":{"groupId":"operation","name":"نمایش و عملکرد","order":10},"power":{"groupId":"operation","name":"تغذیه و شیوهٔ عملکرد","order":20},"lighting":{"groupId":"operation","name":"روشنایی","order":30},"selection-safety":{"groupId":"safety","name":"نکات ایمنی و انتخاب","order":10},"soldering":{"groupId":"operation","name":"کنترل فرایند لحیم‌کاری","order":40},"accessories-list":{"groupId":"accessories","name":"لوازم پیشنهادی","order":10},"drilling-method":{"groupId":"operation","name":"روش سوراخ‌کاری","order":50},"chuck":{"groupId":"operation","name":"سه‌نظام و نگهداری مته","order":60},"drive-control":{"groupId":"operation","name":"کنترل و پیچ‌کاری","order":70},"meter-form":{"groupId":"identity","name":"فرم دستگاه اندازه‌گیری","order":30},"measurement-task":{"groupId":"operation","name":"کار اندازه‌گیری","order":80},"solder-tool":{"groupId":"identity","name":"نوع ابزار لحیم‌کاری","order":40},"heating-method":{"groupId":"operation","name":"روش گرمایش","order":90},"lighting-format":{"groupId":"identity","name":"ساختار چراغ","order":50},"lighting-task":{"groupId":"operation","name":"کاربری روشنایی","order":100},"hand-tool-form":{"groupId":"identity","name":"نوع ابزار دستی","order":60},"tool-set":{"groupId":"accessories","name":"ساختار مجموعه","order":20},"tester-method":{"groupId":"operation","name":"نوع آزمون","order":110},"tester-form":{"groupId":"identity","name":"نوع تستر","order":70}},
  "specificationFeatures": {"tool-kind":{"categoryId":"tool-type","label":"نوع ابزار","order":10},"usage":{"categoryId":"application","label":"کاربری","order":10},"display-type":{"categoryId":"display","label":"نوع نمایش","order":10},"power-source":{"categoryId":"power","label":"منبع تغذیه یا شیوهٔ عملکرد","order":10},"light-source":{"categoryId":"lighting","label":"نوع روشنایی","order":10},"safety-note":{"categoryId":"selection-safety","label":"نکتهٔ ایمنی","order":10},"selection-note":{"categoryId":"selection-safety","label":"نکتهٔ انتخاب","order":20},"temperature-control":{"categoryId":"soldering","label":"کنترل فرایند","order":10},"recommended-accessories":{"categoryId":"accessories-list","label":"لوازم پیشنهادی","order":10},"drill-mode":{"categoryId":"drilling-method","label":"نوع سوراخ‌کاری","order":10},"chuck-type":{"categoryId":"chuck","label":"نوع سه‌نظام","order":10},"drive-speed":{"categoryId":"drive-control","label":"تعداد سرعت","order":10},"drive-use":{"categoryId":"drive-control","label":"کاربری پیچ‌کاری","order":20},"meter-format":{"categoryId":"meter-form","label":"فرم دستگاه","order":10},"measurement-task":{"categoryId":"measurement-task","label":"کار اندازه‌گیری","order":10},"solder-format":{"categoryId":"solder-tool","label":"فرم ابزار","order":10},"heating-method":{"categoryId":"heating-method","label":"روش گرمایش","order":10},"lighting-format":{"categoryId":"lighting-format","label":"ساختار چراغ","order":10},"lighting-task":{"categoryId":"lighting-task","label":"کاربری روشنایی","order":10},"hand-tool-form":{"categoryId":"hand-tool-form","label":"نوع ابزار دستی","order":10},"set-content":{"categoryId":"tool-set","label":"ساختا'+N'ر مجموعه","order":10},"tester-method":{"categoryId":"tester-method","label":"روش آزمون","order":10},"tester-form":{"categoryId":"tester-form","label":"فرم تستر","order":10}},












  "attributeDefinitions": {
    "brand": {
      "label": "برند",
      "icon": "bi-patch-check",
      "source": "brand",
      "filterKey": "brand",
      "is_filterable": true,
      "is_highlight": true
    },
    "category": {
      "label": "دسته‌بندی",
      "icon": "bi-grid",
      "source": "type",
      "filterKey": "type",
      "is_filterable": true,
      "is_highlight": true
    },
    "country": {
      "label": "کشور سازنده",
      "icon": "bi-globe2",
      "source": "country",
      "is_filterable": false,
      "is_highlight": true
    },
    "code": {
      "label": "کد محصول",
      "icon": "bi-upc-scan",
      "source": "code",
      "is_filterable": false,
      "is_highlight": true
    },
    "powerSource": {
      "label": "منبع تغذیه",
      "icon": "bi-lightning-charge",
      "source": "powerSource",
      "filterKey": "movement",
      "is_filterable": true,
      "is_highlight": true
    },
    "useCase": {
      "label": "مورد استفاده",
      "icon": "bi-building-gear",
      "source": "useCase",
      "filterKey": "strap",
      "is_filterable": true,
      "is_highlight": true
    },
    "color": {
      "label": "رنگ",
      "icon": "bi-palette2",
      "source": "color",
      "filterKey": "color",
      "is_filterable": true,
      "is_highlight": true
    },
    "toolType": {
      "label": "نوع ابزار",
      "icon": "bi-tools",
      "source": "spec:نوع ابزار",
      "is_filterable": false,
      "is_highlight": true
    },
    "displayType": {
      "label": "نوع نمایش",
      "icon": "bi-display",
      "source": "spec:نوع نمایش",
      "is_filterable": false,
      "is_highlight": true
    },
    "measurement": {
      "label": "اندازه‌گیری",
      "icon": "bi-speedometer2",
      "source": "spec:اندازه‌گیری",
      "is_filterable": false,
      "is_highlight": true
    },
    "lightingType": {
      "label": "نوع روشنایی",
      "icon": "bi-lightbulb",
      "source": "spec:نوع روشنایی",
      "is_filterable": false,
      "is_highlight": true
    },
    "productType": {
      "label": "نوع محصول",
      "icon": "bi-box-seam",
      "source": "spec:نوع محصول",
      "is_filterable": false,
      "is_highlight": true
    },
    "temperatureControl": {
      "label": "کنترل دما",
      "icon": "bi-thermometer-half",
      "source": "spec:کنترل دما",
      "is_filterable": false,
      "is_highlight": true
    },
    "technicalSpecs": {
      "label": "مشخصات فنی",
      "icon": "bi-list-check",
      "source": "specs",
      "is_filterable": false,
      "is_highlight": true
    },
    "variantOptions": {
      "label": "گزینه‌های قابل انتخاب",
      "icon": "bi-sliders",
      "source": "variants",
      "is_filterable": false,
      "is_highlight": true
    },
    "price": {
      "label": "بازهٔ قیمت",
      "icon": "bi-cash-stack",
      "source": "price",
      "filterKey": "price",
      "is_filterable": true,
      "is_highlight": false
    },
    "availability": {
      "label": "موجودی",
      "icon": "bi-box-seam",
      "source": "stockQuantity",
      "filterKey": "availability",
      "is_filterable": true,
      "is_highlight": false
    },
    "discount": {
   '+N'   "label": "تخفیف",
      "icon": "bi-tags",
      "source": "discountPercent",
      "filterKey": "discount",
      "is_filterable": true,
      "is_highlight": false
    }
  },
  "brands": {
    "bosch": {
      "id": "bosch",
      "name": "Bosch",
      "fa": "بوش",
      "logo": "content/brands/bosch.svg",
      "countryCode": "DE"
    },
    "makita": {
      "id": "makita",
      "name": "Makita",
      "fa": "ماکیتا",
      "logo": "content/brands/makita.svg",
      "countryCode": "JP"
    },
    "dewalt": {
      "id": "dewalt",
      "name": "DeWalt",
      "fa": "دیوالت",
      "logo": "content/brands/dewalt.svg",
      "countryCode": "US"
    },
    "ronix": {
      "id": "ronix",
      "name": "Ronix",
      "fa": "رونیکس",
      "logo": "content/brands/ronix.png",
      "countryCode": "IR"
    },
    "tosan": {
      "id": "tosan",
      "name": "Tosan",
      "fa": "توسن",
      "logo": "content/brands/tosan.webp",
      "countryCode": "IR"
    },
    "uni-t": {
      "id": "uni-t",
      "name": "UNI-T",
      "fa": "یونی‌تی",
      "logo": "content/brands/uni-t.png",
      "countryCode": "CN"
    },
    "fluke": {
      "id": "fluke",
      "name": "Fluke",
      "fa": "فلوک",
      "logo": "content/brands/fluke.svg",
      "countryCode": "US"
    },
    "mastech": {
      "id": "mastech",
      "name": "Mastech",
      "fa": "مستک",
      "logo": "content/brands/mastech.png",
      "countryCode": "CN"
    },
    "kyoritsu": {
      "id": "kyoritsu",
      "name": "Kyoritsu",
      "fa": "کیوریتسو",
      "logo": "content/brands/kyoritsu.png",
      "countryCode": "JP"
    },
    "yihua": {
      "id": "yihua",
      "name": "Yihua",
      "fa": "یوهوا",
      "logo": "content/brands/yihua.png",
      "countryCode": "CN"
    },
    "hakko": {
      "id": "hakko",
      "name": "Hakko",
      "fa": "هاکو",
      "logo": "content/brands/hakko.png",
      "countryCode": "JP"
    },
    "weller": {
      "id": "weller",
      "name": "Weller",
      "fa": "ولر",
      "logo": "content/brands/weller.svg",
      "countryCode": "DE"
    },
    "philips": {
      "id": "philips",
      "name": "Philips",
      "fa": "فیلیپس",
      "logo": "content/brands/philips.svg",
      "countryCode": "NL"
    },
    "wiha": {
      "id": "wiha",
      "name": "Wiha",
      "fa": "وایها",
      "logo": "content/brands/wiha.svg",
      "countryCode": "DE"
    },
    "arva": {
      "id": "arva",
      "name": "Arva",
      "fa": "آروا",
      "logo": null,
      "countryCode": "IR"
    },
    "victor": {
      "id": "victor",
      "name": "Victor",
      "fa": "ویکتور",
      "logo": null,
      "countryCode": "CN"
    },
    "quick": {
      "id": "quick",
      "name": "Quick",
      "fa": "کوئیک",
      "logo": null,
      "countryCode": "CN"
    },
    "goot": {
      "id": "goot",
      "name": "GOOT",
      "fa": "گوت",
      "logo": null,
      "countryCode": "JP"
    },
    "knipex": {
      "id": "knipex",
      "name": "Knipex",
      "fa": "کنیپکس",
      "logo": null,
      "countryCode": "DE"
    },
    "stanley": {
      "id": "stanley",
      "name": "Stanley",
      "fa": "استنلی",
      "logo": null,
      "countryCode": "US"
    },
    "benning": {
      "id": "benning",
      "name": "Benning",
      "fa": "بننینگ",
      "logo": null,
      "countryCode": "DE"
    }'+N'
  },
  "countries": {
    "AD": {
      "code": "AD",
      "fa": "آندورا",
      "en": "Andorra",
      "flag": "content/countries/flags/ad.svg"
    },
    "AE": {
      "code": "AE",
      "fa": "امارات متحدهٔ عربی",
      "en": "United Arab Emirates",
      "flag": "content/countries/flags/ae.svg"
    },
    "AF": {
      "code": "AF",
      "fa": "افغانستان",
      "en": "Afghanistan",
      "flag": "content/countries/flags/af.svg"
    },
    "AG": {
      "code": "AG",
      "fa": "آنتیگوا و باربودا",
      "en": "Antigua & Barbuda",
      "flag": "content/countries/flags/ag.svg"
    },
    "AI": {
      "code": "AI",
      "fa": "آنگویلا",
      "en": "Anguilla",
      "flag": "content/countries/flags/ai.svg"
    },
    "AL": {
      "code": "AL",
      "fa": "آلبانی",
      "en": "Albania",
      "flag": "content/countries/flags/al.svg"
    },
    "AM": {
      "code": "AM",
      "fa": "ارمنستان",
      "en": "Armenia",
      "flag": "content/countries/flags/am.svg"
    },
    "AO": {
      "code": "AO",
      "fa": "آنگولا",
      "en": "Angola",
      "flag": "content/countries/flags/ao.svg"
    },
    "AQ": {
      "code": "AQ",
      "fa": "جنوبگان",
      "en": "Antarctica",
      "flag": "content/countries/flags/aq.svg"
    },
    "AR": {
      "code": "AR",
      "fa": "آرژانتین",
      "en": "Argentina",
      "flag": "content/countries/flags/ar.svg"
    },
    "AS": {
      "code": "AS",
      "fa": "ساموآی امریکا",
      "en": "American Samoa",
      "flag": "content/countries/flags/as.svg"
    },
    "AT": {
      "code": "AT",
      "fa": "اتریش",
      "en": "Austria",
      "flag": "content/countries/flags/at.svg"
    },
    "AU": {
      "code": "AU",
      "fa": "استرالیا",
      "en": "Australia",
      "flag": "content/countries/flags/au.svg"
    },
    "AW": {
      "code": "AW",
      "fa": "آروبا",
      "en": "Aruba",
      "flag": "content/countries/flags/aw.svg"
    },
    "AX": {
      "code": "AX",
      "fa": "جزایر آلاند",
      "en": "Åland Islands",
      "flag": "content/countries/flags/ax.svg"
    },
    "AZ": {
      "code": "AZ",
      "fa": "جمهوری آذربایجان",
      "en": "Azerbaijan",
      "flag": "content/countries/flags/az.svg"
    },
    "BA": {
      "code": "BA",
      "fa": "بوسنی و هرزگوین",
      "en": "Bosnia & Herzegovina",
      "flag": "content/countries/flags/ba.svg"
    },
    "BB": {
      "code": "BB",
      "fa": "باربادوس",
      "en": "Barbados",
      "flag": "content/countries/flags/bb.svg"
    },
    "BD": {
      "code": "BD",
      "fa": "بنگلادش",
      "en": "Bangladesh",
      "flag": "content/countries/flags/bd.svg"
    },
    "BE": {
      "code": "BE",
      "fa": "بلژیک",
      "en": "Belgium",
      "flag": "content/countries/flags/be.svg"
    },
    "BF": {
      "code": "BF",
      "fa": "بورکینافاسو",
      "en": "Burkina Faso",
      "flag": "content/countries/flags/bf.svg"
    },
    "BG": {
      "code": "BG",
      "fa": "بلغارستان",
      "en": "Bulgaria",
      "flag": "content/countries/flags/bg.svg"
    },
    "BH": {
      "code": "BH",
      "fa": "بحرین",
      "en": "Bahrain",
      "flag": "content/countries/flags/bh.svg"
    },
    "BI": {
      "code": "BI",
      "fa": "بوروندی",
      "en": "Burundi",
      "flag": "content/countries/flags/bi.svg"
    },
    "BJ": {
      "code": "BJ",
      "fa'+N'": "بنین",
      "en": "Benin",
      "flag": "content/countries/flags/bj.svg"
    },
    "BL": {
      "code": "BL",
      "fa": "سن بارتلمی",
      "en": "St. Barthélemy",
      "flag": "content/countries/flags/bl.svg"
    },
    "BM": {
      "code": "BM",
      "fa": "برمودا",
      "en": "Bermuda",
      "flag": "content/countries/flags/bm.svg"
    },
    "BN": {
      "code": "BN",
      "fa": "برونئی",
      "en": "Brunei",
      "flag": "content/countries/flags/bn.svg"
    },
    "BO": {
      "code": "BO",
      "fa": "بولیوی",
      "en": "Bolivia",
      "flag": "content/countries/flags/bo.svg"
    },
    "BQ": {
      "code": "BQ",
      "fa": "جزایر کارائیب هلند",
      "en": "Caribbean Netherlands",
      "flag": "content/countries/flags/bq.svg"
    },
    "BR": {
      "code": "BR",
      "fa": "برزیل",
      "en": "Brazil",
      "flag": "content/countries/flags/br.svg"
    },
    "BS": {
      "code": "BS",
      "fa": "باهاما",
      "en": "Bahamas",
      "flag": "content/countries/flags/bs.svg"
    },
    "BT": {
      "code": "BT",
      "fa": "بوتان",
      "en": "Bhutan",
      "flag": "content/countries/flags/bt.svg"
    },
    "BV": {
      "code": "BV",
      "fa": "جزیرهٔ بووه",
      "en": "Bouvet Island",
      "flag": "content/countries/flags/bv.svg"
    },
    "BW": {
      "code": "BW",
      "fa": "بوتسوانا",
      "en": "Botswana",
      "flag": "content/countries/flags/bw.svg"
    },
    "BY": {
      "code": "BY",
      "fa": "بلاروس",
      "en": "Belarus",
      "flag": "content/countries/flags/by.svg"
    },
    "BZ": {
      "code": "BZ",
      "fa": "بلیز",
      "en": "Belize",
      "flag": "content/countries/flags/bz.svg"
    },
    "CA": {
      "code": "CA",
      "fa": "کانادا",
      "en": "Canada",
      "flag": "content/countries/flags/ca.svg"
    },
    "CC": {
      "code": "CC",
      "fa": "جزایر کوکوس",
      "en": "Cocos (Keeling) Islands",
      "flag": "content/countries/flags/cc.svg"
    },
    "CD": {
      "code": "CD",
      "fa": "کنگو - کینشاسا",
      "en": "Congo - Kinshasa",
      "flag": "content/countries/flags/cd.svg"
    },
    "CF": {
      "code": "CF",
      "fa": "جمهوری افریقای مرکزی",
      "en": "Central African Republic",
      "flag": "content/countries/flags/cf.svg"
    },
    "CG": {
      "code": "CG",
      "fa": "کنگو - برازویل",
      "en": "Congo - Brazzaville",
      "flag": "content/countries/flags/cg.svg"
    },
    "CH": {
      "code": "CH",
      "fa": "سوئیس",
      "en": "Switzerland",
      "flag": "content/countries/flags/ch.svg"
    },
    "CI": {
      "code": "CI",
      "fa": "ساحل عاج",
      "en": "Côte d’Ivoire",
      "flag": "content/countries/flags/ci.svg"
    },
    "CK": {
      "code": "CK",
      "fa": "جزایر کوک",
      "en": "Cook Islands",
      "flag": "content/countries/flags/ck.svg"
    },
    "CL": {
      "code": "CL",
      "fa": "شیلی",
      "en": "Chile",
      "flag": "content/countries/flags/cl.svg"
    },
    "CM": {
      "code": "CM",
      "fa": "کامرون",
      "en": "Cameroon",
      "flag": "content/countries/flags/cm.svg"
    },
    "CN": {
      "code": "CN",
      "fa": "چین",
      "en": "China",
      "flag": "content/countries/flags/cn.svg"
    },
    "CO": {
      "code": "CO",
      "fa": "کلمبیا",
      "en": "Colombia",
      "flag": "content/countries/fl'+N'ags/co.svg"
    },
    "CP": {
      "code": "CP",
      "fa": "جزایر کلیپرتون",
      "en": "Clipperton Island",
      "flag": "content/countries/flags/cp.svg"
    },
    "CR": {
      "code": "CR",
      "fa": "کاستاریکا",
      "en": "Costa Rica",
      "flag": "content/countries/flags/cr.svg"
    },
    "CU": {
      "code": "CU",
      "fa": "کوبا",
      "en": "Cuba",
      "flag": "content/countries/flags/cu.svg"
    },
    "CV": {
      "code": "CV",
      "fa": "کیپ‌ورد",
      "en": "Cape Verde",
      "flag": "content/countries/flags/cv.svg"
    },
    "CW": {
      "code": "CW",
      "fa": "کوراسائو",
      "en": "Curaçao",
      "flag": "content/countries/flags/cw.svg"
    },
    "CX": {
      "code": "CX",
      "fa": "جزیرهٔ کریسمس",
      "en": "Christmas Island",
      "flag": "content/countries/flags/cx.svg"
    },
    "CY": {
      "code": "CY",
      "fa": "قبرس",
      "en": "Cyprus",
      "flag": "content/countries/flags/cy.svg"
    },
    "CZ": {
      "code": "CZ",
      "fa": "چک",
      "en": "Czechia",
      "flag": "content/countries/flags/cz.svg"
    },
    "DE": {
      "code": "DE",
      "fa": "آلمان",
      "en": "Germany",
      "flag": "content/countries/flags/de.svg"
    },
    "DG": {
      "code": "DG",
      "fa": "دیه‌گو گارسیا",
      "en": "Diego Garcia",
      "flag": "content/countries/flags/dg.svg"
    },
    "DJ": {
      "code": "DJ",
      "fa": "جیبوتی",
      "en": "Djibouti",
      "flag": "content/countries/flags/dj.svg"
    },
    "DK": {
      "code": "DK",
      "fa": "دانمارک",
      "en": "Denmark",
      "flag": "content/countries/flags/dk.svg"
    },
    "DM": {
      "code": "DM",
      "fa": "دومینیکا",
      "en": "Dominica",
      "flag": "content/countries/flags/dm.svg"
    },
    "DO": {
      "code": "DO",
      "fa": "جمهوری دومینیکن",
      "en": "Dominican Republic",
      "flag": "content/countries/flags/do.svg"
    },
    "DZ": {
      "code": "DZ",
      "fa": "الجزایر",
      "en": "Algeria",
      "flag": "content/countries/flags/dz.svg"
    },
    "EC": {
      "code": "EC",
      "fa": "اکوادور",
      "en": "Ecuador",
      "flag": "content/countries/flags/ec.svg"
    },
    "EE": {
      "code": "EE",
      "fa": "استونی",
      "en": "Estonia",
      "flag": "content/countries/flags/ee.svg"
    },
    "EG": {
      "code": "EG",
      "fa": "مصر",
      "en": "Egypt",
      "flag": "content/countries/flags/eg.svg"
    },
    "EH": {
      "code": "EH",
      "fa": "صحرای غربی",
      "en": "Western Sahara",
      "flag": "content/countries/flags/eh.svg"
    },
    "ER": {
      "code": "ER",
      "fa": "اریتره",
      "en": "Eritrea",
      "flag": "content/countries/flags/er.svg"
    },
    "ES": {
      "code": "ES",
      "fa": "اسپانیا",
      "en": "Spain",
      "flag": "content/countries/flags/es.svg"
    },
    "ET": {
      "code": "ET",
      "fa": "اتیوپی",
      "en": "Ethiopia",
      "flag": "content/countries/flags/et.svg"
    },
    "EU": {
      "code": "EU",
      "fa": "اتحادیهٔ اروپا",
      "en": "European Union",
      "flag": "content/countries/flags/eu.svg"
    },
    "FI": {
      "code": "FI",
      "fa": "فنلاند",
      "en": "Finland",
      "flag": "content/countries/flags/fi.svg"
    },
    "FJ": {
      "code": "FJ",
      "fa": "فیجی",
      "en": "Fiji",
      "flag": "content'+N'/countries/flags/fj.svg"
    },
    "FK": {
      "code": "FK",
      "fa": "جزایر فالکلند",
      "en": "Falkland Islands",
      "flag": "content/countries/flags/fk.svg"
    },
    "FM": {
      "code": "FM",
      "fa": "میکرونزی",
      "en": "Micronesia",
      "flag": "content/countries/flags/fm.svg"
    },
    "FO": {
      "code": "FO",
      "fa": "جزایر فارو",
      "en": "Faroe Islands",
      "flag": "content/countries/flags/fo.svg"
    },
    "FR": {
      "code": "FR",
      "fa": "فرانسه",
      "en": "France",
      "flag": "content/countries/flags/fr.svg"
    },
    "GA": {
      "code": "GA",
      "fa": "گابن",
      "en": "Gabon",
      "flag": "content/countries/flags/ga.svg"
    },
    "GB": {
      "code": "GB",
      "fa": "بریتانیا",
      "en": "United Kingdom",
      "flag": "content/countries/flags/gb.svg"
    },
    "GD": {
      "code": "GD",
      "fa": "گرنادا",
      "en": "Grenada",
      "flag": "content/countries/flags/gd.svg"
    },
    "GE": {
      "code": "GE",
      "fa": "گرجستان",
      "en": "Georgia",
      "flag": "content/countries/flags/ge.svg"
    },
    "GF": {
      "code": "GF",
      "fa": "گویان فرانسه",
      "en": "French Guiana",
      "flag": "content/countries/flags/gf.svg"
    },
    "GG": {
      "code": "GG",
      "fa": "گرنزی",
      "en": "Guernsey",
      "flag": "content/countries/flags/gg.svg"
    },
    "GH": {
      "code": "GH",
      "fa": "غنا",
      "en": "Ghana",
      "flag": "content/countries/flags/gh.svg"
    },
    "GI": {
      "code": "GI",
      "fa": "جبل‌الطارق",
      "en": "Gibraltar",
      "flag": "content/countries/flags/gi.svg"
    },
    "GL": {
      "code": "GL",
      "fa": "گرینلند",
      "en": "Greenland",
      "flag": "content/countries/flags/gl.svg"
    },
    "GM": {
      "code": "GM",
      "fa": "گامبیا",
      "en": "Gambia",
      "flag": "content/countries/flags/gm.svg"
    },
    "GN": {
      "code": "GN",
      "fa": "گینه",
      "en": "Guinea",
      "flag": "content/countries/flags/gn.svg"
    },
    "GP": {
      "code": "GP",
      "fa": "گوادلوپ",
      "en": "Guadeloupe",
      "flag": "content/countries/flags/gp.svg"
    },
    "GQ": {
      "code": "GQ",
      "fa": "گینهٔ استوایی",
      "en": "Equatorial Guinea",
      "flag": "content/countries/flags/gq.svg"
    },
    "GR": {
      "code": "GR",
      "fa": "یونان",
      "en": "Greece",
      "flag": "content/countries/flags/gr.svg"
    },
    "GS": {
      "code": "GS",
      "fa": "جورجیای جنوبی و جزایر ساندویچ جنوبی",
      "en": "South Georgia & South Sandwich Islands",
      "flag": "content/countries/flags/gs.svg"
    },
    "GT": {
      "code": "GT",
      "fa": "گواتمالا",
      "en": "Guatemala",
      "flag": "content/countries/flags/gt.svg"
    },
    "GU": {
      "code": "GU",
      "fa": "گوام",
      "en": "Guam",
      "flag": "content/countries/flags/gu.svg"
    },
    "GW": {
      "code": "GW",
      "fa": "گینهٔ بیسائو",
      "en": "Guinea-Bissau",
      "flag": "content/countries/flags/gw.svg"
    },
    "GY": {
      "code": "GY",
      "fa": "گویان",
      "en": "Guyana",
      "flag": "content/countries/flags/gy.svg"
    },
    "HK": {
      "code": "HK",
      "fa": "هنگ‌کنگ، منطقهٔ ویژهٔ اداری چین",
      "en": "Hong Kong SAR China",
      "flag": "content/countries/flags/hk.svg"
    },
    "'+N'HM": {
      "code": "HM",
      "fa": "هرد و جزایر مک‌دونالد",
      "en": "Heard & McDonald Islands",
      "flag": "content/countries/flags/hm.svg"
    },
    "HN": {
      "code": "HN",
      "fa": "هندوراس",
      "en": "Honduras",
      "flag": "content/countries/flags/hn.svg"
    },
    "HR": {
      "code": "HR",
      "fa": "کرواسی",
      "en": "Croatia",
      "flag": "content/countries/flags/hr.svg"
    },
    "HT": {
      "code": "HT",
      "fa": "هائیتی",
      "en": "Haiti",
      "flag": "content/countries/flags/ht.svg"
    },
    "HU": {
      "code": "HU",
      "fa": "مجارستان",
      "en": "Hungary",
      "flag": "content/countries/flags/hu.svg"
    },
    "IC": {
      "code": "IC",
      "fa": "جزایر قناری",
      "en": "Canary Islands",
      "flag": "content/countries/flags/ic.svg"
    },
    "ID": {
      "code": "ID",
      "fa": "اندونزی",
      "en": "Indonesia",
      "flag": "content/countries/flags/id.svg"
    },
    "IE": {
      "code": "IE",
      "fa": "ایرلند",
      "en": "Ireland",
      "flag": "content/countries/flags/ie.svg"
    },
    "IL": {
      "code": "IL",
      "fa": "اسرائیل",
      "en": "Israel",
      "flag": "content/countries/flags/il.svg"
    },
    "IM": {
      "code": "IM",
      "fa": "جزیرهٔ من",
      "en": "Isle of Man",
      "flag": "content/countries/flags/im.svg"
    },
    "IN": {
      "code": "IN",
      "fa": "هند",
      "en": "India",
      "flag": "content/countries/flags/in.svg"
    },
    "IO": {
      "code": "IO",
      "fa": "قلمرو بریتانیا در اقیانوس هند",
      "en": "British Indian Ocean Territory",
      "flag": "content/countries/flags/io.svg"
    },
    "IQ": {
      "code": "IQ",
      "fa": "عراق",
      "en": "Iraq",
      "flag": "content/countries/flags/iq.svg"
    },
    "IR": {
      "code": "IR",
      "fa": "ایران",
      "en": "Iran",
      "flag": "content/countries/flags/ir.svg"
    },
    "IS": {
      "code": "IS",
      "fa": "ایسلند",
      "en": "Iceland",
      "flag": "content/countries/flags/is.svg"
    },
    "IT": {
      "code": "IT",
      "fa": "ایتالیا",
      "en": "Italy",
      "flag": "content/countries/flags/it.svg"
    },
    "JE": {
      "code": "JE",
      "fa": "جرزی",
      "en": "Jersey",
      "flag": "content/countries/flags/je.svg"
    },
    "JM": {
      "code": "JM",
      "fa": "جامائیکا",
      "en": "Jamaica",
      "flag": "content/countries/flags/jm.svg"
    },
    "JO": {
      "code": "JO",
      "fa": "اردن",
      "en": "Jordan",
      "flag": "content/countries/flags/jo.svg"
    },
    "JP": {
      "code": "JP",
      "fa": "ژاپن",
      "en": "Japan",
      "flag": "content/countries/flags/jp.svg"
    },
    "KE": {
      "code": "KE",
      "fa": "کنیا",
      "en": "Kenya",
      "flag": "content/countries/flags/ke.svg"
    },
    "KG": {
      "code": "KG",
      "fa": "قرقیزستان",
      "en": "Kyrgyzstan",
      "flag": "content/countries/flags/kg.svg"
    },
    "KH": {
      "code": "KH",
      "fa": "کامبوج",
      "en": "Cambodia",
      "flag": "content/countries/flags/kh.svg"
    },
    "KI": {
      "code": "KI",
      "fa": "کیریباتی",
      "en": "Kiribati",
      "flag": "content/countries/flags/ki.svg"
    },
    "KM": {
      "code": "KM",
      "fa": "کومور",
      "en": "Comoros",
      "flag": "content/countries/flags/km.svg"
    '+N'},
    "KN": {
      "code": "KN",
      "fa": "سنت کیتس و نویس",
      "en": "St. Kitts & Nevis",
      "flag": "content/countries/flags/kn.svg"
    },
    "KP": {
      "code": "KP",
      "fa": "کرهٔ شمالی",
      "en": "North Korea",
      "flag": "content/countries/flags/kp.svg"
    },
    "KR": {
      "code": "KR",
      "fa": "کرهٔ جنوبی",
      "en": "South Korea",
      "flag": "content/countries/flags/kr.svg"
    },
    "KW": {
      "code": "KW",
      "fa": "کویت",
      "en": "Kuwait",
      "flag": "content/countries/flags/kw.svg"
    },
    "KY": {
      "code": "KY",
      "fa": "جزایر کِیمن",
      "en": "Cayman Islands",
      "flag": "content/countries/flags/ky.svg"
    },
    "KZ": {
      "code": "KZ",
      "fa": "قزاقستان",
      "en": "Kazakhstan",
      "flag": "content/countries/flags/kz.svg"
    },
    "LA": {
      "code": "LA",
      "fa": "لائوس",
      "en": "Laos",
      "flag": "content/countries/flags/la.svg"
    },
    "LB": {
      "code": "LB",
      "fa": "لبنان",
      "en": "Lebanon",
      "flag": "content/countries/flags/lb.svg"
    },
    "LC": {
      "code": "LC",
      "fa": "سنت لوسیا",
      "en": "St. Lucia",
      "flag": "content/countries/flags/lc.svg"
    },
    "LI": {
      "code": "LI",
      "fa": "لیختن‌اشتاین",
      "en": "Liechtenstein",
      "flag": "content/countries/flags/li.svg"
    },
    "LK": {
      "code": "LK",
      "fa": "سری‌لانکا",
      "en": "Sri Lanka",
      "flag": "content/countries/flags/lk.svg"
    },
    "LR": {
      "code": "LR",
      "fa": "لیبریا",
      "en": "Liberia",
      "flag": "content/countries/flags/lr.svg"
    },
    "LS": {
      "code": "LS",
      "fa": "لسوتو",
      "en": "Lesotho",
      "flag": "content/countries/flags/ls.svg"
    },
    "LT": {
      "code": "LT",
      "fa": "لیتوانی",
      "en": "Lithuania",
      "flag": "content/countries/flags/lt.svg"
    },
    "LU": {
      "code": "LU",
      "fa": "لوکزامبورگ",
      "en": "Luxembourg",
      "flag": "content/countries/flags/lu.svg"
    },
    "LV": {
      "code": "LV",
      "fa": "لتونی",
      "en": "Latvia",
      "flag": "content/countries/flags/lv.svg"
    },
    "LY": {
      "code": "LY",
      "fa": "لیبی",
      "en": "Libya",
      "flag": "content/countries/flags/ly.svg"
    },
    "MA": {
      "code": "MA",
      "fa": "مراکش",
      "en": "Morocco",
      "flag": "content/countries/flags/ma.svg"
    },
    "MC": {
      "code": "MC",
      "fa": "موناکو",
      "en": "Monaco",
      "flag": "content/countries/flags/mc.svg"
    },
    "MD": {
      "code": "MD",
      "fa": "مولداوی",
      "en": "Moldova",
      "flag": "content/countries/flags/md.svg"
    },
    "ME": {
      "code": "ME",
      "fa": "مونته‌نگرو",
      "en": "Montenegro",
      "flag": "content/countries/flags/me.svg"
    },
    "MF": {
      "code": "MF",
      "fa": "سنت مارتین",
      "en": "St. Martin",
      "flag": "content/countries/flags/mf.svg"
    },
    "MG": {
      "code": "MG",
      "fa": "ماداگاسکار",
      "en": "Madagascar",
      "flag": "content/countries/flags/mg.svg"
    },
    "MH": {
      "code": "MH",
      "fa": "جزایر مارشال",
      "en": "Marshall Islands",
      "flag": "content/countries/flags/mh.svg"
    },
    "MK": {
      "code": "MK",
      "fa": "مقدونیهٔ شمالی",
      "en": "North Macedonia",
      "'+N'flag": "content/countries/flags/mk.svg"
    },
    "ML": {
      "code": "ML",
      "fa": "مالی",
      "en": "Mali",
      "flag": "content/countries/flags/ml.svg"
    },
    "MM": {
      "code": "MM",
      "fa": "میانمار (برمه)",
      "en": "Myanmar (Burma)",
      "flag": "content/countries/flags/mm.svg"
    },
    "MN": {
      "code": "MN",
      "fa": "مغولستان",
      "en": "Mongolia",
      "flag": "content/countries/flags/mn.svg"
    },
    "MO": {
      "code": "MO",
      "fa": "ماکائو، منطقهٔ ویژهٔ اداری چین",
      "en": "Macao SAR China",
      "flag": "content/countries/flags/mo.svg"
    },
    "MP": {
      "code": "MP",
      "fa": "جزایر ماریانای شمالی",
      "en": "Northern Mariana Islands",
      "flag": "content/countries/flags/mp.svg"
    },
    "MQ": {
      "code": "MQ",
      "fa": "مارتینیک",
      "en": "Martinique",
      "flag": "content/countries/flags/mq.svg"
    },
    "MR": {
      "code": "MR",
      "fa": "موریتانی",
      "en": "Mauritania",
      "flag": "content/countries/flags/mr.svg"
    },
    "MS": {
      "code": "MS",
      "fa": "مونت‌سرات",
      "en": "Montserrat",
      "flag": "content/countries/flags/ms.svg"
    },
    "MT": {
      "code": "MT",
      "fa": "مالت",
      "en": "Malta",
      "flag": "content/countries/flags/mt.svg"
    },
    "MU": {
      "code": "MU",
      "fa": "موریس",
      "en": "Mauritius",
      "flag": "content/countries/flags/mu.svg"
    },
    "MV": {
      "code": "MV",
      "fa": "مالدیو",
      "en": "Maldives",
      "flag": "content/countries/flags/mv.svg"
    },
    "MW": {
      "code": "MW",
      "fa": "مالاوی",
      "en": "Malawi",
      "flag": "content/countries/flags/mw.svg"
    },
    "MX": {
      "code": "MX",
      "fa": "مکزیک",
      "en": "Mexico",
      "flag": "content/countries/flags/mx.svg"
    },
    "MY": {
      "code": "MY",
      "fa": "مالزی",
      "en": "Malaysia",
      "flag": "content/countries/flags/my.svg"
    },
    "MZ": {
      "code": "MZ",
      "fa": "موزامبیک",
      "en": "Mozambique",
      "flag": "content/countries/flags/mz.svg"
    },
    "NA": {
      "code": "NA",
      "fa": "نامیبیا",
      "en": "Namibia",
      "flag": "content/countries/flags/na.svg"
    },
    "NC": {
      "code": "NC",
      "fa": "کالدونیای جدید",
      "en": "New Caledonia",
      "flag": "content/countries/flags/nc.svg"
    },
    "NE": {
      "code": "NE",
      "fa": "نیجر",
      "en": "Niger",
      "flag": "content/countries/flags/ne.svg"
    },
    "NF": {
      "code": "NF",
      "fa": "جزیرهٔ نورفولک",
      "en": "Norfolk Island",
      "flag": "content/countries/flags/nf.svg"
    },
    "NG": {
      "code": "NG",
      "fa": "نیجریه",
      "en": "Nigeria",
      "flag": "content/countries/flags/ng.svg"
    },
    "NI": {
      "code": "NI",
      "fa": "نیکاراگوئه",
      "en": "Nicaragua",
      "flag": "content/countries/flags/ni.svg"
    },
    "NL": {
      "code": "NL",
      "fa": "هلند",
      "en": "Netherlands",
      "flag": "content/countries/flags/nl.svg"
    },
    "NO": {
      "code": "NO",
      "fa": "نروژ",
      "en": "Norway",
      "flag": "content/countries/flags/no.svg"
    },
    "NP": {
      "code": "NP",
      "fa": "نپال",
      "en": "Nepal",
      "flag": "content/countries/flags/np.svg"
    },
    "NR": {
      "code": "NR",
      "fa'+N'": "نائورو",
      "en": "Nauru",
      "flag": "content/countries/flags/nr.svg"
    },
    "NU": {
      "code": "NU",
      "fa": "نیوئه",
      "en": "Niue",
      "flag": "content/countries/flags/nu.svg"
    },
    "NZ": {
      "code": "NZ",
      "fa": "نیوزیلند",
      "en": "New Zealand",
      "flag": "content/countries/flags/nz.svg"
    },
    "OM": {
      "code": "OM",
      "fa": "عمان",
      "en": "Oman",
      "flag": "content/countries/flags/om.svg"
    },
    "PA": {
      "code": "PA",
      "fa": "پاناما",
      "en": "Panama",
      "flag": "content/countries/flags/pa.svg"
    },
    "PE": {
      "code": "PE",
      "fa": "پرو",
      "en": "Peru",
      "flag": "content/countries/flags/pe.svg"
    },
    "PF": {
      "code": "PF",
      "fa": "پلی‌نزی فرانسه",
      "en": "French Polynesia",
      "flag": "content/countries/flags/pf.svg"
    },
    "PG": {
      "code": "PG",
      "fa": "پاپوا گینهٔ نو",
      "en": "Papua New Guinea",
      "flag": "content/countries/flags/pg.svg"
    },
    "PH": {
      "code": "PH",
      "fa": "فیلیپین",
      "en": "Philippines",
      "flag": "content/countries/flags/ph.svg"
    },
    "PK": {
      "code": "PK",
      "fa": "پاکستان",
      "en": "Pakistan",
      "flag": "content/countries/flags/pk.svg"
    },
    "PL": {
      "code": "PL",
      "fa": "لهستان",
      "en": "Poland",
      "flag": "content/countries/flags/pl.svg"
    },
    "PM": {
      "code": "PM",
      "fa": "سن پیر و میکلن",
      "en": "St. Pierre & Miquelon",
      "flag": "content/countries/flags/pm.svg"
    },
    "PN": {
      "code": "PN",
      "fa": "جزایر پیت‌کرن",
      "en": "Pitcairn Islands",
      "flag": "content/countries/flags/pn.svg"
    },
    "PR": {
      "code": "PR",
      "fa": "پورتوریکو",
      "en": "Puerto Rico",
      "flag": "content/countries/flags/pr.svg"
    },
    "PS": {
      "code": "PS",
      "fa": "سرزمین‌های فلسطینی",
      "en": "Palestinian Territories",
      "flag": "content/countries/flags/ps.svg"
    },
    "PT": {
      "code": "PT",
      "fa": "پرتغال",
      "en": "Portugal",
      "flag": "content/countries/flags/pt.svg"
    },
    "PW": {
      "code": "PW",
      "fa": "پالائو",
      "en": "Palau",
      "flag": "content/countries/flags/pw.svg"
    },
    "PY": {
      "code": "PY",
      "fa": "پاراگوئه",
      "en": "Paraguay",
      "flag": "content/countries/flags/py.svg"
    },
    "QA": {
      "code": "QA",
      "fa": "قطر",
      "en": "Qatar",
      "flag": "content/countries/flags/qa.svg"
    },
    "RE": {
      "code": "RE",
      "fa": "رئونیون",
      "en": "Réunion",
      "flag": "content/countries/flags/re.svg"
    },
    "RO": {
      "code": "RO",
      "fa": "رومانی",
      "en": "Romania",
      "flag": "content/countries/flags/ro.svg"
    },
    "RS": {
      "code": "RS",
      "fa": "صربستان",
      "en": "Serbia",
      "flag": "content/countries/flags/rs.svg"
    },
    "RU": {
      "code": "RU",
      "fa": "روسیه",
      "en": "Russia",
      "flag": "content/countries/flags/ru.svg"
    },
    "RW": {
      "code": "RW",
      "fa": "رواندا",
      "en": "Rwanda",
      "flag": "content/countries/flags/rw.svg"
    },
    "SA": {
      "code": "SA",
      "fa": "عربستان سعودی",
      "en": "Saudi Arabia",
      "flag": "content/countries/flags/sa.svg"
    },
    "SB": '+N'{
      "code": "SB",
      "fa": "جزایر سلیمان",
      "en": "Solomon Islands",
      "flag": "content/countries/flags/sb.svg"
    },
    "SC": {
      "code": "SC",
      "fa": "سیشل",
      "en": "Seychelles",
      "flag": "content/countries/flags/sc.svg"
    },
    "SD": {
      "code": "SD",
      "fa": "سودان",
      "en": "Sudan",
      "flag": "content/countries/flags/sd.svg"
    },
    "SE": {
      "code": "SE",
      "fa": "سوئد",
      "en": "Sweden",
      "flag": "content/countries/flags/se.svg"
    },
    "SG": {
      "code": "SG",
      "fa": "سنگاپور",
      "en": "Singapore",
      "flag": "content/countries/flags/sg.svg"
    },
    "SH": {
      "code": "SH",
      "fa": "سنت هلن",
      "en": "St. Helena",
      "flag": "content/countries/flags/sh.svg"
    },
    "SI": {
      "code": "SI",
      "fa": "اسلوونی",
      "en": "Slovenia",
      "flag": "content/countries/flags/si.svg"
    },
    "SJ": {
      "code": "SJ",
      "fa": "سوالبارد و یان ماین",
      "en": "Svalbard & Jan Mayen",
      "flag": "content/countries/flags/sj.svg"
    },
    "SK": {
      "code": "SK",
      "fa": "اسلواکی",
      "en": "Slovakia",
      "flag": "content/countries/flags/sk.svg"
    },
    "SL": {
      "code": "SL",
      "fa": "سیرالئون",
      "en": "Sierra Leone",
      "flag": "content/countries/flags/sl.svg"
    },
    "SM": {
      "code": "SM",
      "fa": "سان‌مارینو",
      "en": "San Marino",
      "flag": "content/countries/flags/sm.svg"
    },
    "SN": {
      "code": "SN",
      "fa": "سنگال",
      "en": "Senegal",
      "flag": "content/countries/flags/sn.svg"
    },
    "SO": {
      "code": "SO",
      "fa": "سومالی",
      "en": "Somalia",
      "flag": "content/countries/flags/so.svg"
    },
    "SR": {
      "code": "SR",
      "fa": "سورینام",
      "en": "Suriname",
      "flag": "content/countries/flags/sr.svg"
    },
    "SS": {
      "code": "SS",
      "fa": "سودان جنوبی",
      "en": "South Sudan",
      "flag": "content/countries/flags/ss.svg"
    },
    "ST": {
      "code": "ST",
      "fa": "سائوتومه و پرینسیپ",
      "en": "São Tomé & Príncipe",
      "flag": "content/countries/flags/st.svg"
    },
    "SV": {
      "code": "SV",
      "fa": "السالوادور",
      "en": "El Salvador",
      "flag": "content/countries/flags/sv.svg"
    },
    "SX": {
      "code": "SX",
      "fa": "سنت مارتن",
      "en": "Sint Maarten",
      "flag": "content/countries/flags/sx.svg"
    },
    "SY": {
      "code": "SY",
      "fa": "سوریه",
      "en": "Syria",
      "flag": "content/countries/flags/sy.svg"
    },
    "SZ": {
      "code": "SZ",
      "fa": "اسواتینی",
      "en": "Eswatini",
      "flag": "content/countries/flags/sz.svg"
    },
    "TC": {
      "code": "TC",
      "fa": "جزایر تورکس و کایکوس",
      "en": "Turks & Caicos Islands",
      "flag": "content/countries/flags/tc.svg"
    },
    "TD": {
      "code": "TD",
      "fa": "چاد",
      "en": "Chad",
      "flag": "content/countries/flags/td.svg"
    },
    "TF": {
      "code": "TF",
      "fa": "سرزمین‌های جنوبی فرانسه",
      "en": "French Southern Territories",
      "flag": "content/countries/flags/tf.svg"
    },
    "TG": {
      "code": "TG",
      "fa": "توگو",
      "en": "Togo",
      "flag": "content/countries/flags/tg.svg"
    },
    "TH": {
      "code": "TH",
      "fa": "تایلند",
'+N'      "en": "Thailand",
      "flag": "content/countries/flags/th.svg"
    },
    "TJ": {
      "code": "TJ",
      "fa": "تاجیکستان",
      "en": "Tajikistan",
      "flag": "content/countries/flags/tj.svg"
    },
    "TK": {
      "code": "TK",
      "fa": "توکلائو",
      "en": "Tokelau",
      "flag": "content/countries/flags/tk.svg"
    },
    "TL": {
      "code": "TL",
      "fa": "تیمور-لسته",
      "en": "Timor-Leste",
      "flag": "content/countries/flags/tl.svg"
    },
    "TM": {
      "code": "TM",
      "fa": "ترکمنستان",
      "en": "Turkmenistan",
      "flag": "content/countries/flags/tm.svg"
    },
    "TN": {
      "code": "TN",
      "fa": "تونس",
      "en": "Tunisia",
      "flag": "content/countries/flags/tn.svg"
    },
    "TO": {
      "code": "TO",
      "fa": "تونگا",
      "en": "Tonga",
      "flag": "content/countries/flags/to.svg"
    },
    "TR": {
      "code": "TR",
      "fa": "ترکیه",
      "en": "Türkiye",
      "flag": "content/countries/flags/tr.svg"
    },
    "TT": {
      "code": "TT",
      "fa": "ترینیداد و توباگو",
      "en": "Trinidad & Tobago",
      "flag": "content/countries/flags/tt.svg"
    },
    "TV": {
      "code": "TV",
      "fa": "تووالو",
      "en": "Tuvalu",
      "flag": "content/countries/flags/tv.svg"
    },
    "TW": {
      "code": "TW",
      "fa": "تایوان",
      "en": "Taiwan",
      "flag": "content/countries/flags/tw.svg"
    },
    "TZ": {
      "code": "TZ",
      "fa": "تانزانیا",
      "en": "Tanzania",
      "flag": "content/countries/flags/tz.svg"
    },
    "UA": {
      "code": "UA",
      "fa": "اوکراین",
      "en": "Ukraine",
      "flag": "content/countries/flags/ua.svg"
    },
    "UG": {
      "code": "UG",
      "fa": "اوگاندا",
      "en": "Uganda",
      "flag": "content/countries/flags/ug.svg"
    },
    "UM": {
      "code": "UM",
      "fa": "جزایر دورافتادهٔ ایالات متحده",
      "en": "U.S. Outlying Islands",
      "flag": "content/countries/flags/um.svg"
    },
    "UN": {
      "code": "UN",
      "fa": "سازمان ملل متحد",
      "en": "United Nations",
      "flag": "content/countries/flags/un.svg"
    },
    "US": {
      "code": "US",
      "fa": "ایالات متحده",
      "en": "United States",
      "flag": "content/countries/flags/us.svg"
    },
    "UY": {
      "code": "UY",
      "fa": "اروگوئه",
      "en": "Uruguay",
      "flag": "content/countries/flags/uy.svg"
    },
    "UZ": {
      "code": "UZ",
      "fa": "ازبکستان",
      "en": "Uzbekistan",
      "flag": "content/countries/flags/uz.svg"
    },
    "VA": {
      "code": "VA",
      "fa": "واتیکان",
      "en": "Vatican City",
      "flag": "content/countries/flags/va.svg"
    },
    "VC": {
      "code": "VC",
      "fa": "سنت وینسنت و گرنادین",
      "en": "St. Vincent & Grenadines",
      "flag": "content/countries/flags/vc.svg"
    },
    "VE": {
      "code": "VE",
      "fa": "ونزوئلا",
      "en": "Venezuela",
      "flag": "content/countries/flags/ve.svg"
    },
    "VG": {
      "code": "VG",
      "fa": "جزایر ویرجین بریتانیا",
      "en": "British Virgin Islands",
      "flag": "content/countries/flags/vg.svg"
    },
    "VI": {
      "code": "VI",
      "fa": "جزایر ویرجین ایالات متحده",
      "en": "U.S. Virgin Islands",
      "flag": "content/countries/flags/vi.svg"
    },
    "VN": {
      "code": "VN",
      "fa": "ویتنام",'+N'
      "en": "Vietnam",
      "flag": "content/countries/flags/vn.svg"
    },
    "VU": {
      "code": "VU",
      "fa": "وانواتو",
      "en": "Vanuatu",
      "flag": "content/countries/flags/vu.svg"
    },
    "WF": {
      "code": "WF",
      "fa": "والیس و فوتونا",
      "en": "Wallis & Futuna",
      "flag": "content/countries/flags/wf.svg"
    },
    "WS": {
      "code": "WS",
      "fa": "ساموآ",
      "en": "Samoa",
      "flag": "content/countries/flags/ws.svg"
    },
    "XK": {
      "code": "XK",
      "fa": "کوزوو",
      "en": "Kosovo",
      "flag": "content/countries/flags/xk.svg"
    },
    "YE": {
      "code": "YE",
      "fa": "یمن",
      "en": "Yemen",
      "flag": "content/countries/flags/ye.svg"
    },
    "YT": {
      "code": "YT",
      "fa": "مایوت",
      "en": "Mayotte",
      "flag": "content/countries/flags/yt.svg"
    },
    "ZA": {
      "code": "ZA",
      "fa": "افریقای جنوبی",
      "en": "South Africa",
      "flag": "content/countries/flags/za.svg"
    },
    "ZM": {
      "code": "ZM",
      "fa": "زامبیا",
      "en": "Zambia",
      "flag": "content/countries/flags/zm.svg"
    },
    "ZW": {
      "code": "ZW",
      "fa": "زیمبابوه",
      "en": "Zimbabwe",
      "flag": "content/countries/flags/zw.svg"
    }
  },
  "colors": {
    "black": {
      "slug": "black",
      "en": "Black",
      "fa": "مشکی",
      "hex": "#252525"
    },
    "white": {
      "slug": "white",
      "en": "White",
      "fa": "سفید",
      "hex": "#ffffff"
    },
    "red": {
      "slug": "red",
      "en": "Red",
      "fa": "قرمز",
      "hex": "#d62828"
    },
    "blue": {
      "slug": "blue",
      "en": "Blue",
      "fa": "آبی",
      "hex": "#2563eb"
    },
    "green": {
      "slug": "green",
      "en": "Green",
      "fa": "سبز",
      "hex": "#16834a"
    },
    "yellow": {
      "slug": "yellow",
      "en": "Yellow",
      "fa": "زرد",
      "hex": "#f6c515"
    },
    "orange": {
      "slug": "orange",
      "en": "Orange",
      "fa": "نارنجی",
      "hex": "#f97316"
    },
    "gray": {
      "slug": "gray",
      "en": "Gray",
      "fa": "خاکستری",
      "hex": "#8b929a"
    }
  },
  "categories": {
    "hammer-drills": {
      "id": "hammer-drills",
      "en": "Hammer Drills",
      "slug": "hammer-drills",
      "fa": "دریل چکشی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "دریل و پیچ‌گوشتی",
        "دریل چکشی"
      ],
      "parentId": "drill-drivers"
    },
    "drill-drivers": {
      "id": "drill-drivers",
      "en": "Drill Drivers",
      "slug": "drill-drivers",
      "fa": "دریل و پیچ‌گوشتی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "دریل و پیچ‌گوشتی"
      ],
      "parentId": "power-tools"
    },
    "digital-multimeters": {
      "id": "digital-multimeters",
      "en": "Digital Multimeters",
      "slug": "digital-multimeters",
      "fa": "مولتی‌متر دیجیتال",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "مولتی‌متر",
        "مولتی‌متر دیجیتال"
      ],
      "parentId": "multimeters"
    },
    "clamp-meters": {
      "id": "clamp-meters",
      "en": "Clamp Meters",
      "slug": "clamp-meters",
      "fa": "کلمپ‌متر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "ابزار دقیق",
        "کل'+N'مپ‌متر"
      ],
      "parentId": "precision-instruments"
    },
    "soldering-stations": {
      "id": "soldering-stations",
      "en": "Soldering Stations",
      "slug": "soldering-stations",
      "fa": "ایستگاه هویه",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "هیتر و هویه",
        "ایستگاه هویه"
      ],
      "parentId": "hot-air-stations"
    },
    "soldering-irons": {
      "id": "soldering-irons",
      "en": "Soldering Irons",
      "slug": "soldering-irons",
      "fa": "هویه قلمی",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "هویه",
        "هویه قلمی"
      ],
      "parentId": "soldering-equipment-soldering-irons"
    },
    "hot-air-stations": {
      "id": "hot-air-stations",
      "en": "Hot Air Stations",
      "slug": "hot-air-stations",
      "fa": "هیتر و هویه",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "هیتر و هویه"
      ],
      "parentId": "soldering-and-board-repair"
    },
    "work-lights": {
      "id": "work-lights",
      "en": "Work Lights",
      "slug": "work-lights",
      "fa": "چراغ کارگاهی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "چراغ کارگاهی"
      ],
      "parentId": "lighting-and-lamps"
    },
    "work-floodlights": {
      "id": "work-floodlights",
      "en": "Work Floodlights",
      "slug": "work-floodlights",
      "fa": "پروژکتور کارگاهی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "چراغ کارگاهی",
        "پروژکتور",
        "پروژکتور کارگاهی"
      ],
      "parentId": "floodlights"
    },
    "inspection-lights": {
      "id": "inspection-lights",
      "en": "Inspection Lights",
      "slug": "inspection-lights",
      "fa": "چراغ بازرسی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "چراغ کارگاهی",
        "چراغ بازرسی"
      ],
      "parentId": "work-lights"
    },
    "electrician-hand-tools": {
      "id": "electrician-hand-tools",
      "en": "Electrician Hand Tools",
      "slug": "electrician-hand-tools",
      "fa": "ابزار دستی برق‌کاری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "ابزار دستی برق‌کاری"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "electrician-tool-kits": {
      "id": "electrician-tool-kits",
      "en": "Electrician Tool Kits",
      "slug": "electrician-tool-kits",
      "fa": "مجموعه ابزار برق‌کاری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار",
        "جعبه ابزار",
        "مجموعه ابزار برق‌کاری"
      ],
      "parentId": "toolboxes"
    },
    "electrical-testers": {
      "id": "electrical-testers",
      "en": "Electrical Testers",
      "slug": "electrical-testers",
      "fa": "تستر و فازمتر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی",
        "تستر و فازمتر"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "voltage-testers": {
      "id": "voltage-testers",
      "en": "Voltage Testers",
      "slug": "voltage-testers",
      "fa": "تستر ولتاژ",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست '+N'و عیب‌یابی",
        "تستر ولتاژ"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "phase-testers": {
      "id": "phase-testers",
      "en": "Phase Testers",
      "slug": "phase-testers",
      "fa": "فازمتر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی",
        "فازمتر"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "circuit-testers": {
      "id": "circuit-testers",
      "en": "Circuit Testers",
      "slug": "circuit-testers",
      "fa": "تستر مدار",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی",
        "تستر مدار"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "tools-and-equipment": {
      "id": "tools-and-equipment",
      "en": "Tools and Equipment",
      "slug": "tools-and-equipment",
      "fa": "ابزار و تجهیزات",
      "path": [
        "ابزار و تجهیزات"
      ],
      "parentId": null
    },
    "power-tools": {
      "id": "power-tools",
      "en": "Power Tools",
      "slug": "power-tools",
      "fa": "ابزار برقی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی"
      ],
      "parentId": "tools-and-equipment"
    },
    "measurement-tools": {
      "id": "measurement-tools",
      "en": "Measurement Tools",
      "slug": "measurement-tools",
      "fa": "ابزار اندازه‌گیری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری"
      ],
      "parentId": "tools-and-equipment"
    },
    "soldering-and-board-repair": {
      "id": "soldering-and-board-repair",
      "en": "Soldering Equipment",
      "slug": "soldering-and-board-repair",
      "fa": "تجهیزات لحیم‌کاری",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری"
      ],
      "parentId": "electronic-components"
    },
    "lighting-and-lamps": {
      "id": "lighting-and-lamps",
      "en": "Lighting and Lamps",
      "slug": "lighting-and-lamps",
      "fa": "روشنایی و لامپ",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ"
      ],
      "parentId": "tools-and-equipment"
    },
    "electrician-tools": {
      "id": "electrician-tools",
      "en": "Electrician Tools",
      "slug": "electrician-tools",
      "fa": "ابزار برق‌کار",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "ابزار برق‌کار"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "electrical-test-and-safety": {
      "id": "electrical-test-and-safety",
      "en": "Electrical Test and Safety",
      "slug": "electrical-test-and-safety",
      "fa": "تستر و ایمنی برق",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "حفاظت برق",
        "تستر و ایمنی برق"
      ],
      "parentId": "electrical-protection"
    },
    "wires-and-cables": {
      "id": "wires-and-cables",
      "en": "Wires and Cables",
      "slug": "wires-and-cables",
      "fa": "سیم و کابل",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سیم و کابل"
      ],
      "parentId": "wires-cables-and-connectors"
    },
    "switches-and-sockets": {
      "id": "switches-and-sockets",
      "en": "Switches and Sockets",
      "slug": "switches-and-sockets",
      "fa": "کلید و پریز",
      "path": [
        "ابزار و '+N'تجهیزات",
        "کلید و تجهیزات برقی",
        "کلید و پریز"
      ],
      "parentId": "switches-and-electrical-equipment"
    },
    "cordless-drill": {
      "id": "cordless-drill",
      "en": "Cordless Drill",
      "slug": "cordless-drill",
      "fa": "دریل شارژی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "دریل و پیچ‌گوشتی",
        "دریل شارژی"
      ],
      "parentId": "drill-drivers"
    },
    "electric-screwdriver": {
      "id": "electric-screwdriver",
      "en": "Electric Screwdriver",
      "slug": "electric-screwdriver",
      "fa": "پیچ‌گوشتی برقی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "دریل و پیچ‌گوشتی",
        "پیچ‌گوشتی برقی"
      ],
      "parentId": "drill-drivers"
    },
    "rotary-hammer": {
      "id": "rotary-hammer",
      "en": "Rotary Hammer",
      "slug": "rotary-hammer",
      "fa": "دریل بتن‌کن",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "دریل و پیچ‌گوشتی",
        "دریل بتن‌کن"
      ],
      "parentId": "drill-drivers"
    },
    "cutting-and-grinding": {
      "id": "cutting-and-grinding",
      "en": "Cutting and Grinding",
      "slug": "cutting-and-grinding",
      "fa": "برش و سایش",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "برش و سایش"
      ],
      "parentId": "power-tools"
    },
    "angle-grinders": {
      "id": "angle-grinders",
      "en": "Angle Grinders",
      "slug": "angle-grinders",
      "fa": "فرز و مینی‌فرز",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "برش و سایش",
        "فرز و مینی‌فرز"
      ],
      "parentId": "cutting-and-grinding"
    },
    "jigsaws": {
      "id": "jigsaws",
      "en": "Jigsaws",
      "slug": "jigsaws",
      "fa": "اره عمودبر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "برش و سایش",
        "اره عمودبر"
      ],
      "parentId": "cutting-and-grinding"
    },
    "circular-saws": {
      "id": "circular-saws",
      "en": "Circular Saws",
      "slug": "circular-saws",
      "fa": "اره گردبر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "برش و سایش",
        "اره گردبر"
      ],
      "parentId": "cutting-and-grinding"
    },
    "cutting-disc": {
      "id": "cutting-disc",
      "en": "Cutting Disc",
      "slug": "cutting-disc",
      "fa": "صفحه برش",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "برش و سایش",
        "صفحه برش"
      ],
      "parentId": "cutting-and-grinding"
    },
    "workshop-equipment": {
      "id": "workshop-equipment",
      "en": "Workshop Equipment",
      "slug": "workshop-equipment",
      "fa": "تجهیزات کارگاهی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "تجهیزات کارگاهی"
      ],
      "parentId": "power-tools"
    },
    "welding-machines": {
      "id": "welding-machines",
      "en": "Welding Machines",
      "slug": "welding-machines",
      "fa": "دستگاه جوش",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "تجهیزات کارگاهی",
        "دستگاه جوش"
      ],
      "parentId": "workshop-equipment"
    },
    "pressure-washers": {
      "id": "pressure-washers",
      "en": "Pressure Washers",
      "slug": "pressure-washers",
      "fa": "کارواش",
      "path": [
       '+N' "ابزار و تجهیزات",
        "ابزار برقی",
        "تجهیزات کارگاهی",
        "کارواش"
      ],
      "parentId": "workshop-equipment"
    },
    "air-compressors": {
      "id": "air-compressors",
      "en": "Air Compressors",
      "slug": "air-compressors",
      "fa": "کمپرسور باد",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "تجهیزات کارگاهی",
        "کمپرسور باد"
      ],
      "parentId": "workshop-equipment"
    },
    "tool-accessories": {
      "id": "tool-accessories",
      "en": "Tool Accessories",
      "slug": "tool-accessories",
      "fa": "لوازم جانبی ابزار",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "تجهیزات کارگاهی",
        "لوازم جانبی ابزار"
      ],
      "parentId": "workshop-equipment"
    },
    "drill-bits-and-drilling": {
      "id": "drill-bits-and-drilling",
      "en": "Drill Bits and Drilling",
      "slug": "drill-bits-and-drilling",
      "fa": "مته و سوراخ‌کاری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "مته و سوراخ‌کاری"
      ],
      "parentId": "power-tools"
    },
    "concrete-drill-bits": {
      "id": "concrete-drill-bits",
      "en": "Concrete Drill Bits",
      "slug": "concrete-drill-bits",
      "fa": "مته بتن",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "مته و سوراخ‌کاری",
        "مته بتن"
      ],
      "parentId": "drill-bits-and-drilling"
    },
    "metal-drill-bits": {
      "id": "metal-drill-bits",
      "en": "Metal Drill Bits",
      "slug": "metal-drill-bits",
      "fa": "مته فلز",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "مته و سوراخ‌کاری",
        "مته فلز"
      ],
      "parentId": "drill-bits-and-drilling"
    },
    "wood-drill-bits": {
      "id": "wood-drill-bits",
      "en": "Wood Drill Bits",
      "slug": "wood-drill-bits",
      "fa": "مته چوب",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "مته و سوراخ‌کاری",
        "مته چوب"
      ],
      "parentId": "drill-bits-and-drilling"
    },
    "diamond-drill-bits": {
      "id": "diamond-drill-bits",
      "en": "Diamond Drill Bits",
      "slug": "diamond-drill-bits",
      "fa": "مته الماسه",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "مته و سوراخ‌کاری",
        "مته الماسه"
      ],
      "parentId": "drill-bits-and-drilling"
    },
    "discs-and-blades": {
      "id": "discs-and-blades",
      "en": "Discs and Blades",
      "slug": "discs-and-blades",
      "fa": "صفحه و تیغه",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "صفحه و تیغه"
      ],
      "parentId": "power-tools"
    },
    "discs-and-blades-cutting-disc": {
      "id": "discs-and-blades-cutting-disc",
      "en": "Cutting Disc",
      "slug": "discs-and-blades-cutting-disc",
      "fa": "صفحه برش",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "صفحه و تیغه",
        "صفحه برش"
      ],
      "parentId": "discs-and-blades"
    },
    "grinding-discs": {
      "id": "grinding-discs",
      "en": "Grinding Discs",
      "slug": "grinding-discs",
      "fa": "صفحه ساب",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "صفحه و تیغه",
        "صفحه ساب"
      ],
      "parentId": "discs-and-blades"
    },
    "jigsaw-blades": {
     '+N' "id": "jigsaw-blades",
      "en": "Jigsaw Blades",
      "slug": "jigsaw-blades",
      "fa": "تیغ اره عمودبر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "صفحه و تیغه",
        "تیغ اره عمودبر"
      ],
      "parentId": "discs-and-blades"
    },
    "circular-saw-blades": {
      "id": "circular-saw-blades",
      "en": "Circular Saw Blades",
      "slug": "circular-saw-blades",
      "fa": "تیغ اره گردبر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "صفحه و تیغه",
        "تیغ اره گردبر"
      ],
      "parentId": "discs-and-blades"
    },
    "batteries-and-accessories": {
      "id": "batteries-and-accessories",
      "en": "Batteries and Accessories",
      "slug": "batteries-and-accessories",
      "fa": "باتری و متعلقات",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "باتری و متعلقات"
      ],
      "parentId": "power-tools"
    },
    "replacement-batteries": {
      "id": "replacement-batteries",
      "en": "Replacement Batteries",
      "slug": "replacement-batteries",
      "fa": "باتری یدکی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "باتری و متعلقات",
        "باتری یدکی"
      ],
      "parentId": "batteries-and-accessories"
    },
    "tool-chargers": {
      "id": "tool-chargers",
      "en": "Tool Chargers",
      "slug": "tool-chargers",
      "fa": "شارژر ابزار",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "باتری و متعلقات",
        "شارژر ابزار"
      ],
      "parentId": "batteries-and-accessories"
    },
    "drill-chucks": {
      "id": "drill-chucks",
      "en": "Drill Chucks",
      "slug": "drill-chucks",
      "fa": "سه‌نظام",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "باتری و متعلقات",
        "سه‌نظام"
      ],
      "parentId": "batteries-and-accessories"
    },
    "carbon-brushes": {
      "id": "carbon-brushes",
      "en": "Carbon Brushes",
      "slug": "carbon-brushes",
      "fa": "ذغال دستگاه",
      "path": [
        "ابزار و تجهیزات",
        "ابزار برقی",
        "باتری و متعلقات",
        "ذغال دستگاه"
      ],
      "parentId": "batteries-and-accessories"
    },
    "hand-tools": {
      "id": "hand-tools",
      "en": "Hand Tools",
      "slug": "hand-tools",
      "fa": "ابزار دستی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی"
      ],
      "parentId": "tools-and-equipment"
    },
    "wrenches-and-sockets": {
      "id": "wrenches-and-sockets",
      "en": "Wrenches and Sockets",
      "slug": "wrenches-and-sockets",
      "fa": "آچار و بکس",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "آچار و بکس"
      ],
      "parentId": "hand-tools"
    },
    "adjustable-wrenches": {
      "id": "adjustable-wrenches",
      "en": "Adjustable Wrenches",
      "slug": "adjustable-wrenches",
      "fa": "آچار فرانسه",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "آچار و بکس",
        "آچار فرانسه"
      ],
      "parentId": "wrenches-and-sockets"
    },
    "socket-sets": {
      "id": "socket-sets",
      "en": "Socket Sets",
      "slug": "socket-sets",
      "fa": "ست بکس",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "آچار و بکس",
        "ست بکس"
      ],
      "parentId": "wrenches-a'+N'nd-sockets"
    },
    "hex-keys": {
      "id": "hex-keys",
      "en": "Hex Keys",
      "slug": "hex-keys",
      "fa": "آچار آلن",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "آچار و بکس",
        "آچار آلن"
      ],
      "parentId": "wrenches-and-sockets"
    },
    "combination-wrenches": {
      "id": "combination-wrenches",
      "en": "Combination Wrenches",
      "slug": "combination-wrenches",
      "fa": "آچار تخت و رینگی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "آچار و بکس",
        "آچار تخت و رینگی"
      ],
      "parentId": "wrenches-and-sockets"
    },
    "torque-wrenches": {
      "id": "torque-wrenches",
      "en": "Torque Wrenches",
      "slug": "torque-wrenches",
      "fa": "آچار ترکمتر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "آچار و بکس",
        "آچار ترکمتر"
      ],
      "parentId": "wrenches-and-sockets"
    },
    "pipe-wrenches": {
      "id": "pipe-wrenches",
      "en": "Pipe Wrenches",
      "slug": "pipe-wrenches",
      "fa": "آچار لوله‌گیر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "آچار و بکس",
        "آچار لوله‌گیر"
      ],
      "parentId": "wrenches-and-sockets"
    },
    "screwdrivers-and-pliers": {
      "id": "screwdrivers-and-pliers",
      "en": "Screwdrivers and Pliers",
      "slug": "screwdrivers-and-pliers",
      "fa": "پیچ‌گوشتی و انبر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر"
      ],
      "parentId": "hand-tools"
    },
    "hand-screwdrivers": {
      "id": "hand-screwdrivers",
      "en": "Hand Screwdrivers",
      "slug": "hand-screwdrivers",
      "fa": "پیچ‌گوشتی دستی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "پیچ‌گوشتی دستی"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "combination-pliers": {
      "id": "combination-pliers",
      "en": "Combination Pliers",
      "slug": "combination-pliers",
      "fa": "انبردست",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "انبردست"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "diagonal-cutters": {
      "id": "diagonal-cutters",
      "en": "Diagonal Cutters",
      "slug": "diagonal-cutters",
      "fa": "سیم‌چین",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "سیم‌چین"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "needle-nose-pliers": {
      "id": "needle-nose-pliers",
      "en": "Needle-Nose Pliers",
      "slug": "needle-nose-pliers",
      "fa": "دم‌باریک",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "دم‌باریک"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "screwdriver-sets": {
      "id": "screwdriver-sets",
      "en": "Screwdriver Sets",
      "slug": "screwdriver-sets",
      "fa": "ست پیچ‌گوشتی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "ست پیچ‌گوشتی"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "locking-pliers": {
      "id": "locking-pliers",
      "en": "Locking Pliers",
      "slug": "locking-pliers",
      "fa": "انبرقفلی",
  '+N'    "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "پیچ‌گوشتی و انبر",
        "انبرقفلی"
      ],
      "parentId": "screwdrivers-and-pliers"
    },
    "tool-supplies": {
      "id": "tool-supplies",
      "en": "Tool Supplies",
      "slug": "tool-supplies",
      "fa": "لوازم ابزار",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار"
      ],
      "parentId": "hand-tools"
    },
    "toolboxes": {
      "id": "toolboxes",
      "en": "Toolboxes",
      "slug": "toolboxes",
      "fa": "جعبه ابزار",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار",
        "جعبه ابزار"
      ],
      "parentId": "tool-supplies"
    },
    "bits-and-drivers": {
      "id": "bits-and-drivers",
      "en": "Bits and Drivers",
      "slug": "bits-and-drivers",
      "fa": "مته و سری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار",
        "مته و سری"
      ],
      "parentId": "tool-supplies"
    },
    "clamps-and-hand-vises": {
      "id": "clamps-and-hand-vises",
      "en": "Clamps and Hand Vises",
      "slug": "clamps-and-hand-vises",
      "fa": "گیره و پیچ‌دستی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار",
        "گیره و پیچ‌دستی"
      ],
      "parentId": "tool-supplies"
    },
    "utility-knives-and-blades": {
      "id": "utility-knives-and-blades",
      "en": "Utility Knives and Blades",
      "slug": "utility-knives-and-blades",
      "fa": "کاتر و تیغ",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار",
        "کاتر و تیغ"
      ],
      "parentId": "tool-supplies"
    },
    "hammers-and-striking-tools": {
      "id": "hammers-and-striking-tools",
      "en": "Hammers and Striking Tools",
      "slug": "hammers-and-striking-tools",
      "fa": "چکش و ابزار ضربه‌ای",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار",
        "چکش و ابزار ضربه‌ای"
      ],
      "parentId": "tool-supplies"
    },
    "tape-measures-and-rulers": {
      "id": "tape-measures-and-rulers",
      "en": "Tape Measures and Rulers",
      "slug": "tape-measures-and-rulers",
      "fa": "متر و خط‌کش",
      "path": [
        "ابزار و تجهیزات",
        "ابزار دستی",
        "لوازم ابزار",
        "متر و خط‌کش"
      ],
      "parentId": "tool-supplies"
    },
    "electronic-components": {
      "id": "electronic-components",
      "en": "Electronic Components",
      "slug": "electronic-components",
      "fa": "قطعات الکترونیکی",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی"
      ],
      "parentId": "tools-and-equipment"
    },
    "resistors-and-capacitors": {
      "id": "resistors-and-capacitors",
      "en": "Resistors and Capacitors",
      "slug": "resistors-and-capacitors",
      "fa": "مقاومت و خازن",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "مقاومت و خازن"
      ],
      "parentId": "electronic-components"
    },
    "resistors": {
      "id": "resistors",
      "en": "Resistors",
      "slug": "resistors",
      "fa": "مقاومت",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "مقاومت و خازن",
        "مقاومت"
      ],
      "parentId": "resistors-and-capacitors"
    },
    "capa'+N'citors": {
      "id": "capacitors",
      "en": "Capacitors",
      "slug": "capacitors",
      "fa": "خازن",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "مقاومت و خازن",
        "خازن"
      ],
      "parentId": "resistors-and-capacitors"
    },
    "diodes": {
      "id": "diodes",
      "en": "Diodes",
      "slug": "diodes",
      "fa": "دیود",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "مقاومت و خازن",
        "دیود"
      ],
      "parentId": "resistors-and-capacitors"
    },
    "inductors": {
      "id": "inductors",
      "en": "Inductors",
      "slug": "inductors",
      "fa": "سلف",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "مقاومت و خازن",
        "سلف"
      ],
      "parentId": "resistors-and-capacitors"
    },
    "bridge-rectifiers": {
      "id": "bridge-rectifiers",
      "en": "Bridge Rectifiers",
      "slug": "bridge-rectifiers",
      "fa": "پل دیود",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "مقاومت و خازن",
        "پل دیود"
      ],
      "parentId": "resistors-and-capacitors"
    },
    "ics-and-semiconductors": {
      "id": "ics-and-semiconductors",
      "en": "ICs and Semiconductors",
      "slug": "ics-and-semiconductors",
      "fa": "آی‌سی و نیمه‌رسانا",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "آی‌سی و نیمه‌رسانا"
      ],
      "parentId": "electronic-components"
    },
    "integrated-circuits": {
      "id": "integrated-circuits",
      "en": "Integrated Circuits",
      "slug": "integrated-circuits",
      "fa": "آی‌سی",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "آی‌سی و نیمه‌رسانا",
        "آی‌سی"
      ],
      "parentId": "ics-and-semiconductors"
    },
    "transistors": {
      "id": "transistors",
      "en": "Transistors",
      "slug": "transistors",
      "fa": "ترانزیستور",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "آی‌سی و نیمه‌رسانا",
        "ترانزیستور"
      ],
      "parentId": "ics-and-semiconductors"
    },
    "voltage-regulators": {
      "id": "voltage-regulators",
      "en": "Voltage Regulators",
      "slug": "voltage-regulators",
      "fa": "رگولاتور",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "آی‌سی و نیمه‌رسانا",
        "رگولاتور"
      ],
      "parentId": "ics-and-semiconductors"
    },
    "mosfets": {
      "id": "mosfets",
      "en": "MOSFETs",
      "slug": "mosfets",
      "fa": "ماسفت",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "آی‌سی و نیمه‌رسانا",
        "ماسفت"
      ],
      "parentId": "ics-and-semiconductors"
    },
    "optocouplers": {
      "id": "optocouplers",
      "en": "Optocouplers",
      "slug": "optocouplers",
      "fa": "اپتوکوپلر",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "آی‌سی و نیمه‌رسانا",
        "اپتوکوپلر"
      ],
      "parentId": "ics-and-semiconductors"
    },
    "boards-and-modules": {
      "id": "boards-and-modules",
      "en": "Boards and Modules",
      "slug": "boards-and-modules",
      "fa": "برد و ماژول",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "برد و ماژول"
      ],
      "paren'+N'tId": "electronic-components"
    },
    "breadboards": {
      "id": "breadboards",
      "en": "Breadboards",
      "slug": "breadboards",
      "fa": "بردبورد",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "برد و ماژول",
        "بردبورد"
      ],
      "parentId": "boards-and-modules"
    },
    "ready-made-modules": {
      "id": "ready-made-modules",
      "en": "Ready-Made Modules",
      "slug": "ready-made-modules",
      "fa": "ماژول آماده",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "برد و ماژول",
        "ماژول آماده"
      ],
      "parentId": "boards-and-modules"
    },
    "sensors": {
      "id": "sensors",
      "en": "Sensors",
      "slug": "sensors",
      "fa": "سنسور",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "برد و ماژول",
        "سنسور"
      ],
      "parentId": "boards-and-modules"
    },
    "arduino-boards": {
      "id": "arduino-boards",
      "en": "Arduino Boards",
      "slug": "arduino-boards",
      "fa": "برد آردوینو",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "برد و ماژول",
        "برد آردوینو"
      ],
      "parentId": "boards-and-modules"
    },
    "power-modules": {
      "id": "power-modules",
      "en": "Power Modules",
      "slug": "power-modules",
      "fa": "ماژول تغذیه",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "برد و ماژول",
        "ماژول تغذیه"
      ],
      "parentId": "boards-and-modules"
    },
    "sensors-and-modules": {
      "id": "sensors-and-modules",
      "en": "Sensors and Modules",
      "slug": "sensors-and-modules",
      "fa": "سنسور و ماژول",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "سنسور و ماژول"
      ],
      "parentId": "electronic-components"
    },
    "temperature-sensors": {
      "id": "temperature-sensors",
      "en": "Temperature Sensors",
      "slug": "temperature-sensors",
      "fa": "سنسور دما",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "سنسور و ماژول",
        "سنسور دما"
      ],
      "parentId": "sensors-and-modules"
    },
    "humidity-sensors": {
      "id": "humidity-sensors",
      "en": "Humidity Sensors",
      "slug": "humidity-sensors",
      "fa": "سنسور رطوبت",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "سنسور و ماژول",
        "سنسور رطوبت"
      ],
      "parentId": "sensors-and-modules"
    },
    "distance-sensors": {
      "id": "distance-sensors",
      "en": "Distance Sensors",
      "slug": "distance-sensors",
      "fa": "سنسور فاصله",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "سنسور و ماژول",
        "سنسور فاصله"
      ],
      "parentId": "sensors-and-modules"
    },
    "relay-modules": {
      "id": "relay-modules",
      "en": "Relay Modules",
      "slug": "relay-modules",
      "fa": "ماژول رله",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "سنسور و ماژول",
        "ماژول رله"
      ],
      "parentId": "sensors-and-modules"
    },
    "communication-modules": {
      "id": "communication-modules",
      "en": "Communication Modules",
      "slug": "communication-modules",
      "fa": "ماژول ارتباطی",
      "path": [
     '+N'   "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "سنسور و ماژول",
        "ماژول ارتباطی"
      ],
      "parentId": "sensors-and-modules"
    },
    "soldering-equipment-soldering-irons": {
      "id": "soldering-equipment-soldering-irons",
      "en": "Soldering Irons",
      "slug": "soldering-equipment-soldering-irons",
      "fa": "هویه",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "هویه"
      ],
      "parentId": "soldering-and-board-repair"
    },
    "soldering-tips": {
      "id": "soldering-tips",
      "en": "Soldering Tips",
      "slug": "soldering-tips",
      "fa": "نوک هویه",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "نوک هویه"
      ],
      "parentId": "soldering-and-board-repair"
    },
    "solder-wire": {
      "id": "solder-wire",
      "en": "Solder Wire",
      "slug": "solder-wire",
      "fa": "سیم لحیم",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "سیم لحیم"
      ],
      "parentId": "soldering-and-board-repair"
    },
    "desoldering-pumps": {
      "id": "desoldering-pumps",
      "en": "Desoldering Pumps",
      "slug": "desoldering-pumps",
      "fa": "قلع‌کش",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "قلع‌کش"
      ],
      "parentId": "soldering-and-board-repair"
    },
    "development-boards": {
      "id": "development-boards",
      "en": "Development Boards",
      "slug": "development-boards",
      "fa": "بردهای توسعه",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "بردهای توسعه"
      ],
      "parentId": "electronic-components"
    },
    "development-boards-arduino-boards": {
      "id": "development-boards-arduino-boards",
      "en": "Arduino Boards",
      "slug": "development-boards-arduino-boards",
      "fa": "برد آردوینو",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "بردهای توسعه",
        "برد آردوینو"
      ],
      "parentId": "development-boards"
    },
    "esp32-boards": {
      "id": "esp32-boards",
      "en": "ESP32 Boards",
      "slug": "esp32-boards",
      "fa": "برد ESP32",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "بردهای توسعه",
        "برد ESP32"
      ],
      "parentId": "development-boards"
    },
    "raspberry-pi-boards": {
      "id": "raspberry-pi-boards",
      "en": "Raspberry Pi Boards",
      "slug": "raspberry-pi-boards",
      "fa": "برد رزبری پای",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "بردهای توسعه",
        "برد رزبری پای"
      ],
      "parentId": "development-boards"
    },
    "perfboards": {
      "id": "perfboards",
      "en": "Perfboards",
      "slug": "perfboards",
      "fa": "برد سوراخ‌دار",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "بردهای توسعه",
        "برد سوراخ‌دار"
      ],
      "parentId": "development-boards"
    },
    "jumper-wires": {
      "id": "jumper-wires",
      "en": "Jumper Wires",
      "slug": "jumper-wires",
      "fa": "جامپر و سیم رابط",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "بردهای توسعه",
        "'+N'جامپر و سیم رابط"
      ],
      "parentId": "development-boards"
    },
    "wires-cables-and-connectors": {
      "id": "wires-cables-and-connectors",
      "en": "Wires, Cables and Connectors",
      "slug": "wires-cables-and-connectors",
      "fa": "سیم، کابل و اتصالات",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات"
      ],
      "parentId": "tools-and-equipment"
    },
    "stranded-wire": {
      "id": "stranded-wire",
      "en": "Stranded Wire",
      "slug": "stranded-wire",
      "fa": "سیم افشان",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سیم و کابل",
        "سیم افشان"
      ],
      "parentId": "wires-and-cables"
    },
    "power-cables": {
      "id": "power-cables",
      "en": "Power Cables",
      "slug": "power-cables",
      "fa": "کابل برق",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سیم و کابل",
        "کابل برق"
      ],
      "parentId": "wires-and-cables"
    },
    "ethernet-cables": {
      "id": "ethernet-cables",
      "en": "Ethernet Cables",
      "slug": "ethernet-cables",
      "fa": "کابل شبکه",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سیم و کابل",
        "کابل شبکه"
      ],
      "parentId": "wires-and-cables"
    },
    "shielded-cables": {
      "id": "shielded-cables",
      "en": "Shielded Cables",
      "slug": "shielded-cables",
      "fa": "کابل شیلددار",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سیم و کابل",
        "کابل شیلددار"
      ],
      "parentId": "wires-and-cables"
    },
    "solid-wires": {
      "id": "solid-wires",
      "en": "Solid Wires",
      "slug": "solid-wires",
      "fa": "سیم مفتولی",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سیم و کابل",
        "سیم مفتولی"
      ],
      "parentId": "wires-and-cables"
    },
    "instrumentation-cables": {
      "id": "instrumentation-cables",
      "en": "Instrumentation Cables",
      "slug": "instrumentation-cables",
      "fa": "کابل ابزار دقیق",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سیم و کابل",
        "کابل ابزار دقیق"
      ],
      "parentId": "wires-and-cables"
    },
    "sockets-and-connectors": {
      "id": "sockets-and-connectors",
      "en": "Sockets and Connectors",
      "slug": "sockets-and-connectors",
      "fa": "سوکت و کانکتور",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سوکت و کانکتور"
      ],
      "parentId": "wires-cables-and-connectors"
    },
    "power-plugs": {
      "id": "power-plugs",
      "en": "Power Plugs",
      "slug": "power-plugs",
      "fa": "فیش برق",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سوکت و کانکتور",
        "فیش برق"
      ],
      "parentId": "sockets-and-connectors"
    },
    "network-jacks": {
      "id": "network-jacks",
      "en": "Network Jacks",
      "slug": "network-jacks",
      "fa": "سوکت شبکه",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سوکت و کانکتور",
        "سوکت شبکه"
      ],
      "parentId": "sockets-and-connectors"
    },
    "connectors": {
      "id": "connectors",
      "en": "Connectors",
      "slug": "connectors",
    '+N'  "fa": "کانکتور",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سوکت و کانکتور",
        "کانکتور"
      ],
      "parentId": "sockets-and-connectors"
    },
    "bnc-connectors": {
      "id": "bnc-connectors",
      "en": "BNC Connectors",
      "slug": "bnc-connectors",
      "fa": "فیش BNC",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سوکت و کانکتور",
        "فیش BNC"
      ],
      "parentId": "sockets-and-connectors"
    },
    "telecom-connectors": {
      "id": "telecom-connectors",
      "en": "Telecom Connectors",
      "slug": "telecom-connectors",
      "fa": "کانکتور مخابراتی",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سوکت و کانکتور",
        "کانکتور مخابراتی"
      ],
      "parentId": "sockets-and-connectors"
    },
    "banana-plugs": {
      "id": "banana-plugs",
      "en": "Banana Plugs",
      "slug": "banana-plugs",
      "fa": "فیش موزی",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "سوکت و کانکتور",
        "فیش موزی"
      ],
      "parentId": "sockets-and-connectors"
    },
    "installation-connectors": {
      "id": "installation-connectors",
      "en": "Installation Connectors",
      "slug": "installation-connectors",
      "fa": "اتصالات نصب",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "اتصالات نصب"
      ],
      "parentId": "wires-cables-and-connectors"
    },
    "terminal-blocks": {
      "id": "terminal-blocks",
      "en": "Terminal Blocks",
      "slug": "terminal-blocks",
      "fa": "ترمینال",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "اتصالات نصب",
        "ترمینال"
      ],
      "parentId": "installation-connectors"
    },
    "wire-ferrules": {
      "id": "wire-ferrules",
      "en": "Wire Ferrules",
      "slug": "wire-ferrules",
      "fa": "سرسیم",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "اتصالات نصب",
        "سرسیم"
      ],
      "parentId": "installation-connectors"
    },
    "cable-ducts-and-clips": {
      "id": "cable-ducts-and-clips",
      "en": "Cable Ducts and Clips",
      "slug": "cable-ducts-and-clips",
      "fa": "داکت و بست",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "اتصالات نصب",
        "داکت و بست"
      ],
      "parentId": "installation-connectors"
    },
    "heat-shrink-tubing": {
      "id": "heat-shrink-tubing",
      "en": "Heat-Shrink Tubing",
      "slug": "heat-shrink-tubing",
      "fa": "وارنیش حرارتی",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "اتصالات نصب",
        "وارنیش حرارتی"
      ],
      "parentId": "installation-connectors"
    },
    "cable-glands": {
      "id": "cable-glands",
      "en": "Cable Glands",
      "slug": "cable-glands",
      "fa": "گلند کابل",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "اتصالات نصب",
        "گلند کابل"
      ],
      "parentId": "installation-connectors"
    },
    "cable-ties": {
      "id": "cable-ties",
      "en": "Cable Ties",
      "slug": "cable-ties",
      "fa": "بست کمربندی",
      "path": [
        "ابزار و تجهیزات",
        "سیم، کابل و اتصالات",
        "اتصالات نصب",
       '+N' "بست کمربندی"
      ],
      "parentId": "installation-connectors"
    },
    "lamps-and-leds": {
      "id": "lamps-and-leds",
      "en": "Lamps and LEDs",
      "slug": "lamps-and-leds",
      "fa": "لامپ و LED",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لامپ و LED"
      ],
      "parentId": "lighting-and-lamps"
    },
    "led-bulbs": {
      "id": "led-bulbs",
      "en": "LED Bulbs",
      "slug": "led-bulbs",
      "fa": "لامپ LED",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لامپ و LED",
        "لامپ LED"
      ],
      "parentId": "lamps-and-leds"
    },
    "ceiling-panels": {
      "id": "ceiling-panels",
      "en": "Ceiling Panels",
      "slug": "ceiling-panels",
      "fa": "پنل سقفی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لامپ و LED",
        "پنل سقفی"
      ],
      "parentId": "lamps-and-leds"
    },
    "led-strip-lights": {
      "id": "led-strip-lights",
      "en": "LED Strip Lights",
      "slug": "led-strip-lights",
      "fa": "ریسه نواری",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لامپ و LED",
        "ریسه نواری"
      ],
      "parentId": "lamps-and-leds"
    },
    "filament-bulbs": {
      "id": "filament-bulbs",
      "en": "Filament Bulbs",
      "slug": "filament-bulbs",
      "fa": "لامپ فیلامنتی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لامپ و LED",
        "لامپ فیلامنتی"
      ],
      "parentId": "lamps-and-leds"
    },
    "linear-lights": {
      "id": "linear-lights",
      "en": "Linear Lights",
      "slug": "linear-lights",
      "fa": "چراغ خطی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لامپ و LED",
        "چراغ خطی"
      ],
      "parentId": "lamps-and-leds"
    },
    "emergency-lights": {
      "id": "emergency-lights",
      "en": "Emergency Lights",
      "slug": "emergency-lights",
      "fa": "چراغ اضطراری",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "چراغ کارگاهی",
        "چراغ اضطراری"
      ],
      "parentId": "work-lights"
    },
    "floodlights": {
      "id": "floodlights",
      "en": "Floodlights",
      "slug": "floodlights",
      "fa": "پروژکتور",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "چراغ کارگاهی",
        "پروژکتور"
      ],
      "parentId": "work-lights"
    },
    "portable-lights": {
      "id": "portable-lights",
      "en": "Portable Lights",
      "slug": "portable-lights",
      "fa": "چراغ سیار",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "چراغ کارگاهی",
        "چراغ سیار"
      ],
      "parentId": "work-lights"
    },
    "explosion-proof-lights": {
      "id": "explosion-proof-lights",
      "en": "Explosion-Proof Lights",
      "slug": "explosion-proof-lights",
      "fa": "چراغ ضدانفجار",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "چراغ کارگاهی",
        "چراغ ضدانفجار"
      ],
      "parentId": "work-lights"
    },
    "lighting-accessories": {
      "id": "lighting-accessories",
      "en": "Lighting Accessories",
      "slug": "lighting-accessories",
      "fa": "لوازم روشنایی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
    '+N'    "لوازم روشنایی"
      ],
      "parentId": "lighting-and-lamps"
    },
    "lamp-holders": {
      "id": "lamp-holders",
      "en": "Lamp Holders",
      "slug": "lamp-holders",
      "fa": "سرپیچ",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لوازم روشنایی",
        "سرپیچ"
      ],
      "parentId": "lighting-accessories"
    },
    "led-drivers": {
      "id": "led-drivers",
      "en": "LED Drivers",
      "slug": "led-drivers",
      "fa": "درایور LED",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لوازم روشنایی",
        "درایور LED"
      ],
      "parentId": "lighting-accessories"
    },
    "motion-sensors": {
      "id": "motion-sensors",
      "en": "Motion Sensors",
      "slug": "motion-sensors",
      "fa": "سنسور حرکتی",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لوازم روشنایی",
        "سنسور حرکتی"
      ],
      "parentId": "lighting-accessories"
    },
    "photocells": {
      "id": "photocells",
      "en": "Photocells",
      "slug": "photocells",
      "fa": "فتوسل",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لوازم روشنایی",
        "فتوسل"
      ],
      "parentId": "lighting-accessories"
    },
    "light-fixtures-and-accessories": {
      "id": "light-fixtures-and-accessories",
      "en": "Light Fixtures and Accessories",
      "slug": "light-fixtures-and-accessories",
      "fa": "قاب و متعلقات چراغ",
      "path": [
        "ابزار و تجهیزات",
        "روشنایی و لامپ",
        "لوازم روشنایی",
        "قاب و متعلقات چراغ"
      ],
      "parentId": "lighting-accessories"
    },
    "switches-and-electrical-equipment": {
      "id": "switches-and-electrical-equipment",
      "en": "Switches and Electrical Equipment",
      "slug": "switches-and-electrical-equipment",
      "fa": "کلید و تجهیزات برقی",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی"
      ],
      "parentId": "tools-and-equipment"
    },
    "electrical-switches": {
      "id": "electrical-switches",
      "en": "Electrical Switches",
      "slug": "electrical-switches",
      "fa": "کلید برق",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "کلید و پریز",
        "کلید برق"
      ],
      "parentId": "switches-and-sockets"
    },
    "flush-mount-outlets": {
      "id": "flush-mount-outlets",
      "en": "Flush-Mount Outlets",
      "slug": "flush-mount-outlets",
      "fa": "پریز توکار",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "کلید و پریز",
        "پریز توکار"
      ],
      "parentId": "switches-and-sockets"
    },
    "industrial-outlets": {
      "id": "industrial-outlets",
      "en": "Industrial Outlets",
      "slug": "industrial-outlets",
      "fa": "پریز صنعتی",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "کلید و پریز",
        "پریز صنعتی"
      ],
      "parentId": "switches-and-sockets"
    },
    "two-way-switches": {
      "id": "two-way-switches",
      "en": "Two-Way Switches",
      "slug": "two-way-switches",
      "fa": "کلید تبدیل",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "کلید و پریز",
        "کلید تبدیل"
      ],
      "parentId": "switches-and-sockets"
    },
    "grounded'+N'-outlets": {
      "id": "grounded-outlets",
      "en": "Grounded Outlets",
      "slug": "grounded-outlets",
      "fa": "پریز ارت‌دار",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "کلید و پریز",
        "پریز ارت‌دار"
      ],
      "parentId": "switches-and-sockets"
    },
    "fuses-and-surge-protection": {
      "id": "fuses-and-surge-protection",
      "en": "Fuses and Surge Protection",
      "slug": "fuses-and-surge-protection",
      "fa": "فیوز و محافظ",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "فیوز و محافظ"
      ],
      "parentId": "switches-and-electrical-equipment"
    },
    "miniature-circuit-breakers": {
      "id": "miniature-circuit-breakers",
      "en": "Miniature Circuit Breakers",
      "slug": "miniature-circuit-breakers",
      "fa": "فیوز مینیاتوری",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "فیوز و محافظ",
        "فیوز مینیاتوری"
      ],
      "parentId": "fuses-and-surge-protection"
    },
    "residual-current-devices": {
      "id": "residual-current-devices",
      "en": "Residual-Current Devices",
      "slug": "residual-current-devices",
      "fa": "محافظ جان",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "فیوز و محافظ",
        "محافظ جان"
      ],
      "parentId": "fuses-and-surge-protection"
    },
    "voltage-protectors": {
      "id": "voltage-protectors",
      "en": "Voltage Protectors",
      "slug": "voltage-protectors",
      "fa": "محافظ ولتاژ",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "فیوز و محافظ",
        "محافظ ولتاژ"
      ],
      "parentId": "fuses-and-surge-protection"
    },
    "cartridge-fuses": {
      "id": "cartridge-fuses",
      "en": "Cartridge Fuses",
      "slug": "cartridge-fuses",
      "fa": "فیوز کاردی",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "فیوز و محافظ",
        "فیوز کاردی"
      ],
      "parentId": "fuses-and-surge-protection"
    },
    "circuit-breakers": {
      "id": "circuit-breakers",
      "en": "Circuit Breakers",
      "slug": "circuit-breakers",
      "fa": "کلید اتوماتیک",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "فیوز و محافظ",
        "کلید اتوماتیک"
      ],
      "parentId": "fuses-and-surge-protection"
    },
    "control-equipment": {
      "id": "control-equipment",
      "en": "Control Equipment",
      "slug": "control-equipment",
      "fa": "تجهیزات کنترل",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "تجهیزات کنترل"
      ],
      "parentId": "switches-and-electrical-equipment"
    },
    "contactors": {
      "id": "contactors",
      "en": "Contactors",
      "slug": "contactors",
      "fa": "کنتاکتور",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "تجهیزات کنترل",
        "کنتاکتور"
      ],
      "parentId": "control-equipment"
    },
    "timers": {
      "id": "timers",
      "en": "Timers",
      "slug": "timers",
      "fa": "تایمر",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "تجهیزات کنترل",
        "تایمر"
      ],
      "parentId": "control-equipment"
    },
    "pushbuttons-and-indicators": {
      "id": "pu'+N'shbuttons-and-indicators",
      "en": "Pushbuttons and Indicators",
      "slug": "pushbuttons-and-indicators",
      "fa": "شستی و سیگنال",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "تجهیزات کنترل",
        "شستی و سیگنال"
      ],
      "parentId": "control-equipment"
    },
    "thermal-relays": {
      "id": "thermal-relays",
      "en": "Thermal Relays",
      "slug": "thermal-relays",
      "fa": "رله حرارتی",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "تجهیزات کنترل",
        "رله حرارتی"
      ],
      "parentId": "control-equipment"
    },
    "phase-monitors": {
      "id": "phase-monitors",
      "en": "Phase Monitors",
      "slug": "phase-monitors",
      "fa": "کنترل فاز",
      "path": [
        "ابزار و تجهیزات",
        "کلید و تجهیزات برقی",
        "تجهیزات کنترل",
        "کنترل فاز"
      ],
      "parentId": "control-equipment"
    },
    "safety-and-protection": {
      "id": "safety-and-protection",
      "en": "Safety and Protection",
      "slug": "safety-and-protection",
      "fa": "ایمنی و حفاظت",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت"
      ],
      "parentId": "tools-and-equipment"
    },
    "personal-protective-equipment": {
      "id": "personal-protective-equipment",
      "en": "Personal Protective Equipment",
      "slug": "personal-protective-equipment",
      "fa": "ایمنی فردی",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی فردی"
      ],
      "parentId": "safety-and-protection"
    },
    "insulated-gloves": {
      "id": "insulated-gloves",
      "en": "Insulated Gloves",
      "slug": "insulated-gloves",
      "fa": "دستکش عایق",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی فردی",
        "دستکش عایق"
      ],
      "parentId": "personal-protective-equipment"
    },
    "safety-glasses": {
      "id": "safety-glasses",
      "en": "Safety Glasses",
      "slug": "safety-glasses",
      "fa": "عینک ایمنی",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی فردی",
        "عینک ایمنی"
      ],
      "parentId": "personal-protective-equipment"
    },
    "safety-boots": {
      "id": "safety-boots",
      "en": "Safety Boots",
      "slug": "safety-boots",
      "fa": "کفش کار",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی فردی",
        "کفش کار"
      ],
      "parentId": "personal-protective-equipment"
    },
    "hard-hats": {
      "id": "hard-hats",
      "en": "Hard Hats",
      "slug": "hard-hats",
      "fa": "کلاه ایمنی",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی فردی",
        "کلاه ایمنی"
      ],
      "parentId": "personal-protective-equipment"
    },
    "workwear": {
      "id": "workwear",
      "en": "Workwear",
      "slug": "workwear",
      "fa": "لباس کار",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی فردی",
        "لباس کار"
      ],
      "parentId": "personal-protective-equipment"
    },
    "electrical-protection": {
      "id": "electrical-protection",
      "en": "Electrical Protection",
      "slug": "electrical-protection",
      "fa": "حفاظت برق",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفا'+N'ظت",
        "حفاظت برق"
      ],
      "parentId": "safety-and-protection"
    },
    "electrical-protection-voltage-protectors": {
      "id": "electrical-protection-voltage-protectors",
      "en": "Voltage Protectors",
      "slug": "electrical-protection-voltage-protectors",
      "fa": "محافظ ولتاژ",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "حفاظت برق",
        "محافظ ولتاژ"
      ],
      "parentId": "electrical-protection"
    },
    "grounding-equipment": {
      "id": "grounding-equipment",
      "en": "Grounding Equipment",
      "slug": "grounding-equipment",
      "fa": "تجهیزات ارت",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "حفاظت برق",
        "تجهیزات ارت"
      ],
      "parentId": "electrical-protection"
    },
    "lightning-arresters": {
      "id": "lightning-arresters",
      "en": "Lightning Arresters",
      "slug": "lightning-arresters",
      "fa": "صاعقه‌گیر",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "حفاظت برق",
        "صاعقه‌گیر"
      ],
      "parentId": "electrical-protection"
    },
    "insulation-testers": {
      "id": "insulation-testers",
      "en": "Insulation Testers",
      "slug": "insulation-testers",
      "fa": "دستگاه تست عایق",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "حفاظت برق",
        "دستگاه تست عایق"
      ],
      "parentId": "electrical-protection"
    },
    "lockout-and-tagout-equipment": {
      "id": "lockout-and-tagout-equipment",
      "en": "Lockout and Tagout Equipment",
      "slug": "lockout-and-tagout-equipment",
      "fa": "تجهیزات قفل و برچسب",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "حفاظت برق",
        "تجهیزات قفل و برچسب"
      ],
      "parentId": "electrical-protection"
    },
    "workshop-safety": {
      "id": "workshop-safety",
      "en": "Workshop Safety",
      "slug": "workshop-safety",
      "fa": "ایمنی کارگاه",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی کارگاه"
      ],
      "parentId": "safety-and-protection"
    },
    "workshop-safety-hard-hats": {
      "id": "workshop-safety-hard-hats",
      "en": "Hard Hats",
      "slug": "workshop-safety-hard-hats",
      "fa": "کلاه ایمنی",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی کارگاه",
        "کلاه ایمنی"
      ],
      "parentId": "workshop-safety"
    },
    "workshop-safety-workwear": {
      "id": "workshop-safety-workwear",
      "en": "Workwear",
      "slug": "workshop-safety-workwear",
      "fa": "لباس کار",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی کارگاه",
        "لباس کار"
      ],
      "parentId": "workshop-safety"
    },
    "first-aid-kits": {
      "id": "first-aid-kits",
      "en": "First Aid Kits",
      "slug": "first-aid-kits",
      "fa": "جعبه کمک‌های اولیه",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی کارگاه",
        "جعبه کمک‌های اولیه"
      ],
      "parentId": "workshop-safety"
    },
    "hearing-protection": {
      "id": "hearing-protection",
      "en": "Hearing Protection",
      "slug": "hearing-protection",
      "fa": "گوشی ایمنی",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
      '+N'  "ایمنی کارگاه",
        "گوشی ایمنی"
      ],
      "parentId": "workshop-safety"
    },
    "respirator-masks": {
      "id": "respirator-masks",
      "en": "Respirator Masks",
      "slug": "respirator-masks",
      "fa": "ماسک تنفسی",
      "path": [
        "ابزار و تجهیزات",
        "ایمنی و حفاظت",
        "ایمنی کارگاه",
        "ماسک تنفسی"
      ],
      "parentId": "workshop-safety"
    },
    "multimeters": {
      "id": "multimeters",
      "en": "Multimeters",
      "slug": "multimeters",
      "fa": "مولتی‌متر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "مولتی‌متر"
      ],
      "parentId": "measurement-tools"
    },
    "ammeters": {
      "id": "ammeters",
      "en": "Ammeters",
      "slug": "ammeters",
      "fa": "آمپرمتر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "آمپرمتر"
      ],
      "parentId": "measurement-tools"
    },
    "voltmeters": {
      "id": "voltmeters",
      "en": "Voltmeters",
      "slug": "voltmeters",
      "fa": "ولت‌متر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "ولت‌متر"
      ],
      "parentId": "measurement-tools"
    },
    "frequency-meters": {
      "id": "frequency-meters",
      "en": "Frequency Meters",
      "slug": "frequency-meters",
      "fa": "فرکانس‌متر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "فرکانس‌متر"
      ],
      "parentId": "measurement-tools"
    },
    "lcr-meters": {
      "id": "lcr-meters",
      "en": "LCR Meters",
      "slug": "lcr-meters",
      "fa": "LCR متر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "LCR متر"
      ],
      "parentId": "measurement-tools"
    },
    "testing-and-troubleshooting": {
      "id": "testing-and-troubleshooting",
      "en": "Testing and Troubleshooting",
      "slug": "testing-and-troubleshooting",
      "fa": "تست و عیب‌یابی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی"
      ],
      "parentId": "measurement-tools"
    },
    "cable-testers": {
      "id": "cable-testers",
      "en": "Cable Testers",
      "slug": "cable-testers",
      "fa": "تستر کابل",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی",
        "تستر کابل"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "battery-testers": {
      "id": "battery-testers",
      "en": "Battery Testers",
      "slug": "battery-testers",
      "fa": "تستر باتری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی",
        "تستر باتری"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "testing-and-troubleshooting-insulation-testers": {
      "id": "testing-and-troubleshooting-insulation-testers",
      "en": "Insulation Testers",
      "slug": "testing-and-troubleshooting-insulation-testers",
      "fa": "تستر عایق",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی",
        "تستر عایق"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "wire-tracers": {
      "id": "wire-tracers",
      "en": "Wire Tracers",
      "slug": "wire-tracers",
      "fa": "ردیاب سیم",
      "path": [
        "ابزار و تجهیزات"'+N',
        "ابزار اندازه‌گیری",
        "تست و عیب‌یابی",
        "ردیاب سیم"
      ],
      "parentId": "testing-and-troubleshooting"
    },
    "precision-instruments": {
      "id": "precision-instruments",
      "en": "Precision Instruments",
      "slug": "precision-instruments",
      "fa": "ابزار دقیق",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "ابزار دقیق"
      ],
      "parentId": "measurement-tools"
    },
    "oscilloscopes": {
      "id": "oscilloscopes",
      "en": "Oscilloscopes",
      "slug": "oscilloscopes",
      "fa": "اسیلوسکوپ",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "ابزار دقیق",
        "اسیلوسکوپ"
      ],
      "parentId": "precision-instruments"
    },
    "power-supplies": {
      "id": "power-supplies",
      "en": "Power Supplies",
      "slug": "power-supplies",
      "fa": "منبع تغذیه",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "ابزار دقیق",
        "منبع تغذیه"
      ],
      "parentId": "precision-instruments"
    },
    "infrared-thermometers": {
      "id": "infrared-thermometers",
      "en": "Infrared Thermometers",
      "slug": "infrared-thermometers",
      "fa": "دماسنج لیزری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "ابزار دقیق",
        "دماسنج لیزری"
      ],
      "parentId": "precision-instruments"
    },
    "lux-meters": {
      "id": "lux-meters",
      "en": "Lux Meters",
      "slug": "lux-meters",
      "fa": "لوکس‌متر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "ابزار دقیق",
        "لوکس‌متر"
      ],
      "parentId": "precision-instruments"
    },
    "temperature-and-humidity": {
      "id": "temperature-and-humidity",
      "en": "Temperature and Humidity",
      "slug": "temperature-and-humidity",
      "fa": "دما و رطوبت",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "دما و رطوبت"
      ],
      "parentId": "measurement-tools"
    },
    "digital-thermometers": {
      "id": "digital-thermometers",
      "en": "Digital Thermometers",
      "slug": "digital-thermometers",
      "fa": "دماسنج دیجیتال",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "دما و رطوبت",
        "دماسنج دیجیتال"
      ],
      "parentId": "temperature-and-humidity"
    },
    "temperature-and-humidity-infrared-thermometers": {
      "id": "temperature-and-humidity-infrared-thermometers",
      "en": "Infrared Thermometers",
      "slug": "temperature-and-humidity-infrared-thermometers",
      "fa": "دماسنج لیزری",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "دما و رطوبت",
        "دماسنج لیزری"
      ],
      "parentId": "temperature-and-humidity"
    },
    "hygrometers": {
      "id": "hygrometers",
      "en": "Hygrometers",
      "slug": "hygrometers",
      "fa": "رطوبت‌سنج",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "دما و رطوبت",
        "رطوبت‌سنج"
      ],
      "parentId": "temperature-and-humidity"
    },
    "ambient-thermometers": {
      "id": "ambient-thermometers",
      "en": "Ambient Thermometers",
      "slug": "ambient-thermometers",
      "fa": "دماسنج محیطی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گ'+N'یری",
        "دما و رطوبت",
        "دماسنج محیطی"
      ],
      "parentId": "temperature-and-humidity"
    },
    "thermocouples": {
      "id": "thermocouples",
      "en": "Thermocouples",
      "slug": "thermocouples",
      "fa": "ترموکوپل",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "دما و رطوبت",
        "ترموکوپل"
      ],
      "parentId": "temperature-and-humidity"
    },
    "environmental-measurement": {
      "id": "environmental-measurement",
      "en": "Environmental Measurement",
      "slug": "environmental-measurement",
      "fa": "اندازه‌گیری محیط",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "اندازه‌گیری محیط"
      ],
      "parentId": "measurement-tools"
    },
    "environmental-measurement-lux-meters": {
      "id": "environmental-measurement-lux-meters",
      "en": "Lux Meters",
      "slug": "environmental-measurement-lux-meters",
      "fa": "لوکس‌متر",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "اندازه‌گیری محیط",
        "لوکس‌متر"
      ],
      "parentId": "environmental-measurement"
    },
    "sound-level-meters": {
      "id": "sound-level-meters",
      "en": "Sound Level Meters",
      "slug": "sound-level-meters",
      "fa": "صوت‌سنج",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "اندازه‌گیری محیط",
        "صوت‌سنج"
      ],
      "parentId": "environmental-measurement"
    },
    "pressure-gauges": {
      "id": "pressure-gauges",
      "en": "Pressure Gauges",
      "slug": "pressure-gauges",
      "fa": "فشارسنج",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "اندازه‌گیری محیط",
        "فشارسنج"
      ],
      "parentId": "environmental-measurement"
    },
    "speed-meters": {
      "id": "speed-meters",
      "en": "Speed Meters",
      "slug": "speed-meters",
      "fa": "سرعت‌سنج",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "اندازه‌گیری محیط",
        "سرعت‌سنج"
      ],
      "parentId": "environmental-measurement"
    },
    "air-quality-meters": {
      "id": "air-quality-meters",
      "en": "Air Quality Meters",
      "slug": "air-quality-meters",
      "fa": "کیفیت‌سنج هوا",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "اندازه‌گیری محیط",
        "کیفیت‌سنج هوا"
      ],
      "parentId": "environmental-measurement"
    },
    "test-and-calibration-equipment": {
      "id": "test-and-calibration-equipment",
      "en": "Test and Calibration Equipment",
      "slug": "test-and-calibration-equipment",
      "fa": "منابع تست و کالیبراسیون",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "منابع تست و کالیبراسیون"
      ],
      "parentId": "measurement-tools"
    },
    "laboratory-power-supplies": {
      "id": "laboratory-power-supplies",
      "en": "Laboratory Power Supplies",
      "slug": "laboratory-power-supplies",
      "fa": "منبع تغذیه آزمایشگاهی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "منابع تست و کالیبراسیون",
        "منبع تغذیه آزمایشگاهی"
      ],
      "parentId": "test-and-calibration-equipment"
    },
    "signal-generators": {
      "id": "signal-generators",
      "en": "Signal Generators",
      "slug": "signal-generator'+N's",
      "fa": "مولد سیگنال",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "منابع تست و کالیبراسیون",
        "مولد سیگنال"
      ],
      "parentId": "test-and-calibration-equipment"
    },
    "calibrators": {
      "id": "calibrators",
      "en": "Calibrators",
      "slug": "calibrators",
      "fa": "کالیبراتور",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "منابع تست و کالیبراسیون",
        "کالیبراتور"
      ],
      "parentId": "test-and-calibration-equipment"
    },
    "electronic-loads": {
      "id": "electronic-loads",
      "en": "Electronic Loads",
      "slug": "electronic-loads",
      "fa": "بار الکترونیکی",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "منابع تست و کالیبراسیون",
        "بار الکترونیکی"
      ],
      "parentId": "test-and-calibration-equipment"
    },
    "test-leads-and-cables": {
      "id": "test-leads-and-cables",
      "en": "Test Leads and Cables",
      "slug": "test-leads-and-cables",
      "fa": "پراب و کابل تست",
      "path": [
        "ابزار و تجهیزات",
        "ابزار اندازه‌گیری",
        "منابع تست و کالیبراسیون",
        "پراب و کابل تست"
      ],
      "parentId": "test-and-calibration-equipment"
    },
    "hot-air-rework-stations": {
      "id": "hot-air-rework-stations",
      "en": "Hot Air Rework Stations",
      "slug": "hot-air-rework-stations",
      "fa": "ایستگاه هیتر هوای گرم",
      "path": [
        "ابزار و تجهیزات",
        "قطعات الکترونیکی",
        "تجهیزات لحیم‌کاری",
        "هیتر و هویه",
        "ایستگاه هیتر هوای گرم"
      ],
      "parentId": "hot-air-stations"
    }
  },
  "details": {
    "group-1": {
      "intro": "دریل چکشی شارژی برای سوراخ‌کاری و نصب‌های روزمره طراحی شده است. طراحی بی‌سیم، جابه‌جایی بین محل کار و خانه را ساده می‌کند؛ پیش از خرید، ظرفیت باتری و سازگاری مته با سطح موردنظر را بررسی کنید.",
      "highlights": [
        "مناسب نصب و تعمیرات عمومی",
        "قابلیت استفاده در محیط‌های بدون دسترسی مستقیم به برق",
        "انتخاب مته متناسب با چوب، فلز یا مصالح"
      ],
      "specs": [
        [
          "کاربری",
          "سوراخ‌کاری و نصب"
        ]
      ],
      "reviews": [
        [
          "کاربر مهمان",
          "برای نصب قفسه و تعمیرات سبک ازش استفاده کردم؛ خوش‌دست بود و جابه‌جایی بین اتاق‌ها با باتری راحت‌تر شد.",
          5,
          "ممنون از اینکه تجربه‌تان را نوشتید. برای حفظ عملکرد دستگاه، از متهٔ مناسب سطح استفاده کنید و باتری را طبق راهنمای سازنده شارژ کنید."
        ],
        [
          "کاربر مهمان",
          "برای سوراخ‌کاری چوب و فلز کارم را راه انداخت. کنترل سرعت کمک کرد شروع سوراخ‌کاری دقیق‌تر باشد.",
          4
        ],
        [
          "کاربر مهمان",
          "قبل از خرید اندازهٔ سه‌نظام و نوع باتری را با مته‌هایی که داشتم تطبیق دادم؛ پیشنهاد می‌کنم اقلام داخل جعبه را هم بررسی کنید.",
          5
        ],
        [
          "کاربر مهمان",
          "برای کارهای خانگی و نصب‌های معمولی انتخاب مناسبی است. در کار طولانی بهتر است بین کار به دستگاه استراحت بدهید.",
          4
        ],
        [
          "کاربر مهمان",
          "وزن دستگاه برای نصب پرده و قفسه مناسب بود و کنترل دور هم کمک کرد پیچ‌ها را راحت‌تر ببندم.",
          5
        ],
        [
          "کاربر مهمان",
          "برای کارهای سبک خانه خریدم. قبل از استفاده دفترچه را خواندم و متهٔ مناسب دیوار را جدا تهیه کردم.",
      '+N'    4
        ],
        [
          "کاربر مهمان",
          "سه‌نظام سریع باعث شد تعویض مته راحت باشد. برای سوراخ‌کاری‌های پشت‌سرهم، باتری اضافه کاربردی است.",
          5,
          "سپاس از تجربه‌تان. برای کار مداوم، ظرفیت باتری و زمان شارژ را با حجم کارتان هماهنگ کنید."
        ],
        [
          "کاربر مهمان",
          "در کارگاه برای نصب‌های کوتاه استفاده کردم؛ دسته خوش‌دست است و نگهداری دستگاه هم ساده به نظر می‌رسد.",
          4
        ],
        [
          "کاربر مهمان",
          "برای سوراخ‌کاری دقیق روی چوب، شروع آرام و ثابت نگه‌داشتن دستگاه نتیجهٔ خوبی داد.",
          5
        ],
        [
          "کاربر مهمان",
          "پیش از سفارش ابعاد و اقلام همراه را بررسی کردم. پیشنهاد می‌کنم نوع باتری موردنیازتان را هم در مشخصات ببینید.",
          4
        ],
        [
          "کاربر مهمان",
          "برای نصب کابینت و تعمیرات سبک مناسب است؛ برای کار سنگین‌تر بهتر است سراغ مدل متناسب با همان کاربرد بروید.",
          5
        ],
        [
          "کاربر مهمان",
          "برای استفادهٔ خانگی انتخاب راحتی بود. بعد از هر بار کار، مته را جدا کردم و دستگاه را در جای خشک گذاشتم.",
          4
        ]
      ],
      "questions": [
        [
          "چه مته‌ای برای این دریل مناسب است؟",
          "مته را با جنس سطح و قطر سوراخ انتخاب کنید و سازگاری آن با سه‌نظام دستگاه را بررسی کنید.",
          0,
          "برای چوب، فلز یا مصالح از متهٔ مخصوص همان سطح استفاده کنید و قطر قابل پشتیبانی سه‌نظام را در مشخصات مدل ببینید."
        ],
        [
          "باتری و شارژر داخل بسته هستند؟",
          "اقلام همراه را در بخش مشخصات و فهرست محتویات بستهٔ همین کالا ببینید.",
          0,
          "محتویات بسته ممکن است بین نسخه‌های مختلف تفاوت داشته باشد؛ فهرست اقلام همین محصول را پیش از ثبت سفارش بررسی کنید."
        ],
        [
          "برای سوراخ‌کاری مصالح چه نکته‌ای مهم است؟",
          "از متهٔ مناسب مصالح استفاده کنید و حالت چکشی و سرعت را مطابق راهنمای دستگاه تنظیم کنید.",
          0,
          "حالت چکشی را فقط برای مصالح مناسب فعال کنید و هنگام کار از عینک محافظ و فشار یکنواخت استفاده کنید."
        ],
        [
          "برای کارهای نصب خانگی مناسب است؟",
          "برای نصب‌های متداول و تعمیرات عمومی کاربرد دارد؛ نوع کار و مدت استفاده را با مشخصات دستگاه تطبیق دهید.",
          0,
          "بله، برای کارهای معمول خانگی مناسب است. برای استفادهٔ پیوسته یا سنگین، ظرفیت باتری و محدودهٔ کاری دستگاه را در نظر بگیرید."
        ],
        [
          "چطور عمر باتری را بهتر حفظ کنم؟",
          "شرایط شارژ و نگهداری اعلام‌شده از سوی سازنده را رعایت کنید.",
          0,
          "باتری را در دمای مناسب نگه دارید، از شارژر سازگار استفاده کنید و برای نگهداری طولانی‌مدت دستورالعمل سازنده را دنبال کنید."
        ],
        [
          "آیا می‌توان از این دستگاه برای بستن پیچ استفاده کرد؟",
          "در صورت پشتیبانی دستگاه و استفاده از سری پیچ‌گوشتی سازگار، سرعت را متناسب با کار تنظیم کنید.",
          0,
          "برای جلوگیری از آسیب به پیچ یا سطح، از دور پایین شروع کنید و فشار را یکنواخت نگه دارید."
        ],
        [
          "سه‌نظام چه اندازه مته‌ای را می‌پذیرد؟",
          "حداکثر قطر قابل پشتیبانی را در مشخصات فنی همین مدل بررسی کنید.",
          0,
          "اندازهٔ سه‌نظام بین مدل‌ها متفاوت است؛ پیش از خرید مته، محدودهٔ درج‌شده در مشخصات محصول را تطبیق دهید."
        ],
        [
          "برای استفادهٔ طولانی چه نکته‌ای را رعایت کنم؟",
          "زمان کار پیوسته و محدودیت حرارتی دستگاه را مطابق دفترچهٔ راهنما رعایت کنید.",'+N'
          0,
          "در استفادهٔ سنگین به دستگاه استراحت دهید و از مسدود نبودن مسیر تهویه اطمینان پیدا کنید."
        ],
        [
          "آیا باتری دستگاه با ابزارهای دیگر سازگار است؟",
          "سازگاری باتری را فقط بر اساس ولتاژ، سری و اعلام رسمی سازنده بررسی کنید.",
          0,
          "باتری‌های هم‌برند هم ممکن است بین سری‌ها سازگار نباشند؛ شماره مدل باتری و ابزار را مقایسه کنید."
        ],
        [
          "برای سوراخ‌کاری فلز از چه مته‌ای استفاده کنم؟",
          "متهٔ مناسب فلز را انتخاب کنید و سرعت پیشنهادی سازندهٔ مته را رعایت کنید.",
          0,
          "قطعه را محکم کنید، از عینک محافظ استفاده کنید و برای قطرهای بزرگ‌تر، مرحله‌ای سوراخ‌کاری کنید."
        ],
        [
          "چطور از لغزش مته در شروع کار جلوگیری کنم؟",
          "محل سوراخ را علامت بزنید و دستگاه را با سرعت کم و زاویهٔ درست شروع کنید.",
          0,
          "سطح کار را ثابت کنید و تا ایجاد شیار اولیه، دستگاه را بدون فشار ناگهانی کنترل کنید."
        ],
        [
          "آیا دستگاه برای کار روی بتن مناسب است؟",
          "نوع مصالح و توانایی مدل را در مشخصات همین محصول بررسی کنید؛ همهٔ دریل‌ها برای بتن مناسب نیستند.",
          0,
          "برای بتن از مته و حالت مجاز همان دستگاه استفاده کنید و محدودیت کاربرد درج‌شده در دفترچه را رعایت کنید."
        ],
        [
          "چه زمانی باتری را شارژ کنم؟",
          "چرخهٔ شارژ و نگهداری اعلام‌شده برای باتری همین مدل را دنبال کنید.",
          0,
          "از شارژر سازگار استفاده کنید و باتری را در معرض گرمای شدید یا رطوبت قرار ندهید."
        ],
        [
          "آیا کیف یا جعبه در بسته‌بندی وجود دارد؟",
          "اقلام همراه هر نسخه ممکن است متفاوت باشد؛ فهرست بسته‌بندی را در مشخصات سفارش ببینید.",
          0,
          "برای اطمینان، محتویات درج‌شده برای همین محصول را پیش از نهایی‌کردن خرید بررسی کنید."
        ]
      ]
    },
    "group-2": {
      "intro": "کیت مولتی‌متر دیجیتال برای اندازه‌گیری و عیب‌یابی اولیه در مدارهای الکتریکی به کار می‌رود. پیش از اندازه‌گیری، حالت مناسب را انتخاب کنید و ردهٔ ایمنی دستگاه را با محیط و ولتاژ کاری تطبیق دهید.",
      "highlights": [
        "مناسب بررسی اولیهٔ مدار",
        "نمایش دیجیتال برای خواندن آسان‌تر مقدار",
        "انتخاب رنج و ورودی مناسب پیش از اندازه‌گیری"
      ],
      "specs": [
        [
          "کاربری",
          "اندازه‌گیری و عیب‌یابی"
        ],
        [
          "نوع نمایش",
          "دیجیتال"
        ],
        [
          "نکتهٔ ایمنی",
          "بررسی ردهٔ حفاظتی پیش از استفاده"
        ]
      ],
      "reviews": [
        [
          "کاربر مهمان",
          "برای استفادهٔ کارگاهی، خوانایی نمایشگر و انتخاب درست حالت اندازه‌گیری اهمیت زیادی دارد.",
          5,
          "از دیدگاه شما سپاسگزاریم. برای اندازه‌گیری ایمن، حالت و ورودی دستگاه را متناسب با مدار انتخاب کنید و پیش از شروع، محدودهٔ کاری دستگاه را بررسی کنید."
        ],
        [
          "کاربر مهمان",
          "پراب‌ها را قبل از استفاده بررسی کردم و برای کارهای عیب‌یابی روزمره انتخاب حالت دستگاه را ساده یافتم.",
          4
        ],
        [
          "کاربر مهمان",
          "پیش از خرید، ردهٔ ایمنی و کمیت‌های قابل اندازه‌گیری را با نیازم تطبیق دادم؛ پیشنهاد می‌کنم مشخصات مدل را کامل بخوانید.",
          5
        ],
        [
          "کاربر مهمان",
          "برای اندازه‌گیری‌های معمولی کارگاه مناسب بود. نگهداری پراب‌ها در کیف جداگانه کمک می‌کند آسیب نبینند.",
          4
        ]
      ],
      "questions": [
        [
          "پیش از اندازه‌'+N'گیری چه چیزی را بررسی کنم؟",
          "حالت اندازه‌گیری، ورودی پراب و محدودهٔ کاری دستگاه را متناسب با مدار انتخاب کنید.",
          0,
          "پیش از اتصال پراب، سالم‌بودن عایق آن و انتخاب حالت و ورودی درست را بررسی کنید. برای کار روی مدار، ردهٔ ایمنی دستگاه باید با شرایط اندازه‌گیری سازگار باشد."
        ],
        [
          "این دستگاه چه کمیت‌هایی را اندازه می‌گیرد؟",
          "قابلیت‌هایی مانند ولتاژ، جریان و مقاومت را از جدول مشخصات همین مدل بررسی کنید."
        ],
        [
          "چطور اندازه‌گیری ایمن‌تری داشته باشم؟",
          "پراب سالم و ورودی درست را به کار ببرید و ردهٔ ایمنی دستگاه را با محیط کار تطبیق دهید."
        ]
      ]
    },
    "group-3": {
      "intro": "ایستگاه هویه برای لحیم‌کاری قطعات و تعمیرات الکترونیکی طراحی شده است. کنترل دما و انتخاب نوک متناسب با قطعه، به کار دقیق‌تر کمک می‌کند؛ هنگام استفاده تهویهٔ مناسب و پایهٔ ایمن هویه را در نظر بگیرید.",
      "highlights": [
        "مناسب تعمیرات برد و لحیم‌کاری قطعات",
        "امکان انتخاب نوک متناسب با نوع کار",
        "توجه به کنترل دما و تهویهٔ محل کار"
      ],
      "specs": [
        [
          "کاربری",
          "لحیم‌کاری و تعمیرات الکترونیک"
        ],
        [
          "لوازم پیشنهادی",
          "پایهٔ هویه، نوک یدکی و اسفنج تمیزکننده"
        ]
      ],
      "reviews": [
        [
          "کاربر مهمان",
          "برای مونتاژ قطعات ریز، انتخاب نوک مناسب و تنظیم دما نتیجهٔ کار را بهتر می‌کند.",
          5,
          "ممنون از اینکه تجربه‌تان را به اشتراک گذاشتید. نوک هویه را متناسب با ابعاد قطعه انتخاب کنید و برای لحیم‌کاری، تهویهٔ مناسب را در نظر داشته باشید."
        ],
        [
          "کاربر مهمان",
          "کنترل دما برای تعمیرات برد کاربردی است. بهتر است قبل از شروع، زمان گرم‌شدن دستگاه را در نظر بگیرید.",
          4
        ],
        [
          "کاربر مهمان",
          "برای کار دقیق، پایهٔ هویه و نوک متناسب با قطعه اهمیت زیادی دارد؛ اقلام داخل بسته را بررسی کنید.",
          5
        ],
        [
          "کاربر مهمان",
          "در استفادهٔ روزمره عملکرد قابل قبولی داشت. تهویهٔ مناسب میز کار را فراموش نکنید.",
          4
        ]
      ],
      "questions": [
        [
          "چه نوکی برای لحیم‌کاری قطعات ریز مناسب است؟",
          "نوک باریک و سازگار با هویه انتخاب کنید تا کنترل حرارت روی قطعات کوچک آسان‌تر باشد.",
          0,
          "نوک ظریف و سازگار با مدل ایستگاه، کنترل بهتری روی قطعات کوچک می‌دهد. پیش از خرید، سازگاری سری و توان دستگاه را در مشخصات بررسی کنید."
        ],
        [
          "دمای هویه را چطور تنظیم کنم؟",
          "از دمای پیشنهادی قطعه و سیم لحیم شروع کنید و در صورت نیاز تنظیم را مرحله‌ای انجام دهید."
        ],
        [
          "برای کار با هویه چه نکته‌ای را رعایت کنم؟",
          "از پایهٔ نسوز و تهویهٔ مناسب استفاده کنید و نوک داغ را پس از کار در جای ایمن قرار دهید."
        ]
      ]
    },
    "group-4": {
      "intro": "چراغ کار LED برای روشن‌کردن فضای تعمیر، کارگاه یا محل نصب کاربرد دارد. هنگام انتخاب، شدت روشنایی، زاویهٔ تابش، نوع پایه و روش تأمین انرژی را متناسب با محیط استفاده بسنجید.",
      "highlights": [
        "مناسب روشنایی موضعی محیط کار",
        "بررسی زاویهٔ تابش و امکان تنظیم پایه",
        "انتخاب منبع تغذیه متناسب با محل استفاده"
      ],
      "specs": [
        [
          "کاربری",
          "روشنایی کارگاهی و تعمیراتی"
        ],
        [
          "نوع روشنایی",
          "LED"
        ],
        [
          "نکتهٔ انتخاب",
          "بررسی شار نوری و د'+N'رجهٔ حفاظت"
        ]
      ],
      "reviews": [
        [
          "کاربر مهمان",
          "برای کار نزدیک، نور قابل تنظیم و پایهٔ پایدار استفاده را راحت‌تر می‌کند.",
          5,
          "از بازخورد شما سپاسگزاریم. برای انتخاب چراغ کار، زاویهٔ تابش و نوع پایه را با فضای کار خود هماهنگ کنید."
        ],
        [
          "کاربر مهمان",
          "برای میز تعمیرات نور موضعی خوبی فراهم می‌کند. قبل از سفارش، نوع پایه و روش تغذیه را ببینید.",
          4
        ],
        [
          "کاربر مهمان",
          "زاویهٔ تابش قابل تنظیم هنگام کار روی قطعات کوچک کمک‌کننده بود.",
          5
        ],
        [
          "کاربر مهمان",
          "برای استفادهٔ کارگاهی مناسب است؛ محل قرارگیری چراغ را با فضای میز هماهنگ کنید.",
          4
        ]
      ],
      "questions": [
        [
          "چه چراغی برای میز کار مناسب است؟",
          "زاویهٔ تابش، شدت نور و امکان تنظیم پایه را با ابعاد میز و نوع کار خود هماهنگ کنید.",
          0,
          "برای میز کار، نور یکنواخت و پایه یا زاویهٔ قابل تنظیم کاربردی است. شدت روشنایی و روش تأمین انرژی را با فضای کارتان تطبیق دهید."
        ],
        [
          "این چراغ با برق کار می‌کند یا باتری؟",
          "روش تأمین انرژی و اقلام همراه را در مشخصات همین مدل ببینید."
        ],
        [
          "برای فضای مرطوب مناسب است؟",
          "درجهٔ حفاظت درج‌شده برای مدل را بررسی کنید و از استفاده خارج از محدودهٔ اعلام‌شده بپرهیزید."
        ]
      ]
    },
    "group-5": {
      "intro": "مجموعه ابزار برق‌کار برای انجام کارهای نصب و سرویس روزانه گردآوری می‌شود. محتویات بسته در مدل‌های مختلف فرق می‌کند؛ پیش از سفارش، فهرست اقلام و عایق‌بودن ابزارهای موردنیاز را با نوع کار تطبیق دهید.",
      "highlights": [
        "مناسب نصب و سرویس تجهیزات برقی",
        "بررسی تعداد و نوع اقلام داخل بسته",
        "انتخاب ابزار عایق متناسب با سطح ایمنی کار"
      ],
      "specs": [
        [
          "کاربری",
          "نصب و سرویس برق"
        ],
        [
          "نکتهٔ ایمنی",
          "ابزار عایق جایگزین قطع برق و دستورالعمل ایمنی نیست"
        ]
      ],
      "reviews": [
        [
          "کاربر مهمان",
          "قبل از انتخاب، فهرست ابزارهای موردنیاز را با اقلام داخل جعبه تطبیق دهید.",
          5,
          "ممنون از نکتهٔ خوبی که گفتید. محتویات مجموعه را با نیاز کاری‌تان تطبیق دهید و برای کار برقی، ابزار مناسب و دستورالعمل ایمنی را رعایت کنید."
        ],
        [
          "کاربر مهمان",
          "برای سرویس‌های معمولی مجموعهٔ کاربردی‌ای است. تعداد و نوع اقلام را با نیاز کارتان مقایسه کنید.",
          4
        ],
        [
          "کاربر مهمان",
          "چیدمان ابزارها نگهداری را ساده می‌کند؛ مشخصات عایق‌بودن هر ابزار را جداگانه بررسی کنید.",
          5
        ],
        [
          "کاربر مهمان",
          "برای کارهای نصب روزانه مناسب بود. بسته‌بندی را هنگام تحویل با فهرست اقلام تطبیق دادم.",
          4
        ]
      ],
      "questions": [
        [
          "داخل مجموعه چه ابزارهایی قرار دارد؟",
          "فهرست دقیق محتویات بسته را در مشخصات همین کالا بررسی کنید؛ اقلام هر مجموعه متفاوت است.",
          0,
          "محتویات بین مدل‌ها و بسته‌بندی‌ها فرق می‌کند؛ فهرست اقلام درج‌شده برای همین کالا را پیش از سفارش ملاک قرار دهید."
        ],
        [
          "برای کار برق، ابزار عایق لازم است؟",
          "ابزار را با سطح ایمنی و نوع کار انتخاب کنید و همیشه دستورالعمل‌های ایمنی را رعایت کنید."
        ],
        [
          "این مجموعه برای کارهای نصب مناسب است؟",
          '+N'"اقلام داخل بسته را با ابزارهای موردنیاز برای نصب و سرویس خود تطبیق دهید."
        ]
      ]
    },
    "group-6": {
      "intro": "تستر و فازمتر دیجیتال برای بررسی اولیهٔ وضعیت برق و تشخیص سریع در کارهای سرویس استفاده می‌شود. روش کار و سطح ایمنی ابزار را مطابق دستورالعمل سازنده رعایت کنید و برای تشخیص قطعی، از تجهیزات مناسب و فرد متخصص کمک بگیرید.",
      "highlights": [
        "مناسب بررسی اولیه و عیب‌یابی سریع",
        "انتخاب قابلیت متناسب با نوع اندازه‌گیری",
        "رعایت دستورالعمل ایمنی سازنده"
      ],
      "specs": [
        [
          "کاربری",
          "بررسی اولیهٔ مدار"
        ],
        [
          "نوع ابزار",
          "تستر / فازمتر"
        ],
        [
          "نکتهٔ ایمنی",
          "برای کار تخصصی از ابزار دارای ردهٔ ایمنی مناسب استفاده شود"
        ]
      ],
      "reviews": [
        [
          "کاربر مهمان",
          "برای استفادهٔ مطمئن، روش تست و محدودیت‌های ابزار را قبل از کار مطالعه کنید.",
          5,
          "از دیدگاه شما سپاسگزاریم. پیش از استفاده، راهنمای تست و محدودهٔ کاری ابزار را مرور کنید و نکات ایمنی سازنده را رعایت کنید."
        ],
        [
          "کاربر مهمان",
          "برای بررسی‌های اولیه کار را سریع‌تر می‌کند؛ محدودهٔ ولتاژ را پیش از استفاده چک کنید.",
          4
        ],
        [
          "کاربر مهمان",
          "راهنمای دستگاه را قبل از استفاده خواندم و ابزار را فقط در محدودهٔ درج‌شده به کار بردم.",
          5
        ],
        [
          "کاربر مهمان",
          "برای سرویس‌های سبک انتخاب مناسبی بود. در کار تخصصی از تجهیزات دارای ردهٔ ایمنی مناسب استفاده کنید.",
          4
        ]
      ],
      "questions": [
        [
          "این تستر چه مواردی را تشخیص می‌دهد؟",
          "نوع تشخیص و محدودهٔ کاری را در مشخصات همان مدل بررسی کنید؛ قابلیت‌ها بین مدل‌ها متفاوت است.",
          0,
          "قابلیت تشخیص به مدل بستگی دارد. محدودهٔ ولتاژ، نوع نشانگر و ردهٔ ایمنی را از مشخصات همین دستگاه بررسی کنید و خارج از محدودهٔ اعلام‌شده از آن استفاده نکنید."
        ],
        [
          "قبل از استفاده چه کاری انجام بدهم؟",
          "روش تست، نشانگرها و محدودیت‌های ابزار را از راهنمای سازنده بخوانید."
        ],
        [
          "برای بررسی برق شهر قابل استفاده است؟",
          "محدودهٔ ولتاژ و ردهٔ ایمنی مدل را پیش از استفاده بررسی کنید و نکات ایمنی را رعایت کنید."
        ]
      ]
    }
  },
  "media": {
    "product-1": {
      "id": "product-1",
      "main": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
      "card": "content/products/images/card/cordless-hammer-drill-hn-001-row-001-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "con'+N'tent/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-2": {
      "id": "product-2",
      "main": "content/products/images/main/cordless-drill-driver-hn-002-row-002-main.webp",
      "card": "content/products/images/card/cordless-drill-driver-hn-002-row-002-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/cordless-drill-driver-hn-002-row-002-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/cordless-drill-driver-hn-002-row-002-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-drill-driver-hn-002-row-002-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-3": {
      "id": "product-3",
      "main": "content/products/images/main/electric-hammer-drill-hn-003-row-003-main.webp",
      "card": "content/products/images/card/electric-hammer-drill-hn-003-row-003-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/electric-hammer-drill-hn-003-row-003-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/electric-hammer-drill-hn-003-row-003-main.webp",
          "thumbnail": "content/products/images/thumbnail/electric-hammer-drill-hn-003-row-003-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-4": {
      "id": "product-4",
      "main": "content/products/images/main/two-speed-cordless-drill-driver-hn-004-row-004-main.webp",
      "card": "content/products/images/card/two-speed-cordless-drill-driver-hn-004-row-004-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/two-speed-cordless-drill-driver-hn-004-row-004-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/two-speed-cordless-drill-driver-hn-004-row-004-main.webp",
          "thumbnail": "content/produc'+N'ts/images/thumbnail/two-speed-cordless-drill-driver-hn-004-row-004-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-5": {
      "id": "product-5",
      "main": "content/products/images/main/electric-hammer-drill-with-keyless-chuck-hn-005-row-005-main.webp",
      "card": "content/products/images/card/electric-hammer-drill-with-keyless-chuck-hn-005-row-005-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/electric-hammer-drill-with-keyless-chuck-hn-005-row-005-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/electric-hammer-drill-with-keyless-chuck-hn-005-row-005-main.webp",
          "thumbnail": "content/products/images/thumbnail/electric-hammer-drill-with-keyless-chuck-hn-005-row-005-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-6": {
      "id": "product-6",
      "main": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
      "card": "content/products/images/card/digital-multimeter-hn-006-row-006-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content'+N'/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-7": {
      "id": "product-7",
      "main": "content/products/images/main/workshop-digital-multimeter-hn-007-row-007-main.webp",
      "card": "content/products/images/card/workshop-digital-multimeter-hn-007-row-007-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/workshop-digital-multimeter-hn-007-row-007-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/workshop-digital-multimeter-hn-007-row-007-main.webp",
          "thumbnail": "content/products/images/thumbnail/workshop-digital-multimeter-hn-007-row-007-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-8": {
      "id": "product-8",
      "main": "content/products/images/main/handheld-multimeter-with-large-display-hn-008-row-008-main.webp",
      "card": "content/products/images/card/handheld-multimeter-with-large-display-hn-008-row-008-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/handheld-multimeter-with-large-display-hn-008-row-008-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/handheld-multimeter-with-large-display-hn-008-row-008-main.webp",
          "thumbnail": "content/products/images/thumbnail/handheld-multimeter-with-large-display-hn-008-row-008-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-9": {
      "id": "product-9",
      "main": "content/products/images/main/digital-clamp-meter-hn-009-row-009-main.webp",
      "card": "content/products/images/card/digital-clamp-meter-hn-009-row-009-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/digital-clamp-meter-hn-009-row-009-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/digital-clamp-meter-hn-009-row-009-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-cl'+N'amp-meter-hn-009-row-009-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-10": {
      "id": "product-10",
      "main": "content/products/images/main/electrical-installation-digital-multimeter-hn-010-row-010-main.webp",
      "card": "content/products/images/card/electrical-installation-digital-multimeter-hn-010-row-010-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/electrical-installation-digital-multimeter-hn-010-row-010-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/electrical-installation-digital-multimeter-hn-010-row-010-main.webp",
          "thumbnail": "content/products/images/thumbnail/electrical-installation-digital-multimeter-hn-010-row-010-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-11": {
      "id": "product-11",
      "main": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
      "card": "content/products/images/card/digital-soldering-station-hn-011-row-011-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "co'+N'ntent/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-12": {
      "id": "product-12",
      "main": "content/products/images/main/adjustable-soldering-iron-hn-012-row-012-main.webp",
      "card": "content/products/images/card/adjustable-soldering-iron-hn-012-row-012-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/adjustable-soldering-iron-hn-012-row-012-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/adjustable-soldering-iron-hn-012-row-012-main.webp",
          "thumbnail": "content/products/images/thumbnail/adjustable-soldering-iron-hn-012-row-012-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-13": {
      "id": "product-13",
      "main": "content/products/images/main/hot-air-rework-station-hn-013-row-013-main.webp",
      "card": "content/products/images/card/hot-air-rework-station-hn-013-row-013-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/hot-air-rework-station-hn-013-row-013-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/hot-air-rework-station-hn-013-row-013-main.webp",
          "thumbnail": "content/products/images/thumbnail/hot-air-rework-station-hn-013-row-013-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-14": {
      "id": "product-14",
      "main": "content/products/images/main/bench-soldering-station-hn-014-row-014-main.webp",
      "card": "content/products/images/card/bench-soldering-station-hn-014-row-014-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/bench-soldering-station-hn-014-row-014-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/bench-soldering-station-hn-014-row-014-main.webp",
          "thumbnail": "content/products/images/thumbnail/bench-soldering-station-hn-014-row-014-thumbnail-256.jpg"
        },
        {
          "src": "c'+N'ontent/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-15": {
      "id": "product-15",
      "main": "content/products/images/main/lightweight-workshop-soldering-iron-hn-015-row-015-main.webp",
      "card": "content/products/images/card/lightweight-workshop-soldering-iron-hn-015-row-015-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/lightweight-workshop-soldering-iron-hn-015-row-015-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/lightweight-workshop-soldering-iron-hn-015-row-015-main.webp",
          "thumbnail": "content/products/images/thumbnail/lightweight-workshop-soldering-iron-hn-015-row-015-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-16": {
      "id": "product-16",
      "main": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
      "card": "content/products/images/card/portable-led-work-light-hn-016-row-016-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-17"'+N': {
      "id": "product-17",
      "main": "content/products/images/main/workshop-led-floodlight-hn-017-row-017-main.webp",
      "card": "content/products/images/card/workshop-led-floodlight-hn-017-row-017-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/workshop-led-floodlight-hn-017-row-017-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/workshop-led-floodlight-hn-017-row-017-main.webp",
          "thumbnail": "content/products/images/thumbnail/workshop-led-floodlight-hn-017-row-017-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-18": {
      "id": "product-18",
      "main": "content/products/images/main/rechargeable-inspection-light-hn-018-row-018-main.webp",
      "card": "content/products/images/card/rechargeable-inspection-light-hn-018-row-018-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/rechargeable-inspection-light-hn-018-row-018-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/rechargeable-inspection-light-hn-018-row-018-main.webp",
          "thumbnail": "content/products/images/thumbnail/rechargeable-inspection-light-hn-018-row-018-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-19": {
      "id": "product-19",
      "main": "content/products/images/main/led-work-light-with-stand-hn-019-row-019-main.webp",
      "card": "content/products/images/card/led-work-light-with-stand-hn-019-row-019-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/led-work-light-with-stand-hn-019-row-019-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/led-work-light-with-stand-hn-019-row-019-main.webp",
          "thumbnail": "content/products/images/thumbnail/led-work-light-with-stand-hn-019-row-019-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/pro'+N'ducts/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-20": {
      "id": "product-20",
      "main": "content/products/images/main/portable-led-floodlight-hn-020-row-020-main.webp",
      "card": "content/products/images/card/portable-led-floodlight-hn-020-row-020-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/portable-led-floodlight-hn-020-row-020-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/portable-led-floodlight-hn-020-row-020-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-floodlight-hn-020-row-020-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-multimeter-hn-006-row-006-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-multimeter-hn-006-row-006-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-21": {
      "id": "product-21",
      "main": "content/products/images/main/insulated-electrician-screwdriver-set-hn-021-row-021-main.webp",
      "card": "content/products/images/card/insulated-electrician-screwdriver-set-hn-021-row-021-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/insulated-electrician-screwdriver-set-hn-021-row-021-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/insulated-electrician-screwdriver-set-hn-021-row-021-main.webp",
          "thumbnail": "content/products/images/thumbnail/insulated-electrician-screwdriver-set-hn-021-row-021-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-22": {
      "id": "product-22",
      "main": "content/products/images/main/electrici'+N'an-pliers-set-hn-022-row-022-v2-main.jpg",
      "card": "content/products/images/card/electrician-pliers-set-hn-022-row-022-v2-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/electrician-pliers-set-hn-022-row-022-v2-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/electrician-pliers-set-hn-022-row-022-v2-main.jpg",
          "thumbnail": "content/products/images/thumbnail/electrician-pliers-set-hn-022-row-022-v2-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-23": {
      "id": "product-23",
      "main": "content/products/images/main/insulated-electrician-tool-kit-hn-023-row-023-main.webp",
      "card": "content/products/images/card/insulated-electrician-tool-kit-hn-023-row-023-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/insulated-electrician-tool-kit-hn-023-row-023-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/insulated-electrician-tool-kit-hn-023-row-023-main.webp",
          "thumbnail": "content/products/images/thumbnail/insulated-electrician-tool-kit-hn-023-row-023-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-24": {
      "id": "product-24",
      "main": "content/products/images/main/wiring-and-installation-tool-set-hn-024-row-024-main.webp",
      "card": "content/products/images/card/wiring-and-installation-tool-set-hn-024-row-024-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/wiring-and-installation-tool-set-hn-024-row-024-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/wiring-and-installation-tool-set-hn-024-row-024-main.webp",
          "thumbnail": "content/products/images/thumbnail/wiring-and-installation-tool-set-hn-024-row-024-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-25": {
      "id": "product-25",
      "main": "content/products/images/main/electrician-tool-bag-set-hn-025'+N'-row-025-main.webp",
      "card": "content/products/images/card/electrician-tool-bag-set-hn-025-row-025-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/electrician-tool-bag-set-hn-025-row-025-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/electrician-tool-bag-set-hn-025-row-025-main.webp",
          "thumbnail": "content/products/images/thumbnail/electrician-tool-bag-set-hn-025-row-025-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-26": {
      "id": "product-26",
      "main": "content/products/images/main/digital-voltage-tester-hn-026-row-026-main.webp",
      "card": "content/products/images/card/digital-voltage-tester-hn-026-row-026-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/digital-voltage-tester-hn-026-row-026-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/digital-voltage-tester-hn-026-row-026-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-voltage-tester-hn-026-row-026-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-27": {
      "id": "product-27",
      "main": "content/products/images/main/non-contact-voltage-tester-hn-027-row-027-main.webp",
      "card": "content/products/images/card/non-contact-voltage-tester-hn-027-row-027-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/non-contact-voltage-tester-hn-027-row-027-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/non-contact-voltage-tester-hn-027-row-027-main.webp",
          "thumbnail": "content/products/images/thumbnail/non-contact-voltage-tester-hn-027-row-027-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
         '+N' "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-28": {
      "id": "product-28",
      "main": "content/products/images/main/inductive-voltage-tester-hn-028-row-028-main.webp",
      "card": "content/products/images/card/inductive-voltage-tester-hn-028-row-028-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/inductive-voltage-tester-hn-028-row-028-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/inductive-voltage-tester-hn-028-row-028-main.webp",
          "thumbnail": "content/products/images/thumbnail/inductive-voltage-tester-hn-028-row-028-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-29": {
      "id": "product-29",
      "main": "content/products/images/main/circuit-continuity-tester-hn-029-row-029-main.webp",
      "card": "content/products/images/card/circuit-continuity-tester-hn-029-row-029-card-800.jpg",
      "thumbnail": "content/products/images/thumbnail/circuit-continuity-tester-hn-029-row-029-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/circuit-continuity-tester-hn-029-row-029-main.webp",
          "thumbnail": "content/products/images/thumbnail/circuit-continuity-tester-hn-029-row-029-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    },
    "product-30": {
      "id": "product-30",
      "main": "content/products/images/main/voltage-detector-pen-hn-030-row-030-main.webp",
      "card": "content/products/images/card/voltage-detector-pen-hn-030-row-030-card-800.jpg",
      "thum'+N'bnail": "content/products/images/thumbnail/voltage-detector-pen-hn-030-row-030-thumbnail-256.jpg",
      "images": [
        {
          "src": "content/products/images/main/voltage-detector-pen-hn-030-row-030-main.webp",
          "thumbnail": "content/products/images/thumbnail/voltage-detector-pen-hn-030-row-030-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/cordless-hammer-drill-hn-001-row-001-main.webp",
          "thumbnail": "content/products/images/thumbnail/cordless-hammer-drill-hn-001-row-001-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/digital-soldering-station-hn-011-row-011-main.webp",
          "thumbnail": "content/products/images/thumbnail/digital-soldering-station-hn-011-row-011-thumbnail-256.jpg"
        },
        {
          "src": "content/products/images/main/portable-led-work-light-hn-016-row-016-main.webp",
          "thumbnail": "content/products/images/thumbnail/portable-led-work-light-hn-016-row-016-thumbnail-256.jpg"
        }
      ],
      "videos": []
    }
  },
  "products": [
    {
      "id": 1,
      "companionProductIds": [21, 17, 28, 9, 25, 16],
      "englishName": "Cordless Hammer Drill",
      "name": "دریل چکشی شارژی بوش با سه‌نظام اتوماتیک مناسب نصب و کارگاه",
      "powerSource": "باتری شارژی",
      "useCase": "نصب و کارگاه",
      "stockQuantity": 12,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 24.5,
      "discountPercent": 15,
      "imageSet": 0,
      "newest": 30,
      "variants": {
        "battery": {
          "label": "ظرفیت باتری",
          "options": [
            {
              "value": "2ah",
              "label": "۲ آمپرساعت",
              "priceDelta": 0
            },
            {
              "value": "4ah",
              "label": "۴ آمپرساعت",
              "priceDelta": 1.2
            }
          ]
        },
        "chuck": {
          "label": "قطر سه‌نظام",
          "options": [
            {
              "value": "10mm",
              "label": "۱۰ میلی‌متر",
              "priceDelta": 0
            },
            {
              "value": "13mm",
              "label": "۱۳ میلی‌متر",
              "priceDelta": 0.6
            }
          ]
        }
      },
      "variantStock": null,
      "accessories": [
        {
          "id": "shipping-insurance",
          "name": "بیمهٔ حمل و تحویل کالا",
          "price": 0.35,
          "required": true,
          "description": "پوشش بیمه‌ای سفارش؛ در این پیشنهاد به‌صورت الزامی محاسبه می‌شود."
        },
        {
          "id": "sds-bit-set",
          "name": "ست متهٔ چهارشیار بتن",
          "price": 1.25,
          "required": false,
          "description": "مته‌های بتن با اتصال SDS-Plus؛ سازگاری سه‌نظام را بررسی کنید."
        },
        {
          "id": "drill-case",
          "name": "کیف حمل دریل",
          "price": 0.9,
          "required": false,
          "description": "کیف محافظ برای حمل دریل و متعلقات؛ انتخاب این مورد به طرح فروش بستگی دارد."
        }
      ],
      "badge": "پرفروش",
      "slug": "cordless-hammer-drill",
      "code": "HN-001",
      "brandId": "bosch",
      "categoryId": "hammer-drills",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "نصب و کارگاه" },
        { "featureId": "pow'+N'er-source", "value": "باتری شارژی" },
        { "featureId": "tool-kind", "value": "دریل چکشی شارژی" },
        { "featureId": "drill-mode", "value": "چکشی" },
        { "featureId": "chuck-type", "value": "اتوماتیک" }
      ],
      "detailId": "group-1",
      "mediaId": "product-1",
      "includedBenefits": [
        {
          "title": "۱۸ ماه ضمانت خدمات فنی",
          "detail": "از زمان خرید، همراه کالا",
          "icon": "bi-shield-check"
        },
        {
          "title": "بازرسی سه‌نظام پیش از ارسال",
          "detail": "کنترل عملکرد بدون هزینهٔ جداگانه",
          "icon": "bi-wrench-adjustable"
        }
      ]
    },
    {
      "id": 2,
      "companionProductIds": [22, 18, 29, 10, 21, 17],
      "englishName": "Cordless Drill Driver",
      "name": "دریل پیچ‌گوشتی شارژی",
      "powerSource": "باتری شارژی",
      "useCase": "نصب و تعمیر",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 17,
      "discountPercent": 0,
      "imageSet": 0,
      "newest": 29,
      "variants": null,
      "variantStock": null,
      "accessories": [
        {
          "id": "battery-pack",
          "name": "باتری یدک سازگار",
          "price": 1.8,
          "required": false,
          "description": "باتری اضافه برای افزایش زمان کار؛ ولتاژ و نوع اتصال را بررسی کنید."
        },
        {
          "id": "driver-bit-set",
          "name": "ست سری پیچ‌گوشتی",
          "price": 0.55,
          "required": false,
          "description": "سری‌های پرکاربرد برای باز و بسته‌کردن پیچ‌ها."
        }
      ],
      "badge": "",
      "slug": "cordless-drill-driver",
      "code": "HN-002",
      "brandId": "makita",
      "categoryId": "cordless-drill",
      "colorIds": [
        "blue",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "نصب و تعمیر" },
        { "featureId": "power-source", "value": "باتری شارژی" },
        { "featureId": "tool-kind", "value": "دریل پیچ‌گوشتی شارژی" },
        { "featureId": "drive-speed", "value": "کنترل برای سوراخ‌کاری و پیچ‌کاری" }
      ],
      "detailId": "group-1",
      "mediaId": "product-2",
      "includedBenefits": [
        {
          "title": "۱۲ ماه ضمانت تعمیر",
          "detail": "پوشش خدمات فنی محصول",
          "icon": "bi-shield-check"
        }
      ]
    },
    {
      "id": 3,
      "companionProductIds": [23, 19, 30, 6, 22, 18],
      "englishName": "Electric Hammer Drill",
      "name": "دریل چکشی برقی",
      "powerSource": "برق شهری",
      "useCase": "کارگاه ساختمانی",
      "stockQuantity": 7,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 20,
      "discountPercent": 0,
      "imageSet": 0,
      "newest": 28,
      "variants": {
        "color": {
          "label": "رنگ",
          "options": [
            {
              "value": "yellow",
              "label": "زرد",
              "swatch": "#f6c515",
              "priceDelta": 0
            },
            {
              "value": "black",
              "label": "مشکی",
              "swatch": "#252525",
              "priceDelta": 0.35
            }
          ]
        }
      },
      "variantStock": {
        "color=yellow": 7,
        "color=black": 3
      },
      "accessories": [
        {
          "id": "sds-bit-set",
          "name": "ست متهٔ چهارشیار بتن",
          "pri'+N'ce": 1.45,
          "required": false,
          "description": "مته‌های بتن با اتصال SDS-Plus؛ نوع سه‌نظام را بررسی کنید."
        },
        {
          "id": "drill-case",
          "name": "کیف حمل دریل",
          "price": 0.9,
          "required": false,
          "description": "کیف محافظ برای حمل و نگهداری ابزار و متعلقات."
        }
      ],
      "badge": "",
      "slug": "electric-hammer-drill",
      "code": "HN-003",
      "brandId": "dewalt",
      "categoryId": "hammer-drills",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "کارگاه ساختمانی" },
        { "featureId": "power-source", "value": "برق شهری" },
        { "featureId": "tool-kind", "value": "دریل چکشی برقی" },
        { "featureId": "drill-mode", "value": "چکشی" }
      ],
      "detailId": "group-1",
      "mediaId": "product-3",
      "includedBenefits": [
        {
          "title": "تست ایمنی پیش از ارسال",
          "detail": "بررسی عملکرد دستگاه در فروشگاه",
          "icon": "bi-clipboard2-check"
        }
      ]
    },
    {
      "id": 4,
      "companionProductIds": [24, 20, 26, 7, 23, 19],
      "englishName": "Two-Speed Cordless Drill Driver",
      "name": "دریل پیچ‌گوشتی شارژی دوسرعته",
      "powerSource": "باتری شارژی",
      "useCase": "نصب و مونتاژ",
      "stockQuantity": 3,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 23,
      "discountPercent": 0,
      "imageSet": 0,
      "newest": 27,
      "variants": {
        "battery": {
          "label": "ظرفیت باتری",
          "options": [
            {
              "value": "2ah",
              "label": "۲ آمپرساعت",
              "priceDelta": 0
            },
            {
              "value": "4ah",
              "label": "۴ آمپرساعت",
              "priceDelta": 1.1
            }
          ]
        },
        "chuck": {
          "label": "ظرفیت سه‌نظام",
          "options": [
            {
              "value": "10mm",
              "label": "۱۰ میلی‌متر",
              "priceDelta": 0
            },
            {
              "value": "13mm",
              "label": "۱۳ میلی‌متر",
              "priceDelta": 0.55
            }
          ]
        }
      },
      "variantStock": {
        "battery=2ah|chuck=10mm": 3,
        "battery=2ah|chuck=13mm": 0,
        "battery=4ah|chuck=10mm": 2,
        "battery=4ah|chuck=13mm": 1
      },
      "accessories": [
        {
          "id": "battery-charger-kit",
          "name": "باتری و شارژر سازگار",
          "price": 2.4,
          "required": false,
          "description": "پیش از سفارش، سازگاری باتری و شارژر با پلتفرم دستگاه را بررسی کنید."
        },
        {
          "id": "driver-bit-set",
          "name": "ست سری پیچ‌گوشتی",
          "price": 0.55,
          "required": false,
          "description": "سری‌های پرکاربرد برای نصب و مونتاژ."
        }
      ],
      "badge": "",
      "slug": "two-speed-cordless-drill-driver",
      "code": "HN-004",
      "brandId": "ronix",
      "categoryId": "cordless-drill",
      "colorIds": [
        "red",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "نصب و مونتاژ" },
        { "featureId": "power-source", "value": "باتری شارژی" },
        { "featureId": "tool-kind", "value": "دریل پیچ‌گوشتی شارژی" },
   '+N'     { "featureId": "drive-speed", "value": "دوسرعته" }
      ],
      "detailId": "group-1",
      "mediaId": "product-4"
    },
    {
      "id": 5,
      "companionProductIds": [25, 16, 27, 8, 24, 20],
      "englishName": "Electric Hammer Drill with Keyless Chuck",
      "name": "دریل چکشی برقی سه‌نظام اتوماتیک",
      "powerSource": "برق شهری",
      "useCase": "تعمیر و نگهداری",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 26,
      "discountPercent": 0,
      "imageSet": 0,
      "newest": 26,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "electric-hammer-drill-with-keyless-chuck",
      "code": "HN-005",
      "brandId": "tosan",
      "categoryId": "hammer-drills",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تعمیر و نگهداری" },
        { "featureId": "power-source", "value": "برق شهری" },
        { "featureId": "tool-kind", "value": "دریل چکشی برقی" },
        { "featureId": "drill-mode", "value": "چکشی" },
        { "featureId": "chuck-type", "value": "اتوماتیک" }
      ],
      "detailId": "group-1",
      "mediaId": "product-5"
    },
    {
      "id": 6,
      "companionProductIds": [11, 17, 23, 29, 20, 21],
      "englishName": "Digital Multimeter",
      "name": "مولتی‌متر دیجیتال دستی یونی‌تی برای تست و عیب‌یابی مدار",
      "powerSource": "باتری‌خور",
      "useCase": "تست و عیب‌یابی",
      "stockQuantity": 18,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 29,
      "discountPercent": 15,
      "imageSet": 1,
      "newest": 25,
      "variants": {
        "color": {
          "label": "رنگ",
          "options": [
            {
              "value": "yellow",
              "label": "زرد",
              "swatch": "#f6c515",
              "priceDelta": 0
            },
            {
              "value": "black",
              "label": "مشکی",
              "swatch": "#252525",
              "priceDelta": 0.2
            }
          ]
        }
      },
      "variantStock": {
        "color=yellow": 12,
        "color=black": 6
      },
      "accessories": [
        {
          "id": "meter-case",
          "name": "کیف محافظ مولتی‌متر",
          "price": 0.65,
          "required": false,
          "description": "برای نگهداری دستگاه و پراب‌ها در محیط کار."
        },
        {
          "id": "probe-set",
          "name": "ست پراب اندازه‌گیری",
          "price": 0.45,
          "required": false,
          "description": "پراب یدک؛ ردهٔ ایمنی و سازگاری اتصال را بررسی کنید."
        },
        {
          "id": "meter-test-lead",
          "name": "ست کابل و گیرهٔ تست",
          "price": 0.72,
          "required": false,
          "description": "کابل و گیرهٔ کمکی برای تست‌های کارگاهی؛ ردهٔ ایمنی را با محیط کار تطبیق دهید."
        }
      ],
      "badge": "جدید",
      "slug": "digital-multimeter",
      "code": "HN-006",
      "brandId": "uni-t",
      "categoryId": "digital-multimeters",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "tool-kind", "value": "مولتی‌متر دیجیتال" },
        { "featureId": "usage", "value": "اندازه‌گیری و عیب‌یابی" },
        { "featureId": "display-type", "value": "دیجیت'+N'ال" },
        { "featureId": "safety-note", "value": "بررسی ردهٔ حفاظتی پیش از استفاده" }
      ],
      "detailId": "group-2",

      "mediaId": "product-6",
      "includedBenefits": [
        {
          "title": "کالیبراسیون اولیه",
          "detail": "آمادهٔ استفاده، بدون هزینهٔ اضافه",
          "icon": "bi-speedometer2"
        }
      ]
    },
    {
      "id": 7,
      "companionProductIds": [12, 18, 24, 30, 16, 22],
      "englishName": "Workshop Digital Multimeter",
      "name": "مولتی‌متر دیجیتال کارگاهی",
      "powerSource": "باتری‌خور",
      "useCase": "تعمیرگاه و کارگاه",
      "stockQuantity": 5,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 32,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 24,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "workshop-digital-multimeter",
      "code": "HN-007",
      "brandId": "fluke",
      "categoryId": "digital-multimeters",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تعمیرگاه و کارگاه" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "مولتی‌متر دیجیتال" },
        { "featureId": "meter-format", "value": "کارگاهی" },
        { "featureId": "display-type", "value": "دیجیتال" }
      ],
      "detailId": "group-2",
      "mediaId": "product-7",
      "includedBenefits": [
        {
          "title": "بررسی دقت اندازه‌گیری",
          "detail": "کنترل اولیه پیش از تحویل",
          "icon": "bi-crosshair2"
        }
      ]
    },
    {
      "id": 8,
      "companionProductIds": [13, 19, 25, 26, 17, 23],
      "englishName": "Handheld Multimeter with Large Display",
      "name": "مولتی‌متر دستی با نمایشگر بزرگ",
      "powerSource": "باتری‌خور",
      "useCase": "تست و عیب‌یابی",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 35,
      "discountPercent": 10,
      "imageSet": 1,
      "newest": 23,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "پرفروش",
      "slug": "handheld-multimeter-with-large-display",
      "code": "HN-008",
      "brandId": "mastech",
      "categoryId": "digital-multimeters",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تست و عیب‌یابی" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "مولتی‌متر دیجیتال" },
        { "featureId": "meter-format", "value": "دستی با نمایشگر بزرگ" },
        { "featureId": "display-type", "value": "دیجیتال" }
      ],
      "detailId": "group-2",
      "mediaId": "product-8"
    },
    {
      "id": 9,
      "companionProductIds": [14, 20, 21, 27, 18, 24],
      "englishName": "Digital Clamp Meter",
      "name": "کلمپ‌متر دیجیتال",
      "powerSource": "باتری‌خور",
      "useCase": "برق صنعتی",
      "stockQuantity": 2,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 38,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 22,
      "variants": {
        "color": {
          "label": "رنگ",
          "options": [
            {
              "value": "gray",
              "label": "خ'+N'اکستری",
              "swatch": "#8b929a",
              "priceDelta": 0
            },
            {
              "value": "black",
              "label": "مشکی",
              "swatch": "#252525",
              "priceDelta": 0.15
            }
          ]
        }
      },
      "variantStock": {
        "color=gray": 2,
        "color=black": 4
      },
      "accessories": null,
      "badge": "",
      "slug": "digital-clamp-meter",
      "code": "HN-009",
      "brandId": "victor",
      "categoryId": "clamp-meters",
      "colorIds": [
        "gray",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "برق صنعتی" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "کلمپ‌متر دیجیتال" },
        { "featureId": "meter-format", "value": "کلمپی" },
        { "featureId": "display-type", "value": "دیجیتال" }
      ],
      "detailId": "group-2",
      "mediaId": "product-9"
    },
    {
      "id": 10,
      "companionProductIds": [15, 16, 22, 28, 19, 25],
      "englishName": "Electrical Installation Digital Multimeter",
      "name": "مولتی‌متر دیجیتال تأسیساتی",
      "powerSource": "باتری‌خور",
      "useCase": "تأسیسات برقی",
      "stockQuantity": 9,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 41,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 21,
      "variants": {
        "color": {
          "label": "رنگ",
          "options": [
            {
              "value": "red",
              "label": "قرمز",
              "swatch": "#d62828",
              "priceDelta": 0
            },
            {
              "value": "black",
              "label": "مشکی",
              "swatch": "#252525",
              "priceDelta": 0.25
            }
          ]
        }
      },
      "variantStock": {
        "color=red": 9,
        "color=black": 5
      },
      "accessories": null,
      "badge": "",
      "slug": "electrical-installation-digital-multimeter",
      "code": "HN-010",
      "brandId": "kyoritsu",
      "categoryId": "digital-multimeters",
      "colorIds": [
        "red",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تأسیسات برقی" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "مولتی‌متر دیجیتال" },
        { "featureId": "meter-format", "value": "تأسیساتی" },
        { "featureId": "display-type", "value": "دیجیتال" }
      ],
      "detailId": "group-2",
      "mediaId": "product-10"
    },
    {
      "id": 11,
      "companionProductIds": [6, 22, 18, 29, 10, 21],
      "englishName": "Digital Soldering Station",
      "name": "ایستگاه هویه دیجیتال یوهوا با کنترل دما برای تعمیر برد",
      "powerSource": "برق شهری",
      "useCase": "تعمیر برد",
      "stockQuantity": 12,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 44,
      "discountPercent": 15,
      "imageSet": 2,
      "newest": 20,
      "variants": null,
      "variantStock": null,
      "accessories": [
        {
          "id": "solder-tips",
          "name": "مجموعه نوک هویه",
          "price": 0.85,
          "required": false,
          "description": "نوک‌های جایگزین؛ قطر و سازگاری با دستهٔ هویه را بررسی کنید."
        },
        {
          "id"'+N': "solder-wire",
          "name": "سیم لحیم الکترونیکی",
          "price": 0.38,
          "required": false,
          "description": "سیم لحیم مناسب مونتاژ و تعمیر بردهای الکترونیکی."
        },
        {
          "id": "solder-stand-kit",
          "name": "پایه و اسفنج تمیزکنندهٔ هویه",
          "price": 0.52,
          "required": false,
          "description": "پایهٔ نگهدارنده و اسفنج نظافت نوک؛ اقلام انتخابی این طرح."
        }
      ],
      "badge": "جدید",
      "slug": "digital-soldering-station",
      "code": "HN-011",
      "brandId": "yihua",
      "categoryId": "soldering-stations",
      "colorIds": [
        "blue",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "لحیم‌کاری و تعمیرات الکترونیک" },
        { "featureId": "temperature-control", "value": "کنترل دما و انتخاب نوک متناسب با قطعه" },
        { "featureId": "recommended-accessories", "value": "پایهٔ هویه، نوک یدکی و اسفنج تمیزکننده" },
        { "featureId": "tool-kind", "value": "ایستگاه هویه دیجیتال" }
      ],
      "detailId": "group-3",

      "mediaId": "product-11",
      "includedBenefits": [
        {
          "title": "۶ ماه خدمات فنی",
          "detail": "راهنمایی و بررسی اولیهٔ دستگاه",
          "icon": "bi-tools"
        }
      ]
    },
    {
      "id": 12,
      "companionProductIds": [7, 23, 19, 30, 6, 22],
      "englishName": "Adjustable Soldering Iron",
      "name": "هویه قلمی با قابلیت تنظیم دما",
      "powerSource": "برق شهری",
      "useCase": "مونتاژ الکترونیک",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 47,
      "discountPercent": 0,
      "imageSet": 2,
      "newest": 19,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "adjustable-soldering-iron",
      "code": "HN-012",
      "brandId": "hakko",
      "categoryId": "soldering-irons",
      "colorIds": [
        "blue",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "مونتاژ الکترونیک" },
        { "featureId": "power-source", "value": "برق شهری" },
        { "featureId": "tool-kind", "value": "هویه قلمی" },
        { "featureId": "solder-format", "value": "قلمی با قابلیت تنظیم دما" },
        { "featureId": "temperature-control", "value": "تنظیم دما متناسب با کار" }
      ],
      "detailId": "group-3",
      "mediaId": "product-12"
    },
    {
      "id": 13,
      "companionProductIds": [8, 24, 20, 26, 7, 23],
      "englishName": "Hot Air Rework Station",
      "name": "ایستگاه هیتر هوای گرم تعمیر برد",
      "powerSource": "برق شهری",
      "useCase": "تعمیرات الکترونیک",
      "stockQuantity": 7,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 50,
      "discountPercent": 0,
      "imageSet": 2,
      "newest": 18,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "hot-air-rework-station",
      "code": "HN-013",
      "brandId": "quick",
      "categoryId": "hot-air-rework-stations",
      "colorIds": [
        "gray",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تعمیرات الکترونیک" },
        { "featureId": "power-source", "value": "برق شهری" },
        { "featureId": "tool-kind", "value": "ایستگاه هیتر هوای گرم" },'+N'
        { "featureId": "heating-method", "value": "هوای گرم" }
      ],
      "detailId": "group-3",
      "mediaId": "product-13"
    },
    {
      "id": 14,
      "companionProductIds": [9, 25, 16, 27, 8, 24],
      "englishName": "Bench Soldering Station",
      "name": "ایستگاه لحیم‌کاری رومیزی",
      "powerSource": "برق شهری",
      "useCase": "مونتاژ و تعمیر",
      "stockQuantity": 3,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 53,
      "discountPercent": 0,
      "imageSet": 2,
      "newest": 17,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "bench-soldering-station",
      "code": "HN-014",
      "brandId": "weller",
      "categoryId": "soldering-stations",
      "colorIds": [
        "white",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "مونتاژ و تعمیر" },
        { "featureId": "power-source", "value": "برق شهری" },
        { "featureId": "tool-kind", "value": "ایستگاه لحیم‌کاری" },
        { "featureId": "solder-format", "value": "رومیزی" },
        { "featureId": "recommended-accessories", "value": "پایهٔ هویه، نوک یدکی و اسفنج تمیزکننده" }
      ],
      "detailId": "group-3",
      "mediaId": "product-14"
    },
    {
      "id": 15,
      "companionProductIds": [10, 21, 17, 28, 9, 25],
      "englishName": "Lightweight Workshop Soldering Iron",
      "name": "هویه قلمی سبک",
      "powerSource": "برق شهری",
      "useCase": "تعمیر برد",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 56,
      "discountPercent": 10,
      "imageSet": 2,
      "newest": 16,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "پرفروش",
      "slug": "lightweight-workshop-soldering-iron",
      "code": "HN-015",
      "brandId": "goot",
      "categoryId": "soldering-irons",
      "colorIds": [
        "red",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تعمیر برد" },
        { "featureId": "power-source", "value": "برق شهری" },
        { "featureId": "tool-kind", "value": "هویه قلمی" },
        { "featureId": "solder-format", "value": "قلمی سبک" },
        { "featureId": "temperature-control", "value": "انتخاب نوک متناسب با قطعه" }
      ],
      "detailId": "group-3",
      "mediaId": "product-15"
    },
    {
      "id": 16,
      "companionProductIds": [1, 22, 8, 29, 25, 2],
      "englishName": "Portable LED Work Light",
      "name": "چراغ کار LED شارژی قابل‌حمل فیلیپس برای بازرسی و تعمیر",
      "powerSource": "باتری شارژی",
      "useCase": "بازرسی و تعمیر",
      "stockQuantity": 18,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 14,
      "discountPercent": 15,
      "imageSet": 3,
      "newest": 15,
      "variants": {
        "temperature": {
          "label": "دمای رنگ نور",
          "options": [
            {
              "value": "4000k",
              "label": "۴۰۰۰ کلوین · طبیعی",
              "priceDelta": 0
            },
            {
              "value": "6500k",
              "label": "۶۵۰۰ کلوین · سفید",
              "priceDelta": 0.15
            }
          ]
        },
        "brightness": {
          "label": "شار نوری",
          "options": [
            {
              "value": "1000lm",'+N'
              "label": "۱۰۰۰ لومن",
              "priceDelta": 0
            },
            {
              "value": "2000lm",
              "label": "۲۰۰۰ لومن",
              "priceDelta": 0.45
            }
          ]
        }
      },
      "variantStock": null,
      "accessories": [
        {
          "id": "worklight-tripod",
          "name": "پایهٔ سه‌پایه چراغ کار",
          "price": 1.15,
          "required": false,
          "description": "برای قرارگیری چراغ در ارتفاع؛ نوع اتصال پایه را بررسی کنید."
        },
        {
          "id": "worklight-battery",
          "name": "باتری یدک چراغ کار",
          "price": 1.35,
          "required": false,
          "description": "باتری جایگزین؛ ولتاژ و سازگاری با مدل چراغ را بررسی کنید."
        }
      ],
      "badge": "جدید",
      "slug": "portable-led-work-light",
      "code": "HN-016",
      "brandId": "philips",
      "categoryId": "inspection-lights",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "light-source", "value": "LED" },
        { "featureId": "usage", "value": "روشنایی کارگاهی و تعمیراتی" },
        { "featureId": "power-source", "value": "باتری شارژی" },
        { "featureId": "selection-note", "value": "بررسی شار نوری و درجهٔ حفاظت" }
      ],
      "detailId": "group-4",

      "mediaId": "product-16",
      "includedBenefits": [
        {
          "title": "آزمون روشنایی پیش از ارسال",
          "detail": "کنترل روشن‌شدن و حالت‌های نور",
          "icon": "bi-lightbulb"
        }
      ]
    },
    {
      "id": 17,
      "companionProductIds": [2, 23, 9, 30, 21, 3],
      "englishName": "Workshop LED Floodlight",
      "name": "پروژکتور LED کارگاهی",
      "powerSource": "برق شهری",
      "useCase": "کارگاه و نصب",
      "stockQuantity": 5,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 17,
      "discountPercent": 0,
      "imageSet": 3,
      "newest": 14,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "workshop-led-floodlight",
      "code": "HN-017",
      "brandId": "arva",
      "categoryId": "work-floodlights",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "light-source", "value": "LED" },
        { "featureId": "usage", "value": "کارگاه و نصب" },
        { "featureId": "power-source", "value": "برق شهری" },
        { "featureId": "selection-note", "value": "بررسی شار نوری و درجهٔ حفاظت" }
      ],
      "detailId": "group-4",

      "mediaId": "product-17"
    },
    {
      "id": 18,
      "companionProductIds": [3, 24, 10, 26, 22, 4],
      "englishName": "Rechargeable Inspection Light",
      "name": "چراغ بازرسی شارژی",
      "powerSource": "باتری شارژی",
      "useCase": "تعمیر خودرو",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 20,
      "discountPercent": 0,
      "imageSet": 3,
      "newest": 13,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "rechargeable-inspection-light",
      "code": "HN-018",
      "brandId": "ronix",
      "categoryId": "inspection-lights",
      "colorIds": [
        "orange",
        "black"
      ],

      "specificationValues": [
        { "featureI'+N'd": "usage", "value": "تعمیر خودرو" },
        { "featureId": "power-source", "value": "باتری شارژی" },
        { "featureId": "light-source", "value": "LED" },
        { "featureId": "lighting-format", "value": "چراغ بازرسی شارژی" },
        { "featureId": "lighting-task", "value": "تعمیر خودرو" },
        { "featureId": "selection-note", "value": "بررسی زاویهٔ تابش و درجهٔ حفاظت" }
      ],
      "detailId": "group-4",
      "mediaId": "product-18"
    },
    {
      "id": 19,
      "companionProductIds": [4, 25, 6, 27, 23, 5],
      "englishName": "LED Work Light with Stand",
      "name": "چراغ کار LED پایه‌دار",
      "powerSource": "باتری شارژی",
      "useCase": "نصب و کارگاه",
      "stockQuantity": 2,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 23,
      "discountPercent": 0,
      "imageSet": 3,
      "newest": 12,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "led-work-light-with-stand",
      "code": "HN-019",
      "brandId": "makita",
      "categoryId": "work-floodlights",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "نصب و کارگاه" },
        { "featureId": "power-source", "value": "باتری شارژی" },
        { "featureId": "light-source", "value": "LED" },
        { "featureId": "lighting-format", "value": "چراغ پایه‌دار" },
        { "featureId": "lighting-task", "value": "نصب و کارگاه" },
        { "featureId": "selection-note", "value": "بررسی شار نوری و پایداری پایه" }
      ],
      "detailId": "group-4",
      "mediaId": "product-19"
    },
    {
      "id": 20,
      "companionProductIds": [5, 21, 7, 28, 24, 1],
      "englishName": "Portable LED Floodlight",
      "name": "نورافکن LED قابل‌حمل",
      "powerSource": "باتری شارژی",
      "useCase": "محیط کارگاهی",
      "stockQuantity": 9,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 26,
      "discountPercent": 0,
      "imageSet": 3,
      "newest": 11,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "portable-led-floodlight",
      "code": "HN-020",
      "brandId": "dewalt",
      "categoryId": "work-floodlights",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "محیط کارگاهی" },
        { "featureId": "power-source", "value": "باتری شارژی" },
        { "featureId": "light-source", "value": "LED" },
        { "featureId": "lighting-format", "value": "نورافکن قابل‌حمل" },
        { "featureId": "lighting-task", "value": "محیط کارگاهی" },
        { "featureId": "selection-note", "value": "بررسی شار نوری و درجهٔ حفاظت" }
      ],
      "detailId": "group-4",
      "mediaId": "product-20"
    },
    {
      "id": 21,
      "companionProductIds": [1, 7, 18, 29, 20, 6],
      "englishName": "Insulated Electrician Screwdriver Set",
      "name": "ست پیچ‌گوشتی عایق ویها برای نصب و برق‌کاری",
      "powerSource": "دستی",
      "useCase": "نصب و برق‌کاری",
      "stockQuantity": 12,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 29,
      "discountPercent": 15,
      "imageSet": 1,
      "newest": 10,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "جدید",
   '+N'   "slug": "insulated-electrician-screwdriver-set",
      "code": "HN-021",
      "brandId": "wiha",
      "categoryId": "electrician-hand-tools",
      "colorIds": [
        "red",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "نصب و برق‌کاری" },
        { "featureId": "power-source", "value": "دستی" },
        { "featureId": "tool-kind", "value": "ابزار دستی برق‌کاری" },
        { "featureId": "hand-tool-form", "value": "ست پیچ‌گوشتی عایق" },
        { "featureId": "safety-note", "value": "ابزار عایق جایگزین قطع برق و دستورالعمل ایمنی نیست" }
      ],
      "detailId": "group-5",
      "mediaId": "product-21",
      "includedBenefits": [
        {
          "title": "بررسی عایق‌بودن ابزار",
          "detail": "بازبینی اولیه پیش از تحویل",
          "icon": "bi-shield-check"
        }
      ]
    },
    {
      "id": 22,
      "companionProductIds": [2, 8, 19, 30, 16, 7],
      "englishName": "Electrician Pliers Set",
      "name": "ست انبر برق‌کاری",
      "powerSource": "دستی",
      "useCase": "نصب و تعمیر",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 32,
      "discountPercent": 10,
      "imageSet": 1,
      "newest": 9,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "پرفروش",
      "slug": "electrician-pliers-set",
      "code": "HN-022",
      "brandId": "knipex",
      "categoryId": "electrician-hand-tools",
      "colorIds": [
        "red",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "نصب و تعمیر" },
        { "featureId": "power-source", "value": "دستی" },
        { "featureId": "tool-kind", "value": "ابزار دستی برق‌کاری" },
        { "featureId": "hand-tool-form", "value": "ست انبر برق‌کاری" }
      ],
      "detailId": "group-5",
      "mediaId": "product-22",
      "includedBenefits": [
        {
          "title": "بازبینی مفصل‌ها و فک انبر",
          "detail": "کنترل اولیهٔ سلامت ابزار",
          "icon": "bi-wrench"
        }
      ]
    },
    {
      "id": 23,
      "companionProductIds": [3, 9, 20, 26, 17, 8],
      "englishName": "Insulated Electrician Tool Kit",
      "name": "مجموعه ابزار عایق برق‌کار استنلی برای تعمیر و نگهداری",
      "powerSource": "دستی",
      "useCase": "تعمیر و نگهداری",
      "stockQuantity": 7,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 35,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 8,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "insulated-electrician-tool-kit",
      "code": "HN-023",
      "brandId": "stanley",
      "categoryId": "electrician-tool-kits",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تعمیر و نگهداری" },
        { "featureId": "power-source", "value": "دستی" },
        { "featureId": "tool-kind", "value": "مجموعه ابزار برق‌کار" },
        { "featureId": "hand-tool-form", "value": "مجموعه ابزار عایق" },
        { "featureId": "set-content", "value": "ترکیب ابزار را از اقلام همین مدل بررسی کنید" },
        { "featureId": "safety-note", "value": "ابزار عایق جایگزین قطع برق و دستورالعمل ایمنی نیست" }
      ],
      "detailId": "group-5",
      "mediaId": "product-23"
'+N'    },
    {
      "id": 24,
      "companionProductIds": [4, 10, 16, 27, 18, 9],
      "englishName": "Wiring and Installation Tool Set",
      "name": "ست ابزار نصب و سیم‌کشی",
      "powerSource": "دستی",
      "useCase": "سیم‌کشی و نصب",
      "stockQuantity": 3,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 38,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 7,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "wiring-and-installation-tool-set",
      "code": "HN-024",
      "brandId": "tosan",
      "categoryId": "electrician-tool-kits",
      "colorIds": [
        "orange",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "سیم‌کشی و نصب" },
        { "featureId": "power-source", "value": "دستی" },
        { "featureId": "tool-kind", "value": "ست ابزار برق‌کاری" },
        { "featureId": "hand-tool-form", "value": "ابزار نصب و سیم‌کشی" },
        { "featureId": "set-content", "value": "ترکیب اقلام را با نیاز نصب و سیم‌کشی تطبیق دهید" }
      ],
      "detailId": "group-5",
      "mediaId": "product-24"
    },
    {
      "id": 25,
      "companionProductIds": [5, 6, 17, 28, 19, 10],
      "englishName": "Electrician Tool Bag Set",
      "name": "کیف ابزار برق‌کاری",
      "powerSource": "دستی",
      "useCase": "کارگاه و سرویس",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 41,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 6,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "electrician-tool-bag-set",
      "code": "HN-025",
      "brandId": "ronix",
      "categoryId": "electrician-tool-kits",
      "colorIds": [
        "black",
        "gray"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "کارگاه و سرویس" },
        { "featureId": "power-source", "value": "دستی" },
        { "featureId": "tool-kind", "value": "کیف ابزار برق‌کاری" },
        { "featureId": "hand-tool-form", "value": "کیف نگهداری و حمل ابزار" },
        { "featureId": "set-content", "value": "اقلام داخل کیف را از فهرست همین مدل بررسی کنید" }
      ],
      "detailId": "group-5",
      "mediaId": "product-25"
    },
    {
      "id": 26,
      "companionProductIds": [6, 22, 18, 4, 25, 16],
      "englishName": "Digital Voltage Tester",
      "name": "فازمتر دیجیتال",
      "powerSource": "باتری‌خور",
      "useCase": "تست اولیه برق",
      "stockQuantity": 18,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 44,
      "discountPercent": 15,
      "imageSet": 1,
      "newest": 5,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "جدید",
      "slug": "digital-voltage-tester",
      "code": "HN-026",
      "brandId": "uni-t",
      "categoryId": "electrical-testers",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تست اولیه برق" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "فازمتر دیجیتال" },
        { "featureId": "tester-form", "value": "دیجیتال" },
        { "featureId": "tester-method", "value": "بررسی اولیهٔ برق" }
      ],
      "d'+N'etailId": "group-6",
      "mediaId": "product-26"
    },
    {
      "id": 27,
      "companionProductIds": [7, 23, 19, 5, 21, 17],
      "englishName": "Non-Contact Voltage Tester",
      "name": "تستر ولتاژ غیرتماسی",
      "powerSource": "باتری‌خور",
      "useCase": "ایمنی و بازرسی",
      "stockQuantity": 5,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 47,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 4,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "non-contact-voltage-tester",
      "code": "HN-027",
      "brandId": "fluke",
      "categoryId": "voltage-testers",
      "colorIds": [
        "yellow",
        "red"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "ایمنی و بازرسی" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "تستر ولتاژ" },
        { "featureId": "tester-form", "value": "غیرتماسی" },
        { "featureId": "tester-method", "value": "تشخیص ولتاژ بدون تماس مستقیم" }
      ],
      "detailId": "group-6",
      "mediaId": "product-27"
    },
    {
      "id": 28,
      "companionProductIds": [8, 24, 20, 1, 22, 18],
      "englishName": "Inductive Voltage Tester",
      "name": "فازمتر القایی",
      "powerSource": "باتری‌خور",
      "useCase": "تأسیسات برقی",
      "stockQuantity": 0,
      "maxCartQuantity": 99,
      "inStock": false,
      "price": 50,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 3,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "inductive-voltage-tester",
      "code": "HN-028",
      "brandId": "mastech",
      "categoryId": "phase-testers",
      "colorIds": [
        "yellow",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تأسیسات برقی" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "فازمتر" },
        { "featureId": "tester-form", "value": "القایی" },
        { "featureId": "tester-method", "value": "بررسی القایی مدار" }
      ],
      "detailId": "group-6",
      "mediaId": "product-28"
    },
    {
      "id": 29,
      "companionProductIds": [9, 25, 16, 2, 23, 19],
      "englishName": "Circuit Continuity Tester",
      "name": "تستر پیوستگی مدار",
      "powerSource": "باتری‌خور",
      "useCase": "عیب‌یابی برق",
      "stockQuantity": 2,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 53,
      "discountPercent": 10,
      "imageSet": 1,
      "newest": 2,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "پرفروش",
      "slug": "circuit-continuity-tester",
      "code": "HN-029",
      "brandId": "benning",
      "categoryId": "circuit-testers",
      "colorIds": [
        "green",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "عیب‌یابی برق" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "تستر مدار" },
        { "featureId": "tester-form", "value": "تستر پیوستگی" },
        { "featureId": "tester-method", "value": "بررسی پیوستگی مدار" }
      ],
      "detailId": "group-6",
      "mediaId": "product-29'+N'"
    },
    {
      "id": 30,
      "companionProductIds": [10, 21, 17, 3, 24, 20],
      "englishName": "Voltage Detector Pen",
      "name": "قلم تست ولتاژ",
      "powerSource": "باتری‌خور",
      "useCase": "تست و سرویس",
      "stockQuantity": 9,
      "maxCartQuantity": 99,
      "inStock": true,
      "price": 56,
      "discountPercent": 0,
      "imageSet": 1,
      "newest": 1,
      "variants": null,
      "variantStock": null,
      "accessories": null,
      "badge": "",
      "slug": "voltage-detector-pen",
      "code": "HN-030",
      "brandId": "uni-t",
      "categoryId": "voltage-testers",
      "colorIds": [
        "white",
        "black"
      ],

      "specificationValues": [
        { "featureId": "usage", "value": "تست و سرویس" },
        { "featureId": "power-source", "value": "باتری‌خور" },
        { "featureId": "tool-kind", "value": "قلم تست ولتاژ" },
        { "featureId": "tester-form", "value": "قلمی" },
        { "featureId": "tester-method", "value": "تست ولتاژ" }
      ],
      "detailId": "group-6",
      "mediaId": "product-30"
    }
  ],
  "collections": {
    "selected": [
      6,
      1,
      11,
      16,
      2,
      3,
      4,
      7,
      12,
      17
    ],
    "bestsellers": [
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9,
      10,
      11,
      12,
      13,
      14,
      15,
      16,
      17,
      18,
      19,
      20,
      21,
      22,
      23,
      24
    ],
    "newest": [
      21,
      22,
      23,
      24,
      25,
      26
    ]
  }
}
')),
        (N'brandEditorial',CONVERT(NVARCHAR(MAX),CAST(N'' AS NVARCHAR(MAX))+N'[
  {
    "slug": "bosch",
    "name": "Bosch",
    "persianName": "بوش",
    "established": "۱۸۸۶",
    "country": "آلمان",
    "officialUrl": "https://www.bosch-professional.com/",
    "companyIntroductions": [
      "بوش یکی از بزرگ‌ترین شرکت‌های مهندسی و فناوری جهان است که در حوزه ابزارهای برقی حرفه‌ای، ابزارهای اندازه‌گیری و متعلقات صنعتی فعالیت می‌کند. بخش حرفه‌ای بوش (Bosch Professional) ابزارهای برقی آبی‌رنگ را برای کاربران فنی، کارگاه‌ها و پروژه‌های ساختمانی توسعه می‌دهد. سازنده اعلام می‌کند که توسعه محصولات این بخش بر اساس بازخورد مستقیم کاربران حرفه‌ای انجام می‌شود و تمرکز اصلی بر توان، ایمنی، دوام و سازگاری متعلقات است.",
      "بوش یکی از بزرگ‌ترین گروه‌های فناوری و مهندسی جهان است که در سال ۱۸۸۶ با یک کارگاه مکانیک دقیق و مهندسی برق در اشتوتگارت آغاز به کار کرد. بخش ابزار برقی بوش (Bosch Power Tools) در زیرمجموعه «کالاهای مصرفی» فعالیت می‌کند و با دو خط محصول شناخته می‌شود: خط آبی (حرفه‌ای) برای کارگاه‌ها و پیمانکاران، و خط سبز برای کاربران خانگی و علاقه‌مندان به کارهای ساختمانی."
    ],
    "specialization": "ابزارهای برقی (دریل چکشی، دریل شارژی، فرز، اره)، ابزارهای اندازه‌گیری لیزری، متعلقات و لوازم جانبی. محصولات بوش در رده قیمتی بالا قرار می‌گیرند و کیفیت ساخت آن‌ها در سطح بسیار بالا ارزیابی می‌شود .",
    "featuredProducts": "دریل چکشی، دریل شارژی، ابزارهای نصب و ابزارهای اندازه‌گیری.",
    "productFamilies": [
      "دریل، دریل چکشی و بتن‌کن (هیلتی/روتاری)",
      "فرز، اره‌های پرتابل، سنگ‌ها و ابزار نجاری",
      "پلتفرم باتری ۱۲ ولت و ۱۸ ولت با شارژرهای سریع",
      "متر لیزری، ترازهای لیزری و دوربین‌های حرارتی ساختمانی",
      "لوازم مصرفی (مته، تیغه، دیسک) با کیفیت شناخته‌شده"
    ],
    "strengths": [
      "کیفیت ساخت و استانداردهای آلمانی",
      "شبکه گسترده خدمات پس از فروش جهانی",
      "تنوع بسیار زیاد محصول برای هر سطح کاربری"
    ],
    "audience": "پیمانکاران، صنعتگران حرفه‌ای، کارگاه‌ها و کاربران خانگی که به دنبال ابزار با دوام هستند.",
    "shoppingAdvice": "نوع کاربری و قدرت موردنیاز را با پروژه تطبیق دهید. ظرفیت باتری و لوازم همراه مدل را بررسی کنید. سازگاری سه‌نظام و مته را پیش از خرید بسنجید.",
    "fullPastedText": "۱. بوش — Bosch\nحوزه فعالیت: ابزارهای برقی حرفه‌ای، ابزارهای اندازه‌گیری، متعلقات صنعتی\nسال تأسیس: ۱۸۸۶\nکشور مبدأ: آلمان\nوب‌سایت رسمی: https://www.bosch-professional.com/\n\nمعرفی شرکت\nبوش یکی از بزرگ‌ترین شرکت‌های مهندسی و فناوری جهان است که در حوزه ابزارهای برقی حرفه‌ای، ابزارهای اندازه‌گیری و متعلقات صنعتی فعالیت می‌کند. بخش حرفه‌ای بوش (Bosch Professional) ابزارهای برقی آبی‌رنگ را برای کاربران فنی، کارگاه‌ها و پروژه‌های ساختمانی توسعه می‌دهد. سازنده اعلام می‌کند که توسعه محصولات این بخش بر اساس بازخورد مستقیم کاربران حرفه‌ای انجام می‌شود و تمرکز اصلی بر توان، ایمنی، دوام و سازگاری متعلقات است.\n\nحوزه تخصصی\nابزارهای برقی (دریل چکشی، دریل شارژی، فرز، اره)، ابزارهای اندازه‌گیری لیزری، متعلقات و لوازم جانبی. محصولات بوش در رده قیمتی بالا قرار می‌گیرند و کیفیت ساخت آن‌ها در سطح بسیار بالا ارزیابی می‌شود .\n\nمحصولات شاخص\nدریل چکشی، دریل شارژی، ابزارهای نصب و ابزارهای اندازه‌گیری.\n\nنکات خرید\nنوع کاربری و قدرت موردنیاز را با پروژه تطبیق دهید. ظرفیت باتری و لوازم همراه مدل را بررسی کنید. سازگاری سه‌نظام و مته را پیش از خرید بسنجید.",
    "fullDocumentText": "بوش (Bosch)\nنام رسمی شرکت | Robert Bosch GmbH\nسال تأسیس | ۱۸۸۶ میلادی؛ توسط رابرت بوش\nمقر / کشور | گرلینگن (نزدیک اشتوتگارت)، آلمان\nمالکیت | عمدتاً در مالکیت بنیاد رابرت بوش (Robert Bosch Stiftung)؛ شرکت سهامی عام نیست\nحوزه فعالیت | فناوری خودرو، فناوری صنعتی، کالاهای مصرفی، انرژی و فناوری س'+N'اختمان\nمحصولات شاخص | ابزار برقی و شارژی (Bosch Professional)، ابزار اندازه‌گیری لیزری، لوازم جانبی، ابزار باغبانی\nمعرفی\nبوش یکی از بزرگ‌ترین گروه‌های فناوری و مهندسی جهان است که در سال ۱۸۸۶ با یک کارگاه مکانیک دقیق و مهندسی برق در اشتوتگارت آغاز به کار کرد. بخش ابزار برقی بوش (Bosch Power Tools) در زیرمجموعه «کالاهای مصرفی» فعالیت می‌کند و با دو خط محصول شناخته می‌شود: خط آبی (حرفه‌ای) برای کارگاه‌ها و پیمانکاران، و خط سبز برای کاربران خانگی و علاقه‌مندان به کارهای ساختمانی.\nحوزه‌های فعالیت و محصولات\nدریل، دریل چکشی و بتن‌کن (هیلتی/روتاری)\nفرز، اره‌های پرتابل، سنگ‌ها و ابزار نجاری\nپلتفرم باتری ۱۲ ولت و ۱۸ ولت با شارژرهای سریع\nمتر لیزری، ترازهای لیزری و دوربین‌های حرارتی ساختمانی\nلوازم مصرفی (مته، تیغه، دیسک) با کیفیت شناخته‌شده\nنقاط قوت\nکیفیت ساخت و استانداردهای آلمانی\nشبکه گسترده خدمات پس از فروش جهانی\nتنوع بسیار زیاد محصول برای هر سطح کاربری\nمخاطبان اصلی\nپیمانکاران، صنعتگران حرفه‌ای، کارگاه‌ها و کاربران خانگی که به دنبال ابزار با دوام هستند."
  },
  {
    "slug": "makita",
    "name": "Makita",
    "persianName": "ماکیتا",
    "established": "۱۹۱۵",
    "country": "ژاپن",
    "officialUrl": "https://www.makita.biz/company/about/data/",
    "companyIntroductions": [
      "ماکیتا فعالیت خود را در سال ۱۹۱۵ با فروش و تعمیر موتورهای الکتریکی در ناگویا، ژاپن آغاز کرد. این شرکت به‌تدریج از یک کسب‌وکار خدماتی به یکی از بزرگ‌ترین تولیدکنندگان ابزارهای برقی در جهان تبدیل شد. امروزه ماکیتا طیف گسترده‌ای از ابزارهای برقی، شارژی، باغبانی و پنوماتیک را برای کاربران حرفه‌ای و نیمه‌حرفه‌ای عرضه می‌کند.",
      "ماکیتا یکی از پیشگامان ابزار برقی در ژاپن است. این شرکت در دهه ۱۹۵۰ تولید رنده برقی را آغاز کرد و در دهه‌های بعد به یکی از بازیگران اصلی ابزارهای شارژی تبدیل شد. تمرکز ماکیتا بر ساخت ابزار سبک، قابل‌اعتماد و با پلتفرم باتری مشترک است و رنگ آبی‌فیروزه‌ای آن در کارگاه‌ها به‌سادگی شناخته می‌شود."
    ],
    "specialization": "ابزارهای برقی و شارژی برای نجاری، صنعت و کارگاه. ماکیتا به‌ویژه در زمینه ابزارهای شارژی با سامانه باتری یکپارچه شناخته می‌شود. در بازار ایران، کیفیت ساخت این برند «بسیار بالا» ارزیابی می‌شود و از نظر تنوع محصولات در رده بالایی قرار دارد .",
    "featuredProducts": "دریل و پیچ‌گوشتی برقی و شارژی، اره، فرز و ابزارهای باغبانی.",
    "productFamilies": [
      "ابزار شارژی با پلتفرم‌های ۱۸ ولت LXT و ۴۰ ولت XGT",
      "دریل‌ها، پیچ‌گوشتی‌های ضربه‌ای و ابزار ساختمانی",
      "اره‌های دیسکی، فرز، رنده و ابزار نجاری",
      "ابزار باغبانی: چمن‌زن، زنجیرارّه، علف‌تراش، دمنده",
      "جاروبرقی شارژی و تجهیزات کارگاهی"
    ],
    "strengths": [
      "پیشرو در ابزار شارژی لیتیومی و باتری‌های همه‌کاره",
      "وزن کم و ارگونومی مناسب برای کار طولانی",
      "طیف کامل خط شارژی و فضای باز"
    ],
    "audience": "نجاران، برق‌کاران، نصابان، اپراتورهای ساختمانی و باغبانان حرفه‌ای.",
    "shoppingAdvice": "برای کارهای تکراری، وزن و ارگونومی ابزار را در نظر بگیرید. مدل شارژی را بر اساس ولتاژ و ظرفیت باتری انتخاب کنید. اقلام همراه و نوع سه‌نظام را با نیازتان تطبیق دهید.",
    "fullPastedText": "۲. ماکیتا — Makita\nحوزه فعالیت: ابزارهای برقی، ابزارهای باغبانی، پنوماتیک و تجهیزات خانگی\nسال تأسیس: ۱۹۱۵\nکشور مبدأ: ژاپن\nوب‌سایت رسمی: https://www.makita.biz/company/about/data/\n\nمعرفی شرکت\nماکیتا فعالیت خود را در سال ۱۹۱۵ با فروش و تعمیر موتورهای الکتریکی در ناگویا، ژاپن آغاز کرد. این شرکت به‌تدریج از یک کسب‌وکار خدماتی به یکی از بزرگ‌ترین تولیدکنندگان ابزارهای برقی در جهان تبدیل شد. امروزه ماکیتا طیف گسترده‌ای از ابزارهای برقی، شارژی، باغبانی و پنوماتیک را برای کاربران حرفه‌ای و نیمه‌حرفه‌ای عرضه '+N'می‌کند.\n\nحوزه تخصصی\nابزارهای برقی و شارژی برای نجاری، صنعت و کارگاه. ماکیتا به‌ویژه در زمینه ابزارهای شارژی با سامانه باتری یکپارچه شناخته می‌شود. در بازار ایران، کیفیت ساخت این برند «بسیار بالا» ارزیابی می‌شود و از نظر تنوع محصولات در رده بالایی قرار دارد .\n\nمحصولات شاخص\nدریل و پیچ‌گوشتی برقی و شارژی، اره، فرز و ابزارهای باغبانی.\n\nنکات خرید\nبرای کارهای تکراری، وزن و ارگونومی ابزار را در نظر بگیرید. مدل شارژی را بر اساس ولتاژ و ظرفیت باتری انتخاب کنید. اقلام همراه و نوع سه‌نظام را با نیازتان تطبیق دهید.",
    "fullDocumentText": "ماکیتا (Makita)\nنام رسمی شرکت | Makita Corporation (株式会社マキタ)\nسال تأسیس | ۱۹۱۵ میلادی؛ توسط موساپورو ماکیتا (آغاز با تعمیر موتور و ترانس)\nمقر / کشور | آنجو، استان آیچی، ژاپن\nمالکیت | شرکت سهامی عام؛ فهرست‌شده در بورس توکیو\nحوزه فعالیت | ابزار برقی و شارژی، ابزار بادی، تجهیزات فضای باز و باغبانی\nمحصولات شاخص | سری LXT (۱۸ ولت)، سری XGT (۴۰ ولت Max)، دریل شارژی، فرز و اره‌های شارژی\nمعرفی\nماکیتا یکی از پیشگامان ابزار برقی در ژاپن است. این شرکت در دهه ۱۹۵۰ تولید رنده برقی را آغاز کرد و در دهه‌های بعد به یکی از بازیگران اصلی ابزارهای شارژی تبدیل شد. تمرکز ماکیتا بر ساخت ابزار سبک، قابل‌اعتماد و با پلتفرم باتری مشترک است و رنگ آبی‌فیروزه‌ای آن در کارگاه‌ها به‌سادگی شناخته می‌شود.\nحوزه‌های فعالیت و محصولات\nابزار شارژی با پلتفرم‌های ۱۸ ولت LXT و ۴۰ ولت XGT\nدریل‌ها، پیچ‌گوشتی‌های ضربه‌ای و ابزار ساختمانی\nاره‌های دیسکی، فرز، رنده و ابزار نجاری\nابزار باغبانی: چمن‌زن، زنجیرارّه، علف‌تراش، دمنده\nجاروبرقی شارژی و تجهیزات کارگاهی\nنقاط قوت\nپیشرو در ابزار شارژی لیتیومی و باتری‌های همه‌کاره\nوزن کم و ارگونومی مناسب برای کار طولانی\nطیف کامل خط شارژی و فضای باز\nمخاطبان اصلی\nنجاران، برق‌کاران، نصابان، اپراتورهای ساختمانی و باغبانان حرفه‌ای."
  },
  {
    "slug": "dewalt",
    "name": "DeWalt",
    "persianName": "دیوالت",
    "established": "۱۹۲۴",
    "country": "آمریکا",
    "officialUrl": "https://www.dewalt.com/en-us/resources/dewalt-history",
    "companyIntroductions": [
      "ریشه دیوالت به اختراع اره بازویی شعاعی و تأسیس شرکت DEWALT Products در سال ۱۹۲۴ بازمی‌گردد. این برند در ابتدا بر ابزارهای کارگاهی متمرکز بود و به‌تدریج به یکی از شناخته‌شده‌ترین برندهای ابزار برقی صنعتی در جهان تبدیل شد. امروزه دیوالت مجموعه‌ای از ابزارهای برقی و شارژی را برای کاربران حرفه‌ای در کارگاه و محل پروژه عرضه می‌کند.",
      "دیوالت برند آمریکایی ابزار برقی حرفه‌ای است که با رنگ زرد و مشکی شناخته می‌شود. ریموند دیوالت در دهه ۱۹۲۰ اولین اره شعاعی برقی را ساخت و برند او بعدها به Black & Decker پیوست. امروز دیوالت قلب بخش ابزار برقی گروه Stanley Black & Decker است و تمرکز اصلی آن بر کارگاه‌ها و کارگران ساختمانی است."
    ],
    "specialization": "ابزارهای برقی و شارژی صنعتی. در بازار ایران، کیفیت ساخت دیوالت «عالی و مستحکم» توصیف می‌شود و در رده قیمتی بسیار بالا قرار می‌گیرد .",
    "featuredProducts": "دریل چکشی، ابزار کارگاهی و لوازم جانبی.",
    "productFamilies": [
      "پلتفرم باتری ۲۰ ولت MAX و باتری‌های FLEXVOLT چندولتاژه",
      "دریل‌ها، پیچ‌گوشتی‌های ضربه‌ای و چکش‌های دوار",
      "اره‌های مدور، افقی و میزی",
      "فرزها، سنباده‌ها و ابزار برش",
      "جعبه‌ابزار ماژولار TOUGHSYSTEM و ذخیره‌سازی"
    ],
    "strengths": [
      "استحکام بالا برای کار سخت و محیط‌های ساختمانی",
      "پلتفرم باتری گسترده و سازگار",
      "حضور قوی در بازار آمریکا و اروپا"
    ],
    "audience": "پیمانکاران ساختمانی، برق‌کاران، سازندگان و صنعتگران حرفه‌ای.",
    "shoppingAdvice": "توان ابزار را متناسب با جنس سطح و شدت استفاده انتخاب کنید. برای کار طولانی، به وزن و کنترل دستگاه توجه کنید. نوع برق‌رس'+N'انی و اقلام همراه را بررسی کنید.",
    "fullPastedText": "۳. دیوالت — DeWalt\nحوزه فعالیت: ابزارهای برقی و شارژی صنعتی، راهکارهای کارگاهی و محل پروژه\nسال تأسیس: ۱۹۲۴\nکشور مبدأ: آمریکا\nوب‌سایت رسمی: https://www.dewalt.com/en-us/resources/dewalt-history\n\nمعرفی شرکت\nریشه دیوالت به اختراع اره بازویی شعاعی و تأسیس شرکت DEWALT Products در سال ۱۹۲۴ بازمی‌گردد. این برند در ابتدا بر ابزارهای کارگاهی متمرکز بود و به‌تدریج به یکی از شناخته‌شده‌ترین برندهای ابزار برقی صنعتی در جهان تبدیل شد. امروزه دیوالت مجموعه‌ای از ابزارهای برقی و شارژی را برای کاربران حرفه‌ای در کارگاه و محل پروژه عرضه می‌کند.\n\nحوزه تخصصی\nابزارهای برقی و شارژی صنعتی. در بازار ایران، کیفیت ساخت دیوالت «عالی و مستحکم» توصیف می‌شود و در رده قیمتی بسیار بالا قرار می‌گیرد .\n\nمحصولات شاخص\nدریل چکشی، ابزار کارگاهی و لوازم جانبی.\n\nنکات خرید\nتوان ابزار را متناسب با جنس سطح و شدت استفاده انتخاب کنید. برای کار طولانی، به وزن و کنترل دستگاه توجه کنید. نوع برق‌رسانی و اقلام همراه را بررسی کنید.",
    "fullDocumentText": "دیوالت (DeWalt)\nنام رسمی شرکت | DEWALT (زیرمجموعه Stanley Black & Decker)\nسال تأسیس | ۱۹۲۳ میلادی؛ توسط ریموند دیوالت (اختراع اره شعاعی)\nمقر / کشور | ایالات متحده آمریکا\nمالکیت | از ۱۹۶۰ در مالکیت Black & Decker؛ از ۲۰۱۰ بخشی از Stanley Black & Decker\nحوزه فعالیت | ابزار برقی و شارژی حرفه‌ای، لوازم جانبی، ذخیره‌سازی ابزار\nمحصولات شاخص | پلتفرم ۲۰ ولت MAX، سری FLEXVOLT، دریل‌ها و اره‌های شارژی\nمعرفی\nدیوالت برند آمریکایی ابزار برقی حرفه‌ای است که با رنگ زرد و مشکی شناخته می‌شود. ریموند دیوالت در دهه ۱۹۲۰ اولین اره شعاعی برقی را ساخت و برند او بعدها به Black & Decker پیوست. امروز دیوالت قلب بخش ابزار برقی گروه Stanley Black & Decker است و تمرکز اصلی آن بر کارگاه‌ها و کارگران ساختمانی است.\nحوزه‌های فعالیت و محصولات\nپلتفرم باتری ۲۰ ولت MAX و باتری‌های FLEXVOLT چندولتاژه\nدریل‌ها، پیچ‌گوشتی‌های ضربه‌ای و چکش‌های دوار\nاره‌های مدور، افقی و میزی\nفرزها، سنباده‌ها و ابزار برش\nجعبه‌ابزار ماژولار TOUGHSYSTEM و ذخیره‌سازی\nنقاط قوت\nاستحکام بالا برای کار سخت و محیط‌های ساختمانی\nپلتفرم باتری گسترده و سازگار\nحضور قوی در بازار آمریکا و اروپا\nمخاطبان اصلی\nپیمانکاران ساختمانی، برق‌کاران، سازندگان و صنعتگران حرفه‌ای."
  },
  {
    "slug": "ronix",
    "name": "Ronix",
    "persianName": "رونیکس",
    "established": "۲۰۰۴ (به‌طور رسمی)",
    "country": "ایران (با تولید جهانی)",
    "officialUrl": "https://ronixtools.com/en/about-us/",
    "companyIntroductions": [
      "رونیکس یکی از برندهای پیشرو در بازار ابزار ایران است که از سال ۲۰۰۴ فعالیت خود را آغاز کرده و امروزه بیش از ۲,۵۰۰ نوع ابزار و ۳۶,۰۰۰ قطعه یدکی در ۹ دسته‌بندی مختلف عرضه می‌کند . این برند دفاتر و انبارهایی در آلمان، امارات و چین دارد و محصولاتش به بیش از ۹۰ کشور جهان صادر می‌شود . رونیکس در بازار ایران به‌عنوان برند برتر از نظر سهم بازار شناخته می‌شود و شبکه گسترده خدمات پس از فروش و تأمین قطعات یدکی آن از نقاط قوت اصلی این برند است .",
      "رونیکس یکی از شناخته‌شده‌ترین برندهای ایرانی در بازار ابزارآلات است که طیف گسترده‌ای از ابزارهای برقی و دستی را عرضه می‌کند. رونیکس با استراتژی ارائه ابزار با قیمت رقابتی نسبت به برندهای اروپایی و آسیایی، سهم قابل‌توجهی از بازار داخلی را به دست آورده و در مراکز عرضه سراسر کشور حضور دارد."
    ],
    "specialization": "ابزارهای برقی، دستی، شارژی و بادی. رونیکس هم سری‌های اقتصادی (مثل سری لایت) و هم سری‌های صنعتی و حرفه‌ای (مثل سری ۸۹ با باتری ۴۰ ولت) را عرضه می‌کند . بخشی از محصولات این برند در آلمان تولید می‌شود .",
    "featuredProducts": "دریل، ابزار دستی، تجهیزات کارگاهی، ابزار شارژی.",
    "productFamilies": [
      "ابزار برقی: دریل، ف'+N'رز، اره، رنده",
      "ابزار شارژی با پلتفرم باتری لیتیومی",
      "ابزار دستی: آچار، انبردست، پیچ‌گوشتی و جعبه‌ابزار",
      "دستگاه‌های جوشکاری و ابزارهای بادی",
      "لوازم جانبی و مصرفی"
    ],
    "strengths": [
      "قیمت مناسب و دسترسی آسان در بازار ایران",
      "تنوع محصول و شبکه نمایندگی گسترده",
      "گارانتی و خدمات پس از فروش داخلی"
    ],
    "audience": "کارگاه‌های کوچک و متوسط، نصابان، صنعتگران و کاربران خانگی در ایران.",
    "shoppingAdvice": "کاربرد اصلی و دفعات استفاده را پیش از مقایسه مشخص کنید. مشخصات فنی مدل‌های هم‌گروه را کنار هم ببینید. گارانتی و اقلام داخل بسته را بررسی کنید.",
    "fullPastedText": "۴. رونیکس — Ronix\nحوزه فعالیت: ابزارهای دستی، برقی، شارژی و بادی\nسال تأسیس: ۲۰۰۴ (به‌طور رسمی)\nکشور مبدأ: ایران (با تولید جهانی)\nوب‌سایت رسمی: https://ronixtools.com/en/about-us/\n\nمعرفی شرکت\nرونیکس یکی از برندهای پیشرو در بازار ابزار ایران است که از سال ۲۰۰۴ فعالیت خود را آغاز کرده و امروزه بیش از ۲,۵۰۰ نوع ابزار و ۳۶,۰۰۰ قطعه یدکی در ۹ دسته‌بندی مختلف عرضه می‌کند . این برند دفاتر و انبارهایی در آلمان، امارات و چین دارد و محصولاتش به بیش از ۹۰ کشور جهان صادر می‌شود . رونیکس در بازار ایران به‌عنوان برند برتر از نظر سهم بازار شناخته می‌شود و شبکه گسترده خدمات پس از فروش و تأمین قطعات یدکی آن از نقاط قوت اصلی این برند است .\n\nحوزه تخصصی\nابزارهای برقی، دستی، شارژی و بادی. رونیکس هم سری‌های اقتصادی (مثل سری لایت) و هم سری‌های صنعتی و حرفه‌ای (مثل سری ۸۹ با باتری ۴۰ ولت) را عرضه می‌کند . بخشی از محصولات این برند در آلمان تولید می‌شود .\n\nمحصولات شاخص\nدریل، ابزار دستی، تجهیزات کارگاهی، ابزار شارژی.\n\nنکات خرید\nکاربرد اصلی و دفعات استفاده را پیش از مقایسه مشخص کنید. مشخصات فنی مدل‌های هم‌گروه را کنار هم ببینید. گارانتی و اقلام داخل بسته را بررسی کنید.",
    "fullDocumentText": "رونیکس (Ronix)\nنام رسمی شرکت | گروه صنعتی رونیکس (Ronix)\nسال تأسیس | ≈ دهه ۱۳۸۰ شمسی\nمقر / کشور | تهران، ایران\nحوزه فعالیت | ابزار برقی، ابزار دستی، ابزار شارژی، تجهیزات جوشکاری و ابزار بادی\nمحصولات شاخص | دریل‌ها، فرزها، اره‌ها، ابزار شارژی، دستگاه‌های جوش\nمعرفی\nرونیکس یکی از شناخته‌شده‌ترین برندهای ایرانی در بازار ابزارآلات است که طیف گسترده‌ای از ابزارهای برقی و دستی را عرضه می‌کند. رونیکس با استراتژی ارائه ابزار با قیمت رقابتی نسبت به برندهای اروپایی و آسیایی، سهم قابل‌توجهی از بازار داخلی را به دست آورده و در مراکز عرضه سراسر کشور حضور دارد.\nحوزه‌های فعالیت و محصولات\nابزار برقی: دریل، فرز، اره، رنده\nابزار شارژی با پلتفرم باتری لیتیومی\nابزار دستی: آچار، انبردست، پیچ‌گوشتی و جعبه‌ابزار\nدستگاه‌های جوشکاری و ابزارهای بادی\nلوازم جانبی و مصرفی\nنقاط قوت\nقیمت مناسب و دسترسی آسان در بازار ایران\nتنوع محصول و شبکه نمایندگی گسترده\nگارانتی و خدمات پس از فروش داخلی\nمخاطبان اصلی\nکارگاه‌های کوچک و متوسط، نصابان، صنعتگران و کاربران خانگی در ایران."
  },
  {
    "slug": "tosan",
    "name": "Tosan",
    "persianName": "توسن",
    "established": "بیش از ۲۰ سال سابقه",
    "country": "ایران (تولید در چین)",
    "officialUrl": "",
    "companyIntroductions": [
      "توسن یکی از قدیمی‌ترین برندهای ابزار در بازار ایران است که با رنگ سرمه‌ای دستگاه‌هایش شناخته می‌شود . این شرکت بیش از ۲۰ سال سابقه فعالیت دارد و بر ابزارهای کارگاهی سخت‌جان و مقاوم تمرکز کرده است . توسن در سال‌های اخیر سری جدید اینورترهای جوشکاری با تکنولوژی IGBT و ابزارهای شارژی براشلس را معرفی کرده است .",
      "توسن از برندهای ایرانی فعال در حوزه ابزارآلات است که ابزار برقی، دستی و صنعتی را عرضه می‌کند. این برند با هدف ارائه تنوع بالا در محصولات و قیمت مناسب، برای مصرف‌کنندگان خانگی و کارگاهی شناخته شده است. نام «توسن» را نباید با شرکت توسن'+N' در حوزه تجهیزات پزشکی نوزادان (Tosan Co.) اشتباه گرفت."
    ],
    "specialization": "ابزارهای برقی و دستی کارگاهی، ابزارهای اندازه‌گیری (تراز لیزری، متر لیزری)، اینورتر جوشکاری. در بازار ایران، کیفیت ساخت توسن «خوب» ارزیابی می‌شود و از نظر خدمات پس از فروش در رده «بسیار خوب» قرار دارد . استفاده از آلیاژهای مرغوب در ابزارهای دستی و مقاومت در محیط‌های پرگردوغبار از نقاط قوت این برند است .",
    "featuredProducts": "دریل، ابزارهای عمومی کارگاه، اینورتر جوشکاری (مدل‌های ۱۴۱۴ تا ۱۴۲۲)، ابزار شارژی براشلس.",
    "productFamilies": [
      "ابزار برقی: دریل ساده، چکشی و گیربکسی، فرز، اره",
      "ابزار شارژی (مانند پیچ‌گوشتی شارژی)",
      "کارواش‌های خانگی و نیمه‌صنعتی",
      "ابزار دستی: بکس، آچار، سری‌های Elite",
      "مته‌ها و لوازم جانبی ابزار برقی"
    ],
    "strengths": [
      "قیمت مناسب و تنوع بالا",
      "دسترسی گسترده در فروشگاه‌های ابزار ایران",
      "رقیب جدی ابزارهای ایرانی و وارداتی هم‌رده"
    ],
    "audience": "کاربران خانگی، صنعتگران و کارگاه‌هایی که ابزار اقتصادی می‌خواهند.",
    "shoppingAdvice": "مدل را بر اساس کار روزمره یا استفاده حرفه‌ای انتخاب کنید. توان، سرعت و نوع سه‌نظام را مقایسه کنید. لوازم جانبی سازگار با دستگاه را در نظر بگیرید.",
    "fullPastedText": "۵. توسن — Tosan\nحوزه فعالیت: ابزارهای برقی و دستی کارگاهی، اینورتر جوشکاری\nسال تأسیس: بیش از ۲۰ سال سابقه\nکشور مبدأ: ایران (تولید در چین)\nوب‌سایت رسمی: —\n\nمعرفی شرکت\nتوسن یکی از قدیمی‌ترین برندهای ابزار در بازار ایران است که با رنگ سرمه‌ای دستگاه‌هایش شناخته می‌شود . این شرکت بیش از ۲۰ سال سابقه فعالیت دارد و بر ابزارهای کارگاهی سخت‌جان و مقاوم تمرکز کرده است . توسن در سال‌های اخیر سری جدید اینورترهای جوشکاری با تکنولوژی IGBT و ابزارهای شارژی براشلس را معرفی کرده است .\n\nحوزه تخصصی\nابزارهای برقی و دستی کارگاهی، ابزارهای اندازه‌گیری (تراز لیزری، متر لیزری)، اینورتر جوشکاری. در بازار ایران، کیفیت ساخت توسن «خوب» ارزیابی می‌شود و از نظر خدمات پس از فروش در رده «بسیار خوب» قرار دارد . استفاده از آلیاژهای مرغوب در ابزارهای دستی و مقاومت در محیط‌های پرگردوغبار از نقاط قوت این برند است .\n\nمحصولات شاخص\nدریل، ابزارهای عمومی کارگاه، اینورتر جوشکاری (مدل‌های ۱۴۱۴ تا ۱۴۲۲)، ابزار شارژی براشلس.\n\nنکات خرید\nمدل را بر اساس کار روزمره یا استفاده حرفه‌ای انتخاب کنید. توان، سرعت و نوع سه‌نظام را مقایسه کنید. لوازم جانبی سازگار با دستگاه را در نظر بگیرید.",
    "fullDocumentText": "توسن (Tosan)\nنام برند | Tosan (توسن)\nسال تأسیس | ≈ ۱۳۷۹ شمسی (طبق منابع فروشگاهی)\nمقر / کشور | ایران\nحوزه فعالیت | ابزار برقی، ابزار دستی، ابزار صنعتی\nمحصولات شاخص | اره، فرز، دریل شارژی و چکشی، کارواش، مته‌های HSS، آچارآلات\nمعرفی\nتوسن از برندهای ایرانی فعال در حوزه ابزارآلات است که ابزار برقی، دستی و صنعتی را عرضه می‌کند. این برند با هدف ارائه تنوع بالا در محصولات و قیمت مناسب، برای مصرف‌کنندگان خانگی و کارگاهی شناخته شده است. نام «توسن» را نباید با شرکت توسن در حوزه تجهیزات پزشکی نوزادان (Tosan Co.) اشتباه گرفت.\nحوزه‌های فعالیت و محصولات\nابزار برقی: دریل ساده، چکشی و گیربکسی، فرز، اره\nابزار شارژی (مانند پیچ‌گوشتی شارژی)\nکارواش‌های خانگی و نیمه‌صنعتی\nابزار دستی: بکس، آچار، سری‌های Elite\nمته‌ها و لوازم جانبی ابزار برقی\nنقاط قوت\nقیمت مناسب و تنوع بالا\nدسترسی گسترده در فروشگاه‌های ابزار ایران\nرقیب جدی ابزارهای ایرانی و وارداتی هم‌رده\nمخاطبان اصلی\nکاربران خانگی، صنعتگران و کارگاه‌هایی که ابزار اقتصادی می‌خواهند."
  },
  {
    "slug": "uni-t",
    "name": "UNI-T",
    "persianName": "یونی‌تی",
    "established": "۱۹۸۸",
    "country": "چین",
    "officialUrl": "https://www.uni-trend.com/about-us/overview/",
    "companyIntroductions": [
 '+N'     "UNI-T از سال ۱۹۸۸ در حوزه آزمون و اندازه‌گیری فعالیت می‌کند. سبد رسمی این برند ابزارهایی مانند مولتی‌متر، تجهیزات تست، ابزارهای اندازه‌گیری میدانی و دوربین‌های حرارتی را در بر می‌گیرد. این برند خود را ارائه‌دهنده راهکارهای آزمون و اندازه‌گیری برای کاربران فنی، صنعتی و آموزشی معرفی می‌کند.",
      "یونی‌تی از تولیدکنندگان بزرگ تجهیزات اندازه‌گیری و آزمایش الکترونیکی چین است. این شرکت از مولتی‌مترهای ساده تا اسیلوسکوپ‌های دیجیتال، آنالیزورهای طیف و دوربین‌های حرارتی را تولید می‌کند و به‌دلیل نسبت قیمت به کارایی مناسب، در میان تعمیرکاران الکترونیک، دانشجویان و آزمایشگاه‌ها محبوب است."
    ],
    "specialization": "مولتی‌متر دیجیتال، تستر ولتاژ، ابزارهای اندازه‌گیری میدانی، دوربین حرارتی. UNI-T در رده قیمتی متوسط تا اقتصادی فعالیت می‌کند و گزینه‌ای مناسب برای کاربران فنی و کارگاه‌های عمومی است.",
    "featuredProducts": "مولتی‌متر، تستر ولتاژ و ابزار اندازه‌گیری.",
    "productFamilies": [
      "مولتی‌مترهای دیجیتال (سری UT)",
      "کلمپ‌مترهای AC/DC",
      "اسیلوسکوپ‌ها و مولدهای سیگنال",
      "منابع تغذیه DC و بارهای الکترونیکی",
      "دوربین‌های حرارتی و تستر عایق"
    ],
    "strengths": [
      "قیمت رقابتی نسبت به برندهای اروپایی/آمریکایی",
      "طیف کامل از ابزار پایه تا تجهیزات آزمایشگاهی"
    ],
    "audience": "",
    "shoppingAdvice": "کمیت‌های موردنیاز مانند ولتاژ یا جریان را مشخص کنید. رده ایمنی و محدوده اندازه‌گیری دستگاه را بررسی کنید. نوع پراب و شیوه تغذیه را با محیط کار تطبیق دهید.",
    "fullPastedText": "۶. یونی‌تی — UNI-T\nحوزه فعالیت: ابزارهای آزمون و اندازه‌گیری الکتریکی\nسال تأسیس: ۱۹۸۸\nکشور مبدأ: چین\nوب‌سایت رسمی: https://www.uni-trend.com/about-us/overview/\n\nمعرفی شرکت\nUNI-T از سال ۱۹۸۸ در حوزه آزمون و اندازه‌گیری فعالیت می‌کند. سبد رسمی این برند ابزارهایی مانند مولتی‌متر، تجهیزات تست، ابزارهای اندازه‌گیری میدانی و دوربین‌های حرارتی را در بر می‌گیرد. این برند خود را ارائه‌دهنده راهکارهای آزمون و اندازه‌گیری برای کاربران فنی، صنعتی و آموزشی معرفی می‌کند.\n\nحوزه تخصصی\nمولتی‌متر دیجیتال، تستر ولتاژ، ابزارهای اندازه‌گیری میدانی، دوربین حرارتی. UNI-T در رده قیمتی متوسط تا اقتصادی فعالیت می‌کند و گزینه‌ای مناسب برای کاربران فنی و کارگاه‌های عمومی است.\n\nمحصولات شاخص\nمولتی‌متر، تستر ولتاژ و ابزار اندازه‌گیری.\n\nنکات خرید\nکمیت‌های موردنیاز مانند ولتاژ یا جریان را مشخص کنید. رده ایمنی و محدوده اندازه‌گیری دستگاه را بررسی کنید. نوع پراب و شیوه تغذیه را با محیط کار تطبیق دهید.",
    "fullDocumentText": "یونی‌تی (UNI-T)\nنام رسمی شرکت | Uni-Trend Technology (China) Co., Ltd.\nسال تأسیس | ≈ ۱۹۸۸ میلادی\nمقر / کشور | دونگ‌گوان (استان گوانگ‌دونگ)، چین؛ دفتر در هنگ‌کنگ\nحوزه فعالیت | تجهیزات اندازه‌گیری و تست الکترونیکی\nمحصولات شاخص | مولتی‌متر، کلمپ‌متر، اسیلوسکوپ، منبع تغذیه، مولد سیگنال، دوربین حرارتی\nمعرفی\nیونی‌تی از تولیدکنندگان بزرگ تجهیزات اندازه‌گیری و آزمایش الکترونیکی چین است. این شرکت از مولتی‌مترهای ساده تا اسیلوسکوپ‌های دیجیتال، آنالیزورهای طیف و دوربین‌های حرارتی را تولید می‌کند و به‌دلیل نسبت قیمت به کارایی مناسب، در میان تعمیرکاران الکترونیک، دانشجویان و آزمایشگاه‌ها محبوب است.\nحوزه‌های فعالیت و محصولات\nمولتی‌مترهای دیجیتال (سری UT)\nکلمپ‌مترهای AC/DC\nاسیلوسکوپ‌ها و مولدهای سیگنال\nمنابع تغذیه DC و بارهای الکترونیکی\nدوربین‌های حرارتی و تستر عایق\nنقاط قوت\nقیمت رقابتی نسبت به برندهای اروپایی/آمریکایی\nطیف کامل از ابزار پایه تا تجهیزات آزمایشگاهی\nپوشش گسترده رده‌های ایمنی (CAT)\nمخاطبان اصلی\nتعمیرکاران الکترونیک، آزمایشگاه‌ها، دانشجویان و نگهداری صنعتی."
  },
  {
    "slug": "fluke",
    "name": "Fluke",
    "persianName": "فلوک",
    "established": "۱۹۴۸",
  '+N'  "country": "آمریکا",
    "officialUrl": "https://www.fluke.com/en-us/support/about-us/corporate-profile",
    "companyIntroductions": [
      "فلوک از سال ۱۹۴۸ ابزارهای آزمون، عیب‌یابی و اندازه‌گیری را برای نگهداری و سرویس تجهیزات توسعه داده است. این برند در حوزه اندازه‌گیری‌های الکتریکی و صنعتی، از بررسی‌های میدانی تا کنترل و پایش دقیق، شناخته‌شده‌ترین نام در سطح جهانی است. محصولات فلوک برای طیفی از کاربران فنی طراحی شده‌اند، اما سطح دقت، قابلیت‌ها و رده ایمنی در هر مدل متفاوت است.",
      "فلوک مرجع جهانی تجهیزات اندازه‌گیری الکتریکی برای حرفه‌ای‌هاست. مولتی‌مترهای فلوک (مانند خانواده 87 و 179) استانداردی در صنعت محسوب می‌شوند. محصولات این شرکت برای برق‌کاران، مهندسان نگهداری و تکنسین‌های صنعتی ساخته می‌شود و بر ایمنی کاربر و دقت اندازه‌گیری تأکید دارد."
    ],
    "specialization": "مولتی‌متر، کلمپ‌متر، تجهیزات تست صنعتی، ابزارهای عیب‌یابی تخصصی. فلوک در رده قیمتی بالا قرار می‌گیرد و استاندارد مرجع در صنعت اندازه‌گیری محسوب می‌شود.",
    "featuredProducts": "مولتی‌متر، کلمپ‌متر و تجهیزات تست.",
    "productFamilies": [
      "مولتی‌مترهای دیجیتال و True-RMS",
      "کلمپ‌متر و تستر عایق",
      "دوربین‌های حرارتی (سری Ti و TiS)",
      "کالیبراتورهای فرآیند و دما",
      "تجهیزات تست کیفیت توان و نصب برق"
    ],
    "strengths": [
      "دقت و ایمنی استاندارد صنعتی (CAT III/IV)",
      "اعتبار برند در صنایع و مراکز نگهداری",
      "گارانتی و پشتیبانی بین‌المللی"
    ],
    "audience": "برق‌کاران صنعتی، مهندسان نگهداری، آزمایشگاه‌های کالیبراسیون و پیمانکاران.",
    "shoppingAdvice": "نوع اندازه‌گیری و محدوده کاری را از پیش تعیین کنید. رده ایمنی دستگاه را متناسب با محل استفاده بسنجید. دقت، نمایشگر و لوازم اندازه‌گیری همراه را بررسی کنید.",
    "fullPastedText": "۷. فلوک — Fluke\nحوزه فعالیت: ابزارهای آزمون، عیب‌یابی و اندازه‌گیری صنعتی\nسال تأسیس: ۱۹۴۸\nکشور مبدأ: آمریکا\nوب‌سایت رسمی: https://www.fluke.com/en-us/support/about-us/corporate-profile\n\nمعرفی شرکت\nفلوک از سال ۱۹۴۸ ابزارهای آزمون، عیب‌یابی و اندازه‌گیری را برای نگهداری و سرویس تجهیزات توسعه داده است. این برند در حوزه اندازه‌گیری‌های الکتریکی و صنعتی، از بررسی‌های میدانی تا کنترل و پایش دقیق، شناخته‌شده‌ترین نام در سطح جهانی است. محصولات فلوک برای طیفی از کاربران فنی طراحی شده‌اند، اما سطح دقت، قابلیت‌ها و رده ایمنی در هر مدل متفاوت است.\n\nحوزه تخصصی\nمولتی‌متر، کلمپ‌متر، تجهیزات تست صنعتی، ابزارهای عیب‌یابی تخصصی. فلوک در رده قیمتی بالا قرار می‌گیرد و استاندارد مرجع در صنعت اندازه‌گیری محسوب می‌شود.\n\nمحصولات شاخص\nمولتی‌متر، کلمپ‌متر و تجهیزات تست.\n\nنکات خرید\nنوع اندازه‌گیری و محدوده کاری را از پیش تعیین کنید. رده ایمنی دستگاه را متناسب با محل استفاده بسنجید. دقت، نمایشگر و لوازم اندازه‌گیری همراه را بررسی کنید.",
    "fullDocumentText": "فلوک (Fluke)\nنام رسمی شرکت | Fluke Corporation\nسال تأسیس | ۱۹۴۸ میلادی؛ توسط جان فلوک\nمقر / کشور | اورت، ایالت واشنگتن، ایالات متحده\nمالکیت | زیرمجموعه گروه Fortive\nحوزه فعالیت | ابزار اندازه‌گیری و تست الکتریکی صنعتی، کالیبراسیون\nمحصولات شاخص | مولتی‌متر، کلمپ‌متر، دوربین حرارتی، کالیبراتور، تستر نصب\nمعرفی\nفلوک مرجع جهانی تجهیزات اندازه‌گیری الکتریکی برای حرفه‌ای‌هاست. مولتی‌مترهای فلوک (مانند خانواده 87 و 179) استانداردی در صنعت محسوب می‌شوند. محصولات این شرکت برای برق‌کاران، مهندسان نگهداری و تکنسین‌های صنعتی ساخته می‌شود و بر ایمنی کاربر و دقت اندازه‌گیری تأکید دارد.\nحوزه‌های فعالیت و محصولات\nمولتی‌مترهای دیجیتال و True-RMS\nکلمپ‌متر و تستر عایق\nدوربین‌های حرارتی (سری Ti و TiS)\nکالیبراتورهای فرآیند و دما\nتجهیزات تست کیفیت توان و نصب برق\nنقاط قوت\nدقت و ایمنی استاندارد صنعتی (CAT III/IV)\nا'+N'عتبار برند در صنایع و مراکز نگهداری\nگارانتی و پشتیبانی بین‌المللی\nمخاطبان اصلی\nبرق‌کاران صنعتی، مهندسان نگهداری، آزمایشگاه‌های کالیبراسیون و پیمانکاران."
  },
  {
    "slug": "mastech",
    "name": "Mastech",
    "persianName": "مستک",
    "established": "بیش از ۲۰ سال سابقه",
    "country": "چین",
    "officialUrl": "https://mastech-group.com/",
    "companyIntroductions": [
      "Mastech یکی از تولیدکنندگان ابزارهای تست و اندازه‌گیری الکتریکی است که بیش از ۲۰ سال سابقه فعالیت دارد . این شرکت محصولات خود را تحت برندهای Mastech، KPS، C-Logic و Power Probe عرضه می‌کند و در حوزه‌های اندازه‌گیری الکتریکی و خودرویی فعالیت می‌کند . محصولات Mastech برای بخش‌های ساختمانی، DIY و صنعتی طراحی شده‌اند و شامل مولتی‌متر دیجیتال، کلمپ‌متر، تستر ولتاژ، تستر پریز و انواع تسترهای محیطی می‌شود .",
      "مستک برند تجهیزات اندازه‌گیری و تست است که ابزارهای پایه و نیمه‌حرفه‌ای را با قیمت مناسب عرضه می‌کند. مولتی‌مترهای مستک مانند MS8268 به‌دلیل قابلیت‌هایی مثل اندازه‌گیری خازن و فرکانس، قفل نمایش و حفاظت اضافه‌بار در بازار محبوب‌اند. نام Mastech با شرکت آمریکایی Mastech Digital (حوزه فناوری اطلاعات) ارتباطی ندارد."
    ],
    "specialization": "مولتی‌متر دیجیتال، کلمپ‌متر، تستر ولتاژ، تجهیزات تست خودرویی و تسترهای محیطی (دما، رطوبت، سرعت باد، نور، صدا، گاز و فاصله) . این برند در رده قیمتی اقتصادی تا متوسط فعالیت می‌کند.",
    "featuredProducts": "مولتی‌متر و ابزار تست الکتریکی.",
    "productFamilies": [
      "مولتی‌مترهای دیجیتال (MS8268، MS8269، MAS830L)",
      "کلمپ‌مترها (MS2115A و غیره)",
      "LCR متر و خازن‌سنج",
      "تستر مقاومت عایق و بادسنج",
      "کالیبراتورهای دما"
    ],
    "strengths": [
      "قیمت اقتصادی نسبت به برندهای صنعتی",
      "امکانات متنوع در مدل‌های ساده",
      "مناسب آموزش و تعمیرات سبک"
    ],
    "audience": "دانشجویان، تعمیرکاران، علاقه‌مندان و تکنسین‌های عمومی.",
    "shoppingAdvice": "به بازه اندازه‌گیری و قابلیت‌های لازم توجه کنید. تعداد ورودی‌ها و نوع پراب را بررسی کنید. دستورالعمل ایمنی و شرایط محیط کار را در نظر بگیرید.",
    "fullPastedText": "۸. مستک — Mastech\nحوزه فعالیت: ابزارهای اندازه‌گیری و تست الکتریکی و خودرویی\nسال تأسیس: بیش از ۲۰ سال سابقه\nکشور مبدأ: چین\nوب‌سایت رسمی: https://mastech-group.com/\n\nمعرفی شرکت\nMastech یکی از تولیدکنندگان ابزارهای تست و اندازه‌گیری الکتریکی است که بیش از ۲۰ سال سابقه فعالیت دارد . این شرکت محصولات خود را تحت برندهای Mastech، KPS، C-Logic و Power Probe عرضه می‌کند و در حوزه‌های اندازه‌گیری الکتریکی و خودرویی فعالیت می‌کند . محصولات Mastech برای بخش‌های ساختمانی، DIY و صنعتی طراحی شده‌اند و شامل مولتی‌متر دیجیتال، کلمپ‌متر، تستر ولتاژ، تستر پریز و انواع تسترهای محیطی می‌شود .\n\nحوزه تخصصی\nمولتی‌متر دیجیتال، کلمپ‌متر، تستر ولتاژ، تجهیزات تست خودرویی و تسترهای محیطی (دما، رطوبت، سرعت باد، نور، صدا، گاز و فاصله) . این برند در رده قیمتی اقتصادی تا متوسط فعالیت می‌کند.\n\nمحصولات شاخص\nمولتی‌متر و ابزار تست الکتریکی.\n\nنکات خرید\nبه بازه اندازه‌گیری و قابلیت‌های لازم توجه کنید. تعداد ورودی‌ها و نوع پراب را بررسی کنید. دستورالعمل ایمنی و شرایط محیط کار را در نظر بگیرید.",
    "fullDocumentText": "مستک (Mastech)\nنام رسمی شرکت | Mastech Group International Limited\nمقر / کشور | هنگ‌کنگ؛ تولید عمدتاً در چین\nحوزه فعالیت | تجهیزات اندازه‌گیری و تست الکترونیکی اقتصادی\nمحصولات شاخص | مولتی‌متر (MS8268 و MAS830L)، کلمپ‌متر، LCR متر، تستر عایق، بادسنج\nمعرفی\nمستک برند تجهیزات اندازه‌گیری و تست است که ابزارهای پایه و نیمه‌حرفه‌ای را با قیمت مناسب عرضه می‌کند. مولتی‌مترهای مستک مانند MS8268 به‌دلیل قابلیت‌هایی مثل اندازه‌گیری خازن و فرکانس، قفل'+N' نمایش و حفاظت اضافه‌بار در بازار محبوب‌اند. نام Mastech با شرکت آمریکایی Mastech Digital (حوزه فناوری اطلاعات) ارتباطی ندارد.\nحوزه‌های فعالیت و محصولات\nمولتی‌مترهای دیجیتال (MS8268، MS8269، MAS830L)\nکلمپ‌مترها (MS2115A و غیره)\nLCR متر و خازن‌سنج\nتستر مقاومت عایق و بادسنج\nکالیبراتورهای دما\nنقاط قوت\nقیمت اقتصادی نسبت به برندهای صنعتی\nامکانات متنوع در مدل‌های ساده\nمناسب آموزش و تعمیرات سبک\nمخاطبان اصلی\nدانشجویان، تعمیرکاران، علاقه‌مندان و تکنسین‌های عمومی."
  },
  {
    "slug": "kyoritsu",
    "name": "Kyoritsu",
    "persianName": "کیوریتسو",
    "established": "۱۹۴۰",
    "country": "ژاپن",
    "officialUrl": "https://www.kew-ltd.co.jp/",
    "companyIntroductions": [
      "کیوریتسو (Kyoritsu) در سال ۱۹۴۰ توسط تاکئو کوراموتو تأسیس شد و فعالیت خود را با تولید ابزارهای اندازه‌گیری الکتریکی آغاز کرد . این شرکت ژاپنی در سال ۱۹۶۵ اولین کلمپ‌متر ژاپن را توسعه داد و از آن زمان به‌عنوان یکی از پیشگامان صنعت اندازه‌گیری الکتریکی شناخته می‌شود . برند KEW این شرکت در بیش از ۶۰ کشور جهان گسترش یافته است .",
      "کیوریتسو تولیدکننده ژاپنی ابزارهای اندازه‌گیری الکتریکی است که برند محصولاتش «KEW» نام دارد. تخصص اصلی شرکت در تستر عایق، تستر ارت، کلمپ‌متر و ابزار بازرسی ایمنی برق است. این برند به دقت و پایداری در بازرسی نصب‌های برقی شهرت دارد."
    ],
    "specialization": "کلمپ‌متر، مولتی‌متر، تستر عایق، تستر زمین، تستر نشتی جریان و ابزارهای تخصصی اندازه‌گیری الکتریکی برای صنعت و سرویس . کیوریتسو در رده قیمتی متوسط تا بالا فعالیت می‌کند و کیفیت ساخت ژاپنی آن شناخته‌شده است.",
    "featuredProducts": "کلمپ‌متر، مولتی‌متر و تستر.",
    "productFamilies": [
      "تستر مقاومت عایق (مگراهم)",
      "ارت‌تستر و تستر حلقه",
      "کلمپ‌متر نشتی و کلمپ‌متر معمولی",
      "مولتی‌متر و تستر ولتاژ",
      "تجهیزات بازرسی نصب‌های برقی"
    ],
    "strengths": [
      "تخصص ژاپنی در تست‌های ایمنی برق",
      "دوام و دقت پایدار",
      "مناسب بازرسی و نگهداری"
    ],
    "audience": "برق‌کاران حرفه‌ای، بازرسان فنی و واحدهای نگهداری و تعمیرات.",
    "shoppingAdvice": "روش اندازه‌گیری مناسب کارتان را انتخاب کنید. محدوده و دقت اندازه‌گیری را با نیاز پروژه بسنجید. ایمنی، پراب و منبع تغذیه را پیش از خرید بررسی کنید.",
    "fullPastedText": "۹. کیوریتسو — Kyoritsu\nحوزه فعالیت: ابزارهای اندازه‌گیری الکتریکی، سرویس و نگهداری\nسال تأسیس: ۱۹۴۰\nکشور مبدأ: ژاپن\nوب‌سایت رسمی: https://www.kew-ltd.co.jp/\n\nمعرفی شرکت\nکیوریتسو (Kyoritsu) در سال ۱۹۴۰ توسط تاکئو کوراموتو تأسیس شد و فعالیت خود را با تولید ابزارهای اندازه‌گیری الکتریکی آغاز کرد . این شرکت ژاپنی در سال ۱۹۶۵ اولین کلمپ‌متر ژاپن را توسعه داد و از آن زمان به‌عنوان یکی از پیشگامان صنعت اندازه‌گیری الکتریکی شناخته می‌شود . برند KEW این شرکت در بیش از ۶۰ کشور جهان گسترش یافته است .\n\nحوزه تخصصی\nکلمپ‌متر، مولتی‌متر، تستر عایق، تستر زمین، تستر نشتی جریان و ابزارهای تخصصی اندازه‌گیری الکتریکی برای صنعت و سرویس . کیوریتسو در رده قیمتی متوسط تا بالا فعالیت می‌کند و کیفیت ساخت ژاپنی آن شناخته‌شده است.\n\nمحصولات شاخص\nکلمپ‌متر، مولتی‌متر و تستر.\n\nنکات خرید\nروش اندازه‌گیری مناسب کارتان را انتخاب کنید. محدوده و دقت اندازه‌گیری را با نیاز پروژه بسنجید. ایمنی، پراب و منبع تغذیه را پیش از خرید بررسی کنید.",
    "fullDocumentText": "کیوریتسو (Kyoritsu)\nنام رسمی شرکت | Kyoritsu Electrical Instruments Works, Ltd. (共立電気計器)\nسال تأسیس | ≈ ۱۹۴۲ میلادی\nمقر / کشور | توکیو، ژاپن\nحوزه فعالیت | تجهیزات اندازه‌گیری و ایمنی الکتریکی\nمحصولات شاخص | تستر مقاومت عایق (مگر)، ارت‌تستر، کلمپ‌متر، مولتی‌متر (سری KEW)\nمعرفی\nکیوریتسو تولیدکننده ژاپنی ابزارهای اندازه‌گیری الکتریکی است که برند محصول'+N'اتش «KEW» نام دارد. تخصص اصلی شرکت در تستر عایق، تستر ارت، کلمپ‌متر و ابزار بازرسی ایمنی برق است. این برند به دقت و پایداری در بازرسی نصب‌های برقی شهرت دارد.\nحوزه‌های فعالیت و محصولات\nتستر مقاومت عایق (مگراهم)\nارت‌تستر و تستر حلقه\nکلمپ‌متر نشتی و کلمپ‌متر معمولی\nمولتی‌متر و تستر ولتاژ\nتجهیزات بازرسی نصب‌های برقی\nنقاط قوت\nتخصص ژاپنی در تست‌های ایمنی برق\nدوام و دقت پایدار\nمناسب بازرسی و نگهداری\nمخاطبان اصلی\nبرق‌کاران حرفه‌ای، بازرسان فنی و واحدهای نگهداری و تعمیرات."
  },
  {
    "slug": "yihua",
    "name": "Yihua",
    "persianName": "یوهوا",
    "established": "۲۰۰۰ (شروع فعالیت)، ۲۰۰۴ (تأسیس رسمی شرکت)",
    "country": "چین",
    "officialUrl": "http://www.yihua-soldering.com/",
    "companyIntroductions": [
      "Yihua Electronic Equipment در سال ۲۰۰۴ تأسیس شد و بیش از ۱۰,۰۰۰ متر مربع فضای تولید و ۲۳۰ کارمند دارد که حداقل ۲۰ نفر آن‌ها طراح هستند . این شرکت به تحقیق و تولید ایستگاه‌های لحیم‌کاری و منابع تغذیه DC اختصاص دارد و تاکنون بیش از ۲۱ میلیون دستگاه تولید کرده است که ۷۰٪ آن به بیش از ۴۰ کشور جهان صادر شده است . Yihua یکی از تدوین‌کنندگان استاندارد صنعت لحیم‌کاری چین است و بیش از ۱۰۰ پتنت فناوری دارد .",
      "یوهوا (YIHUA) برند چینی تجهیزات لحیم‌کاری و ریورک اقتصادی است که در میان تعمیرکاران موبایل و برد الکترونیک بسیار پرکاربرد است. محصولات یوهوا شامل هویه کنترل دما، هیتر هوای گرم، دستگاه دی‌سولدرینگ و منابع تغذیه DC می‌شود. اطلاعات رسمی عمومی در دسترس درباره تاریخچه شرکت محدود است."
    ],
    "specialization": "ایستگاه هیتر هوای گرم (Hot Air Rework Station)، ایستگاه لحیم‌کاری دمای ثابت، منبع تغذیه DC. محصولات Yihua در رده قیمتی اقتصادی تا متوسط قرار می‌گیرند و گزینه‌ای مقرون‌به‌صرفه برای کارگاه‌های تعمیرات الکترونیک هستند.",
    "featuredProducts": "ایستگاه هویه، هیتر و لوازم لحیم‌کاری.",
    "productFamilies": [
      "ایستگاه لحیم‌کاری با کنترل دما",
      "هیتر هوای گرم SMD/BGA",
      "منبع تغذیه DC و ابزار تعمیر موبایل",
      "دستگاه دی‌سولدر و لوازم مصرفی"
    ],
    "strengths": [
      "قیمت پایین و مناسب کارگاه‌های تعمیر",
      "تنوع مدل و در دسترس بودن قطعات",
      "محبوبیت در بازار تعمیرات موبایل"
    ],
    "audience": "تعمیرکاران موبایل و لپ‌تاپ، علاقه‌مندان الکترونیک و کارگاه‌های کوچک.",
    "shoppingAdvice": "نوع کار و بازه دمای موردنیاز را مشخص کنید. کنترل دما و توان دستگاه را بررسی کنید. سری هویه و اقلام همراه باید با کار شما سازگار باشد.",
    "fullPastedText": "۱۰. یوهوا — Yihua\nحوزه فعالیت: تجهیزات تعمیر و مونتاژ الکترونیک، ایستگاه هویه و هیتر\nسال تأسیس: ۲۰۰۰ (شروع فعالیت)، ۲۰۰۴ (تأسیس رسمی شرکت)\nکشور مبدأ: چین\nوب‌سایت رسمی: http://www.yihua-soldering.com/\n\nمعرفی شرکت\nYihua Electronic Equipment در سال ۲۰۰۴ تأسیس شد و بیش از ۱۰,۰۰۰ متر مربع فضای تولید و ۲۳۰ کارمند دارد که حداقل ۲۰ نفر آن‌ها طراح هستند . این شرکت به تحقیق و تولید ایستگاه‌های لحیم‌کاری و منابع تغذیه DC اختصاص دارد و تاکنون بیش از ۲۱ میلیون دستگاه تولید کرده است که ۷۰٪ آن به بیش از ۴۰ کشور جهان صادر شده است . Yihua یکی از تدوین‌کنندگان استاندارد صنعت لحیم‌کاری چین است و بیش از ۱۰۰ پتنت فناوری دارد .\n\nحوزه تخصصی\nایستگاه هیتر هوای گرم (Hot Air Rework Station)، ایستگاه لحیم‌کاری دمای ثابت، منبع تغذیه DC. محصولات Yihua در رده قیمتی اقتصادی تا متوسط قرار می‌گیرند و گزینه‌ای مقرون‌به‌صرفه برای کارگاه‌های تعمیرات الکترونیک هستند.\n\nمحصولات شاخص\nایستگاه هویه، هیتر و لوازم لحیم‌کاری.\n\nنکات خرید\nنوع کار و بازه دمای موردنیاز را مشخص کنید. کنترل دما و توان دستگاه را بررسی کنید. سری هویه و اقلام همراه باید با کار شما سازگار باشد.",
    "fullDocumentText": "یوهوا (Yihua)\nنام رسمی شرکت | Yihua El'+N'ectronic Equipment (شنژن، چین)\nمقر / کشور | چین\nحوزه فعالیت | ایستگاه‌های لحیم‌کاری، هیتر هوای گرم و منابع تغذیه\nمحصولات شاخص | لحیم‌کاری 936، هیتر 8786D و 898، منبع تغذیه 305D، ابزار تعمیر موبایل\nمعرفی\nیوهوا (YIHUA) برند چینی تجهیزات لحیم‌کاری و ریورک اقتصادی است که در میان تعمیرکاران موبایل و برد الکترونیک بسیار پرکاربرد است. محصولات یوهوا شامل هویه کنترل دما، هیتر هوای گرم، دستگاه دی‌سولدرینگ و منابع تغذیه DC می‌شود. اطلاعات رسمی عمومی در دسترس درباره تاریخچه شرکت محدود است.\nحوزه‌های فعالیت و محصولات\nایستگاه لحیم‌کاری با کنترل دما\nهیتر هوای گرم SMD/BGA\nمنبع تغذیه DC و ابزار تعمیر موبایل\nدستگاه دی‌سولدر و لوازم مصرفی\nنقاط قوت\nقیمت پایین و مناسب کارگاه‌های تعمیر\nتنوع مدل و در دسترس بودن قطعات\nمحبوبیت در بازار تعمیرات موبایل\nمخاطبان اصلی\nتعمیرکاران موبایل و لپ‌تاپ، علاقه‌مندان الکترونیک و کارگاه‌های کوچک."
  },
  {
    "slug": "hakko",
    "name": "Hakko",
    "persianName": "هاکو",
    "established": "۱۹۵۲",
    "country": "ژاپن",
    "officialUrl": "https://www.hakko.com/english/",
    "companyIntroductions": [
      "هاکو از سال ۱۹۵۲ به‌صورت تخصصی بر تولید هویه و تجهیزات لحیم‌کاری متمرکز است. این شرکت ژاپنی یکی از شناخته‌شده‌ترین برندهای صنعت الکترونیک در زمینه تجهیزات لحیم‌کاری محسوب می‌شود. دامنه محصولات رسمی آن از ابزارهای ایستگاهی تا راهکارهای کنترل حرارت را دربرمی‌گیرد. برای کارهای تعمیراتی حساس، سازگاری نوک، پایداری دما و قطعات مصرفی اهمیت ویژه دارند.",
      "هاکو برند ژاپنی و یکی از مراجع بین‌المللی تجهیزات لحیم‌کاری است. سیستم‌های هاکو با گرمایش سریع، کنترل دقیق دما و تیپ‌های با عمر طولانی در خطوط تولید الکترونیک و کارگاه‌های تعمیر استفاده می‌شوند. FX-888D و FX-951 از محبوب‌ترین مدل‌های آن هستند."
    ],
    "specialization": "هویه، ایستگاه لحیم‌کاری، نوک هویه، تجهیزات کنترل حرارت و لوازم مصرفی لحیم‌کاری. هاکو در رده قیمتی بالا فعالیت می‌کند و انتخاب اول بسیاری از تعمیرکاران حرفه‌ای الکترونیک است.",
    "featuredProducts": "هویه، ایستگاه لحیم‌کاری و نوک هویه.",
    "productFamilies": [
      "هویه‌ها و ایستگاه‌های لحیم‌کاری با کنترل دقیق دما",
      "سیستم‌های دی‌سولدر",
      "هیتر هوای گرم و ایستگاه ریورک",
      "فن‌های دود و فیلتر",
      "تیپ‌ها و لوازم مصرفی"
    ],
    "strengths": [
      "گرمایش سریع و پایداری دما",
      "کیفیت ساخت ژاپنی و قطعات یدکی در دسترس",
      "اعتماد در خطوط تولید و آموزش"
    ],
    "audience": "خطوط تولید الکترونیک، آزمایشگاه‌ها، تعمیرکاران حرفه‌ای و مدارس فنی.",
    "shoppingAdvice": "نوع مونتاژ یا تعمیر را مبنای انتخاب قرار دهید. توان و سازگاری نوک هویه را بررسی کنید. لوازم مصرفی و قطعات جایگزین را هم در نظر بگیرید.",
    "fullPastedText": "۱۱. هاکو — Hakko\nحوزه فعالیت: تجهیزات تخصصی لحیم‌کاری\nسال تأسیس: ۱۹۵۲\nکشور مبدأ: ژاپن\nوب‌سایت رسمی: https://www.hakko.com/english/\n\nمعرفی شرکت\nهاکو از سال ۱۹۵۲ به‌صورت تخصصی بر تولید هویه و تجهیزات لحیم‌کاری متمرکز است. این شرکت ژاپنی یکی از شناخته‌شده‌ترین برندهای صنعت الکترونیک در زمینه تجهیزات لحیم‌کاری محسوب می‌شود. دامنه محصولات رسمی آن از ابزارهای ایستگاهی تا راهکارهای کنترل حرارت را دربرمی‌گیرد. برای کارهای تعمیراتی حساس، سازگاری نوک، پایداری دما و قطعات مصرفی اهمیت ویژه دارند.\n\nحوزه تخصصی\nهویه، ایستگاه لحیم‌کاری، نوک هویه، تجهیزات کنترل حرارت و لوازم مصرفی لحیم‌کاری. هاکو در رده قیمتی بالا فعالیت می‌کند و انتخاب اول بسیاری از تعمیرکاران حرفه‌ای الکترونیک است.\n\nمحصولات شاخص\nهویه، ایستگاه لحیم‌کاری و نوک هویه.\n\nنکات خرید\nنوع مونتاژ یا تعمیر را مبنای انتخاب قرار دهید. توان و سازگاری نوک هویه را بررسی کنید. لوازم مصرفی و قطعات جایگزین را هم در نظر بگیرید.",
    "fullDocumentText": "ها'+N'کو (Hakko)\nنام رسمی شرکت | HAKKO Corporation (白光株式会社)\nسال تأسیس | ≈ ۱۹۵۰ میلادی\nمقر / کشور | اوساکا، ژاپن\nحوزه فعالیت | تجهیزات لحیم‌کاری و ریورک حرفه‌ای\nمحصولات شاخص | هویه و ایستگاه لحیم FX-888D، سیستم FX-951، دی‌سولدر، هیتر هوای گرم، فن‌های دود\nمعرفی\nهاکو برند ژاپنی و یکی از مراجع بین‌المللی تجهیزات لحیم‌کاری است. سیستم‌های هاکو با گرمایش سریع، کنترل دقیق دما و تیپ‌های با عمر طولانی در خطوط تولید الکترونیک و کارگاه‌های تعمیر استفاده می‌شوند. FX-888D و FX-951 از محبوب‌ترین مدل‌های آن هستند.\nحوزه‌های فعالیت و محصولات\nهویه‌ها و ایستگاه‌های لحیم‌کاری با کنترل دقیق دما\nسیستم‌های دی‌سولدر\nهیتر هوای گرم و ایستگاه ریورک\nفن‌های دود و فیلتر\nتیپ‌ها و لوازم مصرفی\nنقاط قوت\nگرمایش سریع و پایداری دما\nکیفیت ساخت ژاپنی و قطعات یدکی در دسترس\nاعتماد در خطوط تولید و آموزش\nمخاطبان اصلی\nخطوط تولید الکترونیک، آزمایشگاه‌ها، تعمیرکاران حرفه‌ای و مدارس فنی."
  },
  {
    "slug": "weller",
    "name": "Weller",
    "persianName": "ولر",
    "established": "۱۹۴۵ (تأسیس شرکت)",
    "country": "آلمان/آمریکا (بخشی از Apex Tool Group)",
    "officialUrl": "https://www.weller-tools.com/",
    "companyIntroductions": [
      "Weller در سال ۱۹۴۵ توسط Carl E. Weller تأسیس شد و از پیشگامان صنعت لحیم‌کاری دستی در جهان است . این برند بخشی از Apex Tool Group است که بیش از ۷,۰۰۰ کارمند در بیش از ۳۰ کشور جهان دارد . Weller در زمینه راهکارهای لحیم‌کاری دستی، ایستگاه‌های لحیم‌کاری و بازکاری، واحدهای فیلتراسیون بخار و ابزارهای دقیق برش و انبر تخصص دارد .",
      "ولر از قدیمی‌ترین و شناخته‌شده‌ترین برندهای لحیم‌کاری در جهان است. کارل ولر در سال ۱۹۴۱ سلاح لحیم سریع‌گرم‌شونده را اختراع کرد و شرکت را چند سال بعد بنا نهاد. امروز ولر زیرمجموعه گروه Apex Tool Group است و ایستگاه‌های لحیم‌کاری حرفه‌ای آن در صنعت و آموزش به کار می‌روند."
    ],
    "specialization": "ایستگاه لحیم‌کاری و بازکاری، ابزار لحیم‌کاری دستی، واحدهای فیلتراسیون بخار، ابزارهای دقیق الکترونیک. Weller در رده قیمتی حرفه‌ای و بالا فعالیت می‌کند و در صنایع الکترونیک، خودروسازی و هوافضا کاربرد دارد.",
    "featuredProducts": "ایستگاه هویه و ابزار لحیم‌کاری.",
    "productFamilies": [
      "ایستگاه‌های دیجیتال لحیم‌کاری (WE، WT)",
      "هویه‌های کنترل دما و سلاح لحیم",
      "دی‌سولدر و هیتر هوای گرم",
      "سیستم‌های دودکش و فیلتر",
      "لوازم مصرفی و تیپ‌های متنوع"
    ],
    "strengths": [
      "اعتبار تاریخی و اکوسیستم کامل لوازم",
      "ایستگاه‌های حرفه‌ای ESD-safe",
      "تنوع بالا در تیپ و ابزار"
    ],
    "audience": "خطوط تولید، آزمایشگاه‌ها، تعمیرکاران و دانشجویان مهندسی برق.",
    "shoppingAdvice": "تجهیزات را بر اساس حجم و نوع کار انتخاب کنید. کنترل دما و سازگاری نوک را بسنجید. بررسی کنید پایه و اقلام ضروری در بسته موجود باشند.",
    "fullPastedText": "۱۲. ولر — Weller\nحوزه فعالیت: تجهیزات لحیم‌کاری حرفه‌ای\nسال تأسیس: ۱۹۴۵ (تأسیس شرکت)\nکشور مبدأ: آلمان/آمریکا (بخشی از Apex Tool Group)\nوب‌سایت رسمی: https://www.weller-tools.com/\n\nمعرفی شرکت\nWeller در سال ۱۹۴۵ توسط Carl E. Weller تأسیس شد و از پیشگامان صنعت لحیم‌کاری دستی در جهان است . این برند بخشی از Apex Tool Group است که بیش از ۷,۰۰۰ کارمند در بیش از ۳۰ کشور جهان دارد . Weller در زمینه راهکارهای لحیم‌کاری دستی، ایستگاه‌های لحیم‌کاری و بازکاری، واحدهای فیلتراسیون بخار و ابزارهای دقیق برش و انبر تخصص دارد .\n\nحوزه تخصصی\nایستگاه لحیم‌کاری و بازکاری، ابزار لحیم‌کاری دستی، واحدهای فیلتراسیون بخار، ابزارهای دقیق الکترونیک. Weller در رده قیمتی حرفه‌ای و بالا فعالیت می‌کند و در صنایع الکترونیک، خودروسازی و هوافضا کاربرد دارد.\n\nمحصولات شاخص\nایستگاه هویه و ابزار لحیم‌کاری.\n\nنکات خرید\nتجهیزات را'+N' بر اساس حجم و نوع کار انتخاب کنید. کنترل دما و سازگاری نوک را بسنجید. بررسی کنید پایه و اقلام ضروری در بسته موجود باشند.",
    "fullDocumentText": "ولر (Weller)\nنام رسمی شرکت | Weller (بخشی از Apex Tool Group)\nسال تأسیس | ۱۹۴۵ میلادی؛ توسط کارل ولر (اولین سلاح لحیم دوحالته ۱۹۴۱)\nمقر / کشور | ایستون، پنسیلوانیا (آمریکا)؛ بخش مهم در آلمان\nحوزه فعالیت | لحیم‌کاری، دی‌سولدر و ابزار تعمیر الکترونیک\nمحصولات شاخص | ایستگاه‌های WE/WT، هویه‌های WSD و WP، سلاح لحیم، سیستم‌های دود\nمعرفی\nولر از قدیمی‌ترین و شناخته‌شده‌ترین برندهای لحیم‌کاری در جهان است. کارل ولر در سال ۱۹۴۱ سلاح لحیم سریع‌گرم‌شونده را اختراع کرد و شرکت را چند سال بعد بنا نهاد. امروز ولر زیرمجموعه گروه Apex Tool Group است و ایستگاه‌های لحیم‌کاری حرفه‌ای آن در صنعت و آموزش به کار می‌روند.\nحوزه‌های فعالیت و محصولات\nایستگاه‌های دیجیتال لحیم‌کاری (WE، WT)\nهویه‌های کنترل دما و سلاح لحیم\nدی‌سولدر و هیتر هوای گرم\nسیستم‌های دودکش و فیلتر\nلوازم مصرفی و تیپ‌های متنوع\nنقاط قوت\nاعتبار تاریخی و اکوسیستم کامل لوازم\nایستگاه‌های حرفه‌ای ESD-safe\nتنوع بالا در تیپ و ابزار\nمخاطبان اصلی\nخطوط تولید، آزمایشگاه‌ها، تعمیرکاران و دانشجویان مهندسی برق."
  },
  {
    "slug": "philips",
    "name": "Philips",
    "persianName": "فیلیپس",
    "established": "۱۸۹۱ (شرکت مادر)",
    "country": "هلند",
    "officialUrl": "https://www.signify.com/",
    "companyIntroductions": [
      "فیلیپس یکی از شناخته‌شده‌ترین برندهای جهان در حوزه روشنایی است. بخش روشنایی فیلیپس که اکنون تحت شرکت Signify فعالیت می‌کند، در زمینه توسعه، تولید و کاربرد نورپردازی نوآورانه تخصص دارد . این شرکت در سال ۲۰۱۲ حدود ۳۲۵ میلیون یورو در نوآوری سبز سرمایه‌گذاری کرد و سهم محصولات LED از کل فروش آن به بیش از ۲۲٪ رسید . Signify در سال ۲۰۲۴ بیش از ۲۰,۰۰۰ پتنت ثبت‌شده دارد .",
      "فیلیپس ابتدا با تولید لامپ رشته‌ای آغاز کرد و در قرن بیستم به یکی از بزرگ‌ترین شرکت‌های الکترونیک جهان تبدیل شد. امروز تمرکز اصلی شرکت بر فناوری سلامت است. بخش روشنایی در سال ۲۰۱۶ به شرکت مستقل Signify (فیلیپس لایتینگ) تبدیل شد و برخی دسته‌های لوازم خانگی و صوتی/تصویری با مجوز نام فیلیپس توسط شرکت‌های دیگر ساخته می‌شود. در بازار ایران نام فیلیپس بیشتر با لامپ و روشنایی شناخته می‌شود."
    ],
    "specialization": "لامپ، تجهیزات روشنایی LED، سیستم‌های نورپردازی هوشمند و راهکارهای روشنایی حرفه‌ای برای فضاهای مسکونی، تجاری و صنعتی. محصولات فیلیپس در رده قیمتی متوسط تا بالا قرار می‌گیرند و از نظر کیفیت نور و دوام شناخته‌شده هستند.",
    "featuredProducts": "لامپ و تجهیزات روشنایی.",
    "productFamilies": [
      "فناوری سلامت: تصویربرداری، پایش بیمار، خواب و تنفس",
      "مراقبت شخصی: ریش‌تراش، مسواک برقی",
      "روشنایی (توسط Signify با نام فیلیپس): لامپ LED، چراغ‌های ساختمانی",
      "لوازم خانگی و صوتی/تصویری تحت مجوز برند"
    ],
    "strengths": [
      "اعتبار جهانی بیش از ۱۳۰ سال",
      "نوآوری قوی در سلامت و روشنایی",
      "شبکه توزیع و خدمات بین‌المللی"
    ],
    "audience": "مصرف‌کنندگان خانگی، پروژه‌های روشنایی و کاربران تجهیزات فنی.",
    "shoppingAdvice": "شدت نور و دمای رنگ را با محیط هماهنگ کنید. نوع سرپیچ و توان مصرفی را بررسی کنید. برای فضای کاری، پخش نور و دوام اهمیت دارد.",
    "fullPastedText": "۱۳. فیلیپس — Philips\nحوزه فعالیت: روشنایی و تجهیزات نورپردازی\nسال تأسیس: ۱۸۹۱ (شرکت مادر)\nکشور مبدأ: هلند\nوب‌سایت رسمی: https://www.signify.com/\n\nمعرفی شرکت\nفیلیپس یکی از شناخته‌شده‌ترین برندهای جهان در حوزه روشنایی است. بخش روشنایی فیلیپس که اکنون تحت شرکت Signify فعالیت می‌کند، در زمینه توسعه، تولید و کاربرد نورپردازی نوآورانه تخصص دارد . این شرکت در سال ۲۰۱۲ حدود ۳۲۵ میلیون یورو در نوآوری سبز سرم'+N'ایه‌گذاری کرد و سهم محصولات LED از کل فروش آن به بیش از ۲۲٪ رسید . Signify در سال ۲۰۲۴ بیش از ۲۰,۰۰۰ پتنت ثبت‌شده دارد .\n\nحوزه تخصصی\nلامپ، تجهیزات روشنایی LED، سیستم‌های نورپردازی هوشمند و راهکارهای روشنایی حرفه‌ای برای فضاهای مسکونی، تجاری و صنعتی. محصولات فیلیپس در رده قیمتی متوسط تا بالا قرار می‌گیرند و از نظر کیفیت نور و دوام شناخته‌شده هستند.\n\nمحصولات شاخص\nلامپ و تجهیزات روشنایی.\n\nنکات خرید\nشدت نور و دمای رنگ را با محیط هماهنگ کنید. نوع سرپیچ و توان مصرفی را بررسی کنید. برای فضای کاری، پخش نور و دوام اهمیت دارد.",
    "fullDocumentText": "فیلیپس (Philips)\nنام رسمی شرکت | Koninklijke Philips N.V.\nسال تأسیس | ۱۸۹۱ میلادی؛ توسط جرارد و آنتون فیلیپس\nمقر / کشور | آمستردام، هلند (آغاز در آیندهوون)\nحوزه فعالیت | فناوری سلامت، لوازم مراقبت شخصی؛ برند در روشنایی و لوازم خانگی با مجوز\nمحصولات شاخص | تجهیزات تصویربرداری پزشکی، مراقبت بیمار، ریش‌تراش و مراقبت شخصی؛ لامپ‌ها (توسط Signify)\nمعرفی\nفیلیپس ابتدا با تولید لامپ رشته‌ای آغاز کرد و در قرن بیستم به یکی از بزرگ‌ترین شرکت‌های الکترونیک جهان تبدیل شد. امروز تمرکز اصلی شرکت بر فناوری سلامت است. بخش روشنایی در سال ۲۰۱۶ به شرکت مستقل Signify (فیلیپس لایتینگ) تبدیل شد و برخی دسته‌های لوازم خانگی و صوتی/تصویری با مجوز نام فیلیپس توسط شرکت‌های دیگر ساخته می‌شود. در بازار ایران نام فیلیپس بیشتر با لامپ و روشنایی شناخته می‌شود.\nحوزه‌های فعالیت و محصولات\nفناوری سلامت: تصویربرداری، پایش بیمار، خواب و تنفس\nمراقبت شخصی: ریش‌تراش، مسواک برقی\nروشنایی (توسط Signify با نام فیلیپس): لامپ LED، چراغ‌های ساختمانی\nلوازم خانگی و صوتی/تصویری تحت مجوز برند\nنقاط قوت\nاعتبار جهانی بیش از ۱۳۰ سال\nنوآوری قوی در سلامت و روشنایی\nشبکه توزیع و خدمات بین‌المللی\nمخاطبان اصلی\nمراکز درمانی، مصرف‌کنندگان خانگی و پروژه‌های روشنایی.\nنکات پایانی\nاین سند بر پایه اطلاعات عمومی منتشرشده درباره برندها و جست‌وجوی وب تهیه شده است. برخی اطلاعات تأسیس برای برندهای کمتر مستند (مانند رونیکس، یوهوا و کوئیک) تقریبی یا ناقص است و در جدول‌ها با علامت «≈» یا عبارت مشابه مشخص شده است.\nپیش از استفاده رسمی (محصولات، سایت یا پیشنهاد تجاری)، اطلاعات حساس مانند سال تأسیس، مالکیت و مشخصات محصول را با وب‌سایت رسمی هر شرکت تطبیق دهید.\nتوجه: برخی برندهای هم‌نام وجود دارد (مانند Tosan در حوزه تجهیزات پزشکی نوزادان، Arva در تجهیزات ایمنی کوهستان فرانسه، و Mastech Digital در حوزه فناوری اطلاعات). در این سند منظور برندهای ابزار و تجهیزات ذکرشده در فهرست است."
  },
  {
    "slug": "wiha",
    "name": "Wiha",
    "persianName": "وایها",
    "established": "۱۹۳۹",
    "country": "آلمان",
    "officialUrl": "https://www.wihatools.com/",
    "companyIntroductions": [
      "Wiha در سال ۱۹۳۹ توسط Willi Hahn در ووپرتال آلمان تأسیس شد و یک کسب‌وکار خانوادگی نسل‌سوم محسوب می‌شود . این شرکت بیش از ۱,۴۰۰ کارمند در آلمان، لهستان، سوئیس و ویتنام دارد و بیش از ۳,۵۰۰ محصول عرضه می‌کند . Wiha اولین سازنده پیچ‌گوشتی‌های دقیق مدرن در جهان است و هر ابزار عایق آن به‌صورت انفرادی تا ۱۰,۰۰۰ ولت تست می‌شود . این برند اولین سازنده ابزار دستی است که نشان AGR برای دسته‌های بدون درد دریافت کرده است .",
      "وایها یکی از معتبرترین تولیدکنندگان پیچ‌گوشتی و ابزار دستی در آلمان است. این شرکت خانوادگی با تمرکز بر ارگونومی، دقت و ایمنی برق شناخته می‌شود. پیچ‌گوشتی‌ها و ابزار عایق‌دار تا ۱۰۰۰ ولت وایها در صنایع برق و خودرو پرکاربرد است."
    ],
    "specialization": "پیچ‌گوشتی دقیق، پیچ‌گوشتی و انبر عایق برق‌کاری (VDE)، ابزارهای دستی حرفه‌ای برای نصب و سرویس. Wiha در رده قیمتی بالا فعالیت می‌کند و ابزارهای آن برای کارهای دقیق برق‌کاری و الکترونیک ایده‌آل هستند.",
    "featuredProducts": "پیچ‌گوشتی و ابزار دستی برق‌کاری.",
    "productFamilies":'+N' [
      "پیچ‌گوشتی‌های ارگونومیک و ضربه‌ای"
    ],
    "strengths": [],
    "audience": "",
    "shoppingAdvice": "نوع سری و اندازه ابزار را با اتصال موردنظر تطبیق دهید. برای کار برق، عایق و رده ایمنی ابزار را بررسی کنید. فرم دسته و کنترل ابزار بر دقت کار اثر دارد.",
    "fullPastedText": "۱۴. وایها — Wiha\nحوزه فعالیت: ابزارهای دستی حرفه‌ای و دقیق، ابزار برق‌کاری عایق\nسال تأسیس: ۱۹۳۹\nکشور مبدأ: آلمان\nوب‌سایت رسمی: https://www.wihatools.com/\n\nمعرفی شرکت\nWiha در سال ۱۹۳۹ توسط Willi Hahn در ووپرتال آلمان تأسیس شد و یک کسب‌وکار خانوادگی نسل‌سوم محسوب می‌شود . این شرکت بیش از ۱,۴۰۰ کارمند در آلمان، لهستان، سوئیس و ویتنام دارد و بیش از ۳,۵۰۰ محصول عرضه می‌کند . Wiha اولین سازنده پیچ‌گوشتی‌های دقیق مدرن در جهان است و هر ابزار عایق آن به‌صورت انفرادی تا ۱۰,۰۰۰ ولت تست می‌شود . این برند اولین سازنده ابزار دستی است که نشان AGR برای دسته‌های بدون درد دریافت کرده است .\n\nحوزه تخصصی\nپیچ‌گوشتی دقیق، پیچ‌گوشتی و انبر عایق برق‌کاری (VDE)، ابزارهای دستی حرفه‌ای برای نصب و سرویس. Wiha در رده قیمتی بالا فعالیت می‌کند و ابزارهای آن برای کارهای دقیق برق‌کاری و الکترونیک ایده‌آل هستند.\n\nمحصولات شاخص\nپیچ‌گوشتی و ابزار دستی برق‌کاری.\n\nنکات خرید\nنوع سری و اندازه ابزار را با اتصال موردنظر تطبیق دهید. برای کار برق، عایق و رده ایمنی ابزار را بررسی کنید. فرم دسته و کنترل ابزار بر دقت کار اثر دارد.",
    "fullDocumentText": "وایها (Wiha)\nنام رسمی شرکت | Wiha Werkzeuge GmbH\nسال تأسیس | ۱۹۳۹ میلادی؛ توسط ویلی هان (نام Wiha از Willi Hahn)\nمقر / کشور | شونَخ (جنگل سیاه)، آلمان\nمالکیت | شرکت خانوادگی\nحوزه فعالیت | پیچ‌گوشتی و ابزار دستی تخصصی، ابزار عایق‌دار VDE\nمحصولات شاخص | پیچ‌گوشتی عایق‌دار ۱۰۰۰ ولت، slimFix، ست‌های دقیق، آچار گشتاور\nمعرفی\nوایها یکی از معتبرترین تولیدکنندگان پیچ‌گوشتی و ابزار دستی در آلمان است. این شرکت خانوادگی با تمرکز بر ارگونومی، دقت و ایمنی برق شناخته می‌شود. پیچ‌گوشتی‌ها و ابزار عایق‌دار تا ۱۰۰۰ ولت وایها در صنایع برق و خودرو پرکاربرد است.\nحوزه‌های فعالیت و محصولات\nپیچ‌گوشتی‌های ارگونومیک و ضربه‌ای\nابزار عایق‌دار VDE ۱۰۰۰ ولت (slimFix)\nپیچ‌گوشتی‌های دقیق برای الکترونیک\nآچار گشتاور و ست‌های ابزار\nجعبه‌ابزار و ست‌های تخصصی\nنقاط قوت\nایمنی بالا و استاندارد VDE\nارگونومی و کیفیت ساخت آلمانی\nابزار ویژه الکترونیک و برق‌کاری\nمخاطبان اصلی\nبرق‌کاران، تکنسین‌های الکترونیک و مکانیک‌های خودرو."
  },
  {
    "slug": "arva",
    "name": "Arva",
    "persianName": "آروا",
    "established": "حدود ۱۳۸۹ (۲۰۱۰ میلادی)",
    "country": "ایران",
    "officialUrl": "https://arvatools.com/",
    "companyIntroductions": [
      "آروا یک شرکت ایرانی است که با تمرکز بر تولید داخلی، به‌ویژه در بخش دستگاه‌های جوشکاری اینورتر، توانسته استاندارد بسیار بالایی را ثبت کند و بخش زیادی از ابزارهای برقی را نیز بومی‌سازی کند . این شرکت در سال ۱۴۰۲ کارخانه جدید خود را افتتاح کرد و رکورد تولید ۸۰۰ دستگاه اینورتر در روز را ثبت کرد . آروا در بازار ایران به‌عنوان یکی از شرکت‌های پیشرو در زمینه تولید ملی شناخته می‌شود و گارانتی‌های طولانی‌مدت و خدمات پس از فروش قوی از نقاط قوت آن است .",
      "آروا ابتدا یک مغازه ابزارفروشی بود که ابزار صنعتی می‌فروخت و سپس به شرکت بازرگانی تبدیل شد. مدیران شرکت تصمیم گرفتند به‌جای فروش صرف ابزار دیگران، سفارش‌دهنده تولید با مشخصات خود باشند و برند آروا را ثبت کردند. بعدها شرکت کارخانه تولیدی راه‌اندازی کرد و به‌عنوان شرکت دانش‌بنیان فعالیت می‌کند. آروا خود را حامی تولید ملی و اشتغال‌زایی معرفی می‌کند و گزارش‌ها از تیمی چند صدنفره خبر می‌دهند."
    ],
    "specialization": "اینورتر جوشکاری، ابزارهای برقی، ابزارهای دستی. آروا به‌ویژه در بخش اینورتر جوشکاری سهم بزرگی از بازار ایران را در اختیار دارد'+N' و برای جوشکاران حرفه‌ای، آهنگران و افرادی که دوام و خدمات پس از فروش را در اولویت قرار می‌دهند، گزینه‌ای مطمئن است .",
    "featuredProducts": "ابزارهای دستی و تجهیزات کارگاهی، اینورتر جوشکاری (مدل‌هایی مانند Arc 160).",
    "productFamilies": [
      "ابزار برقی: فرز، مینی‌فرز، دریل و غیره",
      "اینورتر و دستگاه‌های جوشکاری",
      "جعبه‌ابزار و ابزار دستی",
      "متعلقات ابزار",
      "آزمایشگاه تست کیفیت ابزار برقی و جوش"
    ],
    "strengths": [
      "تولید داخلی و پشتیبانی از تولید ملی",
      "قیمت پایین‌تر نسبت به نمونه‌های خارجی",
      "شعار «ابزار موفقیت» با تمرکز بر کیفیت و خدمات پس از فروش"
    ],
    "audience": "کارگاه‌ها، فروشندگان ابزار و کاربران خانگی.",
    "shoppingAdvice": "گروه محصول را با کاربرد اصلی خود هماهنگ کنید. ابعاد، جنس و ظرفیت ابزار را بررسی کنید. شرایط ضمانت و اقلام همراه را پیش از خرید ببینید.",
    "fullPastedText": "۱۵. آروا — Arva\nحوزه فعالیت: ابزارهای برقی، اینورتر جوشکاری، ابزار دستی\nسال تأسیس: حدود ۱۳۸۹ (۲۰۱۰ میلادی)\nکشور مبدأ: ایران\nوب‌سایت رسمی: https://arvatools.com/\n\nمعرفی شرکت\nآروا یک شرکت ایرانی است که با تمرکز بر تولید داخلی، به‌ویژه در بخش دستگاه‌های جوشکاری اینورتر، توانسته استاندارد بسیار بالایی را ثبت کند و بخش زیادی از ابزارهای برقی را نیز بومی‌سازی کند . این شرکت در سال ۱۴۰۲ کارخانه جدید خود را افتتاح کرد و رکورد تولید ۸۰۰ دستگاه اینورتر در روز را ثبت کرد . آروا در بازار ایران به‌عنوان یکی از شرکت‌های پیشرو در زمینه تولید ملی شناخته می‌شود و گارانتی‌های طولانی‌مدت و خدمات پس از فروش قوی از نقاط قوت آن است .\n\nحوزه تخصصی\nاینورتر جوشکاری، ابزارهای برقی، ابزارهای دستی. آروا به‌ویژه در بخش اینورتر جوشکاری سهم بزرگی از بازار ایران را در اختیار دارد و برای جوشکاران حرفه‌ای، آهنگران و افرادی که دوام و خدمات پس از فروش را در اولویت قرار می‌دهند، گزینه‌ای مطمئن است .\n\nمحصولات شاخص\nابزارهای دستی و تجهیزات کارگاهی، اینورتر جوشکاری (مدل‌هایی مانند Arc 160).\n\nنکات خرید\nگروه محصول را با کاربرد اصلی خود هماهنگ کنید. ابعاد، جنس و ظرفیت ابزار را بررسی کنید. شرایط ضمانت و اقلام همراه را پیش از خرید ببینید.",
    "fullDocumentText": "آروا (Arva)\nنام رسمی شرکت | شرکت دانش‌بنیان آروا (Arva)\nثبت برند | ۱۳۸۸ شمسی (ثبت برند با هدف رضایت مشتری)\nمقر / کشور | ایران\nحوزه فعالیت | تولید و توزیع ابزارآلات برقی، دستی و جوشکاری\nمحصولات شاخص | فرز و مینی‌فرز، ابزار برقی، اینورتر جوشکاری، جعبه‌ابزار\nمعرفی\nآروا ابتدا یک مغازه ابزارفروشی بود که ابزار صنعتی می‌فروخت و سپس به شرکت بازرگانی تبدیل شد. مدیران شرکت تصمیم گرفتند به‌جای فروش صرف ابزار دیگران، سفارش‌دهنده تولید با مشخصات خود باشند و برند آروا را ثبت کردند. بعدها شرکت کارخانه تولیدی راه‌اندازی کرد و به‌عنوان شرکت دانش‌بنیان فعالیت می‌کند. آروا خود را حامی تولید ملی و اشتغال‌زایی معرفی می‌کند و گزارش‌ها از تیمی چند صدنفره خبر می‌دهند.\nحوزه‌های فعالیت و محصولات\nابزار برقی: فرز، مینی‌فرز، دریل و غیره\nاینورتر و دستگاه‌های جوشکاری\nجعبه‌ابزار و ابزار دستی\nمتعلقات ابزار\nآزمایشگاه تست کیفیت ابزار برقی و جوش\nنقاط قوت\nتولید داخلی و پشتیبانی از تولید ملی\nقیمت پایین‌تر نسبت به نمونه‌های خارجی\nشعار «ابزار موفقیت» با تمرکز بر کیفیت و خدمات پس از فروش\nمخاطبان اصلی\nبازار داخلی ایران؛ کارگاه‌ها، صنف ابزار و کاربران خانگی.\n2. تجهیزات اندازه‌گیری و تست الکترونیکی"
  },
  {
    "slug": "victor",
    "name": "Victor",
    "persianName": "ویکتور",
    "established": "۱۹۹۸",
    "country": "چین",
    "officialUrl": "http://www.victor-ht.com/",
    "companyIntroductions": [
      "شرکت Shenzhen Victor Hi-Tech در سال ۱۹۹۸ تأسیس شد و در زمینه تحقیق، توسعه، تولید و فروش ابزارهای اندازه‌گیری الکتریکی فعالیت می‌کند . این شرک'+N'ت محصولات خود را تحت برندهای VC، VICTOR و DM عرضه می‌کند و ۴۷ پتنت و ۶ حق ثبت نرم‌افزار دارد . محصولات VICTOR در بیش از ۱۰۰ کشور جهان عرضه می‌شوند و این شرکت در سال ۱۹۹۹ گواهی ISO9001 را از TUV آلمان دریافت کرد .",
      "ویکتور (VICTOR) برند چینی ابزار اندازه‌گیری است که از دهه ۱۹۸۰ در بازار چین عرضه شده و شبکه نمایندگی گسترده‌ای دارد. طیف محصولات آن شامل مولتی‌مترهای دیجیتال و آنالوگ، کلمپ‌مترها، مگراهم‌مترها، ترمومترها، دورسنج‌ها و اسیلوسکوپ‌هاست. مدل‌هایی مانند VC97 و VC9808+ در بازار جهانی رایج‌اند."
    ],
    "specialization": "مولتی‌متر دیجیتال، مولتی‌متر رومیزی، اسیلوسکوپ، کالیبراتور فرآیند، تستر عایق و ابزارهای اندازه‌گیری الکتریکی. VICTOR در رده قیمتی اقتصادی تا متوسط فعالیت می‌کند و گزینه‌ای مناسب برای کارگاه‌های عمومی، آموزش و مصارف خانگی است.",
    "featuredProducts": "تستر و تجهیزات برق‌کاری.",
    "productFamilies": [
      "مولتی‌مترهای رومیزی و قابل‌حمل (VC97، VC86D، VC8045)",
      "کلمپ‌مترها",
      "مگراهم‌متر و ترمومتر",
      "دورسنج، لوکس‌متر و صوت‌سنج",
      "اسیلوسکوپ و مولد تابع"
    ],
    "strengths": [
      "قیمت اقتصادی و مناسب آموزش",
      "تنوع زیاد مدل‌ها",
      "سابقه چند دهه‌ای در بازار چین"
    ],
    "audience": "دانشجویان، تعمیرکاران، آزمایشگاه‌های آموزشی و کاربران مبتدی تا متوسط.",
    "shoppingAdvice": "نوع آزمون یا اندازه‌گیری موردنیاز را مشخص کنید. محدوده کاری و رده ایمنی را بررسی کنید. لوازم جانبی سازگار را در انتخاب لحاظ کنید.",
    "fullPastedText": "۱۶. ویکتور — Victor\nحوزه فعالیت: ابزارهای اندازه‌گیری و تست الکتریکی\nسال تأسیس: ۱۹۹۸\nکشور مبدأ: چین\nوب‌سایت رسمی: http://www.victor-ht.com/\n\nمعرفی شرکت\nشرکت Shenzhen Victor Hi-Tech در سال ۱۹۹۸ تأسیس شد و در زمینه تحقیق، توسعه، تولید و فروش ابزارهای اندازه‌گیری الکتریکی فعالیت می‌کند . این شرکت محصولات خود را تحت برندهای VC، VICTOR و DM عرضه می‌کند و ۴۷ پتنت و ۶ حق ثبت نرم‌افزار دارد . محصولات VICTOR در بیش از ۱۰۰ کشور جهان عرضه می‌شوند و این شرکت در سال ۱۹۹۹ گواهی ISO9001 را از TUV آلمان دریافت کرد .\n\nحوزه تخصصی\nمولتی‌متر دیجیتال، مولتی‌متر رومیزی، اسیلوسکوپ، کالیبراتور فرآیند، تستر عایق و ابزارهای اندازه‌گیری الکتریکی. VICTOR در رده قیمتی اقتصادی تا متوسط فعالیت می‌کند و گزینه‌ای مناسب برای کارگاه‌های عمومی، آموزش و مصارف خانگی است.\n\nمحصولات شاخص\nتستر و تجهیزات برق‌کاری.\n\nنکات خرید\nنوع آزمون یا اندازه‌گیری موردنیاز را مشخص کنید. محدوده کاری و رده ایمنی را بررسی کنید. لوازم جانبی سازگار را در انتخاب لحاظ کنید.",
    "fullDocumentText": "ویکتور (Victor)\nنام رسمی شرکت | Shenzhen Victor Hi-Tech Co., Ltd. / Double King Industrial Holdings\nسابقه | عرضه محصولات از دهه ۱۹۸۰ میلادی\nمقر / کشور | شنژن، چین\nحوزه فعالیت | تجهیزات اندازه‌گیری و تست الکترونیک\nمحصولات شاخص | مولتی‌متر آنالوگ و دیجیتال، کلمپ‌متر، مگراهم‌متر، اسیلوسکوپ، ترمومتر\nمعرفی\nویکتور (VICTOR) برند چینی ابزار اندازه‌گیری است که از دهه ۱۹۸۰ در بازار چین عرضه شده و شبکه نمایندگی گسترده‌ای دارد. طیف محصولات آن شامل مولتی‌مترهای دیجیتال و آنالوگ، کلمپ‌مترها، مگراهم‌مترها، ترمومترها، دورسنج‌ها و اسیلوسکوپ‌هاست. مدل‌هایی مانند VC97 و VC9808+ در بازار جهانی رایج‌اند.\nحوزه‌های فعالیت و محصولات\nمولتی‌مترهای رومیزی و قابل‌حمل (VC97، VC86D، VC8045)\nکلمپ‌مترها\nمگراهم‌متر و ترمومتر\nدورسنج، لوکس‌متر و صوت‌سنج\nاسیلوسکوپ و مولد تابع\nنقاط قوت\nقیمت اقتصادی و مناسب آموزش\nتنوع زیاد مدل‌ها\nسابقه چند دهه‌ای در بازار چین\nمخاطبان اصلی\nدانشجویان، تعمیرکاران، آزمایشگاه‌های آموزشی و کاربران مبتدی تا متوسط."
  },
  {
    "slug": "quick",
    "name": "Quick",
    "persianName": "کوئیک",
    "established": "۲۰۰۶",
    "country": "چین",
    "officialUrl'+N'": "https://en.quick-global.com/",
    "companyIntroductions": [
      "Quick Intelligent Equipment یک شرکت سهامی عام چینی است که در سال ۲۰۰۶ تأسیس شد و در بورس شانگهای با نماد ۶۰۳۲۰۳ معامله می‌شود . این شرکت ۱,۴۷۸ کارمند دارد و درآمد سالانه آن در سال ۲۰۲۶ حدود ۱۷۷ میلیون دلار بوده است . Quick در دو خط محصول اصلی فعالیت می‌کند: تجهیزات هوشمند (ربات‌های جوشکاری، تجهیزات جوشکاری خودکار، ایستگاه‌های لحیم‌کاری هوشمند) و یکپارچه‌سازی سیستم‌ها (جوشکاری خودکار، توزیع، مونتاژ، بازرسی بینایی AOI) . این شرکت مشتریان گسترده‌ای در صنایع الکترونیک ۳C، خودرو، پزشکی، خانه هوشمند و لیتیوم دارد .",
      "کوئیک (Quick) برند چینی تجهیزات لحیم‌کاری و ریورک است که با هیترهای هوای گرم و ایستگاه‌های لحیم در میان تعمیرکاران برد و موبایل شناخته می‌شود. این برند طیف متنوعی از دستگاه‌های اقتصادی تا حرفه‌ای عرضه می‌کند. اطلاعات رسمی دقیق از تاریخچه شرکت در منابع عمومی محدود است."
    ],
    "specialization": "ایستگاه لحیم‌کاری هوشمند، ایستگاه هیتر هوای گرم، ربات‌های جوشکاری، تجهیزات مونتاژ و بازرسی خودکار. Quick در رده قیمتی متوسط تا بالا فعالیت می‌کند و گزینه‌ای حرفه‌ای برای خطوط تولید و کارگاه‌های تخصصی الکترونیک است.",
    "featuredProducts": "هیتر، هویه و تجهیزات تعمیر برد.",
    "productFamilies": [
      "هیتر هوای گرم دیجیتال (861DW، 957D+)",
      "ایستگاه‌های لحیم‌کاری",
      "ایستگاه‌های ریورک SMD و BGA",
      "هویه، فن دود و لوازم مصرفی"
    ],
    "strengths": [
      "توان بالای هیتر هوای گرم",
      "قیمت مناسب نسبت به برندهای ژاپنی/آلمانی",
      "حضور پررنگ در بازارهای تعمیر موبایل"
    ],
    "audience": "تعمیرکاران موبایل، لپ‌تاپ و برد الکترونیک، و کارگاه‌های متوسط.",
    "shoppingAdvice": "تجهیزات را متناسب با نوع قطعه و حجم کار انتخاب کنید. کنترل دما و جریان هوا را بررسی کنید. نازل‌ها و لوازم مصرفی باید متناسب با دستگاه باشند.",
    "fullPastedText": "۱۷. کوئیک — Quick\nحوزه فعالیت: تجهیزات مونتاژ و تعمیر بردهای الکترونیکی\nسال تأسیس: ۲۰۰۶\nکشور مبدأ: چین\nوب‌سایت رسمی: https://en.quick-global.com/\n\nمعرفی شرکت\nQuick Intelligent Equipment یک شرکت سهامی عام چینی است که در سال ۲۰۰۶ تأسیس شد و در بورس شانگهای با نماد ۶۰۳۲۰۳ معامله می‌شود . این شرکت ۱,۴۷۸ کارمند دارد و درآمد سالانه آن در سال ۲۰۲۶ حدود ۱۷۷ میلیون دلار بوده است . Quick در دو خط محصول اصلی فعالیت می‌کند: تجهیزات هوشمند (ربات‌های جوشکاری، تجهیزات جوشکاری خودکار، ایستگاه‌های لحیم‌کاری هوشمند) و یکپارچه‌سازی سیستم‌ها (جوشکاری خودکار، توزیع، مونتاژ، بازرسی بینایی AOI) . این شرکت مشتریان گسترده‌ای در صنایع الکترونیک ۳C، خودرو، پزشکی، خانه هوشمند و لیتیوم دارد .\n\nحوزه تخصصی\nایستگاه لحیم‌کاری هوشمند، ایستگاه هیتر هوای گرم، ربات‌های جوشکاری، تجهیزات مونتاژ و بازرسی خودکار. Quick در رده قیمتی متوسط تا بالا فعالیت می‌کند و گزینه‌ای حرفه‌ای برای خطوط تولید و کارگاه‌های تخصصی الکترونیک است.\n\nمحصولات شاخص\nهیتر، هویه و تجهیزات تعمیر برد.\n\nنکات خرید\nتجهیزات را متناسب با نوع قطعه و حجم کار انتخاب کنید. کنترل دما و جریان هوا را بررسی کنید. نازل‌ها و لوازم مصرفی باید متناسب با دستگاه باشند.",
    "fullDocumentText": "کوئیک (Quick)\nنام برند | QUICK (کوئیک)\nمقر / کشور | چین\nحوزه فعالیت | ایستگاه‌های لحیم‌کاری و ریورک (SMD/BGA)\nمحصولات شاخص | هیتر هوای گرم (مثل 861DW و 957D+)، ایستگاه لحیم، دستگاه‌های ریورک، منابع تغذیه\nمعرفی\nکوئیک (Quick) برند چینی تجهیزات لحیم‌کاری و ریورک است که با هیترهای هوای گرم و ایستگاه‌های لحیم در میان تعمیرکاران برد و موبایل شناخته می‌شود. این برند طیف متنوعی از دستگاه‌های اقتصادی تا حرفه‌ای عرضه می‌کند. اطلاعات رسمی دقیق از تاریخچه شرکت در منابع عمومی محدود است.\nحوزه‌های فعالیت و محصولات\nهیتر هوای گرم دیجیتال (861DW، 957D+)\nایستگاه‌ها'+N'ی لحیم‌کاری\nایستگاه‌های ریورک SMD و BGA\nهویه، فن دود و لوازم مصرفی\nنقاط قوت\nتوان بالای هیتر هوای گرم\nقیمت مناسب نسبت به برندهای ژاپنی/آلمانی\nحضور پررنگ در بازارهای تعمیر موبایل\nمخاطبان اصلی\nتعمیرکاران موبایل، لپ‌تاپ و برد الکترونیک، و کارگاه‌های متوسط."
  },
  {
    "slug": "goot",
    "name": "GOOT",
    "persianName": "گوت",
    "established": "۱۹۶۵",
    "country": "ژاپن",
    "officialUrl": "http://www.goot.cn/",
    "companyIntroductions": [
      "شرکت Taiyo Electric Ind. (برند GOOT) در آوریل ۱۹۶۵ در هیروشیما، ژاپن تأسیس شد و در سال ۲۰۲۵ شصتمین سالگرد تأسیس خود را جشن گرفت . این شرکت از تولیدکنندگان تخصصی ابزارهای لحیم‌کاری است و تنها تولیدکننده‌ای در جهان است که می‌تواند تمام سری نوک هویه‌های خود را به‌طور کامل و مستقل تولید کند . GOOT در سال ۲۰۰۳ گواهی ISO 9001:2000 دریافت کرد و محصولاتش شامل هویه‌های KS و TQ (که به محصولات پرفروش طولانی‌مدت تبدیل شدند)، ایستگاه‌های دمای قابل تنظیم PX و RX و مدل‌های پرقدرت ۱۵۰ وات است .",
      "گوت (GOOT) نام تجاری شرکت ژاپنی Taiyo Electric Industry است. این برند به‌ویژه با هویه‌ها، ایستگاه‌های لحیم‌کاری و سیم‌لحیم‌کش (سولدر ویک) شناخته می‌شود و در ژاپن و بازارهای جهانی برای تعمیرات الکترونیک و کارهای ظریف مورد استفاده است."
    ],
    "specialization": "هویه، ایستگاه لحیم‌کاری، نوک هویه، ابزارهای لحیم‌کاری دستی. GOOT در رده قیمتی متوسط فعالیت می‌کند و به‌ویژه در بازار ژاپن و آسیا شناخته‌شده است.",
    "featuredProducts": "هویه و لوازم لحیم‌کاری.",
    "productFamilies": [
      "هویه و ایستگاه لحیم‌کاری"
    ],
    "strengths": [],
    "audience": "",
    "shoppingAdvice": "توان و فرم هویه را با نوع کار تطبیق دهید. سازگاری نوک و قطعات مصرفی را بررسی کنید. برای کار دقیق، کنترل دما و ارگونومی را بسنجید.",
    "fullPastedText": "۱۸. گوت — GOOT\nحوزه فعالیت: ابزارهای لحیم‌کاری و تعمیر الکترونیک\nسال تأسیس: ۱۹۶۵\nکشور مبدأ: ژاپن\nوب‌سایت رسمی: http://www.goot.cn/\n\nمعرفی شرکت\nشرکت Taiyo Electric Ind. (برند GOOT) در آوریل ۱۹۶۵ در هیروشیما، ژاپن تأسیس شد و در سال ۲۰۲۵ شصتمین سالگرد تأسیس خود را جشن گرفت . این شرکت از تولیدکنندگان تخصصی ابزارهای لحیم‌کاری است و تنها تولیدکننده‌ای در جهان است که می‌تواند تمام سری نوک هویه‌های خود را به‌طور کامل و مستقل تولید کند . GOOT در سال ۲۰۰۳ گواهی ISO 9001:2000 دریافت کرد و محصولاتش شامل هویه‌های KS و TQ (که به محصولات پرفروش طولانی‌مدت تبدیل شدند)، ایستگاه‌های دمای قابل تنظیم PX و RX و مدل‌های پرقدرت ۱۵۰ وات است .\n\nحوزه تخصصی\nهویه، ایستگاه لحیم‌کاری، نوک هویه، ابزارهای لحیم‌کاری دستی. GOOT در رده قیمتی متوسط فعالیت می‌کند و به‌ویژه در بازار ژاپن و آسیا شناخته‌شده است.\n\nمحصولات شاخص\nهویه و لوازم لحیم‌کاری.\n\nنکات خرید\nتوان و فرم هویه را با نوع کار تطبیق دهید. سازگاری نوک و قطعات مصرفی را بررسی کنید. برای کار دقیق، کنترل دما و ارگونومی را بسنجید.",
    "fullDocumentText": "گوت (GOOT)\nنام رسمی شرکت | Taiyo Electric Ind. Co., Ltd. (نام تجاری GOOT)\nمقر / کشور | هیروشیما، ژاپن\nحوزه فعالیت | لحیم‌کاری و لوازم جانبی الکترونیک\nمحصولات شاخص | هویه و ایستگاه، سیم‌لحیم‌کش (سولدر ویک)، هیتر هوای گرم، فلاکس، تمیزکننده\nمعرفی\nگوت (GOOT) نام تجاری شرکت ژاپنی Taiyo Electric Industry است. این برند به‌ویژه با هویه‌ها، ایستگاه‌های لحیم‌کاری و سیم‌لحیم‌کش (سولدر ویک) شناخته می‌شود و در ژاپن و بازارهای جهانی برای تعمیرات الکترونیک و کارهای ظریف مورد استفاده است.\nحوزه‌های فعالیت و محصولات\nهویه و ایستگاه لحیم‌کاری\nسیم‌لحیم‌کش (Solder Wick)\nهیتر هوای گرم\nفلاکس و پاک‌کننده\nپنس‌های مخصوص SMD و لوازم جانبی\nنقاط قوت\nساخت ژاپنی و لوازم مصرفی باکیفیت\nمناسب کار دقیق و ظریف\nبازار گسترده لوازم لحیم‌کاری\nمخاطبان اصلی\nتعمیرکاران الکترونیک، ع'+N'لاقه‌مندان، آموزشگاه‌ها و کارگاه‌های تعمیر.\n4. ابزار دستی تخصصی"
  },
  {
    "slug": "knipex",
    "name": "Knipex",
    "persianName": "کنیپکس",
    "established": "۱۸۸۲",
    "country": "آلمان",
    "officialUrl": "https://www.knipex.com/",
    "companyIntroductions": [
      "کنیپکس در سال ۱۸۸۲ به‌عنوان یک آهنگری کوچک روستایی در آلمان تأسیس شد و امروزه به‌عنوان تولیدکننده پیشرو انبر حرفه‌ای در جهان شناخته می‌شود . این شرکت بیش از ۱۴۰ سال سابقه دارد و امروزه روزانه ۵۹,۰۰۰ انبر تولید می‌کند که به بیش از ۱۰۰ کشور جهان صادر می‌شود . کنیپکس در ووپرتال آلمان حدود ۱,۸۰۰ کارمند دارد و محصولات آن استاندارد جهانی در صنعت انبر محسوب می‌شوند .",
      "کنیپکس شرکت آلمانی با بیش از ۱۴۰ سال سابقه است و تقریباً تمام تمرکزش بر انبردست و ابزارهای مرتبط است. انبر Cobra و انبر آچاری کنیپکس از نوآوری‌های مشهور آن‌اند. ابزارهای عایق‌دار VDE این برند در صنعت برق بسیار مورد اعتماد است."
    ],
    "specialization": "انبر ترکیبی، انبر آب‌بند (Cobra)، سیم‌چین، ابزار پرس و لخت‌کردن سیم، ابزارهای برق‌کاری عایق. کنیپکس در رده قیمتی بالا فعالیت می‌کند و انتخاب اول برقکاران حرفه‌ای، صنعتگران و کاربران فنی است که به دنبال ابزار با دوام و دقیق هستند.",
    "featuredProducts": "انبر، سیم‌چین و ابزار دستی برق‌کاری.",
    "productFamilies": [
      "انبرهای قابل تنظیم Cobra",
      "انبر آچاری برای مهره و پیچ",
      "انبرها و سیم‌چین‌های عایق‌دار VDE",
      "سیم‌لخت‌کن و ابزار پرس سر سیم",
      "قیچی برش کابل و ابزار تخصصی"
    ],
    "strengths": [
      "دقت و دوام ساخت در انبرها",
      "طراحی‌های نوآورانه و ثبت‌شده",
      "تولید در آلمان و مطابقت با استانداردهای ایمنی"
    ],
    "audience": "برق‌کاران، مکانیک‌ها، لوله‌کش‌ها و صنعتگران حرفه‌ای.",
    "shoppingAdvice": "نوع اتصال یا سیم را پیش از انتخاب ابزار مشخص کنید. ظرفیت برش و دهانه ابزار را بررسی کنید. برای برق‌کاری، ایمنی و عایق ابزار را جدی بگیرید.",
    "fullPastedText": "۱۹. کنیپکس — Knipex\nحوزه فعالیت: انبر، سیم‌چین و ابزار دستی برق‌کاری\nسال تأسیس: ۱۸۸۲\nکشور مبدأ: آلمان\nوب‌سایت رسمی: https://www.knipex.com/\n\nمعرفی شرکت\nکنیپکس در سال ۱۸۸۲ به‌عنوان یک آهنگری کوچک روستایی در آلمان تأسیس شد و امروزه به‌عنوان تولیدکننده پیشرو انبر حرفه‌ای در جهان شناخته می‌شود . این شرکت بیش از ۱۴۰ سال سابقه دارد و امروزه روزانه ۵۹,۰۰۰ انبر تولید می‌کند که به بیش از ۱۰۰ کشور جهان صادر می‌شود . کنیپکس در ووپرتال آلمان حدود ۱,۸۰۰ کارمند دارد و محصولات آن استاندارد جهانی در صنعت انبر محسوب می‌شوند .\n\nحوزه تخصصی\nانبر ترکیبی، انبر آب‌بند (Cobra)، سیم‌چین، ابزار پرس و لخت‌کردن سیم، ابزارهای برق‌کاری عایق. کنیپکس در رده قیمتی بالا فعالیت می‌کند و انتخاب اول برقکاران حرفه‌ای، صنعتگران و کاربران فنی است که به دنبال ابزار با دوام و دقیق هستند.\n\nمحصولات شاخص\nانبر، سیم‌چین و ابزار دستی برق‌کاری.\n\nنکات خرید\nنوع اتصال یا سیم را پیش از انتخاب ابزار مشخص کنید. ظرفیت برش و دهانه ابزار را بررسی کنید. برای برق‌کاری، ایمنی و عایق ابزار را جدی بگیرید.",
    "fullDocumentText": "کنیپکس (Knipex)\nنام رسمی شرکت | KNIPEX-Werk C. Gustav Putsch KG\nسال تأسیس | ۱۸۸۲ میلادی؛ توسط سی. گوستاو پوتش\nمقر / کشور | ووپرتال، آلمان\nمالکیت | شرکت خانوادگی (خانواده Putsch)\nحوزه فعالیت | انبردست‌ها و ابزار برش و گیره‌ای\nمحصولات شاخص | انبر Cobra، انبر آچاری (Pliers Wrench)، انبر عایق‌دار VDE، سیم‌چین، سیم‌لخت‌کن\nمعرفی\nکنیپکس شرکت آلمانی با بیش از ۱۴۰ سال سابقه است و تقریباً تمام تمرکزش بر انبردست و ابزارهای مرتبط است. انبر Cobra و انبر آچاری کنیپکس از نوآوری‌های مشهور آن‌اند. ابزارهای عایق‌دار VDE این برند در صنعت برق بسیار مورد اعتماد است.\nحوزه‌های فعالیت و محصولات\nانبرهای قابل تنظیم Cobra\nانبر آچاری برای'+N' مهره و پیچ\nانبرها و سیم‌چین‌های عایق‌دار VDE\nسیم‌لخت‌کن و ابزار پرس سر سیم\nقیچی برش کابل و ابزار تخصصی\nنقاط قوت\nدقت و دوام ساخت در انبرها\nطراحی‌های نوآورانه و ثبت‌شده\nتولید در آلمان و مطابقت با استانداردهای ایمنی\nمخاطبان اصلی\nبرق‌کاران، مکانیک‌ها، لوله‌کش‌ها و صنعتگران حرفه‌ای."
  },
  {
    "slug": "stanley",
    "name": "Stanley",
    "persianName": "استنلی",
    "established": "۱۸۴۳",
    "country": "آمریکا",
    "officialUrl": "https://www.stanleytools.global/",
    "companyIntroductions": [
      "استنلی یکی از قدیمی‌ترین و شناخته‌شده‌ترین برندهای ابزار دستی در جهان است که از سال ۱۸۴۳ فعالیت می‌کند. این برند طیف گسترده‌ای از ابزارهای دستی، اندازه‌گیری و ابزار عمومی را برای مصارف خانگی، کارگاهی و حرفه‌ای عرضه می‌کند. محصولات استنلی در رده قیمتی متوسط تا بالا قرار می‌گیرند و در بازار ایران نیز حضور گسترده‌ای دارند.",
      "استنلی یکی از قدیمی‌ترین برندهای ابزار دستی در جهان است که از قرن نوزدهم در ایالات متحده فعالیت می‌کند. این برند با رنگ زرد و مشکی و محصولاتی مانند متر PowerLock و سری FATMAX شناخته می‌شود. امروز استنلی بخشی از شرکت Stanley Black & Decker است که دیوالت را نیز در اختیار دارد."
    ],
    "specialization": "ابزارهای دستی (چکش، پیچ‌گوشتی، انبردست، آچار)، ابزارهای اندازه‌گیری (متر، تراز، گونیا)، ست‌های ابزار عمومی. استنلی گزینه‌ای مناسب برای کارهای عمومی، دکوراسیون و مصارف روزمره کارگاهی است.",
    "featuredProducts": "ابزار دستی و اندازه‌گیری.",
    "productFamilies": [
      "متر و ابزار اندازه‌گیری (PowerLock، تراز)",
      "آچارآلات، انبرها و پیچ‌گوشتی‌ها",
      "چکش‌ها و ابزار تخریب",
      "جعبه‌ابزار و سیستم ذخیره‌سازی",
      "کاتر و ابزار برش"
    ],
    "strengths": [
      "اعتبار تاریخی و شبکه توزیع جهانی",
      "تنوع بالا برای کاربران خانگی و حرفه‌ای",
      "قیمت متعادل و دسترسی آسان"
    ],
    "audience": "کاربران خانگی، فعالان ساخت‌وساز، نجاران و صنعتگران.",
    "shoppingAdvice": "ابزار را بر اساس دقت و محیط استفاده انتخاب کنید. کیفیت ساخت و خوانایی مقیاس را بررسی کنید. ابعاد و قابلیت حمل ابزار را هم در نظر بگیرید.",
    "fullPastedText": "۲۰. استنلی — Stanley\nحوزه فعالیت: ابزار دستی، اندازه‌گیری و ابزار عمومی\nسال تأسیس: ۱۸۴۳\nکشور مبدأ: آمریکا\nوب‌سایت رسمی: https://www.stanleytools.global/\n\nمعرفی شرکت\nاستنلی یکی از قدیمی‌ترین و شناخته‌شده‌ترین برندهای ابزار دستی در جهان است که از سال ۱۸۴۳ فعالیت می‌کند. این برند طیف گسترده‌ای از ابزارهای دستی، اندازه‌گیری و ابزار عمومی را برای مصارف خانگی، کارگاهی و حرفه‌ای عرضه می‌کند. محصولات استنلی در رده قیمتی متوسط تا بالا قرار می‌گیرند و در بازار ایران نیز حضور گسترده‌ای دارند.\n\nحوزه تخصصی\nابزارهای دستی (چکش، پیچ‌گوشتی، انبردست، آچار)، ابزارهای اندازه‌گیری (متر، تراز، گونیا)، ست‌های ابزار عمومی. استنلی گزینه‌ای مناسب برای کارهای عمومی، دکوراسیون و مصارف روزمره کارگاهی است.\n\nمحصولات شاخص\nابزار دستی و اندازه‌گیری.\n\nنکات خرید\nابزار را بر اساس دقت و محیط استفاده انتخاب کنید. کیفیت ساخت و خوانایی مقیاس را بررسی کنید. ابعاد و قابلیت حمل ابزار را هم در نظر بگیرید.",
    "fullDocumentText": "استنلی (Stanley)\nنام رسمی شرکت | Stanley (بخشی از Stanley Black & Decker)\nسال تأسیس | ۱۸۴۳ میلادی؛ توسط فردریک استنلی\nمقر / کشور | نیوبریتن، کانکتیکات، ایالات متحده\nمالکیت | ادغام با Black & Decker در ۲۰۱۰؛ شرکت سهامی عام\nحوزه فعالیت | ابزار دستی، ابزار اندازه‌گیری، ذخیره‌سازی ابزار\nمحصولات شاخص | متر (PowerLock)، سری FATMAX، تراز، جعبه‌ابزار، چکش و آچارآلات\nمعرفی\nاستنلی یکی از قدیمی‌ترین برندهای ابزار دستی در جهان است که از قرن نوزدهم در ایالات متحده فعالیت می‌کند. این برند با رنگ زرد و مشکی و محصولاتی مانند متر Powe'+N'rLock و سری FATMAX شناخته می‌شود. امروز استنلی بخشی از شرکت Stanley Black & Decker است که دیوالت را نیز در اختیار دارد.\nحوزه‌های فعالیت و محصولات\nمتر و ابزار اندازه‌گیری (PowerLock، تراز)\nآچارآلات، انبرها و پیچ‌گوشتی‌ها\nچکش‌ها و ابزار تخریب\nجعبه‌ابزار و سیستم ذخیره‌سازی\nکاتر و ابزار برش\nنقاط قوت\nاعتبار تاریخی و شبکه توزیع جهانی\nتنوع بالا برای کاربران خانگی و حرفه‌ای\nقیمت متعادل و دسترسی آسان\nمخاطبان اصلی\nکاربران خانگی، ساختمانی‌ها، نجاران و صنعتگران.\n5. فناوری سلامت، روشنایی و مراقبت شخصی"
  },
  {
    "slug": "benning",
    "name": "Benning",
    "persianName": "بننینگ",
    "established": "۱۹۸۴",
    "country": "آلمان",
    "officialUrl": "https://www.benning.de/",
    "companyIntroductions": [
      "بننینگ یک شرکت آلمانی است که در زمینه تولید ابزارهای تست، اندازه‌گیری و سرویس الکتریکی فعالیت می‌کند. محصولات این برند برای کاربردهای حرفه‌ای در برق، صنعت و سرویس تجهیزات طراحی شده‌اند. بننینگ در بازار ایران نیز در کنار سایر برندهای اندازه‌گیری عرضه می‌شود و گزینه‌ای برای کاربران فنی و برقکاران است.",
      "بننینگ شرکت آلمانی با بیش از یک قرن سابقه است که دو حوزه اصلی دارد: ابزارهای تست و اندازه‌گیری الکتریکی برای ایمنی در نصب‌های برق، و الکترونیک قدرت شامل یو‌پی‌اس، شارژر باتری و اینورتر. محصولات تست بننینگ مطابق استانداردهای ایمنی VDE و IEC طراحی می‌شوند."
    ],
    "specialization": "تستر برق، مولتی‌متر، تستر عایق، ابزارهای اندازه‌گیری صنعتی و تجهیزات سرویس الکتریکی. بننینگ در رده قیمتی متوسط تا بالا فعالیت می‌کند.",
    "featuredProducts": "تستر برق و تجهیزات اندازه‌گیری.",
    "productFamilies": [
      "تسترهای ولتاژ دو قطبی و پیوستگی",
      "مولتی‌مترهای ایمن (CAT III/IV)",
      "تسترهای نصب (IT/TN) و عایق",
      "الکترونیک قدرت: UPS و شارژر",
      "ابزارهای ایمنی برای تابلو برق"
    ],
    "strengths": [
      "تمرکز بالا بر ایمنی کاربر",
      "تولید آلمانی و مطابقت با استانداردهای اروپا",
      "مناسب تابلو برق و محیط‌های صنعتی"
    ],
    "audience": "برق‌کاران، بازرسان نصب و واحدهای برق صنعتی.",
    "shoppingAdvice": "کاربرد دستگاه و نوع سیستم مورد بررسی را مشخص کنید. محدوده اندازه‌گیری و سطح ایمنی را بسنجید. پیش از استفاده، دستورالعمل سازنده و شرایط محیط را بررسی کنید.",
    "fullPastedText": "۲۱. بننینگ — Benning\nحوزه فعالیت: ابزارهای تست، اندازه‌گیری و سرویس الکتریکی\nسال تأسیس: ۱۹۸۴\nکشور مبدأ: آلمان\nوب‌سایت رسمی: https://www.benning.de/\n\nمعرفی شرکت\nبننینگ یک شرکت آلمانی است که در زمینه تولید ابزارهای تست، اندازه‌گیری و سرویس الکتریکی فعالیت می‌کند. محصولات این برند برای کاربردهای حرفه‌ای در برق، صنعت و سرویس تجهیزات طراحی شده‌اند. بننینگ در بازار ایران نیز در کنار سایر برندهای اندازه‌گیری عرضه می‌شود و گزینه‌ای برای کاربران فنی و برقکاران است.\n\nحوزه تخصصی\nتستر برق، مولتی‌متر، تستر عایق، ابزارهای اندازه‌گیری صنعتی و تجهیزات سرویس الکتریکی. بننینگ در رده قیمتی متوسط تا بالا فعالیت می‌کند.\n\nمحصولات شاخص\nتستر برق و تجهیزات اندازه‌گیری.\n\nنکات خرید\nکاربرد دستگاه و نوع سیستم مورد بررسی را مشخص کنید. محدوده اندازه‌گیری و سطح ایمنی را بسنجید. پیش از استفاده، دستورالعمل سازنده و شرایط محیط را بررسی کنید.",
    "fullDocumentText": "بننینگ (Benning)\nنام رسمی شرکت | BENNING Elektrotechnik und Elektronik GmbH & Co. KG\nسال تأسیس | ≈ ۱۹۱۳ میلادی\nمقر / کشور | بوخولت، آلمان\nمالکیت | شرکت خانوادگی/خصوصی\nحوزه فعالیت | تجهیزات تست الکتریکی و الکترونیک قدرت\nمحصولات شاخص | تستر ولتاژ و پیوستگی، مولتی‌متر، تستر نصب و عایق، یو‌پی‌اس و شارژر باتری\nمعرفی\nبننینگ شرکت آلمانی با بیش از یک قرن سابقه است که دو حوزه اصلی دارد: ابزارهای تست و اندازه‌گیری الکتریکی برای ایمنی در نصب‌های'+N' برق، و الکترونیک قدرت شامل یو‌پی‌اس، شارژر باتری و اینورتر. محصولات تست بننینگ مطابق استانداردهای ایمنی VDE و IEC طراحی می‌شوند.\nحوزه‌های فعالیت و محصولات\nتسترهای ولتاژ دو قطبی و پیوستگی\nمولتی‌مترهای ایمن (CAT III/IV)\nتسترهای نصب (IT/TN) و عایق\nالکترونیک قدرت: UPS و شارژر\nابزارهای ایمنی برای تابلو برق\nنقاط قوت\nتمرکز بالا بر ایمنی کاربر\nتولید آلمانی و مطابقت با استانداردهای اروپا\nمناسب تابلو برق و محیط‌های صنعتی\nمخاطبان اصلی\nبرق‌کاران، بازرسان نصب برق و شرکت‌های برق صنعتی.\n3. تجهیزات لحیم‌کاری و ریورک"
  }
]
')),
        (N'account',CONVERT(NVARCHAR(MAX),CAST(N'' AS NVARCHAR(MAX))+N'{"schemaVersion":1,"currency":"IRT","orders":[{"id":"demo-order-2026-001","number":"HN-۱۴۰۵۰۶۲۱","status":"pending-payment","orderedOn":"2026-09-08","itemCount":1,"totalAmount":24500000,"deliveryDescription":"در انتظار تکمیل پرداخت سفارش"},{"id":"demo-order-2026-002","number":"HN-۱۴۰۵۰۶۱۸","status":"payment-confirmed","orderedOn":"2026-09-05","itemCount":2,"totalAmount":46000000,"deliveryDescription":"پرداخت سفارش تأیید شده است"},{"id":"demo-order-2026-003","number":"HN-۱۴۰۵۰۵۹۲","status":"warehouse-confirmed","orderedOn":"2026-08-22","itemCount":1,"totalAmount":26000000,"deliveryDescription":"موجودی کالا در انبار تأیید شده است"},{"id":"demo-order-2026-004","number":"HN-۱۴۰۵۰۵۸۵","status":"in-transit","orderedOn":"2026-08-17","itemCount":1,"totalAmount":18700000,"deliveryDescription":"مرسوله در مسیر ارسال قرار دارد"},{"id":"demo-order-2026-005","number":"HN-۱۴۰۵۰۵۷۲","status":"shipped-to-post","orderedOn":"2026-08-08","itemCount":2,"totalAmount":51200000,"deliveryDescription":"سفارش به شرکت پست تحویل شد"},{"id":"demo-order-2026-006","number":"HN-۱۴۰۵۰۵۴۰","status":"delivered","orderedOn":"2026-07-30","itemCount":1,"totalAmount":29800000,"deliveryDescription":"سفارش با موفقیت تحویل شد"},{"id":"demo-order-2026-007","number":"HN-۱۴۰۵۰۴۹۸","status":"delivered","orderedOn":"2026-07-21","itemCount":1,"totalAmount":22400000,"deliveryDescription":"سفارش با موفقیت تحویل شد"},{"id":"demo-order-2026-008","number":"HN-۱۴۰۵۰۴۶۱","status":"delivered","orderedOn":"2026-07-08","itemCount":2,"totalAmount":43600000,"deliveryDescription":"سفارش با موفقیت تحویل شد"},{"id":"demo-order-2026-009","number":"HN-۱۴۰۵۰۴۲۲","status":"cancelled","orderedOn":"2026-06-22","itemCount":1,"totalAmount":25900000,"deliveryDescription":"سفارش به درخواست کاربر لغو شد"},{"id":"demo-order-2026-010","number":"HN-۱۴۰۵۰۳۸۹","status":"delivered","orderedOn":"2026-06-09","itemCount":1,"totalAmount":31500000,"deliveryDescription":"سفارش با موفقیت تحویل شد"}],"coupons":[{"id":"coupon-bazar10","code":"BAZAR10","discountPercent":10,"usageLimit":10,"enabled":true,"expiresOn":"2027-03-20","orderIds":["demo-order-2026-003","demo-order-2026-006"]},{"id":"coupon-tools15","code":"TOOLS15","discountPercent":15,"usageLimit":8,"enabled":true,"expiresOn":"2026-11-21","orderIds":["demo-order-2026-001","demo-order-2026-007"]},{"id":"coupon-welcome8","code":"WELCOME8","discountPercent":8,"usageLimit":8,"enabled":true,"expiresOn":"2026-12-21","orderIds":["demo-order-2026-002"]},{"id":"coupon-nimrooz12","code":"NIMROOZ12","discountPercent":12,"usageLimit":5,"enabled":false,"expiresOn":"2026-08-22","orderIds":["demo-order-2026-004"]},{"id":"coupon-summer8","code":"SUMMER8","discountPercent":8,"usageLimit":5,"enabled":false,"expiresOn":"2026-08-22","orderIds":["demo-order-2026-005"]},{"id":"coupon-autumn5","code":"AUTUMN5","discountPercent":5,"usageLimit":5,"enabled":false,"expiresOn":"2026-07-22","orderIds":["demo-order-2026-008"]},{"id":"coupon-order7","code":"ORDER7","discountPercent":7,"usageLimit":6,"enabled":false,"expiresOn":"2026-07-22","orderIds":["demo-order-2026-009"]},{"id":"coupon-aman15","code":"AMAN15","discountPercent":15,"usageLimit":6,"enabled":false,"expiresOn":"2026-06-21","orderIds":["demo-order-2026-010"]},{"id":"coupon-bahar15","code":"BAHAR15","discountPercent":15,"usageLimit":10,"enabled":false,"expiresOn":"2026-07-22","orderIds":[]},{"id":"coupon-old20","code":"OLD20","discountPercent":20,"usageLimit":10,"enabled":false,"expiresOn":"2026-05-21","orderIds":[]}]}'));
    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'007-store-runtime-datasets');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;