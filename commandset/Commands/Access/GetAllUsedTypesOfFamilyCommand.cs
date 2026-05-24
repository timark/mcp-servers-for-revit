using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAllUsedTypesOfFamilyCommand : ExternalEventCommandBase
    {
        private GetAllUsedTypesOfFamilyEventHandler _handler =>
            (GetAllUsedTypesOfFamilyEventHandler)Handler;

        public override string CommandName => "get_all_used_types_of_a_family";

        public GetAllUsedTypesOfFamilyCommand(UIApplication uiApp)
            : base(new GetAllUsedTypesOfFamilyEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string familyName = parameters?["familyName"]?.ToObject<string>() ?? "";
                _handler.SetParameters(familyName);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("get_all_used_types_of_a_family timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed: {ex.Message}");
            }
        }
    }
}
