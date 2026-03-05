# ShowRevitVersion

This simple application displays the Revit file version without opening the file, simply by right-clicking.

RVT and RFA files are supported.

For RVT files, information about "Worksharing" is also displayed if enabled.

The command is integrated into the "Context Menu" via the Windows registry.

![screenshot](image.png)

There are two ways to obtain the version:

- Simply reading the file as text and searching for the xml attribute \<product-version> (for RFA files)

- Opening a file as OLE using the OpenMcdf library and reading a byte stream from BasicFileInfo (for RVT files)

The .Net Framework version: 4.7 (usually installed automatically with Revit).

2026, Alexandr Zuyev
License: MIT