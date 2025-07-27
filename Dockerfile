# ── BUILD ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Copy & restore only the API csproj
COPY WalletBackend/WalletBackend.csproj WalletBackend/
RUN dotnet restore WalletBackend/WalletBackend.csproj

# 2) Copy the rest of the API source
COPY WalletBackend/. WalletBackend/

# 3) Publish just that project
WORKDIR /src/WalletBackend
RUN dotnet publish WalletBackend.csproj -c Release -o /app/publish

# ── RUNTIME ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# grab your published bits
COPY --from=build /app/publish .

# metadata only—containers still expose 80 by default
EXPOSE 80

# run the API
ENTRYPOINT ["dotnet", "WalletBackend.dll"]