(() => {
    "use strict";

    const endpoint = new URL("api/store/geography", document.baseURI);
    window.HNIranPlacesReady = fetch(endpoint, {
        method: "GET",
        credentials: "same-origin",
        headers: { Accept: "application/json" },
        cache: "no-store",
    }).then((response) => {
        if (!response.ok) throw new Error(`Geography request failed (${response.status}).`);
        return response.json();
    }).then((data) => {
        if (!Array.isArray(data.countries) || !Array.isArray(data.provinces) || !Array.isArray(data.cities))
            throw new Error("The database geography response is invalid.");
        window.HNIranPlaces = Object.freeze({
            countries: Object.freeze(data.countries.map((item) => Object.freeze(item))),
            provinces: Object.freeze(data.provinces.map((item) => Object.freeze(item))),
            cities: Object.freeze(data.cities.map((item) => Object.freeze(item))),
        });
        return window.HNIranPlaces;
    });

    // The address form also awaits this promise and reports a visible failure there.
    void window.HNIranPlacesReady.catch(() => {});
})();
