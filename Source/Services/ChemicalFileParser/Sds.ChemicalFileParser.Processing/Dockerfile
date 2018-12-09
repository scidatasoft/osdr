FROM docker.your-company.com/dotnet-framework-with-netcore:latest

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

COPY . .

RUN dotnet restore --configfile Nuget.config ChemicalFileParser.sln

RUN dotnet build ChemicalFileParser.sln

ENTRYPOINT ["./Sds.ChemicalFileParser.Processing/bin/Debug/Sds.ChemicalFileParser.Processing.exe"]
