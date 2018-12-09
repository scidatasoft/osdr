# Use SDK image to build solution
FROM microsoft/dotnet:2.1-sdk AS build-env

WORKDIR /build

COPY . .

RUN dotnet restore --configfile /build/Nuget.config Sds.ChemicalProperties.WebApi/Sds.ChemicalProperties.WebApi.csproj

RUN dotnet build Sds.ChemicalProperties.WebApi/Sds.ChemicalProperties.WebApi.csproj

RUN dotnet publish Sds.ChemicalProperties.WebApi/Sds.ChemicalProperties.WebApi.csproj -o /dist

# Build runtime image
FROM microsoft/dotnet:2.1-runtime-deps

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

COPY --from=build-env /dist ./

ENV ASPNETCORE_URLS http://+:18006
EXPOSE 18006

ENTRYPOINT ["./Sds.ChemicalProperties.WebApi.dll"]
