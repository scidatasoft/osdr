# Use SDK image to build solution
FROM microsoft/dotnet:2.1-sdk AS build-env

ARG RID=linux-x64

WORKDIR /build

COPY . .

RUN dotnet restore --configfile /build/Nuget.config Sds.Imaging.WebApi/Sds.Imaging.WebApi.csproj

RUN dotnet build Sds.Imaging.WebApi/Sds.Imaging.WebApi.csproj

RUN dotnet publish Sds.Imaging.WebApi/Sds.Imaging.WebApi.csproj -r $RID -o /dist

RUN cp -f Sds.Imaging.WebApi/appsettings.json /dist

# Build runtime image
FROM microsoft/dotnet:2.1-runtime-deps

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

COPY --from=build-env /dist ./

ENV ASPNETCORE_URLS http://+:18006
EXPOSE 18006

ENTRYPOINT ["./Sds.Imaging.WebApi"]
