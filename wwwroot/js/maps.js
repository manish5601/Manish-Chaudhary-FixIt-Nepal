class LeafletMapService {
    constructor(mapElementId, options = {}) {
        console.log("Initializing LeafletMapService for:", mapElementId);
        this.mapElement = document.getElementById(mapElementId);
        this.options = {
            zoom: 13,
            center: [27.7172, 85.3240], // Kathmandu Default [lat, lng]
            ...options
        };
        this.map = null;
        this.markers = null;
        this.userMarker = null;
    }

    init() {
        if (!this.mapElement) {
            console.error("Map element not found!");
            return;
        }

        try {
            console.log("Creating L.map instance...");
            this.map = L.map(this.mapElement).setView(this.options.center, this.options.zoom);

            console.log("Adding Tile Layer...");
            L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            }).addTo(this.map);

            this.markers = L.layerGroup().addTo(this.map);
            console.log("Map initialized successfully.");
        } catch (e) {
            console.error("Leaflet initialization failed:", e);
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
            .then(providers => {
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
                            <a href="/Home/ProviderDetails/${id}" class="btn btn-sm btn-primary w-100">Book Now</a>
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
