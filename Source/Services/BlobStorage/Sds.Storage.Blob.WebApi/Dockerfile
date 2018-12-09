# Use SDK image to build solution
FROM microsoft/dotnet:2.1-sdk AS build-env

ARG RID=linux-x64

WORKDIR /build

COPY . .

RUN dotnet restore --configfile Nuget.config Sds.Storage.Blob.WebApi/Sds.Storage.Blob.WebApi.csproj

RUN dotnet publish Sds.Storage.Blob.WebApi/Sds.Storage.Blob.WebApi.csproj -r $RID -o /dist

# Build runtime image
FROM microsoft/dotnet:2.1-runtime-deps

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

COPY --from=build-env /dist ./

ENV ASPNETCORE_URLS http://+:18006
EXPOSE 18006

ENTRYPOINT ["./Sds.Storage.Blob.WebApi"]