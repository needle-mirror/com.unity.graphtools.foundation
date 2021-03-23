# Getting started

Graph Tools Foundation can be used to build tools that deals with
two main categories of graph:

- Graphs that describe processes, where some data flows through the
  nodes of the graph. Nodes in these graphs represent operation on data.
  Edges between nodes represents data that flow from one operation to
  another, and execution flow. They also have some special nodes that
  represent the data sources and data sinks. Example of graph
  that describe processes are visual programming graphs.
  To illustrate how you can build a tool for this kind of graphs,
  we provide the *Recipe Editor* sample in the `Samples` directory and
  [a tutorial based on this sample](recipes-sample.md).
- Graphs that describe states. In these graphs, nodes represent
  some data, or state, and the edges represent relationship between
  the nodes. Example of graph that describe states are organisational
  charts and state machines. We do not yet have a sample for this
  kind of graphs, but if you read the
  [*Recipe Editor* tutorial](recipes-sample.md), you will get a good
  feel of how you can implement a state graph with GTF.
