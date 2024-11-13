# Utiliser l'image Elasticsearch officielle
FROM docker.elastic.co/elasticsearch/elasticsearch:8.10.2

# # Lance le script pour utiliser le fielddata via le script
# COPY /init/elasticsearch-init.sh /usr/share/elasticsearch/init/

RUN chown elasticsearch:elasticsearch /usr/share/elasticsearch/config/elasticsearch.yml

# Copie les fichiers de configuration de l'index
# COPY /config/custom_analyzer.json /usr/share/elasticsearch/config/custom_analyzer.json
COPY /config/create_index.json /usr/share/elasticsearch/config/create_index.json
COPY /config/notes_index_template.json /usr/share/elasticsearch/config/notes_index_template.json
COPY /config/elasticsearch.yml /usr/share/elasticsearch/config/config/elasticsearch.yml

COPY load_template.sh /usr/local/bin/load_template.sh
RUN chmod +x /usr/local/bin/load_template.sh

# Copier le script de configuration
COPY setup_pipeline.sh /usr/share/elasticsearch/setup_pipeline.sh
RUN chmod +x /usr/share/elasticsearch/setup_pipeline.sh

# Lancer le script après le démarrage d'Elasticsearch
CMD ["sh", "-c", "/usr/local/bin/docker-entrypoint.sh && /usr/share/elasticsearch/setup_pipeline.sh"]

# Configuration de l'environnement
ENV discovery.type=single-node
ENV xpack.security.enabled=false
ENV ES_JAVA_OPTS=-Xms512m
ENV ingest.geoip.downloader.enabled=false
ENV ingest.geoip.enabled=false

# Exposer les ports
EXPOSE 9200 9300