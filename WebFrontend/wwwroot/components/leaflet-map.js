import "/node_modules/leaflet/dist/leaflet-src.js";
import "/node_modules/leaflet.locatecontrol/dist/L.Control.Locate.min.js";

const ORS_TOKEN = "5b3ce3597851110001cf624846c93be49c1f44f0949187d18b1d653c";
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

        let {steps, waypoints} = await this.#getRoute();
        this.markers = [
            L.marker(this.start.coords.toReversed(), {icon: greenIcon}),
            L.marker(this.end.coords.toReversed(), {icon: redIcon}),
        ];

        for (let step of steps) {
            let start = step["way_points"][0];
            let end = step["way_points"][1];
            this.markers.push(L.polyline(waypoints.slice(start, end + 1), {color: "#3388ff"}).bindPopup(step["distance"] + "m"));
            if (start > 0)
                this.markers.push(new LeafCircle(waypoints[start]).bindPopup(step["instruction"]));
        }

        for (let marker of this.markers) marker.addTo(this.map);
    }

    #getRouteUrl(start, end, vehicle = "cycling-regular") {
        return `https://api.openrouteservice.org/v2/directions/${vehicle}?api_key=${ORS_TOKEN}&start=${start[0]},${start[1]}&end=${end[0]},${end[1]}`
    }

    async #getRoute() {
        if (!this.start || !this.end) return null;
        console.log(this.start.coords, this.end.coords);
        let response = await (await fetch(this.#getRouteUrl(this.start.coords, this.end.coords))).json();
        let steps = response["features"][0]["properties"]["segments"][0]["steps"];
        let waypoints = response["features"][0]["geometry"]["coordinates"].map(w => w.toReversed());
        return {steps, waypoints};
    }
}
