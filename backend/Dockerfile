FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/ ./backend/
RUN dotnet restore backend/BankStatementConverter.API
RUN dotnet publish backend/BankStatementConverter.API -c Release --no-restore -o /app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
RUN apt-get update && apt-get install -y --no-install-recommends poppler-utils tesseract-ocr tesseract-ocr-fra tesseract-ocr-eng && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /app/private-statements && chown app:app /app/private-statements
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BankStatementConverter.API.dll"]

