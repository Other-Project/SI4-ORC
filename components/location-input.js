const ADDRESS_API = "https://api-adresse.data.gouv.fr/search/";

export class LocationInput extends HTMLElement {
    // noinspection JSUnusedGlobalSymbols
    static get observedAttributes() {
        return ["name", "display_name"];
    }

    // noinspection JSUnusedGlobalSymbols
    attributeChangedCallback(name, oldValue, newValue) {
        this[name] = newValue;
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
        let context = this;
        this.callTimeout = 0;

        this.input.addEventListener("keydown", async () => {
            if (this.callTimeout) clearTimeout(this.callTimeout);
            this.callTimeout = setTimeout(() => context.#updateSuggestions(), 1000);
        });
    }

    async #getCurrentPosition() {
        const pos = await new Promise((resolve, reject) => {
            navigator.geolocation.getCurrentPosition(resolve, reject);
        });

        return {
            long: pos.coords.longitude,
            lat: pos.coords.latitude,
        };
    };

    async #addressCompletionApiUrl(query) {
        let requestUrl = ADDRESS_API + "?q=" + query;
        await this.#getCurrentPosition().then(coords => requestUrl += "&lat=" + coords.lat + "&lon=" + coords.long).catch(() => null);
        return requestUrl;
    }

    async #updateSuggestions() {
        while (this.datalist.firstChild) this.datalist.removeChild(this.datalist.firstChild);
        const value = this.input.value;
        if (value.length < 3) return; // The api won't respond

        let url = await this.#addressCompletionApiUrl(value);
        let data = await (await fetch(url)).json();
        for (let suggest of data.features) {
            let option = document.createElement('option');
            option.value = suggest.properties.label;
            this.datalist.appendChild(option);
        }
    }
}