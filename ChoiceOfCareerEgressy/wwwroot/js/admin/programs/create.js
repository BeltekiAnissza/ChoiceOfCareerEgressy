document.addEventListener("DOMContentLoaded", function () {

    // =====================================
    // ESEMÉNY ÉS IDŐSÁV KIVÁLASZTÁSA
    // =====================================

    const eventSelect =
        document.getElementById("eventSelect");

    const timeSlotSelect =
        document.getElementById("timeSlotSelect");

    if (!eventSelect || !timeSlotSelect) {
        return;
    }

    // =====================================
    // IDŐSÁVOK SZŰRÉSE
    // =====================================

    function filterTimeSlots() {

        const selectedEventId = eventSelect.value;

        let selectedSlotStillValid = false;

        Array.from(timeSlotSelect.options).forEach(option => {

            // Az alapértelmezett opció mindig látható
            if (!option.value) {
                option.hidden = false;
                option.disabled = false;
                return;
            }

            // Csak a kiválasztott esemény idősávjai jelenjenek meg
            const matches =
                option.dataset.eventId === selectedEventId;

            option.hidden = !matches;
            option.disabled = !matches;

            // Ellenőrizzük, hogy a korábbi választás érvényes-e
            if (matches &&
                option.value === timeSlotSelect.value) {

                selectedSlotStillValid = true;
            }
        });

        // Ha a kiválasztott idősáv nem tartozik az eseményhez,
        // töröljük az idősáv kiválasztását.
        if (!selectedSlotStillValid) {
            timeSlotSelect.value = "";
        }
    }

    // =====================================
    // ESEMÉNYEK
    // =====================================

    eventSelect.addEventListener("change", filterTimeSlots);

    // Oldalbetöltéskor is lefuttatjuk,
    // hogy csak a megfelelő idősávok látszódjanak.
    filterTimeSlots();

});