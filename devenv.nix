{pkgs, ...}: {
  # dotnet SDK 用来 build 插件；ilspycmd 用来读游戏和 FakeFriend 的程序集。
  languages.dotnet.enable = true;
  packages = [pkgs.ilspycmd];
}
