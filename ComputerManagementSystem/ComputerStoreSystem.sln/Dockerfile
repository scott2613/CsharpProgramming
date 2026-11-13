FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ComputerStore.Core/ComputerStore.Core.csproj ComputerStore.Core/
COPY ComputerStore.Application/ComputerStore.Application.csproj ComputerStore.Application/
COPY ComputerStore.Infrastructure/ComputerStore.Infrastructure.csproj ComputerStore.Infrastructure/
COPY ComputerStore.API/ComputerStore.API.csproj ComputerStore.API/
RUN dotnet restore ComputerStore.API/ComputerStore.API.csproj
COPY . .
RUN dotnet publish ComputerStore.API/ComputerStore.API.csproj -c Release --no-restore -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet ComputerStore.API.dll --urls http://0.0.0.0:${PORT:-8080}"]
