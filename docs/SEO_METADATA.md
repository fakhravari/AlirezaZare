# Shared SEO metadata

`Shop/Views/Shared/_SeoMetadata.cshtml` renders the common search and social metadata for every MVC page from `SeoModel` and the optional `Seo` configuration section. Page-specific titles, descriptions, canonical URLs, images, robots rules, and structured data stay in the page model; site-wide verification and social account values belong in configuration.

The same partial adds a `BreadcrumbList` to the JSON-LD graph when an indexable page supplies its visible path as `SeoModel.Breadcrumbs`. Keep those names and URLs aligned with the breadcrumb rendered in the view. Product and article structured data should only include values that are also visible on the page and backed by the product or editorial source data; do not infer live offers, stock, SKU, or ratings from demo content.

The FAQ page uses `FaqCatalog` as the single source for both its visible questions and `FAQPage` JSON-LD, so edits stay synchronized.

Keep descriptions specific to each page and grounded in its visible content. The site uses the shared metadata partial to emit them; approximate character counts are editorial guidance, not a value to reach by padding.

The following ASP.NET Core configuration keys are optional. Set them in deployment configuration or environment variables; for example, use `Seo__GoogleSiteVerification` for `Seo:GoogleSiteVerification`.

| Key | Purpose |
| --- | --- |
| `Seo:SiteName` | Shared site name for Open Graph and JSON-LD |
| `Seo:Locale` | Open Graph locale; defaults to `fa_IR` |
| `Seo:DefaultImagePath` | Fallback social image; defaults to the storefront hero |
| `Seo:DefaultImageAlt` | Accessible description for the fallback social image |
| `Seo:GoogleSiteVerification` | Google Search Console HTML verification token |
| `Seo:BingSiteVerification` | Bing Webmaster Tools verification token |
| `Seo:FacebookAppId` | Facebook app ID for `fb:app_id` |
| `Seo:FacebookDomainVerification` | Facebook domain verification token |
| `Seo:XSite` | X account handle, such as `@example` |
| `Seo:XCreator` | Optional X creator handle for social cards |

Verification tokens and social handles are intentionally unset until the site owner supplies the real values. Google Analytics and Google Tag Manager scripts are not loaded by this metadata layer; adding measurement requires the store's real tag/container ID and its consent behavior.
