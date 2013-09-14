using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Mvp;

namespace HelloWorld
{
    /// <summary>
    /// It is our View implementation. Remember that View have to implement IView interface!
    /// For quick editing XAML's x:TypeArguments you can use the next trick (with ReSharper plugin):
    /// 1. Change class declaration in xaml.cs:
    ///    MainWindow : BaseWindow<MainWindowModel ,MainWindowPresenter, ICloseableView>, ICloseableView
    /// 2. Copy "MainWindowModel ,MainWindowPresenter, ICloseableView"
    /// 3. Open XAML designer
    /// 4. Change root tag to BaseWindow, ReSharper will generate empty x:TypeArguments attribute
    /// 5. Paste into x:TypeArguments! All class names will be automatically resolved.
    /// </summary>
    public partial class MainWindow : BaseWindow<MainWindowModel ,MainWindowPresenter, ICloseableView>, ICloseableView
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
