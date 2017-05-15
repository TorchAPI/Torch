using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Torch.Server.ViewModels.Blocks
{
    public static class BlockViewModelGenerator
    {
        private static Dictionary<Type, Type> _cache = new Dictionary<Type, Type>();
        private static AssemblyName _asmName;
        private static ModuleBuilder _mb;
        private static AssemblyBuilder _ab;
        private static Logger _log = LogManager.GetLogger("Generator");

        static BlockViewModelGenerator()
        {
            _asmName = new AssemblyName("Torch.Server.ViewModels.Generated");
            _ab = AppDomain.CurrentDomain.DefineDynamicAssembly(_asmName, AssemblyBuilderAccess.RunAndSave);
            _mb = _ab.DefineDynamicModule(_asmName.Name);
        }

        public static void GenerateModels()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            {
                foreach (var type in assembly.ExportedTypes.Where(t => t.IsSubclassOf(typeof(MyTerminalBlock))))
                {
                    GenerateModel(type);
                }
            }
            _ab.Save("Generated.dll", PortableExecutableKinds.ILOnly, ImageFileMachine.AMD64);
        }

        public static Type GenerateModel(Type blockType, bool force = false)
        {
            if (_cache.ContainsKey(blockType) && !force)
                return _cache[blockType];

            var propertyList = new List<ITerminalProperty>();
            MyTerminalControlFactoryHelper.Static.GetProperties(blockType, propertyList);

            var getPropertyMethod = blockType.GetMethod("GetProperty", new[] {typeof(string)});
            var getValueMethod = typeof(ITerminalProperty<>).GetMethod("GetValue");
            var setValueMethod = typeof(ITerminalProperty<>).GetMethod("SetValue");
            var tb = _mb.DefineType($"{_asmName.Name}.{blockType.Name}ViewModel", TypeAttributes.Class | TypeAttributes.Public);
            var blockField = tb.DefineField("_block", blockType, FieldAttributes.Private);

            var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] {blockType});
            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, blockField);
            ctorIl.Emit(OpCodes.Ret);

            for (var i = 0; i < propertyList.Count; i++)
            {
                var prop = propertyList[i];
                var propType = prop.GetType();

                Type propGenericArg = null;

                foreach (var iface in propType.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITerminalProperty<>))
                        propGenericArg = iface.GenericTypeArguments[0];
                }

                if (propGenericArg == null)
                {
                    _log.Error($"Property {prop.Id} does not implement {typeof(ITerminalProperty<>).Name}");
                    return null;
                }

                _log.Info($"GENERIC ARG: {propGenericArg.Name}");

                var pb = tb.DefineProperty($"{prop.Id}", PropertyAttributes.HasDefault, propGenericArg, null);
                var getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                var getter = tb.DefineMethod($"get_{prop.Id}", getSetAttr, propGenericArg, Type.EmptyTypes);
                {
                    var getterIl = getter.GetILGenerator();
                    var propLoc = getterIl.DeclareLocal(propType);
                    getterIl.Emit(OpCodes.Ldarg_0);
                    getterIl.Emit(OpCodes.Ldfld, blockField);
                    getterIl.Emit(OpCodes.Ldstr, prop.Id);
                    getterIl.EmitCall(OpCodes.Callvirt, getPropertyMethod, null);
                    getterIl.Emit(OpCodes.Stloc, propLoc);
                    getterIl.Emit(OpCodes.Ldloc, propLoc);
                    getterIl.Emit(OpCodes.Ldarg_0);
                    getterIl.Emit(OpCodes.Ldfld, blockField);
                    getterIl.EmitCall(OpCodes.Callvirt, getValueMethod, null);
                    getterIl.Emit(OpCodes.Ret);
                    pb.SetGetMethod(getter);
                }

                var setter = tb.DefineMethod($"set_{prop.Id}", getSetAttr, null, Type.EmptyTypes);
                {
                    var setterIl = setter.GetILGenerator();
                    var propLoc = setterIl.DeclareLocal(propType);
                    setterIl.Emit(OpCodes.Ldarg_0);
                    setterIl.Emit(OpCodes.Stfld, blockField);
                    setterIl.EmitCall(OpCodes.Callvirt, getPropertyMethod, null);
                    setterIl.Emit(OpCodes.Stloc, propLoc);
                    setterIl.Emit(OpCodes.Ldarg_1);
                    setterIl.Emit(OpCodes.Ldarg_0);
                    setterIl.Emit(OpCodes.Ldfld, blockField);
                    setterIl.EmitCall(OpCodes.Callvirt, setValueMethod, null);
                    setterIl.Emit(OpCodes.Ret);
                    pb.SetSetMethod(setter);
                }
            }

            var vmType = tb.CreateType();
            _cache.Add(blockType, vmType);
            return vmType;
        }
    }
}
