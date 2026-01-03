FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Workers.Push/Workers.Push.csproj
RUN dotnet publish Workers.Push/Workers.Push.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8082
ENTRYPOINT ["dotnet", "Workers.Push.dll"]