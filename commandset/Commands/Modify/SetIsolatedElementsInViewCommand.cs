using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetIsolatedElementsInViewCommand : ExternalEventCommandBase
    {
        private SetIsolatedElementsInViewEventHandler _handler =>
            (SetIsolatedElementsInViewEventHandler)Handler;

        public override string CommandName => "set_isolated_elements_in_view";

        public SetIsolatedElementsInViewCommand(UIApplication uiApp)
            : base(new SetIsolatedElementsInViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var viewId = parameters?["viewId"]?.ToObject<string>() ?? "";
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>() ?? new List<string>();
                _handler.SetParameters(viewId, elementIds);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_isolated_elements_in_view timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
