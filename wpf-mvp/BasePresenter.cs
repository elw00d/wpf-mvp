using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Wpf.Mvp.Attributes;

namespace Wpf.Mvp {

    public interface ICommandsContainer {
        BaseCommand GetCommandByName(string name);
    }

    /// <summary>
    /// Represents information about what model value is changed and what is new value of model.
    /// </summary>
    /// <typeparam name="TModel">Type of Model.</typeparam>
    public class ModelChangedEventArgs<TModel> : EventArgs {
        public ModelChangedEventArgs(TModel oldModel, TModel newModel) {
            OldModel = oldModel;
            NewModel = newModel;
        }

        public TModel OldModel {
            get;
            private set;
        }

        public TModel NewModel {
            get;
            private set;
        }
    }

    /// <summary>
    /// Base presenter class for all components. Marked by <see cref="PresenterCommandAttribute"/> with
    /// default presenter command implementation.
    /// </summary>
    /// <typeparam name="TModel">Type of view model.</typeparam>
    /// <typeparam name="TView">Type of view (interface, not concrete class).</typeparam>
    [PresenterCommand("PresenterMethodCommand")]
    public class BasePresenter<TModel, TView> : AbstractPresenter, ICommandsContainer
        where TModel : class, INotifyPropertyChanged
        where TView : IBaseView {
        //
        protected TView view;

        public TView View {
            get {
                return view;
            }
            set {
                view = value;
            }
        }

        protected BasePresenter() {
            analyzeMethods();
        }

        /// <summary>
        /// Позволяет добавить пользовательские команды к тем, которые определены из анализа методов.
        /// Пользовательские команды должны быть производными от BaseCommand.
        /// На пользовательские команды можно ссылаться в разметке XAML при помощи расширения CommandRef так же, как и
        /// при работе с командами-методами.
        /// Метод презентера CanExecuteAction для пользовательских команд вызываться не будет, пользовательские
        /// команды будут опрашиваться напрямую.
        /// Метод GetCustomCommands вызывается в базовой реализации метода Initialize, поэтому если вы переопределяете
        /// Initialize и не вызываете базовую реализацию, метод GetCustomCommands вызван не будет.
        /// </summary>
        protected virtual void GetCustomCommands(List<BaseCommand> customCommands) {
        }

        private TModel model;

        public TModel Model {
            get {
                return model;
            }
            set {
                if (model != value) {
                    TModel oldModel = model;
                    TModel newModel = value;
                    //
                    if (model != null) {
                        model.PropertyChanged -= onModelPropertyChanged;
                    }
                    model = value;
                    if (model != null) {
                        model.PropertyChanged += onModelPropertyChanged;
                    }
                    onModelChanged(new ModelChangedEventArgs<TModel>(oldModel, newModel));
                }
            }
        }

        private void onModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
            OnModelPropertyChanged(e.PropertyName);
        }

        protected virtual void OnModelPropertyChanged(string propertyName) {
        }

        protected virtual void OnModelChanged(ModelChangedEventArgs<TModel> e) {
        }

        /// <summary>
        /// Occurs when model was changed;
        /// </summary>
        public event EventHandler<ModelChangedEventArgs<TModel>>  ModelChanged;

        private void onModelChanged(ModelChangedEventArgs<TModel> args) {
            OnModelChanged(args);
            EventHandler<ModelChangedEventArgs<TModel>> modelChangedHandler = ModelChanged;
            if (modelChangedHandler != null) {
                modelChangedHandler(this, args);
            }
        }

        protected void OnCanExecuteChangedAll() {
            foreach (BaseCommand action in commands) {
                action.OnCanExecuteChangedInternal();
            }
        }

        protected void OnCanExecuteChanged(BaseCommand command) {
            if (null == command) throw new ArgumentNullException("command");
            command.OnCanExecuteChangedInternal();
        }

        protected void OnCanExecuteChanged(string commandName) {
            if (null == commandName) throw new ArgumentNullException("commandName");
            if (commandName.Length == 0) throw new ArgumentException("String is empty", "commandName");
            BaseCommand command = this.GetCommandByName(commandName);
            command.OnCanExecuteChangedInternal();
        }

        protected void OnCanExecuteChanged(ICommand command) {
            if (null == command) throw new ArgumentNullException("command");
            BaseCommand commandCommand = command as BaseCommand;
            if (commandCommand != null) {
                commandCommand.OnCanExecuteChangedInternal();
            }
        }

        protected override void Initialize() {
        }

