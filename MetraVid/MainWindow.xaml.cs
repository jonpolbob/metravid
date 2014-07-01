using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveImage;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MetraEdit;




namespace MetroWPF
{

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WM_APP = 0x8000;
        private const int WM_GRAPHNOTIFY = WM_APP + 1;
        private const int EC_COMPLETE = 0x01;


        CapStack LeStack = null;
        public MainWindow()
        {
            InitializeComponent();
            LeStack = new CapStack();
            player.setstack(LeStack);


        }


        // elements de gestion de wndproc
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            if (msg == WM_GRAPHNOTIFY)  // WM_APP+1 = WM_GRAPHNOTIFY
            {
                //LeStack.ProcessSysMessage(wParam, lParam, ref handled);
                // wparam est null
                // lparam a le parametre passe dans setnotifywindow
            }

            return IntPtr.Zero;
        }

        private void MouseLDN(object sender, MouseButtonEventArgs e)
        {


            canvas1.VMouseDown(e.GetPosition(canvas1));

            /*Point locationInControl = e.GetPosition(canvas1);

            Xpos.Text = locationInControl.X.ToString();
            Ypos.Text = locationInControl.Y.ToString(); 
            rectangle1.SetValue(Canvas.LeftProperty, locationInControl.X);
            rectangle1.SetValue(Canvas.TopProperty, locationInControl.Y);  
             * */

        }

        private void MouseMOV(object sender, MouseEventArgs e)
        {
            canvas1.VMouseMove(e.GetPosition(canvas1));
        }

        private void MouseLUP(object sender, MouseButtonEventArgs e)
        {
        }

        private void buttonreglage_Click(object sender, RoutedEventArgs e)
        {
            LeStack.DisplayPropertyPage(1);
        }

        private void wnd_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LeStack.Uninit();
        }

        private void wnd_Initialized(object sender, EventArgs e)
        {
            LeStack.Init(true);
            LeStack.Start();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            canvas1.ModeEdit = VidCanvas.VisModeEdit.create;

        }
    }


    public class ScaleConverter : IMultiValueConverter
    {


        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            float v = (float)values[0];
            double m = (double)values[1];
            return v * m / 50;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }


}