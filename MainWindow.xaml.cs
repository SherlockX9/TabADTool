using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.UI.Xaml.Diagram.Controls;
using Syncfusion.UI.Xaml.Diagram.Layout;
using ArcSegment = Syncfusion.UI.Xaml.Diagram.ArcSegment;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Text2TreeTool;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
///
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

    public double Cost { get; set; }

    public bool IsSequentialAndNode { get; set; }


    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public static class TextBlockExtension
{
    public static int GetLineCount(this TextBlock tb)
    {
        var propertyInfo = GetPrivatePropertyInfo(typeof(TextBlock), "LineCount");
        var result = (int)propertyInfo.GetValue(tb);
        return result;
    }

    private static PropertyInfo GetPrivatePropertyInfo(Type type, string propertyName)
    {
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic);
        return props.FirstOrDefault(propInfo => propInfo.Name == propertyName);
    }
}

public class EachNodes : ObservableCollection<EachNode>
{
}

public class Route
{
    public List<EachNode> Nodes { get; set; }
    public double TotalCost { get; set; }

    public string Description => $"Cost: {TotalCost}, Path: {string.Join(" -> ", Nodes.Select(n => n.Name))}";

    public Route(List<EachNode> nodes, double totalCost)
    {
        Nodes = nodes;
        TotalCost = totalCost;
    }
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private bool isArcUpdated = false;

    private string filepath = "";
    private Route _mostCostlyRoute;
    private Route _leastCostlyRoute;

    public Route MostCostlyRoute
    {
        get => _mostCostlyRoute;
        set
        {
            if (_mostCostlyRoute != value)
            {
                _mostCostlyRoute = value;
                OnPropertyChanged(nameof(MostCostlyRoute));
            }
        }
    }

    public Route LeastCostlyRoute
    {
        get => _leastCostlyRoute;
        set
        {
            if (_leastCostlyRoute != value)
            {
                _leastCostlyRoute = value;
                OnPropertyChanged(nameof(LeastCostlyRoute));
            }
        }
    }


