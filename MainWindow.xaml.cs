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
        bool refreshing = false;
        bool addingItem = false;
        int maxMsgId;

        const int TabTypeNone = -1;
        const int TabTypeDefine = 0;
        const int TabTypeClass = 1;

        Brush gColor = new SolidColorBrush(Colors.LightSeaGreen);
        Brush mColor = new SolidColorBrush(Colors.DodgerBlue);
        Brush msColor = new SolidColorBrush(Colors.DeepSkyBlue);
        Brush vColor = new SolidColorBrush(Colors.DarkSlateGray); //Crimson);

        string[] baseVarTypes = { "bool", "byte", "short", "int", "long", "float", "String"};

        public MainWindow()
        {
            refreshing = true;
            InitializeComponent();
            refreshing = false;

            saveBtn.IsEnabled = false;

            refreshShowHide();

            String configPath = Properties.Settings.Default.configPath;
            if (configPath == "" || !Directory.Exists(configPath))
            {
                openSettingDialog();
            }
            else
            {
                parser = new ConfigParser();
                cfg = parser.loadConfig();
                createTree();
            }
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
                if (Properties.Settings.Default.configPath != folderBrowserDialog.SelectedPath)
                {
                    Properties.Settings.Default.configPath = folderBrowserDialog.SelectedPath;

                    //C:\Users\Administrator\AppData\Local\Microsoft\ProtocolEditor.vshost.exe_Url_5eita4co22abg5eveezamcqp024qpu43\1.0.0.0\user.config
                    Properties.Settings.Default.Save();

                    parser = new ConfigParser();
                    cfg = parser.loadConfig();
                    createTree();
                }
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

        private void createTree()
        {
            refreshing = true;

            treeView0.Items.Clear();

            foreach (Group g in cfg.groups)
            {
                TreeViewItem gItem = new TreeViewItem()
                {
                    Header = g.header,
                    FontSize = 14,
                    Foreground = gColor,
                    //Padding = new Thickness(0, 10, 0, 0),
                    Tag = new TreeViewItemArg()
                    {
                        type = ItemType.Group,
                        data = g
                    }
                };

                g.item = gItem;
                treeView0.Items.Add(gItem);

                foreach (Msg m in g.msgs)
                {
                    maxMsgId = Math.Max(maxMsgId, m.idValue);

                    TreeViewItem mItem = new TreeViewItem()
                    {
                        Header = m.header,
                        FontSize = 14,
                        Foreground = (m.type == "CS" ? mColor : msColor),
                        //Padding = new Thickness(0, 10, 0, 0),
                        Tag = new TreeViewItemArg()
                        {
                            type = ItemType.Message,
                            data = m
                        }
                    };

                    m.parent = g;
                    m.item = mItem;
                    gItem.Items.Add(mItem);

                    foreach (Var v in m.vars)
                    {
                        TreeViewItem vItem = new TreeViewItem()
                        {
                            Header = v.header,
                            FontSize = 14,
                            Foreground = vColor,
                            Tag = new TreeViewItemArg()
                            {
                                type = ItemType.Variable,
                                data = v
                            }
                        };

                        v.parent = m;
                        v.item = vItem;
                        mItem.Items.Add(vItem);
                    }
                }
            }


            
            vtypeComboBox.Items.Clear();
            foreach (string t in baseVarTypes)
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = t
                };
                vtypeComboBox.Items.Add(item);
            }


            treeView1.Items.Clear();
            foreach (Class c in cfg.classes)
            {
                ComboBoxItem boxItem = new ComboBoxItem()
                {
                    Content = c.name
                };
                vtypeComboBox.Items.Add(boxItem);

                TreeViewItem cItem = new TreeViewItem()
                {
                    Header = c.header,
                    FontSize = 14,
                    Foreground = mColor,
                    Tag = new TreeViewItemArg()
                    {
                        type = ItemType.Class,
                        data = c
                    }
                };

                c.item = cItem;
                treeView1.Items.Add(cItem);

                foreach (Var v in c.vars)
                {
                    TreeViewItem vItem = new TreeViewItem()
                    {
                        Header = v.header,
                        FontSize = 14,
                        Foreground = vColor,
                        Tag = new TreeViewItemArg()
                        {
                            type = ItemType.Variable,
                            data = v
                        }
                    };

                    v.parent = c;
                    v.item = vItem;
                    cItem.Items.Add(vItem);
                }
            }

            refreshing = false;
        }

        private TreeView getCurTreeView()
        {
            return (tabControl.SelectedIndex == TabTypeDefine ? treeView0 : treeView1);
        }

        private TreeViewItem getSelectedItem()
        {
            return (tabControl.SelectedIndex == TabTypeDefine ? treeView0.SelectedItem : treeView1.SelectedItem) as TreeViewItem;
        }

        private TreeViewItemArg getSelectedArg()
        {
            TreeViewItem selectedItem = getSelectedItem();
            if (selectedItem != null)
            {
                return selectedItem.Tag as TreeViewItemArg;
            }
            return null;
        }

        private object getSelectedData()
        {
            TreeViewItemArg arg = getSelectedArg();
            if (arg != null)
            {
                return arg.data;
            }
            return null;
        }

        private void refreshExpand(bool IsExpanded)
        {
            TreeView tree = getCurTreeView();
            //group
            foreach (TreeViewItem g in tree.Items)
            {
                g.IsExpanded = IsExpanded;
                //msg
                foreach (TreeViewItem m in g.Items)
                {
                    m.IsExpanded = IsExpanded;
                }
            }
        }

        private void doSearch(int searchId, String searchName)
        {
            if (tabControl.SelectedIndex == TabTypeDefine)
            {
                foreach (Group g in cfg.groups)
                {
                   if (g.name.ToLower() == searchName.ToLower())
                    {
                        g.item.IsExpanded = true;
                        g.item.IsSelected = true;
                        ItemsControlExtensions.ScrollToCenterOfView(treeView0, g.item);
                        return;
                    }

                    foreach (Msg m in g.msgs)
                    {
                       if (m.idValue == searchId || m.name.ToLower() == searchName.ToLower())
                        {
                            g.item.IsExpanded = true;
                            m.item.IsExpanded = true;
                            m.item.IsSelected = true;
                            ItemsControlExtensions.ScrollToCenterOfView(treeView0, m.item);
                            return;
                        }

                        foreach (Var v in m.vars)
                        {
                            if (v.name.ToLower() == searchName.ToLower())
                            {
                                g.item.IsExpanded = true;
                                m.item.IsExpanded = true;
                                v.item.IsSelected = true;
                                ItemsControlExtensions.ScrollToCenterOfView(treeView0, v.item);
                                return;
                            }
                        }
                    }
                }
            }
            else if (tabControl.SelectedIndex == TabTypeClass)
            {
                foreach (Class c in cfg.classes)
                {
                    if (c.name.ToLower() == searchName.ToLower())
                    {
                        c.item.IsExpanded = true;
                        c.item.IsSelected = true;
                        ItemsControlExtensions.ScrollToCenterOfView(treeView1, c.item);
                        return;
                    }

                    foreach (Var v in c.vars)
                    {
                        if (v.name.ToLower() == searchName.ToLower())
                        {
                            c.item.IsExpanded = true;
                            v.item.IsExpanded = true;
                            v.item.IsSelected = true;
                            ItemsControlExtensions.ScrollToCenterOfView(treeView1, v.item);
                            return;
                        }
                    }
                }
            }
        }

        private void refreshShowHide()
        {
            int tabIdx = tabControl.SelectedIndex;
            ItemType typeId = ItemType.None;
            TreeViewItem item = getSelectedItem();
            if (item != null)
            {
                typeId = (item.Tag as TreeViewItemArg).type;
            }

            nameTextBox.Visibility = (typeId != ItemType.None ? Visibility.Visible : Visibility.Hidden);
            nameLabel.Visibility = nameTextBox.Visibility;
            commentTextBox.Visibility = (typeId != ItemType.None ? Visibility.Visible : Visibility.Hidden);
            commentLabel.Visibility = commentTextBox.Visibility;
            msgAttrPanel.Visibility = (typeId == ItemType.Message ? Visibility.Visible : Visibility.Hidden);
            varAttrPanel.Visibility = (typeId == ItemType.Variable ? Visibility.Visible : Visibility.Hidden);

            addVarBtn.IsEnabled = (typeId != ItemType.None && typeId != ItemType.Group);
            addMsgBtn.IsEnabled = (typeId != ItemType.None && tabIdx == TabTypeDefine);
            addClassBtn.IsEnabled = (tabIdx == TabTypeClass);
            addNameSpaceBtn.IsEnabled = (tabIdx == TabTypeDefine);
            upBtn.IsEnabled = (typeId != ItemType.None);
            downBtn.IsEnabled = (typeId != ItemType.None);
            delBtn.IsEnabled = (typeId != ItemType.None);
            searchIdTextBox.IsEnabled = (tabIdx == TabTypeDefine);
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Console.WriteLine("tabControl_SelectionChanged");

            refreshShowHide();
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            refreshing = true;

            refreshShowHide();

            TreeViewItem newItem = e.NewValue as TreeViewItem;
            if (newItem == null)
            {
                return;
            }
            
            TreeViewItemArg arg = newItem.Tag as TreeViewItemArg;

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
                        Class c = arg.data as Class;
                        nameTextBox.Text = c.name;
                        commentTextBox.Text = c.comment;
                        break;
                    }
                case ItemType.Variable:
                    {
                        Var v = arg.data as Var;
                        nameTextBox.Text = v.name;
                        commentTextBox.Text = v.comment;
                        isArrayCheckBox.IsChecked = v.isArray;
                        vtypeComboBox.SelectedIndex = -1;

                        string className = "";
                        if (tabControl.SelectedIndex == TabTypeClass)
                        {
                            className = (v.parent as Class).name;
                        }

                        foreach (ComboBoxItem item in vtypeComboBox.Items)
                        {
                            if ((item.Content as string) == v.type)
                            {
                                item.IsSelected = true;
                            }

                            if (tabControl.SelectedIndex == TabTypeClass)
                            {
                                item.IsEnabled = (item.Content as string) != className;
                            }
                        }
                        break;
                    }
                default:
                    break;
            }

            refreshing = false;
        }

        private bool isNameExist(dynamic list, ItemType t, string name)
        {
            foreach (object e in list)
            {
                switch (t)
                {
                    case ItemType.Group:
                        {
                            if ((e as Group).name == name)
                            {
                                return true;
                            }
                            break;
                        }
                    case ItemType.Message:
                        {
                            //if ((e as Msg).name == name)
                            //{
                            //    return true;
                            //}

                            //保持不同的group下的msg都不同名
                            foreach (Msg m in (e as Group).msgs)
                            {
                                if (m.name == name)
                                {
                                    return true;
                                }
                            }
                            break;
                        }
                    case ItemType.Class:
                        {
                            if ((e as Class).name == name)
                            {
                                return true;
                            }
                            break;
                        }
                    case ItemType.Variable:
                        {
                            if ((e as Var).name == name)
                            {
                                return true;
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            return false;
        }

        private string getAutoName(dynamic list, ItemType t)
        {
            string prefix = "";
            switch (t)
            {
                case ItemType.Group:
                    {
                        prefix = "Empty_group";
                        break;
                    }
                case ItemType.Message:
                    {
                        prefix = "Empty_msg";
                        break;
                    }
                case ItemType.Class:
                    {
                        prefix = "Empty_class";
                        break;
                    }
                case ItemType.Variable:
                    {
                        prefix = "empty_var";
                        break;
                    }
                default:
                    break;
            }

            string name = "";
            int n = 0;

            while (true)
            {
                name = prefix + (n == 0 ? "" : n.ToString());
                if (!isNameExist(list, t, name))
                {
                    return name;
                }
                n++;
                if (n >= 1000)
                {
                    break;
                }
            }

            return prefix;
        }

        private void addNameSpaceBtn_Click(object sender, RoutedEventArgs e)
        {
            addingItem = true;

            Group g = new Group()
            {
                name = getAutoName(cfg.groups, ItemType.Group),
                comment = ""
            };

            cfg.groups.Add(g);

            TreeViewItem gItem = new TreeViewItem()
            {
                Header = g.header,
                IsExpanded = true,
                IsSelected = true,
                FontSize = 14,
                Foreground = gColor,
                Tag = new TreeViewItemArg()
                {
                    type = ItemType.Group,
                    data = g
                }
            };

            g.item = gItem;
            treeView0.Items.Add(gItem);

            changed();
        }

        private void addMsgBtn_Click(object sender, RoutedEventArgs e)
        {
            addingItem = true;

            Msg m = new Msg()
            {
                name = getAutoName(cfg.groups, ItemType.Message),
                comment = "",
                id = "0x0000",
                type = "CS"
            };

            TreeViewItem mItem = new TreeViewItem()
            {
                Header = m.header,
                IsExpanded = true,
                IsSelected = true,
                FontSize = 14,
                Foreground = mColor,
                Tag = new TreeViewItemArg()
                {
                    type = ItemType.Message,
                    data = m
                }
            };

            TreeViewItem selectedItem = treeView0.SelectedItem as TreeViewItem;
            if (selectedItem == null)
            {
                return;
            }

            TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;
            Group parent = null;
            int idx = -1;
            if (arg.type == ItemType.Group)
            {
                parent = arg.data as Group;
            }
            else if (arg.type == ItemType.Message)
            {
                parent = (arg.data as Msg).parent as Group;
                idx = parent.msgs.IndexOf(arg.data as Msg) + 1;
            }
            else if (arg.type == ItemType.Variable)
            {
                parent = ((arg.data as Var).parent as Msg).parent as Group;
                idx = parent.msgs.IndexOf((arg.data as Var).parent as Msg) + 1;
            }

            if (parent == null)
            {
                return;
            }

            m.parent = parent;
            m.item = mItem;

            maxMsgId++;
            m.id = "0x" + maxMsgId.ToString("X4");
            mItem.Header = m.header;

            parent.item.IsExpanded = true;

            if (parent.msgs.Count == idx || idx == -1)
            {
                parent.msgs.Add(m);
                parent.item.Items.Add(mItem);
            }
            else
            {
                parent.msgs.Insert(idx, m);
                parent.item.Items.Insert(idx, mItem);
            }

            changed();
        }

        private void addClassBtn_Click(object sender, RoutedEventArgs e)
        {
            addingItem = true;

            Class c = new Class()
            {
                name = getAutoName(cfg.classes, ItemType.Class),
                comment = ""
            };

            TreeViewItem cItem = new TreeViewItem()
            {
                Header = c.header,
                IsExpanded = true,
                IsSelected = true,
                FontSize = 14,
                Foreground = mColor,
                Tag = new TreeViewItemArg()
                {
                    type = ItemType.Class,
                    data = c
                }
            };

            c.item = cItem;

            ComboBoxItem boxItem = new ComboBoxItem()
            {
                Content = c.name
            };


            TreeViewItemArg arg = getSelectedArg();
            if (arg == null)
            {
                cfg.classes.Add(c);
                treeView1.Items.Add(cItem);
                vtypeComboBox.Items.Add(boxItem);
            }
            else
            {
                Class selc = null;
                if (arg.type == ItemType.Class)
                {
                    selc = arg.data as Class;
                }
                else if (arg.type == ItemType.Variable)
                {
                    selc = (arg.data as Var).parent as Class;
                }

                int idx = cfg.classes.IndexOf(selc);
                cfg.classes.Insert(idx + 1, c);
                treeView1.Items.Insert(idx + 1, cItem);
                vtypeComboBox.Items.Insert(baseVarTypes.Length + idx + 1, boxItem);
            }

            changed();
        }

        private void addVarBtn_Click(object sender, RoutedEventArgs e)
        {
            addingItem = true;

            Var v = new Var()
            {
                //name = "empty_var",
                comment = "",
                type = "int",
                isArray = false,
                isClass = false
            };

            TreeViewItem vItem = new TreeViewItem()
            {
                //Header = v.header,
                IsExpanded = true,
                IsSelected = true,
                FontSize = 14,
                Foreground = vColor,
                Tag = new TreeViewItemArg()
                {
                    type = ItemType.Variable,
                    data = v
                }
            };

            if (tabControl.SelectedIndex == TabTypeDefine)
            {
                TreeViewItem selectedItem = treeView0.SelectedItem as TreeViewItem;
                if (selectedItem == null)
                {
                    return;
                }

                TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;
                Msg parent = null;
                int idx = -1;
                if (arg.type == ItemType.Message)
                {
                    parent = arg.data as Msg;
                }
                else if (arg.type == ItemType.Variable)
                {
                    parent = (arg.data as Var).parent as Msg;
                    idx = parent.vars.IndexOf(arg.data as Var) + 1;
                }

                if (parent == null)
                {
                    return;
                }

                v.name = getAutoName(parent.vars, ItemType.Variable);
                vItem.Header = v.header;

                v.parent = parent;
                v.item = vItem;

                parent.item.IsExpanded = true;
                (parent.parent as Group).item.IsExpanded = true;

                if (parent.vars.Count == idx || idx == -1)
                {
                    parent.vars.Add(v);
                    parent.item.Items.Add(vItem);
                }
                else
                {
                    parent.vars.Insert(idx, v);
                    parent.item.Items.Insert(idx, vItem);
                }
            }
            else if (tabControl.SelectedIndex == TabTypeClass)
            {
                TreeViewItem selectedItem = treeView1.SelectedItem as TreeViewItem;
                if (selectedItem == null)
                {
                    return;
                }

                TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;
                Class parent = null;
                int idx = -1;
                if (arg.type == ItemType.Class)
                {
                    parent = arg.data as Class;
                }
                else if (arg.type == ItemType.Variable)
                {
                    parent = (arg.data as Var).parent as Class;
                    idx = parent.vars.IndexOf(arg.data as Var) + 1;
                }

                if (parent == null)
                {
                    return;
                }

                v.name = getAutoName(parent.vars, ItemType.Variable);
                vItem.Header = v.header;

                v.parent = parent;
                v.item = vItem;

                parent.item.IsExpanded = true;

                if (parent.vars.Count == idx || idx == -1)
                {
                    parent.vars.Add(v);
                    parent.item.Items.Add(vItem);
                }
                else
                {
                    parent.vars.Insert(idx, v);
                    parent.item.Items.Insert(idx, vItem);
                }
            }

            changed();
        }

        private void swap<T>(List<T> list, int idx1, int idx2)
        {
            if (idx1 < 0 || idx1 >= list.Count || idx2 < 0 || idx2 >= list.Count)
            {
                return;
            }

            T tmp = list[idx1];
            list[idx1] = list[idx2];
            list[idx2] = tmp;
        }

        //itemType 0:TreeViewItem  1.ComboBoxItem
        private void swapItem(ItemCollection items, int idx1, int idx2, int itemType = 0)
        {
            if (idx1 < 0 || idx1 >= items.Count || idx2 < 0 || idx2 >= items.Count)
            {
                return;
            }

            if (itemType == 1 && (idx1 < baseVarTypes.Length || idx2 < baseVarTypes.Length))
            {
                return;
            }

            int minIdx = Math.Min(idx1, idx2);
            int maxIdx = Math.Max(idx1, idx2);

            object tmp = items[maxIdx];
            items.RemoveAt(maxIdx);
            items.Insert(minIdx, tmp);

            if (itemType == 0)
            {
                (items[idx2] as TreeViewItem).IsSelected = true;
            }
            else if (itemType == 1)
            {
                (items[idx2] as ComboBoxItem).IsSelected = true;
            }

            //object tmp = items[idx1];
            //items[idx1] = items[idx2];
            //items[idx2] = tmp;
        }

        private void changeOrder(bool isUp)
        {
            int n = (isUp ? -1 : 1);

            TreeViewItem selectedItem = getSelectedItem();
            TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;
            switch (arg.type)
            {
                case ItemType.Group:
                    {
                        Group g = arg.data as Group;
                        int idx = cfg.groups.IndexOf(g);
                        swap<Group>(cfg.groups, idx, idx + n);
                        swapItem(treeView0.Items, idx, idx + n);
                        break;
                    }
                case ItemType.Message:
                    {
                        Group g = (arg.data as Msg).parent as Group;
                        int idx = g.msgs.IndexOf(arg.data as Msg);
                        swap<Msg>(g.msgs, idx, idx + n);
                        swapItem(g.item.Items, idx, idx + n);
                        break;
                    }
                case ItemType.Class:
                    {
                        Class c = arg.data as Class;
                        int idx = cfg.classes.IndexOf(c);
                        swap<Class>(cfg.classes, idx, idx + n);
                        swapItem(treeView1.Items, idx, idx + n);
                        swapItem(vtypeComboBox.Items, baseVarTypes.Length + idx, baseVarTypes.Length + idx + n, 1);
                        break;
                    }
                case ItemType.Variable:
                    {
                        if (tabControl.SelectedIndex == TabTypeDefine)
                        {
                            Msg m = (arg.data as Var).parent as Msg;
                            int idx = m.vars.IndexOf(arg.data as Var);
                            swap<Var>(m.vars, idx, idx + n);
                            swapItem(m.item.Items, idx, idx + n);
                        }
                        else
                        {
                            Class c = (arg.data as Var).parent as Class;
                            int idx = c.vars.IndexOf(arg.data as Var);
                            swap<Var>(c.vars, idx, idx + n);
                            swapItem(c.item.Items, idx, idx + n);
                        }
                        break;
                    }
                default:
                    break;
            }

            changed();
        }

        private void upBtn_Click(object sender, RoutedEventArgs e)
        {
            changeOrder(true);
        }

        private void downBtn_Click(object sender, RoutedEventArgs e)
        {
            changeOrder(false);
        }

        private void genCode(string pythonFile, string tips)
        {
            if (saveBtn.IsEnabled)
            {
                parser.saveToFile(cfg);
                saveBtn.IsEnabled = false;
            }

            ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();
            ScriptSource source = engine.CreateScriptSourceFromFile(pythonFile);
            if (source != null)
            {
                try
                {
                    ScriptScope scope = engine.CreateScope();
                    scope.SetVariable("jsonFile", Properties.Settings.Default.configPath + "\\ProtocolEditor.Msg.json");
                    source.Execute(scope);
                    Console.ReadLine();
                    MessageBox.Show(tips, "生成成功");
                }
                catch (Exception ex)
                {
                    string s = "Python: ";
                    MessageBox.Show(s + ex.Message, "生成失败");
                }
            }
        }

        private void genClientCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            genCode("ProtocolEditor.Client.py", "客户端代码生成成功");
        }

        private void genServerCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            genCode("ProtocolEditor.Server.py", "服务器代码生成成功");
        }

        private void settingBtn_Click(object sender, RoutedEventArgs e)
        {
            openSettingDialog(Properties.Settings.Default.configPath);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (saveBtn.IsEnabled)
            {
                if (MessageBox.Show(this, "是否保存修改？", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    parser.saveToFile(cfg);
                }
            }
        }

        private void isArrayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = getSelectedItem();
            Var v = ((selectedItem.Tag as TreeViewItemArg).data as Var);
            v.isArray = true;
            selectedItem.Header = v.header;

            changed();
        }

        private void isArrayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = getSelectedItem();
            Var v = ((selectedItem.Tag as TreeViewItemArg).data as Var);
            v.isArray = false;
            selectedItem.Header = v.header;

            changed();
        }

        private void expandAllBtn_Click(object sender, RoutedEventArgs e)
        {
            refreshExpand(true);
        }

        private void unexpandAllBtn_Click(object sender, RoutedEventArgs e)
        {
            refreshExpand(false);
        }

        private void searchIdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                String idStr = searchIdTextBox.Text;
                if (idStr != "")
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
                        doSearch(searchId, "");
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
                    doSearch(-1, searchName);
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
            if (addingItem)
            {
                nameTextBox.Focus();
                nameTextBox.SelectAll();
                addingItem = false;
                return;
            }

            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = getSelectedItem();
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
                        
                        int idx = cfg.classes.IndexOf(c);
                        ComboBoxItem boxItem = vtypeComboBox.Items[baseVarTypes.Length + idx] as ComboBoxItem;
                        boxItem.Content = name;

                        foreach (Group g in cfg.groups)
                        {
                            foreach (Msg m in g.msgs)
                            {
                                foreach (Var v in m.vars)
                                {
                                    if (v.type == c.name)
                                    {
                                        v.type = name;
                                        v.item.Header = v.header;
                                    }
                                }
                            }
                        }

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

            TreeViewItem selectedItem = getSelectedItem();
            TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;

            String comment = commentTextBox.Text;

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

        private void ptypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = treeView0.SelectedItem as TreeViewItem;
            TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;
            Msg m = arg.data as Msg;
            m.type = (ptypeComboBox.SelectedIndex == 0 ? "CS" : "SC");
            selectedItem.Header = m.header;
            selectedItem.Foreground = (m.type == "CS" ? mColor : msColor);

            changed();
        }

        private void vtypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (refreshing)
            {
                return;
            }

            TreeViewItem selectedItem = getSelectedItem();
            TreeViewItemArg arg = selectedItem.Tag as TreeViewItemArg;
            if (arg.type != ItemType.Variable)
            {
                return;
            }
            Var v = arg.data as Var;

            ComboBoxItem boxItem = vtypeComboBox.SelectedItem as ComboBoxItem;
            v.type = boxItem.Content as string;
            selectedItem.Header = v.header;

            v.isClass = (vtypeComboBox.SelectedIndex >= baseVarTypes.Length);

            changed();
        }

        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.Grid)
            {
                return;
            }

            TreeViewItemArg arg = getSelectedArg();
            if (arg == null || arg.type != ItemType.Variable || !(arg.data as Var).isClass)
            {
                return;
            }

            string className = (arg.data as Var).type;
            foreach (Class c in cfg.classes)
            {
                if (c.name == className)
                {
                    c.item.IsExpanded = true;
                    c.item.IsSelected = true;
                    ItemsControlExtensions.ScrollToCenterOfView(treeView1, c.item);
                    break;
                }
            }

            tabControl.SelectedIndex = TabTypeClass;
        }

        private void removeTreeViewItem(ItemsControl parent, TreeViewItem item)
        {
            int idx = parent.Items.IndexOf(item);
            parent.Items.Remove(item);

            if (idx == 0)
            {
                if (parent is TreeViewItem)
                {
                    (parent as TreeViewItem).IsSelected = true;
                }
            }
            else if (idx < parent.Items.Count)
            {
                (parent.Items[idx] as TreeViewItem).IsSelected = true;
            }
            else
            {
                idx--;
                (parent.Items[idx] as TreeViewItem).IsSelected = true;
            }

            changed();
        }

        private void delBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "确定要删除吗？", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                TreeViewItemArg arg = getSelectedArg();
                switch (arg.type)
                {
                    case ItemType.Group:
                        {
                            Group g = arg.data as Group;
                            cfg.groups.Remove(g);
                            removeTreeViewItem(treeView0, g.item);
                            break;
                        }
                    case ItemType.Message:
                        {
                            Msg m = arg.data as Msg;
                            Group g = m.parent as Group;
                            g.msgs.Remove(m);
                            removeTreeViewItem(g.item, m.item);
                            break;
                        }
                    case ItemType.Class:
                        {
                            Class c = arg.data as Class;
                            cfg.classes.Remove(c);
                            removeTreeViewItem(treeView1, c.item);
                            break;
                        }
                    case ItemType.Variable:
                        {
                            Var v = arg.data as Var;
                            if (tabControl.SelectedIndex == TabTypeDefine)
                            {
                                Msg m = v.parent as Msg;
                                m.vars.Remove(v);
                                removeTreeViewItem(m.item, v.item);
                            }
                            else
                            {
                                Class c = v.parent as Class;
                                c.vars.Remove(v);
                                removeTreeViewItem(c.item, v.item);
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
        }

        private bool check(Key key)
        {
            return Keyboard.IsKeyDown(key);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //save
            if (check(Key.LeftCtrl) && check(Key.S))
            {
                if (saveBtn.IsEnabled)
                {
                    parser.saveToFile(cfg);
                    saveBtn.IsEnabled = false;
                }
            }
            else if (check(Key.LeftCtrl) || check(Key.RightCtrl))
            {
                if (check(Key.D1))
                {
                    if (addNameSpaceBtn.IsEnabled)
                    {
                        addNameSpaceBtn_Click(null, null);
                    }
                }
                else if (check(Key.D2))
                {
                    if (addMsgBtn.IsEnabled)
                    {
	                    addMsgBtn_Click(null, null);
                    }
                }
                else if (check(Key.D3))
                {
                    if (addClassBtn.IsEnabled)
                    {
	                    addClassBtn_Click(null, null);
                    }
                }
                else if (check(Key.D4))
                {
                    if (addVarBtn.IsEnabled)
                    {
	                    addVarBtn_Click(null, null);
                    }
                }
            }
            else if (check(Key.LeftAlt) || check(Key.RightAlt))
            {
                if (check(Key.Up))
                {
                    upBtn_Click(null, null);
                }
                else if (check(Key.Down))
                {
                    downBtn_Click(null, null);
                }
            }
            else if (check(Key.Delete))
            {
                delBtn_Click(null, null);
            }
            else if (check(Key.F5))
            {
                parser = new ConfigParser();
                cfg = parser.loadConfig();
                createTree();
            }
        }
    }
}
