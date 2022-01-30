using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using NLog;

namespace Torch.Plugins;

internal static class AssemblyRewriter
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    private static readonly IAssemblyResolver Resolver;

    static AssemblyRewriter()
    {
        var resolver = new DefaultAssemblyResolver();
        Resolver = resolver;
        resolver.AddSearchDirectory(Directory.GetCurrentDirectory());
        resolver.AddSearchDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DedicatedServer64"));
    }

    public static Assembly ProcessWeavers(this Stream stream)
    {
        using var assStream = new MemoryStream();
        stream.CopyTo(assStream);
        assStream.Position = 0;
        using var module = ModuleDefinition.ReadModule(assStream, new()
        {
            AssemblyResolver = Resolver
        });
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