FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json ./
COPY ToolLib/ToolLib.csproj ToolLib/
RUN dotnet restore ToolLib/ToolLib.csproj
COPY ToolLib/ ToolLib/
WORKDIR /src/ToolLib
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ToolLib.dll"]
