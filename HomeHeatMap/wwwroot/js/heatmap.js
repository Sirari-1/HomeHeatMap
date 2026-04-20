window.floridaHeatMap = {
    map: null,
    heatLayer: null,
    markersLayer: null,
    cityCoords: {},
    coordsCacheKey: 'florida-city-coords-v1',
    geocodeBatchSize: 80,

    getColor(percentile) {
        const r = Math.round(180 - (percentile / 100) * 180);
        const g = Math.round((percentile / 100) * 160);
        return `rgb(${r},${g},30)`;
    },

    getSafetyGrade(violentRate) {
        const rate = Number(violentRate);
        if (!Number.isFinite(rate)) return 'N/A';
        if (rate < 100) return 'A+';
        if (rate < 200) return 'A';
        if (rate < 300) return 'B';
        if (rate < 400) return 'C';
        if (rate < 600) return 'D';
        if (rate < 1000) return 'F';
        return 'F-';
    },

    getSafetyGradeColor(grade) {
        switch (grade) {
            case 'A+': return 'rgb(0,140,30)';
            case 'A': return 'rgb(40,160,40)';
            case 'B': return 'rgb(100,160,0)';
            case 'C': return 'rgb(180,140,0)';
            case 'D': return 'rgb(200,80,0)';
            case 'F': return 'rgb(180,0,30)';
            case 'F-': return 'rgb(120,0,20)';
            default: return '#6c757d';
        }
    },

    getCityKey(city, state) {
        return `${(state ?? '').toLowerCase().trim()}|${(city ?? '').toLowerCase().trim()}`;
    },

    getCityOnlyKey(city) {
        return (city ?? '').toLowerCase().trim();
    },

    getCoordsForCity(city) {
        const dbLat = Number(city?.latitude);
        const dbLon = Number(city?.longitude);
        if (Number.isFinite(dbLat) && Number.isFinite(dbLon)) {
            return [dbLat, dbLon];
        }

        const keyWithState = this.getCityKey(city.city, city.state);
        const keyCityOnly = this.getCityOnlyKey(city.city);
        return this.cityCoords[keyWithState] ?? this.cityCoords[keyCityOnly] ?? null;
    },

    loadCachedCoords() {
        try {
            const raw = localStorage.getItem(this.coordsCacheKey);
            if (!raw) return;

            const parsed = JSON.parse(raw);
            if (parsed && typeof parsed === 'object') {
                this.cityCoords = { ...this.cityCoords, ...parsed };
            }
        } catch {
            // Ignore corrupt local cache
        }
    },

    saveCachedCoords() {
        try {
            localStorage.setItem(this.coordsCacheKey, JSON.stringify(this.cityCoords));
        } catch {
            // Ignore storage failures
        }
    },

    async geocodeCity(city, state) {
        // Try Open-Meteo geocoder first (generally CORS-friendly in browser).
        const openMeteoQuery = encodeURIComponent(city ?? '');
        const openMeteoUrl = `https://geocoding-api.open-meteo.com/v1/search?name=${openMeteoQuery}&count=5&language=en&format=json`;

        try {
            const openMeteoResponse = await fetch(openMeteoUrl);
            if (openMeteoResponse.ok) {
                const openMeteoData = await openMeteoResponse.json();
                const results = Array.isArray(openMeteoData?.results) ? openMeteoData.results : [];
                const flMatch = results.find(r => (r?.country_code === 'US') && ((r?.admin1 ?? '').toLowerCase() === (state ?? 'Florida').toLowerCase()));
                const anyUsMatch = results.find(r => r?.country_code === 'US');
                const best = flMatch ?? anyUsMatch ?? results[0];

                if (best && Number.isFinite(Number(best.latitude)) && Number.isFinite(Number(best.longitude))) {
                    return [Number(best.latitude), Number(best.longitude)];
                }
            }
        } catch {
            // Fall back to Nominatim
        }

        const q = encodeURIComponent(`${city}, ${state ?? 'Florida'}, USA`);
        const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&limit=1&q=${q}`);
        if (!response.ok) return null;

        const data = await response.json();
        if (!Array.isArray(data) || data.length === 0) return null;

        const lat = Number(data[0].lat);
        const lon = Number(data[0].lon);
        if (!Number.isFinite(lat) || !Number.isFinite(lon)) return null;

        return [lat, lon];
    },

    async resolveMissingCoords(cities) {
        const missing = [];

        for (const city of cities) {
            const keyWithState = this.getCityKey(city.city, city.state);
            const keyCityOnly = this.getCityOnlyKey(city.city);
            if (!this.cityCoords[keyWithState] && !this.cityCoords[keyCityOnly]) {
                missing.push(city);
            }
        }

        if (missing.length === 0) return;

        let updated = false;
        for (const city of missing.slice(0, this.geocodeBatchSize)) {
            try {
                const coords = await this.geocodeCity(city.city, city.state);
                if (coords) {
                    this.cityCoords[this.getCityKey(city.city, city.state)] = coords;
                    this.cityCoords[this.getCityOnlyKey(city.city)] = coords;
                    updated = true;
                }
            } catch {
                // Continue if one city fails
            }

            await new Promise(r => setTimeout(r, 60));
        }

        if (updated) {
            this.saveCachedCoords();
        }
    },

    buildSafetyTooltip(city) {
        const cityName = city.city ?? 'Unknown City';
        const safetyScore = Number.isFinite(Number(city.safetyPercentile)) ? Number(city.safetyPercentile) : null;
        const grade = this.getSafetyGrade(city.violentRate);
        const gradeColor = this.getSafetyGradeColor(grade);

        return `
            <div style="min-width:280px; padding:10px 12px; font-family:Segoe UI, Arial, sans-serif;">
                <div style="font-size:20px; font-weight:800; color:#1f2937; margin-bottom:6px;">${cityName}</div>
                <div style="font-size:18px; font-weight:900; color:${gradeColor}; margin-bottom:4px;">
                    Safety Grade: ${grade}
                </div>
                <div style="font-size:16px; font-weight:700; color:#111827;">
                    Safety Score: ${safetyScore !== null ? `${safetyScore}/100` : 'N/A'}
                </div>
            </div>
        `;
    },

    buildCityCard(city, selectedMetric, metricValue) {
        const cityName = city.city ?? 'Unknown City';
        const safetyScore = Number.isFinite(Number(city.safetyPercentile)) ? Number(city.safetyPercentile) : null;
        const grade = this.getSafetyGrade(city.violentRate);
        const gradeColor = this.getSafetyGradeColor(grade);

        return `
            <div style="min-width:280px; padding:10px 12px; font-family:Segoe UI, Arial, sans-serif;">
                <div style="font-size:20px; font-weight:800; color:#1f2937; margin-bottom:6px;">${cityName}</div>
                <div style="font-size:16px; color:#111827; margin-bottom:4px;">
                    ${selectedMetric}: <b>${Number.isFinite(metricValue) ? metricValue.toFixed(2) : 'N/A'}</b>
                </div>
                <div style="font-size:18px; font-weight:900; color:${gradeColor}; margin-bottom:4px;">
                    Safety Grade: ${grade}
                </div>
                <div style="font-size:16px; font-weight:700; color:#111827; margin-bottom:4px;">
                    Safety Score: ${safetyScore !== null ? `${safetyScore}/100` : 'N/A'}
                </div>
                <div style="font-size:15px; color:#111827; margin-bottom:3px;">Population: ${city.population?.toLocaleString() ?? 'N/A'}</div>
                <div style="font-size:15px; color:#111827;">Trajectory: ${city.trajectory ?? 'N/A'}</div>
            </div>
        `;
    },

    drawCityLayers(cities, selectedMetric) {
        if (!this.map) return;

        if (this.markersLayer && this.map.hasLayer(this.markersLayer)) {
            this.map.removeLayer(this.markersLayer);
        }
        this.markersLayer = L.layerGroup().addTo(this.map);

        if (this.heatLayer && this.map.hasLayer(this.heatLayer)) {
            this.map.removeLayer(this.heatLayer);
            this.heatLayer = null;
        }

        const points = [];
        const maxVal = Math.max(...cities.map(c => Number(c[selectedMetric]) || 0));

        for (const city of cities) {
            const coords = this.getCoordsForCity(city);
            const metricValue = Number(city[selectedMetric]);

            if (coords && Number.isFinite(metricValue)) {
                const intensity = maxVal > 0 ? metricValue / maxVal : 0;
                points.push([coords[0], coords[1], intensity]);

                L.circleMarker(coords, {
                    radius: 8,
                    color: '#333',
                    fillColor: '#e74c3c',
                    fillOpacity: 0.7
                })
                    .bindPopup(this.buildCityCard(city, selectedMetric, metricValue), {
                        maxWidth: 320,
                        className: 'city-safety-tooltip'
                    })
                    .bindTooltip(this.buildCityCard(city, selectedMetric, metricValue), {
                        direction: 'top',
                        sticky: false,
                        opacity: 1,
                        offset: [0, -18],
                        className: 'city-safety-tooltip'
                    })
                    .on('mouseover', function () {
                        if (!this.isPopupOpen()) {
                            this.openTooltip();
                        }
                    })
                    .on('mouseout', function () { this.closeTooltip(); })
                    .on('popupopen', function () { this.closeTooltip(); })
                    .addTo(this.markersLayer);
            }
        }

        if (typeof L.heatLayer === 'function' && points.length > 0) {
            this.heatLayer = L.heatLayer(points, {
                radius: 40,
                blur: 25,
                maxZoom: 10,
                pane: 'heatPane',
                gradient: { 0.2: 'blue', 0.5: 'yellow', 0.8: 'orange', 1.0: 'red' }
            }).addTo(this.map);
        }
    },

    async init(metric) {
        const selectedMetric = metric ?? 'violentRate';
        this.loadCachedCoords();

        const response = await fetch('/api/florida');
        if (!response.ok) {
            throw new Error(`City API request failed (${response.status})`);
        }

        const cities = await response.json();
        if (!Array.isArray(cities)) {
            throw new Error('City API returned invalid payload');
        }

        if (this.map) {
            this.map.remove();
            this.map = null;
        }

        if (!document.getElementById('map')) {
            throw new Error('Map element not found');
        }

        this.map = L.map('map').setView([27.9944, -81.7603], 7);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        // Draw immediately using any cached coordinates so the map always appears quickly.
        this.drawCityLayers(cities, selectedMetric);

        // Resolve missing coordinates in the background and redraw with more markers.
        this.resolveMissingCoords(cities)
            .then(() => this.drawCityLayers(cities, selectedMetric))
            .catch(() => {
                // Keep current rendered state
            });
    }
};