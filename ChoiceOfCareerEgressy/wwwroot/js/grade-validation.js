document.addEventListener("DOMContentLoaded", function () {

    // Megengedett formátumok:
    // 9.E, 10.S, 11.K, 12.I, 13.ES, 9.Kny stb.

    const gradePattern =
        /^(9|10|11|12|13|14)\.(?:KNy|[ESKI]{1,2})$/;

    const gradeInputs = document.querySelectorAll(
        ".grade-name-validation"
    );

    gradeInputs.forEach(function (input) {

        function validateGrade() {

            const value = input.value.trim();

            if (value === "") {
                input.setCustomValidity("");
                return;
            }

            if (!gradePattern.test(value)) {
                input.setCustomValidity(
                    "Hibás osztálynév! Például: 9.E, 10.K, 12.ES vagy 9.Kny."
                );
            }
            else {
                input.setCustomValidity("");
            }
        }

        input.addEventListener("input", validateGrade);

        input.addEventListener("blur", function () {
            input.value = input.value.trim();
            validateGrade();
        });

        // Már meglévő érték ellenőrzése
        validateGrade();

    });

});