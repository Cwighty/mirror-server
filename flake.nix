{
  description = "Magic Mirror C# Server Flake";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
  };

  outputs = { self, nixpkgs }:
  let
    pkgs = import nixpkgs { system = "x86_64-linux"; };
    dotnet-sdk = pkgs.dotnetCorePackages.sdk_9_0;
  in {
    devShells.default = {
      packages = [
        dotnet-sdk
      ];
      shellHook = ''
        echo "Starting development shell for Magic Mirror C# Server"
        echo "Run 'dotnet build' to build the project."
        echo "Run 'dotnet run' to start the server."
      '';
    };

    defaultPackage.x86_64-linux = pkgs.stdenv.mkDerivation {
      name = "magic-mirror-server";
      buildInputs = [ dotnet-sdk ];
      buildScript = ''
        dotnet publish -c Release -o dist
        cp -r dist $out
      '';
    };

    nixosModules.magicMirrorServer = { config, pkgs, ... }: {
      systemd.services.magicMirrorServer = {
        description = "Magic Mirror C# Server";
        after = [ "network.target" ];
        wantedBy = [ "multi-user.target" ];
        serviceConfig = {
          ExecStart = "${self.defaultPackage.x86_64-linux}/MagicMirror.dll";  
          Restart = "always";
          WorkingDirectory = "${self.defaultPackage.x86_64-linux}";
        };
      };
    };
  };
}
