using System.IO.Compression;
using System.Text;
using Nyar.Assembler;
using Valkyrie.Compiler.Pipeline;
using Valkyrie.Compiler.Targets;

namespace Valkyrie.Compiler.Packaging;

/// <summary>
/// 默认目标打包器：将后端输出编码为主产物，并补齐启动脚本等 sidecar。
/// </summary>
public sealed class DefaultTargetPackager : ITargetPackager
{
    public ArtifactSet Package(
        string moduleName,
        OutputSpec generated,
        TargetContract targetContract)
    {
        var primaryArtifact = BuildPrimaryArtifact(moduleName, generated, targetContract);
        var sidecarArtifacts = new List<CompilerArtifact>();

        foreach (var asset in generated.Assets)
        {
            sidecarArtifacts.Add(new CompilerArtifact(asset.Name, asset.Content, asset.MediaType));
        }

        AddLauncherArtifacts(sidecarArtifacts, moduleName, targetContract, primaryArtifact);
        return new ArtifactSet(primaryArtifact, sidecarArtifacts);
    }

    private static CompilerArtifact BuildPrimaryArtifact(
        string moduleName,
        OutputSpec generated,
        TargetContract targetContract)
    {
        if (generated is OutputSpec<Acorn.Jvm.Data.JvmClassFileData> jvmSpec)
        {
            var encoder = new Acorn.Jvm.Encode.JvmEncoder();
            var classBytes = encoder.Encode(jvmSpec.Data);
            return new CompilerArtifact($"{moduleName}.class", classBytes, "application/java-vm");
        }

        if (generated is OutputSpec<Acorn.Wasm.Data.WasmModuleData> wasmSpec)
        {
            var bytes = Acorn.Wasm.Encode.WasmEncoder.EncodeModule(wasmSpec.Data);
            return new CompilerArtifact($"{moduleName}.wasm", bytes, "application/wasm");
        }

        if (generated is OutputSpec<Acorn.Clr.Data.ClrModuleData> clrSpec)
        {
            var encoder = new Acorn.Clr.Encode.ClrEncoder();
            var bytes = encoder.Encode(clrSpec.Data);
            return new CompilerArtifact($"{moduleName}{generated.FileExtension}", bytes, "application/octet-stream");
        }

        if (generated is OutputSpec<Nyar.Assembler.Native.NativeCodeInfo>)
        {
            return new CompilerArtifact($"{moduleName}{generated.FileExtension}", [], "application/octet-stream");
        }

        throw new NotSupportedException($"暂不支持为 `{targetContract.CanonicalTriple}` 组装主产物：{generated.GetType().Name}");
    }

    private static void AddLauncherArtifacts(
        ICollection<CompilerArtifact> sidecarArtifacts,
        string moduleName,
        TargetContract targetContract,
        CompilerArtifact primaryArtifact)
    {
        switch (targetContract.BackendFamily)
        {
            case "JVM":
            {
                var jarName = $"{moduleName}.jar";
                AddIfMissing(sidecarArtifacts, new CompilerArtifact(jarName, BuildJvmJar(moduleName, primaryArtifact.Content), "application/java-archive"));
                AddIfMissing(sidecarArtifacts, new CompilerArtifact($"{moduleName}.run.ps1", BuildJvmPs1(jarName), "text/plain"));
                AddIfMissing(sidecarArtifacts, new CompilerArtifact($"{moduleName}.run.sh", BuildJvmSh(jarName), "text/x-shellscript"));
                break;
            }
            case "CLR":
            {
                var exeName = $"{moduleName}.exe";
                AddIfMissing(sidecarArtifacts, new CompilerArtifact($"{moduleName}.runtimeconfig.json", BuildClrRuntimeConfig(), "application/json"));
                AddIfMissing(sidecarArtifacts, new CompilerArtifact($"{moduleName}.pdb", BuildClrPortablePdbPlaceholder(), "application/octet-stream"));
                AddIfMissing(sidecarArtifacts, new CompilerArtifact($"{moduleName}.xml", BuildClrXmlDoc(moduleName), "application/xml"));
                AddIfMissing(sidecarArtifacts, new CompilerArtifact($"{moduleName}.run.ps1", BuildClrPs1(exeName), "text/plain"));
                AddIfMissing(sidecarArtifacts, new CompilerArtifact($"{moduleName}.run.sh", BuildClrSh(exeName), "text/x-shellscript"));
                break;
            }
        }
    }

    private static void AddIfMissing(ICollection<CompilerArtifact> artifacts, CompilerArtifact artifact)
    {
        if (artifacts.Any(item => string.Equals(item.Name, artifact.Name, StringComparison.Ordinal)))
        {
            return;
        }

        artifacts.Add(artifact);
    }

    private static byte[] BuildJvmJar(string mainClassName, byte[] classBytes)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var manifestEntry = archive.CreateEntry("META-INF/MANIFEST.MF");
            using (var writer = new StreamWriter(manifestEntry.Open(), new UTF8Encoding(false)))
            {
                writer.Write("Manifest-Version: 1.0\r\n");
                writer.Write($"Main-Class: {mainClassName}\r\n");
                writer.Write("\r\n");
            }

            var classEntry = archive.CreateEntry($"{mainClassName}.class");
            using var classStream = classEntry.Open();
            classStream.Write(classBytes, 0, classBytes.Length);
        }

        return stream.ToArray();
    }

    private static byte[] BuildJvmPs1(string jarName)
    {
        var script = $"java -jar \"$PSScriptRoot/{jarName}\" @args{Environment.NewLine}";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] BuildJvmSh(string jarName)
    {
        var script = "#!/usr/bin/env sh\nDIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\njava -jar \"$DIR/" + jarName + "\" \"$@\"\n";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] BuildClrPs1(string exeName)
    {
        var script = $"dotnet \"$PSScriptRoot/{exeName}\" @args{Environment.NewLine}";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] BuildClrSh(string exeName)
    {
        var script = "#!/usr/bin/env sh\nDIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\ndotnet \"$DIR/" + exeName + "\" \"$@\"\n";
        return Encoding.UTF8.GetBytes(script);
    }

    private static byte[] BuildClrRuntimeConfig()
    {
        const string json = """
                            {
                              "runtimeOptions": {
                                "tfm": "net8.0",
                                "framework": {
                                  "name": "Microsoft.NETCore.App",
                                  "version": "8.0.0"
                                },
                                "rollForward": "LatestMajor"
                              }
                            }
                            """;
        return Encoding.UTF8.GetBytes(json + Environment.NewLine);
    }

    private static byte[] BuildClrPortablePdbPlaceholder()
    {
        // 占位调试符号文件：用于补齐 sidecar 产物契约。
        // 后续由真正的 CLR 后端符号发射逻辑替换为可调试的 PDB 内容。
        return [];
    }

    private static byte[] BuildClrXmlDoc(string moduleName)
    {
        var xml = $"""
                   <?xml version="1.0" encoding="utf-8"?>
                   <doc>
                     <assembly>
                       <name>{moduleName}</name>
                     </assembly>
                     <members>
                     </members>
                   </doc>
                   """;
        return Encoding.UTF8.GetBytes(xml + Environment.NewLine);
    }
}
