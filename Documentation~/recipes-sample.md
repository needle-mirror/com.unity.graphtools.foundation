# The Recipes Sample

The complete project is in the `Samples/Recipes` directory.

In this section, we will build a simple graph tool to describe food
recipes. A recipe combines ingredients and cookware using nodes
that describe what to do. For example, to prepare a glass of salted
water, we could have a `Mix` node that takes a glass, water and salt
as inputs and makes the result available as its output.

The simplest way to get started with Graph Tools Foundation is to use
the basic model provided in the `Editor/Overdrive/Model/BasicModel`
directory and derive from that. That is what we will do in this sample.

## The Stencil

The stencil is used to configure various behaviors of the graph tool.
Each tool should have its own stencil class, so let's derive one
from `Stencil`. Since the stencil is instantiated by reflection,
we need to define a default constructor for it. For convenience,
we will also define there how we will call our graphs.

```csharp
public partial class RecipeStencil : Stencil
{
    public static readonly string graphName = "Recipe";

    // ReSharper disable once EmptyConstructor
    public RecipeStencil() { }
}
```

Since we would like to differentiate between ingredients and
cookware (a user shall not be able to connect an ingredient where a
cookware is required, and vice versa), let's define types for these
two concepts.

```csharp
public partial class RecipeStencil
{
    public static TypeHandle Ingredient { get; } =
        TypeSerializer.GenerateCustomTypeHandle("Ingredient");

    public static TypeHandle Cookware { get; } =
        TypeSerializer.GenerateCustomTypeHandle("Cookware");
}
```

We have put our type definition in the stencil, but they could have
been anywhere.

## The Graph Editor Window, the Graph Model and the Graph Asset

We will need a graph view and a window to display our
graphs. For this, we simply derive a new class from GTF base classes.

```csharp
public class RecipeGraphView : GraphView
{
    public RecipeGraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher)
        : base(window, commandDispatcher) { }
}

public partial class RecipeGraphWindow : GraphViewEditorWindow
{
    [MenuItem("GTF Samples/Recipe Editor", false)]
    public static void ShowRecipeGraphWindow()
    {
        FindOrCreateGraphWindow<RecipeGraphWindow>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        titleContent = new GUIContent("Recipe Editor");
    }

    protected override GraphView CreateGraphView()
    {
        return new RecipeGraphView(this, CommandDispatcher);
    }
}
```

We need a `GraphModel`, which is the model displayed in the
`GraphView`. Let's derive it from the basic graph model.

```csharp
public partial class RecipeGraphModel : GraphModel
{
}
```

In GTF, the variables, inputs and outputs of a graph are defined in a tool we
call the *Blackboard*. Once defined, variables, inputs and outputs can be dragged
and dropped in the graph view, instantiating nodes for them.
To populate the blackboard, we need a `BlackboardGraphModel`.
It will define the sections of the blackboard and how to create the
variables, inputs and outputs. In our case, the inputs will be the ingredients and
cookware (we will not bother about outputs nor variables).

