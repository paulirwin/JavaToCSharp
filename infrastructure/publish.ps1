param([Parameter(Mandatory)] $version);

# Requires the dotnet-setversion tool installed:
#   dotnet tool install -g dotnet-setversion
setversion $version ../JavaToCSharp/JavaToCSharp.csproj
setversion $version ../JavaToCSharpCli/JavaToCSharpCli.csproj
setversion $version ../JavaToCSharpGui/JavaToCSharpGui.csproj

dotnet publish ../JavaToCSharpGui/JavaToCSharpGui.csproj -c Release -r win10-x64 --self-contained true -o ../publish/gui/
dotnet publish ../JavaToCSharpCli/JavaToCSharpCli.csproj -c Release -r win10-x64 --self-contained true -o ../publish/cli/

Compress-Archive @("../publish/gui/", "../publish/cli/") -DestinationPath ../publish/JavaToCSharp-$version.zip -Force

dotnet pack ../JavaToCSharp/JavaToCSharp.csproj -c Release