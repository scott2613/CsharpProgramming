# Stage 1: Build Web và API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish "ComputerStore.API/ComputerStore.API.csproj" -c Release -o /app/publish/api
RUN dotnet publish "ComputerStore.Web/ComputerStore.Web.csproj" -c Release -o /app/publish/web

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish/api ./api
COPY --from=build /app/publish/web ./web

# Chạy API từ đúng thư mục /app/api để nó load đúng appsettings.json và connection string
RUN printf '#!/bin/sh\n\
cd /app/api && ASPNETCORE_URLS=http://0.0.0.0:5000 dotnet ComputerStore.API.dll &\n\
sleep 4\n\
cd /app/web && export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}" && exec dotnet ComputerStore.Web.dll\n' > /app/entrypoint.sh \
    && chmod +x /app/entrypoint.sh

EXPOSE 8080

ENTRYPOINT ["/bin/sh", "/app/entrypoint.sh"]