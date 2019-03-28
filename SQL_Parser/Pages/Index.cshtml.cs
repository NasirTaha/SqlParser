using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace SQL_Parser.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string Result { get; set; }
        public void OnGet()
        {
        }
   
        public JsonResult OnPostParse()
        {
            string query = "";
            {
                MemoryStream stream = new MemoryStream();
                Request.Body.CopyTo(stream);
                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream))
                {
                    string requestBody = reader.ReadToEnd();
                    if (requestBody.Length > 0)
                    {
                        var obj = JsonConvert.DeserializeObject<Query>(requestBody);
                        if (obj != null)
                        {
                            query  = obj.QueryText;
                        }
                    }
                }
            }
            List<string> lstString = new List<string>
            {
                query
            };
            return new JsonResult(lstString);
        }
    }
    public class Query
    {
        public string QueryText { get; set; }
    }
}
