using System;
using System.ComponentModel;
using System.Windows;

namespace Wpf.Mvp {

    /// <summary>
    /// Non-generic super class for generic BaseWindow. Declares InstanceName dependency property and some other stuff.
    /// </summary>
    public class BaseWindowNonGeneric : Window {
        
        public static readonly DependencyProperty InstanceNameProperty =
            DependencyProperty.Register("InstanceName", typeof(string), typeof(BaseWindowNonGeneric),
            new PropertyMetadata(default(string), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs) {
            ((BaseWindowNonGeneric) dependencyObject).InstanceNameChanged(dependencyPropertyChangedEventArgs);
        }

        protected virtual void InstanceNameChanged(DependencyPropertyChangedEventArgs eventArgs) {
        }

        public string InstanceName {
            get {
                return (string) GetValue(InstanceNameProperty);
            }
            set {
                SetValue(InstanceNameProperty, value);
            }
        }
        
    }

    /// <summary>
    /// Super class for windows managed using ComponentRegistry.
    /// </summary>
    /// <typeparam name="TModel">Type argument of view model.</typeparam>
    /// <typeparam name="TPresenter">Type argument of presenter.</typeparam>
    /// <typeparam name="TView">Type argument of view interface.</typeparam>
    public class BaseWindow<TModel, TPresenter, TView> : BaseWindowNonGeneric where TPresenter : BasePresenter<TModel, TView>, new()
                                                                 where TModel : class, INotifyPropertyChanged, new()
                                                                 where TView : class, IBaseView
     {
        public BaseWindow() : this(new TModel()) {
        }

        protected override void InstanceNameChanged(DependencyPropertyChangedEventArgs eventArgs) {
            // delegate it to presenter
            if (eventArgs.OldValue != null) {
                throw new InvalidOperationException("Cannot replace already specified InstanceName dependency property.");
            }
            if (!DesignerProperties.GetIsInDesignMode(this)) {
                this.presenter.InstanceName = (string) eventArgs.NewValue;
            }
        }

        public BaseWindow(TModel model) {
            this.presenter = new TPresenter();
            //
            this.model = model;
            presenter.Model = model;
            if (!(this is TView)) {
                throw new InvalidOperationException("You should implement TView interface for your view.");
            }
            presenter.View = this as TView;
        }

        private TModel model;

        public TModel Model {
            get {
                return model;
            }
            set {
                if (model != value) {
					TModel oldModel = model;
					model = value;
					this.DataContext = value;
                    // don't call presenter's Model setter from designer mode to avoid
                    // possible unwanted exceptions during design
                    if (!DesignerProperties.GetIsInDesignMode(this)) {
                        this.presenter.Model = value;
                        OnModelChangedInternal(oldModel, value);
                    }
                }
            }
        }

		protected virtual void OnModelChangedInternal(TModel oldModel, TModel newModel) {
		}

        private readonly TPresenter presenter;

        /// <summary>
        /// No concrete presenter info available to concrete view, only base presenter interface.
        /// По идее доступ к презентеру можно вообще убрать, но возможно это иногда будет полезно.
        /// В любом случае придется приводить к конкретному типу, чтобы что-то сделать.
        /// Так что костыли будут видны сразу.
        /// </summary>
        protected BasePresenter<TModel, TView> Presenter {
            get {
                return presenter;
            }
        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
            //
            this.DataContext = this.model;
			if (!DesignerProperties.GetIsInDesignMode(this)) {
                presenter.InitializeInstance();
			}
        	// set flags in resources here because if do that in constructor and window has resources
            // definition, they will be replaced in InitializeComponent() method call
            Resources[Constants.RESOURCE_DICTIONARY_VIEW_FLAG_KEY] = true;
            Resources[Constants.RESOURCE_DICTIONARY_PRESENTER_KEY] = presenter;
        }

        /// <summary>
        /// This method presents in Window only (not in UserControl) so user controls
        /// should unregister themselves manually.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            //
            presenter.WindowClosed();
        }
     }
}