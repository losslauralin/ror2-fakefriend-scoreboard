{pkgs, ...}: {
  # dotnet SDK 用来 build 插件；ilspycmd 用来读游戏程序集（备用）；
  # python3 给 scripts/package.sh、scripts/make-icon.py 用。
  languages.dotnet.enable = true;
  packages = [pkgs.ilspycmd pkgs.python3];
}
