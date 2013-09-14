using System;
using System.Collections.Generic;
using System.Linq;

namespace Wpf.Mvp.Dependencies
{
    internal interface IDependentObject {
        void DependencyLoaded(IDependencyObject dependency);

        void DependencyUnloaded(IDependencyObject dependency);

        string[] GetDependencies();
    }

    internal interface IDependencyObject {
        string InstanceName {
            get;
        }
    }

    /// <summary>
    /// Manages the dependencies between abstract <see cref="IDependentObject"/>'s and
    /// <see cref="IDependencyObject"/>'s. You can implement this interfaces and use registry to
    /// resolve dependencies.
    /// </summary>
    internal sealed class DependenciesRegistry {

        // List of objects registered as dependency objects (only map by name)
        private readonly Dictionary<string, IDependencyObject> dependencyObjects = new Dictionary<string, IDependencyObject>();
        // Multidictionary specifying the list of dependent objects for every instance name
        private readonly Dictionary<string, List<IDependentObject>> dependentObjects = new Dictionary<string, List<IDependentObject>>();
        // Multidictionary specifying the list of dependencies that dependent object is waiting for
        private readonly Dictionary<IDependentObject, List<string>> dependenciesWaiting = new Dictionary<IDependentObject, List<string>>();

        public void RegisterDependency(IDependencyObject dependency) {
            if (null == dependency) {
                throw new ArgumentNullException("dependency");
            }
            if (string.IsNullOrEmpty(dependency.InstanceName)) {
                throw new ArgumentException("Dependency object must have defined name.");
            }
            if (dependencyObjects.ContainsKey(dependency.InstanceName)) {
                throw new ArgumentException("Dependency with this name has already been registered.", "dependency");
            }
            // add it to dependencies registry
            dependencyObjects.Add(dependency.InstanceName, dependency);
            // notify all subscribers about loaded dependency
            if (dependentObjects.ContainsKey(dependency.InstanceName)) {
                // make a copy (to avoid possible modification while enumerating)
                List<IDependentObject> _dependentObjects = dependentObjects[dependency.InstanceName].ToList();
                foreach (IDependentObject dependentObject in _dependentObjects) {
                    dependentObject.DependencyLoaded(dependency);
                }
            }
        }

        public void UnregisterDependency(IDependencyObject dependency) {
            if (null == dependency) {
                throw new ArgumentNullException("dependency");
            }
            if (!dependencyObjects.ContainsKey(dependency.InstanceName)) {
                throw new ArgumentException("Dependency with this name is not registered.", "dependency");
            }
            // notify all subscribers about unloaded dependency
            if (dependentObjects.ContainsKey(dependency.InstanceName)) {
                // make a copy (to avoid possible modification while enumerating)
                List<IDependentObject> _dependentObjects = dependentObjects[dependency.InstanceName].ToList();
                foreach (IDependentObject dependentObject in _dependentObjects) {
                    dependentObject.DependencyUnloaded(dependency);
                }
            }
            // remove it from dependencies registry
            dependencyObjects.Remove(dependency.InstanceName);
        }

        public void SubscribeForDependencies(IDependentObject obj) {
            if (null == obj) {
                throw new ArgumentNullException("obj");
            }
            if (dependenciesWaiting.ContainsKey(obj)) {
                throw new ArgumentException("This dependent object has already been subscribed.", "obj");
            }
            //
            List<string> listOfDependenciesWaitingFor = obj.GetDependencies().ToList();
            dependenciesWaiting.Add(obj, listOfDependenciesWaitingFor);
            //
            foreach (string dependencyName in listOfDependenciesWaitingFor) {
                // if dependency is already registered, call DependencyLoaded
                if (dependencyObjects.ContainsKey(dependencyName)) {
                    obj.DependencyLoaded(dependencyObjects[dependencyName]);
                }
                // add obj to dependentObjects dictionary
                if (dependentObjects.ContainsKey(dependencyName)) {
                    dependentObjects[dependencyName].Add(obj);
                } else {
                    dependentObjects.Add(dependencyName, new List<IDependentObject> {
                        obj
                    });
                }
            }
        }

        public void Unsubscribe(IDependentObject obj) {
            if (null == obj) {
                throw new ArgumentNullException("obj");
            }
            if (!dependenciesWaiting.ContainsKey(obj)) {
                throw new ArgumentException("Specified dependent object has not been subscribed.", "obj");
            }
            // just clear info about dependencies what obj has waiting for
            List<string> listOfDependenciesWaitingFor = dependenciesWaiting[obj];
            foreach (string dependencyName in listOfDependenciesWaitingFor) {
                //
                List<IDependentObject> _dependentObjects = dependentObjects[dependencyName];
                _dependentObjects.Remove(obj);
                if (_dependentObjects.Count == 0) {
                    dependentObjects.Remove(dependencyName);
                }
            }
            //
            dependenciesWaiting.Remove(obj);
        }
    }
}
