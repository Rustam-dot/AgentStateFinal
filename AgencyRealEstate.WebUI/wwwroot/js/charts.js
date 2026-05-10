
window.setupDoubleCharts = (labels, revenueData, transactionData) => {
    console.log('setupDoubleCharts вызвана');
    Chart.defaults.color = '#aaaaaa';
    Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.08)';

    if (window._revenueChart instanceof Chart) window._revenueChart.destroy();
    if (window._transactionsChart instanceof Chart) window._transactionsChart.destroy();

    const revenueCanvas = document.getElementById('revenueChart');
    if (revenueCanvas) {
        window._revenueChart = new Chart(revenueCanvas, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Выручка (₽)',
                    data: revenueData,
                    borderColor: '#f39c12',
                    backgroundColor: 'rgba(243, 156, 18, 0.1)',
                    fill: true,
                    tension: 0.4,
                    borderWidth: 3,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: '#f39c12',
                    pointRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        backgroundColor: '#1a1a1a',
                        titleColor: '#fff',
                        bodyColor: '#f39c12',
                        borderColor: 'rgba(255,255,255,0.1)',
                        borderWidth: 1,
                        padding: 15,
                        displayColors: false,
                        callbacks: {
                            label: function (context) {
                                let label = context.dataset.label || '';
                                if (label) label += ': ';
                                if (context.parsed.y !== null) {
                                    label += new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'RUB', maximumFractionDigits: 0 }).format(context.parsed.y);
                                }
                                return label;
                            }
                        }
                    }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: {
                        ticks: {
                            callback: function (value) {
                                return new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'RUB', maximumFractionDigits: 0 }).format(value);
                            }
                        }
                    }
                }
            }
        });
    }

    const transactionsCanvas = document.getElementById('transactionsChart');
    if (transactionsCanvas) {
        window._transactionsChart = new Chart(transactionsCanvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Сделок',
                    data: transactionData,
                    backgroundColor: 'rgba(52, 152, 219, 0.2)',
                    borderColor: '#3498db',
                    borderWidth: 2,
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1a1a1a',
                        titleColor: '#fff',
                        bodyColor: '#3498db',
                        padding: 15,
                    }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: { grid: { display: true }, ticks: { stepSize: 1 } }
                }
            }
        });
    }
};