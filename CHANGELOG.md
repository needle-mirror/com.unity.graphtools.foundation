# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.5.0-preview.3] - 2020-10-28

## [0.5.0-preview.2] - 2020-10-20

### Removed
- `ChangePlacematColorAction`
- `OpenDocumentationAction`
- `UnloadGraphAssetAction`
- `VariableType.ComponentQueryField` enum value
- `SpawnFlags.CreateNodeAsset` and `SpawnFlags.Undoable` enum values
- `CreateGraphAssetAction` and `CreateGraphAssetFromModelAction`. Use `GraphAssetCreationHelper` to create assets and `LoadGraphAssetAction` to load it in a window.
- `ContextualMenuBuilder` and `IContextualMenuBuilder`; to populate a contextual menu, use the UIToolkit way of registering a callback on a `ContextualMenuPopulateEvent` or, for classes deriving from `GraphElement`, override `BuildContextualMenu`.

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
- `VisualScripting.Node`, merged into `CollapsibleInOutNode`
- `VisualScripting.Token`, merged into `TokenNode`
- All factory extension method in `GraphElementFactoryExtensions` except the one that creates ports.

### Changed
- A lot of classes have been moved outside the `VisualScripting` namespace.
- `IGTFStringWrapperConstantModel` was merged with `IStringWrapperConstantModel`
- When one drops an edge outside of a port, instead of deleting the edge, we now pop the searcher to create a new node.
- Rename `CreateCollapsiblePortNode` to `CreateNode`.
- Remove `GTF` from class and interface names. For example, `IGTFNodeModel` becomes `INodeModel`.

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
