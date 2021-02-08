# The GraphTools Foundation Package

Graph Tools Foundation is a framework to build graph editing tools
including a graph data model, a UI
foundation and graph-to-asset pipeline. Use this package to speed up
the development of graph based tools for the Unity Editor that adhere
to Unity UI and UX guidelines.

This package is available as a pre-release package, so it is still in
the process of becoming stable enough to release. The features and
documentation in this package might change before it is ready for release.

To use this package in a project, you need to manually edit the `manifest.json`
file of the project:

- add `"com.unity.graphtools.foundation": "0.6.0-preview"` (or latest version)
  in the `dependencies` section;
- add `"registry": "https://artifactory.prd.cds.internal.unity3d.com/artifactory/api/npm/upm-candidates"`
  in the top level section.

Then, create an **Assembly Definition** file (if there isn't already one).
Select it to edit it in the Inspector window. In the
**Assembly Definition References** section, add a reference to
`Unity.GraphTools.Foundation.Overdrive.Editor`.

To add this package as a dependency to your own package, add
`"com.unity.graphtools.foundation": "0.6.0-preview"`  (or latest version)  in the
`dependencies` section of your `package.json` file and configure
an *Assembly Definition** file as explained above.

<!--
To install this package, follow the instructions in the
[Package Manager documentation](https://docs.unity3d.com/Manual/upm-ui-install.html).
-->

## Package contents

This packages contains two sets of assembly: the Overdrive assemblies
and the non-Overdrive assemblies.

The non-overdrive assemblies are obsolete and should *not* be used in
any new project. They are undocumented and we expect to remove them as
soon as possible. They are:

- Unity.GraphTools.Foundation
- Unity.GraphTools.Foundation.Editor
- Unity.InternalAPIEngineBridgeDev.002

The Overdrive assemblies are the one that should be used. They are:

- Unity.GraphTools.Foundation.Overdrive.Editor
- Unity.InternalAPIEngineBridgeDev.003

This documentation only applies the Overdrive assemblies.

The following table describes the package folder structure:

|**Location**|**Description**|
|---|---|
|*Documentation~*|Contains the package documentation.|
|*Editor/Overdrive*|Contains the GTF editor assembly.|
|*Editor/Overdrive/InternalBridge*|Contains an assembly used to access some Unity Editor internals.|
|*Tests/Editor/Overdrive*|Contains tests for the package.|

## Requirements

This version of GraphTools Foundation Package is compatible with the following versions of the Unity Editor:

* 2020.1
