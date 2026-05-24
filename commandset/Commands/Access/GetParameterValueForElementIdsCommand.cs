using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetParameterValueForElementIdsCommand : ExternalEventCommandBase
    {
        private GetParameterValueForElementIdsEventHandler _handler =>
            (GetParameterValueForElementIdsEventHandler)Handler;

        public override string CommandName => "get_parameter_value_for_element_ids";

        public GetParameterValueForElementIdsCommand(UIApplication uiApp)
            : base(new GetParameterValueForElementIdsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string parameterName = parameters?["parameterName"]?.ToObject<string>() ?? "";
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>()
                    ?? new List<string>();
                _handler.SetParameters(parameterName, elementIds);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_parameter_value_for_element_ids timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get parameter values: {ex.Message}");
            }
        }
    }
}
