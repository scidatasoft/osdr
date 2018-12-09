# Use SDK image to build solution
FROM microsoft/dotnet:2.1-sdk AS build-env

ARG RID=linux-x64

WORKDIR /build

COPY . .

RUN dotnet restore --configfile /build/Nuget.config Sds.MetadataStorage.Processing/Sds.MetadataStorage.Processing.csproj

RUN dotnet publish Sds.MetadataStorage.Processing/Sds.MetadataStorage.Processing.csproj -r $RID -o /dist

# Build runtime image
FROM microsoft/dotnet:2.1-runtime-deps

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

RUN apt-get update && apt-get install -y curl
RUN curl https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh > /app/wait-for-it.sh && chmod 777 /app/wait-for-it.sh

COPY --from=build-env /dist ./

ENTRYPOINT ["./Sds.MetadataStorage.Processing"]
