export class SidePanel extends HTMLElement {
    constructor() {
        super();

        const shadow = this.attachShadow({mode: "open"});
        fetch("/components/side-panel.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;

                const start_input = shadow.getElementById("start");
                let start_suggestions = shadow.getElementById("start-suggestions");
                this.addSuggestions(start_input, start_suggestions);
                const end_input = shadow.getElementById("end");
                let end_suggestions = shadow.getElementById("end-suggestions");
                this.addSuggestions(end_input, end_suggestions);

            })
    }

    async askAPIAddress(url) {
        return await (await fetch(url)).json();
    }

    addSuggestions(input, suggestion) {
        while (suggestion.firstChild != null) suggestion.removeChild(suggestion.firstChild);
        input.addEventListener("input", async () => {
            const value = input.value;
            if (value.length < 3) {
                return;
            }
            let url = "https://api-adresse.data.gouv.fr/search/?q=" + value;
            let data = await this.askAPIAddress(url);
            for (let suggest of data.features) {
                let option = document.createElement('option');
                option.value = suggest.properties.label;
                suggestion.appendChild(option);
            }
        });
    }
}

