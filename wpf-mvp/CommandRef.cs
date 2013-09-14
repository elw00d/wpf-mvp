using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Wpf.Mvp
{
	[MarkupExtensionReturnType(typeof(ICommand))]
	public class CommandRef : MarkupExtension, INotifyPropertyChanged
	{
		private string commandName;

		public CommandRef() {
		}

		public CommandRef(string commandName) {
		    if (null == commandName) throw new ArgumentNullException("commandName");
		    if (commandName.Length == 0) throw new ArgumentException("String is empty", "commandName");
		    //
			this.commandName = commandName;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public string CommandName {
			get {
				return (commandName);
			}
			set {
			    if (null == value) throw new ArgumentNullException("value");
			    if (value.Length == 0) throw new ArgumentException("String is empty", "value");
			    if (commandName != value) {
					commandName = value;
					onPropertyChanged("Name");
				}
			}
		}

        private string commandHolder;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public string CommandHolder {
			get {
                return commandHolder;
			}
			set {
			    if (null == value) throw new ArgumentNullException("value");
			    if (value.Length == 0) throw new ArgumentException("String is empty", "value");
			    if (commandHolder != value) {
                    commandHolder = value;
                    onPropertyChanged("CommandHolder");
				}
			}
		}

		public override object ProvideValue(IServiceProvider serviceProvider) {
			IProvideValueTarget service = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
			if (service == null) {
				throw new InvalidOperationException("IProvideValueTarget service not found.");
			}
		    if (service.TargetObject == null) {
		        // someone checks our return type. tell that all ok.
		    }
		    return (new WpfCommandProxy(CommandName, service.TargetObject, service.TargetProperty, CommandHolder));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void onPropertyChanged(string propertyName) {
			PropertyChangedEventHandler Handler = PropertyChanged;
			if (Handler != null) {
				Handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public sealed class WpfCommandProxy : ICommand {

			private readonly string commandName;
			private readonly object targetObject;
			private readonly object targetProperty;
		    private readonly string commandHolderExplicit;

			public WpfCommandProxy(string commandName, Object targetObject, Object targetProperty, string commandHolderExplicit) {
				this.commandName = commandName;
				this.targetObject = targetObject;
				this.targetProperty = targetProperty;
			    this.commandHolderExplicit = commandHolderExplicit;
				initialize();
			}

			public Object TargetObject {
				get {
					return (targetObject);
				}
			}

			public Object TargetProperty {
				get {
					return (targetProperty);
				}
			}

			private void initialize() {
				FrameworkElement element = targetObject as FrameworkElement;
				if (element != null) {
					if (!element.IsLoaded) {
						element.Loaded += elementLoaded;
					} else {
						injectCommand(element);
					}
				}
			}

			private void elementLoaded(object sender, EventArgs e) {
				FrameworkElement element = targetObject as FrameworkElement;
				if (element != null) {
					element.Loaded -= elementLoaded;
				}
				//
				injectCommand(element);
			}

			private void injectCommand(FrameworkElement element) {
				DependencyObject dependencyObject = (DependencyObject) targetObject;
				DependencyProperty property = (DependencyProperty) targetProperty;
				List<FrameworkElement> views = traverseTreeAndGetViews(element);
				if (views != null && views.Count != 0) {
				    FrameworkElement frameworkElement = null;
                    if (string.IsNullOrEmpty(commandHolderExplicit)) {
                        frameworkElement = views[0];
                    } else {
                        frameworkElement = views.FirstOrDefault(v => v.Name == commandHolderExplicit);
                        if (frameworkElement == null) {
                            throw new InvalidOperationException(string.Format("Cannot find Command holder by name {0}", commandHolderExplicit));
                        }
                    }
				    var commandsContainer = ((ICommandsContainer) frameworkElement.Resources[Constants.RESOURCE_DICTIONARY_PRESENTER_KEY]);
				    BaseCommand commandByName = commandsContainer.GetCommandByName(commandName);
					dependencyObject.SetValue(property, commandByName);
				}
			}

			public void Execute(object parameter) {
				throw new NotSupportedException("Cannot execute command directly from WpfCommandProxy.");
			}

			public bool CanExecute(object parameter) {
				return false;
			}

			public event EventHandler CanExecuteChanged;

			private static List<FrameworkElement> traverseTreeAndGetViews(DependencyObject element) {
			    if (null == element) throw new ArgumentNullException("element");
			    List<FrameworkElement> views = new List<FrameworkElement>();
				//
				DependencyObject parent = element;
				while (parent != null) {
					element = parent;
					parent = VisualTreeHelper.GetParent(element);
					if (parent == null && element is ContextMenu) {
						parent = (element as ContextMenu).PlacementTarget;
					}
					if (parent == null && element is Popup) {
						parent = (element as Popup).PlacementTarget;
					}
					if (parent == null && element is FrameworkElement) {
						parent = (element as FrameworkElement).Parent;
					}
					FrameworkElement frameworkElement = element as FrameworkElement;
					if (frameworkElement != null) {
						if (frameworkElement.Resources[Constants.RESOURCE_DICTIONARY_VIEW_FLAG_KEY] != null) {
							views.Add(frameworkElement);
						}
					}
				}
				return views;
			}
		}
	}
}
