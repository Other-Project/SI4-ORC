import "/node_modules/leaflet/dist/leaflet-src.js";
import "/node_modules/leaflet.locatecontrol/dist/L.Control.Locate.min.js";

export class LeafletMap extends HTMLDivElement {
    constructor() {
        super();

        const shadow = this.attachShadow({ mode: "open" });
        const LeafIcon = L.Icon.extend({
            options: {
                iconSize: [20, 32.8],
                iconAnchor: [10, 32.8],
                popupAnchor: [0, -32.8]
            }
        });
        this.greenIcon = new LeafIcon({ iconUrl: "/assets/icons/marker-green.png" });
        this.redIcon = new LeafIcon({ iconUrl: "/assets/icons/marker-red.png" });
        this.blueIcon = new LeafIcon({ iconUrl: "/assets/icons/marker-blue.png" });


        let style = document.createElement("link");
        style.href = "/node_modules/leaflet/dist/leaflet.css";
        style.rel = "stylesheet";
        shadow.appendChild(style);
        style = document.createElement("link");
        style.href = "/node_modules/leaflet.locatecontrol/dist/L.Control.Locate.css";
        style.rel = "stylesheet";
        shadow.appendChild(style);

        let mapDiv = document.createElement("div");
        mapDiv.style.height = "100%";
        this.map = L.map(mapDiv, {
            zoomControl: false
        }).setView([43.6155, 7.0719], 16);
        setTimeout(() => this.map.invalidateSize(), 500);
        L.control.zoom({
            position: "bottomright"
        }).addTo(this.map);
        L.control.locate({
            position: "bottomright"
        }).addTo(this.map);

        L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            minZoom: 6,
            attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors"
        }).addTo(this.map);

        shadow.appendChild(mapDiv);

        document.addEventListener("locationValidated", ev => {
            this.start = ev.detail.start;
            this.end = ev.detail.end;
            this.#updateMap();
        });
    }

    #updateMap() {
        if (!this.start || !this.end) return;

        if (this.markers) for (let marker of this.markers) this.map.removeLayer(marker);
        this.markers = [
            L.marker(this.start.coords, { icon: this.greenIcon }),
            L.marker(this.end.coords, { icon: this.redIcon })
        ];
        for (let marker of this.markers) marker.addTo(this.map);
    }
}