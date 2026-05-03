/**
 * GoldAI Charts – Chart.js integration helper
 *
 * Works in both static SSR (first load via DOMContentLoaded) and
 * Blazor Enhanced Navigation (via Blazor.addEventListener 'enhancedload').
 *
 * Each chart stores its data in a hidden <input type="hidden"> element so
 * the server can embed JSON without escaping issues.  The canvas id and
 * the data input id must follow the convention below.
 */
(function () {
    'use strict';

    // Registry so we can destroy old Chart.js instances before re-creating them.
    var _charts = {};

    function destroyChart(id) {
        if (_charts[id]) {
            _charts[id].destroy();
            delete _charts[id];
        }
    }

    function getJson(inputId) {
        var el = document.getElementById(inputId);
        if (!el) return null;
        try {
            return JSON.parse(el.value);
        } catch (e) {
            console.warn('GoldAI Charts: could not parse JSON from #' + inputId, e);
            return null;
        }
    }

    // ── Chart initialisers ────────────────────────────────────────────────────

    function initPredictionChart() {
        var canvas = document.getElementById('predictionChart');
        if (!canvas) return;
        var data = getJson('predictionChartData');
        if (!data) return;

        destroyChart('predictionChart');

        _charts['predictionChart'] = new Chart(canvas, {
            type: 'line',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        label: 'Gold ↑ Probability',
                        data: data.goldUp,
                        borderColor: '#d4a017',
                        backgroundColor: 'rgba(212,160,23,0.12)',
                        fill: true,
                        tension: 0.35,
                        pointRadius: 3
                    },
                    {
                        label: 'Silver ↑ Probability',
                        data: data.silverUp,
                        borderColor: '#808080',
                        backgroundColor: 'rgba(128,128,128,0.12)',
                        fill: true,
                        tension: 0.35,
                        pointRadius: 3
                    }
                ]
            },
            options: {
                responsive: true,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return ctx.dataset.label + ': ' +
                                    (ctx.parsed.y * 100).toFixed(1) + '%';
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        min: 0, max: 1,
                        ticks: {
                            callback: function (v) { return (v * 100).toFixed(0) + '%'; }
                        },
                        title: { display: true, text: 'Up Probability' }
                    },
                    x: { title: { display: true, text: 'Date' } }
                }
            }
        });
    }

    function initWalletHistoryChart() {
        var canvas = document.getElementById('walletHistoryChart');
        if (!canvas) return;
        var data = getJson('walletHistoryChartData');
        if (!data) return;

        destroyChart('walletHistoryChart');

        _charts['walletHistoryChart'] = new Chart(canvas, {
            type: 'line',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        label: 'Portfolio Value (IRT)',
                        data: data.values,
                        borderColor: '#1b6ec2',
                        backgroundColor: 'rgba(27,110,194,0.1)',
                        fill: true,
                        tension: 0.35,
                        pointRadius: 3
                    }
                ]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return 'Value: ' +
                                    ctx.parsed.y.toLocaleString('en-US') + ' ﷼';
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        ticks: {
                            callback: function (v) {
                                if (v >= 1e9) return (v / 1e9).toFixed(1) + 'B';
                                if (v >= 1e6) return (v / 1e6).toFixed(1) + 'M';
                                if (v >= 1e3) return (v / 1e3).toFixed(0) + 'K';
                                return v;
                            }
                        },
                        title: { display: true, text: 'Portfolio Value (IRT)' }
                    },
                    x: { title: { display: true, text: 'Date' } }
                }
            }
        });
    }

    function initWalletShareChart() {
        var canvas = document.getElementById('walletShareChart');
        if (!canvas) return;
        var data = getJson('walletShareChartData');
        if (!data) return;

        destroyChart('walletShareChart');

        _charts['walletShareChart'] = new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        data: data.values,
                        backgroundColor: [
                            'rgba(212,160,23,0.85)',
                            'rgba(140,140,140,0.85)',
                            'rgba(56,161,105,0.85)'
                        ],
                        borderColor: ['#d4a017', '#8c8c8c', '#38a169'],
                        borderWidth: 2
                    }
                ]
            },
            options: {
                responsive: true,
                cutout: '60%',
                plugins: {
                    legend: { position: 'right' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                var total = ctx.dataset.data.reduce(
                                    function (a, b) { return a + b; }, 0);
                                var pct = total > 0
                                    ? ((ctx.parsed / total) * 100).toFixed(1)
                                    : '0.0';
                                return ctx.label + ': ' +
                                    ctx.parsed.toLocaleString('en-US') + ' ﷼ (' + pct + '%)';
                            }
                        }
                    }
                }
            }
        });
    }

    // ── Main init ─────────────────────────────────────────────────────────────

    function initAllCharts() {
        initPredictionChart();
        initWalletHistoryChart();
        initWalletShareChart();
    }

    // First page load (static SSR or full refresh)
    document.addEventListener('DOMContentLoaded', initAllCharts);

    // Blazor Enhanced Navigation – fires after each in-page navigation
    document.addEventListener('blazor:load', function () {
        if (typeof Blazor !== 'undefined') {
            Blazor.addEventListener('enhancedload', initAllCharts);
        }
    });
})();
