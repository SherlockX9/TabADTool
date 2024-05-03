using System.Windows;
using System.Windows.Controls;

namespace Text2TreeTool;

public partial class SampleTrees : Window
{
    public class AttackTree
    {
        public string Name { get; set; }
        public string Description { get; set; } // This holds the tab-delimited text for the tree
    }
    public AttackTree SelectedTree { get; private set; }

    public SampleTrees(List<AttackTree> trees)
    {
        InitializeComponent();
        ListViewTrees.ItemsSource = trees;
    }

    private void ListViewTrees_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedTree = ListViewTrees.SelectedItem as AttackTree;
        if (SelectedTree != null)
        {
            this.DialogResult = true;
        }
    }
}