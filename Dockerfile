# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy only .csproj file first for better caching
COPY ["CheckOut.csproj", "./"]

# Restore dependencies
RUN dotnet restore "CheckOut.csproj"

# Copy the actual source code
COPY . .

# Build and publish
RUN dotnet publish "CheckOut.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install cultures for Alpine (fixes common localization errors in .NET)
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy the build artifacts from the previous stage
COPY --from=build /app/publish .

# Use a non-root user for security (best practice)
USER $APP_UID

ENTRYPOINT ["dotnet", "CheckOut.dll"]
