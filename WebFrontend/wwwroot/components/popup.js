import {ActiveMQ} from "/components/activemq.js";

export class Popup extends HTMLElement {
    constructor() {
        super();
        const shadow = this.attachShadow({mode: "open"});
        fetch("/components/popup.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;
            });
    }

}