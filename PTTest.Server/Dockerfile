FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PTTest.Server.csproj", "PTTest.Server/"]
RUN dotnet restore "PTTest.Server.csproj"
COPY . .
WORKDIR "/src/PTTest.Server"
RUN dotnet build "PTTest.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PTTest.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PTTest.Server.dll"]