```csharp
public class RecipeBlackboardGraphModel : BlackboardGraphModel
{
    static readonly string[] k_Sections = { "Ingredients", "Cookware" };

    public override string GetBlackboardTitle()
    {
        return AssetModel?.FriendlyScriptName == null ? "Recipe" :
            AssetModel?.FriendlyScriptName + " Recipe";
    }

    public override string GetBlackboardSubTitle()
    {
        return "The Pantry";
    }

    public override IEnumerable<string> SectionNames =>
        GraphModel == null ? Enumerable.Empty<string>() : k_Sections;

    public override IEnumerable<IVariableDeclarationModel>
        GetSectionRows(string sectionName)
    {
        if (sectionName == k_Sections[0])
        {
            return GraphModel?.VariableDeclarations?.Where(
                v => v.DataType == RecipeStencil.Ingredient) ??
                Enumerable.Empty<IVariableDeclarationModel>();
        }

        if (sectionName == k_Sections[1])
        {
            return GraphModel?.VariableDeclarations?.Where(
                v => v.DataType == RecipeStencil.Cookware) ??
                Enumerable.Empty<IVariableDeclarationModel>();
        }

        return Enumerable.Empty<IVariableDeclarationModel>();
    }

    public override void PopulateCreateMenu(string sectionName, GenericMenu menu,
                                        CommandDispatcher commandDispatcher)
    {
        if (sectionName == k_Sections[0])
        {
            menu.AddItem(new GUIContent("Add"), false, () =>
            {
                CreateVariableDeclaration(commandDispatcher,
                    RecipeStencil.Ingredient.Identification, RecipeStencil.Ingredient);
            });
        }
        else if (sectionName == k_Sections[1])
        {
            menu.AddItem(new GUIContent("Add"), false, () =>
            {
                CreateVariableDeclaration(commandDispatcher,
                    RecipeStencil.Cookware.Identification, RecipeStencil.Cookware);
            });
        }
    }

    static void CreateVariableDeclaration(
        CommandDispatcher commandDispatcher, string name, TypeHandle type)
    {
        var finalName = name;
        var i = 0;

        while (commandDispatcher.GraphToolState.GraphModel.
                VariableDeclarations.Any(v => v.Title == finalName))
            finalName = name + i++;

        commandDispatcher.Dispatch(
            new CreateGraphVariableDeclarationCommand(finalName, true, type));
    }
}
```

Having defined the models, we need an asset to serialize the graph
and store it in our Unity project.
We derive a class from `GraphAssetModel`; the new class defines
the type of `GraphModel` to use, which will be our `RecipeGraphModel`,
and initialize the `BlackboardGraphModel`:

```csharp
public partial class RecipeGraphAssetModel : GraphAssetModel
{
    protected override Type GraphModelType => typeof(RecipeGraphModel);
    public override IBlackboardGraphModel BlackboardGraphModel { get; }

    public RecipeGraphAssetModel()
    {
        BlackboardGraphModel = new RecipeBlackboardGraphModel { AssetModel = this };
    }
}
```

Let's add a couple of methods to create graph assets from the
*Asset/Create* menu and to open graph assets in our window:

```csharp
public partial class RecipeGraphAssetModel
{
    [MenuItem("Assets/Create/Recipe")]
    public static void CreateGraph(MenuCommand menuCommand)
    {
        const string path = "Assets";
        var template = new GraphTemplate<RecipeStencil>(RecipeStencil.graphName);
        CommandDispatcher commandDispatcher = null;
        if (EditorWindow.HasOpenInstances<RecipeGraphWindow>())
        {
            var window = EditorWindow.GetWindow<RecipeGraphWindow>();
            if (window != null)
            {
                commandDispatcher = window.CommandDispatcher;
            }
        }

        GraphAssetCreationHelpers<RecipeGraphAssetModel>.
            CreateInProjectWindow(template, commandDispatcher, path);
    }

    [OnOpenAsset(1)]
    public static bool OpenGraphAsset(int instanceId, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceId);
        if (obj is RecipeGraphAssetModel)
        {
            string path = AssetDatabase.GetAssetPath(instanceId);
            var asset = AssetDatabase.LoadAssetAtPath<RecipeGraphAssetModel>(path);
            if (asset == null)
                return false;

            var window = GraphViewEditorWindow.
                FindOrCreateGraphWindow<RecipeGraphWindow>();
            return window != null;
        }

        return false;
    }
}
```

With this code, we can go in the Unity Editor, create a graph asset
and open it in our graph editor window. Right-clicking in the graph
view makes a contextual menu appear; we can already create a placemat
(an element used to group nodes) but nothing happens if we try to create
nodes. Let's see how we can fix that.

## The Nodes

When selecting the *Create Node* item in the contextual menu.
you invoke the Searcher, which list the node types it knows. So in
addition to defining node types, we will need to tell the searcher
to add them to its list.

Unless overriden in the stencil class, Graph Tools Foundation
uses the `DefaultSearcherDatabaseProvider` to tell the searcher what to
list. This provider returns a database containing information about
the Sticky Note and about all nodes that have the
`SearcherItemAttribute`.

Knowing this, we can now define a `Fry` node types,
decorated with the `SearcherItem` attribute.

