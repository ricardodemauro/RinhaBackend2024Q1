tag=$(date +"%Y%m%d%H%M")

docker buildx build --platform linux/amd64 -t ricardomauro/rinha-2024q1-dotnet-postgresql:$tag -t ricardomauro/rinha-2024q1-dotnet-postgresql:latest .

#docker build -t rinha-2024q1-dotnet:$tag -t ricardomauro/rinha-2024q1-dotnet-postgresql:latest .
docker push ricardomauro/rinha-2024q1-dotnet-postgresql:$tag
docker push ricardomauro/rinha-2024q1-dotnet-postgresql:latest

echo $tag