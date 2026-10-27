// =========================================
// CHOICE OF CAREER
// SZÍNTÉMA KORAI BETÖLTÉSE
// =========================================

(function () {

    "use strict";

    // =====================================
    // BEÁLLÍTÁSOK
    // =====================================

    const storageKey = "choiceOfCareerTheme";

    const headerColorKey = "choiceOfCareerHeaderColor";
    const backgroundColorKey = "choiceOfCareerBackgroundColor";

    const allowedThemes = [
        "purple",
        "blue",
        "gray",
        "pink",
        "green"
    ];

    const defaultTheme = "purple";

    const root = document.documentElement;

    // =====================================
    // HEX SZÍN ELLENŐRZÉSE
    // =====================================

    function isValidHexColor(color) {

        return typeof color === "string" &&
            /^#[0-9a-fA-F]{6}$/.test(color);
    }

    // =====================================
    // HEX ÁTALAKÍTÁSA RGB-VÉ
    // =====================================

    function hexToRgb(hex) {

        return {
            r: parseInt(hex.substring(1, 3), 16),
            g: parseInt(hex.substring(3, 5), 16),
            b: parseInt(hex.substring(5, 7), 16)
        };
    }

    // =====================================
    // RGB ÁTALAKÍTÁSA HEX SZÍNNÉ
    // =====================================

    function rgbToHex(r, g, b) {

        return "#" + [r, g, b]
            .map(value =>
                Math.round(value)
                    .toString(16)
                    .padStart(2, "0")
            )
            .join("");
    }

    // =====================================
    // SZÍNEK KEVERÉSE
    // =====================================

    function mixColors(color1, color2, weight) {

        const first = hexToRgb(color1);
        const second = hexToRgb(color2);

        const r = first.r + (second.r - first.r) * weight;
        const g = first.g + (second.g - first.g) * weight;
        const b = first.b + (second.b - first.b) * weight;

        return rgbToHex(r, g, b);
    }

    // =====================================
    // SZÍN VILÁGOSSÁGÁNAK MEGHATÁROZÁSA
    // =====================================

    function isDarkColor(hex) {

        const { r, g, b } = hexToRgb(hex);

        const brightness =
            (r * 299 + g * 587 + b * 114) / 1000;

        return brightness < 150;
    }

    // =====================================
    // FEJLÉC SZÖVEGSZÍNÉNEK BEÁLLÍTÁSA
    // =====================================

    function updateHeaderTextColor(color) {

        root.style.setProperty(
            "--header-text-color",
            isDarkColor(color) ? "#FFF4DC" : "#292929"
        );
    }

    // =====================================
    // EGYÉNI FEJLÉCSZÍN BEÁLLÍTÁSA
    // =====================================

    function applyHeaderColor(color) {

        if (!isValidHexColor(color)) {
            return;
        }

        const dark = mixColors(color, "#000000", 0.35);
        const middle = mixColors(color, "#ffffff", 0.25);

        root.style.setProperty("--header-primary", color);
        root.style.setProperty("--header-dark", dark);
        root.style.setProperty("--header-mid", middle);

        updateHeaderTextColor(color);
    }

    // =====================================
    // EGYÉNI HÁTTÉRSZÍN BEÁLLÍTÁSA
    // =====================================

    function applyBackgroundColor(color) {

        if (!isValidHexColor(color)) {
            return;
        }

        const dark = mixColors(color, "#000000", 0.35);
        const light = mixColors(color, "#ffffff", 0.85);

        root.style.setProperty("--background-primary", color);
        root.style.setProperty("--background-dark", dark);
        root.style.setProperty("--background-light", light);

        root.style.setProperty(
            "--page-heading-color",
            isDarkColor(color) ? "#FFF4DC" : "#292929"
        );

        const brightness =
            (hexToRgb(color).r * 299 +
                hexToRgb(color).g * 587 +
                hexToRgb(color).b * 114) / 1000;

        const isDark = brightness < 150;

        root.style.setProperty(
            "--page-text-color",
            isDark ? "#FFF4DC" : "#292929"
        );

        root.style.setProperty(
            "--page-muted-color",
            isDark ? "#E6DCCB" : "#404040"
        );
    }

    // =====================================
    // MENTETT TÉMA BETÖLTÉSE
    // =====================================

    let selectedTheme = defaultTheme;

    try {

        const savedTheme = localStorage.getItem(storageKey);

        if (allowedThemes.includes(savedTheme)) {
            selectedTheme = savedTheme;
        }

    }
    catch (error) {

        console.warn(
            "A mentett színtéma nem olvasható:",
            error
        );
    }

    root.dataset.theme = selectedTheme;

    // =====================================
    // EGYÉNI SZÍNEK BETÖLTÉSE
    // =====================================

    try {

        const savedHeaderColor =
            localStorage.getItem(headerColorKey);

        const savedBackgroundColor =
            localStorage.getItem(backgroundColorKey);

        if (isValidHexColor(savedHeaderColor)) {

            applyHeaderColor(savedHeaderColor);
        }

        if (isValidHexColor(savedBackgroundColor)) {

            applyBackgroundColor(savedBackgroundColor);
        }

    }
    catch (error) {

        console.warn(
            "Az egyéni színek betöltése sikertelen:",
            error
        );
    }

})();