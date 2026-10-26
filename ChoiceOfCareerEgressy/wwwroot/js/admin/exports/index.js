// =====================================================
// EXCEL EXPORTOK
// =====================================================

document.addEventListener("DOMContentLoaded", function () {

    // =================================================
    // ELEMEK LEKÉRÉSE
    // =================================================

    const eventSelect =
        document.getElementById("planEventId");

    const planSelect =
        document.getElementById("planId");

    const slotSelect =
        document.getElementById("timeSlotId");

    const sessionsData =
        document.getElementById("exportSessionsData");

    if (
        !eventSelect ||
        !planSelect ||
        !slotSelect ||
        !sessionsData
    ) {
        return;
    }

    // =================================================
    // ADATOK BEOLVASÁSA
    // =================================================

    const sessions = JSON.parse(
        sessionsData.textContent
    );

    const allPlans = Array.from(
        planSelect.options
    ).filter(option => option.value !== "");


    // =================================================
    // PROGRAMOK FRISSÍTÉSE
    // =================================================

    function updatePlans() {

        const eventId = Number(eventSelect.value);

        planSelect.innerHTML = "";

        const placeholder =
            document.createElement("option");

        placeholder.value = "";
        placeholder.textContent = "Válassz programot";

        planSelect.appendChild(placeholder);

        const availablePlanIds = new Set(
            sessions
                .filter(session =>
                    session.eventId === eventId
                )
                .map(session => session.planId)
        );

        allPlans.forEach(function (option) {

            if (
                availablePlanIds.has(
                    Number(option.value)
                )
            ) {

                planSelect.appendChild(
                    option.cloneNode(true)
                );

            }

        });

        planSelect.disabled =
            availablePlanIds.size === 0;

        updateSlots();

    }


    // =================================================
    // IDŐSÁVOK FRISSÍTÉSE
    // =================================================

    function updateSlots() {

        const eventId = Number(eventSelect.value);
        const planId = Number(planSelect.value);

        slotSelect.innerHTML = "";

        const placeholder =
            document.createElement("option");

        placeholder.value = "";
        placeholder.textContent = "Válassz idősávot";

        slotSelect.appendChild(placeholder);

        const availableSlots = sessions
            .filter(session =>
                session.eventId === eventId &&
                session.planId === planId
            )
            .sort((a, b) => a.number - b.number);

        availableSlots.forEach(function (slot) {

            const option =
                document.createElement("option");

            option.value = slot.timeSlotId;

            option.textContent =
                `${slot.number}. idősáv – ${slot.name}`;

            slotSelect.appendChild(option);

        });

        slotSelect.disabled =
            availableSlots.length === 0;

    }


    // =================================================
    // ESEMÉNYKEZELŐK
    // =================================================

    eventSelect.addEventListener(
        "change",
        updatePlans
    );

    planSelect.addEventListener(
        "change",
        updateSlots
    );


    // =================================================
    // KEZDŐ ÁLLAPOT
    // =================================================

    updatePlans();

});