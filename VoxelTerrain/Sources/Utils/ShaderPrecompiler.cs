using SlimDX;
using SlimDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VoxelTerrain
{
    public static class ShaderPrecompiler
    {
        public static ShaderBytecode PrecompileOrLoad(string fileName, string entryPoint, string profile, ShaderFlags shaderFlags, EffectFlags effectFlags)
        {
            FileInfo sourceFile = new FileInfo(fileName);

            if (!sourceFile.Exists)
                throw new FileNotFoundException();

            FileInfo compiledFile = new FileInfo(@"Precompiled\" + Path.GetFileNameWithoutExtension(sourceFile.Name) + "_" + entryPoint + "_" + profile + ".bin");

            if (compiledFile.Exists && sourceFile.LastWriteTime > compiledFile.LastWriteTime)
            {
                compiledFile.Delete();
                compiledFile.Refresh();
            }

            ShaderBytecode shaderBytecode = null;

            if (compiledFile.Exists)
            {
                byte[] compiledBytes = File.ReadAllBytes(compiledFile.FullName);
                DataStream compiledDataStream = new DataStream(compiledBytes, true, false);

                shaderBytecode = new ShaderBytecode(compiledDataStream);
            }
            else
            {
                shaderBytecode = ShaderBytecode.CompileFromFile(fileName, entryPoint, profile, shaderFlags, effectFlags);

                byte[] compiledBytes = shaderBytecode.Data.ReadRange<byte>((int)shaderBytecode.Data.Length);

                Directory.CreateDirectory(Path.GetDirectoryName(compiledFile.FullName));
                File.WriteAllBytes(compiledFile.FullName, compiledBytes);
            }

            if (shaderBytecode == null)
                throw new D3DCompilerException();

            return shaderBytecode;
        }

        public static ShaderBytecode PrecompileOrLoad(string fileName, string profile, ShaderFlags shaderFlags, EffectFlags effectFlags)
        {
            return PrecompileOrLoad(fileName, null, profile, shaderFlags, effectFlags);
        }
    }
}
