# Utilise l'image officielle MongoDB
FROM mongo:8.0.3

# Installer dockerize pour attendre le démarrage de MongoDB
RUN apt-get update && apt-get install -y wget \
    && wget https://github.com/jwilder/dockerize/releases/download/v0.6.1/dockerize-linux-amd64-v0.6.1.tar.gz \
    && tar -C /usr/local/bin -xzf dockerize-linux-amd64-v0.6.1.tar.gz

# S'assurer que mongosh est disponible
RUN apt-get update && apt-get install -y mongodb-mongosh