```csharp
[Serializable]
[SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Cooking/Fry")]
public partial class FryNodeModel : NodeModel
{
}
```

Now, if we go in the editor, we will see that we can create *Fry* nodes
from the searcher.

## Linking Nodes Together

Now that we have nodes, we need to represent relationship between them
using edges. In Graph Tools Foundation, nodes need to define attachment
points for edges. These attachment points are called `Port`s. They
are typed because typically, edges are used to communicate a value
from one node to another.

Let's add some ports to our node to define what it needs to accomplish
its task.

```csharp
public partial class FryNodeModel
{
    protected override void OnDefineNode()
    {
        base.OnDefineNode();

        AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware,
            options: PortModelOptions.NoEmbeddedConstant);

        AddInputPort("Ingredients", PortType.Data, RecipeStencil.Ingredient,
            options: PortModelOptions.NoEmbeddedConstant);
        AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient,
            options: PortModelOptions.NoEmbeddedConstant);
    }
}
```

By default, there is no type checking when attempting to connect ports.
We would like to restrict the connections to what makes
sense in our model. For this, we need to override
`GraphModel.IsCompatiblePort()`. For the sake of simplicity,
we will only check if the port types are the same, but more
elaborate checks could be done.

```csharp
public partial class RecipeGraphModel
{
    protected override bool IsCompatiblePort(
        IPortModel startPortModel, IPortModel compatiblePortModel)
    {
        return startPortModel.DataTypeHandle == compatiblePortModel.DataTypeHandle;
    }
}
```

Using the blackboard, we can now define an *Egg*
ingredient and a *Frying Pan* cookware. Dragging these inputs in the
graph view will create nodes that can be connected to the *Fry* node
to create fried eggs.

## Adding an initial view

In the current state of things, people using our tool need to first create
a Recipe Graph in the `Assets/Create` menu and then double-click on
it to edit it. If they try to open the **Recipe Editor** window
without any graph selected, they will see a blank, unresponsive window.

When the window is opened without any graph selected, it shows a
`BlankPage`, which is a `VisualElement` that contains the UI of the
available `OnboardingProvider`, if any. So, to display
a UI to create a new graph, we need to implement
an `OnboardingProvider` that displays a UI to create a new recipe.

```csharp
public class RecipeOnboardingProvider : OnboardingProvider
{
    public override VisualElement CreateOnboardingElements(
        CommandDispatcher commandDispatcher)
    {
        var template = new GraphTemplate<RecipeStencil>(RecipeStencil.graphName);
        return AddNewGraphButton<RecipeGraphAssetModel>(template);
    }
}
```

Then, we modify our window to pass our `RecipeOnboardingProvider` to the
`BlankPage` when creating a blank page.

```csharp
public partial class RecipeGraphWindow
{
    protected override BlankPage CreateBlankPage()
    {
        var onboardingProviders = new List<OnboardingProvider>();
        onboardingProviders.Add(new RecipeOnboardingProvider());

        return new BlankPage(CommandDispatcher, onboardingProviders);
    }
}
```

### Nodes with properties

You will find that sometimnes, a node needs properties to parametrize
its action. This can be achieved by adding serialized fields to the
node.

```csharp
[Serializable]
[SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Cooking/Bake")]
public class BakeNodeModel : NodeModel
{
    [SerializeField]
    int m_TemperatureC = 180;
    [SerializeField]
    int m_Minutes = 60;

    public int Temperature
    {
        get => m_TemperatureC;
        set => m_TemperatureC = value;
    }

    public int Duration
    {
        get => m_Minutes;
        set => m_Minutes = value;
    }

    protected override void OnDefineNode()
    {
        base.OnDefineNode();

        AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware,
            options: PortModelOptions.NoEmbeddedConstant);

        AddInputPort("Ingredients", PortType.Data, RecipeStencil.Ingredient,
            options: PortModelOptions.NoEmbeddedConstant);
        AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient,
            options: PortModelOptions.NoEmbeddedConstant);
    }
}
```

All serializable fields will be displayed in the local node inspector,
unless they have the `HideInInspectorAttribute`.

