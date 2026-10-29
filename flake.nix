{
  description = "RatioMaster.NET - BitTorrent tracker simulator";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs { inherit system; };

        runtimeLibs = with pkgs; [
          fontconfig
          libx11
          libice
          libsm
        ];
      in
      {
        devShells.default = pkgs.mkShell {
          buildInputs = [
            pkgs.dotnetCorePackages.sdk_10_0
          ]
          ++ runtimeLibs;

          LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath runtimeLibs;
        };

        packages.default = pkgs.buildDotnetModule {
          pname = "ratiomaster-net";
          version = "1.3.1";

          src = ./.;

          projectFile = "src/RatioMaster.App/RatioMaster.App.csproj";
          nugetDeps = ./deps.json;

          dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
          dotnet-runtime = pkgs.dotnetCorePackages.runtime_10_0;

          selfContainedBuild = false;

          nativeBuildInputs = [ pkgs.copyDesktopItems ];

          desktopItems = [
            (pkgs.makeDesktopItem {
              name = "RatioMaster.NET";
              desktopName = "RatioMaster.NET";
              exec = "RatioMaster.NET";
              icon = "ratiomaster-net";
              comment = "BitTorrent tracker simulator";
              categories = [ "Network" ];
            })
          ];

          dotnetFlags = [
            "-p:PublishSingleFile=false"
            "-p:EnableCompressionInSingleFile=false"
            "-p:PublishReadyToRun=false"
            "-p:PublishTrimmed=false"
            "-p:PublishAot=false"
          ];

          executables = [ "RatioMaster.NET" ];

          runtimeDeps = runtimeLibs;

          postInstall = ''
            install -Dm644 icon.svg $out/share/icons/hicolor/scalable/apps/ratiomaster-net.svg
          '';

          postFixup = ''
            ln -s $out/bin/RatioMaster.NET $out/bin/ratiomaster-net
          '';

          meta.mainProgram = "RatioMaster.NET";
        };
      }
    );
}
