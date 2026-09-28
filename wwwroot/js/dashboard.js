(function () {
    const state = {
        charts: new Map(),
        tables: new Map(),
        kpis: new Map()
    };

    const endpoints = {
        status: "/api/dashboard/connection-status",
        filters: "/api/dashboard/filter-options",
        kpis: "/api/dashboard/kpis",
        calculatedKpis: "/api/dashboard/calculated-kpis"
    };

    const palette = ["#1f6feb", "#18c4d8", "#0f766e", "#64748b", "#38bdf8", "#16a34a", "#2563eb", "#94a3b8"];

    document.addEventListener("DOMContentLoaded", () => {
        if (typeof Chart !== "undefined") {
            Chart.defaults.font.family = '"Segoe UI", Arial, sans-serif';
            Chart.defaults.color = "#475569";
            Chart.defaults.animation.duration = 900;
            Chart.defaults.animation.easing = "easeOutQuart";
        }

        loadConnectionStatus();
        initializeFilters();
        initializeRefreshButton();
        refreshDashboard();
    });

    function initializeRefreshButton() {
        const button = document.getElementById("refreshDashboard");
        if (!button) {
            return;
        }

        button.addEventListener("click", () => {
            button.classList.add("is-refreshing");
            refreshDashboard().finally(() => {
                setTimeout(() => button.classList.remove("is-refreshing"), 180);
            });
        });
    }

    function initializeFilters() {
        document.querySelectorAll(".dashboard-filter").forEach(select => {
            select.addEventListener("change", () => refreshDashboard());
        });

        const resetButton = document.getElementById("resetFilters");
        if (resetButton) {
            resetButton.addEventListener("click", () => {
                document.querySelectorAll(".dashboard-filter").forEach(select => {
                    select.value = "";
                });
                refreshDashboard();
            });
        }

        loadFilterOptions();
    }

    async function refreshDashboard() {
        clearMessage();
        await Promise.all([
            loadConnectionStatus(),
            loadKpis(),
            loadCalculatedKpis(),
            loadCharts(),
            loadTables()
        ]);
        updateRefreshStamp();
    }

    async function loadConnectionStatus() {
        const pill = document.getElementById("connectionStatus");
        if (!pill) {
            return;
        }

        try {
            const status = await requestJson(endpoints.status, new URLSearchParams());
            pill.textContent = `${status.serverName} / ${status.cubeName}`;
            pill.classList.toggle("offline", !status.isConnected);
            pill.classList.toggle("pending", false);
        } catch (error) {
            pill.textContent = "Cube indisponible";
            pill.classList.add("offline");
            pill.classList.remove("pending");
        }
    }

    async function loadFilterOptions() {
        try {
            const options = await requestJson(endpoints.filters, collectFilters());
            fillSelect("filterYear", options.years);
            fillSelect("filterMonth", options.months);
            fillSelect("filterProduct", options.products);
            fillSelect("filterCustomer", options.customers);
            fillSelect("filterSupplier", options.suppliers);
            fillSelect("filterCountry", options.countries);
        } catch (error) {
            showMessage(error.message);
        }
    }

    async function loadKpis() {
        const kpiElements = [...document.querySelectorAll("[data-kpi]")];
        if (!kpiElements.length) {
            return;
        }

        kpiElements.forEach(element => {
            element.closest(".kpi-card")?.classList.add("is-loading");
        });

        try {
            const data = await requestJson(endpoints.kpis, collectFilters());
            kpiElements.forEach(element => {
                const key = element.dataset.kpi;
                animateKpi(element, Number(data[key] ?? 0), element.dataset.format);
                element.closest(".kpi-card")?.classList.remove("is-loading");
            });
        } catch (error) {
            kpiElements.forEach(element => {
                element.textContent = "N/D";
                element.closest(".kpi-card")?.classList.remove("is-loading");
            });
            showMessage(error.message);
        }
    }

    async function loadCalculatedKpis() {
        const kpiElements = [...document.querySelectorAll("[data-calculated-kpi]")];
        if (!kpiElements.length) {
            return;
        }

        kpiElements.forEach(element => {
            element.closest(".kpi-card")?.classList.add("is-loading");
        });

        try {
            const data = await requestJson(endpoints.calculatedKpis, collectFilters());
            kpiElements.forEach(element => {
                const key = element.dataset.calculatedKpi;
                const value = data[key];
                if (value === null || value === undefined || Number.isNaN(Number(value))) {
                    element.textContent = "N/A";
                    element.closest(".kpi-card")?.classList.remove("is-loading");
                    return;
                }

                animateKpi(element, Number(value), element.dataset.format);
                element.closest(".kpi-card")?.classList.remove("is-loading");
            });

            if (data.message) {
                showMessage(data.message);
            }
        } catch (error) {
            kpiElements.forEach(element => {
                element.textContent = "N/A";
                element.closest(".kpi-card")?.classList.remove("is-loading");
            });
            showMessage(error.message);
        }
    }

    async function loadCharts() {
        const canvases = [...document.querySelectorAll("[data-dashboard-chart]")];
        if (!canvases.length || typeof Chart === "undefined") {
            if (typeof Chart === "undefined") {
                showMessage("Chart.js n'a pas pu etre charge. Verifiez la connexion au CDN.");
            }
            return;
        }

        await Promise.all(canvases.map(loadChart));
    }

    async function loadChart(canvas) {
        const panel = canvas.closest(".panel");
        panel?.classList.add("is-loading");
        removePanelError(panel);

        try {
            const data = await requestJson(canvas.dataset.endpoint, collectFilters());
            canvas.style.display = "";

            if (!Array.isArray(data) || data.length === 0) {
                renderEmptyChart(canvas);
                return;
            }

            updatePanelSummary(panel, data, canvas.dataset.format || "currency");
            const config = buildChartConfig(canvas, data);
            const existing = state.charts.get(canvas);
            if (existing) {
                existing.destroy();
            }

            state.charts.set(canvas, new Chart(canvas, config));
        } catch (error) {
            const existing = state.charts.get(canvas);
            if (existing) {
                existing.destroy();
                state.charts.delete(canvas);
            }
            canvas.style.display = "none";
            renderPanelError(panel, error.message);
            showMessage(error.message);
        } finally {
            panel?.classList.remove("is-loading");
        }
    }

    async function loadTables() {
        const hosts = [...document.querySelectorAll("[data-dashboard-table]")];
        await Promise.all(hosts.map(loadTable));
    }

    async function loadTable(host) {
        host.classList.add("is-loading");
        host.innerHTML = "";

        try {
            const data = await requestJson(host.dataset.endpoint, collectFilters());
            renderTable(host, Array.isArray(data) ? data : []);
        } catch (error) {
            host.innerHTML = `<div class="panel-error">${escapeHtml(error.message)}</div>`;
            showMessage(error.message);
        } finally {
            host.classList.remove("is-loading");
        }
    }

    function buildChartConfig(canvas, data) {
        const context = canvas.getContext("2d");
        const chartType = canvas.dataset.chart || "bar";
        const format = canvas.dataset.format || "currency";
        const labels = data.map(item => item.label);
        const values = data.map(item => Number(item.value || 0));
        const secondaryValues = data.map(item => Number(item.secondaryValue || 0));
        const primaryLabel = canvas.dataset.primaryLabel || "Valeur";
        const secondaryLabel = canvas.dataset.endpoint?.includes("quantity") ? "Achetees" : (canvas.dataset.secondaryLabel || "Achats");

        if (chartType === "doughnut") {
            return {
                type: "doughnut",
                data: {
                    labels,
                    datasets: [{
                        label: primaryLabel,
                        data: values,
                        backgroundColor: labels.map((_, index) => palette[index % palette.length]),
                        borderWidth: 2,
                        borderColor: "#ffffff"
                    }]
                },
                options: chartOptions(format, false, false)
            };
        }

        if (chartType === "line") {
            return {
                type: "line",
                data: {
                    labels,
                    datasets: [{
                        label: primaryLabel,
                        data: values,
                        borderColor: "#1f6feb",
                        backgroundColor: makeGradient(context, "rgba(31, 111, 235, 0.22)", "rgba(31, 111, 235, 0.02)"),
                        borderWidth: 3,
                        pointRadius: 4,
                        tension: 0.34,
                        fill: true
                    }]
                },
                options: chartOptions(format, true, false)
            };
        }

        if (chartType === "mixed") {
            return {
                data: {
                    labels,
                    datasets: [
                        {
                            type: "bar",
                            label: primaryLabel,
                            data: values,
                            backgroundColor: makeGradient(context, "rgba(31, 111, 235, 0.92)", "rgba(24, 196, 216, 0.58)"),
                            borderRadius: 7,
                            maxBarThickness: 42
                        },
                        {
                            type: "line",
                            label: secondaryLabel,
                            data: secondaryValues,
                            borderColor: "#18c4d8",
                            backgroundColor: "rgba(24, 196, 216, 0.16)",
                            borderWidth: 3,
                            pointRadius: 4,
                            tension: 0.3
                        }
                    ]
                },
                options: chartOptions(format, true, false)
            };
        }

        const isHorizontal = chartType === "horizontalBar";
        return {
            type: "bar",
            data: {
                labels,
                datasets: [{
                    label: primaryLabel,
                    data: values,
                    backgroundColor: makeGradient(context, "rgba(31, 111, 235, 0.92)", "rgba(24, 196, 216, 0.58)"),
                    borderRadius: 7,
                    maxBarThickness: 42
                }]
            },
            options: {
                ...chartOptions(format, true, isHorizontal),
                indexAxis: isHorizontal ? "y" : "x"
            }
        };
    }

    function chartOptions(format, showScales, horizontal) {
        const scales = horizontal ? {
            x: {
                beginAtZero: true,
                grid: { color: "#d8e2ef" },
                ticks: {
                    color: "#64748b",
                    callback: value => formatAxis(value, format)
                }
            },
            y: {
                grid: { display: false },
                ticks: {
                    color: "#64748b"
                }
            }
        } : {
            x: {
                grid: { display: false },
                ticks: {
                    color: "#64748b",
                    maxRotation: 35
                }
            },
            y: {
                beginAtZero: true,
                grid: { color: "#d8e2ef" },
                ticks: {
                    color: "#64748b",
                    callback: value => formatAxis(value, format)
                }
            }
        };

        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: {
                duration: 900,
                easing: "easeOutQuart"
            },
            interaction: {
                intersect: false,
                mode: "index"
            },
            plugins: {
                legend: {
                    position: "top",
                    labels: {
                        color: "#475569",
                        boxWidth: 12,
                        boxHeight: 12,
                        useBorderRadius: true
                    }
                },
                tooltip: {
                    backgroundColor: "#071b33",
                    borderColor: "rgba(24, 196, 216, 0.35)",
                    borderWidth: 1,
                    padding: 12,
                    titleFont: {
                        weight: "700"
                    },
                    callbacks: {
                        label: context => {
                            const value = context.parsed.y ?? context.parsed.x ?? context.parsed;
                            return `${context.dataset.label}: ${formatValue(value, format)}`;
                        }
                    }
                }
            },
            scales: showScales ? scales : undefined
        };
    }

    function renderEmptyChart(canvas) {
        const existing = state.charts.get(canvas);
        if (existing) {
            existing.destroy();
            state.charts.delete(canvas);
        }

        const panel = canvas.closest(".panel");
        canvas.style.display = "none";
        renderPanelError(panel, "Aucune donnee disponible pour cette selection.");
    }

    function renderTable(host, rows) {
        const title = host.dataset.title || "Tableau";
        const format = host.dataset.format || "currency";
        const stateKey = host.dataset.endpoint;
        const tableState = state.tables.get(stateKey) || { query: "", desc: true };

        host.innerHTML = `
            <div class="table-toolbar">
                <div>
                    <h3>${escapeHtml(title)}</h3>
                    <span class="sort-hint">Cliquer sur Valeur pour inverser le tri</span>
                </div>
                <input class="form-control form-control-sm" type="search" placeholder="Rechercher" value="${escapeHtml(tableState.query)}">
            </div>
            <div class="table-responsive">
                <table class="table bi-table align-middle">
                    <thead>
                        <tr>
                            <th data-sort="rank">Rang</th>
                            <th data-sort="label">Libelle</th>
                            <th class="text-end" data-sort="value">Valeur</th>
                        </tr>
                    </thead>
                    <tbody></tbody>
                </table>
            </div>
        `;

        const input = host.querySelector("input");
        const tbody = host.querySelector("tbody");

        const draw = () => {
            const query = input.value.trim().toLowerCase();
            state.tables.set(stateKey, { query, desc: tableState.desc });

            const filtered = rows
                .filter(row => String(row.label).toLowerCase().includes(query))
                .sort((a, b) => tableState.desc
                    ? Number(b.value || 0) - Number(a.value || 0)
                    : Number(a.value || 0) - Number(b.value || 0));

            if (!filtered.length) {
                tbody.innerHTML = `<tr><td colspan="3"><div class="empty-state">Aucune ligne disponible.</div></td></tr>`;
                return;
            }

            tbody.innerHTML = filtered.map((row, index) => `
                <tr>
                    <td><span class="rank-badge">${index + 1}</span></td>
                    <td class="fw-semibold">${escapeHtml(row.label)}</td>
                    <td class="text-end"><span class="value-badge">${formatValue(row.value, format)}</span></td>
                </tr>
            `).join("");
        };

        input.addEventListener("input", draw);
        host.querySelector('[data-sort="value"]').addEventListener("click", () => {
            tableState.desc = !tableState.desc;
            draw();
        });

        draw();
    }

    async function requestJson(endpoint, params) {
        const query = params.toString();
        const url = query ? `${endpoint}?${query}` : endpoint;
        const response = await fetch(url, { headers: { "Accept": "application/json" } });
        const payload = await response.json().catch(() => null);

        if (!response.ok || !payload || payload.success === false) {
            throw new Error(payload?.message || "Impossible de charger les donnees du cube SSAS.");
        }

        return payload.data;
    }

    function collectFilters() {
        const params = new URLSearchParams();
        document.querySelectorAll(".dashboard-filter").forEach(select => {
            if (select.value) {
                params.set(select.dataset.filter, select.value);
            }
        });
        return params;
    }

    function fillSelect(id, options) {
        const select = document.getElementById(id);
        if (!select || !Array.isArray(options)) {
            return;
        }

        const current = select.value;
        const first = select.options[0]?.outerHTML || '<option value="">Tous</option>';
        select.innerHTML = first + options.map(option =>
            `<option value="${escapeHtml(option.value)}">${escapeHtml(option.label)}</option>`
        ).join("");

        if ([...select.options].some(option => option.value === current)) {
            select.value = current;
        }
    }

    function animateKpi(element, target, format) {
        const previous = state.kpis.get(element) ?? 0;
        const duration = 760;
        const startedAt = performance.now();
        state.kpis.set(element, target);

        const tick = now => {
            const progress = Math.min((now - startedAt) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 4);
            const current = previous + ((target - previous) * eased);
            element.innerHTML = formatKpiValue(current, format);

            if (progress < 1) {
                requestAnimationFrame(tick);
            } else {
                element.innerHTML = formatKpiValue(target, format);
            }
        };

        requestAnimationFrame(tick);
    }

    function updatePanelSummary(panel, data, format) {
        if (!panel) {
            return;
        }

        const header = panel.querySelector(".panel-header");
        if (!header) {
            return;
        }

        const total = data.reduce((sum, item) => sum + Number(item.value || 0), 0);
        let stat = header.querySelector(".panel-stat");
        if (!stat) {
            stat = document.createElement("div");
            stat.className = "panel-stat";
            header.appendChild(stat);
        }

        stat.innerHTML = `
            <strong>${formatValue(total, format)}</strong>
            <span>${data.length} points</span>
        `;
    }

    function makeGradient(context, start, end) {
        if (!context) {
            return start;
        }

        const gradient = context.createLinearGradient(0, 0, 0, 320);
        gradient.addColorStop(0, start);
        gradient.addColorStop(1, end);
        return gradient;
    }

    function updateRefreshStamp() {
        const stamp = document.getElementById("lastRefresh");
        if (!stamp) {
            return;
        }

        stamp.textContent = `Actualise ${new Intl.DateTimeFormat("fr-FR", {
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit"
        }).format(new Date())}`;
    }

    function showMessage(message) {
        const alert = document.getElementById("connectionAlert");
        if (!alert) {
            return;
        }

        alert.textContent = message;
        alert.classList.add("is-visible");
    }

    function clearMessage() {
        const alert = document.getElementById("connectionAlert");
        if (!alert) {
            return;
        }

        alert.textContent = "";
        alert.classList.remove("is-visible");
    }

    function renderPanelError(panel, message) {
        if (!panel) {
            return;
        }

        removePanelError(panel);
        const error = document.createElement("div");
        error.className = "panel-error";
        error.textContent = message;
        panel.appendChild(error);
    }

    function removePanelError(panel) {
        panel?.querySelectorAll(".panel-error").forEach(error => error.remove());
    }

    function formatValue(value, format) {
        const number = Number(value || 0);
        if (format === "percent") {
            return new Intl.NumberFormat("fr-FR", {
                style: "percent",
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            }).format(number);
        }

        if (format === "number") {
            return new Intl.NumberFormat("fr-FR", {
                minimumFractionDigits: 0,
                maximumFractionDigits: 0
            }).format(number);
        }

        return `${new Intl.NumberFormat("fr-FR", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }).format(number)} USD`;
    }

    function formatKpiValue(value, format) {
        if (format !== "currency") {
            return escapeHtml(formatValue(value, format));
        }

        const formatted = new Intl.NumberFormat("fr-FR", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }).format(Number(value || 0));
        const parts = formatted.split(",");
        const integerPart = parts[0] || "0";
        const decimalPart = parts[1] || "00";

        return `${escapeHtml(integerPart)}<span class="decimal">,${escapeHtml(decimalPart)}</span><span class="unit">USD</span>`;
    }

    function formatAxis(value, format) {
        if (format === "percent") {
            return `${Number(value * 100).toFixed(0)} %`;
        }

        return new Intl.NumberFormat("fr-FR", {
            notation: "compact",
            maximumFractionDigits: 1
        }).format(Number(value || 0));
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }
})();
