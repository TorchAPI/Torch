using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    /// <summary>
    /// Version in the form v#.#.#.#-info
    /// </summary>
    public class InformationalVersion
    {
        public Version Version { get; set; }
        public string[] Information { get; set; }

        public static bool TryParse(string input, out InformationalVersion version)
        {
            version = default(InformationalVersion);
            var trim = input.TrimStart('v');
            var info = trim.Split('-');
            if (!Version.TryParse(info[0], out Version result))
                return false;

            version = new InformationalVersion { Version = result };

            if (info.Length > 1)
                version.Information = info.Skip(1).ToArray();

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Information == null || Information.Length == 0)
                return $"v{Version}";

            return $"v{Version}-{string.Join("-", Information)}";
        }

        public static explicit operator InformationalVersion(Version v)
        {
            return new InformationalVersion { Version = v };
        }

        public static implicit operator Version(InformationalVersion v)
        {
            return v.Version;
        }
    }
}
