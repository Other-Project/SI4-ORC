export class ActiveMQ {
    static client;
    url;
    login;
    passcode;
    destination;
    messageCallback;

    constructor(url, login, passcode, destination, messageCallback) {
        console.log("Constructor")
        if (ActiveMQ.INSTANCE) {
            console.log("Im returning the instance");
            return ActiveMQ.INSTANCE;
        }
        ActiveMQ.INSTANCE = this;
        this.messageCallback = messageCallback;
        $(document).ready(() => this.initialize(url, login, passcode, destination));
    }

    initialize(url, login, passcode, destination) {
        console.log("Hello, I'm ready!");
        if (window.WebSocket) {
            this.setupStompClient(url, login, passcode, destination);
        }
    }

    setupStompClient(url, login, passcode, destination) {
        this.url = url;
        this.client = Stomp.client(url);
        this.login = login;
        this.passcode = passcode;
        this.destination = destination;

        this.client.connect(login, passcode, () => {
            console.log("connected to Stomp");
            this.client.subscribe(destination, this.onMessage.bind(this));
        });
    }

    onConnect() {
        console.log("connected to Stomp");
        this.client.subscribe(this.destination, this.onMessage.bind(this));
    }

    send(message) {
        console.log("Message to send : ", message);
        this.client.send(this.destination, {}, message);
    }

    onMessage(message) {
        console.log("received message: " + message.body);
        if (this.messageCallback) {
            this.messageCallback(message.body);
        }
    }
}