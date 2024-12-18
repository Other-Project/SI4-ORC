import "/node_modules/leaflet/dist/leaflet-src.js";
import "/node_modules/leaflet.locatecontrol/dist/L.Control.Locate.min.js";

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

const typeColor = [
    "#00a491", // driving-car
    "#1dba8b", // driving-hgv
    "#3388ff", // cycling-regular
    "#00b1ff", // cycling-road
    "#00cff2", // cycling-mountain
    "#0088b0", // cycling-electric
    "#5c34d5", // foot-walking
    "#9480b3", // foot-hiking
    "#df9a1c", // wheelchair
];

export class LeafletMap extends HTMLDivElement {
    constructor() {
        super();

        this.lineList = [];
        this.leafCirleList = [];
        this.segmentList = [];
        this.segmentWithNoPoints = 0;

        const shadow = this.attachShadow({mode: "open"});

        let style = document.createElement("link");
        style.href = "/node_modules/leaflet/dist/leaflet.css";
        style.rel = "stylesheet";
        shadow.appendChild(style);
        style = document.createElement("link");
        style.href = "/node_modules/leaflet.locatecontrol/dist/L.Control.Locate.css";
        style.rel = "stylesheet";
        shadow.appendChild(style);
        style = document.createElement("link");
        style.href = "/components/leaflet-map.css";
        style.rel = "stylesheet";
        shadow.appendChild(style);

        let mapDiv = document.createElement("div");
        mapDiv.style.height = "100%";
        this.map = L.map(mapDiv, {
            zoomControl: false
        }).setView([43.6155, 7.0719], 16);
        setTimeout(() => this.map.invalidateSize(), 500);
        L.control.zoom({position: "bottomright"}).addTo(this.map);
        L.control.locate({position: "bottomright"}).addTo(this.map);

        L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            minZoom: 6,
            attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors"
        }).addTo(this.map);

        shadow.appendChild(mapDiv);

        document.addEventListener("addSegment", async ev => await this.#addSegment(ev.detail.segment));

        document.addEventListener("resetMap", async () => await this.#resetMap());

        document.addEventListener("addMarkers", async ev => {
            this.start = ev.detail.start;
            this.end = ev.detail.end;
            await this.#addMarkers();
        });

        document.addEventListener("highlightSegment", async ev => {
            await this.highlightSegment(ev.detail.index);
        });
    }

    async #addMarkers() {
        if (!this.start || !this.end) return;
        L.marker(this.start.coords.toReversed(), {icon: greenIcon}).addTo(this.layer);
        L.marker(this.end.coords.toReversed(), {icon: redIcon}).addTo(this.layer);
        let padding = window.innerWidth > 800 ? {paddingTopLeft: [425, 25], paddingBottomRight: [25, 25]} : undefined;
        this.map.fitBounds(this.layer.getBounds(), padding);
    }

    async #resetMap() {
        if (this.layer) this.map.removeLayer(this.layer);
        this.layer = new L.FeatureGroup();
        this.layer.addTo(this.map);
        this.lineList = [];
        this.leafCirleList = [];
        this.segmentList = [];
        this.segmentWithNoPoints = 0;
        delete this.colorLine;
        delete this.colorCircle;
    }

    async #addSegment(segment) {
        let points = segment["Points"].map(p => [p["Latitude"], p["Longitude"]]);
        this.segmentList.push(segment);
        if (!points || points.length === 0){
            return;
        }
        let color = typeColor[segment["Vehicle"]];
        this.lineList.push(L.polyline(points, {color: color}).bindPopup(segment["Distance"] + "m").addTo(this.layer));
        this.leafCirleList.push(new LeafCircle(points[0], {
            color: color,
            fillColor: color,
        }).bindPopup(segment["InstructionText"]).addTo(this.layer));
    }

    async highlightSegment(index) {
        if (index < 0 || index >= this.segmentList.length) return;
        let segment = this.segmentList[index];
        let points = segment["Points"].map(p => [p["Latitude"], p["Longitude"]]);
        if (!points || points.length === 0) {
            this.segmentWithNoPoints++;
            return;
        }
        index = index-this.segmentWithNoPoints;
        if (this.colorLine) this.lineList[index - 1].setStyle({color: this.colorLine});
        let currentLine = this.lineList[index];
        this.colorLine = currentLine.options.color;
        currentLine.setStyle({
            color: "#ffff00",
        })

        if (this.colorCircle) this.leafCirleList[index - 1].setStyle({
            color: this.colorCircle,
            fillColor: this.colorCircle
        });
        let currentLeafCircle = this.leafCirleList[index];
        this.colorCircle = currentLeafCircle.options.color;
        currentLeafCircle.setStyle({
            color: "#ffff00",
            fillColor: "#ffff00"
        })
    }
}
