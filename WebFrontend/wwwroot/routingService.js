export class RoutingService {
    constructor() {
        if (RoutingService._instance) return RoutingService._instance;
        RoutingService._instance = this;
    }

    isLastRoute(start, end) {
        return this.start === start && this.end === end;
    }

    async getRoute(start, end) {
        if (this.isLastRoute(start, end)) return this.route;
        
        let url = `http://localhost:5001/web/route?startLon=${start[0]}&startLat=${start[1]}&endLon=${end[0]}&endLat=${end[1]}`;
        this.route = (await (await fetch(url)).json())["CalculateRouteResult"];
        this.start = start;
        this.end = end;
        return this.route;
    }
}