# Utiliser l'image Elasticsearch officielle
FROM docker.elastic.co/elasticsearch/elasticsearch:8.10.2

# Configuration de l'environnement
ENV discovery.type=single-node
ENV xpack.security.enabled=false
ENV ES_JAVA_OPTS=-Xms512m
ENV ingest.geoip.downloader.enabled=false
ENV ingest.geoip.enabled=false

# Exposer les ports
EXPOSE 9200 9300
