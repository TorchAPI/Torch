using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Torch.Managers.PatchManager
{
    internal class AssemblyMemory
    {
        /// <summary>
        /// Gets the address, in RAM, where the body of a method starts.
        /// </summary>
        /// <param name="method">Method to find the start of</param>
        /// <returns>Address of the method's start</returns>
        public static long GetMethodBodyStart(MethodBase method)
        {
            RuntimeMethodHandle handle;
            if (method is DynamicMethod)
                handle = (RuntimeMethodHandle)typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(method, new object[0]);
            else
                handle = method.MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);
            return handle.GetFunctionPointer().ToInt64();
        }



        // x64 ISA format:
        // [prefixes] [opcode] [mod-r/m]
        // [mod-r/m] is bitfield:
        // [7-6] = "mod" adressing mode
        // [5-3] = register or opcode extension
        // [2-0] = "r/m" extra addressing mode


        // http://ref.x86asm.net/coder64.html
        /// Direct register addressing mode.  (Jump directly to register)
        private const byte MODRM_MOD_DIRECT = 0b11;

        /// Long-mode prefix (64-bit operand)
        private const byte REX_W = 0x48;

        /// Moves a 16/32/64 operand into register i when opcode is (MOV_R0+i)
        private const byte MOV_R0 = 0xB8;

        // Extra opcodes.  Used with opcode extension.
        private const byte EXT = 0xFF;

        /// Opcode extension used with <see cref="EXT"/> for the JMP opcode.
        private const byte OPCODE_EXTENSION_JMP = 4;


        /// <summary>
        /// Reads a byte array from a memory location
        /// </summary>
        /// <param name="memory">Address to read from</param>
        /// <param name="bytes">Number of bytes to read</param>
        /// <returns>The bytes that were read</returns>
        public static byte[] ReadMemory(long memory, int bytes)
        {
            var data = new byte[bytes];
            Marshal.Copy(new IntPtr(memory), data,0, bytes);
            return data;
        }

        /// <summary>
        /// Writes a byte array to a memory location.
        /// </summary>
        /// <param name="memory">Address to write to</param>
        /// <param name="bytes">Data to write</param>
        public static void WriteMemory(long memory, byte[] bytes)
        {
            Marshal.Copy(bytes,0, new IntPtr(memory), bytes.Length);
        }

        /// <summary>
        /// Writes an x64 assembly jump instruction at the given address.
        /// </summary>
        /// <param name="memory">Address to write the instruction at</param>
        /// <param name="jumpTarget">Target address of the jump</param>
        /// <returns>The bytes that were overwritten</returns>
        public static byte[] WriteJump(long memory, long jumpTarget)
        {
            byte[] result = ReadMemory(memory, 12);
            unsafe
            {
                var ptr = (byte*)memory;
                *ptr = REX_W;
                *(ptr + 1) = MOV_R0;
                *((long*)(ptr + 2)) = jumpTarget;
                *(ptr + 10) = EXT;
                *(ptr + 11) = (MODRM_MOD_DIRECT << 6) | (OPCODE_EXTENSION_JMP << 3) | 0;
            }
            return result;
        }
    }
}
