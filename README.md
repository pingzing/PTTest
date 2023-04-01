# PTTest

Small test server and client application for displaying coordinates of all connected users.

The server uses SignalR to maintain connections between itself and clients.

The client displays the user's mouse position (displayed as a red square) when mousing over a HTML canvas. If the user clicks
"Connect", their position will be shared with the server, and the client will begin displaying all other users' positions 
(displayed as black squares).

## Hosted Demo
A hosted demo is available at https://ptownteststorage.blob.core.windows.net/pttest/index.html.

If you want to point a locally-running client at the hosted server, the server is at https://ptowntest.azurewebsites.net/position. 
(The `/position` endpoint is what the SignalR client uses to establish a connection.)

## Test mode

If the server reeives a GET at `<baseurl>/testMode?playerCount=<positive integer>`, the server will begin simulating `playerCount` users
connected at once.

To end test mode, send a GET to `<baseurl>/endtestmode`.

### Building the Server

Note: by default, the server logs are very verbose. To turn them down, change the following log overrides in `appsettings.json`:
```
"Microsoft.AspNetCore.SignalR": "Debug",
"Microsoft.AspNetCore.Http.Connections": "Debug",
```
from `Debug` to `Information`.

#### Native (e.g. non-Docker)

Requirements:
 - .NET 7 SDK

 Navigate to `/PTTest.Server`.

 Then, run 
 ```bash
 dotnet run
 ```
 The server will print the port it's listening on to the console.

 #### Docker

 (Note that the Docker image only exposes non-HTTPS endpoints and port 80, to avoid needing to deal with certificates).

 Building the container:

 Navigate to `/PTTest.Server`, and run
 ```bash
 docker build -f ./Dockerfile . --tag=pttest.server
 ```

 Running the container:
 ```bash
 docker run -dt -e "ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=true" -e "ASPNETCORE_ENVIRONMENT=Development" -e "ASPNETCORE_URLS=http://+:80" -e "DOTNET_USE_POLLING_FILE_WATCHER=1" -p 5276:80 --name PTTest.Server pttest.server
 ```

 If the client's base URL is configured to look for another port, change the `-p 5276:80` flag from `5276` to whichever port the client is trying to connect on.

 ### Building the Client

 #### Native (e.g non-Docker)

 Requirements: 
  - NodeJS 18.15.0
  - npm 9.5.0

 Navigate to `/PTTest.Client`.

 Then, run
 ```bash
 npm ci
 npx webpack
 ```

 The generated webpage will be in `./dist`.

 #### Docker

 (Note that the generated container is not runnable--it's only a wrapper around the output.)

 Navigate to `/PTTest.Client` and run
 ```bash
 docker buildx build -f ./Dockerfile . --tag=pttest.client --output type=local,dest=./
 ```

 The generated webpage will be in `./dist`.

 ## Architectural notes and caveats

 The basic flow works like this:

 - Client connects, receives player ID
 - On canvas mouseover, client sends `SendPosition()` calls with an ID, and the coordinates.
 - Then, on the server, that message goes through this flow:
   - PositionApiHub -> PositionService -> _positionUpdates Channel
   - PositionService continuously runs a background task, reading messages from the _positionUpdates Channel, and updates the _currentPositions dictionary.
   - TickService continuously runs a background task:
	 - On tick, get snapshot of _currentPositions
	 - Send that snapshot to every connected client
 - The client receives a steady stream of these updates.
 - When it receives an update, it iterates through the list of received coordinates, and draws them all.  

 Strictly speaking, the Channel between the `PositionApiHub` and the `PositionService` background task isn't necessary. We could just update the dictionary of positions immediately.
 But having the Channel in place allows us to apply a limit to the number of in-flight position updates, and begin dropping old ones if we begin falling behind.

The TickService tries to limit the amount of CPU time it hogs by sleeping between ticks. However, given that `Task.Delay()` 
uses timers under the hood and (on Windows, at least), timers only have a resolution of about 16ms, it's probably not as effective as I'd like.

There's no explicit idle detection for a player. It'd be nice if each entry in the PositionService _currentPositions had a TTL. We could also apply a timeout to 
the SignalR connection, and disconnect them if we haven't heard a heartbeat from them in a while, and remove their entry in the PositionService.

The client is kind of bodged together. My experience in web front-end is limited, and this is actually the first time I've had to put together a web page that
required a bundler from scratch. Whee, Webpack.

The client messages to the server definitely need some throttling. Right now, it just sends position updates as fast as it gets mouseover events. Would be nice to restrict
it to the server tick rate--not much point in sendings updates more frequently.

Speaking of which, the server's tick rate is hard-coded in the TickService. That should probably be configurable.