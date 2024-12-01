import {RoutingService} from "/routingService.js";

const ORS_STEP_TYPES = [
    "left",
    "right",
    "sharp_left",
    "sharp_right",
    "slight_left",
    "slight_right",
    "straight",
    "enter_roundabout",
    "exit_roundabout",
    "u_turn",
    "goal",
    "depart",
    "keep_left",
    "keep_right",
    "change_vehicle"
];
const ORS_VEHICLE_TYPES = [
    "car",
    "car",
    "bike",
    "bike",
    "bike",
    "bike",
    "foot",
    "foot",
    "foot"
];

export class SidePanel extends HTMLElement {

    instructions = [];
    compteur = 0;

    constructor() {
        super();
        const shadow = this.attachShadow({mode: "open"});
        fetch("/components/side-panel.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;
                this.startInput = shadow.getElementById("startInput");
                this.endInput = shadow.getElementById("endInput");
                this.sendBtn = shadow.getElementById("sendBtn");
                this.invertBtn = shadow.getElementById("invertBtn");
                this.instructionsDiv = shadow.getElementById("instructions");
                this.#setupComponents();
            });

        document.addEventListener("valueChanged", ev => {
            if (!["start", "end"].includes(ev.detail.fieldName)) return;
            this[ev.detail.fieldName] = ev.detail.value;
        });

        document.addEventListener("instructionAdded", ev => this.addInstructions(ev.detail));
        document.addEventListener("instructionsReset", () => this.resetInstructions());
        setInterval(() => this.#nextInstruction(), 300);
    }

    addInstructions(instruction) {
        this.instructions.push(instruction);
        let instructionElement = document.createElement("app-instruction");
        instructionElement.setAttribute("active", (instruction === this.instructions[0]).toString());
        instructionElement.setAttribute("label", instruction["InstructionText"]);
        instructionElement.setAttribute("type", ORS_STEP_TYPES[instruction["InstructionType"]]);
        instructionElement.setAttribute("dist", instruction["Distance"]);
        instructionElement.setAttribute("vehicle", ORS_VEHICLE_TYPES[instruction["Vehicle"]]);
        this.instructionsDiv.appendChild(instructionElement);
        document.dispatchEvent(new CustomEvent("addSegment", {
            detail: {
                segment: instruction
            }
        }));
    }

    resetInstructions() {
        this.instructionsDiv.innerHTML = "";
        this.instructions = [];
        this.compteur = 0;
        document.dispatchEvent(new CustomEvent("resetMap"));
        document.dispatchEvent(new CustomEvent("hidePopup"));
    }

    #setupComponents() {
        if (!this.sendBtn) return;

        this.routingService = new RoutingService();
        this.sendBtn.addEventListener("click", async () => {
            if (this.routingService.isLastRoute(this["start"].coords, this["end"].coords)) return;
            await this.routingService.getRoute(this["start"].coords, this["end"].coords);
            document.dispatchEvent(new CustomEvent("addMarkers", {
                detail: {
                    start: this["start"],
                    end: this["end"],
                }
            }));
        });

        this.invertBtn.addEventListener("click", () => {
            if (!this["start"] || !this["end"]) return;
            let tmp = this["start"];
            this["start"] = this["end"];
            this["end"] = tmp;
            this.startInput.setLocation(this["start"].label);
            this.endInput.setLocation(this["end"].label);
        });
    }

    async #nextInstruction() {
        if (!this.instructionsDiv || this.instructions.length === 0) return;
        if (this.instructions.length === this.compteur) {
            console.log("Requesting more instructions");
            await this.routingService.sendMessage("Send me more message");
            this.compteur++;
        }
        if (this.compteur > this.instructions.length) return;
        this.compteur++;
        let active = this.instructionsDiv.querySelector("[active=true]");
        if (!active) return;
        active.setAttribute("active", "false");
        let newActive = active.nextElementSibling ?? this.instructionsDiv.firstElementChild;
        newActive.setAttribute("active", "true");
        this.instructionsDiv.scroll({
            top: newActive.offsetTop - active.getBoundingClientRect().height, // so that the precedent instruction stays visible
            behavior: "smooth"
        });
    }
}
