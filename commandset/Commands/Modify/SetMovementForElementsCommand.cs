using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetMovementForElementsCommand : ExternalEventCommandBase
    {
        private SetMovementForElementsEventHandler _handler =>
            (SetMovementForElementsEventHandler)Handler;

        public override string CommandName => "set_movement_for_elements";

        public SetMovementForElementsCommand(UIApplication uiApp)
            : base(new SetMovementForElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>() ?? new List<string>();
                var dx = parameters?["dx"]?.ToObject<double>() ?? 0.0;
                var dy = parameters?["dy"]?.ToObject<double>() ?? 0.0;
                var dz = parameters?["dz"]?.ToObject<double>() ?? 0.0;
                _handler.SetParameters(elementIds, dx, dy, dz);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_movement_for_elements timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
