// =========================================
// CHOICE OF CAREER
// DIÁK PROGRAMVÁLASZTÁS
// =========================================

document.addEventListener("DOMContentLoaded", function () {

    // =========================================
    // JELENTKEZÉSI ŰRLAPOK
    // =========================================

    const forms = document.querySelectorAll(
        ".student-registration-form"
    );

    forms.forEach(function (form) {

        const radios = Array.from(
            form.querySelectorAll(
                ".student-program-radio"
            )
        );

        const summary = form.querySelector(
            ".student-selection-summary"
        );

        const selectedInputs = form.querySelector(
            ".student-selected-inputs"
        );

        // =====================================
        // KIVÁLASZTOTT PROGRAMOK
        // =====================================

        function selectedPrograms() {

            return radios.filter(function (radio) {
                return radio.checked;
            });
        }

        // =====================================
        // IDŐBELI ÜTKÖZÉS
        // =====================================

        function overlaps(first, second) {

            const firstStart =
                Number(first.dataset.start);

            const firstEnd =
                Number(first.dataset.end);

            const secondStart =
                Number(second.dataset.start);

            const secondEnd =
                Number(second.dataset.end);

            return firstStart < secondEnd &&
                secondStart < firstEnd;
        }

        // =====================================
        // PROGRAMNÉV ÖSSZEHASONLÍTÁSA
        // =====================================

        function sameProgram(first, second) {

            return first.dataset.programName
                .trim()
                .toLocaleLowerCase("hu-HU") ===
                second.dataset.programName
                    .trim()
                    .toLocaleLowerCase("hu-HU");
        }

        // =====================================
        // KIJELÖLT PROGRAMOK KIEMELÉSE
        // =====================================

        function updateProgramCards() {

            radios.forEach(function (radio) {

                const card = radio.closest(
                    ".student-program-card"
                );

                if (!card) {
                    return;
                }

                card.classList.toggle(
                    "border-primary",
                    radio.checked
                );

                card.classList.toggle(
                    "border-2",
                    radio.checked
                );

                card.classList.toggle(
                    "bg-primary-subtle",
                    radio.checked
                );
            });
        }

        // =====================================
        // ÖSSZEFOGLALÓ FRISSÍTÉSE
        // =====================================

        function updateSummary() {

            const selected = selectedPrograms();

            if (selected.length === 0) {

                summary.textContent =
                    "Még nem választottál programot.";

                return;
            }

            summary.textContent =
                "Kiválasztott programok: " +
                selected
                    .map(function (radio) {
                        return radio.dataset.programName;
                    })
                    .join(", ");
        }

        // =====================================
        // REJTETT INPUTOK FRISSÍTÉSE
        // =====================================

        function updateSelectedInputs() {

            selectedInputs.replaceChildren();

            selectedPrograms().forEach(function (radio) {

                const input =
                    document.createElement("input");

                input.type = "hidden";

                input.name =
                    "Registration.SelectedPlanSessionIds";

                input.value = radio.value;

                selectedInputs.appendChild(input);
            });
        }

        // =====================================
        // TELJES FELÜLET FRISSÍTÉSE
        // =====================================

        function updateInterface() {

            updateProgramCards();
            updateSummary();
            updateSelectedInputs();
        }

        // =====================================
        // AUTOMATIKUS PROGRAMVÁLTÁS
        // =====================================

        radios.forEach(function (radio) {

            radio.addEventListener(
                "change",
                function () {

                    if (!radio.checked) {
                        return;
                    }

                    // Az új programmal ütköző
                    // korábbi választások törlése.

                    radios.forEach(function (other) {

                        if (other === radio) {
                            return;
                        }

                        if (!other.checked) {
                            return;
                        }

                        if (
                            overlaps(radio, other) ||
                            sameProgram(radio, other)
                        ) {
                            other.checked = false;
                        }
                    });

                    updateInterface();
                }
            );
        });

        // =====================================
        // MENTÉS ELŐTTI ELLENŐRZÉS
        // =====================================

        form.addEventListener(
            "submit",
            function (event) {

                const selected = selectedPrograms();

                if (selected.length === 0) {

                    event.preventDefault();

                    alert(
                        "Válassz legalább egy programot!"
                    );

                    return;
                }

                // Biztonsági ellenőrzés:
                // nincs-e időbeli ütközés
                // vagy azonos program.

                for (let i = 0; i < selected.length; i++) {

                    for (
                        let j = i + 1;
                        j < selected.length;
                        j++
                    ) {

                        if (
                            overlaps(
                                selected[i],
                                selected[j]
                            ) ||
                            sameProgram(
                                selected[i],
                                selected[j]
                            )
                        ) {

                            event.preventDefault();

                            alert(
                                "A kiválasztott programok " +
                                "nem ütközhetnek időben, " +
                                "és ugyanaz a program " +
                                "csak egyszer választható."
                            );

                            return;
                        }
                    }
                }

                updateSelectedInputs();
            }
        );

        // =====================================
        // KEZDETI ÁLLAPOT
        // =====================================

        updateInterface();

    });

});