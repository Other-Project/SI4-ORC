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
            document.dispatchEvent(new CustomEvent("locationValidated", {
                detail: {
                    start: this["start"],
                    end: this["end"]
                }
            }));

            let instructions = await this.#getInstructions();
            this.instructionsDiv.innerHTML = "";
            for (let instruction of instructions) {
                let instructionElement = document.createElement("app-instruction");
                instructionElement.setAttribute("active", (instruction === instructions[0]).toString());
                instructionElement.setAttribute("label", instruction.label);
                instructionElement.setAttribute("type", instruction.type);
                instructionElement.setAttribute("dist", instruction.distance);
                this.instructionsDiv.appendChild(instructionElement);
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

    async #getInstructions() {
        // TODO : Mocked, needs to be really be connected
        return [
            {
                label: "Continuez",
                type: "STRAIGHT",
                position: [43.613877, 7.073036],
                distance: 23
            },
            {
                label: "Tournez à gauche",
                type: "LEFT",
                position: [43.613877, 7.073036],
                distance: 96
            },
            {
                label: "Prenez la 2e sortie",
                type: "ROUNDABOUT_LEFT",
                position: [43.612052, 7.078460],
                distance: 932
            },
            {
                label: "Prenez la 1ere sortie",
                type: "ROUNDABOUT_RIGHT",
                position: [43.615223, 7.080608],
                distance: 3051
            },
            {
                label: "Prenez la 3e sortie",
                type: "ROUNDABOUT_STRAIGHT",
                position: [43.614015, 7.088676],
                distance: 20103
            },
            {
                label: "Tournez à droite",
                type: "RIGHT",
                position: [43.614015, 7.088676],
                distance: 200105
            }
        ]
    }
}

