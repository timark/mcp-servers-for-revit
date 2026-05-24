using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAllUsedFamiliesCommand : ExternalEventCommandBase
    {
        private GetAllUsedFamiliesEventHandler _handler =>
            (GetAllUsedFamiliesEventHandler)Handler;

        public override string CommandName => "get_all_used_families_in_model";

        public GetAllUsedFamiliesCommand(UIApplication uiApp)
            : base(new GetAllUsedFamiliesEventHandler(), uiApp)
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
                    throw new TimeoutException("get_all_used_families_in_model timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
