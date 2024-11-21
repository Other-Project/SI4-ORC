import {SidePanel} from "./components/side-panel.js";
import {LocationInput} from "./components/location-input.js";
import {LeafletMap} from "./components/leaflet-map.js";
import {Instruction} from "./components/instruction.js";
import {Popup} from "./components/popup.js";

customElements.define("side-panel", SidePanel, {extends: "aside"});
customElements.define("location-input", LocationInput);
customElements.define("app-instruction", Instruction);
customElements.define("leaflet-map", LeafletMap, {extends: "div"});
customElements.define("app-popup", Popup);