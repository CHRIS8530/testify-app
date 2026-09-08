FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
EXPOSE 5173
ENV VITE_API_URL=http://localhost:5000/api/v1
CMD ["npm", "run", "dev", "--", "--host"]