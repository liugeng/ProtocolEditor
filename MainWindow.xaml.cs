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
using System.IO;
using System.Globalization;

namespace ProtocolEditor
{
    public enum ItemType
    {
        None = -1,
        Group,
        Message,
        Variable,
        Class
    }

    public class TreeViewItemArg
    {
        public ItemType type;
        public object data;
    }


    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ConfigParser parser;
        Config cfg;
        List<object> expandDataNodes = new List<object>();
        object selectedDataNode = null;
        bool refreshing = false;

        const int TabTypeNone = -1;
        const int TabTypeDefine = 0;
        const int TabTypeStruct = 1;

        public MainWindow()
        {
            refreshing = true;
            InitializeComponent();
            refreshing = false;

            saveBtn.IsEnabled = false;
            saveBtn.Foreground = new SolidColorBrush(Colors.Gray);

            refreshShowHide();

            String configPath = Properties.Settings.Default.configPath;
            if (configPath == "" || !Directory.Exists(configPath))
            {
                openSettingDialog();
            }

            parser = new ConfigParser();
            cfg = parser.loadConfig();
            refreshTree();
        }

        private void openSettingDialog(String selectedPath = "")
        {
            if (selectedPath == "")
            {
                selectedPath = Directory.GetCurrentDirectory();
                Console.WriteLine(selectedPath);
            }

            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "=== 选择配置文件的存放路径 ===";
            folderBrowserDialog.SelectedPath = selectedPath;
            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.configPath = folderBrowserDialog.SelectedPath;

                //C:\Users\Administrator\AppData\Local\Microsoft\ProtocolEditor.vshost.exe_Url_5eita4co22abg5eveezamcqp024qpu43\1.0.0.0\user.config
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.configPath = selectedPath;
                Properties.Settings.Default.Save();
            }
        }

        private void changed()
        {
            saveBtn.IsEnabled = true;
        }

        // expand: 
        // 0.remain current state
        // 1.expand all
        // 2.unexpand all
        private void refreshTree(byte expand = 0, int searchId = -1, String searchName = "")
        {
            refreshing = true;

            if (searchId != -1 || searchName != "")
            {
                expand = 1;
            }

            if (tabControl.SelectedIndex == TabTypeDefine || tabControl.SelectedIndex == TabTypeNone)
            {
                // save current states
                if (expand == 0)
                {
                    expandDataNodes.Clear();

                    foreach (TreeViewItem i in treeView0.Items) //groups
                    {
                        if (i.IsExpanded)
                        {
                            expandDataNodes.Add((i.Tag as TreeViewItemArg).data);
                        }

                        foreach (TreeViewItem j in i.Items) //messages or classes
                        {
                            if (j.IsExpanded)
                            {
                                expandDataNodes.Add((j.Tag as TreeViewItemArg).data);
                            }

                            foreach (TreeViewItem k in j.Items) //vars
                            {
                                if (k.IsExpanded)
                                {
                                    expandDataNodes.Add((k.Tag as TreeViewItemArg).data);
                                }
                            }
                        }
                    }
                }

                TreeViewItem selectedItem = treeView0.SelectedItem as TreeViewItem;
                if (selectedItem != null)
                {
                    selectedDataNode = (selectedItem.Tag as TreeViewItemArg).data;
                }

                treeView0.Items.Clear();


                Brush gColor = new SolidColorBrush(Colors.Blue);
                Brush mColor = new SolidColorBrush(Colors.CornflowerBlue);
                Brush vColor = new SolidColorBrush(Colors.Violet);

                foreach (Group g in cfg.groups)
                {
                    TreeViewItem gItem = new TreeViewItem()
                    {
                        Header = g.header,
                        IsExpanded = ((expand==0 && expandDataNodes.Contains(g)) || expand == 1 ),
                        IsSelected = selectedDataNode == g,
                        FontSize = 14,
                        Foreground = gColor,
                        //Padding = new Thickness(0, 10, 0, 0),
                        Tag = new TreeViewItemArg()
                        {
                            type = ItemType.Group,
                            data = g
                        }
                    };

                    treeView0.Items.Add(gItem);

                    foreach (Msg m in g.msgs)
                    {
                        m.parent = g;

                        TreeViewItem mItem = new TreeViewItem()
                        {
                            Header = m.header,
                            IsExpanded = ((expand == 0 && expandDataNodes.Contains(m)) || expand == 1),
                            IsSelected = selectedDataNode == m,
                            FontSize = 14,
                            Foreground = mColor,
                            //Padding = new Thickness(0, 10, 0, 0),
                            Tag = new TreeViewItemArg()
                            {
                                type = ItemType.Message,
                                data = m
                            }
                        };

                        if (m.idValue == searchId || m.name.ToLower().Equals(searchName.ToLower()))
                        {
                            selectedDataNode = m;
                            mItem.IsSelected = true;
                        }
                        gItem.Items.Add(mItem);

                        foreach (Var v in m.vars)
                        {
                            v.parent = m;

                            TreeViewItem vItem = new TreeViewItem()
                            {
                                Header = v.header,
                                IsExpanded = ((expand == 0 && expandDataNodes.Contains(v)) || expand == 1),
                                IsSelected = selectedDataNode == v,
                                FontSize = 14,
                                Foreground = vColor,
                                Tag = new TreeViewItemArg()
                                {
                                    type = ItemType.Variable,
                                    data = v
                                }
                            };

                            if (v.name.ToLower().Equals(searchName.ToLower()))
                            {
                                selectedDataNode = v;
                                vItem.IsSelected = true;
                            }
                            mItem.Items.Add(vItem);
                        }
                    }
                }
            }
            else
            {

            }

            refreshing = false;
        }

        private void refreshShowHide()
        {
            ItemType typeId = ItemType.None;
            int tabIdx = tabControl.SelectedIndex;
            if (tabIdx == TabTypeDefine)
            {
                TreeViewItem item = treeView0.SelectedItem as TreeViewItem;
                if (item != null)
                {
                    TreeViewItemArg arg = item.Tag as TreeViewItemArg;
                    typeId = arg.type;
                }
            }
            else
            {
                TreeViewItem item = treeView1.SelectedItem as TreeViewItem;
                if (item != null)
                {
                    TreeViewItemArg arg = item.Tag as TreeViewItemArg;
                    typeId = arg.type;
                }
            }

            nameTextBox.Visibility = (typeId != ItemType.None ? Visibility.Visible : Visibility.Hidden);
            commentTextBox.Visibility = (typeId != ItemType.None ? Visibility.Visible : Visibility.Hidden);
            msgAttrPanel.Visibility = (typeId == ItemType.Message ? Visibility.Visible : Visibility.Hidden);
            varAttrPanel.Visibility = (typeId == ItemType.Variable ? Visibility.Visible : Visibility.Hidden);

            addVarBtn.IsEnabled = (typeId != ItemType.None && typeId != ItemType.Group);
            addMsgBtn.IsEnabled = (typeId != ItemType.None && tabIdx == TabTypeDefine);
            addClassBtn.IsEnabled = (tabIdx == TabTypeStruct);
            addNameSpaceBtn.IsEnabled = (tabIdx == TabTypeDefine);
            upBtn.IsEnabled = (typeId != ItemType.None);
            downBtn.IsEnabled = (typeId != ItemType.None);
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine("tabControl_SelectionChanged");

            refreshShowHide();
        }

        private void treeView0_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            refreshing = true;

            refreshShowHide();

            TreeViewItem newItem = e.NewValue as TreeViewItem;
            if (newItem == null)
            {
                return;
            }
            
            TreeViewItemArg arg = newItem.Tag as TreeViewItemArg;

            if (tabControl.SelectedIndex == TabTypeDefine)
            {
                switch (arg.type)
                {
                    case ItemType.Group:
                        {
                            Group g = arg.data as Group;
                            nameTextBox.Text = g.name;
                            commentTextBox.Text = g.comment;
                            break;
                        }
                    case ItemType.Message:
                        {
                            Msg m = arg.data as Msg;
                            nameTextBox.Text = m.name;
                            commentTextBox.Text = m.comment;
                            idTextBox.Text = m.id;
                            ptypeComboBox.SelectedIndex = (m.type == "CS" ? 0 : 1);
                            break;
                        }
                    case ItemType.Class:
                        {
                            break;
                        }
                    case ItemType.Variable:
                        {
                            Var v = arg.data as Var;
                            nameTextBox.Text = v.name;
                            commentTextBox.Text = v.comment;
                            isArrayCheckBox.IsChecked = v.isArray;
                            vtypeComboBox.SelectedIndex = -1;
                            foreach (ComboBoxItem item in vtypeComboBox.Items)
                            {
                                if ((item.Content as String) == v.type)
                                {
                                    item.IsSelected = true;
                                    break;
                                }
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            else
            {

            }

            refreshing = false;
        }

        private void addNameSpaceBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addMsgBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addClassBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addVarBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void upBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void downBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void genClientCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (saveBtn.IsEnabled)
            {
                parser.saveToFile(cfg);
            }

            ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();
            engine.CreateScriptSourceFromFile("genclientcode.py").Execute();

            Console.ReadLine();
        }

        private void genServerCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            //ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();
            //engine.CreateScriptSourceFromFile("genclientcode.py").Execute();

            //Console.ReadLine();
        }

        private void settingBtn_Click(object sender, RoutedEventArgs e)
        {
            openSettingDialog(Properties.Settings.Default.configPath);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void isArrayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = treeView0.SelectedItem as TreeViewItem;
            Var v = ((selectedItem.Tag as TreeViewItemArg).data as Var);
            v.isArray = true;
            selectedItem.Header = v.header;
        }

        private void isArrayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = treeView0.SelectedItem as TreeViewItem;
            Var v = ((selectedItem.Tag as TreeViewItemArg).data as Var);
            v.isArray = false;
            selectedItem.Header = v.header;
        }

        private void expandAllBtn_Click(object sender, RoutedEventArgs e)
        {
            refreshTree(1);
        }

        private void unexpandAllBtn_Click(object sender, RoutedEventArgs e)
        {
            refreshTree(2);
        }

        private void treeView0_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void treeView0_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void searchIdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                String idStr = searchIdTextBox.Text;
                if (idStr != "")
                {
                    if (tabControl.SelectedIndex == TabTypeDefine)
                    {
                        int searchId = -1;
                        if (idStr.StartsWith("0x"))
                        {
                            int.TryParse(idStr.Substring(2), NumberStyles.HexNumber, null, out searchId);
                        }
                        else
                        {
                            int.TryParse(idStr, out searchId);
                        }

                        if (searchId != -1)
                        {
                            refreshTree(0, searchId);
                        }
                    }
                }
            }
        }

        private void searchNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                String searchName = searchNameTextBox.Text;
                if (searchName != "")
                {
                    refreshTree(0, -1, searchName);
                }
            }
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            parser.saveToFile(cfg);
            saveBtn.IsEnabled = false;
        }

        private void nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeView tree = (tabControl.SelectedIndex == TabTypeDefine ? treeView0 : treeView1);
            TreeViewItem selectedItem = tree.SelectedItem as TreeViewItem;
            TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;

            String name = nameTextBox.Text;

            switch (arg.type)
            {
                case ItemType.Group:
                    {
                        Group g = arg.data as Group;
                        g.name = name;
                        selectedItem.Header = g.header;
                        break;
                    }
                case ItemType.Message:
                    {
                        Msg m = arg.data as Msg;
                        m.name = name;
                        selectedItem.Header = m.header;
                        break;
                    }
                case ItemType.Variable:
                    {
                        Var v = arg.data as Var;
                        v.name = name;
                        selectedItem.Header = v.header;
                        break;
                    }
                case ItemType.Class:
                    {
                        Class c = arg.data as Class;
                        c.name = name;
                        selectedItem.Header = c.header;
                        break;
                    }
                default:
                    break;
            }

            changed();
        }

        private void commentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (refreshing)
            {
                return;
            }

            TreeView tree = (tabControl.SelectedIndex == TabTypeDefine ? treeView0 : treeView1);
            TreeViewItem selectedItem = tree.SelectedItem as TreeViewItem;
            TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;

            String comment = nameTextBox.Text;

            switch (arg.type)
            {
                case ItemType.Group:
                    {
                        Group g = arg.data as Group;
                        g.comment = comment;
                        selectedItem.Header = g.header;
                        break;
                    }
                case ItemType.Message:
                    {
                        Msg m = arg.data as Msg;
                        m.comment = comment;
                        selectedItem.Header = m.header;
                        break;
                    }
                case ItemType.Variable:
                    {
                        Var v = arg.data as Var;
                        v.comment = comment;
                        selectedItem.Header = v.header;
                        break;
                    }
                case ItemType.Class:
                    {
                        Class c = arg.data as Class;
                        c.comment = comment;
                        selectedItem.Header = c.header;
                        break;
                    }
                default:
                    break;
            }

            changed();
        }

        private void idTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = treeView0.SelectedItem as TreeViewItem;
            Msg m = ((selectedItem.Tag as TreeViewItemArg).data as Msg);
            m.id = idTextBox.Text;
            selectedItem.Header = m.header;

            changed();
        }
    }
}
