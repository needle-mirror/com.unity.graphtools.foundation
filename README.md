# Visual Scripting

This is the Visual Scripting package.

Some documentation will be written at some point.

## Getting started

If you want to start developing on the Visual Scripting project, follow these easy steps:

1. Get yourself a Unity build based on `editor\visualscripting\staging`.
2. Fork the Visual Scripting repo at https://gitlab.internal.unity3d.com/upm-packages/visual-scripting/com.unity.visualscripting.
3. Clone the forked repo locally (say to `C:\code\my-amazing-vs-repo`).
4. Start the Unity build mentioned at step and create yourself a new project.
5. Make sure your project in a .Net 4.x project by changing the player setting found at `Menu` | `Edit` | `Project Settings` | `Player`.
6. In the project, under the `Packages` folder, there should be a `manifest.json` file. In there, enter the following (for Windows users, notice that we're using _forward slashes_ for path separator in the json file):

```json
{
    "dependencies": {
        "com.unity.graphtools.foundation": "file:C:/code/my-amazing-vs-repo"
    },
    "testables" : [ "com.unity.graphtools.foundation" ],
    "registry": "https://staging-packages.unity.com"
}
```

You should now be able to play with Visual Scripting in your project.
