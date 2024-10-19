import {SidePanel} from "./components/side-panel.js";
import {LocationInput} from "./components/location-input.js";
import {LeafletMap} from "./components/leaflet-map.js";

customElements.define("side-panel", SidePanel, { extends: "aside" });
customElements.define("location-input", LocationInput);
customElements.define("leaflet-map", LeafletMap, { extends: "div" });
