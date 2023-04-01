# PTTest

Small test server and client application for displaying coordinates of all connected users.

The server uses SignalR to maintain connections between itself and clients.

The client displays the user's mouse position (displayed as a red square) when mousing over a HTML canvas. If the user clicks
"Connect", their position will be shared with the server, and the client will begin displaying all other users' positions 
(displayed as black squares).

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




