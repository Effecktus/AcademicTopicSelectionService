## DirectoryOfGraduates backend
## Multi-stage build for ASP.NET Core 10

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY ["DirectoryOfGraduates.slnx", "./"]
COPY ["src/DirectoryOfGraduates.API/DirectoryOfGraduates.API.csproj", "src/DirectoryOfGraduates.API/"]
COPY ["src/DirectoryOfGraduates.Application/DirectoryOfGraduates.Application.csproj", "src/DirectoryOfGraduates.Application/"]
COPY ["src/DirectoryOfGraduates.Infrastructure/DirectoryOfGraduates.Infrastructure.csproj", "src/DirectoryOfGraduates.Infrastructure/"]
COPY ["src/DirectoryOfGraduates.Shared/DirectoryOfGraduates.Shared.csproj", "src/DirectoryOfGraduates.Shared/"]

RUN dotnet restore "src/DirectoryOfGraduates.API/DirectoryOfGraduates.API.csproj"

# Copy the rest of the source
COPY . .

RUN dotnet publish "src/DirectoryOfGraduates.API/DirectoryOfGraduates.API.csproj" \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DirectoryOfGraduates.API.dll"]
