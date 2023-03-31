import * as signalR from "@microsoft/signalr";

const InitializeName: string = "Initialize";

//const canvas: HTMLCanvasElement = document.getElementById('pttest-canvas') as HTMLCanvasElement;
const connection = new signalR.HubConnectionBuilder().withUrl("/position").build();

connection.on(InitializeName, (playerId: string) => {
    console.log(`OnInitialize called, got playerID : ${playerId}`);
})