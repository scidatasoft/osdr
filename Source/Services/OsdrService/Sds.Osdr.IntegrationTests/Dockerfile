#FROM microsoft/dotnet:2.1-runtime AS base
#WORKDIR /app
#
#FROM microsoft/dotnet:2.1-sdk AS build
#WORKDIR /src
#COPY Sds.Osdr.IntegrationTests/Sds.Osdr.IntegrationTests.csproj Sds.Osdr.IntegrationTests/
#RUN dotnet restore Sds.Osdr.IntegrationTests/Sds.Osdr.IntegrationTests.csproj
#COPY . .
#WORKDIR /src/Sds.Osdr.IntegrationTests
#RUN dotnet build Sds.Osdr.IntegrationTests.csproj -o /app
#
#FROM build AS publish
#RUN dotnet publish Sds.Osdr.IntegrationTests.csproj -o /app
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app .
#ENTRYPOINT ["dotnet", "test"]

FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /integration

COPY Sds.Osdr.IntegrationTests.csproj .
RUN dotnet restore

COPY . .
ENTRYPOINT ["dotnet", "test"]