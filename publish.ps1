param(
  [string]$clusterId,
  [string]$registry,
  [switch]$build = $false
)

if ($clusterId)
{
  $clusterDomain = "$clusterId.neoncluster.io"
}
else
{
  $clusterInfo = neon cluster info | ConvertFrom-Json
  $clusterDomain = $clusterInfo.Domain
}

if (!$registry)
{
  $registry = "neon-registry.$clusterDomain/library"
}

$mypath = $MyInvocation.MyCommand.Path
$myDir = Split-Path $mypath -Parent
$DEMO_DIR=$myDir

if ($build)
{
  cd $DEMO_DIR\Services\hello-world-operator
  dotnet publish hello-world-operator.csproj -c Release -o .\bin\Release\7.0\publish
  unix-text docker-entrypoint.sh
  docker build -t $registry/hello-world-operator .
}
else
{
  docker pull ghcr.io/nforgeio/hello-world-operator:latest
  docker tag ghcr.io/nforgeio/hello-world-operator:latest $registry/hello-world-operator:latest
}
docker push $registry/hello-world-operator:latest

if (-not $?)
{
    throw "ERROR: Publish hello-world-operator failed."
}

if ($build)
{
  cd $DEMO_DIR\Services\hello-world
  dotnet publish hello-world.csproj -c Release -o .\bin\Release\7.0\publish
  unix-text docker-entrypoint.sh
  docker build -t $registry/hello-world .
}
else
{
  docker pull ghcr.io/nforgeio/hello-world:latest
  docker tag ghcr.io/nforgeio/hello-world:latest $registry/hello-world:latest
}

docker push $registry/hello-world

if (-not $?)
{
    throw "ERROR: Publish hello-world failed."
}

if ($build)
{
  cd $DEMO_DIR\Services\load-generator
  dotnet publish load-generator.csproj -c Release -o .\bin\Release\7.0\publish
  unix-text docker-entrypoint.sh
  docker build -t $registry/load-generator .
}
else 
{
  docker pull ghcr.io/nforgeio/load-generator:latest
  docker tag ghcr.io/nforgeio/load-generator:latest $registry/load-generator:latest
}

docker push $registry/load-generator

if (-not $?)
{
    throw "ERROR: Publish load-generator failed."
}