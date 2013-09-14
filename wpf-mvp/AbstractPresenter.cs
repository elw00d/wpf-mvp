using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using NLog;
using Wpf.Mvp.Dependencies;

namespace Wpf.Mvp {
    /// <summary>
    /// Represents non-generic super class for all presenters including generic <see cref="BasePresenter{TModel,TView}"/>.
    /// </summary>
    public abstract class AbstractPresenter : IDependencyObject, IDependentObject {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // true если либо нет required-зависимостей вообще, либо если все были загружены
        private bool m_allRequiredDependenciesLoaded;
        private bool allRequiredDependenciesLoaded {
            get {
                return m_allRequiredDependenciesLoaded;
            }
            set {
                m_allRequiredDependenciesLoaded = value;
                if (logger.IsTraceEnabled && m_allRequiredDependenciesLoaded && hasDependencies()) {
                    logger.Trace("Component's all required dependencies are loaded. Presenter type : {0}.", this.GetType());
                }
            }
        }

        private bool m_componentRegistered;
        private bool componentRegistered {
            get {
                return m_componentRegistered;
            }
            set {
                m_componentRegistered = value;
                if (componentRegistered) {
                    if (logger.IsTraceEnabled) {
                        logger.Trace("Registered as available to subscribe: {0}. Presenter type : {1}.", InstanceName, this.GetType());
                    }
                }
            }
        }

        private readonly List<DependencyStatus> dependencyStatuses = new List<DependencyStatus>();

        protected internal bool hasDependencies() {
            return dependencyStatuses.Count != 0;
        }

        protected AbstractPresenter() {
            // get dependencies and make dictionary <dependencyInfo, bool loaded>
            var dependencyAttributeInfos = ComponentRegistry.GetDependencyAttributes(this.GetType());
            bool anyRequiredDependencyPresent = false;
            foreach (var attributeInfo in dependencyAttributeInfos) {
                dependencyStatuses.Add(new DependencyStatus(attributeInfo));
                if (attributeInfo.Attribute.IsRequired && !anyRequiredDependencyPresent) {
                    anyRequiredDependencyPresent = true;
                }
            }
            //
            allRequiredDependenciesLoaded = !anyRequiredDependencyPresent;
        }

        /// <summary>
        /// This property shows that Initialization sequence completed.
        /// </summary>
        protected bool Initialized {
            get;
            private set;
        }

        /// <summary>
        /// This method called from <see cref="BaseUserControl{TModel,TController,TView}"/> or
        /// <see cref="BaseWindow{TModel,TPresenter,TView}"/> when they are initializing.
        /// </summary>
        internal void InitializeInstance() {
            // first presenter initialization step - init dependencies (can be overriden)
            InitializeDependencies();
            // second initialization step - init commands (can be overriden too)
            InitializeCommands();
            // final step - calling overrideable method Initialize
            Initialize();
            //
            Initialized = true;
        }

        /// <summary>
        /// Subscribes this instance for its dependencies if any dependency declared.
        /// If you override this method and don't call base implementation, instance won't be
        /// subscribed for its dependencies.
        /// </summary>
        protected virtual void InitializeDependencies() {
            // if presenter is annotated with dependency attributes, subscribe self in DependenciesRegistry
            if (hasDependencies()) {
                if (logger.IsTraceEnabled) {
                    logger.Trace("Component {0} has a dependencies: {1}.", this.GetType(), getDependenciesStatusDescription());
                }
                ComponentRegistry.Instance.SubscribeForDependencies(this);
            }
        }

        /// <summary>
        /// Commands initialization.
        /// </summary>
        protected virtual void InitializeCommands() {
        }

        /// <summary>
        /// Presenter main initialization method. Called after commands and dependencies initialization.
        /// todo : to protected
        /// </summary>
        protected virtual void Initialize() {
        }

        /// <summary>
        /// Represents loaded status for specified dependency identified by <see cref="DependencyAttributeInfo"/>.
        /// </summary>
        private class DependencyStatus {
            public DependencyAttributeInfo AttributeInfo {
                get;
                private set;
            }