### Nodes with custom UI

In this section, we describe how you can create customized UI
for a node model type. We will add some UI elements to the *Bake* node
to display the temperature and duration, instead of having to rely
on the node inspector.

Since we also want the users to be able to edit the values, we will
use `EditableLabel`s to show the value. We will listen for `ChangeEvent` on
the editable labels and dispatch commands whenever we receive the event. The
commands we will send are:

```csharp
public class SetTemperatureCommand : ModelCommand<BakeNodeModel, int>
{
    const string k_UndoStringSingular = "Set Bake Node Temperature";
    const string k_UndoStringPlural = "Set Bake Nodes Temperature";

    public SetTemperatureCommand(BakeNodeModel[] nodes, int value)
        : base(k_UndoStringSingular, k_UndoStringPlural, nodes, value)
    {
    }

    public static void DefaultHandler(GraphToolState state, SetTemperatureCommand command)
    {
        state.PushUndo(command);

        foreach (var nodeModel in command.Models)
        {
            nodeModel.Temperature = command.Value;
            state.MarkChanged(nodeModel);
        }
    }
}

public class SetDurationCommand : ModelCommand<BakeNodeModel, int>
{
    const string k_UndoStringSingular = "Set Bake Node Duration";
    const string k_UndoStringPlural = "Set Bake Nodes Duration";

    public SetDurationCommand(BakeNodeModel[] nodes, int value)
        : base(k_UndoStringSingular, k_UndoStringPlural, nodes, value)
    {
    }

    public static void DefaultHandler(GraphToolState state, SetDurationCommand command)
    {
        state.PushUndo(command);

        foreach (var nodeModel in command.Models)
        {
            nodeModel.Duration = command.Value;
            state.MarkChanged(nodeModel);
        }
    }
}
```

These commands and their handlers need to be registered to the
`CommandDispatcher` in `RecipeGraphWindow.RegisterCommandHandlers()` method:

```charp
public partial class RecipeGraphWindow
{
    protected override void RegisterCommandHandlers()
    {
        base.RegisterCommandHandlers();

        CommandDispatcher.RegisterCommandHandler<SetTemperatureCommand>(
            SetTemperatureCommand.DefaultHandler);
        CommandDispatcher.RegisterCommandHandler<SetDurationCommand>(
            SetDurationCommand.DefaultHandler);
    }
}
```

Now, let's create a UI part to display the additional information and dispatch the commands
when we receive a `ChangeEvent` on an `EditableLabel`. The `BuildPartUI()` method instantiates
and sets up the `VisualElement`s that make up our UI. The `PostBuildPartUI()` method adds
a USS file. This is done in the post-build step to ensure consistent ordering of USS files
when UI parts are made of other parts. This way, USS files of parent parts have precedence
over those of their children. The `UpdatePartFromModel()` method is where we update the UI
to reflect model changes.

