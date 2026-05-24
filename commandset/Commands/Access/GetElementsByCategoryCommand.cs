using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetElementsByCategoryCommand : ExternalEventCommandBase
    {
        private GetElementsByCategoryEventHandler _handler =>
            (GetElementsByCategoryEventHandler)Handler;

        public override string CommandName => "get_elements_by_category";

        public GetElementsByCategoryCommand(UIApplication uiApp)
            : base(new GetElementsByCategoryEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int categoryId = parameters?["categoryId"]?.ToObject<int>() ?? 0;
                _handler.SetParameters(categoryId);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_elements_by_category timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get elements by category: {ex.Message}");
            }
        }
    }
}
