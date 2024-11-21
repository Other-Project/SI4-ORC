export class Instruction extends HTMLElement {
    // noinspection JSUnusedGlobalSymbols
    static get observedAttributes() {
        return ["label", "type", "dist", "active"];
    }

    // noinspection JSUnusedGlobalSymbols
    attributeChangedCallback(name, oldValue, newValue) {
        this[name] = newValue;
        this.#updateComponents();
    }

    constructor() {
        super();

        const shadow = this.attachShadow({mode: "open"});
        fetch("/components/instruction.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;
                this.instruction = shadow.querySelector(".instruction");
                this.icon = shadow.querySelector(".instruction-icon");
                this.displayText = shadow.querySelector(".instruction-text");
                this.distance = shadow.querySelector(".instruction-distance");
                this.#updateComponents();
            });
    }

    #updateComponents() {
        if (!this.instruction) return;
        this.instruction.classList.toggle("active", this["active"] === "true");
        this.icon.src = this.type ? "/assets/icons/steps/" + this["type"].toLowerCase() + ".png" : "";
        this.displayText.innerText = this["label"] ?? "";
        this.distance.innerText = this.#distanceToString(this["dist"]) ?? "";
    }

    #distanceToString(dist) {
        if (!dist) return null;
        if(dist < 10) return dist + " m";
        if (dist < 1000) return (dist / 10).toFixed(0) + "0 m";
        dist /= 1000;
        if (dist < 10) return dist.toFixed(2) + " km";
        if (dist < 100) return dist.toFixed(1) + " km";
        return dist.toFixed(0) + " km";
    }
}

