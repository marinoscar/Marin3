# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MarinApp/MarinApp.csproj", "MarinApp/"]
RUN dotnet restore "./MarinApp/MarinApp.csproj"
COPY . .
WORKDIR "/src/MarinApp"
RUN dotnet build "./MarinApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MarinApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# create the dir in the final image (use root for this layer)
USER 0:0
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R 1654:1654 /home/app/.aspnet

# switch to app user for runtime
USER 1654:1654

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarinApp.dll"]