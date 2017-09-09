using System.Collections.Generic;
using System.Reflection;
using Torch.API;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Applies and removes patches from the IL of methods.
    /// </summary>
    public class PatchManager : Manager
    {
        /// <summary>
        /// Creates a new patch manager.  Only have one active at a time.
        /// </summary>
        /// <param name="torchInstance"></param>
        public PatchManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        private readonly Dictionary<MethodBase, DecoratedMethod> _rewritePatterns = new Dictionary<MethodBase, DecoratedMethod>();
        private readonly HashSet<PatchContext> _contexts = new HashSet<PatchContext>();

        /// <summary>
        /// Gets the rewrite pattern for the given method, creating one if it doesn't exist.
        /// </summary>
        /// <param name="method">Method to get the pattern for</param>
        /// <returns></returns>
        public MethodRewritePattern GetPattern(MethodBase method)
        {
            if (_rewritePatterns.TryGetValue(method, out DecoratedMethod pattern))
                return pattern;
            var res = new DecoratedMethod(method);
            _rewritePatterns.Add(method, res);
            return res;
        }


        /// <summary>
        /// Creates a new <see cref="PatchContext"/> used for tracking changes.  A call to <see cref="Commit"/> will apply the patches.
        /// </summary>
        public PatchContext AcquireContext()
        {
            var context = new PatchContext(this);
            _contexts.Add(context);
            return context;
        }

        /// <summary>
        /// Frees the given context, and unregister all patches from it.  A call to <see cref="Commit"/> will apply the unpatching operation.
        /// </summary>
        /// <param name="context">Context to remove</param>
        public void FreeContext(PatchContext context)
        {
            context.RemoveAll();
            _contexts.Remove(context);
        }

        /// <summary>
        /// Commits all method decorations into IL.
        /// </summary>
        public void Commit()
        {
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
            foreach (DecoratedMethod m in _rewritePatterns.Values)
                m.Revert();
            _rewritePatterns.Clear();
            _contexts.Clear();
        }
    }
}
