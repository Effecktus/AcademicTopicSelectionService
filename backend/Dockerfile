## AcademicTopicSelectionService backend
## Multi-stage build for ASP.NET Core 10

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY ["AcademicTopicSelectionService.slnx", "./"]
COPY ["src/AcademicTopicSelectionService.API/AcademicTopicSelectionService.API.csproj", "src/AcademicTopicSelectionService.API/"]
COPY ["src/AcademicTopicSelectionService.Application/AcademicTopicSelectionService.Application.csproj", "src/AcademicTopicSelectionService.Application/"]
COPY ["src/AcademicTopicSelectionService.Infrastructure/AcademicTopicSelectionService.Infrastructure.csproj", "src/AcademicTopicSelectionService.Infrastructure/"]
RUN dotnet restore "src/AcademicTopicSelectionService.API/AcademicTopicSelectionService.API.csproj"

# Copy the rest of the source
COPY . .

RUN dotnet publish "src/AcademicTopicSelectionService.API/AcademicTopicSelectionService.API.csproj" \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AcademicTopicSelectionService.API.dll"]
