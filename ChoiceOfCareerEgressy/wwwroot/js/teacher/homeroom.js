document.addEventListener("DOMContentLoaded", function () {

    const storageKey = "homeroom-page-state:" + window.location.pathname;

    // =====================================================
    // ÁLLAPOTKEZELÉS
    // =====================================================

    function loadState() {
        try {
            return JSON.parse(sessionStorage.getItem(storageKey)) || {};
        } catch {
            return {};
        }
    }

    function saveState() {
        const openStudents = [];

        document.querySelectorAll(
            ".accordion-collapse.show[data-student-id]"
        ).forEach(function (element) {
            openStudents.push(element.dataset.studentId);
        });

        sessionStorage.setItem(storageKey, JSON.stringify({
            openStudents: openStudents,
            scrollY: window.scrollY
        }));
    }

    function restoreState() {
        const state = loadState();

        if (Array.isArray(state.openStudents)) {
            state.openStudents.forEach(function (studentId) {
                const element = document.querySelector(
                    '.accordion-collapse[data-student-id="' +
                    studentId + '"]'
                );

                if (!element) return;

                bootstrap.Collapse.getOrCreateInstance(
                    element,
                    { toggle: false }
                ).show();
            });
        }

        if (typeof state.scrollY === "number") {
            setTimeout(function () {
                window.scrollTo(0, state.scrollY);
            }, 450);
        }
    }

    document.querySelectorAll(
        ".accordion-collapse[data-student-id]"
    ).forEach(function (element) {

        element.addEventListener("shown.bs.collapse", saveState);
        element.addEventListener("hidden.bs.collapse", saveState);

    });

    // =====================================================
    // ÜZENETEK
    // =====================================================

    function showMessage(message, success, studentElement) {
        const existing = studentElement.querySelector(
            ".homeroom-ajax-message"
        );

        if (existing) {
            existing.remove();
        }

        const alert = document.createElement("div");

        alert.className =
            "alert mt-3 homeroom-ajax-message " +
            (success ? "alert-success" : "alert-danger");

        alert.textContent = message;
        alert.setAttribute("role", "alert");

        const body = studentElement.querySelector(".accordion-body");

        if (body) {
            body.prepend(alert);
        }
    }

    // =====================================================
    // JELENTKEZÉSI ADATOK FRISSÍTÉSE
    // =====================================================

    function updateStudent(data) {

        const studentElement = document.querySelector(
            '.accordion-collapse[data-student-id="' +
            data.studentId + '"]'
        );

        if (!studentElement) return;

        const accordionItem = studentElement.closest(".accordion-item");

        // Jelentkezések számának frissítése
        const badge = accordionItem.querySelector(".badge");

        if (badge) {
            badge.textContent =
                data.registrationCount + " / " +
                data.totalSlots + " program";

            badge.classList.remove(
                "bg-success",
                "bg-warning",
                "text-dark"
            );

            if (data.registrationCount === data.totalSlots) {
                badge.classList.add("bg-success");
            } else {
                badge.classList.add("bg-warning", "text-dark");
            }
        }

        // Az adott diák idősávjainak frissítése
        data.timeSlots.forEach(function (slot) {

            const form = studentElement.querySelector(
                'input[name="Change.TimeSlotId"][value="' +
                slot.timeSlotId + '"]'
            )?.closest("form");

            if (!form) return;

            const container = form.closest(".border.rounded");

            if (!container) return;

            const application = data.applications.find(function (a) {
                return a.applicationId === slot.applicationId;
            });

            // Korábbi jelentkezési információ eltávolítása
            container.querySelectorAll(
                ".homeroom-current-application"
            ).forEach(function (element) {
                element.remove();
            });

            // Új jelentkezési információ
            const info = document.createElement("div");
            info.className = "homeroom-current-application";

            if (application) {
                const name = document.createElement("p");
                const strong = document.createElement("strong");
                strong.textContent = application.planName;
                name.appendChild(strong);
                name.appendChild(
                    document.createTextNode(
                        " – " + application.locationName
                    )
                );

                const time = document.createElement("p");
                time.className = "small text-muted";
                time.textContent =
                    application.beginDate + " – " +
                    application.endDate;

                info.appendChild(name);
                info.appendChild(time);
            } else {
                const empty = document.createElement("p");
                empty.className = "text-warning";
                empty.textContent = "Még nem jelentkezett.";
                info.appendChild(empty);
            }

            form.before(info);

            // Programválasztó frissítése
            const select = form.querySelector(
                'select[name="Change.PlanSessionId"]'
            );

            if (select) {
                select.replaceChildren();

                const placeholder = new Option(
                    "Válassz programot!",
                    ""
                );

                select.add(placeholder);

                slot.programs.forEach(function (program) {
                    const option = new Option(
                        program.planName +
                        " – " +
                        program.locationName +
                        " (szabad: " +
                        program.availablePlaces + ")",
                        program.planSessionId
                    );

                    select.add(option);
                });

                select.value =
                    slot.selectedPlanSessionId?.toString() || "";
            }

            // Törlés gomb / űrlap frissítése
            const deleteForm = container.querySelector(
                ".homeroom-delete-form"
            );

            if (application) {
                if (deleteForm) {
                    const applicationIdInput =
                        deleteForm.querySelector(
                            'input[name="applicationId"]'
                        );

                    if (applicationIdInput) {
                        applicationIdInput.value =
                            application.applicationId;
                    }
                } else {
                    const newDeleteForm = document.createElement("form");

                    newDeleteForm.method = "post";
                    newDeleteForm.action =
                        form.action.replace(
                            "handler=Change",
                            "handler=Delete"
                        );

                    newDeleteForm.className =
                        "homeroom-action-form homeroom-delete-form";

                    const studentId = document.createElement("input");
                    studentId.type = "hidden";
                    studentId.name = "studentId";
                    studentId.value = data.studentId;

                    const applicationId = document.createElement("input");
                    applicationId.type = "hidden";
                    applicationId.name = "applicationId";
                    applicationId.value = application.applicationId;

                    const eventId = document.createElement("input");
                    eventId.type = "hidden";
                    eventId.name = "eventId";
                    eventId.value = form.querySelector(
                        'input[name="Change.EventId"]'
                    ).value;

                    const token = form.querySelector(
                        'input[name="__RequestVerificationToken"]'
                    );

                    if (token) {
                        newDeleteForm.appendChild(token.cloneNode(true));
                    }

                    const button = document.createElement("button");
                    button.type = "submit";
                    button.className =
                        "btn btn-outline-danger btn-sm";
                    button.textContent = "Jelentkezés törlése";

                    newDeleteForm.append(
                        studentId,
                        applicationId,
                        eventId,
                        button
                    );

                    container.appendChild(newDeleteForm);
                }
            } else if (deleteForm) {
                deleteForm.remove();
            }
        });
    }

    // =====================================================
    // AJAX MENTÉS ÉS TÖRLÉS
    // =====================================================

    document.addEventListener("submit", async function (event) {

        const form = event.target.closest(".homeroom-action-form");

        if (!form) return;

        event.preventDefault();

        if (form.dataset.submitting === "true") return;

        const studentElement = form.closest(
            ".accordion-collapse[data-student-id]"
        );

        if (!studentElement) return;

        if (form.classList.contains("homeroom-delete-form")) {
            if (!confirm("Biztosan törlöd a jelentkezést?")) {
                return;
            }
        }

        form.dataset.submitting = "true";

        const submitButton = form.querySelector(
            'button[type="submit"]'
        );

        if (submitButton) {
            submitButton.disabled = true;
        }

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                credentials: "same-origin"
            });

            const contentType =
                response.headers.get("content-type") || "";

            if (!contentType.includes("application/json")) {
                throw new Error(
                    "A szerver nem JSON-választ küldött. " +
                    "Ellenőrizd a bejelentkezést és az AJAX-kezelést."
                );
            }

            const data = await response.json();

            if (!response.ok || !data.success) {
                throw new Error(
                    data.message || "A művelet nem sikerült."
                );
            }

            updateStudent(data);

            showMessage(
                data.message,
                true,
                studentElement
            );

            saveState();

        } catch (error) {
            showMessage(
                error.message || "Ismeretlen hiba történt.",
                false,
                studentElement
            );
        } finally {
            form.dataset.submitting = "false";

            if (submitButton && submitButton.isConnected) {
                submitButton.disabled = false;
            }
        }
    });

    // =====================================================
    // OLDAL ÁLLAPOTÁNAK MEGŐRZÉSE
    // =====================================================

    let scrollTimeout;

    window.addEventListener("scroll", function () {
        clearTimeout(scrollTimeout);

        scrollTimeout = setTimeout(saveState, 150);
    });

    restoreState();

});