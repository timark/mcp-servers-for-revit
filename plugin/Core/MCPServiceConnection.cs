using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace revit_mcp_plugin.Core
{
    [Transaction(TransactionMode.Manual)]
    public class MCPServiceConnection : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Obtain socket service.
                SocketService service = SocketService.Instance;

                if (service.IsRunning)
                {
                    service.Stop();
                    Application.ConnectButton.Image = new System.Windows.Media.Imaging.BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Resources/mcp-server-not-connected-16.png", UriKind.RelativeOrAbsolute));
                    Application.ConnectButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Resources/mcp-server-not-connected-32.png", UriKind.RelativeOrAbsolute));
                    TaskDialog.Show("revitMCP", "Close Server");
                }
                else
                {
                    service.Initialize(commandData.Application);
                    service.Start();
                    Application.ConnectButton.Image = new System.Windows.Media.Imaging.BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Resources/mcp-server-connected-16.png", UriKind.RelativeOrAbsolute));
                    Application.ConnectButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Resources/mcp-server-connected-32.png", UriKind.RelativeOrAbsolute));
                    TaskDialog.Show("revitMCP", "Open Server");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
