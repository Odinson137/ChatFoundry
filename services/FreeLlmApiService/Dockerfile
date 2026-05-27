# Этап сборки
FROM node:20-alpine AS builder
WORKDIR /app

# Копируем package файлы для npm workspaces
COPY package*.json ./
COPY shared/package.json ./shared/
COPY server/package.json ./server/
COPY client/package.json ./client/

# Устанавливаем зависимости
RUN npm ci

# Копируем исходный код
COPY . .

# Собираем сервер и клиент
RUN npm run build

# Этап выполнения
FROM node:20-alpine
WORKDIR /app

COPY package*.json ./
COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/shared ./shared
COPY --from=builder /app/server/dist ./server/dist
COPY --from=builder /app/server/package.json ./server/package.json
COPY --from=builder /app/client/dist ./client/dist

ENV PORT=3001
ENV NODE_ENV=production

EXPOSE 3001

CMD ["node", "server/dist/index.js"]
