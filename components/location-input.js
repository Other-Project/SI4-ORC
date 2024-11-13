const FRENCH_ADDRESS_API = "https://api-adresse.data.gouv.fr/search/";
const ORS_ADDRESS_API = "https://api.openrouteservice.org/geocode/autocomplete?api_key=5b3ce3597851110001cf624846c93be49c1f44f0949187d18b1d653c";

let cachedGeoPos;
let cachedGeoPosTime;

await getCurrentPosition();

async function getCurrentPosition() {
    if (!("geolocation" in navigator)) {
        await Promise.reject("No geolocation service");
        return;
    }

    if (cachedGeoPos && Date() - cachedGeoPosTime < 60000) return cachedGeoPos;

    const pos = await new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject);
    });
    cachedGeoPosTime = +Date();
    return cachedGeoPos = {
        long: pos.coords.longitude,
        lat: pos.coords.latitude
    };
}

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
            context.datalist = context.root.querySelector(".suggestions");

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
        this.lastUpdate = 0;

        this.input.addEventListener("keydown", async () => {
            let oldText = this.input.value;
            let time = +new Date();

            setTimeout(async () => {
                if (oldText === this.seachText || oldText !== this.input.value && time - this.lastUpdate < 500) return;
                let data = await this.#getNewSuggestions(oldText);
                if (time < this.lastUpdate) return; // The request "expired" (a new one was received in-between)
                this.lastUpdate = time;
                this.seachText = oldText;
                await this.#updateSuggestions(data);
            }, 500);
        });
    }

    async #addressCompletionApiUrl(query, frenchApi = false) {
        if (frenchApi) {
            let requestUrl = FRENCH_ADDRESS_API + "?q=" + query;
            await getCurrentPosition().then(coords => requestUrl += "&lat=" + coords.lat + "&lon=" + coords.long).catch(() => null);
            return requestUrl;
        } else {
            let requestUrl = ORS_ADDRESS_API + "&layers=address,neighbourhood,locality,borough&text=" + query;
            await getCurrentPosition().then(coords => requestUrl += "&focus.point.lon=" + coords.long + "&focus.point.lat=" + coords.lat).catch(() => null);
            return requestUrl;
        }
    }

    #clearSuggestions() {
        this.datalist.innerHTML = "";
        this.datalist.classList.add("hide");
    }

    #selectSuggestion(suggestion) {
        this.input.value = suggestion.properties.label;
        document.dispatchEvent(new CustomEvent("valueChanged", {
            detail: {
                fieldName: this["name"],
                value: {
                    label: suggestion.properties.label,
                    coords: suggestion.geometry.coordinates
                }
            }
        }));

        this.#clearSuggestions();
    }

    async #getNewSuggestions(searchValue) {
        let result = [];

        if ("Ma position".match(searchValue)) {
            let coords = await getCurrentPosition();
            if (coords) result.push({
                properties: {
                    label: "Ma position"
                },
                geometry: {
                    coordinates: [coords.long, coords.lat]
                }
            });
        }

        if (searchValue.length >= 3) { // The api won't respond if len < 3
            let url = await this.#addressCompletionApiUrl(searchValue);
            let response = await (await fetch(url)).json();
            result.push(...(response["features"] ?? []));
        }
        return result;
    }

    async #updateSuggestions(data) {
        this.#clearSuggestions();

        for (let suggest of data) {
            let option = document.createElement("li");
            option.innerText = suggest.properties.label;
            option.onclick = () => this.#selectSuggestion(suggest);
            this.datalist.appendChild(option);
        }
        this.datalist.classList.remove("hide");
    }
}