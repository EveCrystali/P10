FROM docker.elastic.co/logstash/logstash:8.5.1

# Copie du fichier de configuration de Logstash
COPY logstash.conf /usr/share/logstash/pipeline/logstash.conf

# Installation du plugin MongoDB pour Logstash
RUN logstash-plugin install logstash-input-mongodb_change_streams

# Crée le répertoire pour les fichiers de base de données SQLite
RUN mkdir -p /usr/share/logstash/pipeline/db
