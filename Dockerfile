# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 443
EXPOSE 80
EXPOSE 5000
EXPOSE 5001


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FooBooRealTime-back-dotnet.csproj", "."]
RUN dotnet restore "./FooBooRealTime-back-dotnet.csproj"
COPY . .

WORKDIR "/src/."
RUN dotnet build "./FooBooRealTime-back-dotnet.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FooBooRealTime-back-dotnet.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FooBooRealTime-back-dotnet.dll"]

# docker command
#docker run -p 8080:80 -p 8081:443 -p 5000:5000 -p 5001:5001 -e ASPNETCORE_URLS="https://+;http://+" -e ASPNETCORE_HTTPS_PORT=7001 -e ASPNETCORE_Kestrel__Certificates__Default__Password="Xuan12343@" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/dockerdemo.pfx -v C:\Users\thanc\.aspnet\https:/https/ ssldemo:v1