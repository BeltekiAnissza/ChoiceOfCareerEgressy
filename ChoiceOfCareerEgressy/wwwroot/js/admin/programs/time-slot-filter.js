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

            if (!option.value) {
                option.hidden = false;
                option.disabled = false;
                return;
            }

            const matches =
                option.dataset.eventId === selectedEventId;

            option.hidden = !matches;
            option.disabled = !matches;

            if (matches &&
                option.value === timeSlotSelect.value) {

                selectedSlotStillValid = true;
            }
        });

        if (!selectedSlotStillValid) {
            timeSlotSelect.value = "";
        }
    }

    // =====================================
    // ESEMÉNYVÁLTOZÁS
    // =====================================

    eventSelect.addEventListener("change", filterTimeSlots);

    // Kezdeti állapot beállítása
    filterTimeSlots();

});