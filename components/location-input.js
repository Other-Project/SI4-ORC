export class LocationInput extends HTMLElement {
    // noinspection JSUnusedGlobalSymbols
    static get observedAttributes() {
        return ["name", "display_name"];
    }

    // noinspection JSUnusedGlobalSymbols
    attributeChangedCallback(name, oldValue, newValue) {
        this[name] = newValue;
        console.log(name + ": " + newValue)
        this.#updateComponents();
    }

    constructor() {
        super();

        let shadowRoot = this.attachShadow({mode: "open"});

        let context = this;
        fetch("/components/location-input.html").then(async function (response) {
            let templateContent = new DOMParser()
                .parseFromString(await response.text(), "text/html")
                .getElementsByTagName("template")[0]
                .content;

            context.root = templateContent.cloneNode(true);
            context.label = context.root.querySelector("label");
            context.input = context.root.querySelector("input");
            context.datalist = context.root.querySelector("datalist");

            context.#updateComponents();
            context.#setupComponents();
            shadowRoot.appendChild(context.root);
        });
    }

    #updateComponents() {
        if (!this.root) return;

        this.label.setAttribute("for", this["name"]);
        this.label.innerText = this["display_name"];
        this.input.setAttribute("id", this["name"]);
        this.input.setAttribute("name", this["name"]);
        this.input.setAttribute("list", this.datalist.id = this["name"] + "Suggestions");
    }

    #setupComponents() {
        this.input.addEventListener("input", async () => {
            await this.#updateSuggestions();
        });
    }

    async #updateSuggestions() {
        while (this.datalist.firstChild) this.datalist.removeChild(this.datalist.firstChild);
        const value = this.input.value;
        if (value.length < 3) return; // The api won't respond

        let url = "https://api-adresse.data.gouv.fr/search/?q=" + value;
        let data = await (await fetch(url)).json();
        for (let suggest of data.features) {
            let option = document.createElement('option');
            option.value = suggest.properties.label;
            this.datalist.appendChild(option);
        }
    }
}