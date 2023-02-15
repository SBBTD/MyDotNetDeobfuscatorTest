using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Windows;

namespace MyDotNetDeobfuscator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("请在参数中指定模块路径。");
                return;
            }

            var module = ModuleDefMD.Load(args[0]);
            if (module == null) return;
            var n = 0;
            foreach (var type in module.Types) {
                foreach (var method in type.Methods) {
                    if (!method.HasBody) continue;
                    for (int i = 0; i < method.Body.Instructions.Count - 2; i++) {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldsfld
                            && method.Body.Instructions[i].Operand.ToString().EndsWith(".c_get_Syntax_GW9XDT::f_ExecuteDependencyCode")
                            && method.Body.Instructions[i + 1].OpCode == OpCodes.Ldstr
                            && method.Body.Instructions[i + 2].OpCode == OpCodes.Callvirt
                            && method.Body.Instructions[i + 2].Operand.ToString().EndsWith(".c_set_AvatarFull::DecryptText(System.String)")
                        ) {
                            // Decrypt
                            var key = Convert.FromBase64String("PmMFO5H9dEQBHdJapNKdWpUKg/4Uk/kyqcmgUddoWiY=");
                            var enc = Convert.FromBase64String(method.Body.Instructions[i + 1].Operand.ToString());
                            var result = "";
                            AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider {
                                Key = key,
                                BlockSize = 128,
                                Mode = CipherMode.CBC,
                                Padding = PaddingMode.PKCS7
                            };
                            byte[] array = new byte[aesCryptoServiceProvider.BlockSize / 8];
                            Array.Copy(enc, 0, array, 0, array.Length);
                            ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateDecryptor(aesCryptoServiceProvider.Key, array);
                            MemoryStream memoryStream = new MemoryStream();
                            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
                            BinaryWriter binaryWriter = new BinaryWriter(cryptoStream);
                            binaryWriter.Write(enc, array.Length, enc.Length - array.Length);
                            result = Encoding.UTF8.GetString(memoryStream.ToArray());

                            // Patch
                            method.Body.Instructions[i].OpCode = OpCodes.Nop;
                            method.Body.Instructions[i + 1].Operand = result;
                            method.Body.Instructions[i + 2].OpCode = OpCodes.Nop;
                            i += 2;
                            n++;
                        }
                    }
                }
            }

            module.Write(Path.GetFileNameWithoutExtension(args[0]) + "-dec" + Path.GetExtension(args[0]));
            Console.WriteLine($"Replaced {n} strings.");
        }
    }
}
