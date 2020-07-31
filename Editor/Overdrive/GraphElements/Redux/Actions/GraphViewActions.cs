using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    // PF : Ponder: Unify with Change*PositionAction ?
    public class MoveElementsAction : IAction
    {
        public IPositioned[] Models;
        public Vector2 Delta;

        public MoveElementsAction()
        {
        }

        public MoveElementsAction(Vector2 delta, IReadOnlyCollection<IPositioned> models)
        {
            Models = models.ToArray();
            Delta = delta;
        }

        public static TState DefaultReducer<TState>(TState previousState, MoveElementsAction action) where TState : State
        {
            if (action.Models == null || action.Delta == Vector2.zero)
                return previousState;

            foreach (var pModel in action.Models)
                pModel.Move(action.Delta);

            return previousState;
        }
    }

    public class AlignElementsAction : IAction
    {
        public IPositioned[] Models;
        public Vector2[] Deltas;
        public AlignElementsAction()
        {
        }

        public AlignElementsAction(IReadOnlyCollection<Vector2> delta, IReadOnlyCollection<IPositioned> models)
        {
            Models = models.ToArray();
            Deltas = delta.ToArray();
        }

        public static TState DefaultReducer<TState>(TState previousState, AlignElementsAction action) where TState : State
        {
            if (action.Models == null || action.Deltas == null || action.Models.Length != action.Deltas.Length)
                return previousState;

            for (int i = 0; i < action.Deltas.Length; ++i)
            {
                action.Models[i].Move(action.Deltas[i]);
                previousState.MarkForUpdate(UpdateFlags.UpdateView, action.Models[i] as IGTFGraphElementModel);
            }

            if (previousState.AssetModel != null)
            {
                EditorUtility.SetDirty((Object)previousState.AssetModel);
            }

            return previousState;
        }
    }

    public class DeleteElementsAction : IAction
    {
        public IGTFGraphElementModel[] ElementsToRemove;
        public string OperationName;
        public AskUser AskConfirmation;

        public DeleteElementsAction()
        {
        }

        public DeleteElementsAction(string operationName, AskUser askUser, params IGTFGraphElementModel[] elementsToRemove)
        {
            ElementsToRemove = elementsToRemove;
            OperationName = operationName;
            AskConfirmation = askUser;
        }

        public DeleteElementsAction(params IGTFGraphElementModel[] elementsToRemove)
            : this("Delete", AskUser.DontAskUser, elementsToRemove) {}

        public enum AskUser
        {
            AskUser,
            DontAskUser
        }

        public static TState DefaultReducer<TState>(TState previousState, DeleteElementsAction action) where TState : State
        {
            IGTFGraphElementModel[] deletables = action.ElementsToRemove.Where(x => x is IDeletable deletable && deletable.IsDeletable).Distinct().ToArray();

            previousState.CurrentGraphModel.DeleteElements(deletables);
            return previousState;
        }
    }

    public class UpdatePortConstantAction : IAction
    {
        public IGTFPortModel PortModel;
        public object NewValue;

        public UpdatePortConstantAction()
        {
        }

        public UpdatePortConstantAction(IGTFPortModel portModel, object newValue)
        {
            PortModel = portModel;
            NewValue = newValue;
        }
    }
}
