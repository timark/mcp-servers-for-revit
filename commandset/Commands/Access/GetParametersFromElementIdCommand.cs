using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetParametersFromElementIdCommand : ExternalEventCommandBase
    {
        private GetParametersFromElementIdEventHandler _handler =>
            (GetParametersFromElementIdEventHandler)Handler;

        public override string CommandName => "get_parameters_from_elementid";

        public GetParametersFromElementIdCommand(UIApplication uiApp)
            : base(new GetParametersFromElementIdEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<string> elementIds = parameters?["elementIds"]?.ToObject<List<string>>()
                    ?? new List<string>();
                List<string> parameterNames = parameters?["parameterNames"]?.ToObject<List<string>>()
                    ?? new List<string>();

                _handler.SetQueryParameters(elementIds, parameterNames);

                if (RaiseAndWaitForCompletion(30000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Get parameters from element ID timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get parameters: {ex.Message}");
            }
        }
    }
}
