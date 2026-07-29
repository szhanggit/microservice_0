# syntax=docker/dockerfile:1

# Unlike the .NET services, this is a plain npm project with no dependency on
# anything outside its own folder, so the build context is this directory
# (see docker-compose.yml at the repo root), not the repo root.

FROM node:22-alpine AS build
WORKDIR /src

# Copy manifests first so `npm ci` is cached until a dependency actually changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

# Static output only from here - nginx serves it, no Node.js at runtime.
FROM nginx:1.27-alpine AS final

# wget (from busybox) is used only by the HEALTHCHECK below; nginx:alpine has no curl.
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/UserManagementWeb/browser /usr/share/nginx/html

# Matches the 8080 convention the .NET services in this repo use, so every
# container in docker-compose.yml exposes the same port number.
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=10s --retries=5 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/ || exit 1
