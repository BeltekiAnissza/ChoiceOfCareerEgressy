document.addEventListener("DOMContentLoaded", function () {

    // =====================================
    // ESEMÉNYVÁLASZTÓ
    // =====================================

    const eventSelect = document.getElementById("eventSelect");
    const eventForm = document.getElementById("eventForm");

    if (!eventSelect || !eventForm) {
        return;
    }

    eventSelect.addEventListener("change", function () {
        eventForm.requestSubmit();
    });

});