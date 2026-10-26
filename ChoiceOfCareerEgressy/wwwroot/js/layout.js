// =========================================
// CHOICE OF CAREER - LAYOUT JAVASCRIPT
// =========================================

document.addEventListener("DOMContentLoaded", function () {

    "use strict";

    // =====================================
    // SZÍNTÉMÁK
    // =====================================

    const storageKey = "choiceOfCareerTheme";

    const allowedThemes = [
        "purple",
        "blue",
        "gray",
        "pink",
        "green"
    ];

    const defaultTheme = "gray";

    // =====================================
    // AKTUÁLIS TÉMA LEKÉRÉSE
    // =====================================

    function getCurrentTheme() {

        const currentTheme =
            document.documentElement.dataset.theme;

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

        document.querySelectorAll("[data-theme-choice]")
            .forEach(function (button) {

                const isActive =
                    button.dataset.themeChoice === currentTheme;

                button.classList.toggle("active", isActive);

                button.setAttribute(
                    "aria-pressed",
                    String(isActive)
                );
            });
    }

    // =====================================
    // SZÍNTÉMA BEÁLLÍTÁSA
    // =====================================

    function setTheme(theme) {

        if (!allowedThemes.includes(theme)) {
            return;
        }

        document.documentElement.dataset.theme = theme;

        try {

            localStorage.setItem(storageKey, theme);

        }
        catch (error) {

            console.warn(
                "A színtéma mentése sikertelen:",
                error
            );
        }

        updateThemeButtons();
    }

    // =====================================
    // SZÍNTÉMAVÁLASZTÓ GOMBOK
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
    // KEZDETI ÁLLAPOT
    // =====================================

    updateThemeButtons();



});
