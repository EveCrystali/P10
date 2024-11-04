FROM docker.elastic.co/logstash/logstash:8.5.1

# Passer à l'utilisateur root pour exécuter les commandes d'installation
USER root

# Installation du plugin MongoDB pour Logstash
RUN logstash-plugin install logstash-input-mongodb

# Installer netbase pour obtenir /etc/protocols
RUN apt-get update && \
    apt-get install -y netbase && \
    rm -rf /var/lib/apt/lists/*

# Crée le répertoire pour les fichiers de base de données SQLite
RUN mkdir -p /usr/share/logstash/pipeline/db && chmod 777 /usr/share/logstash/pipeline/db

COPY .pipeline/logstash.conf /usr/share/logstash/pipeline/logstash.conf

# Revenir à l'utilisateur logstash
USER logstash

ENV xpack.monitoring.enabled=false
ENV ELASTICSEARCH_HOST=http://elasticsearch:9200
ENV discovery.type=single-node
ENV xpack.security.enabled=false
ENV xpack.security.transport.ssl.enabled=false
ENV ES_JAVA_OPTS="-Xms512m -Xmx512m"