# HRM API image. Build context is hrm-server/ (see docker-compose.yml):
#   docker build -f src/HRM.API/Dockerfile -t hrm-api hrm-server

# ---- build: restore and publish with the full SDK -------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer is cached until a dependency changes,
# so editing code doesn't re-download every package.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/HRM.Domain/HRM.Domain.csproj                 src/HRM.Domain/
COPY src/HRM.Application/HRM.Application.csproj       src/HRM.Application/
COPY src/HRM.Infrastructure/HRM.Infrastructure.csproj src/HRM.Infrastructure/
COPY src/HRM.API/HRM.API.csproj                       src/HRM.API/
RUN dotnet restore src/HRM.API/HRM.API.csproj

COPY src/ src/
RUN dotnet publish src/HRM.API/HRM.API.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# ---- runtime: ASP.NET Core only, no SDK or source code ---------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Plain HTTP inside the Docker network; Nginx is the only thing that talks to it.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# The base image ships a non-root "app" user; never run the API as root.
USER $APP_UID
ENTRYPOINT ["dotnet", "HRM.API.dll"]
