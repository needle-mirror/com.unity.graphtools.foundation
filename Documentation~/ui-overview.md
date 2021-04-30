# UI overview

## The `GraphViewEditorWindow`

The `GraphViewEditorWindow` is an `EditorWindow` that contains one or more
views on the application state. It also holds the `CommandDispatcher` and
the `GraphToolState`.

## The views

Views are `VisualElement`s associated with a `StateObserver`. They display
some part of the application state. For example, the `GraphView` displays
the graph model using nodes and edges. Another view, the `Blackboard`,
displays the list of variables defined in the graph model.

Views children are `ModelUI` instances.

## The `ModelUI` elements

`ModelUI` is the base class for all `VisualElement`s that displays some
model from the application state. The definition of *model* is highly
subjective, but it is usually some self-contained piece of data from the
application state, for example, a node or an edge.

A `ModelUI` is a container for all the `VisualElement`s needed
to represent a model. To help customization, most `ModelUI` are built using
`BaseModelUIPart`s, which are blueprints to create a subset of the elements
needed by the `ModelUI`.
Usually, a single `BaseModelUIPart` is responsible for displaying some particular
part of the model. For example the `Node` is a `ModelUI` that is built
using two parts, one for the title and one for the ports.

It is important to understand that `BaseModelUIPart`s are not `VisualElement`s
themselves, but are used to instantiate `VisualElement`s.

Parts are meant
to be replaceable: if a tool developer wishes to display the node title
using different `VisualElement`s, she can do so by replacing the node's
title part by her own title part.

`ModelUI` styling is done with USS stylesheets; customizing the look of the
UI is done by adding a stylesheet to the `ModelUI`.

## UI Factory Methods

The UI for a model is instantiated using factory methods. GTF defines
a set of factory methods in `DefaultFactoryExtensions`. As an example,
the factory method to build an edge's `ModelUI` looks like this:

```csharp
public static IModelUI CreateEdge(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IEdgeModel model)
{
    var ui = new Edge();
    ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
    return ui;
}
```

The last parameter of the method defines the model class for which it is used.
If a tool developer wishes to replace GTF factory method by her own, she simply
needs to define a method having the same signature:

```csharp
[GraphElementsExtensionMethodsCache]
public static class MyFactoryExtensions
{
    public static IModelUI MyCreateEdge(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IEdgeModel model)
    {
        //...
    }
}
```

If, on the other hand, she wishes to provide a factory method for a particular
specialization of the edge model, say `class MyEdgeModel : IEdgeModel`,
she would needs to define a method that receives a `MyEdgeModel` as its last parameter:

```csharp
[GraphElementsExtensionMethodsCache]
public static class MyFactoryExtensions
{
    public static IModelUI MyCreateEdge(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, MyDirectedEdgeModel model)
    {
        var ui = new Edge();
        Setup(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.GraphView);

        // Add a part to display the edge direction
        ui.PartList.AppendPart(new MyEdgeDirectionPart());

        ui.BuildUI();
        
        // Add our own stylesheet
        ui.AddStylesheet("directedEdge.uss");
        
        ui.UpdateFromModel();
        return ui;
    }
}
```

Defining a new factory method is a simple way to customize the UI for a model:
the new method can either instantiate its own `ModelUI` derived class, or it can
customize an existing one by modifying the part list and adding stylesheets,
like in the example above.

## `ModelUI` lifecycle

The first time a view is updated, it must create all its content. At this moment,
it needs to examine the model and call `GraphElementFactory.CreateUI<GraphElement>()`
for each model that needs to be displayed in the view. This has the effect of
selecting a factory method and calling it. Most factory methods instantiate a new
`ModelUI` and call `ModelUI.SetupBuildAndUpdate()`. This method is simply a
shortcut to call three methods:

- `Setup(model, commandDispatcher, graphView, context)`, which initializes some fields and
  builds the part list. As a reminder, a `BaseModelUIPart` is not a `VisualElement`, but rather
  a blueprint to instantiate `VisualElement`s.
- `BuildUI()`, which instantiates `VisualElement`s, including those of the parts in the part list.
- `UpdateFromModel()`, which updates the `VisualElement`s according to the data in the model.

On subsequent updates, whenever possible, views are not rebuilt but updated and
only `ModelUI.UpdateFromModel()` is called.

When the underlying model is deleted, the `ModelUI` needs to be removed from the
`VisualElement` tree by calling `ModelUI.RemoveFromHierarchy()` and disposed of
by calling `ModelUI.RemoveFromGraphView()` (yes, name is bad).
