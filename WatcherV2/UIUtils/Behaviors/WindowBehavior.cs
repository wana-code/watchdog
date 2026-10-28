using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WatcherV2.Core;

namespace WatcherV2.UIUtils.Behaviors
{
    class WindowBehavior : Behavior<Window>
    {
        public string State
        {
            get { return (string)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }
        // DependencyProperty를 통해 State 속성에 대한 Binding을 처리할 수 있도록 설정
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(string), typeof(WindowBehavior),
                new FrameworkPropertyMetadata(null, OnStateChanged));

        public string WindowName
        {
            get { return (string)GetValue(WindowNameProperty); }
            set { SetValue(WindowNameProperty, value); }
        }
        // DependencyProperty를 통해 State 속성에 대한 Binding을 처리할 수 있도록 설정
        public static readonly DependencyProperty WindowNameProperty =
            DependencyProperty.Register("WindowName", typeof(string), typeof(WindowBehavior),
                new FrameworkPropertyMetadata(null, OnStateChanged));

        public DispWithRatio MainWidthRatio
        {
            get { return (DispWithRatio)GetValue(MainWidthRatioProperty); }
            set { SetValue(MainWidthRatioProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MainWidthRatio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MainWidthRatioProperty =
            DependencyProperty.Register("MainWidthRatio", typeof(DispWithRatio), typeof(WindowBehavior), new FrameworkPropertyMetadata(DispWithRatio.Disp_30P, OnStateChanged));

        protected override void OnAttached()
        {
            if (WindowName == "Main")
            {
                AssociatedObject.Loaded += MainWindow_Loaded;
            }
            UpdateState();
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (WindowBehavior)d;
            behavior.UpdateState();

            if (e.Property.Name == "MainWidthRatio" && behavior.AssociatedObject != null && behavior.WindowName == "Main")
            {
                behavior.AssociatedObject.Loaded -= behavior.MainWindow_Loaded;
                behavior.AssociatedObject.Loaded += behavior.MainWindow_Loaded;

                if (behavior.AssociatedObject.IsLoaded)
                {
                    behavior.ProcessMainWindow_Loaded();
                }
            }
        }
        private void UpdateState()
        {
            if (State != null)
            {
                switch (State)
                {
                    case "Min":
                        AssociatedObject.WindowState = WindowState.Minimized;
                        break;
                    case "Max":
                        AssociatedObject.WindowState = AssociatedObject.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                        break;
                    case "Close":
                        AssociatedObject.Close();
                        break;
                    case "Drag":
                        AssociatedObject.DragMove();
                        break;
                    default:
                        break;
                }
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ProcessMainWindow_Loaded();
        }

        private void ProcessMainWindow_Loaded()
        {
            var win = AssociatedObject;
            var wa = SystemParameters.WorkArea;   // 작업표시줄 제외, DIP 단위 (-50 하드코딩 대체)

            double widthRatio = 0.3;
            if (MainWidthRatio == DispWithRatio.Disp_20P) widthRatio = 0.2;
            else if (MainWidthRatio == DispWithRatio.Disp_40P) widthRatio = 0.4;

            win.WindowState = WindowState.Normal;
            win.SizeToContent = SizeToContent.Manual;

            double w = Math.Max(wa.Width * widthRatio, win.MinWidth);  // MinWidth보다 작게는 못 줄임
            win.Width = w;
            win.Height = wa.Height;
            win.Top = wa.Top;

            win.UpdateLayout();
            win.Left = wa.Right - win.ActualWidth;   // 실제 너비 기준으로 오른쪽 끝에 붙임
        }
    }
}
