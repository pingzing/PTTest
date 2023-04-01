import * as signalR from "@microsoft/signalr";

const InitializeName: string = "Initialize";
const SendPositionName: string = "SendPosition"; // Sending position to server
const PushPositionsName: string = "PushPositions"; // Receiving positions from server

// Local dev URL
const url = "http://localhost:5276/position";

// Azure URL
//const url = "https://ptowntest.azurewebsites.net/position";

interface PlayerPosition {
    id: string,
    x: number,
    y: number
}

const canvas: HTMLCanvasElement = document.getElementById('pttest-canvas') as HTMLCanvasElement;
const connectButton: HTMLButtonElement = document.getElementById("connect-button") as HTMLButtonElement;
const connectedLabel: HTMLParagraphElement = document.getElementById("connected-label") as HTMLParagraphElement;

let _playerId: string = "disconnected";
let _connection: signalR.HubConnection | null = null;

let _selfX: number | null;
let _selfY: number | null;
let _allPositions: PlayerPosition[] = [];

async function main(): Promise<void> {
    canvas.addEventListener('mousemove', onCanvasMouseMove);

    connectButton.addEventListener("click", connect);

    _connection = new signalR.HubConnectionBuilder().withUrl(url).build();

    _connection.on(InitializeName, onInitialize);
    _connection.on(PushPositionsName, onPushPositions);
    _connection.onclose(e => {
        console.log(`Connection closed. Details: ${e}`);
        connectedLabel.textContent = "Disconnected...";
    });

    async function connect(_: Event) {
        console.log(`connect clicked!`);
        await _connection?.start();
    }
}

function onCanvasMouseMove(e: MouseEvent) {
    // tell the browser we're handling this event
    e.preventDefault();
    e.stopPropagation();

    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    _selfX = mouseX;
    _selfY = mouseY;

    draw();

    if (_connection?.connectionId)
    {
        // TODO: This is probably hilariously spammy. Would need a throttle of some kind here.
        _connection?.send(SendPositionName, _playerId, mouseX, mouseY);
    }
}

function onInitialize(playerId: string): void {
    console.log(`OnInitialize claled with playerId ${playerId}`);
    connectedLabel.textContent = `Connected! Player ID: ${playerId}`;
    _playerId = playerId;
}

function onPushPositions(positions: PlayerPosition[]): void {
    console.log(`Heard onPushPositions with list of length ${positions.length}`);
    _allPositions = positions;    
    draw();
}

function draw() {
    const context = canvas.getContext("2d");
    if (!context) {
        return;
    }

    context.clearRect(0, 0, canvas.width, canvas.height);

    if (_selfX && _selfY) {
        // Draw the player's rect
        context.fillStyle = "red";
        context.fillRect(_selfX - 3, _selfY - 3, 4, 4);
    }

    // Draw everyone else's

    // Copy array so it doesn't vanish out from underneath if we get an update mid-draw.    
    const allPositions = [..._allPositions]
    if (allPositions.length > 0) {
        context.fillStyle = "black";
        for (const pos of allPositions) {
            // Don't draw the player.
            if (pos.id === _playerId) {
                continue;
            }

            context.fillRect(pos.x - 3, pos.y - 3, 4, 4);
        }
    }
}

// Bootstrap main
(async () => {
    await main();
})().catch(e => {
    console.error(`Main failed. Details: ${e}`);
})
