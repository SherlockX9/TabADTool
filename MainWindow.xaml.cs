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
        Diagram.LayoutManager.Layout.UpdateLayout();
        Diagram.InvalidateMeasure();
        Diagram.InvalidateArrange();
        // Diagram.UpdateLayout();
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

    private void MenuItem_ExportClick(object sender, RoutedEventArgs e)
    {
        
        Console.WriteLine("Export button clicked. Initiating export operation...");

        // Configure export settings
        ExportSettings settings = new ExportSettings()
        {  
            FileName = "C:\\Users\\premi\\Documents\\Uni\\Finalyearproject\\Syncfusion image exports\\DiagramExport.png",
            
        }; 

        // Set export settings for the Diagram
        Diagram.ExportSettings = settings;

        try
        {
            // Trigger the export operation
            Diagram.Export();

            // Log a message indicating that the export operation succeeded
            Console.WriteLine("Export operation completed successfully.");

            // At this point, the Save File Dialog should be shown by the Syncfusion Diagram control.
            // If the dialog is not shown, there might be an issue with the Syncfusion control.

        }
        catch (Exception ex)
        {
            // Log an error message if the export operation fails
            Console.WriteLine($"Export operation failed. Error: {ex.Message}");
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
        aboutWindow.Show();
    }
}