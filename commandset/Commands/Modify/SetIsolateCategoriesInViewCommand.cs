using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetIsolateCategoriesInViewCommand : ExternalEventCommandBase
    {
        private SetIsolateCategoriesInViewEventHandler _handler =>
            (SetIsolateCategoriesInViewEventHandler)Handler;

        public override string CommandName => "set_isolate_categories_in_view";

        public SetIsolateCategoriesInViewCommand(UIApplication uiApp)
            : base(new SetIsolateCategoriesInViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var viewId = parameters?["viewId"]?.ToObject<string>() ?? "";
                var categoryIds = parameters?["categoryIds"]?.ToObject<List<long>>() ?? new List<long>();
                _handler.SetParameters(viewId, categoryIds);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_isolate_categories_in_view timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
