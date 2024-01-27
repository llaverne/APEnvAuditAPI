using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.Mvc;

namespace APEnvAuditAPI.Models
{
    public class EnvironmentModel
    {
        public IList<string> SelectedEnvironments { get; set; }
        public IList<SelectListItem> AvailableEnvironments { get; set; }

        public EnvironmentModel()
        {
            SelectedEnvironments = new List<string>();
            AvailableEnvironments = new List<SelectListItem>();
        }
    }
}