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
    /// StepStatusUC.xaml 的交互逻辑
    /// </summary>
    public partial class StepStatusUC : UserControl
    {
        public StepStatusUC()
        {
            InitializeComponent();
        }

        public int IoStatus
        {
            get { return (int)GetValue(IoStatusProperty); }
            set { SetValue(IoStatusProperty, value); }
        }


        public static readonly DependencyProperty IoStatusProperty =
            DependencyProperty.Register("IoStatus", typeof(int), typeof(StepStatusUC), new FrameworkPropertyMetadata(default(int), new PropertyChangedCallback(OnIoStatusPropertyChanged)));



        public static void OnIoStatusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as StepStatusUC;
            if (instance == null)
            {
                return;
            }

            int index = 0;
            foreach (var item in instance.io_StackPanel.Children)
            {
                var cb = item as CheckBox;
                if (cb == null)
                {
                    return;
                }
                cb.IsChecked = (instance.IoStatus & (int)Math.Pow(2, index)) == (int)Math.Pow(2, index);
                index++;
            }

        }
    }
}
