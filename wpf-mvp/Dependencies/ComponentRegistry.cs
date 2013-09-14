using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wpf.Mvp.Attributes;

namespace Wpf.Mvp.Dependencies {

    /// <summary>
    /// Represents an dependency attribute associated with property if it presents.
    /// </summary>
    internal class DependencyAttributeInfo {
        public readonly MvpDependencyAttribute Attribute;
        public readonly PropertyInfo PropertyInfo;

        public DependencyAttributeInfo(MvpDependencyAttribute attribute, PropertyInfo propertyInfo) {
            Attribute = attribute;
            PropertyInfo = propertyInfo;
        }
    }

    /// <summary>
    /// Registry for all components having any dependencies or being a dependency component itself.
    /// </summary>
    public sealed class ComponentRegistry {
        private static readonly ComponentRegistry instance = new ComponentRegistry();
        public static ComponentRegistry Instance {
            get {
                return instance;
            }
        }

        private ComponentRegistry() {
        }

        private readonly DependenciesRegistry dependenciesRegistry = new DependenciesRegistry();

        public void RegisterComponentInstance(AbstractPresenter presenter) {
            dependenciesRegistry.RegisterDependency(presenter);
        }

        public void UnregisterComponentInstance(AbstractPresenter presenter) {
            dependenciesRegistry.UnregisterDependency(presenter);
        }

        /// <summary>
        /// Bridge class to provide user code access to <see cref="IComponentDependentObject"/>
        /// instead of <see cref="IDependentObject"/>.
        /// </summary>
        private sealed class ComponentDependentObjectBridge : IDependentObject {
            private readonly IComponentDependentObject componentDependentObject;
            public ComponentDependentObjectBridge(IComponentDependentObject componentDependentObject) {
                this.componentDependentObject = componentDependentObject;
            }

            public void DependencyLoaded(IDependencyObject dependency) {
                componentDependentObject.DependencyLoaded((AbstractPresenter) dependency);
                // set properties too
                foreach (PropertyInfo propertyInfo in dependencyAttributes.Where(
                    attribute => attribute.PropertyInfo != null && attribute.Attribute.InstanceName == dependency.InstanceName)
                    .Select(attribute => attribute.PropertyInfo)) {
                    //
                    propertyInfo.SetValue(componentDependentObject, dependency, null);
                }
            }

            public void DependencyUnloaded(IDependencyObject dependency) {
                componentDependentObject.DependencyUnloaded((AbstractPresenter)dependency);
                // unset properties too
                foreach (PropertyInfo propertyInfo in dependencyAttributes.Where(
                    attribute => attribute.PropertyInfo != null && attribute.Attribute.InstanceName == dependency.InstanceName)
                    .Select(attribute => attribute.PropertyInfo)) {
                    //
                    propertyInfo.SetValue(componentDependentObject, null, null);
                }
            }

            private List<DependencyAttributeInfo> dependencyAttributes = null;
            public string[] GetDependencies() {
                if (null == dependencyAttributes) {
                    dependencyAttributes = GetDependencyAttributes(componentDependentObject.GetType());
                }
                return dependencyAttributes.Select(dependencyAttribute => dependencyAttribute.Attribute.InstanceName).ToArray();
            }
        }

        private readonly Dictionary<IComponentDependentObject, ComponentDependentObjectBridge> bridgeObjects = new Dictionary<IComponentDependentObject,ComponentDependentObjectBridge>();

        public void SubscribeForDependencies(IComponentDependentObject dependentObject) {
            if (null == dependentObject) {
                throw new ArgumentNullException("dependentObject");
            }
            //
            ComponentDependentObjectBridge objectBridge;
            if (!bridgeObjects.TryGetValue(dependentObject, out objectBridge)) {
                objectBridge = new ComponentDependentObjectBridge(dependentObject);
                bridgeObjects.Add(dependentObject, objectBridge);
            } else {
                throw new ArgumentException("This dependent object is already subscribed for its dependencies.");
            }
            dependenciesRegistry.SubscribeForDependencies(new ComponentDependentObjectBridge(dependentObject));
        }

        public void Unsubscribe(IComponentDependentObject dependentObject) {
            if (null == dependentObject) {
                throw new ArgumentNullException("dependentObject");
            }
            //
            ComponentDependentObjectBridge objectBridge;
            if (!bridgeObjects.TryGetValue(dependentObject, out objectBridge)) {
                throw new ArgumentException("Unknown dependent object.", "dependentObject");
            } else {
                dependenciesRegistry.Unsubscribe(objectBridge);
                bridgeObjects.Remove(dependentObject);
            }
        }

        public void SubscribeForDependencies(AbstractPresenter presenter) {
            dependenciesRegistry.SubscribeForDependencies(presenter);
        }

        public void Unsubscribe(AbstractPresenter presenter) {
            dependenciesRegistry.Unsubscribe(presenter);
        }

        /// <summary>
        /// Merges type-level attributes and property-level attributes.
        /// </summary>
        internal static List<DependencyAttributeInfo> GetDependencyAttributes(Type type) {
            List<DependencyAttributeInfo> res = new List<DependencyAttributeInfo>();
            //
            object[] typeAttributes = type.GetCustomAttributes(typeof(MvpDependencyAttribute), true);
            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo propertyInfo in propertyInfos) {
                object[] propertyAttributes = propertyInfo.GetCustomAttributes(typeof(MvpDependencyAttribute), true);
                if (propertyAttributes.Length > 1) {
                    throw new InvalidOperationException("Multiple dependency attributes on property is not supported.");
                }
                if (propertyAttributes.Length == 1) {
                    MvpDependencyAttribute dependencyAttribute = (MvpDependencyAttribute) propertyAttributes[0];
                    if (dependencyAttribute.Type == null) {
                        dependencyAttribute.Type = propertyInfo.PropertyType;
                    } else {
                        if (dependencyAttribute.Type != propertyInfo.PropertyType) {
                            throw new InvalidOperationException("Property type and type of dependency attribute is not match.");
                        }
                    }
                    if (res.Any(info => info.Attribute.Equals(dependencyAttribute))) {
                        throw new InvalidOperationException("Duplicate dependency attribute declaration.");
                    }
                    res.Add(new DependencyAttributeInfo(dependencyAttribute, propertyInfo));
                }
            }
            //
            foreach (object attribute in typeAttributes) {
                MvpDependencyAttribute dependencyAttribute = (MvpDependencyAttribute) attribute;
                if (dependencyAttribute.Type == null) {
                    throw new InvalidOperationException("Presenter type must be specified when annotating whole class.");
                }
                if (!res.Any(info => info.Attribute.Equals(dependencyAttribute))) {
                    res.Add(new DependencyAttributeInfo(dependencyAttribute, null));
                }
            }
            //
            return res;
        }
    }
}