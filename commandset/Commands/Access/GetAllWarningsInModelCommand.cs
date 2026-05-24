using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAllWarningsInModelCommand : ExternalEventCommandBase
    {
        private GetAllWarningsInModelEventHandler _handler =>
            (GetAllWarningsInModelEventHandler)Handler;

        public override string CommandName => "get_all_warnings_in_model";

        public GetAllWarningsInModelCommand(UIApplication uiApp)
            : base(new GetAllWarningsInModelEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                _handler.SetParameters();
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_all_warnings_in_model timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get warnings: {ex.Message}");
            }
        }
    }
}
