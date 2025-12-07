FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend.csproj ./
RUN dotnet restore

COPY . .

# 🔥 แก้ใหม่: ให้ grep หาคำว่า Timestamp โดยเฉพาะ (ถ้าเจอจะปริ้นท์บรรทัดนั้นออกมา)
RUN echo "=== SEARCHING FOR TIMESTAMP ==="
RUN grep "Timestamp" Models/NetworkMetric.cs || echo "NOT FOUND!"
RUN echo "==============================="

RUN dotnet publish -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "backend.dll"]