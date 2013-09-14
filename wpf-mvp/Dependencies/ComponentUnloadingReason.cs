namespace Wpf.Mvp.Dependencies {
    /// <summary>
    /// Reason of component unloading from <see cref="ComponentRegistry"/>.
    /// </summary>
    public enum ComponentUnloadingReason {
        /// <summary>
        /// If UnregisterMe method called when window is closed.
        /// </summary>
        WindowOnClosedCalled,
        /// <summary>
        /// If you manually call this method in your window or user control.
        /// </summary>
        ManualCall,
        /// <summary>
        /// If one of required dependency has been unloaded.
        /// </summary>
        RequiredDependencyUnloaded
    }
}