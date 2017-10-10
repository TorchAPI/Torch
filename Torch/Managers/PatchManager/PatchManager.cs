using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using Torch.API;
using Torch.Managers.PatchManager.Transpile;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Applies and removes patches from the IL of methods.
    /// </summary>
    public class PatchManager : Manager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        internal static void AddPatchShims(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
                if (t.HasAttribute<PatchShimAttribute>())
                    AddPatchShim(t);
        }

        private static void AddPatchShim(Type type)
        {
            if (!type.IsSealed || !type.IsAbstract)
                _log.Warn($"Registering type {type.FullName} as a patch shim type, even though it isn't declared singleton");
            MethodInfo method = type.GetMethod("Patch", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                _log.Error($"Patch shim type {type.FullName} doesn't have a static Patch method.");
                return;
            }
            ParameterInfo[] ps = method.GetParameters();
            if (ps.Length != 1 || ps[0].IsOut || ps[0].IsOptional || ps[0].ParameterType.IsByRef ||
                ps[0].ParameterType == typeof(PatchContext) || method.ReturnType == typeof(void))
            {
                _log.Error($"Patch shim type {type.FullName} doesn't have a method with signature `void Patch(PatchContext)`");
                return;
            }
            var context = new PatchContext();
            lock (_coreContexts)
                _coreContexts.Add(context);
            method.Invoke(null, new object[] { context });
        }

        /// <summary>
        /// Creates a new patch manager.
        /// </summary>
        /// <param name="torchInstance"></param>
        public PatchManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        private static readonly Dictionary<MethodBase, DecoratedMethod> _rewritePatterns = new Dictionary<MethodBase, DecoratedMethod>();
        private static readonly Dictionary<Assembly, List<PatchContext>> _contexts = new Dictionary<Assembly, List<PatchContext>>();
        private static List<PatchContext> _coreContexts = new List<PatchContext>();

        /// <summary>
        /// Gets the rewrite pattern for the given method, creating one if it doesn't exist.
        /// </summary>
        /// <param name="method">Method to get the pattern for</param>
        /// <returns></returns>
        internal static MethodRewritePattern GetPatternInternal(MethodBase method)
        {
            lock (_rewritePatterns)
            {
                if (_rewritePatterns.TryGetValue(method, out DecoratedMethod pattern))
                    return pattern;
                var res = new DecoratedMethod(method);
                _rewritePatterns.Add(method, res);
                return res;
            }
        }

        /// <summary>
        /// Gets the rewrite pattern for the given method, creating one if it doesn't exist.
        /// </summary>
        /// <param name="method">Method to get the pattern for</param>
        /// <returns></returns>
        public MethodRewritePattern GetPattern(MethodBase method)
        {
            return GetPatternInternal(method);
        }


        /// <summary>
        /// Creates a new <see cref="PatchContext"/> used for tracking changes.  A call to <see cref="Commit"/> will apply the patches.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public PatchContext AcquireContext()
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            var context = new PatchContext();
            lock (_contexts)
            {
                if (!_contexts.TryGetValue(assembly, out List<PatchContext> localContexts))
                    _contexts.Add(assembly, localContexts = new List<PatchContext>());
                localContexts.Add(context);
            }
            return context;
        }

        /// <summary>
        /// Frees the given context, and unregister all patches from it.  A call to <see cref="Commit"/> will apply the unpatching operation.
        /// </summary>
        /// <param name="context">Context to remove</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FreeContext(PatchContext context)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            context.RemoveAll();
            lock (_contexts)
            {
                if (_contexts.TryGetValue(assembly, out List<PatchContext> localContexts))
                    localContexts.Remove(context);
            }
        }

        /// <summary>
        /// Frees all contexts owned by the given assembly.  A call to <see cref="Commit"/> will apply the unpatching operation.
        /// </summary>
        /// <param name="assembly">Assembly to retrieve owned contexts for</param>
        /// <param name="callback">Callback to run for before each context is freed, ignored if null.</param>
        /// <returns>number of contexts freed</returns>
        internal int FreeAllContexts(Assembly assembly, Action<PatchContext> callback = null)
        {
            List<PatchContext> localContexts;
            lock (_contexts)
            {
                if (!_contexts.TryGetValue(assembly, out localContexts))
                    return 0;
                _contexts.Remove(assembly);
            }
            if (localContexts == null)
                return 0;
            int count = localContexts.Count;
            foreach (PatchContext k in localContexts)
            {
                callback?.Invoke(k);
                k.RemoveAll();
            }
            localContexts.Clear();
            return count;
        }

        /// <summary>
        /// Commits all method decorations into IL.
        /// </summary>
        public void Commit()
        {
            lock (_rewritePatterns)
                foreach (DecoratedMethod m in _rewritePatterns.Values)
                    m.Commit();
        }

        /// <summary>
        /// Commits any existing patches.
        /// </summary>
        public override void Attach()
        {
            Commit();
        }

        /// <summary>
        /// Unregisters and removes all patches, then applies the unpatching operation.
        /// </summary>
        public override void Detach()
        {
            lock (_contexts)
            {
                foreach (List<PatchContext> set in _contexts.Values)
                    foreach (PatchContext ctx in set)
                        ctx.RemoveAll();
                _contexts.Clear();
                foreach (DecoratedMethod m in _rewritePatterns.Values)
                    m.Revert();
            }
        }
    }
}
