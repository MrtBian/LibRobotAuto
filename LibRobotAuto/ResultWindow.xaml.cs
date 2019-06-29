using LibRobotAuto.Common;
using LibRobotAuto.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibRobotAuto
{
    /// <summary>
    /// ResultWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ResultWindow : Window
    {
        private List<Button> btnList;

        private List<MyListViewItem> listViewItems;

        private int selectShelfID;

        private Dictionary<int, Shelf> shelves;

        public ResultWindow()
        {
            InitializeComponent();

            btnList = new List<Button>();
            btnList.Add(layer1);
            btnList.Add(layer2);
            btnList.Add(layer3);
            btnList.Add(layer4);
            btnList.Add(layer5);
            btnList.Add(close);

            listViewItems = new List<MyListViewItem>();
            listView.DataContext = listViewItems;

            //for (int i = 0; i < 10; i++)
            //{
            //    MyListViewItem item = new MyListViewItem(i, "Title" + i, "state", 1, 2, 1, 2);
            //    listViewItems.Add(item);
            //}

            //listView.Items.Refresh();

            //shelves = Robot.getInstance().GetInventoryResult();
            //UpdateComboBox(shelves.Keys.ToList());
        }

        //private void UpdateComboBox(List<int> shelfIDs)
        //{
        //    if (shelfIDs == null || shelfIDs.Count == 0)
        //    {
        //        MessageBox.Show("未扫描到任何书本...");
        //        return;
        //    }

        //    shelfIDs.Sort();

        //    foreach (int shelfID in shelfIDs)
        //        comboBox.Items.Add(shelfID);

        //    comboBox.SelectedIndex = 0;
        //}

        private void UpdateView(int shelfID, int layer)
        {
            listViewItems.Clear();

            if (shelves[shelfID].Books.ContainsKey(layer))
            {
                int wrongCount = 0;
                int lostCount = 0;

                List<Book> books = shelves[shelfID].Books[layer];
                for (int i = 0; i < books.Count; i++)
                {
                    string state = "正确";
                    if (books[i].Type == 1)
                    {
                        state = "错架";
                        wrongCount++;
                    }
                    else if (books[i].Type == 2)
                    {
                        state = "扫描失败";
                        lostCount++;
                    }

                    int scanShelfGroupID = (books[i].ScanGroup - 1) * 6 + books[i].ScanRow;
                    int shelfGroupID = (books[i].group - 1) * 6 + books[i].row;
                    MyListViewItem item = new MyListViewItem(i, books[i].barcode, books[i].title, state,
                        scanShelfGroupID, books[i].ScanLayer,
                        shelfGroupID, books[i].Layer);

                    listViewItems.Add(item);
                }

                listView.Items.Refresh();

                info2.Content = "本层盘点总数" + books.Count + "本";

                info3.Content = "错架" + wrongCount + "本 | 扫描失败" + lostCount + "本";
            }
        }

        private void ButtonStyle(int index)
        {
            for (int i = 0; i < btnList.Count; i++)
            {
                if (i == index)
                {
                    btnList[i].Background = new SolidColorBrush(Colors.Black);
                    btnList[i].Foreground = new SolidColorBrush(Colors.LightGray);
                }
                else
                {
                    btnList[i].Background = new SolidColorBrush(Colors.WhiteSmoke);
                    btnList[i].Foreground = new SolidColorBrush(Colors.DimGray);
                }
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectShelfID = (int)comboBox.SelectedItem;

            info1.Content = "书架号:" + selectShelfID;

            info2.Content = "本层盘点总数0本";

            info3.Content = "错架0本 | 扫描失败0本";

            ButtonStyle(4);
            UpdateView(selectShelfID, 5);
        }

        private void layer1_Click(object sender, RoutedEventArgs e)
        {
            ButtonStyle(0);
            UpdateView(selectShelfID, 1);
        }

        private void layer2_Click(object sender, RoutedEventArgs e)
        {
            ButtonStyle(1);
            UpdateView(selectShelfID, 2);
        }

        private void layer3_Click(object sender, RoutedEventArgs e)
        {
            ButtonStyle(2);
            UpdateView(selectShelfID, 3);
        }

        private void layer4_Click(object sender, RoutedEventArgs e)
        {
            ButtonStyle(3);
            UpdateView(selectShelfID, 4);
        }

        private void layer5_Click(object sender, RoutedEventArgs e)
        {
            ButtonStyle(4);
            UpdateView(selectShelfID, 5);
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MyListViewItem item = (MyListViewItem)listView.SelectedItem;
            if (item == null)
            {
                return;
            }

            BookLocationWindow bookLocationWindow = new BookLocationWindow(item.ScanLayer, item.SN, listViewItems.Count);
            bookLocationWindow.ShowDialog();
        }

        public class MyListViewItem
        {
            private int sn;
            private string barcode;
            private string title;
            private string state;
            private int scanShelf;
            private int scanLayer;
            private int shelf;
            private int layer;

            public MyListViewItem(int sn, string barcode, string title, string state, int scanShelf, int scanLayer, int shelf, int layer)
            {
                this.sn = sn;
                this.barcode = barcode;
                this.title = title;
                
                this.state = state;

                this.scanShelf = scanShelf;
                this.scanLayer = scanLayer;

                this.shelf = shelf;
                this.layer = layer;
            }

            public int SN
            {
                set { sn = value; }
                get { return sn; }
            }

            public string Barcode
            {
                set { barcode = value; }
                get { return barcode; }
            }

            public string Title
            {
                set { title = value; }
                get { return title; }
            }

            public string State
            {
                set { state = value; }
                get { return state; }
            }

            public int ScanShelf
            {
                set { scanShelf = value; }
                get { return scanShelf; }
            }

            public int ScanLayer
            {
                set { scanLayer = value; }
                get { return scanLayer; }
            }

            public int Shelf
            {
                set { shelf = value; }
                get { return shelf; }
            }

            public int Layer
            {
                set { layer = value; }
                get { return layer; }
            }
        }

    }
}
