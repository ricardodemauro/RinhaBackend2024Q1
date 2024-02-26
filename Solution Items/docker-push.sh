tag=$(date +"%Y%m%d%H%M")

docker buildx build --platform linux/amd64 -t rinha-2024q1-dotnet:$tag -t ricardomauro/rinha-2024q1-dotnet:latest .

#docker build -t rinha-2024q1-dotnet:$tag -t ricardomauro/rinha-2024q1-dotnet:latest .
docker push ricardomauro/rinha-2024q1-dotnet:$tag
docker push ricardomauro/rinha-2024q1-dotnet:latest

echo $tag