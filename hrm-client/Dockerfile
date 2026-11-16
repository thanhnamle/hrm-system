# HRM web image: the Angular app, served by Nginx, which also forwards /api to the API container.
#   docker build -t hrm-web hrm-client

# ---- build: compile the Angular app ----------------------------------------------------------
FROM node:24-alpine AS build
WORKDIR /app

# Install from the lock file first, so the dependency layer is cached until package*.json changes.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npm run build

# ---- runtime: static files + reverse proxy ---------------------------------------------------
FROM nginx:1.30-alpine AS runtime
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist/hrm-client/browser /usr/share/nginx/html

EXPOSE 80
