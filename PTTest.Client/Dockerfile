FROM node:18-alpine as build

WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx webpack

# We just need the artifacts, so only copy the /dist folder

FROM scratch as output
COPY --from=build /app/dist /dist