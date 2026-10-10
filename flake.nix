{
  description = "FakeFriend scoreboard + item inspection for Risk of Rain 2";

  # 官方 flake-parts 模板的 nixpkgs 与 cache：改成 nixos-unstable 的话，
  # devenv CLI 的派生物哈希对不上 devenv.cachix.org，会在本地重编 Rust。
  inputs = {
    devenv-root = {
      url = "file+file:///dev/null";
      flake = false;
    };
    nixpkgs.url = "github:cachix/devenv-nixpkgs/rolling";
    flake-parts.url = "github:hercules-ci/flake-parts";
    flake-parts.inputs.nixpkgs-lib.follows = "nixpkgs";
    devenv.url = "github:cachix/devenv";
    nix2container.url = "github:nlewo/nix2container";
    nix2container.inputs.nixpkgs.follows = "nixpkgs";
    mk-shell-bin.url = "github:rrbutani/nix-mk-shell-bin";
  };

  nixConfig = {
    extra-trusted-public-keys = "devenv.cachix.org-1:w1cLUi8dv3hnoSPGAuibQv+f9TZLr6cv/Hm9XgU50cw=";
    extra-substituters = "https://devenv.cachix.org";
  };

  outputs = inputs @ {flake-parts, ...}:
    flake-parts.lib.mkFlake {inherit inputs;} {
      imports = [inputs.devenv.flakeModule];
      systems = ["x86_64-linux"];

      perSystem = {pkgs, ...}: {
        devenv.shells.default = {
          name = "ror2-fakefriend-scoreboard";
          imports = [./devenv.nix];
        };
      };
    };
}
