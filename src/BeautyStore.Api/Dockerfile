# ── BeautyStore.Api — multi-stage Dockerfile ─────────────────────────────────
#
# Build context: repo root (D:\thinkschool\BeautyStoreCapstone)
#
#   docker build -f src/BeautyStore.Api/Dockerfile -t beautystoreapi:local .
#
# The multi-project COPY pattern (restore → copy → publish) allows Docker to
# cache the expensive `dotnet restore` layer. Re-builds after a source change
# only re-run from the COPY . . line onward.
#
# Port 8080 matches:
#   infra/modules/api.bicep → ingress.targetPort: 8080
#   ASPNETCORE_URLS=http://+:8080  (set as Container App env var in Bicep)

# ── Stage 1: Restore ─────────────────────────────────────────────────────────
# Restore NuGet packages first so this layer is cached until .csproj changes.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy only the project files needed for restore.
# Add more lines here when Application / Infrastructure / Domain .csproj files exist.
COPY ["src/BeautyStore.Api/BeautyStore.Api.csproj", "src/BeautyStore.Api/"]

RUN dotnet restore "src/BeautyStore.Api/BeautyStore.Api.csproj" \
    --runtime linux-x64

# ── Stage 2: Build & Publish ──────────────────────────────────────────────────
FROM restore AS publish
WORKDIR /src

# Copy the full source tree (from repo root) now that packages are cached.
COPY . .

RUN dotnet publish "src/BeautyStore.Api/BeautyStore.Api.csproj" \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained false \
    --output /app/publish

# ── Stage 3: Runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Security: run as a non-root user.
# Container Apps (and AKS) block root by default at the platform level,
# but setting it explicitly here is defence-in-depth.
RUN groupadd --system --gid 1001 appgroup \
 && useradd  --system --uid 1001 --gid appgroup --no-create-home appuser

# Copy published output from the publish stage.
COPY --from=publish --chown=appuser:appgroup /app/publish .

USER appuser

# Bind to port 8080 (non-privileged — doesn't require root).
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BeautyStore.Api.dll"]
