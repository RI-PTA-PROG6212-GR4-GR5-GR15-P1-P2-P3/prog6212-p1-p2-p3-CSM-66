# ---------- stage 1: build the app ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy the project file on its own first, so the package restore is cached until the packages change
COPY RaceDayMvc.csproj ./
RUN dotnet restore RaceDayMvc.csproj

# then the rest of the source
COPY . ./
RUN dotnet publish RaceDayMvc.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- stage 2: run it ----------
# the runtime image is a lot smaller than the SDK one, so the final image only holds what the app needs
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

# listen on 8080 inside the container
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

# where the API lives. the container reaches the API running on the host through host.docker.internal
ENV Api__BaseUrl=http://host.docker.internal:5163/

EXPOSE 8080

# never run as root
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDayMvc.dll"]
