FROM node:22-bookworm-slim AS ocr
WORKDIR /ocr
COPY backend/ocr/package*.json ./
RUN npm ci --omit=dev
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/ ./backend/
RUN dotnet restore backend/BankStatementConverter.API
RUN dotnet publish backend/BankStatementConverter.API -c Release --no-restore -o /app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
COPY --from=ocr /usr/local/bin/node /usr/local/bin/node
COPY --from=ocr /ocr/node_modules /app/ocr/node_modules
RUN mkdir -p /app/uploads && chown -R app:app /app/uploads
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BankStatementConverter.API.dll"]
