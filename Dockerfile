FROM microsoft/dotnet:latest

COPY ./app /app

WORKDIR /app

RUN ["dotnet", "restore"]

RUN ["dotnet", "build"]

EXPOSE 8085

ENTRYPOINT ["dotnet", "run", "-p", "/app/Xyzies.Devices.API/" ]

