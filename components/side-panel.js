export class SidePanel extends HTMLElement {


    constructor() {
        super();

        const shadow = this.attachShadow({mode: "open"});
        fetch("/components/side-panel.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;
            })
    }
}

