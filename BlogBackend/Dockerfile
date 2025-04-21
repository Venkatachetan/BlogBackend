# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Copy the published output to the runtime image
COPY --from=build /app/publish .

# Expose the port (adjust if your app listens on a different port)
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "BlogBackend.dll"]
