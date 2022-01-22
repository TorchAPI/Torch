using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Torch.Plugins;

internal static class AssemblyRewriter
{
    public static Assembly ProcessWeavers(this Stream stream)
    {
        using var assStream = new MemoryStream();
        stream.CopyTo(assStream);
        assStream.Position = 0;
        using var module = ModuleDefinition.ReadModule(assStream);
        foreach (var fieldDefinition in FindAllToRewrite(module))
        {
            fieldDefinition.IsInitOnly = false;
        }

        using var memStream = new MemoryStream();
        module.Assembly.Write(memStream);
        return Assembly.Load(memStream.ToArray());
    }

    private static IEnumerable<FieldDefinition> FindAllToRewrite(ModuleDefinition definition)
    {
        return definition.Types.SelectMany(b => b.Fields.Where(HasValidAttributes));
    }

    private static bool HasValidAttributes(FieldDefinition definition) =>
        definition.CustomAttributes.Any(b => b.AttributeType.Name.Contains("Reflected") || b.AttributeType.Name == "DependencyAttribute");
}