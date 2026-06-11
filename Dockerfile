# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PGKing.sln", "./"]
COPY ["PGKing.UI/PGKing.UI.csproj", "PGKing.UI/"]
COPY ["PGKing.Infrastructure/PGKing.Infrastructure.csproj", "PGKing.Infrastructure/"]
COPY ["PGKing.Application/PGKing.Application.csproj", "PGKing.Application/"]

# Restore dependencies
RUN dotnet restore

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet publish "PGKing.UI/PGKing.UI.csproj" -c Release -o /app/publish

# Use the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Enable globalization invariant mode to prevent crashes on some Linux distros
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV ASPNETCORE_URLS=http://+:80

# Set the entry point
ENTRYPOINT ["dotnet", "PGKing.UI.dll"]