        /// <summary>
        /// Called from BaseWindow or BaseUserControl while component being initialized.
        /// Во время выполнения этого метода команды, определенные в xaml с помощью CommandRef, еще
        /// не связаны с командами презентера, но после выполнения метода у элементов визуального дерева будет
        /// вызван обработчик Loaded, в котором WpfCommandProxy будет искать каждая свою команды по имени,
        /// обращаясь к презентеру. Поэтому это единственная точка, в которой можно добавить свои кастомные команды.
        /// В конструкторе их добавлять нельзя, поскольку это обернется вызовом виртуального метода в конструкторе.
        /// После Initialize() их добавлять нельзя, поскольку все CommandRefs уже выполнили подстановку команд, и
        /// больше в процессе связывания не участвуют.
        /// </summary>
        protected override void InitializeCommands() {
            List<BaseCommand> customCommands = new List<BaseCommand>();
            GetCustomCommands(customCommands);
            foreach (BaseCommand customCommand in customCommands) {
                commandsByName[customCommand.Name] = customCommand;
                commands.Add(customCommand);
            }
        }

        protected virtual bool CanExecuteCommand(PresenterMethodCommand command, object parameter) {
            return false;
        }

        private readonly List<BaseCommand> commands = new List<BaseCommand>();
        private readonly Dictionary<string, BaseCommand> commandsByName = new Dictionary<string, BaseCommand>();

