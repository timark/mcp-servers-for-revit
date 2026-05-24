using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetUserSelectionInRevitCommand : ExternalEventCommandBase
    {
        private SetUserSelectionInRevitEventHandler _handler =>
            (SetUserSelectionInRevitEventHandler)Handler;

        public override string CommandName => "set_user_selection_in_revit";

        public SetUserSelectionInRevitCommand(UIApplication uiApp)
            : base(new SetUserSelectionInRevitEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters?["elementIds"]?.ToObject<List<string>>() ?? new List<string>();
                _handler.SetParameters(elementIds);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("set_user_selection_in_revit timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
