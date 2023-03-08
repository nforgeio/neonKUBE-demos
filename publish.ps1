param(
  [string]$clusterId
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

$mypath = $MyInvocation.MyCommand.Path
$myDir = Split-Path $mypath -Parent
$DEMO_DIR=$myDir

cd $DEMO_DIR\Services\hello-world-operator
dotnet publish hello-world-operator.csproj -c Release -o .\bin\Release\7.0\publish
unix-text docker-entrypoint.sh
docker build -t neon-registry.$clusterDomain/library/hello-world-operator .
docker push neon-registry.$clusterDomain/library/hello-world-operator

if (-not $?)
{
    throw "ERROR: Publish hello-world-operator failed."
}

cd $DEMO_DIR\Services\hello-world
dotnet publish hello-world.csproj -c Release -o .\bin\Release\7.0\publish
unix-text docker-entrypoint.sh
docker build -t neon-registry.$clusterDomain/library/hello-world .
docker push neon-registry.$clusterDomain/library/hello-world

if (-not $?)
{
    throw "ERROR: Publish hello-world failed."
}

cd $DEMO_DIR\Services\load-generator
dotnet publish load-generator.csproj -c Release -o .\bin\Release\7.0\publish
unix-text docker-entrypoint.sh
docker build -t neon-registry.$clusterDomain/library/load-generator .
docker push neon-registry.$clusterDomain/library/load-generator

if (-not $?)
{
    throw "ERROR: Publish load-generator failed."
}