        /// <summary>
        /// 1. Ищем методы, аннотированные атрибутом, производным от CommandAttribute
        /// 2. Определяем тип всех Command'ов для данного презентера по атрибуту PresenterCommandAttribute
        /// 3. Для каждого метода создаем инстанс типа, найденного на втором шаге
        /// 4. Пихаем все это в Dictionary
        /// </summary>
        private void analyzeMethods() {
            Type presenterType = this.GetType();
            PresenterCommandAttribute baseCommandAttribute = null;
            Type objectType = typeof (Object);
            while (presenterType != objectType) {
                Attribute presenterAction = Attribute.GetCustomAttribute(presenterType,
                                                                         typeof (PresenterCommandAttribute), false);
                if (presenterAction == null) {
                    presenterType = presenterType.BaseType;
                    if (presenterType == null) {
                        throw new InvalidOperationException("Variable presenterType cannot be null here.");
                    }
                    continue;
                }
                baseCommandAttribute = (PresenterCommandAttribute) presenterAction;
                break;
            }
            if (presenterType == null || presenterType == objectType || baseCommandAttribute == null) {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                  "Cannot compile presenter, '{0}' attribute not found.",
                                                                  typeof (PresenterCommandAttribute).FullName));
            }
            //
            Type baseActionType =
                presenterType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(
                    type => type.Name == baseCommandAttribute.ActionTypeName);
            if (baseActionType == null) {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                  "Cannot compile presenter, '{0}' attribute found, but base Command is not. Attribute defines following name: {1}",
                                                                  typeof (PresenterCommandAttribute).FullName,
                                                                  baseCommandAttribute.ActionTypeName));
            }
            if (baseActionType.IsAbstract) {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                  "Cannot compile presenter, '{0}' attribute defines following base Command {1}, but this type is abstract.",
                                                                  typeof (PresenterCommandAttribute).FullName,
                                                                  baseActionType.FullName));
            }
            if (baseActionType.ContainsGenericParameters) {
                baseActionType = baseActionType.MakeGenericType(presenterType.GetGenericArguments());
            }
            //
            ConstructorInfo[] constructors =
                baseActionType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (constructors == null || constructors.Length == 0) {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                  "Cannot compile presenter. '{0}' attribute defines following base Command {1}, but this type does not contain any constructor.",
                                                                  typeof (PresenterCommandAttribute).FullName,
                                                                  baseActionType.FullName));
            }
            //
            ConstructorInfo constructorWithPresenter = null;
            ConstructorInfo constructorWithPresenterAndAttribute = null;
            searchConstructors(baseActionType, constructors, ref constructorWithPresenter,
                               ref constructorWithPresenterAndAttribute);
            //
            Dictionary<string, CommandAttribute> actionAttributes = new Dictionary<string, CommandAttribute>();
            Dictionary<CommandAttribute, MethodInfo> actionMethods = new Dictionary<CommandAttribute, MethodInfo>();
            presenterType = this.GetType();
            // todo : presenterType != typeof (BasePresenter<,>) doesnt work
            while (presenterType != null && presenterType != typeof (BasePresenter<,>) && presenterType != objectType) {
                //
                MethodInfo[] methods =
                    presenterType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
                                             BindingFlags.Static |
                                             BindingFlags.DeclaredOnly);
                foreach (MethodInfo info in methods) {
                    Attribute attribute = Attribute.GetCustomAttribute(info, typeof (CommandAttribute), false);
                    if (attribute != null) {
                        CommandAttribute commandAttribute = attribute as CommandAttribute;

                        if (commandAttribute != null) {
                            //
                            if (string.IsNullOrEmpty(commandAttribute.Name)) {
                                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                                  "Method '{0}' defines an Command which have empty or null name.",
                                                                                  info));
                            }
                            if (actionAttributes.ContainsKey(commandAttribute.Name)) {
                                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                                  "Method '{0}' defines an Command with name which already used.",
                                                                                  info));
                            }
                            actionAttributes.Add(commandAttribute.Name, commandAttribute);
                            actionMethods.Add(commandAttribute, info);
                            //
                        }
                    }
                }
                //
                presenterType = presenterType.BaseType;
            }

            foreach (KeyValuePair<CommandAttribute, MethodInfo> pair in actionMethods) {
                CommandAttribute commandAttribute = pair.Key;
                MethodInfo info = pair.Value;
                //
                BaseCommand instance = null;
                if (constructorWithPresenterAndAttribute != null) {
                    instance = (BaseCommand) constructorWithPresenterAndAttribute.Invoke(new object[] {
                        this, info, commandAttribute
                    });
                } else {
                    instance = (BaseCommand) constructorWithPresenter.Invoke(new object[] {
                        this, info
                    });
                }
                if (instance == null) {
                    throw new InvalidOperationException(
                        "Cannot create an Command instance using reflected constructors.");
                }
                instance.Name = commandAttribute.Name;
                this.AddCommand(instance);
            }
        }

        /// <summary>
        /// Add's specified <see cref="BaseCommand"/> instance to presenter's Command list.
        /// </summary>
        /// <param name="command">Command to add.</param>
        protected void AddCommand(BaseCommand command) {
            if (null == command) throw new ArgumentNullException("command");
            string value = command.Name;
            if (value == null) {
                throw new ArgumentNullException("command");
            }
            if (value.Length == 0) {
                throw new ArgumentException("Name of command cannot be null or empty.", "command");
            }
            //
            if (commandsByName.ContainsKey(command.Name)) {
                throw new ArgumentException("Command with specified name already exists.");
            }
            commands.Add(command);
            commandsByName.Add(command.Name, command);
        }

        private static void searchConstructors(Type baseCommandType, IEnumerable<ConstructorInfo> constructors,
                                               ref ConstructorInfo constructorWithPresenter,
                                               ref ConstructorInfo constructorWithPresenterAndAttribute) {
            foreach (ConstructorInfo info in constructors) {
                ParameterInfo[] parameters = info.GetParameters();
                if (parameters.Length == 2) {
                    ParameterInfo info0 = parameters[0];
                    ParameterInfo info1 = parameters[1];
                    if (IsBaseType(info0.ParameterType, typeof (BasePresenter<,>)) &&
                        typeof (MethodInfo).IsAssignableFrom(info1.ParameterType)) {
                        constructorWithPresenter = info;
                        continue;
                    }
                }
                if (parameters.Length == 3) {
                    ParameterInfo info0 = parameters[0];
                    ParameterInfo info1 = parameters[1];
                    ParameterInfo info2 = parameters[2];
                    if (IsBaseType(info0.ParameterType, typeof (BasePresenter<,>)) &&
                        typeof (MethodInfo).IsAssignableFrom(info1.ParameterType) &&
                        typeof (CommandAttribute).IsAssignableFrom(info2.ParameterType)) {
                        constructorWithPresenterAndAttribute = info;
                        continue;
                    }
                }
            }
            if (constructorWithPresenter == null && constructorWithPresenterAndAttribute == null) {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                  "Cannot compile presenter. '{0}' attribute defines following base Command {1}, but this type does not contain any of supported constructors.",
                                                                  typeof (PresenterCommandAttribute).FullName,
                                                                  baseCommandType.FullName));
            }
        }

        /// <summary>
        /// Default command which will be default base class for all presenter commands.
        /// You can derive from this class and implement advanced logic (for example, add roles support).
        /// </summary>
        protected class PresenterMethodCommand : BaseCommand {
            private readonly CommandAttribute commandAttribute;
            private readonly MethodInfo commandMethod;
            private readonly BasePresenter<TModel, TView> presenter;
            private bool commandMethodParameterCanBeNull;
            private Type actionMethodParameterType;

            /// <summary>
            /// Default constructor of Command which is bound to presenter.
            /// </summary>
            /// <param name="presenter">Presenter where this Command is bound.</param>
            protected PresenterMethodCommand(BasePresenter<TModel, TView> presenter) {
                if (null == presenter) throw new ArgumentNullException("presenter");
                //
                this.presenter = presenter;
            }

            /// <summary>
            /// Default constructor of Command which is bound to presenter and declares specific Command.
            /// </summary>
            /// <param name="presenter">Presenter where this Command is bound.</param>
            /// <param name="attributeInstance">Attribute defines this Command.</param>
            protected PresenterMethodCommand(BasePresenter<TModel, TView> presenter, CommandAttribute attributeInstance) {
                if (null == presenter) throw new ArgumentNullException("presenter");
                if (null == ( object ) attributeInstance) throw new ArgumentNullException("attributeInstance");
                //
                this.presenter = presenter;
                commandAttribute = attributeInstance;
                Name = commandAttribute.Name;
            }

            /// <summary>
            /// Default constructor for internal support of presenter's method invokation.
            /// </summary>
            /// <param name="presenter">Presenter where this Command is bound.</param>
            /// <param name="commandMethod">Method marked with attribute.</param>
            protected PresenterMethodCommand(BasePresenter<TModel, TView> presenter, MethodInfo commandMethod) {
                if (null == presenter) throw new ArgumentNullException("presenter");
                if (null == commandMethod) throw new ArgumentNullException("commandMethod");
                //
                this.presenter = presenter;
                this.commandMethod = commandMethod;
                initializeMethodInfo();
            }

            /// <summary>
            /// Default constructor for internal support of presenter's method invocation.
            /// </summary>
            /// <param name="presenter">Presenter where this Command is bound.</param>
            /// <param name="commandMethod">Method annotated with CommandAttribute.</param>
            /// <param name="attributeInstance">Instance of CommandAttibute used to annotate the method.</param>
            protected PresenterMethodCommand(BasePresenter<TModel, TView> presenter, MethodInfo commandMethod, CommandAttribute attributeInstance) {
                if (null == presenter) throw new ArgumentNullException("presenter");
                if (null == commandMethod) throw new ArgumentNullException("commandMethod");
                if (null == ( object ) attributeInstance) throw new ArgumentNullException("attributeInstance");
                //
                this.presenter = presenter;
                this.commandMethod = commandMethod;
                commandAttribute = attributeInstance;
                Name = commandAttribute.Name;
                initializeMethodInfo();
            }

            /// <summary>
            /// Attribute which defines this Command.
            /// </summary>
            public CommandAttribute CommandAttribute {
                get {
                    return (commandAttribute);
                }
            }

            private void initializeMethodInfo() {
                ParameterInfo[] parameters = commandMethod.GetParameters();
                if (parameters.Length > 1) {
                    throw new InvalidOperationException(
                        "Invalid Command method. Command method cannot contain more than one parameter.");
                }
                if (parameters.Length == 1) {
                    foreach (ParameterInfo info in parameters) {
                        actionMethodParameterType = info.ParameterType;
                        Attribute attribute = Attribute.GetCustomAttribute(info, typeof (NotNullAttribute));
                        commandMethodParameterCanBeNull = (attribute == null);
                    }
                } else {
                    if (parameters.Length == 0) {
                        commandMethodParameterCanBeNull = true;
                    }
                }
            }

            protected override bool CanExecuteInternal(object parameter) {
                if (presenter.CanExecuteCommand(this, parameter)) {
                    if (parameter == null) {
                        return commandMethodParameterCanBeNull && base.CanExecuteInternal(null);
                    }
                    return true;
                }
                return (false);
            }

            [DebuggerStepThrough]
            protected override void ExecuteInternal(object parameter) {
                if (commandMethod != null) {
                    if (commandMethod.IsStatic) {
                        if (actionMethodParameterType != null) {
                            commandMethod.Invoke(null, new[] {
                                parameter
                            });
                        } else {
                            commandMethod.Invoke(null, new Object[] { });
                        }
                    } else {
                        if (actionMethodParameterType != null) {
                            commandMethod.Invoke(presenter, new[] {
                                parameter
                            });
                        } else {
                            commandMethod.Invoke(presenter, new Object[] { });
                        }
                    }
                }
            }

            public override string ToString() {
                return ("[Command: " + Name + "]");
            }
        }

        private static bool IsBaseType(Type type, Type baseType) {
            if (null == type) throw new ArgumentNullException("type");
            if (null == baseType) throw new ArgumentNullException("baseType");
            //
            Type objectType = typeof (Object);
            while (type != objectType) {
                if (type == null) {
                    throw new InvalidOperationException("Variable type cannot be null here.");
                }
                //
                if (type == baseType) {
                    return true;
                }
                if (type.Module == baseType.Module &&
                    (type.Namespace + type.Name) == (baseType.Namespace + baseType.Name) &&
                    (type.IsGenericType && baseType.IsGenericTypeDefinition)) {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

    	public BaseCommand GetCommandByName(string name) {
			if (!commandsByName.ContainsKey(name)) {
				throw new InvalidOperationException(String.Format("Unknown command: {0}.", name));
			}
    		return commandsByName[name];
    	}
    }
}