import {RoutingService} from "/routingService.js";

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

import {ActiveMQ} from "/components/activemq.js";

export class SidePanel extends HTMLElement {
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
    }

    #setupComponents() {
        if (!this.sendBtn) return;

        this.sendBtn.addEventListener("click", async () => {
            let instructions = await new RoutingService().getRoute(this["start"].coords, this["end"].coords);
            let activeMQ = new ActiveMQ("ws://localhost:61614/admin", "admin", "admin", "/topic/chat.general");
            document.dispatchEvent(new CustomEvent("locationValidated", {
                detail: {
                    start: this["start"],
                    end: this["end"],
                    instructions: instructions
                }
            }));

            this.instructionsDiv.innerHTML = "";
            for (let instruction of instructions) {
                let instructionElement = document.createElement("app-instruction");
                instructionElement.setAttribute("active", (instruction === instructions[0]).toString());
                instructionElement.setAttribute("label", instruction["InstructionText"]);
                instructionElement.setAttribute("type", ORS_STEP_TYPES[instruction["InstructionType"]]);
                instructionElement.setAttribute("dist", instruction["Distance"]);
                this.instructionsDiv.appendChild(instructionElement);
                activeMQ.send(instruction.label);
            }
            setInterval(() => this.#nextInstruction(), 3000);
        });
    }

    #nextInstruction() {
        if (!this.instructionsDiv) return;
        let active = this.instructionsDiv.querySelector("[active=true]");
        if (!active) return;
        active.setAttribute("active", "false");
        (active.nextElementSibling ?? this.instructionsDiv.firstElementChild)?.setAttribute("active", "true");
    }
}

