FROM mongo-express

COPY /wait-for-mongo.sh /wait-for-mongo.sh

RUN chmod +x /wait-for-mongo.sh
