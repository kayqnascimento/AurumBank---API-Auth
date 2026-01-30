FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Aurum.AuthApi/*.csproj Aurum.AuthApi/
RUN dotnet restore Aurum.AuthApi/Aurum.AuthApi.csproj

COPY . ./
RUN dotnet publish Aurum.AuthApi/Aurum.AuthApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Aurum.AuthApi.dll"]
