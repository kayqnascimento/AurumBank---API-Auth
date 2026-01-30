# ================================
# Build stage
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solução e projeto
COPY Aurum.AuthApi/*.csproj Aurum.AuthApi/
RUN dotnet restore Aurum.AuthApi/Aurum.AuthApi.csproj

# Copiar tudo
COPY . ./

# Publish
RUN dotnet publish Aurum.AuthApi/Aurum.AuthApi.csproj \
    -c Release -o /app/publish

# ================================
# Runtime stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Aurum.AuthApi.dll"]
