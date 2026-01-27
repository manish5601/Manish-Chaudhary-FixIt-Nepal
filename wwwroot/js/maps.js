class GoogleMapService {
    constructor(mapElementId, options = {}) {
        this.mapElement = document.getElementById(mapElementId);
        this.options = {
            zoom: 13,
            center: { lat: 27.7172, lng: 85.3240 }, // Kathmandu Default
            ...options
        };
        this.map = null;
        this.markers = [];
        this.userMarker = null;
    }

    init() {
        if (!this.mapElement) return;

        this.map = new google.maps.Map(this.mapElement, {
            zoom: this.options.zoom,
            center: this.options.center,
            mapTypeId: google.maps.MapTypeId.ROADMAP,
            styles: [
                {
                    "featureType": "poi",
                    "elementType": "labels",
                    "stylers": [{ "visibility": "off" }]
                }
            ]
        });
    }

    addMarker(lat, lng, title, infoContent = null, icon = null) {
        const marker = new google.maps.Marker({
            position: { lat, lng },
            map: this.map,
            title: title,
            icon: icon
            // animation: google.maps.Animation.DROP
        });

        if (infoContent) {
            const infoWindow = new google.maps.InfoWindow({
                content: infoContent
            });

            marker.addListener("click", () => {
                infoWindow.open(this.map, marker);
            });
        }

        this.markers.push(marker);
        return marker;
    }

    clearMarkers() {
        this.markers.forEach(m => m.setMap(null));
        this.markers = [];
    }

    getCurrentLocation(callback) {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const pos = {
                        lat: position.coords.latitude,
                        lng: position.coords.longitude
                    };

                    if (this.userMarker) this.userMarker.setMap(null);

                    this.userMarker = new google.maps.Marker({
                        position: pos,
                        map: this.map,
                        title: "Your Location",
                        icon: 'http://maps.google.com/mapfiles/ms/icons/blue-dot.png'
                    });

                    this.map.setCenter(pos);
                    if (callback) callback(pos);
                },
                () => {
                    console.warn("Geolocation failed or denied.");
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
                this.clearMarkers();
                if (providers.length === 0) {
                    // Maybe handle empty state visually?
                    // For now just clearing is fine.
                }
                providers.forEach(p => {
                    const content = `
                        <div style="min-width: 200px;">
                            <h6 class="fw-bold mb-1">${p.name}</h6>
                            <span class="badge bg-primary mb-2">${p.category}</span>
                            <div class="d-flex align-items-center mb-2">
                                <i class="bi bi-star-fill text-warning me-1"></i>
                                <span>${p.rating > 0 ? p.rating.toFixed(1) : 'New'}</span>
                            </div>
                            <!-- <a href="/Customer/Book/${p.id}" class="btn btn-sm btn-outline-primary w-100">Book Now</a> -->
                            <button class="btn btn-sm btn-secondary w-100" disabled>Booking Coming Soon</button>
                        </div>
                    `;
                    this.addMarker(p.lat, p.lng, p.name, content);
                });
            })
            .catch(err => console.error("Error loading providers:", err));
    }

    enableClickToPick(callback) {
        this.map.addListener("click", (mapsMouseEvent) => {
            const lat = mapsMouseEvent.latLng.lat();
            const lng = mapsMouseEvent.latLng.lng();

            this.clearMarkers();
            this.addMarker(lat, lng, "Selected Location");

            if (callback) callback(lat, lng);
        });
    }
}
