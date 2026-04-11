class LeafletMapService {
    constructor(mapElementId, options = {}) {
        console.log("FIXITNEPAL: Initializing LeafletMapService for:", mapElementId);
        this.mapElement = document.getElementById(mapElementId);
        this.options = {
            zoom: 13,
            center: [27.7172, 85.3240], // Kathmandu Default [lat, lng]
            ...options
        };
        this.map = null;
        this.markers = null;
        this.userMarker = null;

        // Configure Default Icons from CDN to ensure they load across all environments
        // This prevents blank maps caused by missing marker image errors
        if (typeof L !== 'undefined') {
            delete L.Icon.Default.prototype._getIconUrl;
            L.Icon.Default.mergeOptions({
                iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
                iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
                shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
            });
        }
    }

    init() {
        if (!this.mapElement) {
            console.error("FIXITNEPAL: Map element not found!");
            return;
        }

        // Add a visible indicator to the map element while loading
        this.mapElement.innerHTML = `
            <div id="${this.mapElement.id}_loader" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; display: flex; flex-direction: column; align-items: center; justify-content: center; background: #f8fafc; z-index: 10000; border: 1px solid #e2e8f0;">
                <div class="spinner-border text-primary mb-3" role="status"></div>
                <div class="fw-bold text-secondary">Loading Map Assets...</div>
                <div id="${this.mapElement.id}_status" class="small text-muted mt-2">Checking Leaflet library...</div>
            </div>
        `;

        const statusEl = document.getElementById(`${this.mapElement.id}_status`);

        try {
            if (typeof L === 'undefined') {
                statusEl.innerText = "Error: Leaflet library (L) not loaded. Check connection.";
                statusEl.classList.add("text-danger");
                return;
            }

            console.log("FIXITNEPAL: Creating L.map instance...");
            statusEl.innerText = "Initializing map container...";
            
            // Fix: Ensure the container has relative positioning
            this.mapElement.style.position = 'relative';

            this.map = L.map(this.mapElement, {
                tap: true, // Force tap support for mobile
                ...this.options
            }).setView(this.options.center, this.options.zoom);

            console.log("FIXITNEPAL: Adding Tile Layer...");
            statusEl.innerText = "Loading map tiles...";

            // Primary Tile Layer: OpenStreetMap
            const osm = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                crossOrigin: true
            });

            // Fallback Tile Layer: CartoDB Positron (often more reliable in strict environments)
            const carto = L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
                subdomains: 'abcd',
                maxZoom: 20,
                crossOrigin: true
            });

            osm.addTo(this.map);

            // If OSM fails to load tiles within 3 seconds, try switching to Carto
            let tilesLoaded = false;
            osm.on('tileload', () => { tilesLoaded = true; });
            
            setTimeout(() => {
                if (!tilesLoaded && this.map) {
                    console.warn("FIXITNEPAL: OSM tiles slow or blocked, trying CartoDB fallback...");
                    this.map.removeLayer(osm);
                    carto.addTo(this.map);
                }
            }, 3000);

            this.markers = L.layerGroup().addTo(this.map);

            // Handle window resize automatically
            window.addEventListener('resize', () => {
                if (this.map) {
                    this.map.invalidateSize();
                }
            });

            console.log("FIXITNEPAL: Map initialized successfully.");
            
            // Remove loader after successful init
            const loader = document.getElementById(`${this.mapElement.id}_loader`);
            if (loader) loader.style.display = 'none';

            // Initial size refresh
            setTimeout(() => this.refresh(), 100);
            setTimeout(() => this.refresh(), 500);
            setTimeout(() => this.refresh(), 2000); // Robustness for mobile
        } catch (e) {
            console.error("FIXITNEPAL: Leaflet initialization failed:", e);
            if (statusEl) {
                statusEl.innerText = `Initialization Error: ${e.message}`;
                statusEl.classList.add("text-danger");
            }
        }
    }

    refresh() {
        if (this.map) {
            this.map.invalidateSize();
            console.log("FIXITNEPAL: Map size invalidated.");
        }
    }

    addMarker(lat, lng, title, infoContent = null, iconUrl = null) {
        let markerOptions = { title: title };
        
        if (iconUrl) {
            const customIcon = L.icon({
                iconUrl: iconUrl,
                iconSize: [25, 41],
                iconAnchor: [12, 41],
                popupAnchor: [1, -34],
                shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
                shadowSize: [41, 41]
            });
            markerOptions.icon = customIcon;
        }

        const marker = L.marker([lat, lng], markerOptions);

        if (infoContent) {
            marker.bindPopup(infoContent);
        }

        marker.addTo(this.markers);
        return marker;
    }

    clearMarkers() {
        if (this.markers) {
            this.markers.clearLayers();
        }
    }

    getCurrentLocation(callback) {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const lat = position.coords.latitude;
                    const lng = position.coords.longitude;

                    if (this.userMarker) {
                        this.map.removeLayer(this.userMarker);
                    }

                    this.userMarker = L.circleMarker([lat, lng], {
                        radius: 8,
                        fillColor: "#3388ff",
                        color: "#fff",
                        weight: 2,
                        opacity: 1,
                        fillOpacity: 0.8
                    }).addTo(this.map).bindPopup("Your Location");

                    this.map.setView([lat, lng], 14);
                    if (callback) callback({ lat, lng });
                },
                (err) => {
                    console.warn("Geolocation failed:", err.message);
                }
            );
        } else {
            console.warn("Browser doesn't support Geolocation");
        }
    }

    loadProviders(apiUrl, params = {}) {
        const url = new URL(apiUrl, window.location.origin);

        Object.keys(params).forEach(key => {
            if (params[key] !== null && params[key] !== undefined && params[key] !== '')
                url.searchParams.append(key, params[key]);
        });

        fetch(url)
            .then(response => response.json())
            .then(result => {
                // Support both direct arrays and standard ApiResponse payloads
                const providers = result.data ? result.data : (Array.isArray(result) ? result : []);
                console.log(`FIXITNEPAL: Received ${providers.length} providers from API`);
                this.clearMarkers();
                providers.forEach(p => {
                    // Handle both camelCase and PascalCase
                    const id = p.id || p.Id;
                    const name = p.name || p.Name;
                    const category = p.category || p.Category;
                    const lat = p.lat || p.Lat;
                    const lng = p.lng || p.Lng;
                    const rating = p.rating || p.Rating;

                    const content = `
                        <div style="min-width: 180px;">
                            <h6 class="fw-bold mb-1">${name}</h6>
                            <span class="badge bg-primary mb-2">${category}</span>
                            <div class="d-flex align-items-center mb-2">
                                <i class="bi bi-star-fill text-warning me-1"></i>
                                <span>${rating > 0 ? Number(rating).toFixed(1) : 'New'}</span>
                            </div>
                            <div class="d-flex gap-2">
                                <a href="/Home/ProviderDetails/${id}" class="btn btn-sm btn-primary w-50">View Profile</a>
                                <a href="/Home/ProviderDetails/${id}?action=book" class="btn btn-sm btn-primary w-50">Book Now</a>
                            </div>
                        </div>
                    `;
                    this.addMarker(lat, lng, name, content);
                });
            })
            .catch(err => console.error("FIXITNEPAL: Error loading providers:", err));
    }

    // Geocoding using OpenStreetMap Nominatim
    searchLocation(query, callback) {
        if (!query || query.length < 3) return;
        
        console.log("FIXITNEPAL: Searching for location:", query);
        const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`;
        
        fetch(url)
            .then(response => response.json())
            .then(data => {
                if (data && data.length > 0) {
                    const result = {
                        lat: parseFloat(data[0].lat),
                        lng: parseFloat(data[0].lon),
                        displayName: data[0].display_name
                    };
                    console.log("FIXITNEPAL: Location found:", result);
                    if (this.map) {
                        this.map.setView([result.lat, result.lng], 13);
                    }
                    if (callback) callback(result);
                } else {
                    console.warn("FIXITNEPAL: No results for location search");
                }
            })
            .catch(err => console.error("FIXITNEPAL: Geocoding error:", err));
    }

    enableClickToPick(callback) {
        if (!this.map) {
            console.warn("Map not initialized, cannot enable click-to-pick");
            return;
        }
        this.map.on('click', (e) => {
            const { lat, lng } = e.latlng;
            
            this.clearMarkers();
            this.addMarker(lat, lng, "Selected Location");

            if (callback) callback(lat, lng);
        });
    }
}
