export class SidePanel extends HTMLElement {


    constructor() {
        super();

        const shadow = this.attachShadow({ mode: "open" });
        fetch("/components/side-panel.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;
                this.sendBtn = shadow.getElementById("sendBtn");
                this.#setupComponents();
            });

        document.addEventListener("valueChanged", ev => {
            if (!["start", "end"].includes(ev.detail.fieldName)) return;
            this[ev.detail.fieldName] = ev.detail.value;
        });
    }

    #setupComponents() {
        if (!this.sendBtn) return;

        this.sendBtn.addEventListener("click", () => {
            document.dispatchEvent(new CustomEvent("locationValidated", {
                detail: {
                    start: this["start"],
                    end: this["end"]
                }
            }));
        });
    }
}

