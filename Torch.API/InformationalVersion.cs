using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    /// <summary>
    /// Version in the form v#.#.#.#-branch
    /// </summary>
    public class InformationalVersion
    {
        public Version Version { get; set; }
        public string Branch { get; set; }

        public static bool TryParse(string input, out InformationalVersion version)
        {
            version = default(InformationalVersion);
            var trim = input.TrimStart('v');
            var info = trim.Split(new[]{'-'}, 2);
            if (!Version.TryParse(info[0], out Version result))
                return false;

            version = new InformationalVersion { Version = result };

            if (info.Length > 1)
                version.Branch = info[1];
            
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Branch == null)
                return $"v{Version}";

            return $"v{Version}-{string.Join("-", Branch)}";
        }

        public static explicit operator InformationalVersion(Version v)
        {
            return new InformationalVersion { Version = v };
        }

        public static implicit operator Version(InformationalVersion v)
        {
            return v.Version;
        }

        public static bool operator >(InformationalVersion lhs, InformationalVersion rhs)
        {
            return lhs.Version > rhs.Version;
        }

        public static bool operator <(InformationalVersion lhs, InformationalVersion rhs)
        {
            return lhs.Version < rhs.Version;
        }
    }
}
