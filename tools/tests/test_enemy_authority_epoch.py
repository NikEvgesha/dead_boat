"""Compile and execute the production readiness gate without starting Unity.
Usage: python tools/tests/test_enemy_authority_epoch.py --unity-editor PATH/Unity.exe
Uses the C# compiler and .NET runtime bundled with that editor, writes only Logs/.
"""
import argparse
import json
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity-editor", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    data = args.unity_editor.resolve().parent / "Data"
    dotnet = data / "NetCoreRuntime/dotnet.exe"
    versions = data / "NetCoreRuntime/shared/Microsoft.NETCore.App"
    version = max(versions.iterdir(), key=lambda p: tuple(int(x) for x in p.name.split(".")))
    compiler = data / "DotNetSdkRoslyn/csc.dll"
    output = root / "Logs/EnemyAuthorityEpochProbe"
    output.mkdir(parents=True, exist_ok=True)
    assembly = output / "Probe.dll"
    refs = [version / (name + ".dll") for name in ("System.Private.CoreLib", "System.Runtime", "System.Console", "System.Collections")]
    sources = [root / "Assets/Online/EnemyAuthorityEpoch.cs", root / "tools/tests/EnemyAuthorityEpochProbe.cs"]
    rsp = output / "probe.rsp"
    lines = ["-target:exe", '-out:"' + str(assembly) + '"', "-langversion:latest"]
    lines += ['-r:"' + str(p) + '"' for p in refs]
    lines += ['"' + str(p) + '"' for p in sources]
    rsp.write_text("\n".join(lines) + "\n", encoding="utf-8")
    with (output / "compile.log").open("w", encoding="utf-8") as log:
        subprocess.run([str(dotnet), "exec", str(compiler), "/noconfig", "/nostdlib", "@" + str(rsp)], cwd=root, stdout=log, stderr=subprocess.STDOUT, check=True)
    assembly.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {"tfm": "net" + ".".join(version.name.split(".")[:2]), "framework": {"name": "Microsoft.NETCore.App", "version": version.name}}}), encoding="utf-8")
    result = subprocess.run([str(dotnet), str(assembly)], cwd=root, text=True, encoding="utf-8", stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "result.log").write_text(result.stdout, encoding="utf-8")
    print(result.stdout, end="")
    result.check_returncode()


if __name__ == "__main__":
    main()
