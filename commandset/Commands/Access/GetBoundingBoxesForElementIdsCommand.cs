using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetBoundingBoxesForElementIdsCommand : ExternalEventCommandBase
    {
        private GetBoundingBoxesForElementIdsEventHandler _handler =>
            (GetBoundingBoxesForElementIdsEventHandler)Handler;

        public override string CommandName => "get_boundingboxes_for_element_ids";

        public GetBoundingBoxesForElementIdsCommand(UIApplication uiApp)
            : base(new GetBoundingBoxesForElementIdsEventHandler(), uiApp)
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
                    throw new TimeoutException("get_boundingboxes_for_element_ids timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get bounding boxes: {ex.Message}");
            }
        }
    }
}
