import {ActiveMQ} from "/components/activemq.js";

export class Popup extends HTMLElement {
    constructor() {
        super();
        const shadow = this.attachShadow({mode: "open"});
        this.connectedCallback();
        fetch("/components/popup.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;
            });
    }


    connectedCallback() {
        let url = "ws://localhost:61614/admin"
        let login = "admin";
        let passcode = "admin";
        let destination = "/topic/chat.general";
        this.activeMQ = new ActiveMQ(url, login, passcode, destination, this.updateMessage.bind(this));
    }

    updateMessage(message) {
        console.log("In updateMessage : ", message);
        this.shadowRoot.getElementById("text").innerText = message;
    }
}