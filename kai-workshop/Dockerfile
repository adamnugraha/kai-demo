FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props ./
COPY src/RailBooking.Core/ src/RailBooking.Core/
COPY src/RailBooking.Modern/ src/RailBooking.Modern/
RUN dotnet publish src/RailBooking.Modern -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet","RailBooking.Modern.dll"]
