########################################
# 1) Build your app
########################################
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# 1a) Copy only the csproj into the container, from the sub‑folder
COPY BlogBackend/BlogBackend.csproj ./BlogBackend.csproj

# 1b) Restore that project
RUN dotnet restore ./BlogBackend.csproj

# 2) Copy the rest of the files and publish
COPY . .
WORKDIR /src/BlogBackend
RUN dotnet publish -c Release -o /app/publish

########################################
# 3) Run your app
########################################
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "BlogBackend.dll"]
