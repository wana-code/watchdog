using Microsoft.Xaml.Behaviors;
using System.Windows;
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
            // 화면의 해상도를 가져옵니다.
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double windowHeight = SystemParameters.PrimaryScreenHeight - 50;

            // 윈도우의 너비를 설정합니다. (필요에 따라 조정)
            double widthRatio = 0.3;

            if (MainWidthRatio == DispWithRatio.Disp_20P)
            {
                widthRatio = 0.2;
            }
            else if (MainWidthRatio == DispWithRatio.Disp_30P)
            {
                widthRatio = 0.3;
            }
            else if (MainWidthRatio == DispWithRatio.Disp_40P)
            {
                widthRatio = 0.4;
            }

            //전체 화면 대비 20% 사용            
            double windowWidth = screenWidth * widthRatio; // 예시 너비

            // 윈도우의 위치를 화면 오른쪽 끝으로 설정합니다.
            AssociatedObject.Left = screenWidth - windowWidth;
            AssociatedObject.Top = 0; // 화면 하단에 위치시킬 경우
            AssociatedObject.Height = windowHeight;
            AssociatedObject.Width = windowWidth;
        }
    }
}
