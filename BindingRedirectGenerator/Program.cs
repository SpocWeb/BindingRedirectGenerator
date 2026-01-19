using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;
// we use cecil because System.Reflection.MetaData crashes...

namespace BindingRedirectGenerator;

public static class Program
{
    /// <summary> Xml Namespace </summary>
    public const string ns = "urn:schemas-microsoft-com:asm.v1";

    public const string AttribCulture = "culture";
    public const string AttribName = "name";
    public const string AttribPublicKeyToken = "publicKeyToken";
    private const string OptionKeepExisting = "keepExisting";
    private const string OptionOverwrite = "overwrite";

    public static readonly XName NameDependentAssembly = XName.Get("dependentAssembly", ns);
    public static readonly XName NameAssemblyIdentity = XName.Get("assemblyIdentity", ns);
    public static readonly XName NameBindingRedirect = XName.Get("bindingRedirect", ns);

    /// <inheritdoc cref="ReWriteBindingRedirects"/>
    public static void Main(params string[] args)
    {
        try
        {
            if (args.Length < 1)
            {
                var entryAssembly = Assembly.GetEntryAssembly()!.GetName();
                Console.WriteLine(entryAssembly.Name + " <config file path> [" + OptionKeepExisting + "|" + OptionOverwrite + "] [<search directory path>]");
                return;
            }

            var outputFilePath = new FileInfo(Path.GetFullPath(args[0]));
            bool keepExisting = args.Length > 1 && !OptionOverwrite.Equals(args[1]);
            var inputDirectoryPath = args.Length > 2 ? new DirectoryInfo(args[1]) : null;

            Console.WriteLine("Input       : " + inputDirectoryPath);
            Console.WriteLine("Output      : " + outputFilePath);
            Console.WriteLine();

            ReWriteBindingRedirects(outputFilePath, keepExisting, inputDirectoryPath);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    /// <summary> Rewrites binding Redirects in the <paramref name="configFile"/> for all Assemblies (DLLs and EXEs) in the <paramref name="searchDirectory"/> </summary>
    public static void ReWriteBindingRedirects(this FileInfo configFile, bool keepExisting , DirectoryInfo? searchDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(configFile);

        var doc = configFile.Exists ? XDocument.Load(configFile.FullName) : new XDocument();

        searchDirectory ??= configFile.Directory ?? throw new ArgumentException(null, nameof(configFile));
        var configuration = doc.GetOrCreateElement("configuration");
        var runtime = configuration.GetOrCreateElement("runtime");
        var assemblyBinding = runtime.GetOrCreateElement(XName.Get("assemblyBinding", ns));

        foreach (var file in searchDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories))
        {
            var ext = file.Extension;
            if (string.Compare(".dll", ext, StringComparison.OrdinalIgnoreCase) != 0 &&
                string.Compare(".exe", ext, StringComparison.OrdinalIgnoreCase) != 0)
            {
                Trace.TraceInformation("Ignoring: " + file);
                continue;
            }

            var asm = LoadAssembly(file);
            if (asm == null) // not .NET, not valid, etc.
                continue;

            if (asm.Name.PublicKeyToken == null || asm.Name.PublicKeyToken.Length == 0) // no strong name
            {
                Console.WriteLine("Skipping '" + file + "': No public key token.");
                continue;
            }

            var pkt = string.Join(string.Empty, asm.Name.PublicKeyToken.Select(i => i.ToString("x2")));
            Func<XElement, bool> cultureFunc;
            if (string.IsNullOrEmpty(asm.Name.Culture))
            {
                cultureFunc = i => i.Attribute(AttribCulture) == null || i.Attribute(AttribCulture)?.Value == "neutral";
            }
            else
            {
                cultureFunc = i => i.Attribute(AttribCulture)?.Value == asm.Name.Culture;
            }

            XElement bindingRedirect;
            var dependentAssembly = assemblyBinding
                .Elements(NameDependentAssembly)
                .Elements(NameAssemblyIdentity)
                .FirstOrDefault(i => i.Attribute(AttribName)?.Value == asm.Name.Name && i.Attribute(AttribPublicKeyToken)?.Value == pkt && cultureFunc(i))?
                .Parent;
            if (dependentAssembly != null)
            {
                if (keepExisting)
                {
                    Console.WriteLine("Ignoring '" + file + "' Binding Redirect already exists. ");
                    continue;
                }
                bindingRedirect = dependentAssembly.Element(NameBindingRedirect)!;
            }
            else
            {
                var xName = NameDependentAssembly;
                dependentAssembly = AddElement(assemblyBinding, xName);
                var assemblyIdentity = new XElement(NameAssemblyIdentity); dependentAssembly.Add(assemblyIdentity);
                assemblyIdentity.SetAttributeValue(AttribName, asm.Name.Name);
                assemblyIdentity.SetAttributeValue(AttribPublicKeyToken, pkt);
                if (!string.IsNullOrEmpty(asm.Name.Culture))
                {
                    assemblyIdentity.SetAttributeValue(AttribCulture, asm.Name.Culture);
                }
                bindingRedirect = new XElement(NameBindingRedirect); dependentAssembly.Add(bindingRedirect);
            }

            bindingRedirect.SetAttributeValue("oldVersion", "0.0.0.0-65535.65535.65535.65535");
            bindingRedirect.SetAttributeValue("newVersion", asm.Name.Version.ToString());
        }

        doc.Save(configFile.FullName);
    }

    public static XElement AddElement(this XElement assemblyBinding, XName xName)
    {
        var element = new XElement(xName); assemblyBinding.Add(element);
        return element;
    }

    /// <summary> Gets the first <see cref="XElement"/> with the <paramref name="xName"/> from the <paramref name="parent"/>
    /// or creates and adds it to the <paramref name="parent"/> </summary>
    /// <returns> the found or created Element. </returns>
    public static XElement GetOrCreateElement(this XContainer parent, XName xName)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(xName);

        var configuration = parent.Element(xName);
        if (configuration == null)
        {
            configuration = new XElement(xName);
            parent.Add(configuration);
        }

        return configuration;
    }

    public static AssemblyDefinition? LoadAssembly(this FileInfo path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            return AssemblyDefinition.ReadAssembly(path.FullName);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Ignoring '" + path + "': " + e.Message);
            return null;
        }
    }
}
