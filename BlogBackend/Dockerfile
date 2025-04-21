########################################
# 1) Build your app using .NET 8 SDK
########################################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file from the sub‑folder and restore
COPY BlogBackend/BlogBackend.csproj ./BlogBackend.csproj
RUN dotnet restore ./BlogBackend.csproj

# Copy everything else, then publish
COPY . .
WORKDIR /src/BlogBackend
RUN dotnet publish -c Release -o /app/publish

########################################
# 2) Run your app on .NET 8 ASP.NET runtime
########################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80

ENTRYPOINT ["dotnet", "BlogBackend.dll"]
