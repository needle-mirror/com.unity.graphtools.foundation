# Data Flow Overview

The data flow in Graph Tools Foundation is inspired by the Flux
design, where user interactions create actions that 
are sent to a dispatcher to modify the application state (called the store). 
When the store is modified, events are emitted and caught by the views,
which they can then update themselves.

The interesting idea behind Flux is the well defined data flow: 
modifications to the application state can only be done through actions,
and views subscribe to changes on the state:

- user interacts with UI
- UI sends an action to the dispatcher
- the dispatcher updates the store according to the action
- the store notifies the view that have subscribe to it
- the view updates its UI (without sending any additional actions)

GTF mostly follows this pattern, with some changes in the actor names.
One of the main differences between Flux and GTF is the use of 
observers to update the views, instead of the listener pattern used in Flux. 
This enables GTF to use observers not only to update the UI, but also 
to modify the application state before the UI observers update the UI.

## The `GraphToolState`

The `GraphToolState` holds all the data necessary to display
and edit the graph. The state is made of `StateComponent`s. Each component
holds data related to a specific domain of the tool. For example, the
`GraphViewStateComponent` holds information needed by the `GraphView`,
the `SelectionStateComponent` holds selection information, etc.

Each state component holds a version number which is incremented when
the state component is modified. `StateComponent`s can hold a list of changes
associated with each version number. These change lists can be used by 
observers to limit the amount of work needed to update themselves.

In Flux, the `StateComponent`s would be the stores, and `GraphToolState`
would be the collection of all stores.

## The `Command`s

The `Command`s encapsulate data about a user interaction. Anytime
the user interacts with the UI to modify the graph, a command is created
and sent to the command dispatcher. The type of the command carries its
semantics and its fields carry the information needed to execute it.
For example, when the user clicks into a node, a `SelectElementCommand`
is sent; its fields hold the node model and the selection mode.

In Flux, the equivalent of the `Command` is the action.

## The command handlers

Command handlers are static methods that receive
the `GraphToolState` and the `Command` as parameters and use the command
fields to act upon the state. Each command type is associated with a single
command handler. GTF provides a default command handlers for each command
type, but graph tool implementers can replace them with their own.

## The `StateObserver`s

The `StateObserver`s declare interest in some state components and
update other state components or the UI. The observers have an API
to declare which state components are observed and which state components
are potentially modified by the observer, if any.

After state components have been modified by a `Command`, the relevant
observers are notified by the command dispatcher.
In GTF, observer notification is done as part of the `EditorWindow.Update()`
event. It is thus possible that more than one command is dispatched
before the observers are notified.

The state observer keeps the last observed version of each observed
state component in order to avoid doing work if state components did not
change.

## The `CommandDispatcher`

The `CommandDispatcher` is the main communication hub. As part of the
graph tool initialization process, command handlers and state observers
are registered to the dispatcher.

As UIToolkit events are processed, commands are sent to the dispatcher
using its `Dispatch()` method. There, the dispatcher selects the appropriate
command handler and executes it. This usually has the effect of modifying
some state components and incrementing their version number.

Then, as part of `EditorWindow.Update()`, the command dispatcher
`NotifyObserver()` method is called. There, the command dispatcher gathers
a list of observers that are interested in one or more of the dirtied state
components. Using information about the observers observed components and
modified components, the observer list is sorted. The goal is to execute
an observer that modifies the 'X' component before another observer that
observes this same 'X' component. Each observer is then executed. Finally,
state component dirty state and change lists are cleared if no observer
needs them anymore.

Observers that update UI views will either rebuild the whole view by 
removing all `ModelUI`s from the view and re-creating them, or update only 
the relevant `ModelUI`s by calling `ModelUI.UpdateFromModel()`.
