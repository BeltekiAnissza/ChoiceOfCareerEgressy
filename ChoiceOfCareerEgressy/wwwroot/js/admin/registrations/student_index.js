// =====================================================
// DIÁKJELENTKEZÉSEK KEZELÉSE
// =====================================================


// =====================================================
// GÖRGETÉSI POZÍCIÓ KEZELÉSE
// =====================================================

if ("scrollRestoration" in history) {
    history.scrollRestoration = "manual";
}

const registrationScrollStorageKey = "registrationScrollPosition";

function saveRegistrationScroll() {

    sessionStorage.setItem(
        registrationScrollStorageKey,
        String(window.scrollY)
    );

}


// =====================================================
// OSZTÁLYOK AJAX-KEZELÉSE
// =====================================================

let activeGradeRequest = null;
let gradeRequestVersion = 0;


// =====================================================
// OSZTÁLYGOMB ÁLLAPOTÁNAK FRISSÍTÉSE
// =====================================================

function updateGradeButton(button, isOpen) {

    button.textContent = isOpen
        ? "Diákok elrejtése"
        : "Diákok megtekintése";

    button.classList.toggle("btn-primary", isOpen);
    button.classList.toggle("btn-outline-primary", !isOpen);

    button.setAttribute(
        "aria-expanded",
        isOpen ? "true" : "false"
    );

}


// =====================================================
// MEGNYITOTT OSZTÁLYOK BEZÁRÁSA
// =====================================================

function closeGradeRows() {

    document.querySelectorAll(".grade-students-row")
        .forEach(function (row) {
            row.remove();
        });

    document.querySelectorAll(".grade-toggle")
        .forEach(function (button) {
            updateGradeButton(button, false);
        });

}


// =====================================================
// URL FRISSÍTÉSE
// =====================================================

function updateGradeUrl(gradeId = null) {

    const url = new URL(window.location.href);

    if (gradeId !== null) {
        url.searchParams.set("gradeId", gradeId);
    }
    else {
        url.searchParams.delete("gradeId");
        url.searchParams.delete("studentId");
    }

    history.replaceState(
        history.state,
        "",
        url.pathname + url.search
    );

}


// =====================================================
// OSZTÁLY DIÁKJAINAK BETÖLTÉSE
// =====================================================

async function toggleGradeStudents(button) {

    const gradeId = button.dataset.gradeId;

    if (!gradeId) {
        return;
    }

    const existingRow =
        document.getElementById("grade-" + gradeId);

    // =================================================
    // HA NYITVA VAN, BEZÁRJUK
    // =================================================

    if (existingRow) {

        gradeRequestVersion++;

        if (activeGradeRequest) {
            activeGradeRequest.abort();
            activeGradeRequest = null;
        }

        closeGradeRows();

        updateGradeUrl();

        return;
    }

    // =================================================
    // ÚJ OSZTÁLY MEGNYITÁSA
    // =================================================

    if (activeGradeRequest) {
        activeGradeRequest.abort();
    }

    const requestVersion = ++gradeRequestVersion;

    const controller = new AbortController();

    activeGradeRequest = controller;

    closeGradeRows();

    button.disabled = true;
    button.textContent = "Betöltés...";

    try {

        const response = await fetch(
            button.dataset.url,
            {
                method: "GET",
                credentials: "same-origin",
                signal: controller.signal,
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            }
        );

        if (!response.ok) {
            throw new Error(
                "A diákok betöltése nem sikerült."
            );
        }

        const html = await response.text();

        if (requestVersion !== gradeRequestVersion) {
            return;
        }

        const parser = new DOMParser();

        const documentFromServer =
            parser.parseFromString(html, "text/html");

        const studentsRow =
            documentFromServer.getElementById(
                "grade-" + gradeId
            );

        if (!studentsRow) {
            throw new Error(
                "A kiválasztott osztály adatai nem találhatók."
            );
        }

        const summaryRow =
            document.getElementById(
                "grade-summary-" + gradeId
            );

        if (!summaryRow) {
            throw new Error(
                "A kiválasztott osztály sora nem található."
            );
        }

        const importedRow =
            document.importNode(studentsRow, true);

        importedRow.classList.add(
            "grade-students-row"
        );

        summaryRow.insertAdjacentElement(
            "afterend",
            importedRow
        );

        updateGradeButton(button, true);

        updateGradeUrl(gradeId);

    }
    catch (error) {

        if (error.name !== "AbortError" &&
            requestVersion === gradeRequestVersion) {

            console.error(error);

            alert(
                "Nem sikerült betölteni a diákokat. Próbáld újra!"
            );

        }

    }
    finally {

        button.disabled = false;

        if (requestVersion === gradeRequestVersion) {

            activeGradeRequest = null;

            if (!document.getElementById(
                "grade-" + gradeId
            )) {
                updateGradeButton(button, false);
            }

        }

    }

}


// =====================================================
// ESEMÉNYKEZELŐK
// =====================================================

document.addEventListener("DOMContentLoaded", function () {

    // =================================================
    // ESEMÉNY ÉS PROGRAM AUTOMATIKUS KIVÁLASZTÁSA
    // =================================================

    document.querySelectorAll('[data-auto-submit="true"]')
        .forEach(function (select) {

            select.addEventListener("change", function () {

                if (select.form) {
                    select.form.requestSubmit();
                }

            });

        });

});


// =====================================================
// OSZTÁLYGOMBOK KATTINTÁSA
// =====================================================

document.addEventListener("click", function (event) {

    const button = event.target.closest(".grade-toggle");

    if (!button) {
        return;
    }

    event.preventDefault();

    toggleGradeStudents(button);

});


// =====================================================
// ŰRLAPOK BEKÜLDÉSE
// =====================================================

document.addEventListener("submit", function (event) {

    const form = event.target;

    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    // =================================================
    // TÖRLÉS MEGERŐSÍTÉSE
    // =================================================

    if (form.dataset.confirmDelete === "true") {

        const confirmed = confirm(
            "Biztosan törlöd a jelentkezést?"
        );

        if (!confirmed) {
            event.preventDefault();
            return;
        }

    }

    // =================================================
    // GÖRGETÉSI POZÍCIÓ MENTÉSE
    // =================================================

    if (
        !event.defaultPrevented &&
        form.method.toLowerCase() === "post"
    ) {
        saveRegistrationScroll();
    }

});


// =====================================================
// GÖRGETÉSI POZÍCIÓ VISSZAÁLLÍTÁSA
// =====================================================

window.addEventListener("load", function () {

    const savedPosition =
        sessionStorage.getItem(registrationScrollStorageKey);

    if (savedPosition === null) {
        return;
    }

    sessionStorage.removeItem(registrationScrollStorageKey);

    requestAnimationFrame(function () {

        window.scrollTo({
            top: Number(savedPosition),
            behavior: "instant"
        });

    });

});