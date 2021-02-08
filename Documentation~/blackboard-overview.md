# The `Blackboard`

The `Blackboard` is the UI that provides the functionality to define
the inputs and outputs of a graph. It is backed by a
`IBlackboardGraphModel`.

To customize your blackboard, implement the `IBlackboardGraphModel`
class or derive from `BasicModel.BlackboardGraphModel`. The create menu
of each section should create `IVariableDeclarationModel`. A variable
declaration is an object that uniquely identifies a variable, an input
or an output of the graph. Variables are for local use and can be read
and written, whereas inputs and outputs are used to interface with
other graphs and the outside world. Inputs are read-only and output
are write-only.

The variable declarations of a graph are listed
in the sections of the blackboard, according to the content returned
by `IBlackboardGraphModel.GetSectionRows()`. They can be dragged and
dropped into the graph view. When this happens, a node representing
the variable declaration is created. This is typically a node with
a single output port or a single input port.
