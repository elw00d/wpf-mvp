using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Wpf.Mvp
{
	/// <summary>
	/// Command pattern abstraction.
	/// </summary>
	public abstract class BaseCommand : ICommand, INotifyPropertyChanged
	{
		private string name;

		protected BaseCommand(string name) {
		    if (null == name) throw new ArgumentNullException("name");
		    if (name.Length == 0) throw new ArgumentException("String is empty", "name");
		    this.name = name;
		}

		protected BaseCommand() {
		}

		/// <summary>
		/// Name of Command.
		/// </summary>
		/// <remarks>
		/// Each Command have name. Command name should be unique in the scope of presenter.
		/// </remarks>
		public string Name {
			get {
				return (name);
			}
			set {
			    if (null == value) throw new ArgumentNullException("value");
			    if (value.Length == 0) throw new ArgumentException("String is empty", "value");
			    if (string.Compare(name, value, StringComparison.Ordinal) != 0) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
        
		#region ICommand Members

		/// <summary>
		/// Defines the way to know about when command can be executed.
		/// </summary>
		public event EventHandler CanExecuteChanged {
			add {
				AddCanExecuteChanged(value);
			}
			remove {
				RemoveCanExecuteChanged(value);
			}
		}

		/// <summary>
		/// Executes command with specified parameter.
		/// </summary>
		/// <param name="parameter">Parameter for command's logic.</param>
		public void Execute(object parameter) {
			if (CanExecuteInternal(parameter)) {
				ExecuteInternal(parameter);
			}
		}

		/// <summary>
		/// Checks is command can be executed or not.
		/// </summary>
		/// <param name="parameter">Parameter for command's logic.</param>
		/// <returns><code>True</code> if command can be executed. Otherwise <code>False</code>.</returns>
		public bool CanExecute(object parameter) {
			return (CanExecuteInternal(parameter));
		}

		#endregion

		#region INotifyPropertyChanged Members

		///<summary>
		/// Occurs when a property value changes.
		///</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		/// <summary>
		/// Should be overriden in inherited classes to
		/// provide command's logic.
		/// </summary>
		/// <param name="parameter">Parameter for command's logic.</param>
		protected virtual void ExecuteInternal(object parameter) {
		}

		/// <summary>
		/// Should be overriden in inherited classes to provide information whether command can be
		/// executed and whether not.
		/// </summary>
		/// <param name="parameter">Parameter for command's logic.</param>
		/// <returns><code>True</code> if command can be executed. Else <code>False</code>.</returns>
		protected virtual bool CanExecuteInternal(object parameter) {
			return (true);
		}

		/// <summary>
		/// Raises <see cref="CanExecuteChanged"/> event. Internal
		/// library method to allow to control this event with presenters.
		/// </summary>
		internal void OnCanExecuteChangedInternal() {
			OnCanExecuteChanged();
		}

		/// <summary>
		/// Should be overriden in inherited classes only if <see cref="AddCanExecuteChanged"/> and
		/// <see cref="RemoveCanExecuteChanged"/> methods was overriden.
		/// </summary>
		protected virtual void OnCanExecuteChanged() {
			EventHandler handler = canExecuteChangedInternal;
			if (handler != null) {
				handler(this, EventArgs.Empty);
			}
		}

		private event EventHandler canExecuteChangedInternal;

		/// <summary>
		/// Changes logic how to subscribe to <see cref="CanExecuteChanged"/>
		/// event. If overriden in inherited class, then <see cref="RemoveCanExecuteChanged"/>
		/// also should be overriden.
		/// </summary>
		/// <param name="handler">Handler of subscriber.</param>
		protected virtual void AddCanExecuteChanged(EventHandler handler) {
			canExecuteChangedInternal += handler;
		}

		/// <summary>
		/// Changes logic how to unsubscribe from <see cref="CanExecuteChanged"/>
		/// event. If overriden in inherited class, then <see cref="AddCanExecuteChanged"/>
		/// also should be overriden.
		/// </summary>
		/// <param name="handler"></param>
		protected virtual void RemoveCanExecuteChanged(EventHandler handler) {
			canExecuteChangedInternal -= handler;
		}

		/// <summary>
		/// Called when property is changed. Raises <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="propertyName">Name of property which has changed.</param>
		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
		}
	}
}
