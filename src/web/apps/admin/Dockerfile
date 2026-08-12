# Build stage. Context is the pnpm workspace root (src/web).
FROM node:20-alpine AS build
WORKDIR /app

# pnpm is not bundled reliably via corepack shims; install it explicitly.
RUN npm install -g pnpm@9

# Copy the whole workspace (node_modules/dist excluded via .dockerignore)
# so pnpm resolves the monorepo against the committed lockfile.
COPY . .

RUN pnpm install --frozen-lockfile
RUN pnpm --filter @fms/admin build

# Serve stage
FROM nginx:alpine
COPY --from=build /app/apps/admin/dist /usr/share/nginx/html
EXPOSE 80
