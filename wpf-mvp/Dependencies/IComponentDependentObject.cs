namespace Wpf.Mvp.Dependencies {
    /// <summary>
    /// You should implement this interface if your custom class (not derived from <see cref="AbstractPresenter"/>
    /// needs to be informed about its dependencies loading / unloading.
    /// </summary>
    public interface IComponentDependentObject {
        /// <summary>
        /// This method will be called when necessary component instance is loaded.
        /// </summary>
        void DependencyLoaded(AbstractPresenter dependency);

        /// <summary>
        /// This method will be called when necessary component instance is unloaded.
        /// </summary>
        void DependencyUnloaded(AbstractPresenter dependency);
    }
}