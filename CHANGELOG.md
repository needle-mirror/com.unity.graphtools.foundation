# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.7.0-preview.2] - 2021-01-18

### Fixed

- Fixed compilation error on Unity 2020.2 and newer.

## [0.7.0-preview.1] - 2021-01-11

### Fixed

- Remove orphaned .meta files for empty folders.

## [0.7.0-preview] - 2021-01-05

### Added

- `Store.MarkStateDirty` to dirty the state and rebuild the UI completely.
- `Store.BeginStateChange` and `Store.EndStateChange` to frame modifications to models. Except inside action reducers (where this
  is done for you), all calls to `State.MarkModelNew`, `State.MarkModelChanged` and `State.MarkModelDeleted` should occur between
  `Store.BeginStateChange` and `Store.EndStateChange`.
- `Store.BeginViewUpdate` and `Store.EndViewUpdate`. These should be the first and last operations when you update the UI.
  `GtfoWindow.Update` call them for you.
- Dependency system for UI: a graph element can declare dependencies to other graph elements. They can be forward dependencies
  (elements that need to be updated when the element changes) or reverse dependencies (elements that cause the element to be updated
  when they change). There are also additional model dependencies: a graph element can specifies it needs an update whenever some model changes.
- Graph element parts are notified when their owner is added or removed from the graph view, by calls to
  `BaseGraphElementPart.PartOwnerAddedToGraphView` and `BaseGraphElementPart.PartOwnerRemovedFromGraphView`.
- `IGraphElement.Setup` and `IGraphElement.SetupBuildAndUpdate` now take an additional context parameter, a string that can be used
  to modulate the UI. This parameters is most often null, but can take different values to specify the instantiation point
  of the UI. The goal is to specify a context for the model representation, when we need to use different graph elements for the same
  model type represented in different parts of the graph view.
- `GraphElement.AddToGraphView` and `GraphElement.RemoveFromGraphView` are called when an element is added or removed from the graph view.
- `ChangeVariableDeclarationAction`, sent when the user changes the variable declaration of a variable node.
- `RequestCompilationAction`, sent when the user request a compilation.
- Polymorphic `AnyConstant`
- `AnimationClipConstant`, `MeshConstant`, `Texture2DConstant` and `Texture3DConstant`

### Removed

- `State.AddModelToUpdate`. Use `State.MarkModelNew`, `State.MarkModelChanged` and `State.MarkModelDeleted` instead.
- `State.ClearModelsToUpdate`
- `State.MarkForUpdate`. Use `State.RequestUIRebuild`
- `Store.StateChanged`. Use store observers instead.
- `GraphElementFactory.CreateUI<T>(this IGraphElementModel)` extension method was removed. Use the static `GraphElementFactory.CreateUI<T>`
  instead.
- `GraphView.DeleteElements()`. Use `GraphView.RemoveElement()` instead.
- `GtfoGraphView.UpdateTopology`. Override `GtfoGraphView.UpdateUI` instead.
- `GraphModel.LastChanges`. Use `State.MarkModelNew`, `State.MarkModelChanged` and `State.MarkModelDeleted` instead.

### Changed

- Add `BadgeModel` as a model for `Badge`
- Itemize menu item is enabled only on constants and (Get) variables that have more than one edge connected to their data output port.
- GTF-140 The blackboard and minimap no longer close when the add graph(+) button is pressed.
- Default BlankPage now provided
- `BasicModel.DeclarationModel.DisplayTitle` now marked virtual
- `Store.RegisterObserver` and `Store.UnregisterObserver` now take a parameter to register the observer as a
  pre-observer, triggered before the action is executed, or a post-observer, triggered after the action was executed.
- `GraphElementFactory.GetUI`, `GraphElementFactory.GetUI<T>`, `GraphElementFactory.GetAllUIs` were moved to the `UIForModel` class.
- `CreateEdgeAction.InputPortModel` renamed to `CreateEdgeAction.ToPortModel`
- `CreateEdgeAction.OutputPortModel` renamed to `CreateEdgeAction.FromPortModel`
- `GraphView.PositionDependenciesManagers` renamed to `GraphView.PositionDependenciesManager` (without the final s)
- `GraphView.AddElement` and `GraphView.RemoveElement` are now virtual.
- Compilation is now an observer on the `Store`. The virtual property `GtfoWindow.RecompilationTriggerActions` lists the actions
  that should trigger a compilation.
