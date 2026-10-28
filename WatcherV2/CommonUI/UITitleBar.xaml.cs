using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WatcherV2.CommonUI
{
    /// <summary>
    /// Interaction logic for UITitleBar.xaml
    /// </summary>
    public partial class UITitleBar : UserControl
    {
        public string UITitleText
        {
            get { return (string)GetValue(UITitleTextProperty); }
            set { SetValue(UITitleTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UITitleText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UITitleTextProperty =
            DependencyProperty.Register("UITitleText", typeof(string), typeof(UITitleBar));

        public DelegateCommand<string> BtnCommand
        {
            get { return (DelegateCommand<string>)GetValue(BtnCommandProperty); }
            set { SetValue(BtnCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BtnCommandProperty =
            DependencyProperty.Register("BtnCommand", typeof(DelegateCommand<string>), typeof(UITitleBar));

        public UITitleBar()
        {
            InitializeComponent();
        }
    }
}