    public MainWindow()
    {
        SplashScreen splashScreen = new SplashScreen("Images/TabADTolr.png");
        splashScreen.Show(autoClose: true);
        InitializeComponent();
        //Initialize the node collection
        Diagram.Nodes = new NodeCollection();
        Diagram.Connectors = new ConnectorCollection();
        // Diagram.ScrollSettings.ScrollInfo.ZoomPan(new ZoomPositionParameter
        // {
        //     ZoomCommand = ZoomCommand.VerticalScroll,
        //     ScrollDelta = 50,
        // });
        Diagram.Constraints = GraphConstraints.Default & ~GraphConstraints.Selectable;
        // this.ATdescription.StatusBarSettings.Visibility = Visibility.Visible;
        // this.ATdescription.StatusBarSettings.ShowFilePath = Visibility.Visible;
        // this.ATdescription.StatusBarSettings.ShowLineNumber = Visibility.Visible;
        // this.ATdescription.StatusBarSettings.ShowColumnNumber = Visibility.Visible;
    }

    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }


    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
    }

    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
    }

    private void ButtonGenerateTree_OnClick(object sender, RoutedEventArgs e)
    {
        Diagram.Nodes = new NodeCollection();
        Diagram.Connectors = new ConnectorCollection();
        SearchTermTextBox.Text = "";
        bool isValid = ValidateInputAndDisplayErrors(); // Capture the return value to decide next steps
        
        if (!isValid)
        {
            // If the input is not valid, stop further execution and perhaps notify the user
            SearchTermTextBox.Text += "\nFix the errors above to generate the tree.";
            return; // Prevent the execution of further code
        }
        // ValidateInputAndDisplayErrors();

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
                HorizontalSpacing = 25,
                VerticalSpacing = 55,
                Margin = new Thickness(5)
            },
            RefreshFrequency = RefreshFrequency.ArrangeParsing
        };

        Diagram.UpdateLayout();
        Diagram.LayoutManager.Layout.UpdateLayout();
        UpdateArcSizeForAllNodes();

        // Calculate the routes using the data obtained
        EachNodes nodes = GetData();
        CalculateRoutes(nodes);

        // Display the routes
        if (MostCostlyRoute != null && LeastCostlyRoute != null)
        {
            SearchTermTextBox.Text =
                $"Most Costly: {MostCostlyRoute.Description}\nLeast Costly: {LeastCostlyRoute.Description}";
        }
        else
        {
            SearchTermTextBox.Text = "No routes calculated.";
        }
    }


    private EachNodes GetData()
    {
        var tabDelimitedText = ATdescription.Text;
        var lines = tabDelimitedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var nodes = new EachNodes();
        var parentStack = new Stack<EachNode>();
        var nodeIdCounter = 1; // Initialize node ID counter to ensure unique IDs
        var rootAdded = false; // Track whether the first root node has been added

        foreach ((var lineNumber, var line) in lines.Select((value, index) => (index + 1, value)))
        {
            // Ignore lines that only contain tabs or whitespace
            if (string.IsNullOrWhiteSpace(line.Trim('\t')))
            {
                continue;
            }

            var currentLevel = line.Length - line.TrimStart('\t').Length;
            var nodeName = line.TrimStart('\t').TrimEnd();
            var trimmedNodeName = nodeName.TrimStart('\t').TrimEnd();

            // Attempt to parse cost if it exists
            double cost = 0;
            int dollarIndex = trimmedNodeName.LastIndexOf('$'); // Look for the last '$' in the string
            if (dollarIndex != -1) // Check if '$' was found
            {
                var namePart = trimmedNodeName.Substring(0, dollarIndex).TrimEnd();
                var costPart = trimmedNodeName.Substring(dollarIndex + 1).Trim();

                if (double.TryParse(costPart, out cost)) // Try to parse the cost
                {
                    trimmedNodeName = namePart; // Update the node name to exclude the cost
                    Console.WriteLine($"Parsed cost: {cost} for node '{namePart}'"); // Output to console for debugging
                }
            }

            // Determine and trim keywords "&" for AND and "!" for DEFENCE
            bool isAndNode = trimmedNodeName.StartsWith("&");
            bool isDefenceNode = trimmedNodeName.StartsWith("!");
            if (isAndNode)
            {
                trimmedNodeName = trimmedNodeName.Substring(1).Trim(); // Remove "&" and trim again
            }
            else if (isDefenceNode)
            {
                trimmedNodeName = trimmedNodeName.Substring(1).Trim(); // Remove "!" and trim again
            }

            var node = new EachNode
            {
                Name = trimmedNodeName,
                NodeId = nodeIdCounter.ToString(), // Use the counter for unique ID
                IsAndNode = isAndNode,
                IsDefenceNode = isDefenceNode,
                Cost = cost
            };

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
                node.IsParentAndNode = parentStack.Peek().IsAndNode;
            }
            else if (!rootAdded)
            {
                node.ParentId = null;
                rootAdded = true;
            }

            // Push the current node onto the stack as the new potential parent
            parentStack.Push(node);
            nodes.Add(node);
        }

        return nodes;
    }


    private void MenuItem_OnClic(object sender, RoutedEventArgs e)
    {
        Diagram.PrintingService.ShowDialog = true;
        Diagram.PrintingService.Print();
        // throw new NotImplementedException();
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        // var tabDelimited =
        //     "Bank account\n\tCash Machine\n\t\tANDPIN\n\t\t\tFind Note\n\t\t\tEavesdrop\n\t\t\tPhysical Force\n\t\tCard\n\tOnline Account\n\t\tPassword\n\t\t\tPhishing\n\t\t\tKey Logger\n\t\tUsername";
        // ATdescription.Text = tabDelimited;

        ShowTreeSelection();
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


    private void LoadMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("clicked");

        MessageBox.Show("Diagram loaded successfully.", "Load Diagram", MessageBoxButton.OK,
            MessageBoxImage.Information);
        // throw new NotImplementedException();
    }

    private void closeButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("\t Do you want to exit? \n \t Unsaved data will be lost", " Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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

    private void ButtonFullScreen_OnClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow.WindowStyle != WindowStyle.None)
        {
            Application.Current.MainWindow.WindowStyle = WindowStyle.None;
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            FullScreenButton.Header = "Exit Fullscreen";
        }
        else
        {
            Application.Current.MainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            FullScreenButton.Header = "Full screen";
        }
        // throw new NotImplementedException();
    }

    // private void SetFullScreenButtonText(string buttonText)
    // {
    //     FullScreenButton.Header = new DockPanel
    //     {
    //         Children =
    //         {
    //             new Image { Source = new BitmapImage(new Uri("/Icons/")), Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0) },
    //             new TextBlock { Text = buttonText, VerticalAlignment = VerticalAlignment.Center }
    //         }
    //     };
    // }

    private void ButtonZoomIn_OnClick(object sender, RoutedEventArgs e)
    {
        if (Diagram.DataSourceSettings != null && Diagram.DataSourceSettings.DataSource != null &&
            ((IEnumerable<object>)Diagram.DataSourceSettings.DataSource).Any())
        {
            var graphinfo = Diagram.Info as IGraphInfo;

            // For ZoomIn function
            graphinfo.Commands.Zoom.Execute(new ZoomPositionParameter()
            {
                ZoomCommand = ZoomCommand.ZoomIn,
                ZoomFactor = 0.2,
            });
        }
    }

    private void ButtonZoomOut_OnClick(object sender, RoutedEventArgs e)
    {
        if (Diagram.DataSourceSettings is { DataSource: not null } &&
            ((IEnumerable<object>)Diagram.DataSourceSettings.DataSource).Any())
        {
            var graphinfo = Diagram.Info as IGraphInfo;

            // For ZoomOut function
            graphinfo.Commands.Zoom.Execute(new ZoomPositionParameter()
            {
                ZoomCommand = ZoomCommand.ZoomOut,
                ZoomFactor = 0.2,
            });
        }
    }


    private void NewAttackItem_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result =
            MessageBox.Show(
                "Are you sure you want to create a new attack tree? This will clear the current text and tree view.",
                "Confirm New Attack Tree", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        // Check the user's response
        if (result == MessageBoxResult.Yes)
        {
            // Clear the text input
            ATdescription.Text = "";

            // Clear the diagram view by assigning new empty collections to Nodes and Connectors
            Diagram.Nodes = new NodeCollection();
            Diagram.Connectors = new ConnectorCollection();

            // Optionally reset any related data structures or variables
            // For example:
            // currentDiagram.Clear();

            // Optionally, you can set focus back to the text input
            ATdescription.Focus();
        }
    }

    private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ButtonFullScreen_OnClick(sender, e);
        }
    }

    private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            SaveAsMenuItem_Click(sender, e);
        }
        else
        {
            File.WriteAllText(filepath, ATdescription.Text);
            MessageBox.Show("File saved successfully.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
        if (saveFileDialog.ShowDialog() != true) return;
        File.WriteAllText(saveFileDialog.FileName, ATdescription.Text);
        filepath = saveFileDialog.FileName;
        MessageBox.Show("File saved successfully.", "Save As", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ImportTextFile()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Text Files (*.txt)|*.txt";
        openFileDialog.Title = "Select a Text File to Import";

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;

            try
            {
                // Read the text from the selected file
                string text = File.ReadAllText(filePath);

                // Set the text to the TextBox or wherever you want to display it
                ATdescription.Text = text;

                MessageBox.Show("File imported successfully.", "Import Successful", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing file: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

// Event handler for the Import button click
    private void ImportMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ImportTextFile();
    }

    // private void ValidateInputAndDisplayErrors()
    // {
    //     // Clear previous error messages
    //     SearchTermTextBox.Text = "";
    //
    //     var lines = ATdescription.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    //     foreach (var line in lines)
    //     {
    //         Console.WriteLine(line);
    //     }
    //
    //     var errorMessage = new StringBuilder();
    //
    //     for (int i = 0; i < lines.Length; i++)
    //     {
    //         if (string.IsNullOrWhiteSpace(lines[i].Trim()))
    //         {
    //             errorMessage.AppendLine($"Empty line found at line {i + 1}. Please remove unnecessary blank lines.");
    //         }
    //     }
    //
    //     if (errorMessage.Length > 0)
    //     {
    //         // Display new error messages
    //         SearchTermTextBox.Text = errorMessage.ToString();
    //     }
    // }

    private bool ValidateInputAndDisplayErrors()
    {
        SearchTermTextBox.Text = "";
        var lines = ATdescription.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

        // Remove trailing empty lines
        int lastNonEmptyLine = Array.FindLastIndex(lines, line => !string.IsNullOrWhiteSpace(line));
        lines = lines.Take(lastNonEmptyLine + 1).ToArray();

        if (lines.Length == 0) {
            SearchTermTextBox.Text = "No content found. Please enter valid tree data.";
            return false;
        }

        // Check if the first line is empty or improperly formatted
        if (string.IsNullOrWhiteSpace(lines[0]) || lines[0].StartsWith("\t"))
        {
            SearchTermTextBox.Text = "The root node must be on the first line and must not start with a tab.";
            return false;
        }

        // Check for more than one root node
        int rootNodesCount = lines.Count(line => !line.StartsWith("\t") && !string.IsNullOrWhiteSpace(line));
        if (rootNodesCount > 1)
        {
            SearchTermTextBox.Text = "Only one root node is allowed. Please remove extra root nodes.";
            return false;
        }

        // Validate child nodes and empty lines


        return true;
    }



    //
    // private void NodeBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    // {
    //     if (!isArcUpdated)
    //     {
    //         var border = sender as Border;
    //         if (border != null && border.ActualWidth > 0)
    //         {
    //             
    //             if (border != null && border.ActualWidth > 0)
    //             {
    //                 var arcPath = border.FindName("ArcPath") as System.Windows.Shapes.Path;
    //                 if (arcPath != null)
    //                 {
    //                     var geometry = arcPath.Data as PathGeometry;
    //                     if (geometry != null && geometry.Figures.Count > 0)
    //                     {
    //                         var figure = geometry.Figures.First();
    //                         var arcSegment = figure.Segments[0] as System.Windows.Media.ArcSegment;
    //                         if (arcSegment != null)
    //                         {
    //                             // Adjust the endpoint to the right edge of the border
    //                             arcSegment.Point = new Point(border.ActualWidth, 0);
    //                             // Set the size of the arc to half the width of the border, making a perfect semi-circle
    //                             arcSegment.Size = new Size(border.ActualWidth / 2, 25);
    //                         }
    //                     }
    //                 }
    //             }
    //             isArcUpdated = true; // Set flag to true to prevent further updates
    //         }
    //     }
    //
    // }


    private void NodeBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var border = sender as Border;
        if (border != null && e.PreviousSize.Width != e.NewSize.Width && !isArcUpdated)
        {
            UpdateArc(border);
        }
    }


    private void UpdateArcSizeForAllNodes()
    {
        var borders = FindVisualChildren<Border>(Diagram).Where(b => b.DataContext is EachNode);
        foreach (Border border in borders)
        {
            UpdateArc(border);
        }
    }

    // private void UpdateArc(Border border)
    // {
    //     var arcPath = border.FindName("ArcPath") as System.Windows.Shapes.Path;
    //     if (arcPath != null)
    //     {
    //         var geometry = arcPath.Data as PathGeometry;
    //         if (geometry != null && geometry.Figures.Count > 0)
    //         {
    //             var figure = geometry.Figures.First();
    //             var arcSegment = figure.Segments[0] as System.Windows.Media.ArcSegment;
    //             if (arcSegment != null)
    //             {
    //                 arcSegment.Point = new Point(border.ActualWidth, 0);
    //                 arcSegment.Size = new Size(border.ActualWidth / 2, 25); // Ensure the arc is a semi-circle
    //             }
    //         }
    //     }
    // }

    private void UpdateArc(Border border)
    {
        isArcUpdated = true;
        var arcPath = border.FindName("ArcPath") as System.Windows.Shapes.Path;
        if (arcPath != null)
        {
            var geometry = arcPath.Data as PathGeometry;
            if (geometry != null && geometry.Figures.Count > 0)
            {
                var figure = geometry.Figures.First();
                var arcSegment = figure.Segments[0] as System.Windows.Media.ArcSegment;
                if (arcSegment != null)
                {
                    var newEndPoint = new Point(border.ActualWidth, 0);
                    var newSize = new Size(border.ActualWidth / 2, 25); // Ensure the arc is a semi-circle

                    // Check if the update is necessary to prevent infinite loop
                    if (arcSegment.Point != newEndPoint || arcSegment.Size != newSize)
                    {
                        arcSegment.Point = newEndPoint;
                        arcSegment.Size = newSize;
                    }
                }
            }
        }

        isArcUpdated = false;
    }


    private void CalculateRoutes(EachNodes nodes)
    {
        var rootNodes = nodes.Where(n => string.IsNullOrEmpty(n.ParentId));
        double maxCost = 0;
        double minCost = double.MaxValue;
        Route mostCostly = null;
        Route leastCostly = null;

        foreach (var root in rootNodes)
        {
            var currentCost = 0.0;
            var currentPath = new List<EachNode>();
            FindCostlyPaths(root, nodes, currentPath, ref currentCost, ref maxCost, ref minCost, ref mostCostly,
                ref leastCostly);
        }

        MostCostlyRoute = mostCostly;
        LeastCostlyRoute = leastCostly;
    }


    //
    //
    // private void FindCostlyPaths(EachNode node, ObservableCollection<EachNode> nodes, List<EachNode> currentPath, ref double currentCost, ref double maxCost, ref double minCost, ref Route mostCostly, ref Route leastCostly)
    // {
    //     currentPath.Add(node);
    //     currentCost += node.Cost;
    //
    //     var children = nodes.Where(n => n.ParentId == node.NodeId).ToList();
    //     if (!children.Any()) // Leaf node
    //     {
    //         if (currentCost > maxCost)
    //         {
    //             maxCost = currentCost;
    //             mostCostly = new Route(new List<EachNode>(currentPath), currentCost);
    //         }
    //         if (currentCost < minCost)
    //         {
    //             minCost = currentCost;
    //             leastCostly = new Route(new List<EachNode>(currentPath), currentCost);
    //         }
    //     }
    //     else
    //     {
    //         foreach (var child in children)
    //         {
    //             FindCostlyPaths(child, nodes, new List<EachNode>(currentPath), ref currentCost, ref maxCost, ref minCost, ref mostCostly, ref leastCostly);
    //         }
    //     }
    //
    //     currentPath.RemoveAt(currentPath.Count - 1);
    //     currentCost -= node.Cost;
    // }


    private void FindCostlyPaths(EachNode node, ObservableCollection<EachNode> nodes, List<EachNode> currentPath,
        ref double currentCost, ref double maxCost, ref double minCost, ref Route mostCostly, ref Route leastCostly)
    {
        currentPath.Add(node);
        currentCost += node.Cost;

        var children = nodes.Where(n => n.ParentId == node.NodeId).ToList();
        if (!children.Any()) // Leaf node
        {
            if (currentCost > maxCost)
            {
                maxCost = currentCost;
                mostCostly = new Route(new List<EachNode>(currentPath), currentCost);
            }

            if (currentCost < minCost)
            {
                minCost = currentCost;
                leastCostly = new Route(new List<EachNode>(currentPath), currentCost);
            }
        }
        else
        {
            foreach (var child in children)
            {
                FindCostlyPaths(child, nodes, new List<EachNode>(currentPath), ref currentCost, ref maxCost,
                    ref minCost, ref mostCostly, ref leastCostly);
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        currentCost -= node.Cost;
    }


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


    // private void AppendStyledText(string text, FontWeight fontWeight, double fontSize, SolidColorBrush color)
    // {
    //     Paragraph paragraph = new Paragraph();
    //     paragraph.Inlines.Add(new Run(text)
    //     {
    //         FontWeight = fontWeight,
    //         FontSize = fontSize,
    //         Foreground = color
    //     });
    //     SearchTermTextBox.Document.Blocks.Add(paragraph);
    // }

    private void ShowTreeSelection()
    {
        // ResetDiagram();
        var trees = new List<SampleTrees.AttackTree>
        {
         
            new SampleTrees.AttackTree
            {
                Name = "Bank account fraud v.1",
                Description =
                    "Bank Account\n\tCash Machine\n\t\tPIN\n\t\t\tFind Note\n\t\t\tEavesdrop\n\t\t\tPhysical Force \n\t\tCard\n\tOnline\n\t\tPassword\n\t\t\tPhishing\n\t\t\tKey Logger\n\t\tUsername"
            },
            new SampleTrees.AttackTree
            {
                Name = "Bank account fraud v.2",
                Description =
                    "Bank Account\n\t&Cash Machine\n\t\tPIN\n\t\t\tFind Note\n\t\t\t\t!Memorize Pin\n\t\t\tEavesdrop\n\t\t\t\t!Shield Pin\n\t\t\tPhysical Force \n\t\tCard\n\t&Online\n\t\tPassword\n\t\t\tPhishing\n\t\t\tKey Logger\n\t\tUsername"
            },
            new SampleTrees.AttackTree
            {
                Name = "Bank account fraud v.3",
                Description =
                    "Bank Account\n\t&Cash Machine\n\t\tPIN $110\n\t\t\tFind Note $200\n\t\t\t\t!Memorize Pin\n\t\t\tEavesdrop $50\n\t\t\tPhysical Force $400\n\t\tCard $70\n\t&Online\n\t\tPassword $210\n\t\t\tPhishing $60\n\t\t\tKey Logger $200\n\t\tUsername $40"
            },
            new SampleTrees.AttackTree
            {
                Name = "Open Safe ",
                Description =
                    "Open Safe\n\tPick Lock\n\tLearn Combo\n\t\tFind Written Combo\n\t\tGet Combo From Target\n\t\t\tThreaten\n\t\t\tBlackmail\n\t\t\t&Eavesdrop\n\t\t\t\tListen to conversation\n\t\t\t\tGet Target to State Combo\t\n\t\t\tBribe\n\tCut Safe Open\n\tInstall Improperly\t"
            },
            new SampleTrees.AttackTree
            {
                Name = "Compromise server",
                Description =
                    "Steal data stored in server\n\tObtain root privileges\n\t\tSteal access to a user with root priviliges\n\t\t\t!Change the password periodically\n\t\t\t!Log out the pc after use\n\t\t\t!Add an identification token\n\t\tCorrupt a user with root privileges\n\t\t\t!Add an indetification token\n\t\t\t!Motivate employees\n\tAttack the system with a remote login\n\t\tExploit an on-line vulnerability\n\t\t\t!Update the system periodically\n\t\t\t!Separate the contents on the server\n\t\tExploit a web server vulnerability\n\t\t\t!Use an anti-virus software\n\t\t\t!Stop suspicious attachments\n\t&Steal the server\n\t\tAccess to the server room\n\t\t\t!Install a security door\n\t\t\t!Install a safety lock\n\t\tGo out unobserved\n\t\t\t!Install video surveillance equipment\n\t\t\t!Employ a security guard"
            }
        };
        SampleTrees selectWindow = new SampleTrees(trees);
        if (selectWindow.ShowDialog() == true)
        {
            DisplayTree(selectWindow.SelectedTree);
            
        }
    }


    private void DisplayTree(SampleTrees.AttackTree tree)
    {
        // First, update the text box with the tree description
        ATdescription.Text = tree.Description;

        // Reset and prepare the diagram for new data
        ResetDiagram();

        // Parse the description into nodes, including cost parsing
        Dispatcher.Invoke(() =>
        {
            EachNodes nodes = GetDataFromDescription(tree.Description);

            // Configure the diagram's data source settings
            Diagram.DataSourceSettings.DataSource = nodes;

            // Apply layout settings and update diagram layout
            Diagram.LayoutManager.Layout.UpdateLayout();

            // Perform layout updates before calculating routes to ensure all nodes are positioned correctly
            Diagram.LayoutManager.Layout.UpdateLayout();

            // After the nodes are updated, calculate the routes to update cost paths
            CalculateRoutes(nodes);

            // Update the UI to display the calculated most and least costly routes
            if (MostCostlyRoute != null && LeastCostlyRoute != null)
            {
                SearchTermTextBox.Text =
                    $"Most Costly: {MostCostlyRoute.Description}\nLeast Costly: {LeastCostlyRoute.Description}";
            }
            else
            {
                SearchTermTextBox.Text = "No routes calculated.";
            }
        }, DispatcherPriority.DataBind);

        // Update visual elements if needed
        Diagram.LayoutUpdated += Diagram_LayoutUpdated;
    }

    private void Diagram_LayoutUpdated(object sender, EventArgs e)
    {
        // Unsubscribe from the LayoutUpdated event to prevent multiple calls
        Diagram.LayoutUpdated -= Diagram_LayoutUpdated;

        // Additional updates post layout, if required
        UpdateArcSizeForAllNodes();
    }



    private void ResetDiagram()
    {
        // Clear existing nodes and connectors
        Diagram.Nodes = new NodeCollection();
        Diagram.Connectors = new ConnectorCollection();

        // Re-initialize DataSourceSettings and LayoutManager to their default configurations
        Diagram.DataSourceSettings = new DataSourceSettings
        {
            Id = "NodeId",
            ParentId = "ParentId",
            Root = "1" // Set this according to what your application needs
        };

        // Initialize a new layout to avoid any null references
        Diagram.LayoutManager = new LayoutManager
        {
            Layout = new DirectedTreeLayout
            {
                Type = LayoutType.Hierarchical,
                Orientation = TreeOrientation.TopToBottom,
                HorizontalSpacing = 25,
                VerticalSpacing = 55,
                Margin = new Thickness(25)
            }
        };
    }


    private void GenerateTreeVisualization(string treeDescription)
    {
        try
        {
            // Parse the new description into nodes
            EachNodes nodes = GetDataFromDescription(treeDescription);

            // Configure the diagram's data source settings consistently
            Diagram.DataSourceSettings = new DataSourceSettings
            {
                Id = "NodeId",
                ParentId = "ParentId",
                Root = "1", // Ensure there's a default root set, adjust as necessary for your data
                DataSource = nodes
            };

            // Reapply the diagram's layout
            Diagram.LayoutManager.Layout = new DirectedTreeLayout
            {
                Type = LayoutType.Hierarchical,
                Orientation = TreeOrientation.TopToBottom,
                HorizontalSpacing = 50,
                VerticalSpacing = 75,
                Margin = new Thickness(25)
            };


            Diagram.LayoutManager.Layout.UpdateLayout();
        }
        catch (Exception ex)
        {
            // Log the exception or display a message to the user
            Console.WriteLine($"An error occurred while generating the tree visualization: {ex.Message}");
        }
    }


    private EachNodes GetDataFromDescription(string description)
    {
        var nodes = new EachNodes();
        var parentStack = new Stack<EachNode>();
        var nodeIdCounter = 1;
        var rootAdded = false;

        var lines = description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            ProcessLine(line, nodes, parentStack, ref nodeIdCounter, ref rootAdded);
        }

        return nodes;
    }

    private void ProcessLine(string line, EachNodes nodes, Stack<EachNode> parentStack, ref int nodeIdCounter,
        ref bool rootAdded)
    {
        var currentLevel = CountLeadingTabs(line);
        var trimmedNodeName = line.TrimStart('\t').Trim();

        // Logic to parse the node attributes such as name, cost, and type (AND, DEFENSE, etc.)
        ParseNodeAttributes(ref trimmedNodeName, out double cost, out bool isAndNode, out bool isDefenseNode);

        var node = new EachNode
        {
            Name = trimmedNodeName,
            NodeId = nodeIdCounter.ToString(),
            ParentId = parentStack.Count > 0 ? parentStack.Peek().NodeId : null,
            IsAndNode = isAndNode,
            IsDefenceNode = isDefenseNode,
            Cost = cost
        };

        nodeIdCounter++;

        // Manage the stack to ensure the correct hierarchical structure
        while (parentStack.Count > currentLevel)
            parentStack.Pop();

        if (currentLevel == 0 && !rootAdded)
        {
            node.ParentId = null;
            rootAdded = true;
        }

        if (parentStack.Any())
        {
            node.ParentId = parentStack.Peek().NodeId;
        }

        parentStack.Push(node);
        nodes.Add(node);
    }

    private int CountLeadingTabs(string line)
    {
        return line.TakeWhile(c => c == '\t').Count();
    }

    private void ParseNodeAttributes(ref string nodeName, out double cost, out bool isAndNode, out bool isDefenseNode)
    {
        // Logic to extract cost and identify AND, DEFENSE nodes
        isAndNode = nodeName.StartsWith("&");
        isDefenseNode = nodeName.StartsWith("!");
        if (isAndNode || isDefenseNode)
            nodeName = nodeName.Substring(1).Trim();

        int dollarIndex = nodeName.LastIndexOf('$');
        cost = 0;
        if (dollarIndex != -1)
        {
            if (double.TryParse(nodeName.Substring(dollarIndex + 1), out cost))
                nodeName = nodeName.Substring(0, dollarIndex).Trim();
        }
    }
}