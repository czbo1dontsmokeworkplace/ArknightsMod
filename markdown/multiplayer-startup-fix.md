# 多人服务器启动失败修复（2026-09-20）

## 已复现的原因

只启用 ArknightsMod 1.3.2.3 和 InnoVault 1.9.101，在 tModLoader 2026.7.3.0
独立服务端加载当前源码，发生以下错误，随后 tModLoader 自动禁用 ArknightsMod：

```text
System.TypeInitializationException: The type initializer for
'ArknightsMod.Content.SwingHelper.SwingHelper' threw an exception.
 ---> System.NullReferenceException
 at ArknightsMod.Content.SwingHelper.SwingHelper..cctor() [SwingHelper.cs:37]
 at ArknightsMod.Content.SwingHelper.EffectLoad.Load() [EffectLoad.cs:16]
```

`EffectLoad.Load()` 在服务端执行着色器初始化，触发 `SwingHelper` 的静态字段初始化。
该字段直接读取 `TextureAssets.Extra[98].Value`，但独立服务端没有初始化这张客户端贴图。
“开服并加入”也需要运行服务端，因此这类加载异常会阻止多人房间正常启动。

## 修改

- `SwingHelper.lightTex` 改为按需读取，服务端返回 null，避免挥舞逻辑触发图形资源的静态初始化。
- `EffectLoad.Load()` 在访问 SwingHelper 和着色器之前检查 `Main.dedServ`。
- 主模组的天空、滤镜和着色器初始化统一使用 `Main.dedServ` 隔离客户端。
- `ElementalUI.Load()` 在服务端跳过 UI 创建。

## 验证

测试文件位于忽略提交的 `obj/multiplayer-validation/`，使用独立模组目录和新建测试世界。
未覆盖玩家现有世界或已安装模组。

1. 修复前使用当前源码编译、打包并启动，复现上述异常；日志为 `baseline-server.log`。
2. 修复后完整编译通过：0 错误、271 个既有警告。
   本机项目配置指向的 `../ModAssemblies/InnoVault.dll` 不存在；验证时从已安装的
   InnoVault 1.9.101 包提取程序集，通过 `obj/MultiplayerValidation.targets` 临时指定引用。
3. 修复后打包成功；修复前后打包均有 `Image loading failed: unknown image type` 警告，
   不将其视为客户端资源验证通过。
4. 修复后服务端保持两个模组启用，完成内容加载、创建并加载小世界，输出
   `Listening on port 7777` 和 `Server started`。日志为 `fixed-server-console.log`
   及 `tModLoader-Logs/server.log`，未再出现上述类型初始化异常或天空纹理加载错误。
5. Python socket 成功连接 `127.0.0.1:7777`，随后结束测试服务器。

本次覆盖服务端启动和 TCP 可连接性；尚未验证两名真实客户端加入、Steam 邀请及战斗同步。
发布前应让主机与加入者使用同一修复版本，复测“开服并加入”和独立服务器进服。
