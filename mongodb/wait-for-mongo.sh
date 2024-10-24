#!/bin/sh

>&2 echo "MongoDB is up - executing command"
exec "$@"
