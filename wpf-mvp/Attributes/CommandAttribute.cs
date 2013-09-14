using System;

namespace Wpf.Mvp.Attributes
{
    /// <summary>
	/// Marks method of presenter as "command" method. This allows us getting this method
	/// as "command" and then, bind to view if required.
	/// </summary>
	[MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public class CommandAttribute : Attribute, IEquatable<CommandAttribute> {
		private readonly string name;

		/// <summary>
		/// Instantiates <see cref="CommandAttribute"/> instance, which
		/// defines action with specified name.
		/// </summary>
		/// <param name="name">Name of action.</param>
		public CommandAttribute(string name) {
			this.name = name;
		}

		/// <summary>
		/// Name of action to declare.
		/// </summary>
		public string Name {
			get {
				return (name);
			}
		}

		public bool Equals(CommandAttribute obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return Equals(obj.name, name);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return Equals(obj as CommandAttribute);
		}

		public override int GetHashCode() {
			unchecked {
				{
					return (base.GetHashCode()*397) ^ name.GetHashCode();
				}
			}
		}

		public static bool operator ==(CommandAttribute left, CommandAttribute right) {
			return Equals(left, right);
		}

		public static bool operator !=(CommandAttribute left, CommandAttribute right) {
			return !Equals(left, right);
		}
	}
}
