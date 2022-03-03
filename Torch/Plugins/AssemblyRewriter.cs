using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Torch.Plugins;

internal static class AssemblyRewriter
{
    private static readonly ZipResolver _zipResolver;
    private static readonly DefaultAssemblyResolver _defaultResolver;

    static AssemblyRewriter()
    {
        _defaultResolver = new();
        _zipResolver = new(_defaultResolver);
        _defaultResolver.AddSearchDirectory(Directory.GetCurrentDirectory());
        _defaultResolver.AddSearchDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DedicatedServer64"));
    }

    public static Assembly ProcessWeavers(this Stream stream, ZipArchive archive)
    {
        _zipResolver.Archive = archive;
        using var assStream = new MemoryStream();
        stream.CopyTo(assStream);
        assStream.Position = 0;
        var ass = ProcessInternal(assStream, _zipResolver);
        _zipResolver.Archive = null;
        return ass;
    }
    
    public static Assembly ProcessWeavers(this Stream stream, string path)
    {
        _defaultResolver.AddSearchDirectory(path);
        using var assStream = new MemoryStream();
        stream.CopyTo(assStream);
        assStream.Position = 0;
        var ass = ProcessInternal(assStream, _defaultResolver);
        _defaultResolver.RemoveSearchDirectory(path);
        return ass;
    }

    private static Assembly ProcessInternal(Stream inputStream, IAssemblyResolver resolver)
    {
        using var module = ModuleDefinition.ReadModule(inputStream, new()
        {
            AssemblyResolver = _zipResolver
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
    
    private class ZipResolver : IAssemblyResolver
    {
        private readonly IAssemblyResolver _fallbackResolver;
        public ZipArchive Archive { get; set; }
        
        public ZipResolver(IAssemblyResolver fallbackResolver)
        {
            _fallbackResolver = fallbackResolver;
        }

        public void Dispose()
        {
            _fallbackResolver.Dispose();
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            var fileName = $"{name.Name}.dll";
            
            if (Archive.Entries.FirstOrDefault(entry => entry.Name == fileName) is not { } archiveEntry)
                return _fallbackResolver.Resolve(name, parameters);
            
            using var stream = archiveEntry.Open();
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Position = 0;
            
            return AssemblyDefinition.ReadAssembly(memStream, parameters);
        }
    }
}