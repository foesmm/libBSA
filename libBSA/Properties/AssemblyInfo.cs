using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("foesmm.org")]
[assembly: AssemblyProduct("Bethesda Archive Toolkit")]
[assembly: AssemblyCopyright("Copyright © 2018 foesmm.org")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyDescription("Flavor=Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyDescription("Flavor=Release")]
#endif

[assembly: ComVisible(false)]

[assembly: AssemblyTitle("libBSA")]
[assembly: Guid("f5cf3830-f5fb-43b9-8dce-d970757c7ebd")]

[assembly: AssemblyVersion("0.1.0")]
[assembly: AssemblyInformationalVersion("0.1.0")]
[assembly: AssemblyFileVersion("0.0.1.0")]