import {ActiveMQ} from "/components/activemq.js";

export class RoutingService {
    constructor() {
        if (RoutingService._instance) return RoutingService._instance;
        RoutingService._instance = this;
        this.activemq = new ActiveMQ("ws://localhost:61614", "admin", "admin");
    }

    isLastRoute(start, end) {
        return this.start === start && this.end === end;
    }

    async getRoute(start, end) {
        if (this.isLastRoute(start, end)) return;

        let url = `http://localhost:5001/web/route?startLon=${start[0]}&startLat=${start[1]}&endLon=${end[0]}&endLat=${end[1]}`;
        const queueName = (await (await fetch(url)).json())["CalculateRouteResult"];
        const receiveQueueName = queueName["Item1"];
        this.sendQueueName = queueName["Item2"];
        await this.activemq.connect(receiveQueueName);
        this.start = start;
        this.end = end;
    }

    async sendMessage(message) {
        console.log("sending message : ", message);
        await this.activemq.sendTo(this.sendQueueName, message);
    }
}