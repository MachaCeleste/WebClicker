let sequence = 0;

export class ServerMessage {
    constructor(id) {
        this.id = id;
        this.sequence = ++sequence;
        this.strValues = [];
        this.intValues = [];
        this.floatValues = [];
    }
    addString(val) { this.strValues.push(String(val)); return this; }
    addInt(val) { this.intValues.push(Math.floor(val)); return this; }
    addFloat(val) { this.floatValues.push(Number(val)); return this; }

    send(socket) {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send(JSON.stringify(this));
            console.log(`Sent message #${this.sequence} (ID: ${this.id})`);
        }
    }
}

export class ClientMessage {
    constructor(rawData) {
        this.id = rawData.id;
        this.strValues = [...(rawData.strValues || [])];
        this.intValues = [...(rawData.intValues || [])];
        this.floatValues = [...(rawData.floatValues || [])];
    }

    getString() {
        return this.strValues.shift();
    }

    getInt() {
        return this.intValues.shift();
    }

    getFloat() {
        return this.floatValues.shift();
    }
}