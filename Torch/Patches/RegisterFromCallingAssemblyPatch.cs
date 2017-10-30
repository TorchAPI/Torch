using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Game.Entities;
using Torch.Utils;
using VRage.Game.Common;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ObjectBuilders;

namespace Torch.Patches
{

    /// <summary>
    /// There are places in static ctors where the registered assembly depends on the <see cref="Assembly.GetCallingAssembly"/>.
    /// Here we force those registrations with the proper assemblies to ensure they work correctly.
    /// </summary>
    internal static class RegisterFromCallingAssemblyPatch
    {
#pragma warning disable 649
        [ReflectedGetter(Name="m_objectFactory", TypeName = "Sandbox.Game.Entities.MyEntityFactory, Sandbox.Game")]
        private static readonly Func<MyObjectFactory<MyEntityTypeAttribute, MyEntity>> _entityFactoryObjectFactory;
#pragma warning restore 649

        internal static void ForceRegisterAssemblies()
        {
            // static MyEntities() called by MySandboxGame.ForceStaticCtor
            RuntimeHelpers.RunClassConstructor(typeof(MyEntities).TypeHandle);
            RegisterFromAssemblySafe(_entityFactoryObjectFactory(), typeof(MySandboxGame).Assembly);

            // static MyGuiManager():
            // MyGuiControlsFactory.RegisterDescriptorsFromAssembly();

            // static MyComponentTypeFactory() called by MyComponentContainer.Add
            // _componentFactoryRegisterAssembly(typeof(MyComponentContainer).Assembly);

            // static MyObjectPoolManager()
            // Render, so should be fine.
        }

        private static void RegisterDescriptorSafe<TAttribute, TCreatedObjectBase>(
            MyObjectFactory<TAttribute, TCreatedObjectBase> factory, TAttribute descriptor, Type type) where TAttribute : MyFactoryTagAttribute where TCreatedObjectBase : class
        {
            if (factory.Attributes.TryGetValue(type, out _))
                return;
            if (descriptor.ObjectBuilderType != null && factory.TryGetProducedType(descriptor.ObjectBuilderType) != null)
                return;
            if (typeof(MyObjectBuilder_Base).IsAssignableFrom(descriptor.ProducedType) &&
                factory.TryGetProducedType(descriptor.ProducedType) != null)
                return;
            factory.RegisterDescriptor(descriptor, type);
        }

        private static void RegisterFromAssemblySafe<TAttribute, TCreatedObjectBase>(MyObjectFactory<TAttribute, TCreatedObjectBase> factory, Assembly assembly) where TAttribute : MyFactoryTagAttribute where TCreatedObjectBase : class
        {
            if (assembly == null)
            {
                return;
            }
            foreach (Type type in assembly.GetTypes())
            {
                foreach (TAttribute descriptor in type.GetCustomAttributes<TAttribute>())
                {
                    RegisterDescriptorSafe(factory, descriptor, type);
                }
            }
        }
    }
}
