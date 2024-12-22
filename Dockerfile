# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
EXPOSE 443


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN dotnet dev-certs https --trust || echo "Certificate trust not supported in Docker"
ENV ASPNETCORE_URLS="https://+:5001;http://+:5000"
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