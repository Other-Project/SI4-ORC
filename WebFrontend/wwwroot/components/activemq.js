export class ActiveMQ {
    static client;
    url;
    login;
    passcode;

    constructor(url, login, passcode) {
        this.url = url;
        this.login = login;
        this.passcode = passcode;
    }

    async connect(destination) {
        if (window.WebSocket) {
            document.dispatchEvent(new CustomEvent("instructionsReset"));
            this.client = Stomp.client(this.url);
            this.client.connect(this.login, this.passcode, () => {
                this.client.subscribe(destination, (message) => {
                    const parsedMessage = JSON.parse(message.body);
                    const tag = message.headers["tag"];
                    if (tag === "instruction") {
                        document.dispatchEvent(new CustomEvent("instructionAdded", {detail: parsedMessage}));
                    } else if (tag === "distance") {
                        document.dispatchEvent(new CustomEvent("distanceAdded", {detail: parsedMessage}));
                    } else if (tag === "duration") {
                        document.dispatchEvent(new CustomEvent("durationAdded", {detail: parsedMessage}));
                    }
                    else {
                        document.dispatchEvent(new CustomEvent("popupMessage", {detail: parsedMessage}));
                    }
                });
            });
        }
    }

    async sendTo(destination, message) {
        if (window.WebSocket) {
            this.client.send(destination, {}, JSON.stringify(message));
        }
    }
}