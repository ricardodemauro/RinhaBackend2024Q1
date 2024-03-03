tag=$(date +"%Y%m%d%H%M")

docker buildx build --platform linux/amd64 -t ricardomauro/rinha-2024q1-dotnet-yarp:$tag -t ricardomauro/rinha-2024q1-dotnet-yarp:latest .

#docker build -t rinha-2024q1-dotnet:$tag -t ricardomauro/rinha-2024q1-dotnet-postgresql:latest .
docker push ricardomauro/rinha-2024q1-dotnet-yarp:$tag
docker push ricardomauro/rinha-2024q1-dotnet-yarp:latest

echo $tag