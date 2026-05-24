using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetLocationForElementIdsCommand : ExternalEventCommandBase
    {
        private GetLocationForElementIdsEventHandler _handler =>
            (GetLocationForElementIdsEventHandler)Handler;

        public override string CommandName => "get_location_for_element_ids";

        public GetLocationForElementIdsCommand(UIApplication uiApp)
            : base(new GetLocationForElementIdsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>()
                    ?? new List<string>();
                _handler.SetParameters(elementIds);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_location_for_element_ids timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get location for element IDs: {ex.Message}");
            }
        }
    }
}
