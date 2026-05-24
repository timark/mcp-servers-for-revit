using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetResetCategoryVisibilityInViewCommand : ExternalEventCommandBase
    {
        private SetResetCategoryVisibilityInViewEventHandler _handler =>
            (SetResetCategoryVisibilityInViewEventHandler)Handler;

        public override string CommandName => "set_reset_category_visibility_in_view";

        public SetResetCategoryVisibilityInViewCommand(UIApplication uiApp)
            : base(new SetResetCategoryVisibilityInViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var viewId = parameters?["viewId"]?.ToObject<string>() ?? "";
                _handler.SetParameters(viewId);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_reset_category_visibility_in_view timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
