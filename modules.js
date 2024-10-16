import { SidePanel } from "./components/side-panel.js";
import { LeafletMap } from "./components/leaflet-map.js";

customElements.define("side-panel", SidePanel, { extends: "aside" });
customElements.define("leaflet-map", LeafletMap, { extends: "div" });
