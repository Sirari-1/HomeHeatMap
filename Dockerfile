FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["HomeHeatMap/HomeHeatMap.csproj", "HomeHeatMap/"]
RUN dotnet restore "HomeHeatMap/HomeHeatMap.csproj"

COPY . .
WORKDIR "/src/HomeHeatMap"
RUN dotnet publish "HomeHeatMap.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet HomeHeatMap.dll --urls http://0.0.0.0:${PORT:-10000}"]
