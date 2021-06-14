namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for UIs based on a model, i.e. graph elements but also ports and blackboard elements
    /// </summary>
    public interface IModelUI
    {
        /// <summary>
        /// The model that backs the UI.
        /// </summary>
        IGraphElementModel Model { get; }

        /// <summary>
        /// The command dispatcher to which the UI should send commands to.
        /// </summary>
        CommandDispatcher CommandDispatcher { get; }

        /// <summary>
        /// The view that owns this object.
        /// </summary>
        IModelView View { get; }

        /// <summary>
        /// The UI creation context.
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Adds the instance to a view.
        /// </summary>
        /// <param name="view">The view to add the element to.</param>
        void AddToView(IModelView view);

        /// <summary>
        /// Removes the instance from the view.
        /// </summary>
        void RemoveFromView();

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="model">The model that backs the instance.</param>
        /// <param name="commandDispatcher">The command dispatcher to wich commands should be sent.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        void Setup(IGraphElementModel model, CommandDispatcher commandDispatcher, IModelView view, string context);

        /// <summary>
        /// Instantiates and initializes the VisualElements that makes the UI.
        /// </summary>
        void BuildUI();

        /// <summary>
        /// Updates the UI using data from the model.
        /// </summary>
        void UpdateFromModel();

        /// <summary>
        /// Helper method that calls <see cref="Setup"/>, <see cref="BuildUI"/> and <see cref="UpdateFromModel"/>.
        /// </summary>
        /// <param name="model">The model that backs the instance.</param>
        /// <param name="commandDispatcher">The command dispatcher to which commands should be sent.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        void SetupBuildAndUpdate(IGraphElementModel model, CommandDispatcher commandDispatcher, IModelView view, string context);

        /// <summary>
        /// Adds graph elements to the backward dependencies list. A backward dependency is
        /// a graph element that causes this model UI to be updated whenever it is updated.
        /// </summary>
        void AddBackwardDependencies();

        /// <summary>
        /// Adds graph elements to the forward dependencies list. A forward dependency is
        /// a graph element that should be updated whenever this model UI is updated.
        /// </summary>
        void AddForwardDependencies();

        /// <summary>
        /// Adds graph elements to the model dependencies list. A model dependency is
        /// a graph element model that causes this model UI to be updated whenever it is updated.
        /// </summary>
        void AddModelDependencies();
    }
}
