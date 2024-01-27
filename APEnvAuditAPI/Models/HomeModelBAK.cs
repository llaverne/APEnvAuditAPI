using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.Mvc;

namespace APEnvAuditAPI.Models
{
    public class HomeModel
    {
        public IList<string> SelectedServices { get; set; }
        public IList<SelectListItem> AvailableServices { get; set; }

        public HomeModel()
        {
            SelectedServices = new List<string>();
            AvailableServices = new List<SelectListItem>();
        }
    }
}