using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Torch.Plugins
{
    [Serializable]
    [XmlRoot("configuration")]
    public class AppConfig
    {
        [XmlElement("runtime")]
        public RuntimeSection Runtime { get; set; }
    }

    [Serializable]
    public class RuntimeSection
    {
        [XmlElement("assemblyBinding", Namespace = "urn:schemas-microsoft-com:asm.v1")]
        public AssemblyBindingSection AssemblyBinding { get; set; }
    }

    [Serializable]
    public class AssemblyBindingSection
    {
        [XmlElement("dependentAssembly")]
        public List<DependentAssemblyElement> DependentAssemblies { get; set; }
    }

    [Serializable]
    public class DependentAssemblyElement
    {
        [XmlElement("assemblyIdentity")]
        public AssemblyIdentityElement AssemblyIdentity { get; set; }

        [XmlElement("bindingRedirect")]
        public BindingRedirectElement BindingRedirect { get; set; }
    }

    [Serializable]
    public class AssemblyIdentityElement
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("publicKeyToken")]
        public string PublicKeyToken { get; set; }

        [XmlAttribute("culture")]
        public string Culture { get; set; }
    }

    [Serializable]
    public class BindingRedirectElement
    {
        [XmlAttribute("oldVersion")]
        public string OldVersion { get; set; }

        [XmlAttribute("newVersion")]
        public string NewVersion { get; set; }
    }
}
