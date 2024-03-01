using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.UI.Xaml.Diagram.Stencil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Syncfusion.UI.Xaml.Diagram.Layout;
using Syncfusion.UI.Xaml.Diagram.Theming;

namespace Text2TreeTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class EachNode : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string NodeId { get; set; }
        public string ParentId { get; set; }
        
        public string _Color { get; set; }
        public bool IsAndNode { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class EachNodes : ObservableCollection<EachNode>
    {
    }
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            InitializeComponent();
            //Initialize the node collection
            Diagram.Nodes = new NodeCollection();
            Diagram.Connectors = new ConnectorCollection();
            Diagram.Theme = new OfficeTheme();
            
        }

        private EachNodes GetData()
        {
            string tabDelimitedText = ATdescription.Text;
            string[] lines = tabDelimitedText.Split(
                new char[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            EachNodes nodes = new EachNodes();
            Stack<EachNode> parentStack = new Stack<EachNode>();
            int nodeIdCounter = 1; // Initialize node ID counter to ensure unique IDs
            bool rootAdded = false; // Track whether the first root node has been added
            EachNode lastAddedNode = null; // Track the last added node

            foreach (var line in lines)
            {
                int currentLevel = line.Length - line.TrimStart('\t').Length;
                string nodeName = line.TrimStart('\t').TrimEnd();
                string trimmedNodeName = nodeName.TrimStart('\t').TrimEnd();

                if (string.IsNullOrWhiteSpace(nodeName))
                    continue;

                EachNode node = new EachNode()
                {
                    Name = nodeName,
                    NodeId = nodeIdCounter.ToString(), // Use the counter for unique ID
                    _Color = "#034d6d",
                    IsAndNode = trimmedNodeName.StartsWith("AND")
                };
                if (node.IsAndNode)
                {
                    trimmedNodeName = trimmedNodeName.Substring(3).Trim(); // Remove the 'AND' keyword from the node name
                }

                node.Name = trimmedNodeName;

                nodeIdCounter++; // Increment ID counter for the next node

                // Ignore nodes with zero indentation after the first one has been added
                if (currentLevel == 0 && rootAdded)
                    continue;

                // Clear stack to the current level (find the correct parent)
                while (parentStack.Count > currentLevel)
                {
                    parentStack.Pop();
                }

                // Set the parent ID for non-root nodes
                if (parentStack.Count > 0)
                {
                    node.ParentId = parentStack.Peek().NodeId;
                }
                else if (!rootAdded)
                {
                    // If no parent in the stack and the first root node hasn't been added,
                    // set ParentId to null or 0 for the root node
                    node.ParentId = null; // or "0" if you prefer string representation
                    rootAdded = true;
                }

                // Push the current node onto the stack as the new potential parent
                parentStack.Push(node);

                nodes.Add(node);

                // Update the last added node
                lastAddedNode = node;
            }

            return nodes;


        }

 

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {

            Diagram.DataSourceSettings = new DataSourceSettings()
            
            {
                Id = "NodeId",
                ParentId = "ParentId",
                Root = "1",
                DataSource = GetData(),
            };
            Diagram.LayoutManager = new LayoutManager()
            {
                Layout = new DirectedTreeLayout()
                {
                    Type = LayoutType.Hierarchical,
                    Orientation = TreeOrientation.TopToBottom,
                    HorizontalSpacing = 60,
                    VerticalSpacing = 50,
                }, 
                RefreshFrequency = RefreshFrequency.ArrangeParsing, 
            };
        }


        private void MenuItem_OnClic(object sender, RoutedEventArgs e)
        {
            Diagram.PrintingService.ShowDialog = true;
            Diagram.PrintingService.Print();
            // throw new NotImplementedException();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            string tabDelimited = "Fruits\n\tOranges\n\t\tSatsumas\n\t\tTangarines\n\tBananas\n\tGrapes\n\tBerries\n\t\tRasberries\n\t\tBlueberries\n\t\tBlackberries";
            ATdescription.Text = tabDelimited;
        }
    }


}    

