using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetModelCategoriesCommand : ExternalEventCommandBase
    {
        private GetModelCategoriesEventHandler _handler =>
            (GetModelCategoriesEventHandler)Handler;

        public override string CommandName => "get_model_categories";

        public GetModelCategoriesCommand(UIApplication uiApp)
            : base(new GetModelCategoriesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                _handler.SetParameters();
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_model_categories timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
