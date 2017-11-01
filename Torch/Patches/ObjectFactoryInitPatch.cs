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
using VRage.Plugins;
using VRage.Utils;

namespace Torch.Patches
{

    /// <summary>
    /// There are places in static ctors where the registered assembly depends on the <see cref="Assembly.GetCallingAssembly"/>
    /// or <see cref="MyPlugins"/>.  Here we force those registrations with the proper assemblies to ensure they work correctly.
    /// </summary>
    internal static class ObjectFactoryInitPatch
    {
#pragma warning disable 649
        [ReflectedGetter(Name = "m_objectFactory", TypeName = "Sandbox.Game.Entities.MyEntityFactory, Sandbox.Game")]
        private static readonly Func<MyObjectFactory<MyEntityTypeAttribute, MyEntity>> _entityFactoryObjectFactory;
#pragma warning restore 649

        internal static void ForceRegisterAssemblies()
        {
            // static MyEntities() called by MySandboxGame.ForceStaticCtor
            RuntimeHelpers.RunClassConstructor(typeof(MyEntities).TypeHandle);
            {
                MyObjectFactory<MyEntityTypeAttribute, MyEntity> factory = _entityFactoryObjectFactory();
                ObjectFactory_RegisterFromAssemblySafe(factory, typeof(MySandboxGame).Assembly); // calling assembly
                ObjectFactory_RegisterFromAssemblySafe(factory, MyPlugins.GameAssembly);
                ObjectFactory_RegisterFromAssemblySafe(factory, MyPlugins.SandboxAssembly);
                ObjectFactory_RegisterFromAssemblySafe(factory, MyPlugins.UserAssembly);
            }

            // static MyGuiManager():
            // MyGuiControlsFactory.RegisterDescriptorsFromAssembly();

            // static MyComponentTypeFactory() called by MyComponentContainer.Add
            RuntimeHelpers.RunClassConstructor(typeof(MyComponentTypeFactory).TypeHandle);
            {
                ComponentTypeFactory_RegisterFromAssemblySafe(typeof(MyComponentContainer).Assembly); // calling assembly
                ComponentTypeFactory_RegisterFromAssemblySafe(MyPlugins.SandboxAssembly);
                ComponentTypeFactory_RegisterFromAssemblySafe(MyPlugins.GameAssembly);
                ComponentTypeFactory_RegisterFromAssemblySafe(MyPlugins.SandboxGameAssembly);
                ComponentTypeFactory_RegisterFromAssemblySafe(MyPlugins.UserAssembly);
            }

            // static MyObjectPoolManager()
            // Render, so should be fine.
        }

        #region MyObjectFactory Adders
        private static void ObjectFactory_RegisterDescriptorSafe<TAttribute, TCreatedObjectBase>(
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

        private static void ObjectFactory_RegisterFromAssemblySafe<TAttribute, TCreatedObjectBase>(MyObjectFactory<TAttribute, TCreatedObjectBase> factory, Assembly assembly) where TAttribute : MyFactoryTagAttribute where TCreatedObjectBase : class
        {
            if (assembly == null)
            {
                return;
            }
            foreach (Type type in assembly.GetTypes())
            {
                foreach (TAttribute descriptor in type.GetCustomAttributes<TAttribute>())
                {
                    ObjectFactory_RegisterDescriptorSafe(factory, descriptor, type);
                }
            }
        }
        #endregion
        #region MyComponentTypeFactory Adders

        [ReflectedGetter(Name = "m_idToType", Type = typeof(MyComponentTypeFactory))]
        private static Func<Dictionary<MyStringId, Type>> _componentTypeFactoryIdToType;
        [ReflectedGetter(Name = "m_typeToId", Type = typeof(MyComponentTypeFactory))]
        private static Func<Dictionary<Type, MyStringId>> _componentTypeFactoryTypeToId;
        [ReflectedGetter(Name = "m_typeToContainerComponentType", Type = typeof(MyComponentTypeFactory))]
        private static Func<Dictionary<Type, Type>> _componentTypeFactoryContainerComponentType;

        private static void ComponentTypeFactory_RegisterFromAssemblySafe(Assembly assembly)
        {
            if (assembly == null)
                return;
            foreach (Type type in assembly.GetTypes())
                if (typeof(MyComponentBase).IsAssignableFrom(type))
                {
                    ComponentTypeFactory_AddIdSafe(type, MyStringId.GetOrCompute(type.Name));
                    ComponentTypeFactory_RegisterComponentTypeAttributeSafe(type);
                }
        }

        private static void ComponentTypeFactory_RegisterComponentTypeAttributeSafe(Type type)
        {
            Type componentType = type.GetCustomAttribute<MyComponentTypeAttribute>(true)?.ComponentType;
            if (componentType != null)
                _componentTypeFactoryContainerComponentType()[type] = componentType;
        }

        private static void ComponentTypeFactory_AddIdSafe(Type type, MyStringId id)
        {
            _componentTypeFactoryIdToType()[id] = type;
            _componentTypeFactoryTypeToId()[type] = id;
        }
        #endregion
    }
}
