// =========================================
// CHOICE OF CAREER - LAYOUT JAVASCRIPT
// =========================================

document.addEventListener("DOMContentLoaded", function () {

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
    // SZÍNVÁLASZTÓ ELEMEK
    // =====================================

    const headerColorInput =
        document.getElementById("customHeaderColor");

    const backgroundColorInput =
        document.getElementById("customBackgroundColor");

    const resetHeaderButton =
        document.getElementById("resetHeaderColor");

    const resetBackgroundButton =
        document.getElementById("resetBackgroundColor");

    // =====================================
    // HELYI TÁROLÁS
    // =====================================

    function getStoredValue(key) {

        try {
            return localStorage.getItem(key);
        }
        catch (error) {

            console.warn(
                "A beállítás nem olvasható:",
                error
            );

            return null;
        }
    }

    function saveValue(key, value) {

        try {
            localStorage.setItem(key, value);
        }
        catch (error) {

            console.warn(
                "A beállítás mentése sikertelen:",
                error
            );
        }
    }

    function removeValue(key) {

        try {
            localStorage.removeItem(key);
        }
        catch (error) {

            console.warn(
                "A beállítás törlése sikertelen:",
                error
            );
        }
    }

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
            .map(function (value) {

                return Math.round(value)
                    .toString(16)
                    .padStart(2, "0");

            })
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
    // AKTUÁLIS TÉMA LEKÉRÉSE
    // =====================================

    function getCurrentTheme() {

        const currentTheme = root.dataset.theme;

        if (allowedThemes.includes(currentTheme)) {
            return currentTheme;
        }

        return defaultTheme;
    }

    // =====================================
    // AKTÍV TÉMA JELÖLÉSE
    // =====================================

    function updateThemeButtons() {

        const currentTheme = getCurrentTheme();

        const hasCustomColors =
            isValidHexColor(getStoredValue(headerColorKey)) ||
            isValidHexColor(getStoredValue(backgroundColorKey));

        document.querySelectorAll("[data-theme-choice]")
            .forEach(function (button) {

                const isActive =
                    !hasCustomColors &&
                    button.dataset.themeChoice === currentTheme;

                button.classList.toggle("active", isActive);

                button.setAttribute(
                    "aria-pressed",
                    String(isActive)
                );
            });
    }

    // =====================================
    // EGYÉNI SZÍNEK ELTÁVOLÍTÁSA
    // =====================================

    function clearHeaderColor() {

        root.style.removeProperty("--header-primary");
        root.style.removeProperty("--header-dark");
        root.style.removeProperty("--header-mid");

        root.style.removeProperty("--header-text-color");

        removeValue(headerColorKey);
    }

    function clearBackgroundColor() {

        root.style.removeProperty("--background-primary");
        root.style.removeProperty("--background-dark");
        root.style.removeProperty("--background-light");

        root.style.removeProperty("--page-text-color");
        root.style.removeProperty("--page-muted-color");

        removeValue(backgroundColorKey);
    }
    function isDarkColor(hex) {
        const color = hex.replace("#", "");

        const r = parseInt(color.substring(0, 2), 16);
        const g = parseInt(color.substring(2, 4), 16);
        const b = parseInt(color.substring(4, 6), 16);

        // Relatív fényesség számítása

        const brightness =
            (r * 299 + g * 587 + b * 114) / 1000;

        return brightness < 150;
    }

    function updateHeaderTextColor(headerColor) {

        const dark = isDarkColor(headerColor);

        document.documentElement.style.setProperty(
            "--header-text-color",
            dark ? "#FFF4DC" : "#292929"
        );
    }
    function updatePageTextColor(color) {

        const dark = isDarkColor(color);

        const textColor = dark ? "#FFF4DC" : "#292929";
        const mutedColor = dark ? "#E6DCCB" : "#404040";

        root.style.setProperty(
            "--page-text-color",
            textColor
        );

        root.style.setProperty(
            "--page-muted-color",
            mutedColor
        );
    }

    // =====================================
    // SZÍNTÉMA BEÁLLÍTÁSA
    // =====================================

    function setTheme(theme) {

        if (!allowedThemes.includes(theme)) {
            return;
        }

        root.dataset.theme = theme;

        saveValue(storageKey, theme);

        // Előre beállított téma választásakor
        // visszaállítjuk annak eredeti színeit.

        clearHeaderColor();
        clearBackgroundColor();

        updateColorInputs();
        updateThemeButtons();
    }

    // =====================================
    // EGYÉNI FEJLÉCSZÍN
    // =====================================

    function applyHeaderColor(color) {

        if (!isValidHexColor(color)) {
            return;
        }

        const dark = mixColors(color, "#000000", 0.35);
        const middle = mixColors(color, "#ffffff", 0.25);

        root.style.setProperty(
            "--header-primary",
            color
        );

        root.style.setProperty(
            "--header-dark",
            dark
        );

        root.style.setProperty(
            "--header-mid",
            middle
        );

        saveValue(headerColorKey, color);
        updateHeaderTextColor(color);

        updateThemeButtons();
    }

    // =====================================
    // EGYÉNI HÁTTÉRSZÍN
    // =====================================

    function applyBackgroundColor(color) {

        if (!isValidHexColor(color)) {
            return;
        }

        const dark = mixColors(color, "#000000", 0.35);
        const light = mixColors(color, "#ffffff", 0.85);

        root.style.setProperty(
            "--background-primary",
            color
        );

        root.style.setProperty(
            "--background-dark",
            dark
        );

        root.style.setProperty(
            "--background-light",
            light
        );

        saveValue(backgroundColorKey, color);

        updatePageTextColor(color);


        updateThemeButtons();

        root.style.setProperty(
            "--page-heading-color",
            isDarkColor(color) ? "#FFF4DC" : "#292929"
        );
    }

    // =====================================
    // SZÍNVÁLASZTÓK FRISSÍTÉSE
    // =====================================

    function updateColorInputs() {

        const savedHeaderColor =
            getStoredValue(headerColorKey);

        const savedBackgroundColor =
            getStoredValue(backgroundColorKey);

        const computedStyle =
            getComputedStyle(root);

        if (headerColorInput) {

            headerColorInput.value =
                isValidHexColor(savedHeaderColor)
                    ? savedHeaderColor
                    : rgbStringToHex(
                        computedStyle.getPropertyValue("--theme-primary")
                    );
        }

        if (backgroundColorInput) {

            backgroundColorInput.value =
                isValidHexColor(savedBackgroundColor)
                    ? savedBackgroundColor
                    : rgbStringToHex(
                        computedStyle.getPropertyValue("--theme-primary")
                    );
        }
    }

    // =====================================
    // CSS SZÍN ÁTALAKÍTÁSA HEX-RE
    // =====================================

    function rgbStringToHex(color) {

        const value = color.trim();

        if (isValidHexColor(value)) {
            return value;
        }

        const match = value.match(
            /^rgba?\(\s*(\d+)[,\s]+(\d+)[,\s]+(\d+)/
        );

        if (match) {

            return rgbToHex(
                Number(match[1]),
                Number(match[2]),
                Number(match[3])
            );
        }

        return "#7654d4";
    }

    // =====================================
    // ELŐRE BEÁLLÍTOTT TÉMÁK
    // =====================================

    document.querySelectorAll("[data-theme-choice]")
        .forEach(function (button) {

            button.addEventListener("click", function () {

                const selectedTheme =
                    button.dataset.themeChoice;

                setTheme(selectedTheme);
            });
        });

    // =====================================
    // FEJLÉC SZÍNVÁLASZTÓ
    // =====================================

    if (headerColorInput) {

        headerColorInput.addEventListener(
            "input",
            function () {

                applyHeaderColor(this.value);
            }
        );
    }

    // =====================================
    // HÁTTÉR SZÍNVÁLASZTÓ
    // =====================================

    if (backgroundColorInput) {

        backgroundColorInput.addEventListener(
            "input",
            function () {

                applyBackgroundColor(this.value);
            }
        );
    }

    // =====================================
    // FEJLÉC VISSZAÁLLÍTÁSA
    // =====================================

    if (resetHeaderButton) {

        resetHeaderButton.addEventListener(
            "click",
            function () {

                clearHeaderColor();

                updateColorInputs();
                updateThemeButtons();
            }
        );
    }

    // =====================================
    // HÁTTÉR VISSZAÁLLÍTÁSA
    // =====================================

    if (resetBackgroundButton) {

        resetBackgroundButton.addEventListener(
            "click",
            function () {

                clearBackgroundColor();

                updateColorInputs();
                updateThemeButtons();
            }
        );
    }

    // =====================================
    // KEZDETI ÁLLAPOT
    // =====================================

    updateColorInputs();
    updateThemeButtons();

});