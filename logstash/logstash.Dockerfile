FROM docker.elastic.co/logstash/logstash:8.5.1

# Copie du fichier de configuration de Logstash
COPY logstash.conf /usr/share/logstash/pipeline/logstash.conf

# Installation du plugin MongoDB pour Logstash
RUN logstash-plugin install logstash-input-mongodb

# Crée le répertoire pour les fichiers de base de données SQLite
RUN mkdir -p /usr/share/logstash/pipeline/db

ENV xpack.monitoring.enabled=false
ENV ELASTICSEARCH_HOST=http://elasticsearch:9200
ENV discovery.type=single-node
ENV xpack.security.enabled=false
ENV xpack.security.transport.ssl.enabled=false
ENV ES_JAVA_OPTS="-Xms512m -Xmx512m"