            public bool IsLoaded {
                get;
                set;
            }

            public DependencyStatus(DependencyAttributeInfo attributeInfo) {
                AttributeInfo = attributeInfo;
            }
        }

        void IDependentObject.DependencyLoaded(IDependencyObject dependency) {
            AbstractPresenter componentInstance = (AbstractPresenter) dependency;
            // записей о статусе зависимости может быть несколько, так как возможна ситуация, когда в презентере
            // есть несколько свойств, помеченных одним и тем же атрибутом, мы должны для каждого из них вызвать сеттер
            IList<DependencyStatus> statuses = dependencyStatuses.Where(status => status.AttributeInfo.Attribute.InstanceName == dependency.InstanceName).ToList();
            if(statuses.Any( s => s.IsLoaded )) throw new InvalidOperationException("Assertion failed");
            //
            this.OnDependencyLoaded(componentInstance.GetType(), dependency.InstanceName, componentInstance);
            //
            foreach (DependencyStatus status in statuses) {
                // if presenter has annotated property, set them too
                PropertyInfo propertyInfo = status.AttributeInfo.PropertyInfo;
                if (propertyInfo != null) {
                    // set the property value
                    propertyInfo.SetValue(this, componentInstance, null);
                }
                //
                status.IsLoaded = true;
            }
            //
            if (logger.IsTraceEnabled) {
                logger.Trace("Changed dependencies set for component {0}: loaded {1}. Dependencies status now is: {2}",
                    this.GetType(), dependency.InstanceName, getDependenciesStatusDescription());
            }
            // if all required dependencies are loaded, call appropriate presenter method
            if (statuses.All(status => !status.AttributeInfo.Attribute.IsRequired || status.IsLoaded)) {
                if (!allRequiredDependenciesLoaded) {
                    this.OnComponentLoaded();
                    allRequiredDependenciesLoaded = true;
                    registerComponentIfNeed();
                }
            }
        }

        /// <summary>
        /// Registers the component as available to subscribe.
        /// Component should have not empty Name and all required dependencies are loaded.
        /// </summary>
        private void registerComponentIfNeed() {
            if (!string.IsNullOrEmpty(InstanceName) && !componentRegistered && allRequiredDependenciesLoaded) {
                if (logger.IsTraceEnabled) {
                    logger.Trace("Registering as available for subscribe: {0}. Presenter type: {1}.", InstanceName, this.GetType());
                }
                ComponentRegistry.Instance.RegisterComponentInstance(this);
                componentRegistered = true;
            }
        }

        void IDependentObject.DependencyUnloaded(IDependencyObject dependency) {
            AbstractPresenter componentInstance = (AbstractPresenter)dependency;
            IList<DependencyStatus> statuses = dependencyStatuses.Where(status => status.AttributeInfo.Attribute.InstanceName == dependency.InstanceName).ToList();
            if(!statuses.All(s => s.IsLoaded)) throw new InvalidOperationException("Assertion failed");
            //
            this.OnDependencyUnloaded(componentInstance.GetType(), dependency.InstanceName);
            //
            bool requiredDependencyIsUnloaded = false;
            foreach (DependencyStatus status in statuses) {
                // if presenter has annotated properties, set them to null
                PropertyInfo propertyInfo = status.AttributeInfo.PropertyInfo;
                if (null != propertyInfo) {
                    propertyInfo.SetValue(this, null, null);
                }
                status.IsLoaded = false;
                if (status.AttributeInfo.Attribute.IsRequired) {
                    requiredDependencyIsUnloaded = true;
                }
            }
            //
            if (logger.IsTraceEnabled) {
                logger.Trace("Changed dependencies set for component {0}: unloaded {1}. Dependencies status now is: {2}",
                    this.GetType(), dependency.InstanceName, getDependenciesStatusDescription());
            }
            // if any required dependency unloaded, unloading self too
            if (requiredDependencyIsUnloaded && allRequiredDependenciesLoaded) {
                unloadComponentCore(ComponentUnloadingReason.RequiredDependencyUnloaded);
            }
        }

