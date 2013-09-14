using System;

namespace Wpf.Mvp.Attributes
{
    /// <summary>
	/// Provides way to define base class for presenter actions. Base action should
	/// be nested, public (or protected). Also default constructor required. Also, constructor
	/// with parameter of presenter type can be defined. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PresenterCommandAttribute : Attribute, IEquatable<PresenterCommandAttribute> {
		private readonly string commandTypeName;

		/// <summary>
		/// Defines base class for all presenter's actions.
		/// </summary>
		/// <param name="commandTypeName">Short class name of presenter's nested class.</param>
		public PresenterCommandAttribute(string commandTypeName) {
		    if (commandTypeName == null) throw new ArgumentNullException("commandTypeName");
		    if (commandTypeName.Length == 0) throw new ArgumentException("String is empty", "commandTypeName");
		    this.commandTypeName = commandTypeName;
		}

		/// <summary>
		/// Name of base class.
		/// </summary>
		public string ActionTypeName {
			get {
				return (commandTypeName);
			}
		}

		public bool Equals(PresenterCommandAttribute obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return Equals(obj.commandTypeName, commandTypeName);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return Equals(obj as PresenterCommandAttribute);
		}

		public override int GetHashCode() {
			unchecked {
				{
					return (base.GetHashCode()*397) ^ (commandTypeName != null ? commandTypeName.GetHashCode() : 0);
				}
			}
		}

		public static bool operator ==(PresenterCommandAttribute left, PresenterCommandAttribute right) {
			return Equals(left, right);
		}

		public static bool operator !=(PresenterCommandAttribute left, PresenterCommandAttribute right) {
			return !Equals(left, right);
		}
	}
}
