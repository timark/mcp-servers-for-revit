using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetElementsOnLevelCommand : ExternalEventCommandBase
    {
        private GetElementsOnLevelEventHandler _handler =>
            (GetElementsOnLevelEventHandler)Handler;

        public override string CommandName => "get_elements_on_level";

        public GetElementsOnLevelCommand(UIApplication uiApp)
            : base(new GetElementsOnLevelEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string levelName = parameters?["levelName"]?.ToObject<string>() ?? "";
                var categories = parameters?["categories"]?.ToObject<List<string>>()
                    ?? new List<string>();
                _handler.SetParameters(levelName, categories);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_elements_on_level timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get elements on level: {ex.Message}");
            }
        }
    }
}
