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

namespace Q_Platform.Views.UC.Base
{
    /// <summary>
    /// ServoStatusUC.xaml 的交互逻辑
    /// </summary>
    public partial class ServoStatusUC : UserControl
    {
        public ServoStatusUC()
        {
            InitializeComponent();
        }


        public uint IoStatus
        {
            get { return (uint)GetValue(IoStatusProperty); }
            set { SetValue(IoStatusProperty, value); }
        }


        public static readonly DependencyProperty IoStatusProperty =
            DependencyProperty.Register("IoStatus", typeof(uint), typeof(ServoStatusUC), new FrameworkPropertyMetadata(default(uint), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnIoStatusPropertyChanged)));



        public static void OnIoStatusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as ServoStatusUC;
            if (instance == null)
            {
                return;
            }

            uint index = 0;
            foreach (var item in instance.io_StackPanel.Children)
            {
                var cb = item as CheckBox;
                if (cb == null)
                {
                    return;
                }
                cb.IsChecked = (instance.IoStatus & (uint)Math.Pow(2, index)) == (uint)Math.Pow(2, index);
                index++;
            }

        }




        public int MotionStatus
        {
            get { return (int)GetValue(MotionStatusProperty); }
            set { SetValue(MotionStatusProperty, value); }
        }

        public static readonly DependencyProperty MotionStatusProperty =
            DependencyProperty.Register("MotionStatus", typeof(int), typeof(ServoStatusUC), new FrameworkPropertyMetadata(default(int), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnMotionStatusPropertyChanged)));



        private static void OnMotionStatusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as ServoStatusUC;
            if (instance == null)
            {
                return;
            }

            int index = 0;
            foreach (var item in instance.motionStatusPanel.Children)
            {
                var cb = item as CheckBox;
                if (cb == null)
                {
                    return;
                }
                cb.IsChecked = (instance.MotionStatus & (int)Math.Pow(2, index)) == (int)Math.Pow(2, index);
                index++;
            }
        }
    }
}
