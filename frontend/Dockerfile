# Stage 1: build the static site. Node and the source only exist in this stage.
FROM node:22-alpine AS builder
WORKDIR /app

# Install first, so this step is cached until the lockfile changes. npm ci installs exactly what the lockfile pins.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite bakes VITE_API_URL into the bundle at build time, so it is a build argument with no default.
ARG VITE_API_URL
RUN test -n "$VITE_API_URL" || (echo "VITE_API_URL build argument is required" && exit 1)
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# Stage 2: serve the files with nginx. No Node and no source in the final image.
# This image runs as a non-root user (uid 101) and listens on 8080.
FROM nginxinc/nginx-unprivileged:stable-alpine
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=builder /app/dist /usr/share/nginx/html
EXPOSE 8080