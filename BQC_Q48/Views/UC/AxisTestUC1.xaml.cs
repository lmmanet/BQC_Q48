using BQC_Q48.ViewModels;
using Q_Platform.ViewModels.UC;
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

namespace Q_Platform.Views.UC
{
    /// <summary>
    /// AxisTestUC1.xaml 的交互逻辑
    /// </summary>
    public partial class AxisTestUC1 : UserControl
    {
        public AxisTestUC1()
        {
            InitializeComponent();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.Cleanup<AxisTestUCViewModel>();
        }
    }
}
