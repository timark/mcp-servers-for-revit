using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetCategoryVisibilityInViewCommand : ExternalEventCommandBase
    {
        private SetCategoryVisibilityInViewEventHandler _handler =>
            (SetCategoryVisibilityInViewEventHandler)Handler;

        public override string CommandName => "set_category_visibility_in_view";

        public SetCategoryVisibilityInViewCommand(UIApplication uiApp)
            : base(new SetCategoryVisibilityInViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var viewId = parameters?["viewId"]?.ToObject<string>() ?? "";
                var categoryIds = parameters?["categoryIds"]?.ToObject<List<long>>() ?? new List<long>();
                var visible = parameters?["visible"]?.ToObject<bool>() ?? true;
                _handler.SetParameters(viewId, categoryIds, visible);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_category_visibility_in_view timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
