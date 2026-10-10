{
  description = "FakeFriend scoreboard + item inspection for Risk of Rain 2";

  inputs = {
    # devenv 的 flake-parts 模板用这个 input 让 direnv 把真实项目根目录喂给
    # devenv.root（纯求值模式下 devenv 自己问不出 cwd）。没有 derevn 时用
    # `nix develop --no-pure-eval` 走同一条路。
    devenv-root = {
      url = "file+file:///dev/null";
      flake = false;
    };
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    flake-parts.inputs.nixpkgs-lib.follows = "nixpkgs";
    devenv.url = "github:cachix/devenv";
    nix2container.url = "github:nlewo/nix2container";
    nix2container.inputs.nixpkgs.follows = "nixpkgs";
    mk-shell-bin.url = "github:rrbutani/nix-mk-shell-bin";
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
