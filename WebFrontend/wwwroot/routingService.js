import {ActiveMQ} from "/components/activemq.js";

export class RoutingService {
    constructor() {
        if (RoutingService._instance) return RoutingService._instance;
        RoutingService._instance = this;
    }

    isLastRoute(start, end) {
        return this.start === start && this.end === end;
    }

    async getRoute(start, end) {
        //if (this.isLastRoute(start, end)) return this.route;

        let url = `http://localhost:5001/web/route?startLon=${start[0]}&startLat=${start[1]}&endLon=${end[0]}&endLat=${end[1]}`;
        const queueName = (await (await fetch(url)).json())["CalculateRouteResult"];
        console.log("QueueName : ", queueName);
        this.activemq = new ActiveMQ("ws://localhost:61614", "admin", "admin", queueName);
        console.log(this.activemq.getInstructions());
        return "test";
        this.start = start;
        this.end = end;
        return this.route;
    }
}