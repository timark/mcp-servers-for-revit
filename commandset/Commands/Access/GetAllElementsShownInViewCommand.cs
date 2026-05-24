using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAllElementsShownInViewCommand : ExternalEventCommandBase
    {
        private GetAllElementsShownInViewEventHandler _handler =>
            (GetAllElementsShownInViewEventHandler)Handler;

        public override string CommandName => "get_all_elements_shown_in_view";

        public GetAllElementsShownInViewCommand(UIApplication uiApp)
            : base(new GetAllElementsShownInViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                long viewId = parameters?["viewId"]?.ToObject<long>() ?? 0;
                _handler.SetParameters(viewId);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_all_elements_shown_in_view timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get elements shown in view: {ex.Message}");
            }
        }
    }
}
