# 1. Use the .NET SDK to build all projects
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and every single project file to cache dependencies
COPY Roblox.sln ./
COPY Roblox.Website/Roblox.Website.csproj ./Roblox.Website/
COPY Roblox.Libraries/Roblox.Libraries.csproj ./Roblox.Libraries/
COPY Roblox.Dto/Roblox.Dto.csproj ./Roblox.Dto/
COPY Roblox.Models/Roblox.Models.csproj ./Roblox.Models/
COPY Roblox.Services/Roblox.Services.csproj ./Roblox.Services/
COPY Roblox.Rendering/Roblox.Rendering.csproj ./Roblox.Rendering/
COPY Roblox.Configuration/Roblox.Configuration.csproj ./Roblox.Configuration/
COPY Roblox.Logging/Roblox.Logging.csproj ./Roblox.Logging/
COPY Roblox.Exceptions/Roblox.Exceptions.csproj ./Roblox.Exceptions/
COPY Roblox.AbuseDetection/Roblox.AbuseDetection.csproj ./Roblox.AbuseDetection/
COPY Roblox.Metrics/Roblox.Metrics.csproj ./Roblox.Metrics/
COPY Roblox.EconomyChat/Roblox.EconomyChat.csproj ./Roblox.EconomyChat/
COPY Roblox.Cache/Roblox.Cache.csproj ./Roblox.Cache/

# Copy unit tests so the solution context is intact
COPY Roblox.Libraries.UnitTest/Roblox.Libraries.UnitTest.csproj ./Roblox.Libraries.UnitTest/
COPY Roblox.Rendering.Test/Roblox.Rendering.Test.csproj ./Roblox.Rendering.Test/
COPY Roblox.IntegrationTest/Roblox.IntegrationTest.csproj ./Roblox.IntegrationTest/
COPY Roblox.UnitTest/Roblox.UnitTest.csproj ./Roblox.UnitTest/

# Restore all projects 
RUN dotnet restore

# Copy all source files over
COPY . .

# Compile and publish specifically the Website project
WORKDIR "/src/Roblox.Website"
RUN dotnet publish "Roblox.Website.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Use the ASP.NET runtime image to execute the final built app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose standard web traffic ports
EXPOSE 80
EXPOSE 8080

ENTRYPOINT ["dotnet", "Roblox.Website.dll"]
