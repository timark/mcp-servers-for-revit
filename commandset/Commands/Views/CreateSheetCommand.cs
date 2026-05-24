using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateSheetCommand : ExternalEventCommandBase
    {
        private CreateSheetEventHandler _handler =>
            (CreateSheetEventHandler)Handler;

        public override string CommandName => "create_sheet";

        public CreateSheetCommand(UIApplication uiApp)
            : base(new CreateSheetEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var data = parameters?["data"]?.ToObject<List<SheetCreationInfo>>()
                    ?? new List<SheetCreationInfo>();

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("create_sheet timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create sheet: {ex.Message}");
            }
        }
    }
}
