using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.UI.Xaml.Diagram.Controls;
using Syncfusion.UI.Xaml.Diagram.Layout;

namespace Text2TreeTool;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class EachNode : ObservableObject
{
    // [ObservableProperty] private string _connectorColor;
    public string Name { get; set; }
    public string NodeId { get; set; }

    public string ParentId { get; set; }

    // public string _Color { get; set; }
    public bool IsAndNode { get; set; }

    public bool IsParentAndNode { get; set; }
    // [ObservableProperty] public bool IsParentAndNode { get; set; }
    
    public bool IsDefenceNode { get; set; }
    


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
    }

    private EachNodes GetData()
    {
        var tabDelimitedText = ATdescription.Text;
        var lines = tabDelimitedText.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        var nodes = new EachNodes();
        var parentStack = new Stack<EachNode>();
        var nodeIdCounter = 1; // Initialize node ID counter to ensure unique IDs
        var rootAdded = false; // Track whether the first root node has been added
        EachNode lastAddedNode = null; // Track the last added node

        foreach ((var lineNumber, var line) in lines.Select((value, index) => (index + 1, value)))
        {
            var currentLevel = line.Length - line.TrimStart('\t').Length;
            var nodeName = line.TrimStart('\t').TrimEnd();
            var trimmedNodeName = nodeName.TrimStart('\t').TrimEnd();

            if (string.IsNullOrWhiteSpace(nodeName))
            {
                SearchTermTextBox.Text = $"Message: Line {lineNumber} has whitespace but no text.";
            
                continue;
            }


            var node = new EachNode
            {
                Name = nodeName,
                NodeId = nodeIdCounter.ToString(), // Use the counter for unique ID
                IsAndNode = trimmedNodeName.StartsWith("AND"),
                IsDefenceNode = trimmedNodeName.StartsWith("DEFENCE"),
            };
            if (node.IsAndNode)
            {
                trimmedNodeName =
                    trimmedNodeName.Substring(3).Trim(); // Remove the 'AND' keyword from the node name
            }
            else if (node.IsDefenceNode)
            {
                trimmedNodeName =
                    trimmedNodeName.Substring(7).Trim(); // Remove the 'DEFENCE' keyword from the node name
            }

            node.Name = trimmedNodeName;

            nodeIdCounter++; // Increment ID counter for the next node

            // Ignore nodes with zero indentation after the first one has been added
            if (currentLevel == 0 && rootAdded)
                continue;

            // Clear stack to the current level (find the correct parent)
            while (parentStack.Count > currentLevel) parentStack.Pop();

            // Set the parent ID for non-root nodes
            if (parentStack.Count > 0)
            {
                node.ParentId = parentStack.Peek().NodeId;
                // node.IsParentAndNode = parentStack.Peek().IsAndNode;
                node.IsParentAndNode = parentStack.Count > 0 && parentStack.Peek().IsAndNode;
                Console.WriteLine($"Node: {node.Name}, ParentAndNode: {node.IsParentAndNode}");
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

            nodes.Add(new EachNode(){NodeId=node.NodeId, ParentId = node.ParentId, Name = node.Name, IsAndNode = node.IsAndNode, IsParentAndNode = node.IsParentAndNode, IsDefenceNode = node.IsDefenceNode});

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
        Diagram.DataSourceSettings = new DataSourceSettings
        {
            Id = "NodeId",
            ParentId = "ParentId",
            Root = "1",
            DataSource = GetData()
 
        };
        Diagram.LayoutManager = new LayoutManager
        {
            Layout = new DirectedTreeLayout
            {
                Type = LayoutType.Hierarchical,
                Orientation = TreeOrientation.TopToBottom,
                HorizontalSpacing = 60,
                VerticalSpacing = 70
            },
            RefreshFrequency = RefreshFrequency.ArrangeParsing
        };
        Diagram.UpdateLayout();
        Diagram.LayoutManager.Layout.UpdateLayout();
        // Diagram.InvalidateMeasure();
        // Diagram.InvalidateArrange();
        // Diagram.UpdateLayout();
        // UpdateLayout();
    }


    private void MenuItem_OnClic(object sender, RoutedEventArgs e)
    {
        Diagram.PrintingService.ShowDialog = true;
        Diagram.PrintingService.Print();
        // throw new NotImplementedException();
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        var tabDelimited =
            "Bank account\n\tCash Machine\n\t\tPIN\n\t\t\tFind Note\n\t\t\tEavesdrop\n\t\t\tPhysical Force\n\t\tCard\n\tOnline Account\n\t\tPassword\n\t\t\tPhishing\n\t\t\tKey Logger\n\t\tUsername";
        ATdescription.Text = tabDelimited;
    }

    private void MenuItem_PNGClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("PNG button clicked. Initiating export operation...");
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "PNG Files (*.png)|*.png";
        saveFileDialog.Title = "Choose export location";
        saveFileDialog.DefaultExt = ".png";

        // Show the SaveFileDialog
        bool? result = saveFileDialog.ShowDialog();

        // Check if the user selected a file
        if (result == true)
        {
            // Get the selected file path
            string filePath = saveFileDialog.FileName;

            // Export the diagram to the selected file
            Diagram.ExportSettings.ExportType = ExportType.PNG;
            Diagram.ExportSettings.FileName = filePath;
            Diagram.Export();
        }
    }
    private void MenuItem_JPEGClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("JPEG button clicked. Initiating export operation...");
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "JPEG Files (*.jpeg)|*.jpeg";
        saveFileDialog.Title = "Choose export location";
        saveFileDialog.DefaultExt = ".jpeg";

        // Show the SaveFileDialog
        bool? result = saveFileDialog.ShowDialog();

        // Check if the user selected a file
        if (result == true)
        {
            // Get the selected file path
            string filePath = saveFileDialog.FileName;

            // Export the diagram to the selected file
            Diagram.ExportSettings.ExportType = ExportType.JPEG;
            Diagram.ExportSettings.FileName = filePath;
            Diagram.Export();
        }
    }
    private void MenuItem_TIFFClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("TIFF button clicked. Initiating export operation...");
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "TIFF Files (*.tiff)|*.tiff";
        saveFileDialog.Title = "Choose export location";
        saveFileDialog.DefaultExt = ".tiff";

        // Show the SaveFileDialog
        bool? result = saveFileDialog.ShowDialog();

        // Check if the user selected a file
        if (result == true)
        {
            // Get the selected file path
            string filePath = saveFileDialog.FileName;

            // Export the diagram to the selected file
            Diagram.ExportSettings.ExportType = ExportType.TIF;
            Diagram.ExportSettings.FileName = filePath;
            Diagram.Export();
        }
    }
    private void MenuItem_GIFClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("GIF button clicked. Initiating export operation...");
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "GIF Files (*.gif)|*.gif";
        saveFileDialog.Title = "Choose export location";
        saveFileDialog.DefaultExt = ".gif";

        // Show the SaveFileDialog
        bool? result = saveFileDialog.ShowDialog();

        // Check if the user selected a file
        if (result == true)
        {
            // Get the selected file path
            string filePath = saveFileDialog.FileName;

            // Export the diagram to the selected file
            Diagram.ExportSettings.ExportType = ExportType.GIF;
            Diagram.ExportSettings.FileName = filePath;
            Diagram.Export();
        }
    }
    private void MenuItem_BMPClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("BMP button clicked. Initiating export operation...");
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "BMP Files (*.bmp)|*.bmp";
        saveFileDialog.Title = "Choose export location";
        saveFileDialog.DefaultExt = ".bmp";

        // Show the SaveFileDialog
        bool? result = saveFileDialog.ShowDialog();

        // Check if the user selected a file
        if (result == true)
        {
            // Get the selected file path
            string filePath = saveFileDialog.FileName;

            // Export the diagram to the selected file
            Diagram.ExportSettings.ExportType = ExportType.BMP;
            Diagram.ExportSettings.FileName = filePath;
            Diagram.Export();
        }
    }
    private void MenuItem_WDPClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("WDP button clicked. Initiating export operation...");
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "WDP Files (*.wdp)|*.wdp";
        saveFileDialog.Title = "Choose export location";
        saveFileDialog.DefaultExt = ".wdp";

        // Show the SaveFileDialog
        bool? result = saveFileDialog.ShowDialog();

        // Check if the user selected a file
        if (result == true)
        {
            // Get the selected file path
            string filePath = saveFileDialog.FileName;

            // Export the diagram to the selected file
            Diagram.ExportSettings.ExportType = ExportType.WDP;
            Diagram.ExportSettings.FileName = filePath;
            Diagram.Export();
        }
    }
    private void MenuItem_XPSClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("XPS button clicked. Initiating export operation...");
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "XPS Files (*.xps)|*.xps";
        saveFileDialog.Title = "Choose export location";
        saveFileDialog.DefaultExt = ".xps";

        // Show the SaveFileDialog
        bool? result = saveFileDialog.ShowDialog();

        // Check if the user selected a file
        if (result == true)
        {
            // Get the selected file path
            string filePath = saveFileDialog.FileName;

            // Export the diagram to the selected file
            Diagram.ExportSettings.ExportType = ExportType.BMP;
            Diagram.ExportSettings.FileName = filePath;
            Diagram.Export();
        }
    }

    private void MenuItemFullScreen(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        WindowStyle = WindowStyle.None;
        // throw new NotImplementedException();
    }

    private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("clicked");
        // SaveDiagram();
        MessageBox.Show("Diagram saved successfully.", "Save Diagram", MessageBoxButton.OK, MessageBoxImage.Information);
        // throw new NotImplementedException();
    }


    private void LoadMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("clicked");
        
        MessageBox.Show("Diagram loaded successfully.", "Load Diagram", MessageBoxButton.OK, MessageBoxImage.Information);
        // throw new NotImplementedException();
    }
    private void closeButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("\t Do you want to exit? \n \t Unsaved data will be lost", " Confirm",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }


    private void AboutItem_Click(object sender, RoutedEventArgs e)
    {
        AboutWindow aboutWindow = new AboutWindow();
        aboutWindow.ResizeMode = ResizeMode.NoResize;
        aboutWindow.ShowDialog();
        // aboutWindow.Show();
    }
}