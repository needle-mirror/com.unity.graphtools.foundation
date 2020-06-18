using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    // PF : Ponder: Unify with Change*PositionAction ?
    public class MoveElementsAction : IAction
    {
        public readonly IReadOnlyCollection<IPositioned> Models;
        public readonly Vector2 Delta;

        public MoveElementsAction(Vector2 delta, IReadOnlyCollection<IPositioned> models)
        {
            Models = models;
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

    public class DeleteElementsAction : IAction
    {
        public readonly IReadOnlyCollection<IGTFGraphElementModel> ElementsToRemove;
        public readonly string OperationName;
        public readonly AskUser AskConfirmation;

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

            previousState.GraphModel.DeleteElements(deletables);
            return previousState;
        }
    }
}