- `IGraphModel` delete methods and extension methods now return the list of deleted models.
- Visual Scripting `CompiledScriptingGraphAsset` now serialized in `VsGraphModel` instead of `DotsStencil`
- Manipulators for all graph elements as well as graph view are now overridable.
- `BlackboardGraphModel` is now owned by the `GraphAssetModel` instead of the `State`.
- `BlackboardField` is now a `GraphElement`
- `Blackboard.GraphVariables` renamed to `Blackboard.Highlightables`
- `ExpandOrCollapseVariableDeclarationAction`  renamed to `ExpandOrCollapseBlackboardRowAction`
- `BlackboardGraphModel` was moved to the `UnityEditor.GraphToolsFoundation.Overdrive.BasicModel` namespace
- Removed the `k_` prefix from all non-private readonly fields.
- Moved some images used by USS.
- `Stencil.GetConstantNodeValueType()` replaced by `TypeToConstantMapper.GetConstantNodeType()`
- Constant editor extension methods now takes an `IConstant` as their second parameter,
  instead of an object representing the value of the constant.
- `ConstantEditorExtensions.BuildInlineValueEditor()` is now public.


### Fixed

- GTF-126: NRE when itemize or convert variables on a set var node
- `TypeSerializer` wasn't resolving `TypeHandle` marked with `MovedFromAttribute` when type wasn't in any namespace.
- Fix a bug where dragging a token on a port would block further dragging
- Fix a bug where dragging a token to a port wouldn't create an edge
- GTF-145 Collapsed placemats at launch not hidding edges
- Fix a bug where dragging a blackboard variable to a port wouldn't be allowed

### Deprecated

- Stencil shouldn't be considered serialized anymore. Kept Serializable for backward compatibility

## [0.6.0-preview.4] - 2020-12-02

### Changed
- Updating minimum requirements for com.unity.collections
- BasicModel.DeclarationModel.DisplayTitle now marked virtual
- Updating minimum requirements for com.unity.collections
- GraphModel.OnDuplicateNode now marked virtual
- BasicModel.DeclarationModel.DisplayTitle now marked virtual

### Fixed
- TypeSerializer wasn't resolving TypeHandle marked with MovedFromAttribute when type wasn't in any namespace.

### Added

- GtfoWindow-derived classes needs to implement `CanHandleAssetType(Type)` to dictate supported asset types.
- Added hook (`OnDuplicateNode(INodeModel copiedNode)`) on `INodeModel` when duplicating node
- Added options to toggle Tracing / Options elements on `MainToolbar`
- Added the `CloneGraph` function in the `GraphModel` for duplicating all the models of a source graph

## [0.5.0-preview.3] - 2020-10-28

## [0.5.0-preview.2] - 2020-10-20

### Added

- Generic `CreateEdge` on base `GraphModel` for easier overriding.
- `GetPort` extension method to get ports by direction and port type.
- Add `GraphModel` reference to `Stencil`
- Add `InstantiateStencil`, changing `Stencil` set pattern in `GraphModel`
- new virtual property `HasEditableLabel` for `EditableTitlePart`

### Removed

- `ChangePlacematColorAction`
- `OpenDocumentationAction`
- `UnloadGraphAssetAction`
- `VariableType.ComponentQueryField` enum value
- `SpawnFlags.CreateNodeAsset` and `SpawnFlags.Undoable` enum values
- `CreateGraphAssetAction` and `CreateGraphAssetFromModelAction`. Use `GraphAssetCreationHelper` to create assets and `LoadGraphAssetAction` to load it in a window.
- `ContextualMenuBuilder` and `IContextualMenuBuilder`; to populate a contextual menu, use the UIToolkit way of registering a callback on a `ContextualMenuPopulateEvent` or, for classes deriving from `GraphElement`, override `BuildContextualMenu`.
- `IEditorDataModel` and `EditorDataModel`: use `EditorStateComponent` if you want to hold state that is related to a window or a window-asset combination.
- `IPluginRepository`
- `ICompilationResultModel`

### Changed

