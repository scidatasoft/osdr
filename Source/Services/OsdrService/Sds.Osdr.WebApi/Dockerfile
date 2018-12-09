# Use SDK image to build solution
FROM microsoft/dotnet:2.1-sdk AS build-env

ARG RID=linux-x64

# mongo db url for tests
ARG CI_MONGO_DB=ci_url

ENV OSDR_MONGO_DB=$CI_MONGO_DB

WORKDIR /build

COPY . .

RUN dotnet restore --configfile /build/Nuget.config Sds.Osdr.WebApi/Sds.Osdr.WebApi.csproj

# RUN dotnet build OsdrService.sln

# RUN dotnet test Sds.Osdr.Domain.BddTests/Sds.Osdr.Domain.BddTests.csproj

# RUN dotnet test Sds.Osdr.WebApi.IntegrationTests/Sds.Osdr.WebApi.IntegrationTests.csproj

RUN dotnet publish Sds.Osdr.WebApi/Sds.Osdr.WebApi.csproj -r $RID -o /dist

# Build runtime image
FROM microsoft/dotnet:2.1-runtime-deps

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

COPY --from=build-env /dist ./

ENV ASPNETCORE_URLS http://+:18006
EXPOSE 18006

ENTRYPOINT ["./Sds.Osdr.WebApi"]

