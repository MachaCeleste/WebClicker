export class Upgrade {
    constructor(data) {
        this.type = data.upgradeType;
        this.state = data.state;
        this.toggle = data.toggle;
        this.multilevel = data.multilevel;
        this.level = data.level;
        this.cost = data.cost;
        this.cap = data.cap;
    }
}