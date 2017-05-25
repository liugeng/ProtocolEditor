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

namespace ProtocolEditor
{
	/// <summary>
	/// UITreeViewItem.xaml 的交互逻辑
	/// </summary>
	public partial class UITreeViewItem : TreeViewItem
	{
		ItemType itemType;
		const int CODE_TYPE_SCRIPT = 0;
		const int CODE_TYPE_CPP = 1;

		public UITreeViewItem(ItemType t)
		{
			InitializeComponent();
			itemType = t;
		}

		public void contextMenu_Loaded(object sender, RoutedEventArgs e)
		{
			Group g = (Tag as TreeViewItemArg).data as Group;
			if (g.codeType == CODE_TYPE_CPP)
			{
				contextMenu.Visibility = Visibility.Hidden;
				return;
			}

			switch (itemType)
			{
				case ItemType.Group:
					{
						genGroup.Visibility = Visibility.Visible;
						genDebug.Visibility = Visibility.Visible;
						genMsg.Visibility = Visibility.Collapsed;
						genMsgWithDump.Visibility = Visibility.Collapsed;
						IsSelected = true;
						break;
					}
				case ItemType.Message:
					{
						genGroup.Visibility = Visibility.Collapsed;
						genDebug.Visibility = Visibility.Collapsed;
						genMsg.Visibility = Visibility.Visible;
						genMsgWithDump.Visibility = Visibility.Visible;
						IsSelected = true;
						break;
					}
				default:
					contextMenu.Visibility = Visibility.Hidden;
					break;
			}
		}

		public void treeViewItemRightMouseDown(object sender, MouseButtonEventArgs e)
		{
			IsSelected = true;
			e.Handled = true;
		}

		private void genGroup_Click(object sender, RoutedEventArgs e)
		{
			Group g = (Tag as TreeViewItemArg).data as Group;
			string args =
				"{"+
				"	\"gentype\": \"group\","+
				"	\"gname\":\"" + g.name + "\"" +
				"}";

			if (g.codeType == CODE_TYPE_SCRIPT)
			{
				MainWindow.instance.genCode("ProtocolEditor.Client.Script.py", "生成组成功: " + g.name, args);
			}
			else
			{
				MainWindow.instance.genCode("ProtocolEditor.Client.Cpp.py", "生成组成功: " + g.name, args);
			}
		}

		private void genDebug_Click(object sender, RoutedEventArgs e)
		{
			Group g = (Tag as TreeViewItemArg).data as Group;
			string args =
				"{" +
				"	\"gentype\": \"debug\"," +
				"	\"gname\":\"" + g.name + "\"" +
				"}";
			if (g.codeType == CODE_TYPE_SCRIPT)
			{
				MainWindow.instance.genCode("ProtocolEditor.Client.Script.py", "生成测试代码成功: Net_" + g.name + "_Debug.bolos", args);
			}
		}

		private void genMsg_Click(object sender, RoutedEventArgs e)
		{
			Msg m = (Tag as TreeViewItemArg).data as Msg;
			Group g = m.parent as Group;
			string args =
				"{" +
				"	\"gentype\": \"msg\"," +
				"	\"mname\":\"" + m.name + "\"," +
				"	\"mtype\":\"" + m.type + "\"," +
				"	\"gname\":\"" + g.name + "\"" +
				"}";
			if (g.codeType == CODE_TYPE_SCRIPT)
			{
				MainWindow.instance.genCode("ProtocolEditor.Client.Script.py", "生成单个协议成功: " + m.name, args);
			}
		}

		private void genMsgWithDump_Click(object sender, RoutedEventArgs e)
		{
			Msg m = (Tag as TreeViewItemArg).data as Msg;
			Group g = m.parent as Group;
			string args =
				"{" +
				"	\"gentype\": \"msg_dump\"," +
				"	\"mname\":\"" + m.name + "\"," +
				"	\"mtype\":\"" + m.type + "\"," +
				"	\"gname\":\"" + g.name + "\"" +
				"}";
			if (g.codeType == CODE_TYPE_SCRIPT)
			{
				MainWindow.instance.genCode("ProtocolEditor.Client.Script.py", "生成单个协议成功: " + m.name, args);
			}
		}
	}
}
