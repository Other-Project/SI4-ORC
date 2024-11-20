import "/node_modules/leaflet/dist/leaflet-src.js";
import "/node_modules/leaflet.locatecontrol/dist/L.Control.Locate.min.js";

const ORS_STEP_TYPES = [
    "Left",
    "Right",
    "Sharp left",
    "Sharp right",
    "Slight left",
    "Slight right",
    "Straight",
    "Enter roundabout",
    "Exit roundabout",
    "U-turn",
    "Goal",
    "Depart",
    "Keep left",
    "Keep right"
];

const LeafIcon = L.Icon.extend({
    options: {
        iconSize: [20, 32.8],
        iconAnchor: [10, 32.8],
        popupAnchor: [0, -32.8]
    }
});
const LeafCircle = L.CircleMarker.extend({
    options: {
        radius: 6,
        fillColor: "#3388ff",
        color: "#3388ff",
        weight: 1,
        opacity: 1,
        fillOpacity: 1
    }
});

const greenIcon = new LeafIcon({iconUrl: "/assets/icons/marker-green.png"});
const redIcon = new LeafIcon({iconUrl: "/assets/icons/marker-red.png"});
L.Icon.Default = new LeafIcon({iconUrl: "/assets/icons/marker-blue.png"});

export class LeafletMap extends HTMLDivElement {
    constructor() {
        super();

        const shadow = this.attachShadow({mode: "open"});

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

        document.addEventListener("locationValidated", async ev => {
            this.start = ev.detail.start;
            this.end = ev.detail.end;
            await this.#updateMap();
        });
    }

    async #updateMap() {
        if (!this.start || !this.end) return;

        if (this.markers) for (let marker of this.markers) this.map.removeLayer(marker);

        let segments = await this.#getRoute();
        this.markers = [
            L.marker(this.start.coords.toReversed(), {icon: greenIcon}),
            L.marker(this.end.coords.toReversed(), {icon: redIcon}),
        ];

        let first = true;
        for (let segment of segments) {
            let points = segment["Points"].map(p => [p["Latitude"], p["Longitude"]]);
            let color = segment["Vehicle"] === 6 ? "#5c34d5" : "#3388ff";
            this.markers.push(L.polyline(points, {color: color}).bindPopup(segment["Distance"] + "m"));
            if (!first)
                this.markers.push(new LeafCircle(points[0], {
                    color: color,
                    fillColor: color
                }).bindPopup(segment["InstructionText"]));
            first = false;
        }

        for (let marker of this.markers) marker.addTo(this.map);
    }

    async #getRoute() {
        if (!this.start || !this.end) return null;
        let url = `http://localhost:5001/web/route?startLon=${this.start.coords[0]}&startLat=${this.start.coords[1]}&endLon=${this.end.coords[0]}&endLat=${this.end.coords[1]}`;
        return (await (await fetch(url)).json())["CalculateRouteResult"];
    }
}
