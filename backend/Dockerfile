# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore trước để tận dụng layer cache
COPY PhoneStore.sln ./
COPY src/PhoneStore.Domain/PhoneStore.Domain.csproj src/PhoneStore.Domain/
COPY src/PhoneStore.Application/PhoneStore.Application.csproj src/PhoneStore.Application/
COPY src/PhoneStore.Infrastructure/PhoneStore.Infrastructure.csproj src/PhoneStore.Infrastructure/
COPY src/PhoneStore.API/PhoneStore.API.csproj src/PhoneStore.API/
RUN dotnet restore src/PhoneStore.API/PhoneStore.API.csproj

# Copy toàn bộ source và publish
COPY . .
RUN dotnet publish src/PhoneStore.API/PhoneStore.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PhoneStore.API.dll"]
