using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetGraphicOverridesForElementsInViewCommand : ExternalEventCommandBase
    {
        private SetGraphicOverridesForElementsInViewEventHandler _handler =>
            (SetGraphicOverridesForElementsInViewEventHandler)Handler;

        public override string CommandName => "set_graphic_overrides_for_elements_in_view";

        public SetGraphicOverridesForElementsInViewCommand(UIApplication uiApp)
            : base(new SetGraphicOverridesForElementsInViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var viewId = parameters?["viewId"]?.ToObject<string>() ?? "";
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>() ?? new List<string>();
                var colorRgb = parameters?["colorRgb"]?.ToObject<List<int>>();
                _handler.SetParameters(viewId, elementIds, colorRgb);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_graphic_overrides_for_elements_in_view timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
