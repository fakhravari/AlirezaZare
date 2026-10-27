window.HNUI.ready(() => window.HNUI.mount("pages-address-fields", document, () => {
    const form = document.querySelector("#addressForm");
    if (!form) return;
    const data = window.HNIranPlaces;
    if (!data?.countries?.length || !data.provinces?.length || !data.cities?.length)
        throw new Error("The country, province, and city data is unavailable.");

    const country = form.elements.country;
    const province = form.elements.province;
    const provinceSelect = form.elements.provinceId;
    const countryId = form.elements.countryId;
    const city = form.elements.city;
    const citySelect = form.elements.cityId;
    const normalize = (value) => String(value || "")
        .replace(/[يى]/g, "ی")
        .replace(/ك/g, "ک")
        .replace(/[\s‌]+/g, " ")
        .trim();
    const countries = [...data.countries].sort((a, b) => a.name.localeCompare(b.name, "fa"));

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

    function renderCities(address, selectedCountry, selectedProvince) {
        const available = selectedCountry && selectedProvince
            ? data.cities
                .filter((item) => item.countryId === selectedCountry.id && item.provinceId === selectedProvince.id)
                .sort((a, b) => a.name.localeCompare(b.name, "fa"))
            : [];
        citySelect.replaceChildren(new Option(
            selectedProvince ? "انتخاب شهر" : "ابتدا استان را انتخاب کنید",
            "",
        ));
        citySelect.disabled = !selectedProvince;
        available.forEach((item) => citySelect.add(new Option(item.name, String(item.id))));

        const selectedCity = available.find((item) =>
            (address.cityId && String(item.id) === String(address.cityId)) ||
            normalize(item.name) === normalize(address.city),
        );
        citySelect.value = selectedCity ? String(selectedCity.id) : "";
        city.value = selectedCity?.name || "";
    }

    function renderProvinces(address = {}, selectedCountry = countries[0]) {
        country.value = selectedCountry?.name || "";
        countryId.value = selectedCountry ? String(selectedCountry.id) : "";
        const available = selectedCountry
            ? data.provinces.filter((item) => item.countryId === selectedCountry.id)
                .sort((a, b) => a.name.localeCompare(b.name, "fa"))
            : [];
        provinceSelect.replaceChildren(new Option("انتخاب استان", ""));
        provinceSelect.disabled = !selectedCountry;
        available.forEach((item) => provinceSelect.add(new Option(item.name, String(item.id))));

        const legacyProvince = findLegacyProvince(address, selectedCountry || {});
        const selectedProvince = available.find((item) =>
            (address.provinceId && String(item.id) === String(address.provinceId)) ||
            normalize(item.name) === normalize(address.province),
        ) || available.find((item) => item === legacyProvince);
        provinceSelect.value = selectedProvince ? String(selectedProvince.id) : "";
        province.value = selectedProvince?.name || "";
        renderCities(address, selectedCountry, selectedProvince);
    }

    window.HNAddressFields = {
        set(address = {}) {
            const selectedCountry = countries.find((item) =>
                (address.countryId && String(item.id) === String(address.countryId)) ||
                normalize(item.name) === normalize(address.country) ||
                normalize(item.nameEn) === normalize(address.country),
            ) || countries[0];
            renderProvinces(address, selectedCountry);
        },
    };

    provinceSelect.addEventListener("change", () => {
        const selectedCountry = countries[0];
        const selectedProvince = data.provinces.find((item) =>
            item.countryId === selectedCountry?.id && String(item.id) === provinceSelect.value,
        );
        province.value = selectedProvince?.name || "";
        city.value = "";
        renderCities({}, selectedCountry, selectedProvince);
    });
    citySelect.addEventListener("change", () => {
        const selectedCity = data.cities.find((item) => String(item.id) === citySelect.value);
        city.value = selectedCity?.name || "";
    });
    window.HNAddressFields.set();
}));
