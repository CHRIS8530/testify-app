# Stage 1: build. The build context is the repo root, so every path below starts with api/.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Restore first, so this step is cached until the project file changes.
COPY api/Testify.Api.csproj api/
RUN dotnet restore api/Testify.Api.csproj

COPY api/ api/
RUN dotnet publish api/Testify.Api.csproj -c Release -o /out --no-restore

# Stage 2: runtime only. No SDK and no source code in the final image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN useradd -m appuser
COPY --from=builder --chown=appuser:appuser /out .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
CMD ["dotnet", "Testify.Api.dll"]