- All reducers in the `UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting` namespace have been moved to the `UnityEditor.GraphToolsFoundation.Overdrive` namespace.
- Reducers do not return the `State` anymore.
- Almost all reducers are undoable.
- Replace interface `IAction` by class `BaseAction`.
- `RemoveNodesAction` renamed to `BypassNodesAction`
- `ItemizeVariableNodeAction` and `ItemizeConstantNodeAction` renamed to `ItemizeNodeAction`
- `CreatePortalsOppositeAction` renamed to `CreateOppositePortalAction`
- `SplitEdgeAndInsertNodeAction` renamed to `SplitEdgeAndInsertExistingNodeAction`
- `UpdateConstantNodeActionValue` renamed to `UpdateConstantNodeValueAction`
- `ChangePlacematPositionAction` renamed to `ResizePlacematAction`
- `DropEdgeInEmptyRegionAction` renamed to `DeleteEdgeAction`
- `CreateNodeFromInputPortAction` and `CreateNodeFromOutputPortAction` renamed to `CreateNodeFromPortAction`
- Moved `GraphAssetModel` outside of `BasicModel` namespace since the asset is needed by GTF code.
- The namespaces `UnityEditor.GraphToolsFoundation.Overdrive.Models`,
  `UnityEditor.GraphToolsFoundation.Overdrive.GraphElements` and
  `UnityEditor.GraphToolsFoundation.Overdrive.SmartSearch` have all been merged into
  `UnityEditor.GraphToolsFoundation.Overdrive`.
- Simplify `IGraphModel`. Lots of methods moved out of the interface and made extension methods. Order of parameters has been somewhat standardized.
- `EdgePortalModel` is now backed by a `DeclarationModel` rather than a `VariableDeclarationModel`
- `Store.GetState()` is now `Store.State`
- Bug fix: the expanded/collapsed state of blackboard variables is persisted again.
- Blackboard UI adopts the same architecture as the `GraphView`, with a backing model
  (`IBlackboardGraphProxyElementModel`), using `IGraphElementParts` and the Setup/Build/Update lifecycle.
- Added a `priority` parameter to `GraphElementsExtensionMethodsCacheAttribute` to enable overriding of reducers
  (and all other extensions)
- `Store.GetState()` is now `Store.State`
- `State.CurrentGraphModel` is now `State.GraphModel`
- `IBlackboardGraphProxyElementModel` is now `IBlackboardGraphModel`.
- Moved `EditorDataModel.UpdateFlags`, `EditorDataModel.AddModelToUpdate` and `EditorDataModel.ClearModelsToUpdate` to `State`.
- `CompilationResultModel` is now `CompilationStateComponent`
- `TracingDataModel` is now `TracingStateComponent`
- Made `CreatePort` and `DeletePort` part ot the `IPortNode` interface.
- Made `AddInputPort` and `AddOutputPort` part of the `IInOutPortsNode` interface.
- Moved all variations of `AddInputPort` and `AddOutputPort` to extension methods.

## [0.5.0-preview.1] - 2020-09-25

### Added
- `ContextualMenuBuilder`, implementing the common functionality of `VseContextualMenuBuilder`
- `BlankPage`, implementing the common functionality of `VseBlankPage`
- `GtfoGraphView`, implementing the common functionality of `VseGraphView`
- `GtfoWindow`, implementing the common functionality of `VseWindow`

### Removed
- `VSNodeModel`
- `VSPortModel`
- `VSEditorDataModel`
- `VSPreferences`
- `VSTypeHandle`
- `VseContextualMenuBuilder`
- `VseBlankPage`
- `VseGraphView`
- `VseWindow`
- `DebugDisplayElement`
- `ISystemConstantNodeModel`
- `VseUIController`, now part of `GraphView`
- `VisualScripting.State`, now part of `Overdrive.State`
- `UICreationHelper`, now part of `PackageTransitionHelper`
- `IEditorDataModel` now part of `IGTFEditorDataModel`
- `IBuilder`
- `InputConstant`
- `InputConstantModel`
- `IStringWrapperConstantModel.GetAllInputs`
- `Stencil.Builder`
- `ConstantEditorExtensions.BuildStringWrapperEditor`
- `PortAlignmentType`
- `VisualScripting.Node`, merged into `CollapsibleInOutNode`
- `VisualScripting.Token`, merged into `TokenNode`
- All factory extension method in `GraphElementFactoryExtensions` except the one that creates ports.

