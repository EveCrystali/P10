#!/bin/sh

echo "Starting Elasticsearch template initialization..."

# TEMPLATE_PATH="/usr/share/elasticsearch/config/notes_index_template.json"

# if [ ! -f "$TEMPLATE_PATH" ]; then
#   echo "Error: Template file not found at $TEMPLATE_PATH"
#   exit 1
# fi

echo "Loading template 'notes_index_template'..."
curl -X PUT "http://elasticsearch:9200/_template/notes_index_template" \
  -H "Content-Type: application/json" \
  -d @"$TEMPLATE_PATH"

if [ $? -eq 0 ]; then
  echo "Template 'notes_index_template' loaded successfully."
else
  echo "Error: Failed to load template 'notes_index_template'."
  exit 1
fi

echo "Elasticsearch template initialization completed."
