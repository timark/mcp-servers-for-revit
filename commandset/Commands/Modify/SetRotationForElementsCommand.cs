using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetRotationForElementsCommand : ExternalEventCommandBase
    {
        private SetRotationForElementsEventHandler _handler =>
            (SetRotationForElementsEventHandler)Handler;

        public override string CommandName => "set_rotation_for_elements";

        public SetRotationForElementsCommand(UIApplication uiApp)
            : base(new SetRotationForElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>() ?? new List<string>();
                var radians = parameters?["radians"]?.ToObject<double>() ?? 0.0;
                var axisStartX = parameters?["axisStartX"]?.ToObject<double>() ?? 0.0;
                var axisStartY = parameters?["axisStartY"]?.ToObject<double>() ?? 0.0;
                var axisStartZ = parameters?["axisStartZ"]?.ToObject<double>() ?? 0.0;
                _handler.SetParameters(elementIds, radians, axisStartX, axisStartY, axisStartZ);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_rotation_for_elements timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
