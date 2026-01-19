# Binding Redirect Generator
A tool to generate binding redirects from assemblies in a given path.
from the actual assemblies in the given path.
- leaves existing Redirects alone. 
- add Redirects for all other DLLs and EXEs... 
- and sets the Version-Range to (2^32)-1 = 65535, so `0.0.0.0-65535.65535.65535.65535`
- set the Version to the one found in the corresponding `.dll`. 

## Command Line Parameters: 

```
BindingRedirectGenerator.exe <configFilePath> [<searchDirectoryPath>]
```

1. Parameter: configFilePath = Path to the app.config/web.config 
2. Parameter: optional searchDirectoryPath = Path to the bin Directory (fallback to the app.config Directory and subdirectories) 

The `searchDirectoryPath` is optional and defaults to the Directory of the `configFilePath` and all subdirectories. 
This works for both `App.config` which is typically in the same  directory as the assemblies
and for `Web.config` where the assemblies are in the `bin` subdirectory.

## optimized usage in Windows 

The optional 2nd Parameter allows to associate the `*.config` Extension with the `BindingRedirectGenerator.exe`
and generate the redirects by simply double-clicking the the `Web.config` or `app.config`.


