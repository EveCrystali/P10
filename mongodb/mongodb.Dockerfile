# Utilise l'image officielle MongoDB
FROM mongo:latest

# Installer dockerize pour attendre le démarrage de MongoDB
RUN apt-get update && apt-get install -y wget \
    && wget https://github.com/jwilder/dockerize/releases/download/v0.6.1/dockerize-linux-amd64-v0.6.1.tar.gz \
    && tar -C /usr/local/bin -xzf dockerize-linux-amd64-v0.6.1.tar.gz


# Crée un répertoire pour stocker les fichiers de sauvegarde
RUN mkdir -p /backup

# Copie le script de sauvegarde
COPY /backup/restore.sh /backup/restore.sh

# S'assurer que mongosh est disponible
RUN apt-get update && apt-get install -y mongodb-mongosh

# Donner les permissions d'exécution au script
RUN chmod +x /backup/restore.sh

# Exécute le script de restauration au démarrage du conteneur
CMD ["dockerize", "-wait", "tcp://mongodb_container:27017", "-timeout", "5s", "/backup/restore.sh"]
