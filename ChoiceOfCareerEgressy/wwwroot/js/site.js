// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/* =========================================
   CHOICE OF CAREER - TÉMAVÁLASZTÁS
   ========================================= */

(function () {
    const availableThemes = [
        "purple",
        "blue",
        "gray",
        "pink",
        "green"
    ];

    function applyTheme(theme) {
        if (!availableThemes.includes(theme)) {
            theme = "purple";
        }

        document.documentElement.setAttribute(
            "data-theme",
            theme
        );

        document.querySelectorAll(
            "[data-theme-choice]"
        ).forEach(button => {
            const isSelected =
                button.dataset.themeChoice === theme;

            button.classList.toggle(
                "theme-selected",
                isSelected
            );

            button.setAttribute(
                "aria-pressed",
                String(isSelected)
            );
        });
    }

    document.addEventListener(
        "DOMContentLoaded",
        function () {
            let savedTheme = "purple";

            try {
                savedTheme =
                    localStorage.getItem("choiceOfCareerTheme")
                    || "purple";
            } catch {
                // Ha a tárolás nem elérhető,
                // az alapértelmezett téma marad.
            }

            applyTheme(savedTheme);

            document.querySelectorAll(
                "[data-theme-choice]"
            ).forEach(button => {

                button.addEventListener(
                    "click",
                    function () {
                        const theme =
                            button.dataset.themeChoice;

                        applyTheme(theme);

                        try {
                            localStorage.setItem(
                                "choiceOfCareerTheme",
                                theme
                            );
                        } catch {
                            // A téma így is működik,
                            // csak nem marad meg.
                        }
                    }
                );
            });
        }
    );
})();
