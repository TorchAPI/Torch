using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Represents a set of common patches that can all be reversed in a single step.
    /// </summary>
    public sealed class PatchContext
    {
        private readonly Dictionary<MethodBase, MethodRewritePattern> _rewritePatterns = new Dictionary<MethodBase, MethodRewritePattern>();

        internal PatchContext()
        {
        }

        /// <summary>
        /// Gets the rewrite pattern used to tracking changes in this context, creating one if it doesn't exist.
        /// </summary>
        /// <param name="method">Method to get the pattern for</param>
        /// <returns></returns>
        public MethodRewritePattern GetPattern(MethodBase method)
        {
            if (_rewritePatterns.TryGetValue(method, out MethodRewritePattern pattern))
                return pattern;
            MethodRewritePattern parent = PatchManager.GetPatternInternal(method);
            var res = new MethodRewritePattern(parent);
            _rewritePatterns.Add(method, res);
            return res;
        }

        /// <summary>
        /// Gets all methods that this context has patched
        /// </summary>
        internal IEnumerable<MethodBase> PatchedMethods => _rewritePatterns
            .Where(x => x.Value.Prefixes.Count > 0 || x.Value.Suffixes.Count > 0 || x.Value.Transpilers.Count > 0)
            .Select(x => x.Key);
        
        /// <summary>
        /// Removes all patches in this context
        /// </summary>
        internal void RemoveAll()
        {
            foreach (MethodRewritePattern pattern in _rewritePatterns.Values)
            {
                pattern.Prefixes.RemoveAll();
                pattern.Transpilers.RemoveAll();
                pattern.Suffixes.RemoveAll();
            }
        }
    }
}
