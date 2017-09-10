using System.Collections.Generic;
using System.Reflection;

namespace Torch.Managers.PatchManager
{
    /// <summary>
    /// Represents a set of common patches that can all be reversed in a single step.
    /// </summary>
    public class PatchContext
    {
        private readonly PatchManager _replacer;
        private readonly Dictionary<MethodBase, MethodRewritePattern> _rewritePatterns = new Dictionary<MethodBase, MethodRewritePattern>();

        internal PatchContext(PatchManager replacer)
        {
            _replacer = replacer;
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
            MethodRewritePattern parent = _replacer.GetPattern(method);
            var res = new MethodRewritePattern(parent);
            _rewritePatterns.Add(method, res);
            return res;
        }
        
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