### Changed
- A lot of classes have been moved outside the `VisualScripting` namespace.
- `IGTFStringWrapperConstantModel` was merged with `IStringWrapperConstantModel`
- When one drops an edge outside of a port, instead of deleting the edge, we now pop the searcher to create a new node.
- Rename `CreateCollapsiblePortNode` to `CreateNode`.
- Remove `GTF` from class and interface names. For example, `IGTFNodeModel` becomes `INodeModel`.
- `PackageTransitionHelper` becomes `AssetHelper.
- Base capabilities are no longer serialized with the `GraphToolsFoundation` prefix.

## [0.4.0-preview.1] - 2020-07-28

Drop-12

### Added

- Added an automatic spacing feature

#### API Changes

- `IGTFNodeModel` now has a `Tooltip` property.
- `IGTFEdgeModel` `FromPort` and `ToPort` are settable.
- Implementations of Unity event functions are now virtual.
- `GraphModel` basic implementation now serializes edges, sticky notes and placemats as references to enable the use of derived classes for them.
- `EditorDataModel.ElementModelToRename` was moved to `IGTFEditorDataModel`.
- Added default value for `IGTFGraphModel.CreateNode` `spawnFlag` parameter.
- Added support for `List<>` in `TypeSerializer`.

### Removed

- `PanToNodeAction`. Call `GraphView.PanToNode` instead.
- `RefreshUIAction`. Call `Store.ForceRefreshUI` instead.

### Fixed

- Fix issue when moving two nodes connected by edge with control points.
- Fix issue with auto placement of vertical ports with labels.
- Fix behavior of the default move and auto-placement reducers.

### Changed

- Changed the automatic alignment feature to consider connected nodes
- Extract basic model implementation from VisualScripting folder/namespace to GTFO.
- Split `IGTFNodeModel` and `IGTFPortModel` into finer grained interfaces.
- Add default implementation of some interfaces.
- Replace `IGraphModel` by `IGTFGraphModel`
- Replace `IVariableDeclarationModel` by `IGTFVariableDeclarationModel`
- Remove unused `BlackboardThisField` and `ThisNodeModel`
- Base Store class is now sealed. All derived store classes have been merged into the base class.
- Capabilities API modified to be more versatile
  - Capabilities are no longer interfaces but rather "simple" capabilities that can be added to models.
  - `IPositioned` has been renamed `IMovable`.
- Test models in `Tests\Editor\Overdrive\GraphElements\GraphViewTesting\BasicModel` and `Tests\Editor\Overdrive\GTFO\UIFromModelTests\Model` have been unified under `Tests\Editor\Overdrive\TestModels`

## [0.3.0-preview.1] - 2020-07-31

Drop 11

## [0.2.3-preview.3] - 2020-07-15

### Added

- Added dirty asset indicator in the window title
- Made VseWindow.Update virtual to enable derived classes to override it

### Fixed

- Fixed copy / paste issues with graph edges
- Mark graph asset dirty when edges are created or deleted
- Fixed resize issues with the sticky notes

## [0.2.3-preview.2] - 2020-06-18

## [0.2.3-preview.1] - 2020-06-18

## [0.2.3-preview] - 2020-06-12

## [0.2.2-preview.1] - 2020-05-06

### Changed

- Enabling vertical alignment in out-of-stack nodes w/ execution ports

## [0.2.1-preview.1] - 2020-03-20

### Added

- AnimationCurve constant editor
- Allow support of polymorphic edges in graph.
- Allow windows to decide if they handle specific asset types

### Changed

- Rework pills visual to tell apart read-only/write-only fields

### Fixed

- Fix graph dirty flag when renaming token

## [0.2.0-preview.4] - 2020-03-20

### Changed

- Updated com.unity.properties.ui@1.1.0-preview

## [0.2.0-preview.3] - 2020-02-26

### Fixed

- Fixed package warnings

## [0.2.0-preview.2] - 2020-02-21

### Changed

- Changed the handing of the MovedFrom attribute to accept assembly strings without version and fixed support for nested types

## [0.2.0-preview.1] - 2020-02-06

## [0.2.0-preview] - 2020-02-05

## [0.1.3-preview.1] - 2019-01-29

### Added

- Added support for migrating node types which have been moved or renamed

## [0.1.2-preview.10] - 2019-01-16

## [0.1.2-preview.9] - 2019-12-17

## [0.1.2-preview.8] - 2019-12-10

## [0.1.2-preview.7] - 2019-12-09

## [0.1.2-preview.6] - 2019-11-25

## [0.1.2-preview.5] - 2019-11-12

## [0.1.2-preview.4] - 2019-11-11

## [0.1.2-preview.3] - 2019-10-28

## [0.1.2] - 2019-08-15

## [0.1.1] - 2019-08-12

## [0.1.0] - 2019-08-01

### This is the first release of _Visual Scripting framework_.

_Short description of this release_
