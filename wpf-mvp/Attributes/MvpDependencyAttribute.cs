using System;

namespace Wpf.Mvp.Attributes {
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class MvpDependencyAttribute : Attribute {
        private readonly string instanceName;
        private readonly bool required;

        public Type Type {
            get;
            set;
        }

        public string InstanceName {
            get {
                return instanceName;
            }
        }

        public bool IsRequired {
            get {
                return required;
            }
        }

        public MvpDependencyAttribute(Type type) {
            if (type == null) {
                throw new ArgumentNullException("type");
            }
            //
            this.Type = type;
            this.required = true;
        }

        public MvpDependencyAttribute(Type type, string instanceName) {
            if (null == type) throw new ArgumentNullException("type");
            if (null == instanceName) throw new ArgumentNullException("instanceName");
            if (instanceName.Length == 0) throw new ArgumentException("String is empty", "instanceName");
            //
            this.Type = type;
            this.instanceName = instanceName;
            this.required = true;
        }

        public MvpDependencyAttribute(Type type, string instanceName, bool required) {
            if (null == type) throw new ArgumentNullException("type");
            if (instanceName == null) throw new ArgumentNullException("instanceName");
            if (instanceName.Length == 0) throw new ArgumentException("String is empty", "instanceName");
            //
            this.Type = type;
            this.instanceName = instanceName;
            this.required = required;
        }

        /// <summary>
        /// This constuctor can be used on properties only.
        /// </summary>
        public MvpDependencyAttribute(string instanceName) {
            if (null == instanceName) throw new ArgumentNullException("instanceName");
            if (instanceName.Length == 0) throw new ArgumentException("String is empty", "instanceName");
            //
            this.instanceName = instanceName;
            this.required = true;
        }

        /// <summary>
        /// This constructor can be used on properties only.
        /// </summary>
        public MvpDependencyAttribute(string instanceName, bool required) {
            if (null == instanceName) throw new ArgumentNullException("instanceName");
            if (instanceName.Length == 0) throw new ArgumentException("String is empty", "instanceName");
            //
            this.instanceName = instanceName;
            this.required = required;
        }

        public bool Equals(MvpDependencyAttribute other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(other.instanceName, instanceName) && Equals(other.Type, Type);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as MvpDependencyAttribute);
        }

        public override int GetHashCode() {
            unchecked {
                int result = base.GetHashCode();
                result = (result*397) ^ (instanceName != null ? instanceName.GetHashCode() : 0);
                result = (result*397) ^ (Type != null ? Type.GetHashCode() : 0);
                return result;
            }
        }
    }
}