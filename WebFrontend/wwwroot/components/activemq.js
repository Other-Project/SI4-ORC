export class ActiveMQ {
    static client;
    url;
    login;
    passcode;
    destination;
    instructions = [];

    constructor(url, login, passcode, destination) {
        if (window.WebSocket) {

            var client = Stomp.client(url);

            // the client is notified when it is connected to the server.
            client.connect(login, passcode, () => {
                console.log("connected to Stomp");
                //$('#connect').fadeOut({duration: 'fast'});
                //$('#connected').fadeIn();
                client.subscribe(destination,  (message) => {
                    //console.log("received message: " + message.body);
                    console.log("Before parsing : ");
                    var instructions = JSON.parse(message.body);
                    console.log("Instructions : ", instructions);
                    this.instructions.push(instructions);
                });
            });
        }
    }

    async getInstructions() {
        console.log("Instructions : ", this.instructions);
        return this.instructions;
    }

    setupStompClient(url, login, passcode, destination) {
        /*console.log("Setting up Stomp client");
        this.url = url;
        this.client = Stomp.client(url);
        this.login = login;
        this.passcode = passcode;
        this.destination = destination;

        this.client.connect(login, passcode, () => {
            console.log("connected to Stomp");
            console.log("Destination : ", destination);
            this.client.subscribe(destination, this.onMessage.bind(this));
        });*/
    }
/*
    connectTo(login, passcode, destination) {
        console.log("Setting up Stomp client");
        console.log("URL : ", this.url);
        console.log("Login : ", login);
        console.log("Passcode : ", passcode);
        console.log("Destination : ", destination);
        console.log("Client : ", this.client);
        this.client.connect(login, passcode, () => {
            console.log("connected to Stomp");
            console.log("Destination : ", destination);
            this.client.subscribe(destination, function (message) {
                console.log("received message: " + message.body);
            });
        }, (error) => {
            console.log("Error : ", error);
        });
        console.log("Test d'envoi de message");
        //this.send("Test d'envoi de message", destination);
    }*/

    /*onConnect() {
        console.log("connected to Stomp");
        this.client.subscribe(this.destination, this.onMessage.bind(this));
    }

    send(message, destination) {
        console.log("Message to send : ", message);
        this.client.send(destination, {}, message);
    }

    onMessage(message) {
        console.log("received message: " + message.body);
    }*/
}