using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Wpf.Mvp;
using Wpf.Mvp.Attributes;

namespace HelloWorld
{
    /// <summary>
    /// Here we are defining presenter for our component. It "depends" on Model class and
    /// View interface. So presenter can call Model object and View (but only through interface).
    /// And if you need to make some UI call from presenter, you should introduce appropriate
    /// method in View interface before that. This constraint enforces you to not interoperate with
    /// UI class directly. All such interoperations should be implemented through binding to Model's
    /// properties (like MVVM pattern).
    /// </summary>
    public class MainWindowPresenter : BasePresenter<MainWindowModel , ICloseableView>
    {
        /// <summary>
        /// Called when Window or UserControl called OnInitialized method.
        /// </summary>
        protected override void Initialize( ) {
            // The Model instance is already created, and you can initialize it with your data.
            //Model.Name = "Igor";
        }

        /// <summary>
        /// This method is invoked by presenter when need to define CanExecute status of some command
        /// Usually command is a "method command", but you can define your custom commands. 
        /// </summary>
        protected override bool CanExecuteCommand( PresenterMethodCommand command, object parameter ) {
            if ( command.Name == "SayHello" )
                return !string.IsNullOrEmpty( Model.Name );
            return false;
        }

        protected override void OnModelPropertyChanged( string propertyName ) {
            if ( ExpressionHelpers.GetPropertyName( ( ) => Model.Name ) == propertyName ) {
                // Tell presenter to refresh CanExecuteCommand status of our command
                OnCanExecuteChanged( "SayHello" );
            }
        }

        /// <summary>
        /// This method declares a Command with specified name.
        /// You can bind buttons or UI menus to this method using CommandRef markup extension.
        /// CanExecute status of method commands is defined in presenter method CanExecuteCommand().
        /// If you want to call command with parameter, just add the object argument to method signature.
        /// </summary>
        [Command("SayHello")]
        private void hello( ) {
            MessageBox.Show( "Hello, " + Model.Name );
        }
    }
}
