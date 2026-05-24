using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetElementTypesForElementIdsCommand : ExternalEventCommandBase
    {
        private GetElementTypesForElementIdsEventHandler _handler =>
            (GetElementTypesForElementIdsEventHandler)Handler;

        public override string CommandName => "get_element_types_for_element_ids";

        public GetElementTypesForElementIdsCommand(UIApplication uiApp)
            : base(new GetElementTypesForElementIdsEventHandler(), uiApp)
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
                    throw new TimeoutException("get_element_types_for_element_ids timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
