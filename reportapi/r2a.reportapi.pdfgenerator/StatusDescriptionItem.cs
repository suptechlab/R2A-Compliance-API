using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R2A.ReportApi.PdfGenerator
{
    public class StatusDescriptionItem
    {
        public string Header { get; set; }
        public string HeaderAdditionalInfo { get; set; }
        public string Description { get; set; }

        public List<StatusDescriptionDetailItem> Details { get; set; }
    }

    public class StatusDescriptionDetailItem
    {
        public string Name { get; set; }
        public string Text { get; set; }

        public StatusDescriptionDetailItem()
        {
        }

        public StatusDescriptionDetailItem(string name, string text)
        {
            Name = name;
            Text = text;
        }
    }
}
