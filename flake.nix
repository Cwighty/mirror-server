{
  description = "Magic Mirror C# Server Flake";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
  };

  outputs = {
    self,
    nixpkgs,
  }: let
    # Support both Linux and Darwin systems
    supportedSystems = ["x86_64-linux" "aarch64-darwin" "x86_64-darwin"];
    forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
    pkgsForSystem = system: import nixpkgs {inherit system;};
  in {
    packages = forAllSystems (system: let
      pkgs = pkgsForSystem system;
    in {
      default = let
        server = pkgs.buildDotnetModule {
          pname = "magic-mirror-server";
          version = "1.0.0";
          src = ./.;
          projectFile = "src/MagicMirror/MagicMirror.csproj";

          dotnet-sdk = pkgs.dotnetCorePackages.sdk_9_0;
          dotnet-runtime = pkgs.dotnetCorePackages.aspnetcore_9_0;

          # Build configuration
          selfContainedBuild = true;
          useAppHost = true;

          # NuGet configuration
          dotnetFlags = ["--source" "https://api.nuget.org/v3/index.json"];
          __noChroot = true;

          # Copy static files
          postInstall = ''
            mkdir -p $out/lib/magic-mirror-server/wwwroot
            cp -r $src/src/MagicMirror/wwwroot/* $out/lib/magic-mirror-server/wwwroot/
            cp $src/src/MagicMirror/appsettings*.json $out/lib/magic-mirror-server/
          '';
        };
      in
        pkgs.writeScriptBin "magic-mirror-server" ''
          #!${pkgs.bash}/bin/bash
          export ASPNETCORE_ENVIRONMENT=Production
          export ASPNETCORE_CONTENTROOT="${server}/lib/magic-mirror-server"
          export ASPNETCORE_WEBROOT="${server}/lib/magic-mirror-server/wwwroot"
          export ASPNETCORE_URLS="http://localhost:5001"

          exec ${server}/bin/MagicMirror "$@"
        '';
    });

    apps = forAllSystems (system: {
      default = {
        type = "app";
        program = "${self.packages.${system}.default}/bin/magic-mirror-server";
      };
    });

    nixosModules.default = {
      config,
      lib,
      pkgs,
      ...
    }: let
      cfg = config.services.magic-mirror-server;
    in {
      options.services.magic-mirror-server = with lib; {
        enable = mkEnableOption "Magic Mirror Server";

        port = mkOption {
          type = types.port;
          default = 5001;
          description = "Port to listen on";
        };

        openAIApiKey = mkOption {
          type = types.str;
          description = "OpenAI API Key";
        };
      };

      config = lib.mkIf cfg.enable {
        systemd.services.magic-mirror-server = {
          description = "Magic Mirror Server";
          wantedBy = ["multi-user.target"];
          after = ["network.target"];

          serviceConfig = {
            ExecStart = "${self.packages.${pkgs.system}.default}/bin/magic-mirror-server";
            Restart = "always";
            WorkingDirectory = "${self.packages.${pkgs.system}.default}/lib/magic-mirror-server";
          };

          environment = {
            ASPNETCORE_ENVIRONMENT = "Production";
            ASPNETCORE_URLS = "http://0.0.0.0:${toString cfg.port}";
            OPENAI_API_KEY = cfg.openAIApiKey;
          };
        };
      };
    };
  };
}
