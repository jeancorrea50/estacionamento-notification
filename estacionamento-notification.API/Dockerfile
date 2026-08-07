FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["estacionamento-notification.API/estacionamento-notification.API.csproj", "estacionamento-notification.API/"]
COPY ["estacionamento-notification.Application/estacionamento-notification.Application.csproj", "estacionamento-notification.Application/"]
COPY ["estacionamento-notification.Domain/estacionamento-notification.Domain.csproj", "estacionamento-notification.Domain/"]
COPY ["estacionamento-notification.Infrastructure/estacionamento-notification.Infrastructure.csproj", "estacionamento-notification.Infrastructure/"]

RUN dotnet restore "estacionamento-notification.API/estacionamento-notification.API.csproj"

COPY . .

WORKDIR "/src/estacionamento-notification.API"
RUN dotnet publish "estacionamento-notification.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV PathBase=/estacnotification

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "estacionamento-notification.API.dll"]
