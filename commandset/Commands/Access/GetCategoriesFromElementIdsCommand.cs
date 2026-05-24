using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetCategoriesFromElementIdsCommand : ExternalEventCommandBase
    {
        private GetCategoriesFromElementIdsEventHandler _handler =>
            (GetCategoriesFromElementIdsEventHandler)Handler;

        public override string CommandName => "get_categories_from_elementids";

        public GetCategoriesFromElementIdsCommand(UIApplication uiApp)
            : base(new GetCategoriesFromElementIdsEventHandler(), uiApp)
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
                    throw new TimeoutException("get_categories_from_elementids timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