        string[] IDependentObject.GetDependencies() {
            return this.dependencyStatuses.Select(st => st.AttributeInfo.Attribute.InstanceName).ToArray();
        }

        /// <summary>
        /// Called when all required dependencies are successfully loaded or there are no
        /// required dependencies on creating.
        /// </summary>
        protected virtual void OnComponentLoaded() {
        }

        /// <summary>
        /// Called when one of component dependencies is loaded (with all required dependencies).
        /// You may not call base.<see cref="OnDependencyLoaded"/>.
        /// </summary>
        protected virtual void OnDependencyLoaded(Type presenterType, string instanceName, AbstractPresenter instance ) {
        }

        /// <summary>
        /// Called when one of component dependencies is unloaded (if window is closed or some of required dependency has been unloaded).
        /// You may not call base.<see cref="OnDependencyUnloaded"/>.
        /// </summary>
        protected virtual void OnDependencyUnloaded(Type presenterType, string instanceName) {
        }

        /// <summary>
        /// Called when component is unloaded from dependencies registry (window is closed or some of required dependency 
        /// has been unloaded or someone manually called <see cref="UnloadComponent"/> method).
        /// </summary>
        /// <param name="reason">Reason of unloading.</param>
        protected virtual void OnComponentUnloaded(ComponentUnloadingReason reason) {
        }

        private void unloadComponentCore(ComponentUnloadingReason reason) {
            if (!allRequiredDependenciesLoaded) {
                if (logger.IsWarnEnabled) {
                    logger.Warn("Unloading component that is not fully loaded yet. In the vast majority of cases it is a sign of an error.");
                }
                if (Debugger.IsAttached) {
                    Debugger.Log(1, "WARN", "Unloading component that is not fully loaded yet. In the vast majority of cases it is a sign of an error.");
                    Debugger.Break();
                }
            }
            //
            if (logger.IsTraceEnabled) {
                logger.Trace("Unloading component {0} ({1}) with reason: {2}.", InstanceName, this.GetType(), reason);
            }
            //
            if (hasDependencies()) {
                ComponentRegistry.Instance.Unsubscribe(this);
            }
            if (componentRegistered) {
                ComponentRegistry.Instance.UnregisterComponentInstance(this);
            }
            //
            OnComponentUnloaded(reason);
            //
            if (logger.IsTraceEnabled) {
                logger.Trace("Unloaded component instance: {0} ({1}) with reason {2}.", InstanceName, this.GetType(), reason);
            }
        }

        internal void WindowClosed() {
            unloadComponentCore(ComponentUnloadingReason.WindowOnClosedCalled);
        }

        /// <summary>
        /// Unloads component from <see cref="ComponentRegistry"/> manually.
        /// Unsubscribes from its dependencies and unregisters itself if it has specified <see cref="InstanceName"/>.
        /// You should call this method if your control is unloaded and it was subscribed to dependencies notifications.
        /// If your component is Window, there are no meaning to manually unload it, because WindowClosed do this work automatically.
        /// </summary>
        public void UnloadComponent() {
            unloadComponentCore(ComponentUnloadingReason.ManualCall);
        }

        private string instanceName;
        /// <summary>
        /// Instance name of component.
        /// </summary>
        public string InstanceName {
            get {
                return instanceName;
            }
            internal set {
                instanceName = value;
                registerComponentIfNeed();
            }
        }

        /// <summary>
        /// Returns a string description about dependencies (all - loaded or waiting for).
        /// </summary>
        private string getDependenciesStatusDescription() {
            if (!this.hasDependencies()) {
                return "no dependencies.";
            }
            List<DependencyStatus> statuses = this.dependencyStatuses;
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0} dependencies: ", statuses.Count));
            bool first = true;
            foreach (var status in statuses) {
                string formatted = string.Format((first ? "" : ", ") + "{0} ({1})", status.AttributeInfo.Attribute.InstanceName, status.IsLoaded ? "loaded" : "waiting");
                sb.Append(formatted);
                first = false;
            }
            return sb.ToString();
        }
    }
}