import {ActiveMQ} from "/components/activemq.js";

export class Popup extends HTMLElement {
    constructor() {
        super();
        const shadow = this.attachShadow({mode: "open"});
        let context = this;
        fetch("/components/popup.html")
            .then(stream => stream.text())
            .then(async text => {
                shadow.innerHTML = text;

                context.text = shadow.getElementById("text");
                context.popup = shadow.getElementById("app-popup");
            });

        document.addEventListener("popupMessage", ev => this.changePopupMessage(ev.detail.message));
        document.addEventListener("hidePopup", () => this.hidePopup());
    }

    changePopupMessage(message) {
        this.text.innerHTML = message;
        this.popup.style.display = "block";
    }

    hidePopup() {
        this.popup.style.display = "none";
    }
}