```csharp
public class TemperatureAndTimePart : BaseModelUIPart
{
    public static readonly string ussClassName = "ge-sample-bake-node-part";
    public static readonly string temperatureLabelName = "temperature";
    public static readonly string durationLabelName = "duration";

    public static TemperatureAndTimePart Create(string name, IGraphElementModel model,
        IModelUI modelUI, string parentClassName)
    {
        if (model is INodeModel)
        {
            return new TemperatureAndTimePart(name, model, modelUI, parentClassName);
        }

        return null;
    }

    VisualElement TemperatureAndTimeContainer { get; set; }
    EditableLabel TemperatureLabel { get; set; }
    EditableLabel DurationLabel { get; set; }

    public override VisualElement Root => TemperatureAndTimeContainer;

    TemperatureAndTimePart(string name, IGraphElementModel model, IModelUI ownerElement,
        string parentClassName) : base(name, model, ownerElement, parentClassName)
    {
    }

    protected override void BuildPartUI(VisualElement container)
    {
        if (!(m_Model is BakeNodeModel))
            return;

        TemperatureAndTimeContainer = new VisualElement { name = PartName };
        TemperatureAndTimeContainer.AddToClassList(ussClassName);
        TemperatureAndTimeContainer.AddToClassList(
            m_ParentClassName.WithUssElement(PartName));

        TemperatureLabel = new EditableLabel { name = temperatureLabelName };
        TemperatureLabel.RegisterCallback<ChangeEvent<string>>(OnChangeTemperature);
        TemperatureAndTimeContainer.Add(TemperatureLabel);

        DurationLabel = new EditableLabel { name = durationLabelName };
        DurationLabel.RegisterCallback<ChangeEvent<string>>(OnChangeTime);
        TemperatureAndTimeContainer.Add(DurationLabel);

        container.Add(TemperatureAndTimeContainer);
    }

    protected override void PostBuildPartUI()
    {
        base.PostBuildPartUI();

        var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Packages/com.unity.graphtools.foundation/Samples/Recipes/Editor/UI/Stylesheets/BakeNodePart.uss");

        if (stylesheet != null)
        {
            TemperatureAndTimeContainer.styleSheets.Add(stylesheet);
        }
    }

    void OnChangeTemperature(ChangeEvent<string> evt)
    {
        if (!(m_Model is BakeNodeModel bakeNodeModel))
            return;

        if (int.TryParse(evt.newValue, out var v))
            m_OwnerElement.CommandDispatcher.Dispatch(
                new SetTemperatureCommand(new[] { bakeNodeModel }, v));
    }

    void OnChangeTime(ChangeEvent<string> evt)
    {
        if (!(m_Model is BakeNodeModel bakeNodeModel))
            return;

        if (int.TryParse(evt.newValue, out var v))
            m_OwnerElement.CommandDispatcher.Dispatch(
                new SetDurationCommand(new[] { bakeNodeModel }, v));
    }

    protected override void UpdatePartFromModel()
    {
        if (!(m_Model is BakeNodeModel bakeNodeModel))
            return;

        TemperatureLabel.SetValueWithoutNotify($"{bakeNodeModel.Temperature} C");
        DurationLabel.SetValueWithoutNotify($"{bakeNodeModel.Duration} min.");
    }
}
```

The `BakeNodePart.uss` stylesheet contains these simple USS rules putting the two fields
side by side:

```css
.ge-sample-bake-node-part {
    flex-direction: row;
}

.ge-sample-bake-node-part .ge-editable-label {
    flex-grow: 1;
}
```

Having defined the new UI part, we are ready to add it to a new node UI class:

```csharp
class BakeNode : CollapsibleInOutNode
{
    public static readonly string paramContainerPartName = "parameter-container";

    protected override void BuildPartList()
    {
        base.BuildPartList();

        PartList.InsertPartAfter(titleIconContainerPartName,
           TemperatureAndTimePart.Create(paramContainerPartName, Model, this, ussClassName));
    }
}
```

Finally, we add a factory method to create `BakeNode` for `BakeNodeModel`:

```csharp
[GraphElementsExtensionMethodsCache]
public static partial class RecipeUIFactoryExtensions
{
    public static IModelUI CreateNode(this ElementBuilder elementBuilder,
        CommandDispatcher store, BakeNodeModel model)
    {
        IModelUI ui = new BakeNode();
        ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView,
            elementBuilder.Context);
        return ui;
    }
}
```

UI factories are discovered by GTF using reflection. The
`GraphElementsExtensionMethodsCache` attribute makes a static class
discoverable in this process. All methods within the class that match
the signature `public static IModelUI Foo(this ElementBuilder, CommandDispatcher, ModelType)`
will be considered a UI factory method for model types that are of type or
derive from type `ModelType`.

### Node with variable number of ports

A frequent case in graphs is a node that can takes any number of inputs.
In this section, we show you how you can define a node model for this case and use
custom node UI to represent such nodes.

First let's define the *Mix* node model, that can mix two or more
ingredients together:

