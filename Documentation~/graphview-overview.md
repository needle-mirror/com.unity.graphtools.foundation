# The `GraphView`

The `GraphView` is the element used to show a graphical representation
of the graph, with nodes and edges. It is backed by an implementation
of an `IGraphModel`.

## User interface elements

Graph UI elements derive from `GraphElement`, which derives from
`ModelUI`. This class provides the
base class for every UI element that represent a model. `GraphElement`
is a `ModelUI` that is a direct child of a `GraphView` (for example,
a `Node` or an `Edge`).

Instances of the `ModelUI` class have the following life cycle:

- The `ModelUI` instance is set up by calling `Setup()`. This
  is where the instance is associated with a model and a view.
- UI is built by instantiating child `VisualElement`s. This is
  done by calling `BuildUI()`, which only need to be called when
  the `ModelUI` instance is created or when the UI is fully rebuilt.
- The `ModelUI` instance and its children are updated to reflect changes
  in the underlying model by calling `UpdateFromModel()`.

### Binding the UI to the model

GTF uses factory methods to instantiate the UI for a model. You can find
the default factory methods in the `DefaultFactoryExtensions` class.
Factories are discovered using reflection, so you can provide your
own methods to specialize or override the default one. For example, if
you would like to represent some special node with some special node UI
class, you could write something like this:

```csharp
class MySpecialNodeModel : INodeModel
{
    //...
}

class MySpecialNodeUI : GraphElement /* or Node */
{
    //...
}

[GraphElementsExtensionMethodsCache]
public static class MyFactoryExtensions
{
    public static IModelUI CreateNode(this ElementBuilder elementBuilder,
                                      CommandDispatcher commandDispatcher, MySpecialNodeModel model)
    {
        var ui = new MySpecialNodeUI();
        ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
        return ui;
    }
}
```

On the other hand, if you would like to replace all nodes UI by
your own, you would write something like this (note the difference in
the type of the `model` parameter):

```csharp
class MySpecialNodeUI : GraphElement
{
    //...
}

[GraphElementsExtensionMethodsCache]
public static class MyFactoryExtensions
{
    public static IModelUI CreateNode(this ElementBuilder elementBuilder,
                                      CommandDispatcher commandDispatcher, INodeModel model)
    {
        var ui = new MySpecialNodeUI();
        ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
        return ui;
    }
}
```

The `SetupBuildAndUpdate()` method simply calls `Setup()`, `BuildUI()`
and `UpdateFromModel()`. Subsequently, the GTF update loop will call
`UpdateFromModel()` whenever the model changes, unless a full UI rebuild
is requested. In this case, both `BuildUI()` and `UpdateFromModel()`
would be called.

### Defining the UI

`ModelUI` content is defined using a `ModelUIPartList` which
is a list of `IModelUIPart` filled by the
`protected virtual BuildPartList()` method, called in `Setup()`.
An `IModelUIPart` represents a reusable, replaceable or removable
section of a `ModelUI` that represents a part of the model.
For example, a `Node` has two main parts: the
title and the port container. Here is the `Node.BuildPartList()`
implementation:

```csharp
protected override void BuildPartList()
{
    PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName));
    PartList.AppendPart(PortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
}
```

If you would like your `MySpecialNodeUI` to not show their title,
you can remove the title part from the list by overriding `BuildPartList()`.

```csharp
public class MySpecialNodeUI : Node
{
    protected override void BuildPartList()
    {
        base.BuildPartList();
        PartList.RemovePart(titleContainerPartName);
    }
}
```

Thus, when `MySpecialNodeUI.BuildUI()` is called, only the port
container UI will be built.

If you would like to insert additional `VisualElement`s
(not `ModelUIPart`s) in the UI, you would need to override
`BuildElementUI()`, which is called by `BuildUI()`.

### Updating the UI

When the model changes, GTF updates the `ModelUI` by calling its
`UpdateFromModel()` method, which calls the
`UpdateElementFromModel()` virtual method. If you customized
the UI of a `ModelUI`, you should override this method to update the
various `VisualElement`s in the UI.

`ModelUI.UpdateFromModel()` will also call
`IModelUIPart.UpdateFromModel()` for each part in the `PartList`.
To update the UI of a custom `IModelUIPart`, simply override the
`UpdatePartFromModel() method`.

### Acting on the model using the UI

When the user interacts with instances of `ModelUI`, UI events are
translated to `Command`s that are sent to the `CommandDispatcher`.
Using the `ModelUI.CommandDispatcher` property, you would send a
command from the UI by calling
`CommandDispatcher.Dispatch(new MyCommand(/* command parameters */));`

Upon creation, the
`CommandDispatcher` is configured with command handlers, which
are static methods that receive a `GraphToolState` and a `Command`
as parameters. Their role is to modify the state. Since the state
includes the graph
model, this is how the UI can act on the graph. Command handlers are
also responsible for pushing the current state on the undo stack
by calling `GraphToolState.PushUndo()` and for marking models as changed
using `GraphToolState.MarkNew()`, `GraphToolState.MarkChanged()`
and `GraphToolState.MarkDeleted()`.

Each command handler is associated with a single `Command` class.
GTF provides default command handlers for all `Command`s, but you can
replace them by registering your own handlers to the
`CommandDispatcher`.

