const FRENCH_ADDRESS_API = "https://api-adresse.data.gouv.fr/search/";
const ORS_ADDRESS_API = "https://api.openrouteservice.org/geocode/autocomplete?api_key=5b3ce3597851110001cf624846c93be49c1f44f0949187d18b1d653c";

let currentPosition;
if ("geolocation" in navigator)
    (async () => {
        try {
            await updateGeoLoc();
            setInterval(async () => await updateGeoLoc(), 15000);
        } catch (e) {
            console.log(e);
        }
    })().then();
else console.warn("No geolocation service");

async function updateGeoLoc() {
    const pos = await new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject);
    });
    currentPosition = {
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

    setLocation(location) {
        this.input.value = location;
        this.input.setCustomValidity("");
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
        this.input.addEventListener("focusin", async () => await this.#updateSuggestions(await this.#getNewSuggestions(this.input.value)));
        this.input.addEventListener("focusout", () => {
            this.#clearSuggestions();
            this.input.reportValidity();
        });
        this.input.addEventListener("keydown", async () => {
            this.input.setCustomValidity("No suggestion selected");
            if (this.inputTimeout) clearTimeout(this.inputTimeout);
            this.inputTimeout = setTimeout(async () => await this.#updateSuggestions(await this.#getNewSuggestions(this.input.value)), 500);
        });
    }

    async #addressCompletionApiUrl(query, frenchApi = false) {
        if (frenchApi) {
            let requestUrl = FRENCH_ADDRESS_API + "?q=" + query;
            if (currentPosition) requestUrl += "&lat=" + currentPosition.lat + "&lon=" + currentPosition.long;
            return requestUrl;
        } else {
            let requestUrl = ORS_ADDRESS_API + "&layers=address,neighbourhood,locality,borough&text=" + query;
            if (currentPosition) requestUrl += "&focus.point.lon=" + currentPosition.long + "&focus.point.lat=" + currentPosition.lat;
            return requestUrl;
        }
    }

    #clearSuggestions() {
        this.datalist.innerHTML = "";
        this.datalist.classList.add("hide");
    }

    #selectSuggestion(suggestion) {
        this.setLocation(suggestion.properties.label);
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

        if ("ma position".includes(searchValue.toLowerCase()) && currentPosition)
            result.push({
                properties: {
                    label: "Ma position"
                },
                geometry: {
                    coordinates: [currentPosition.long, currentPosition.lat]
                }
            });

        if (/^[-+]?([1-8]?\d(\.\d+)?|90(\.0+)?),\s*[-+]?(180(\.0+)?|((1[0-7]\d)|([1-9]?\d))(\.\d+)?)$/.test(searchValue)) {
            let [lat, long] = searchValue.split(",").map(s => s.trim());
            result.push({
                properties: {
                    label: `Position GPS (${lat}, ${long})`
                },
                geometry: {
                    coordinates: [long, lat]
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
            option.onmousedown = e => e.preventDefault();
            this.datalist.appendChild(option);
        }
        if (data && data.length > 0) this.datalist.classList.remove("hide");
    }
}