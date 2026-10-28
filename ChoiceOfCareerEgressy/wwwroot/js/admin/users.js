// =========================================
// FELHASZNÁLÓKEZELÉS - SZEREPKÖR SZŰRÉS
// =========================================

function updateRoleFilter() {

    const selectedRole =
        document.getElementById("roleFilter").value;

    const roleInput =
        document.getElementById("roleValue");

    const homeroomInput =
        document.getElementById("homeroomValue");

    // Osztályfőnök kiválasztása
    if (selectedRole === "Homeroom") {

        roleInput.value = "";
        homeroomInput.value = "true";

    }
    else {

        roleInput.value = selectedRole;
        homeroomInput.value = "false";

    }
}


// =========================================
// OLDAL BETÖLTÉSE
// =========================================

document.addEventListener("DOMContentLoaded", function () {

    const roleFilter =
        document.getElementById("roleFilter");

    if (roleFilter) {

        updateRoleFilter();

        roleFilter.addEventListener(
            "change",
            updateRoleFilter
        );

    }

});