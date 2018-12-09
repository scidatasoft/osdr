# Use SDK image to build solution
FROM microsoft/dotnet:2.1-sdk AS build-env

ARG RID=linux-x64

WORKDIR /build

COPY . .

RUN dotnet restore --configfile /build/Nuget.config Sds.ChemicalStandardizationValidation.Persistance/Sds.ChemicalStandardizationValidation.Persistance.csproj

RUN dotnet build Sds.ChemicalStandardizationValidation.Persistance/Sds.ChemicalStandardizationValidation.Persistance.csproj

RUN dotnet publish Sds.ChemicalStandardizationValidation.Persistance/Sds.ChemicalStandardizationValidation.Persistance.csproj -r $RID -o /dist

# Build runtime image
FROM microsoft/dotnet:2.1-runtime-deps

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

COPY --from=build-env /dist ./

ENTRYPOINT ["./Sds.ChemicalStandardizationValidation.Persistence"]
