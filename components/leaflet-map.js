import * as leaflet from "/node_modules/leaflet/dist/leaflet-src.esm.js";

export class LeafletMap extends HTMLDivElement {
    constructor() {
        super();

        const shadow = this.attachShadow({ mode: "open" });

        let style = document.createElement("link");
        style.href = "/node_modules/leaflet/dist/leaflet.css";
        style.rel = "stylesheet";
        shadow.appendChild(style);

        let mapDiv = document.createElement("div");
        mapDiv.style.height = "100%";
        let map = leaflet.map(mapDiv, {
            zoomControl: false
        }).setView([43.6155, 7.0719], 16);
        setTimeout(() => map.invalidateSize(), 500);
        leaflet.control.zoom({
            position: "bottomright"
        }).addTo(map);
        leaflet.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            minZoom: 6,
            attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors"
        }).addTo(map);

        shadow.appendChild(mapDiv);
    }
}