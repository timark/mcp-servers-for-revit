using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAllElementsOfFamiliesCommand : ExternalEventCommandBase
    {
        private GetAllElementsOfFamiliesEventHandler _handler =>
            (GetAllElementsOfFamiliesEventHandler)Handler;

        public override string CommandName => "get_all_elements_of_specific_families";

        public GetAllElementsOfFamiliesCommand(UIApplication uiApp)
            : base(new GetAllElementsOfFamiliesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var familyNames = parameters?["familyNames"]?.ToObject<List<string>>()
                    ?? new List<string>();
                _handler.SetParameters(familyNames);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_all_elements_of_specific_families timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
