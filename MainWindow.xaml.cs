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
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace ProtocolEditor
{
    public enum ItemType {
        None = -1,
        NameSpace,
        Message,
        Variable,
        Class
    }

    public class TreeViewItemArg
    {
        public ItemType typeId;
        public int index;
    }

    

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            nameTextBox.Visibility = Visibility.Hidden;
            commentTextBox.Visibility = Visibility.Hidden;
            msgAttrPanel.Visibility = Visibility.Hidden;
            varAttrPanel.Visibility = Visibility.Hidden;
        }

        private void refreshShowHide()
        {
            ItemType typeId = ItemType.None;
            int tabIdx = tabControl.SelectedIndex;
            if (tabIdx == 0)
            {
                TreeViewItem item = treeView0.SelectedItem as TreeViewItem;
                if (item != null)
                {
                    TreeViewItemArg arg = item.Tag as TreeViewItemArg;
                    typeId = arg.typeId;
                }
            }
            else
            {
                TreeViewItem item = treeView1.SelectedItem as TreeViewItem;
                if (item != null)
                {
                    TreeViewItemArg arg = item.Tag as TreeViewItemArg;
                    typeId = arg.typeId;
                }
            }

            nameTextBox.Visibility = (typeId != ItemType.None ? Visibility.Visible : Visibility.Hidden);
            commentTextBox.Visibility = (typeId != ItemType.None ? Visibility.Visible : Visibility.Hidden);
            msgAttrPanel.Visibility = (typeId == ItemType.Message ? Visibility.Visible : Visibility.Hidden);
            varAttrPanel.Visibility = (typeId == ItemType.Variable ? Visibility.Visible : Visibility.Hidden);

            addVarBtn.IsEnabled = (typeId != ItemType.None && typeId != ItemType.NameSpace);
            addMsgBtn.IsEnabled = (typeId != ItemType.None && tabIdx == 0);
            addClassBtn.IsEnabled = (typeId != ItemType.None && tabIdx == 1);
            upBtn.IsEnabled = (typeId != ItemType.None);
            downBtn.IsEnabled = (typeId != ItemType.None);
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine("tabControl_SelectionChanged");

            refreshShowHide();

            int tabIdx = tabControl.SelectedIndex;
            switch (tabIdx)
            {
                case 0:
                    {
                        
                    }
                    break;
                case 1:
                    {
                        
                    }
                    break;
                default:
                    break;
            }
            
        }

        private void treeView0_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            refreshShowHide();

            TreeView tree = sender as TreeView;
            TreeViewItem item = tree.SelectedItem as TreeViewItem;
            TreeViewItemArg arg = item.Tag as TreeViewItemArg;

            int tabIdx = tabControl.SelectedIndex;
            switch (tabIdx)
            {
                case 0:
                    {

                    }
                    break;
                case 1:
                    {

                    }
                    break;
                default:
                    break;
            }
        }

        private void addNameSpaceBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void addMsgBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void addClassBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void addVarBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void upBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void downBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void genClientCodeBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("genClientCodeBtn_MouseLeftButtonDown");

            ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();
            engine.CreateScriptSourceFromFile("genclientcode.py").Execute();

            Console.ReadLine();
        }

        private void genServerCodeBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void addNameSpaceBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void genClientCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("genClientCodeBtn_Click");

            ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();
            engine.CreateScriptSourceFromFile("genclientcode.py").Execute();

            Console.ReadLine();
        }
    }
}
