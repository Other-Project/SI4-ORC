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
    "keep_right"
];

export class SidePanel extends HTMLElement {

    instructions = [];

    constructor() {
        super();
        const shadow = this.attachShadow({mode: "open"});
        fetch("/components/side-panel.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;
                this.sendBtn = shadow.getElementById("sendBtn");
                this.instructionsDiv = shadow.getElementById("instructions");
                this.#setupComponents();
            });

        document.addEventListener("valueChanged", ev => {
            if (!["start", "end"].includes(ev.detail.fieldName)) return;
            this[ev.detail.fieldName] = ev.detail.value;
        });

        document.addEventListener("instructionAdded", ev => {
            this.addInstructions(ev.detail);
        });

        document.addEventListener("instructionsReset", () => {
            this.resetInstructions();
        });
    }

    addInstructions(instruction) {
        this.instructions.push(instruction);
        //console.log("Instructions in side-panel: ", this.instructions);
        let instructionElement = document.createElement("app-instruction");
        instructionElement.setAttribute("active", (instruction === this.instructions[0]).toString());
        instructionElement.setAttribute("label", instruction["InstructionText"]);
        instructionElement.setAttribute("type", ORS_STEP_TYPES[instruction["InstructionType"]]);
        instructionElement.setAttribute("dist", instruction["Distance"]);
        this.instructionsDiv.appendChild(instructionElement);
        document.dispatchEvent(new CustomEvent("addSegment", {
            detail: {
                segment: instruction
            }
        }));
        setInterval(() => this.#nextInstruction(), 3000);
    }

    resetInstructions() {
        this.instructionsDiv.innerHTML = "";
        this.instructions = [];
        document.dispatchEvent(new CustomEvent("resetMap"));
    }

    #setupComponents() {
        if (!this.sendBtn) return;

        const routingService = new RoutingService();
        this.sendBtn.addEventListener("click", async () => {
            if (routingService.isLastRoute(this["start"].coords, this["end"].coords)) return;
            await routingService.getRoute(this["start"].coords, this["end"].coords);
            document.dispatchEvent(new CustomEvent("addMarkers", {
                detail: {
                    start: this["start"],
                    end: this["end"],
                }
            }));
        });
    }

    #updateComponents() {
        console.log("Before instructions : ", this.instructions);
        console.log("Length : ", this.instructions.length);
        this.instructionsDiv.innerHTML = "";
        for (let instruction of this.instructions) {
            /*let instructionElement = document.createElement("app-instruction");
            instructionElement.setAttribute("active", (instruction === this.instructions[0]).toString());
            instructionElement.setAttribute("label", instruction["InstructionText"]);
            instructionElement.setAttribute("type", ORS_STEP_TYPES[instruction["InstructionType"]]);
            instructionElement.setAttribute("dist", instruction["Distance"]);
            this.instructionsDiv.appendChild(instructionElement);*/
        }
        setInterval(() => this.#nextInstruction(), 3000);
    }

    #nextInstruction() {
        if (!this.instructionsDiv) return;
        let active = this.instructionsDiv.querySelector("[active=true]");
        if (!active) return;
        active.setAttribute("active", "false");
        (active.nextElementSibling ?? this.instructionsDiv.firstElementChild)?.setAttribute("active", "true");
    }
}
