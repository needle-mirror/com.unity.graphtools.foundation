using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class EdgeOrderObserver : StateObserver<GraphToolState>
    {
        public EdgeOrderObserver()
            : base(new[] { nameof(GraphToolState.SelectionState) },
                new[] { nameof(GraphToolState.GraphViewState) })
        { }

        protected override void Observe(GraphToolState state)
        {
            using (var selObs = this.ObserveState(state.SelectionState))
            {
                List<IGraphElementModel> changedModels = null;

                if (selObs.UpdateType == UpdateType.Complete)
                {
                    changedModels = state.GraphViewState.GraphModel.EdgeModels.Concat<IGraphElementModel>(state.GraphViewState.GraphModel.NodeModels).ToList();
                }
                else if (selObs.UpdateType == UpdateType.Partial)
                {
                    var changeset = state.SelectionState.GetAggregatedChangeset(selObs.LastObservedVersion);
                    changedModels = changeset.ChangedModels.ToList();
                }

                if (changedModels != null)
                {
                    var portsToUpdate = new HashSet<IReorderableEdgesPortModel>();

                    foreach (var model in changedModels.OfType<IEdgeModel>())
                    {
                        if (model.FromPort is IReorderableEdgesPortModel reorderableEdgesPort && reorderableEdgesPort.HasReorderableEdges)
                        {
                            portsToUpdate.Add(reorderableEdgesPort);
                        }
                    }

                    foreach (var model in changedModels.OfType<IPortNodeModel>())
                    {
                        foreach (var port in model.Ports
                            .OfType<IReorderableEdgesPortModel>()
                            .Where(p => p.HasReorderableEdges))
                        {
                            portsToUpdate.Add(port);
                        }
                    }

                    if (portsToUpdate.Count > 0)
                    {
                        using (var updater = state.GraphViewState.UpdateScope)
                        {
                            foreach (var portModel in portsToUpdate)
                            {
                                var connectedEdges = portModel.GetConnectedEdges().ToList();

                                var selected = state.SelectionState.IsSelected(portModel.NodeModel);
                                if (!selected)
                                {
                                    foreach (var edgeModel in connectedEdges)
                                    {
                                        selected = state.SelectionState.IsSelected(edgeModel);
                                        if (selected)
                                            break;
                                    }
                                }

                                if (selected && connectedEdges.Count > 1)
                                {
                                    foreach (var edgeModel in connectedEdges)
                                    {
                                        var newLabel = (portModel.GetEdgeOrder(edgeModel) + 1).ToString();
                                        if (edgeModel.EdgeLabel != newLabel)
                                        {
                                            edgeModel.EdgeLabel = newLabel;
                                            updater.MarkChanged(edgeModel);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var edgeModel in connectedEdges)
                                    {
                                        if (!string.IsNullOrEmpty(edgeModel.EdgeLabel))
                                        {
                                            edgeModel.EdgeLabel = "";
                                            updater.MarkChanged(edgeModel);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
