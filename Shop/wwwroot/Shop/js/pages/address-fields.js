document.addEventListener("DOMContentLoaded", () => {
  const form = document.querySelector("#addressForm");
  if (!form) return;
  const data = window.HNIranPlaces,
    province = form.elements.province,
    city = form.elements.city;
  const normalize = (s) =>
    String(s || "")
      .replace(/ي/g, "ی")
      .replace(/ك/g, "ک")
      .replace(/[\s‌]+/g, " ")
      .trim();
  const provinces = Object.keys(data).sort((a, b) => a.localeCompare(b, "fa"));
  province.replaceChildren(new Option("انتخاب استان", ""));
  provinces.forEach((name) => province.add(new Option(name, name)));
  function cities(selected = "") {
    city.replaceChildren(
      new Option(
        province.value ? "انتخاب شهر" : "ابتدا استان را انتخاب کنید",
        "",
      ),
    );
    city.disabled = !province.value;
    (data[province.value] || []).forEach((name) =>
      city.add(new Option(name, name)),
    );
    const match = (data[province.value] || []).find(
      (name) => normalize(name) === normalize(selected),
    );
    // Preserve an existing address when its city has changed in the dataset.
    if (selected && !match && province.value)
      city.add(new Option(selected, selected));
    city.value = match || selected;
  }
  window.HNAddressFields = {
    set(address = {}) {
      let region = provinces.find(
        (p) => normalize(p) === normalize(address.province),
      );
      if (!region && address.city) {
        const candidates = provinces.filter((p) =>
          data[p].some((c) => normalize(c) === normalize(address.city)),
        );
        if (candidates.length === 1) region = candidates[0];
      }
      province.value = region || "";
      cities(address.city || "");
    },
  };
  province.addEventListener("change", () => cities());
  window.HNAddressFields.set();
});
