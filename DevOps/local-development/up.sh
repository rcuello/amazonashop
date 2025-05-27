#!/bin/bash

docker-compose up -d

sleep 15 # sleep 10 seconds to give time to docker to finish the setup
echo setup vault configuration
./tools/vault/config.sh
echo setup consul configuration
./tools/consul/config.sh
echo completed

# Básico - elimina contenedores y redes
#docker-compose down

# Elimina también los volúmenes
#docker-compose down -v

# Elimina también las imágenes creadas por el compose
#docker-compose down --rmi all

# Combinado - elimina todo (contenedores, redes, volúmenes e imágenes)
#docker-compose down -v --rmi all

# Solo detener sin eliminar (alternativa más suave)
#docker-compose stop