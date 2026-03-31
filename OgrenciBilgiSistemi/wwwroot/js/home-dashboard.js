(function () {
    const elToplam = document.getElementById('kpi-toplam');
    const elYemek = document.getElementById('kpi-yemekhane');
    const elCikis = document.getElementById('kpi-anakapi');
    const lineEl = document.getElementById('dashboardLine');
    const locale = 'tr-TR';
    let lineChart = null;

    // --- KPI'lar ---
    fetch('/Home/DashboardStats', { cache: 'no-store' })
        .then(r => r.ok ? r.json() : Promise.reject(r.status))
        .then(d => {
            console.log('DashboardStats ->', d);

            const toplam = d.toplamOgrenci ?? d.ToplamOgrenci ?? 0;
            const ymk = d.bugunYemekhaneGiris ?? d.BugunYemekhaneGiris ?? 0;
            const ank = d.bugunAnakapiCikis ?? d.BugunAnakapiCikis ?? 0;

            if (elToplam) elToplam.textContent = Number(toplam).toLocaleString(locale);
            if (elYemek) elYemek.textContent = Number(ymk).toLocaleString(locale);
            if (elCikis) elCikis.textContent = Number(ank).toLocaleString(locale);
        })
        .catch(err => console.error('DashboardStats yüklenemedi:', err));

    // --- Çizgi grafik ---
    fetch('/Home/DashboardSeries', { cache: 'no-store' })
        .then(r => r.ok ? r.json() : Promise.reject(r.status))
        .then(s => {
            console.log('DashboardSeries ->', s);
            if (!lineEl || !window.Chart) return;

            // Her iki casing'i de destekle
            const labelsRaw = s.gunEtiketleri ?? s.GunEtiketleri ?? [];
            const yemekRaw = (Array.isArray(s.yemekhaneGiris) ? s.yemekhaneGiris :
                (Array.isArray(s.YemekhaneGiris) ? s.YemekhaneGiris : []));
            const cikisRaw = (Array.isArray(s.anakapiCikis) ? s.anakapiCikis :
                (Array.isArray(s.AnakapiCikis) ? s.AnakapiCikis : []));

            // Tip güvenliği: sayıya çevir
            const labels = labelsRaw.map(x => String(x));
            const yemek = yemekRaw.map(n => Number(n) || 0);
            const cikis = cikisRaw.map(n => Number(n) || 0);

            if (!labels.length || (!yemek.length && !cikis.length)) {
                console.warn('DashboardSeries: gösterilecek veri yok.');
                return;
            }

            if (lineChart) { lineChart.destroy(); lineChart = null; }

            const isSmall = window.matchMedia('(max-width: 576px)').matches;
            const maxTicks = isSmall ? 8 : 14;
            const pointR = isSmall ? 2.5 : 4;
            const pointRHover = isSmall ? 4 : 6;

            lineChart = new Chart(lineEl, {
                type: 'line',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Yemekhane Giriş',
                            data: yemek,
                            borderColor: '#28a745',
                            backgroundColor: 'rgba(40,167,69,0.10)',
                            tension: 0.4,
                            fill: true,
                            pointRadius: pointR,
                            pointHoverRadius: pointRHover,
                            pointBackgroundColor: '#28a745'
                        },
                        {
                            label: 'Anakapı Çıkış',
                            data: cikis,
                            borderColor: '#dc3545',
                            backgroundColor: 'rgba(220,53,69,0.10)',
                            tension: 0.4,
                            fill: true,
                            pointRadius: pointR,
                            pointHoverRadius: pointRHover,
                            pointBackgroundColor: '#dc3545'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: { intersect: false, mode: 'index' },
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: { font: { size: isSmall ? 11 : 13 }, color: '#555' }
                        },
                        tooltip: {
                            backgroundColor: 'rgba(0,0,0,0.8)',
                            titleColor: '#fff',
                            bodyColor: '#fff',
                            callbacks: {
                                label: ctx => `${ctx.dataset.label}: ${Number(ctx.raw ?? 0).toLocaleString(locale)} öğrenci`
                            }
                        }
                    },
                    scales: {
                        x: {
                            title: { display: true, text: 'Gün', color: '#555', font: { size: isSmall ? 12 : 13 } },
                            grid: { color: 'rgba(200,200,200,0.1)' },
                            ticks: { autoSkip: true, maxTicksLimit: maxTicks }
                        },
                        y: {
                            beginAtZero: true,
                            ticks: { precision: 0 },
                            title: { display: true, text: 'Öğrenci Sayısı', color: '#555', font: { size: isSmall ? 12 : 13 } },
                            grid: { color: 'rgba(200,200,200,0.2)' }
                        }
                    }
                }
            });

            window.addEventListener('resize', () => {
                const small = window.matchMedia('(max-width: 576px)').matches;
                const pr = small ? 2.5 : 4;
                const phr = small ? 4 : 6;
                lineChart.data.datasets.forEach(ds => {
                    ds.pointRadius = pr;
                    ds.pointHoverRadius = phr;
                });
                lineChart.options.scales.x.ticks.maxTicksLimit = small ? 8 : 14;
                lineChart.update('none');
            }, { passive: true });
        })
        .catch(err => console.error('DashboardSeries yüklenemedi:', err));
})();