```csharp
[Serializable]
[SearcherItem(typeof(RecipeStencil), SearcherContext.Graph, "Preparation/Mix")]
public class MixNodeModel : NodeModel
{
    [SerializeField, HideInInspector]
    int m_IngredientCount = 2;

    protected override void OnDefineNode()
    {
        base.OnDefineNode();

        AddInputPort("Cookware", PortType.Data, RecipeStencil.Cookware,
            options: PortModelOptions.NoEmbeddedConstant);

        for (var i = 0; i < m_IngredientCount; i++)
        {
            AddInputPort("Ingredient " + (i + 1), PortType.Data, RecipeStencil.Ingredient,
                options: PortModelOptions.NoEmbeddedConstant);
        }

        AddOutputPort("Result", PortType.Data, RecipeStencil.Ingredient,
            options: PortModelOptions.NoEmbeddedConstant);
    }

    public void AddIngredientPort()
    {
        m_IngredientCount++;
        DefineNode();
    }

    public void RemoveIngredientPort()
    {
        m_IngredientCount--;
        if (m_IngredientCount < 2)
            m_IngredientCount = 2;

        DefineNode();
    }
}
```

We then need to alter the UI to enable the user to add and remove
ingredient ports. Since in GTF, all model alteration need to be done
using `Command`s, let's define them along with their default handlers:

```csharp
public class AddPortCommand : ModelCommand<MixNodeModel>
{
    const string k_UndoStringSingular = "Add Ingredient";

    public AddPortCommand(MixNodeModel[] nodes)
        : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
    {
    }

    public static void DefaultHandler(GraphToolState state, AddPortCommand command)
    {
        state.PushUndo(command);

        foreach (var nodeModel in command.Models)
        {
            nodeModel.AddIngredientPort();
            state.MarkChanged(nodeModel);
        }
    }
}

public class RemovePortCommand : ModelCommand<MixNodeModel>
{
    const string k_UndoStringSingular = "Remove Ingredient";

    public RemovePortCommand(MixNodeModel[] nodes)
        : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
    {
    }

    public static void DefaultHandler(GraphToolState state, RemovePortCommand command)
    {
        state.PushUndo(command);

        foreach (var nodeModel in command.Models)
        {
            nodeModel.RemoveIngredientPort();
            state.MarkChanged(nodeModel);
        }
    }
}
```

Like we did did for the `SetDurationCommand` and `SetTemperatureCommand`, we need to register
these commands and their handler in `RecipeGraphWindow.RegisterCommandHandlers()`:

```csharp
public partial class RecipeGraphWindow
{
    protected override void RegisterCommandHandlers()
    {
        base.RegisterCommandHandlers();

        CommandDispatcher.RegisterCommandHandler<AddPortCommand>(
            AddPortCommand.DefaultHandler);
        CommandDispatcher.RegisterCommandHandler<RemovePortCommand>(
            RemovePortCommand.DefaultHandler);

        CommandDispatcher.RegisterCommandHandler<SetTemperatureCommand>(
            SetTemperatureCommand.DefaultHandler);
        CommandDispatcher.RegisterCommandHandler<SetDurationCommand>(
            SetDurationCommand.DefaultHandler);
    }
}
```

Then, we derive a class for the node UI,
where we add new items in the node's contextual menu. These items will
dispatch the commands we just defined.

```csharp
class VariableIngredientNode : CollapsibleInOutNode
{
    protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);

        if (!(Model is MixNodeModel mixNodeModel))
        {
            return;
        }

        if (evt.menu.MenuItems().Count > 0)
            evt.menu.AppendSeparator();

        evt.menu.AppendAction($"Add Ingredient", action: action =>
        {
            CommandDispatcher.Dispatch(new AddPortCommand(new[] { mixNodeModel }));
        });

        evt.menu.AppendAction($"Remove Ingredient", action: action =>
        {
            CommandDispatcher.Dispatch(new RemovePortCommand(new[] { mixNodeModel }));
        });
    }
}
```

Finally, like for the `BakeNode`, we add a factory method to create a
`VariableIngredientNode` for a `MixNodeModel`:

```csharp
[GraphElementsExtensionMethodsCache]
public static partial class RecipeUIFactoryExtensions
{
    public static IModelUI CreateNode(this ElementBuilder elementBuilder,
        CommandDispatcher store, MixNodeModel model)
    {
        IModelUI ui = new VariableIngredientNode();
        ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView,
            elementBuilder.Context);
        return ui;
    }
}
```
