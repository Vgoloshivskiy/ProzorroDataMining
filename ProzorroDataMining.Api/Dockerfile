# (apply only the highlighted change — add the ASPNETCORE_URLS env)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9090
EXPOSE 9091

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ProzorroDataMining.Api/ProzorroDataMining.Api.csproj", "ProzorroDataMining.Api/"]
RUN dotnet restore "./ProzorroDataMining.Api/ProzorroDataMining.Api.csproj"
COPY . .
WORKDIR "/src/ProzorroDataMining.Api"
RUN dotnet build "./ProzorroDataMining.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProzorroDataMining.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV POSTGRES_HOST=localhost
ENV POSTGRES_PASSWORD=admin
ENV POSTGRES_USER=postgres
ENV POSTGRES_DATABASE=ProzorroData
ENV POSTGRES_PORT=5432
ENV ASPNETCORE_URLS=http://+:9090
ENTRYPOINT ["dotnet", "ProzorroDataMining.Api.dll"]