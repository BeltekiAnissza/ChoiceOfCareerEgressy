// =====================================================
// TANÁRI FELÜGYELETEK KEZELÉSE
// =====================================================


// =====================================================
// ESEMÉNY ÉS PROGRAM KIVÁLASZTÁSA
// =====================================================

document.addEventListener("change", function (event) {

    const select = event.target.closest(
        '[data-auto-submit="true"]'
    );

    if (!select || !select.form) {
        return;
    }

    select.form.requestSubmit();

});


// =====================================================
// AJAX MENTÉS ÉS TÖRLÉS
// =====================================================

document.addEventListener("submit", async function (event) {

    const form = event.target;

    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    if (!form.classList.contains("supervision-ajax-form")) {
        return;
    }

    event.preventDefault();

    // =================================================
    // TÖRLÉS MEGERŐSÍTÉSE
    // =================================================

    if (form.classList.contains("supervision-delete-form")) {

        const confirmed = confirm(
            "Biztosan törlöd a felügyeleti jelentkezést?"
        );

        if (!confirmed) {
            return;
        }

    }

    // =================================================
    // MENTÉS / TÖRLÉS INDÍTÁSA
    // =================================================

    const button = form.querySelector('button[type="submit"]');

    if (!button || button.disabled) {
        return;
    }

    const originalText = button.textContent;

    const teacherCard = form.closest(".accordion-item");

    const teacherId = teacherCard
        ?.id.replace("teacher-card-", "");

    const viewMode = new URL(
        form.action,
        window.location.origin
    ).searchParams.get("viewMode") || "teachers";

    button.disabled = true;
    button.textContent = "Feldolgozás...";

    try {

        const response = await fetch(form.action, {
            method: "POST",
            body: new FormData(form),
            credentials: "same-origin",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        // =================================================
        // SZERVER VÁLASZÁNAK FELDOLGOZÁSA
        // =================================================

        if (!response.ok) {

            let errorMessage = "A művelet nem sikerült.";

            try {

                const errorResult = await response.json();

                if (errorResult.message) {
                    errorMessage = errorResult.message;
                }

            }
            catch {
                // Nem JSON-válasz esetén az alapértelmezett
                // hibaüzenetet használjuk.
            }

            throw new Error(errorMessage);
        }

        const result = await response.json();

        if (!result.success) {
            throw new Error(
                result.message || "A művelet nem sikerült."
            );
        }

        // =================================================
        // NÉZET FRISSÍTÉSE
        // =================================================

        if (viewMode === "programs") {

            await refreshProgramSupervisions();

        }
        else {

            await refreshTeacherSupervisions(teacherId);

        }

        showSupervisionMessage(
            result.message || "A művelet sikeresen befejeződött.",
            "success"
        );

    }
    catch (error) {

        console.error(error);

        showSupervisionMessage(
            error.message || "Ismeretlen hiba történt.",
            "danger"
        );

    }
    finally {

        button.disabled = false;
        button.textContent = originalText;

    }

});


// =====================================================
// TANÁRI ADATOK FRISSÍTÉSE OLDALFRISSÍTÉS NÉLKÜL
// =====================================================

async function refreshTeacherSupervisions(openTeacherId) {

    const newDocument = await fetchUpdatedPage();

    const newAccordion =
        newDocument.getElementById("teachersAccordion");

    const currentAccordion =
        document.getElementById("teachersAccordion");

    if (!newAccordion || !currentAccordion) {
        throw new Error(
            "A tanári adatok nem találhatók."
        );
    }

    // =================================================
    // TANÁRI KÁRTYÁK FRISSÍTÉSE
    // =================================================

    currentAccordion.innerHTML = newAccordion.innerHTML;

    // =================================================
    // KORÁBBAN MEGNYITOTT TANÁR VISSZANYITÁSA
    // =================================================

    if (!openTeacherId) {
        return;
    }

    const collapseElement =
        document.getElementById(
            "teacher-" + openTeacherId
        );

    if (!collapseElement) {
        return;
    }

    collapseElement.classList.add("show");

    const headerButton =
        collapseElement.previousElementSibling
            ?.querySelector(".accordion-button");

    if (headerButton) {

        headerButton.classList.remove("collapsed");

        headerButton.setAttribute(
            "aria-expanded",
            "true"
        );

    }

}


// =====================================================
// PROGRAM ADATAINAK FRISSÍTÉSE
// =====================================================

async function refreshProgramSupervisions() {

    const newDocument = await fetchUpdatedPage();

    const newProgramContainer =
        newDocument.getElementById("programSupervisions");

    const currentProgramContainer =
        document.getElementById("programSupervisions");

    if (!newProgramContainer || !currentProgramContainer) {
        throw new Error(
            "A program felügyeleti adatai nem találhatók."
        );
    }

    // =================================================
    // PROGRAM ADATAINAK ÉS FELÜGYELŐINEK FRISSÍTÉSE
    // =================================================

    currentProgramContainer.innerHTML =
        newProgramContainer.innerHTML;

    // =================================================
    // PROGRAMVÁLASZTÓ FRISSÍTÉSE
    // =================================================

    const newProgramSelect =
        newDocument.querySelector(
            'select[name="planSessionId"]'
        );

    const currentProgramSelect =
        document.querySelector(
            'select[name="planSessionId"]'
        );

    if (newProgramSelect && currentProgramSelect) {

        currentProgramSelect.innerHTML =
            newProgramSelect.innerHTML;

    }

}


// =====================================================
// FRISSÍTETT OLDAL LEKÉRÉSE
// =====================================================

async function fetchUpdatedPage() {

    const url = new URL(window.location.href);

    const response = await fetch(url.toString(), {
        method: "GET",
        credentials: "same-origin",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    });

    if (!response.ok) {
        throw new Error(
            "A frissített adatok betöltése nem sikerült."
        );
    }

    const html = await response.text();

    const parser = new DOMParser();

    return parser.parseFromString(
        html,
        "text/html"
    );

}


// =====================================================
// VISSZAJELZÉSEK MEGJELENÍTÉSE
// =====================================================

function showSupervisionMessage(message, type) {

    const container =
        document.getElementById("supervisionMessage");

    if (!container) {
        return;
    }

    const alert = document.createElement("div");

    alert.className = "alert alert-" + type;
    alert.textContent = message;

    container.replaceChildren(alert);

    // =================================================
    // ÜZENET AUTOMATIKUS ELTÜNTETÉSE
    // =================================================

    setTimeout(function () {

        if (alert.isConnected) {
            alert.remove();
        }

    }, 5000);

}