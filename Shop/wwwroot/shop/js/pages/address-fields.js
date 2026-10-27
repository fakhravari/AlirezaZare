window.HNUI.ready(() => window.HNUI.mount("pages-address-fields", document, () => {
    const form = document.querySelector("#addressForm");
    if (!form) return;
    const data = window.HNIranPlaces;
    if (!data?.countries?.length || !data.provinces?.length || !data.cities?.length)
        throw new Error("The country, province, and city data is unavailable.");

    const country = form.elements.country;
    const province = form.elements.province;
    const countryId = form.elements.countryId;
    const provinceId = form.elements.provinceId;
    const city = form.elements.city;
    const normalize = (value) => String(value || "")
        .replace(/[يى]/g, "ی")
        .replace(/ك/g, "ک")
        .replace(/[\s‌]+/g, " ")
        .trim();
    const countries = [...data.countries].sort((a, b) => a.name.localeCompare(b.name, "fa"));
    country.replaceChildren(new Option("انتخاب کشور", ""));
    countries.forEach((item) => country.add(new Option(item.name, item.name)));

    function findLegacyProvince(address, selectedCountry) {
        if (!address.city || address.province) return null;
        const matchingCities = data.cities.filter((item) =>
            item.countryId === selectedCountry.id && normalize(item.name) === normalize(address.city),
        );
        const provinceIds = [...new Set(matchingCities.map((item) => item.provinceId))];
        return provinceIds.length === 1
            ? data.provinces.find((item) => item.id === provinceIds[0]) || null
            : null;
    }

    function renderProvinces(address = {}) {
        const selectedCountry = countries.find((item) => item.name === country.value);
        countryId.value = selectedCountry ? String(selectedCountry.id) : "";
        const available = selectedCountry
            ? data.provinces.filter((item) => item.countryId === selectedCountry.id)
            : [];
        province.replaceChildren(new Option(
            selectedCountry ? "انتخاب استان" : "ابتدا کشور را انتخاب کنید",
            "",
        ));
        province.disabled = !selectedCountry;
        available.forEach((item) => province.add(new Option(item.name, item.name)));

        const legacyProvince = findLegacyProvince(address, selectedCountry || {});
        const selectedProvince = available.find((item) =>
            (address.provinceId && String(item.id) === String(address.provinceId)) ||
            normalize(item.name) === normalize(address.province),
        ) || available.find((item) => item === legacyProvince);
        province.value = selectedProvince?.name || "";
        provinceId.value = selectedProvince ? String(selectedProvince.id) : "";
    }

    window.HNAddressFields = {
        set(address = {}) {
            const selectedCountry = countries.find((item) =>
                (address.countryId && String(item.id) === String(address.countryId)) ||
                normalize(item.name) === normalize(address.country) ||
                normalize(item.nameEn) === normalize(address.country),
            ) || countries[0];
            country.value = selectedCountry?.name || "";
            city.value = address.city || "";
            renderProvinces(address);
        },
    };

    country.addEventListener("change", () => {
        city.value = "";
        renderProvinces();
    });
    province.addEventListener("change", () => {
        const selectedCountry = countries.find((item) => item.name === country.value);
        const selectedProvince = data.provinces.find((item) =>
            item.countryId === selectedCountry?.id && item.name === province.value,
        );
        provinceId.value = selectedProvince ? String(selectedProvince.id) : "";
        city.value = "";
    });
    window.HNAddressFields.set();
}));
