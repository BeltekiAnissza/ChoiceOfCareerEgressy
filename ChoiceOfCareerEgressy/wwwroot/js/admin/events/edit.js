document.addEventListener("DOMContentLoaded", function () {

    // =========================================
    // ELEMEK
    // =========================================

    const container =
        document.getElementById("timeSlotsContainer");

    const addButton =
        document.getElementById("addTimeSlotButton");

    const countBadge =
        document.getElementById("timeSlotCount");

    const form =
        document.getElementById("editEventForm");

    if (!container || !addButton || !countBadge || !form) {
        return;
    }

    // =========================================
    // BEÁLLÍTÁSOK
    // =========================================

    const MIN_TIME_SLOTS = 2;

    // =========================================
    // IDŐSÁVKÁRTYÁK LEKÉRÉSE
    // =========================================

    function getTimeSlotCards() {

        return Array.from(
            container.querySelectorAll(".time-slot-card")
        );

    }

    // =========================================
    // IDŐSÁV VÉDELMÉNEK ELLENŐRZÉSE
    // =========================================

    function isProtected(card) {

        return card.dataset.protected === "true";

    }

    // =========================================
    // IDŐSÁVOK ÚJRASZÁMOZÁSA
    // =========================================

    function updateTimeSlots() {

        const cards = getTimeSlotCards();

        cards.forEach(function (card, index) {

            const slotNumber = index + 1;

            const protectedSlot = isProtected(card);

            // =====================================
            // KÁRTYA CÍME
            // =====================================

            const title =
                card.querySelector(".time-slot-title");

            if (title) {

                title.textContent =
                    `${slotNumber}. idősáv`;

            }

            // =====================================
            // MEZŐK
            // =====================================

            const idInput =
                card.querySelector(".time-slot-id");

            const nameInput =
                card.querySelector(".time-slot-name");

            const startInput =
                card.querySelector(".time-slot-start");

            const endInput =
                card.querySelector(".time-slot-end");

            // =====================================
            // AZONOSÍTÓ
            // =====================================

            if (idInput) {

                idInput.name =
                    `TimeSlots[${index}].TimeSlotId`;

                idInput.id =
                    `TimeSlots_${index}__TimeSlotId`;

            }

            // =====================================
            // NÉV
            // =====================================

            if (nameInput) {

                nameInput.name =
                    `TimeSlots[${index}].TimeSlotName`;

                nameInput.id =
                    `TimeSlots_${index}__TimeSlotName`;

                nameInput.placeholder =
                    `${slotNumber}. idősáv`;

            }

            // =====================================
            // KEZDÉS
            // =====================================

            if (startInput) {

                startInput.name =
                    `TimeSlots[${index}].StartTime`;

                startInput.id =
                    `TimeSlots_${index}__StartTime`;

                startInput.readOnly = protectedSlot;

            }

            // =====================================
            // BEFEJEZÉS
            // =====================================

            if (endInput) {

                endInput.name =
                    `TimeSlots[${index}].EndTime`;

                endInput.id =
                    `TimeSlots_${index}__EndTime`;

                endInput.readOnly = protectedSlot;

            }

            // =====================================
            // TÖRLÉSGOMB
            // =====================================

            const removeButton =
                card.querySelector(".remove-time-slot");

            if (removeButton) {

                const minimumReached =
                    cards.length <= MIN_TIME_SLOTS;

                removeButton.disabled =
                    protectedSlot || minimumReached;

                if (protectedSlot) {

                    removeButton.title =
                        "Az idősáv használatban van, ezért nem törölhető.";

                }
                else if (minimumReached) {

                    removeButton.title =
                        "Legalább két idősávnak meg kell maradnia.";

                }
                else {

                    removeButton.title =
                        "Idősáv törlése";

                }

            }

        });

        // =====================================
        // IDŐSÁVOK SZÁMA
        // =====================================

        countBadge.textContent =
            `${cards.length} idősáv`;

    }

    // =========================================
    // ÚJ IDŐSÁV LÉTREHOZÁSA
    // =========================================

    function createTimeSlotCard(index) {

        const slotNumber = index + 1;

        const card = document.createElement("div");

        card.className =
            "card border mb-3 time-slot-card";

        card.dataset.protected = "false";

        card.innerHTML = `
            <div class="card-header bg-light">

                <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">

                    <h6 class="mb-0 fw-bold time-slot-title">
                        ${slotNumber}. idősáv
                    </h6>

                    <div class="d-flex flex-wrap align-items-center gap-2">

                        <button type="button"
                                class="btn btn-outline-danger btn-sm remove-time-slot">

                            <i class="bi bi-trash me-1"></i>
                            Törlés

                        </button>

                    </div>

                </div>

            </div>

            <div class="card-body">

                <!-- ================================= -->
                <!-- AZONOSÍTÓ                        -->
                <!-- ================================= -->

                <input type="hidden"
                       class="time-slot-id"
                       name="TimeSlots[${index}].TimeSlotId"
                       value="0" />

                <!-- ================================= -->
                <!-- IDŐSÁV NEVE                      -->
                <!-- ================================= -->

                <div class="mb-3">

                    <label class="form-label fw-semibold">
                        Idősáv neve
                    </label>

                    <input type="text"
                           class="form-control time-slot-name"
                           name="TimeSlots[${index}].TimeSlotName"
                           value="${slotNumber}. idősáv"
                           placeholder="${slotNumber}. idősáv"
                           maxlength="100"
                           required />

                </div>

                <div class="row">

                    <!-- ================================= -->
                    <!-- KEZDÉS                           -->
                    <!-- ================================= -->

                    <div class="col-md-6 mb-3">

                        <label class="form-label fw-semibold">
                            Kezdés
                        </label>

                        <input type="text"
                               class="form-control time-slot-start"
                               name="TimeSlots[${index}].StartTime"
                               placeholder="08:00"
                               pattern="([01][0-9]|2[0-3]):[0-5][0-9]"
                               title="24 órás időformátum, például 08:00."
                               inputmode="numeric"
                               maxlength="5"
                               autocomplete="off"
                               required />

                    </div>

                    <!-- ================================= -->
                    <!-- BEFEJEZÉS                        -->
                    <!-- ================================= -->

                    <div class="col-md-6 mb-3">

                        <label class="form-label fw-semibold">
                            Befejezés
                        </label>

                        <input type="text"
                               class="form-control time-slot-end"
                               name="TimeSlots[${index}].EndTime"
                               placeholder="10:00"
                               pattern="([01][0-9]|2[0-3]):[0-5][0-9]"
                               title="24 órás időformátum, például 10:00."
                               inputmode="numeric"
                               maxlength="5"
                               autocomplete="off"
                               required />

                    </div>

                </div>

            </div>
        `;

        return card;

    }

    // =========================================
    // ÚJ IDŐSÁV HOZZÁADÁSA
    // =========================================

    addButton.addEventListener("click", function () {

        const cards = getTimeSlotCards();

        const newCard =
            createTimeSlotCard(cards.length);

        container.appendChild(newCard);

        updateTimeSlots();

        // Az új kártyához görgetünk.

        newCard.scrollIntoView({
            behavior: "smooth",
            block: "center"
        });

        // A névmező fókuszt kap.

        const nameInput =
            newCard.querySelector(".time-slot-name");

        if (nameInput) {

            nameInput.focus({
                preventScroll: true
            });

        }

    });

    // =========================================
    // IDŐSÁV TÖRLÉSE
    // =========================================

    container.addEventListener("click", function (event) {

        const removeButton =
            event.target.closest(".remove-time-slot");

        if (!removeButton) {
            return;
        }

        const card =
            removeButton.closest(".time-slot-card");

        if (!card) {
            return;
        }

        // =====================================
        // HASZNÁLATBAN LÉVŐ IDŐSÁV VÉDELME
        // =====================================

        if (isProtected(card)) {
            return;
        }

        // =====================================
        // MINIMUM KÉT IDŐSÁV
        // =====================================

        const cards = getTimeSlotCards();

        if (cards.length <= MIN_TIME_SLOTS) {
            return;
        }

        // =====================================
        // TÖRLÉS
        // =====================================

        card.remove();

        updateTimeSlots();

    });

    // =========================================
    // ŰRLAP BEKÜLDÉSE
    // =========================================

    form.addEventListener("submit", function (event) {

        const cards = getTimeSlotCards();

        // =====================================
        // MINIMUM KÉT IDŐSÁV
        // =====================================

        if (cards.length < MIN_TIME_SLOTS) {

            event.preventDefault();

            alert(
                "Legalább két idősávot meg kell adni."
            );

            return;
        }

        // =====================================
        // MEZŐNEVEK VÉGSŐ FRISSÍTÉSE
        // =====================================

        updateTimeSlots();

    });

    // =========================================
    // KEZDETI ÁLLAPOT
    // =========================================

    updateTimeSlots();

});