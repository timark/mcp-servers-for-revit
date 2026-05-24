using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetParameterValueForElementsCommand : ExternalEventCommandBase
    {
        private SetParameterValueForElementsEventHandler _handler =>
            (SetParameterValueForElementsEventHandler)Handler;

        public override string CommandName => "set_parameter_value_for_elements";

        public SetParameterValueForElementsCommand(UIApplication uiApp)
            : base(new SetParameterValueForElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var parameterName = parameters?["parameterName"]?.ToObject<string>() ?? "";
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>() ?? new List<string>();
                var value = parameters?["value"]?.ToObject<string>() ?? "";
                _handler.SetParameters(parameterName, elementIds, value);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_parameter_value_for_elements timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