When you create new `ModelUI` classes, use `Command`s to handle
user interaction that should result in a modification the
`GraphToolState`. You can use either one of the existing `Command` class
or a new one that you would derive from `Command`.
As an example, here is what a command to change a node color would look
like, including its default command handler.

```csharp
public class ChangeNodeColorCommand : Command
{
    const string k_UndoStringSingular = "Change Node Color";
    const string k_UndoStringPlural = "Change Nodes Color";

    public readonly IReadOnlyList<INodeModel> NodeModels;
    public readonly Color Color;

    public ChangeNodeColorCommand()
    {
        UndoString = k_UndoStringSingular;
    }

    public ChangeNodeColorCommand(Color color,
                                  IReadOnlyList<INodeModel> nodeModels) : this()
    {
        NodeModels = nodeModels;
        Color = color;

        UndoString = (NodeModels?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
    }

    public static void DefaultHandler(GraphToolState state, ChangeNodeColorCommand command)
    {
        state.PushUndo(command);

        if (command.NodeModels != null)
        {
            foreach (var model in command.NodeModels)
            {
                model.Color = command.Color;
            }
            state.MarkChanged(command.NodeModels);
        }
    }
}
```

You would then register this new command by overriding your window's
`RegisterCommandHandlers()` function:

```csharp
public partial class MyWindow : GraphViewWindow
{
    protected override void RegisterCommandHandlers()
    {
        base.RegisterCommandHandlers();
        CommandDispatcher.RegisterCommandHandler<ChangeNodeColorCommand>
            (ChangeNodeColorCommand.DefaultHandler);
    }
}

```

It is important that you do not modify the
tool state (which includes the graph model) without dispatching commands
through the dispatcher. Dispatching commands will ensure a proper
update of the UI and it will trigger notifications to the interested
observers. However, if the user interaction has no impact on the tool
state, do not use a `Command` and respond to the user interaction
as you would usually do.

## The graph model

The graph will be specific to the tool using the Graph Tool Foundation
framework. As such, GTF tries to put as little requirement as possible
on the model. If the tool already has classes that defines the graph
model, the tool can adapt its model to GTF requirements by implementing
the interfaces that are in `Editor/Overdrive/Model/Interfaces`.

GTF also provides a basic model, a set of base classes that a tool
can use to quickly get running with GTF. These classes are in the
`UnityEditor.GraphToolsFoundation.Overdrive.BasicModel` namespace
and they implement all the interfaces required by GTF.

### `INodeModel`

Represents the graph nodes.

#### `IPortNode`

Represents a node with connection points. `IPortNode`s must define
the connection points for the edges (represented by `IPortModel`) in
`IPortModel.DefineNode()` or `BasicModel.NodeModel.OnDefineNode()`.

Other subclasses:

- `IInOutPortsNode`, with differentiated input and output ports.
- `ISingleInputPortNode`, with a single input port.
- `ISingleOutputPortNode`, with a singe output port.
- `IConstantNodeModel`, a `ISingleOutputPortNode` associated with a `IConstant`.
- `IVariableNodeModel`, a node associated with a `IVariableDeclarationModel`

### `IEdgeModel`

Represents the graph edges. Edges are connected to `IPortModel`.

- IEditableEdge is a routable edge, with control points (`IEdgeControlPointModel`).

### `IPortModel`

Connection points for the edges. Owned by a `IPortNode`.

- `IReorderableEdgesPort`, a port for which the connected edges can be reordered.

### `IConstant`

Represents an embedded, typed value.

### `IVariableDeclarationModel`

Represents an external typed value. When evaluating the graph, the
value from a `IVariableDeclarationModel` typically depends from
something outside the graph.

### `IPlacematModel`

Represents a placemat, an element used to group other element. The
grouping is not explicit (placemats do not contain a list of elements);
it is done using the position of the elements: every element that
intersects the placemat rectangle belongs to the placemat.

### `IStickyNoteModel`

Represents a positioned text that can be used to annotate the graph.

### `IBadgeModel`

Badges are elements that are attached to a parent model. In GTF, they
are used to attach error messages to nodes and to display the
values on ports when the graph is evaluated.

### Portals

Portals are used to replace edges, in order to unclutter the graph.
An edge between two very distant nodes can be replaced by two
edges: one from the origin node to an `IEdgePortalEntryModel` and a
another from a `IEdgePortalExitModel` to the destination node.
Your model should treat the portals as things that creates
an invisible connection between the `IEdgePortalEntryModel`
and the `IEdgePortalExitModel`. Portals that share the same
`IDeclarationModel` are all connected together.

### `IGraphModel`

Represents the graph. Holds lists of nodes, edges, badges, sticky notes,
placemats, variable declaration (for `IVariableNodeModel`)
and portal declaration (for portals). In GTF, all elements creation and
deletion are done through the graph model.

### `IGraphAssetModel`

The asset used to serialize the `IGraphModel`. Saved as a file in
the `Asset` folder of the Unity project.

### `IBlackboardGraphModel`

Represents a blackboard. In GTF, the blackboard is a side panel where
the user can create `IVariableDeclarationModel` (which are the external
inputs and outputs of the graph). Once created,
`IVariableDeclarationModel` are displayed in one of the blackboard
sections; the user can then drag and drop them into the graph view.
