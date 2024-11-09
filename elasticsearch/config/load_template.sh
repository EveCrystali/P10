#!/bin/bash

# Fonction pour vérifier si Elasticsearch est prêt
is_elasticsearch_ready() {
  curl -s "http://elasticsearch:9200/_cat/health" > /dev/null
  return $?
}

# Attendre qu'Elasticsearch soit prêt
echo "Waiting for Elasticsearch to start..."
RETRY_COUNT=0
MAX_RETRIES=10
while ! is_elasticsearch_ready; do
  RETRY_COUNT=$((RETRY_COUNT+1))
  if [ "$RETRY_COUNT" -ge "$MAX_RETRIES" ]; then
    echo "Elasticsearch did not start in time. Exiting."
    exit 1
  fi
  echo "Elasticsearch is not ready. Retrying in 5 seconds... ($RETRY_COUNT/$MAX_RETRIES)"
  sleep 5
done

# Charger le template
echo "Loading index template..."
curl -X PUT "http://elasticsearch:9200/_index_template/notes_index_template" \
  -H "Content-Type: application/json" \
  -d @/usr/share/elasticsearch/config/notes_index_template.json

if [ $? -eq 0 ]; then
  echo "Template loaded successfully!"
else
  echo "Failed to load template."
  exit 1
fi
