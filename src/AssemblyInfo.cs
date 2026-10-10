using System.Security.Permissions;

// 官方 wiki 要求：这一行让 Mono 跳过程序集可见性校验，从而能在运行时使用
// RiskOfRain2.GameLibs 里被 publicize 出来的 private / internal 成员
// （我们要用 ScoreboardController 的私有 Rebuild / stripAllocator）。
// https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Assembly-References/
#pragma warning disable SYSLIB0003, CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore SYSLIB0003
