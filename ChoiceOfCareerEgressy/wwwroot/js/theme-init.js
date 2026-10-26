// =========================================
// SZÍNTÉMA KORAI BETÖLTÉSE
// =========================================

(function () {

    "use strict";

    const storageKey = "choiceOfCareerTheme";

    const allowedThemes = [
        "purple",
        "blue",
        "gray",
        "pink",
        "green"
    ];

    const defaultTheme = "purple";

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

    document.documentElement.dataset.theme = selectedTheme;

})();