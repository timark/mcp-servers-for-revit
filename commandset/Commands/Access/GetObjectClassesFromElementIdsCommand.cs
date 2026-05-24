using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetObjectClassesFromElementIdsCommand : ExternalEventCommandBase
    {
        private GetObjectClassesFromElementIdsEventHandler _handler =>
            (GetObjectClassesFromElementIdsEventHandler)Handler;

        public override string CommandName => "get_object_classes_from_elementids";

        public GetObjectClassesFromElementIdsCommand(UIApplication uiApp)
            : base(new GetObjectClassesFromElementIdsEventHandler(), uiApp)
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
                    throw new TimeoutException("get_object_classes_from_elementids timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
