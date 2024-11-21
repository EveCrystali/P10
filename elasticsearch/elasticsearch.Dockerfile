# Utiliser l'image Elasticsearch officielle
FROM docker.elastic.co/elasticsearch/elasticsearch:8.10.2

# # Lance le script pour utiliser le fielddata via le script
# COPY /init/elasticsearch-init.sh /usr/share/elasticsearch/init/

RUN chown elasticsearch:elasticsearch /usr/share/elasticsearch/config/elasticsearch.yml

# Copie les fichiers de configuration de l'index
COPY /config/notes_index_template.json /usr/share/elasticsearch/config/notes_index_template.json
COPY /config/elasticsearch.yml /usr/share/elasticsearch/config/config/elasticsearch.yml


# Lancer le script après le démarrage d'Elasticsearch
CMD ["sh", "-c", "/usr/local/bin/docker-entrypoint.sh"]

# Configuration de l'environnement
ENV discovery.type=single-node
ENV xpack.security.enabled=false
ENV ES_JAVA_OPTS=-Xms512m
ENV ingest.geoip.downloader.enabled=false
ENV ingest.geoip.enabled=false

# Exposer les ports
EXPOSE 9200 9300