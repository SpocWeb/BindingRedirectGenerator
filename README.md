# Binding Redirect Generator
A tool to generate missing binding redirects in a `Web.config` or `app.config` 
from the actual assemblies in the given path.

# usage 

```
BindingRedirectGenerator.exe <configFilePath> [<searchDirectoryPath>]
```

The `searchDirectoryPath` is optional and defaults to the Directory of the `configFilePath` and all subdirectories. 
This works for both `App.config` which is typically in the same  directory as the assemblies
and for `Web.config` where the assemblies are in the `bin` subdirectory.

# optimized usage 

By associating the `*.config` extension with the `BindingRedirectGenerator.exe`
the patching would be performed on a simple double-click on the `Web.config` or `app.config`.

