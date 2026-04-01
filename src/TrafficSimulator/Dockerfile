FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/TrafficSimulator/TrafficSimulator.csproj src/TrafficSimulator/
RUN dotnet restore src/TrafficSimulator/TrafficSimulator.csproj
COPY src/TrafficSimulator/ src/TrafficSimulator/
RUN dotnet publish src/TrafficSimulator/TrafficSimulator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TrafficSimulator.dll"]
