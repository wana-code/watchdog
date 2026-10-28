using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for UISideBar.xaml
    /// </summary>
    public partial class UISideBar : UserControl
    {
        public ObservableCollection<string> MenuItems
        {
            get { return (ObservableCollection<string>)GetValue(MenuItemsProperty); }
            set { SetValue(MenuItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MenuItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuItemsProperty =
            DependencyProperty.Register("MenuItems", typeof(ObservableCollection<string>), typeof(UISideBar));

        public DelegateCommand<string> BtnCommand
        {
            get { return (DelegateCommand<string>)GetValue(BtnCommandProperty); }
            set { SetValue(BtnCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BtnCommandProperty =
            DependencyProperty.Register("BtnCommand", typeof(DelegateCommand<string>), typeof(UISideBar));
        public UISideBar()
        {
            InitializeComponent();
        }
    }
}
