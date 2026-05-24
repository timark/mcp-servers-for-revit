using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetCategoryByKeywordCommand : ExternalEventCommandBase
    {
        private GetCategoryByKeywordEventHandler _handler =>
            (GetCategoryByKeywordEventHandler)Handler;

        public override string CommandName => "get_category_by_keyword";

        public GetCategoryByKeywordCommand(UIApplication uiApp)
            : base(new GetCategoryByKeywordEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string keyword = parameters?["keyword"]?.ToObject<string>() ?? "";
                _handler.SetParameters(keyword);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_category_by_keyword timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
