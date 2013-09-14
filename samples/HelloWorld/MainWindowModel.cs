using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wpf.Mvp;

namespace HelloWorld
{
    /// <summary>
    /// This is our Model class. It holds the data for the View, View is bound to it.
    /// When some property is changed, Model should call OnPropertyChanged method with
    /// appropriate parameter to identify what property has been changed.
    /// </summary>
    public class MainWindowModel : BaseModel
    {
        private string name;
        public string Name {
            get { return name; }
            set {
                if ( name != value ) {
                    name = value;
                    // We dont need use string names to specify property,
                    // we can use lambda expressions for it, it allows refactor
                    // property names quick and safe
                    OnPropertyChanged( ( ) => Name );
                }
            }
        }
    }
}
