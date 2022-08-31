using Q_Platform.Models;
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
    /// TechPosUC.xaml 的交互逻辑
    /// </summary>
    public partial class TechPosUC : UserControl
    {
        public TechPosUC()
        {
            InitializeComponent();
            
        }


        public IEnumerable<AxisPosInfo> AxisPosInfosList
        {
            get { return (IEnumerable<AxisPosInfo>)GetValue(AxisPosInfosListProperty); }
            set { SetValue(AxisPosInfosListProperty, value); }
            
        }

        public static readonly DependencyProperty AxisPosInfosListProperty =
            DependencyProperty.Register("AxisPosInfosList", typeof(IEnumerable<AxisPosInfo>), typeof(TechPosUC), new FrameworkPropertyMetadata(default(IEnumerable<AxisPosInfo>),new PropertyChangedCallback(OnListChanged)));

        private static void OnListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as TechPosUC;
            if (d == null)
            {
                return;
            }
            var itemsCtr = instance.FindName("itemsControl") as ItemsControl;
            if (itemsCtr == null)
            {
                return;
            }
            itemsCtr.ItemsSource = null;
            itemsCtr.ItemsSource = instance.AxisPosInfosList;
        }
    }
}
