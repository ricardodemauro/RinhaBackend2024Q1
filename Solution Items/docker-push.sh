tag=$(date +"%Y%m%d%H%M")

docker buildx build --platform linux/amd64 -t rinha-2024q1-dotnet-postgresql:$tag -t rinha-2024q1-dotnet-postgresql:latest .

#docker build -t rinha-2024q1-dotnet:$tag -t rinha-2024q1-dotnet-postgresql:latest .
docker push rinha-2024q1-dotnet-postgresql:$tag
docker push rinha-2024q1-dotnet-postgresql:latest

echo $tag