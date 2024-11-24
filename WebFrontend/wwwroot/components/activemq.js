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
            this.instructions = [];
            this.client = Stomp.client(this.url);

            this.client.connect(this.login, this.passcode, () => {
                console.log("connected to Stomp");
                this.client.subscribe(destination, (message) => {
                    //console.log("Instructions : ", instructions);
                    this.instructions.push(JSON.parse(message.body));
                    this.instructionsReceived = true;
                });
            });
        }
    }

    async getInstructions() {
        return new Promise((resolve) => {
            const checkInstructions = () => {
                if (this.instructionsReceived) {
                    resolve(this.instructions);
                } else {
                    setTimeout(checkInstructions, 500);
                }
            };
            checkInstructions();
        });
    }
}