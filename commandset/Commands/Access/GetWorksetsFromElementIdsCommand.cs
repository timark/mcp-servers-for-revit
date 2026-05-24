using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetWorksetsFromElementIdsCommand : ExternalEventCommandBase
    {
        private GetWorksetsFromElementIdsEventHandler _handler =>
            (GetWorksetsFromElementIdsEventHandler)Handler;

        public override string CommandName => "get_worksets_from_elementids";

        public GetWorksetsFromElementIdsCommand(UIApplication uiApp)
            : base(new GetWorksetsFromElementIdsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>() ?? new List<string>();
                _handler.SetParameters(elementIds);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_worksets_from_elementids timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
