document.addEventListener("DOMContentLoaded", function () {

    // =====================================
    // ESEMÉNYVÁLASZTÓ
    // =====================================

    document.querySelectorAll("[data-auto-submit=true]").forEach(select => {

        select.addEventListener("change", function () {

            select.form?.requestSubmit();

        });

    });

    // =====================================
    // FOLYAMATJELZŐK SZÉLESSÉGE
    // =====================================

    document.querySelectorAll(".progress-bar[data-progress]").forEach(bar => {

        const value = Number(bar.dataset.progress);

        if (Number.isFinite(value)) {

            const percentage = Math.max(
                0,
                Math.min(100, value)
            );

            bar.style.width = `${percentage}%`;
        }

    });

    // =====================================
    // DASHBOARD ADATOK BEOLVASÁSA
    // =====================================

    const dataElement = document.getElementById("dashboardData");

    if (!dataElement) {
        console.error("Nem található a dashboardData elem.");
        return;
    }

    let data;

    try {
        data = JSON.parse(dataElement.dataset.json);
    }
    catch (error) {
        console.error(
            "Hiba a Dashboard JSON-adatainak feldolgozásakor:",
            error
        );
        return;
    }

    // =====================================
    // CHART.JS ELLENŐRZÉSE
    // =====================================

    if (typeof Chart === "undefined") {
        console.error("A Chart.js nem töltődött be.");
        return;
    }

    // =====================================
    // JELENTKEZÉSEK ÁGAZATONKÉNT
    // =====================================

    const branchChart = document.getElementById("branchChart");

    if (branchChart) {

        new Chart(branchChart, {

            type: "bar",

            data: {
                labels: data.branchLabels,

                datasets: [{
                    label: "Aktív jelentkezések",
                    data: data.branchValues,
                    backgroundColor: "#3b82f6",
                    borderRadius: 6
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,

                plugins: {
                    legend: {
                        display: false
                    }
                },

                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }

        });
    }

    // =====================================
    // PROGRAMOK KIHASZNÁLTSÁGA
    // =====================================

    const capacityChart = document.getElementById("capacityChart");

    if (capacityChart) {

        new Chart(capacityChart, {

            type: "doughnut",

            data: {
                labels: [
                    "Betelt",
                    "Részben feltöltött",
                    "Jelentkezés nélküli"
                ],

                datasets: [{
                    data: data.capacityValues,

                    backgroundColor: [
                        "#ef4444",
                        "#f59e0b",
                        "#10b981"
                    ],

                    borderWidth: 0
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "70%",

                plugins: {
                    legend: {
                        position: "bottom"
                    }
                }
            }

        });
    }

    // =====================================
    // JELENTKEZÉSEK IDŐBELI ALAKULÁSA
    // =====================================

    const trendChart = document.getElementById("applicationTrendChart");

    if (trendChart) {

        new Chart(trendChart, {

            type: "line",

            data: {
                labels: data.trendLabels,

                datasets: [{
                    label: "Aktív jelentkezések",
                    data: data.trendValues,

                    borderColor: "#6366f1",
                    backgroundColor: "rgba(99, 102, 241, 0.1)",

                    borderWidth: 2,
                    fill: true,
                    tension: 0.3,
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,

                plugins: {
                    legend: {
                        display: false
                    }
                },

                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }

        });
    }

});