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
        if (this.isLastRoute(start, end)) return this.instructions;

        let url = `http://localhost:5001/web/route?startLon=${start[0]}&startLat=${start[1]}&endLon=${end[0]}&endLat=${end[1]}`;
        const queueName = (await (await fetch(url)).json())["CalculateRouteResult"];
        console.log("QueueName : ", queueName);
        await this.activemq.connect(queueName);
        this.instructions = await this.activemq.getInstructions();
        /*while (this.instructions.length < 1915)
        {

            this.instructions = await this.activemq.getInstructions();
        }*/
        console.log("Lenght instructions : ", this.instructions.length);
        this.start = start;
        this.end = end;
        return this.instructions;
    }
}