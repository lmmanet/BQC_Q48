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
    /// TubePos.xaml 的交互逻辑
    /// </summary>
    public partial class TubePos : UserControl
    {
        public TubePos()
        {
            InitializeComponent();
        }



        public int SampleCount
        {
            get { return (int)GetValue(SampleCountProperty); }
            set { SetValue(SampleCountProperty, value); }
        }

      
        public static readonly DependencyProperty SampleCountProperty =
            DependencyProperty.Register("SampleCount", typeof(int), typeof(TubePos), new FrameworkPropertyMetadata(default(int),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPropertyChanged)));

        public static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uc = d as TubePos;
            if (uc == null)
            {
                return;
            }

            int value = uc.SampleCount;

            int index = 0;
            foreach (var children in uc.gridPanel.Children)
            {
                var button = children as Button;
                var stackpanel = button.Content as StackPanel;

                foreach (var item in stackpanel.Children)
                {
                    var checkbox = item as CheckBox;
                    int pValue = (int)Math.Pow(2, index);
                    checkbox.IsChecked = (value & pValue) == pValue;
                }
                index++;
            }

        }







        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(TubePos), new PropertyMetadata(default(ICommand)));





        private void gridPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var btn = e.Source as Button;
            if (btn == null)
            {
                return;
            }
            string name = btn.Name;
            this.Command?.Execute(name);
        }
    }
}
