using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAllWorksetInformationCommand : ExternalEventCommandBase
    {
        private GetAllWorksetInformationEventHandler _handler =>
            (GetAllWorksetInformationEventHandler)Handler;

        public override string CommandName => "get_all_workset_information";

        public GetAllWorksetInformationCommand(UIApplication uiApp)
            : base(new GetAllWorksetInformationEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_all_workset_information timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
