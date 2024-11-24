export class ActiveMQ {
    static client;
    url;
    login;
    passcode;
    instructions = [];

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
                console.log("connected to Stomp");
                this.client.subscribe(destination, (message) => {
                    document.dispatchEvent(new CustomEvent("instructionAdded", { detail: JSON.parse(message.body) }));
                });
            });
        }
    }
}